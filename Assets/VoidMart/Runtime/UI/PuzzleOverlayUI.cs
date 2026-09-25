using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Puzzle;

namespace VoidMart.UI
{
    /// <summary>
    /// The block-sorting overlay.  Pops in on EaseOutElastic when a machine jams, builds its board
    /// from the live config (so grid size and tray count are tunable), and drives the drag-to-place
    /// interaction against <see cref="PuzzleController"/>.
    /// </summary>
    public class PuzzleOverlayUI : ModalPanel
    {
        [SerializeField] RectTransform m_Board;
        [SerializeField] Image m_CellTemplate;
        [SerializeField] RectTransform m_TrayRoot;
        [SerializeField] RectTransform m_TraySlotTemplate;
        [SerializeField] Image m_BlockTemplate;
        [SerializeField] Image m_TimerFill;
        [SerializeField] VMText m_Header;
        [SerializeField] VMText m_TimerLabel;
        [SerializeField] Button m_SkipButton;
        [SerializeField] float m_CellSize = 72f;

        readonly List<PuzzleCell> m_Cells = new List<PuzzleCell>(64);
        readonly List<PuzzleTrayPiece> m_Tray = new List<PuzzleTrayPiece>(4);
        readonly HashSet<int> m_Highlighted = new HashSet<int>();

        PuzzleController m_Puzzle;
        int m_BuiltWidth, m_BuiltHeight;

        public void BindBoard(RectTransform board, Image cellTemplate, RectTransform trayRoot, RectTransform traySlotTemplate,
            Image blockTemplate, Image timerFill, VMText header, VMText timerLabel, Button skipButton, float cellSize)
        {
            m_Board = board;
            m_CellTemplate = cellTemplate;
            m_TrayRoot = trayRoot;
            m_TraySlotTemplate = traySlotTemplate;
            m_BlockTemplate = blockTemplate;
            m_TimerFill = timerFill;
            m_Header = header;
            m_TimerLabel = timerLabel;
            m_SkipButton = skipButton;
            m_CellSize = cellSize;
        }

        protected override void Awake()
        {
            m_Transition = ModalTransition.ScalePop;
            base.Awake();
            if (m_SkipButton != null)
            {
                m_SkipButton.onClick.RemoveAllListeners();
                m_SkipButton.onClick.AddListener(OnSkip);
            }
        }

        void Start() => TryBind();

        void TryBind()
        {
            if (m_Puzzle != null) return;
            m_Puzzle = ServiceLocator.Get<PuzzleController>();
            if (m_Puzzle == null) return;
            m_Puzzle.Generated += OnGenerated;
            m_Puzzle.Finished += OnFinished;
            m_Puzzle.PiecePlaced += OnPiecePlaced;
        }

        void OnDestroy()
        {
            if (m_Puzzle == null) return;
            m_Puzzle.Generated -= OnGenerated;
            m_Puzzle.Finished -= OnFinished;
            m_Puzzle.PiecePlaced -= OnPiecePlaced;
        }

        void OnGenerated()
        {
            BuildBoard();
            RefreshBoard();
            RefreshTray();
            if (m_Header != null) m_Header.SetText("UNJAM IT!");
            Open();
        }

        void OnFinished(PuzzleResult result)
        {
            if (m_Header != null) m_Header.SetText(result.solved ? "FIXED! +" + NumberFormat.Money(result.reward) : "TOO SLOW");
            Invoke(nameof(Close), 0.55f);
        }

        void OnPiecePlaced(int trayIndex, int lines)
        {
            RefreshBoard();
            RefreshTray();
        }

        // ------------------------------------------------------------ building

        void BuildBoard()
        {
            if (m_Puzzle == null || m_Board == null || m_CellTemplate == null) return;
            int width = m_Puzzle.Grid.Width;
            int height = m_Puzzle.Grid.Height;
            if (width == m_BuiltWidth && height == m_BuiltHeight && m_Cells.Count == width * height) return;

            for (int i = 0; i < m_Cells.Count; i++) if (m_Cells[i] != null) Destroy(m_Cells[i].gameObject);
            m_Cells.Clear();

            float boardSize = Mathf.Min(m_Board.rect.width, m_Board.rect.height);
            if (boardSize > 1f) m_CellSize = boardSize / Mathf.Max(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var image = Instantiate(m_CellTemplate, m_Board);
                    image.gameObject.SetActive(true);
                    image.name = $"Cell {x},{y}";
                    var rect = image.rectTransform;
                    rect.sizeDelta = new Vector2(m_CellSize * 0.94f, m_CellSize * 0.94f);
                    rect.anchoredPosition = CellPosition(x, y, width, height);

                    var cell = image.gameObject.AddComponent<PuzzleCell>();
                    cell.Setup(image, x, y);
                    m_Cells.Add(cell);
                }
            }

            m_BuiltWidth = width;
            m_BuiltHeight = height;
            BuildTray();
        }

        Vector2 CellPosition(int x, int y, int width, int height) => new Vector2(
            (x - (width - 1) * 0.5f) * m_CellSize,
            (y - (height - 1) * 0.5f) * m_CellSize);

