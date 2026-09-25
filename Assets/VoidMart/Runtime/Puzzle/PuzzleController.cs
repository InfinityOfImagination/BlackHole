using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Monetization;
using VoidMart.Services;

namespace VoidMart.Puzzle
{
    /// <summary>
    /// The 5-second unjam minigame.  A board is generated that is *always* solvable in a single
    /// placement, so the interruption stays a satisfying burst rather than a difficulty spike:
    /// one tray piece is guaranteed to complete a line.
    /// </summary>
    public class PuzzleController : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] PuzzlePieceSet m_PieceSet;

        readonly List<PuzzleShape> m_Tray = new List<PuzzleShape>(4);
        readonly PuzzleGrid m_Grid = new PuzzleGrid();
        System.Random m_Random;

        public event Action Generated;
        public event Action<int, int> PiecePlaced;     // trayIndex, linesCleared
        public event Action<float> TimerTick;          // normalised remaining
        public event Action<PuzzleResult> Finished;

        float m_TimeRemaining;
        float m_IntroTimer;
        bool m_Running;
        int m_LinesCleared;
        int m_BlocksPlaced;
        string m_MachineId;
        float m_CachedTimeScale = 1f;

        public PuzzleGrid Grid => m_Grid;
        public IReadOnlyList<PuzzleShape> Tray => m_Tray;
        public bool Running => m_Running;
        public float TimeRemaining => m_TimeRemaining;
        public float TimeNormalized => m_Config == null || m_Config.puzzle.timeLimit <= 0f ? 0f : Mathf.Clamp01(m_TimeRemaining / m_Config.puzzle.timeLimit);
        public string MachineId => m_MachineId;
        public int LinesCleared => m_LinesCleared;

        public void Configure(GameConfig config, PuzzlePieceSet pieceSet)
        {
            m_Config = config;
            m_PieceSet = pieceSet;
        }

        void Awake()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            if (ServiceLocator.Get<PuzzleController>() == this) ServiceLocator.Unregister<PuzzleController>();
        }

        void Start()
        {
            var bus = m_Config != null ? m_Config.events : null;
            bus?.puzzleRequested?.Register(OnPuzzleRequested);
        }

        void OnDisable()
        {
            var bus = m_Config != null ? m_Config.events : null;
            bus?.puzzleRequested?.Unregister(OnPuzzleRequested);
        }

        void OnPuzzleRequested(string machineId) => Begin(machineId);

        // ------------------------------------------------------------- lifecycle

        public void Begin(string machineId)
        {
            if (m_Running || m_Config == null) return;

            m_MachineId = machineId;
            m_Random = new System.Random(Environment.TickCount ^ machineId?.GetHashCode() ?? 0);
            m_LinesCleared = 0;
            m_BlocksPlaced = 0;
            m_TimeRemaining = m_Config.puzzle.timeLimit;
            m_IntroTimer = m_Config.puzzle.introSeconds;
            m_Running = true;

            Generate();

            if (m_Config.puzzle.pauseWorldDuringPuzzle)
            {
                m_CachedTimeScale = Time.timeScale;
                Time.timeScale = Mathf.Clamp(m_Config.puzzle.slowMotionScale, 0f, 1f);
            }

            ServiceLocator.Get<AudioService>()?.PlayMusic(AudioService.MusicPuzzle);
            Generated?.Invoke();
        }

        void Update()
        {
            if (!m_Running) return;

            float deltaTime = Time.unscaledDeltaTime;
            if (m_IntroTimer > 0f)
            {
                m_IntroTimer -= deltaTime;
                return;
            }

            m_TimeRemaining -= deltaTime;
            TimerTick?.Invoke(TimeNormalized);

            if (m_TimeRemaining <= 0f) Finish(false);
        }

        public void Abort() => Finish(false);

        void Finish(bool solved)
        {
            if (!m_Running) return;
            m_Running = false;

            if (m_Config.puzzle.pauseWorldDuringPuzzle) Time.timeScale = m_CachedTimeScale <= 0f ? 1f : m_CachedTimeScale;

            double reward = 0d;
            if (solved)
            {
                var puzzle = m_Config.puzzle;
                reward = puzzle.rewardBase + puzzle.rewardPerLine * m_LinesCleared;
                if (m_LinesCleared > 1) reward *= Math.Pow(puzzle.comboMultiplier, m_LinesCleared - 1);

                var economy = ServiceLocator.Get<EconomyService>();
                if (economy != null) reward *= economy.GetMultiplier(UpgradeEffect.SellPrice);
            }

            var result = new PuzzleResult
            {
                solved = solved,
                linesCleared = m_LinesCleared,
                blocksPlaced = m_BlocksPlaced,
                reward = reward,
                machineId = m_MachineId
            };

            var audio = ServiceLocator.Get<AudioService>();
            audio?.PlaySfx(solved ? AudioService.SfxClear : AudioService.SfxJam, solved ? 1.2f : 0.8f);
            audio?.PlayMusicForArea(GameManager.Instance != null ? GameManager.Instance.Area : WorldArea.Store);

            m_Config.events?.puzzleCompleted?.Raise(result);
            Finished?.Invoke(result);
        }

        /// <summary>Rewarded-video bail-out: fixes the machine without solving the board.</summary>
        public void SkipWithAd()
        {
            if (!m_Running || m_Config == null) return;
            var ads = ServiceLocator.Get<IAdService>();
            if (ads == null)
            {
                m_LinesCleared = Mathf.Max(1, m_LinesCleared);
                Finish(true);
                return;
            }
            ads.ShowRewarded("rv_unjam_skip", success =>
            {
                if (!success) return;
                m_LinesCleared = Mathf.Max(1, m_LinesCleared);
                Finish(true);
            });
        }

        // ------------------------------------------------------------ generation

        void Generate()
        {
            var puzzle = m_Config.puzzle;
            m_Grid.Resize(puzzle.gridWidth, puzzle.gridHeight);
            m_Grid.Clear();
            m_Tray.Clear();

            var set = m_PieceSet;
            var pool = set != null && set.shapes.Count > 0 ? set.shapes : PuzzlePieceSet.DefaultShapes();

            // 1. Pick the guaranteed solution piece and where it will land.
            var key = pool[m_Random.Next(0, pool.Count)];
            int maxX = Mathf.Max(0, m_Grid.Width - key.Width);
            int maxY = Mathf.Max(0, m_Grid.Height - key.Height);
            var origin = new Vector2Int(m_Random.Next(0, maxX + 1), m_Random.Next(0, maxY + 1));

            var reserved = new HashSet<int>();
            for (int i = 0; i < key.cells.Count; i++)
            {
                var cell = key.cells[i] + origin;
                reserved.Add(cell.y * m_Grid.Width + cell.x);
            }

            // 2. Scatter noise everywhere except the reserved landing cells.
            int colours = 5;
            for (int y = 0; y < m_Grid.Height; y++)
            {
                for (int x = 0; x < m_Grid.Width; x++)
                {
                    if (reserved.Contains(y * m_Grid.Width + x)) continue;
                    if (m_Random.NextDouble() < puzzle.prefillDensity)
                        m_Grid.Set(x, y, m_Random.Next(0, colours) + 1);
                }
            }

            // 3. Complete the target row so the key piece finishes it exactly.
            int targetRow = (key.cells[0] + origin).y;
            for (int x = 0; x < m_Grid.Width; x++)
            {
                int index = targetRow * m_Grid.Width + x;
                if (reserved.Contains(index)) m_Grid.Set(x, targetRow, 0);
                else if (!m_Grid.IsOccupied(x, targetRow)) m_Grid.Set(x, targetRow, m_Random.Next(0, colours) + 1);
            }

            // 4. Never hand out a board that is already solved.
            m_Grid.ResolveLines();
            for (int x = 0; x < m_Grid.Width; x++)
            {
                int index = targetRow * m_Grid.Width + x;
                if (reserved.Contains(index)) m_Grid.Set(x, targetRow, 0);
                else if (!m_Grid.IsOccupied(x, targetRow)) m_Grid.Set(x, targetRow, m_Random.Next(0, colours) + 1);
            }

            // 5. Fill the tray, key piece in a random slot.
            int trayCount = Mathf.Max(1, puzzle.trayPieces);
            int keySlot = m_Random.Next(0, trayCount);
            for (int i = 0; i < trayCount; i++)
                m_Tray.Add(i == keySlot ? key : pool[m_Random.Next(0, pool.Count)]);
        }

        // -------------------------------------------------------------- playing

        public bool CanPlace(int trayIndex, Vector2Int origin)
        {
            if (!m_Running || trayIndex < 0 || trayIndex >= m_Tray.Count || m_Tray[trayIndex] == null) return false;
            return m_Grid.CanPlace(m_Tray[trayIndex], origin);
        }

        public bool TryPlace(int trayIndex, Vector2Int origin)
        {
            if (!CanPlace(trayIndex, origin)) return false;

            var shape = m_Tray[trayIndex];
            m_Grid.Place(shape, origin, m_Random.Next(0, 5));
            m_Tray[trayIndex] = null;
            m_BlocksPlaced += shape.cells.Count;

            int lines = m_Grid.ResolveLines();
            m_LinesCleared += lines;

            var audio = ServiceLocator.Get<AudioService>();
            if (lines > 0)
            {
                audio?.PlaySfx(AudioService.SfxClear, 1f + 0.08f * lines, 1f);
                Haptics.Play(HapticStrength.Medium);
                m_TimeRemaining += m_Config.puzzle.bonusTimePerLine * lines;
            }
            else
            {
                audio?.PlaySfx(AudioService.SfxPlace, UnityEngine.Random.Range(0.95f, 1.06f), 0.7f);
                Haptics.Play(HapticStrength.Light);
            }

            PiecePlaced?.Invoke(trayIndex, lines);

            if (m_LinesCleared >= m_Config.puzzle.linesToWin) Finish(true);
            else if (TrayEmpty() || !m_Grid.AnyPlacementExists(m_Tray)) Finish(m_LinesCleared > 0);

            return true;
        }

        bool TrayEmpty()
        {
            for (int i = 0; i < m_Tray.Count; i++) if (m_Tray[i] != null) return false;
            return true;
        }
    }
}