        void BuildTray()
        {
            if (m_TrayRoot == null || m_TraySlotTemplate == null || m_Puzzle == null) return;

            int wanted = m_Puzzle.Tray.Count;
            while (m_Tray.Count < wanted)
            {
                var slot = Instantiate(m_TraySlotTemplate, m_TrayRoot);
                slot.gameObject.SetActive(true);
                var piece = slot.gameObject.GetComponent<PuzzleTrayPiece>();
                if (piece == null) piece = slot.gameObject.AddComponent<PuzzleTrayPiece>();
                piece.Setup(this, m_Tray.Count, slot, m_BlockTemplate, m_CellSize * 0.8f);
                m_Tray.Add(piece);
            }
        }

        // ----------------------------------------------------------- refreshing

        void RefreshBoard()
        {
            if (m_Puzzle == null || m_Config == null) return;
            var palette = m_Config.theme.AccentRamp;
            var empty = m_Config.theme.voidIndigo;
            empty.a = 0.55f;

            for (int i = 0; i < m_Cells.Count; i++)
            {
                var cell = m_Cells[i];
                int value = m_Puzzle.Grid.Get(cell.X, cell.Y);
                cell.Paint(value <= 0 ? empty : palette[(value - 1) % palette.Length]);
            }
        }

        void RefreshTray()
        {
            if (m_Puzzle == null || m_Config == null) return;
            var palette = m_Config.theme.AccentRamp;
            for (int i = 0; i < m_Tray.Count; i++)
            {
                var shape = i < m_Puzzle.Tray.Count ? m_Puzzle.Tray[i] : null;
                m_Tray[i].SetShape(shape, palette[i % palette.Length]);
            }
        }

        void Update()
        {
            if (m_Puzzle == null) TryBind();
            if (!IsOpen || m_Puzzle == null) return;
            if (m_TimerFill != null) m_TimerFill.fillAmount = m_Puzzle.TimeNormalized;
            if (m_TimerLabel != null) m_TimerLabel.SetText(Mathf.Max(0f, m_Puzzle.TimeRemaining).ToString("0.0") + "s");
            if (m_TimerFill != null && m_Config != null)
                m_TimerFill.color = m_Puzzle.TimeNormalized < 0.35f ? m_Config.theme.coral : m_Config.theme.neonCyan;
        }

        // ---------------------------------------------------------------- drag

        public void BeginDrag(PuzzleTrayPiece piece) => ClearHighlight();

        public void UpdateDrag(PuzzleTrayPiece piece, PointerEventData eventData)
        {
            ClearHighlight();
            if (!TryResolveOrigin(piece, eventData, out var origin)) return;

            bool valid = m_Puzzle.CanPlace(piece.Index, origin);
            var shape = piece.Shape;
            var colour = valid ? m_Config.theme.mint : m_Config.theme.coral;

            for (int i = 0; i < shape.cells.Count; i++)
            {
                var cell = shape.cells[i] + origin;
                int index = IndexOf(cell.x, cell.y);
                if (index < 0) continue;
                m_Cells[index].Paint(colour);
                m_Highlighted.Add(index);
            }
        }

        public void EndDrag(PuzzleTrayPiece piece, PointerEventData eventData)
        {
            ClearHighlight();
            if (TryResolveOrigin(piece, eventData, out var origin) && m_Puzzle.TryPlace(piece.Index, origin))
            {
                piece.ResetPosition();
                return;
            }
            piece.ResetPosition();
            RefreshBoard();
        }

        bool TryResolveOrigin(PuzzleTrayPiece piece, PointerEventData eventData, out Vector2Int origin)
        {
            origin = default;
            if (m_Puzzle == null || piece.Shape == null || m_Board == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Board, eventData.position, eventData.pressEventCamera, out var local))
                return false;

            local += new Vector2(0f, 120f); // match the lift applied while dragging
            int width = m_Puzzle.Grid.Width;
            int height = m_Puzzle.Grid.Height;
            float fx = local.x / m_CellSize + (width - 1) * 0.5f;
            float fy = local.y / m_CellSize + (height - 1) * 0.5f;

            var shape = piece.Shape;
            origin = new Vector2Int(
                Mathf.RoundToInt(fx - (shape.Width - 1) * 0.5f),
                Mathf.RoundToInt(fy - (shape.Height - 1) * 0.5f));
            return true;
        }

        int IndexOf(int x, int y)
        {
            if (m_Puzzle == null) return -1;
            if (x < 0 || y < 0 || x >= m_Puzzle.Grid.Width || y >= m_Puzzle.Grid.Height) return -1;
            int index = y * m_Puzzle.Grid.Width + x;
            return index < m_Cells.Count ? index : -1;
        }

        void ClearHighlight()
        {
            if (m_Highlighted.Count == 0) return;
            m_Highlighted.Clear();
            RefreshBoard();
        }

        void OnSkip()
        {
            m_Puzzle?.SkipWithAd();
        }

        public override void Close()
        {
            base.Close();
            if (m_Puzzle != null && m_Puzzle.Running) m_Puzzle.Abort();
        }
    }
}
