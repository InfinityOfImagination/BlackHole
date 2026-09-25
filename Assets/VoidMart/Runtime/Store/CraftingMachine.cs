using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// A factory processor: pulls raw city junk out of the shared hopper balance, turns it into a
    /// consumer product on a timer and ships the finished unit to a shelf.  Machines jam on the
    /// spec's schedule, which is what summons the block-sorting puzzle.
    /// </summary>
    public class CraftingMachine : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] string m_MachineId = "crusher_machine_01";
        [SerializeField] string m_ProductId = "product_chair";
        [SerializeField] float m_RateMultiplier = 1f;
        [SerializeField] Transform m_OutputPoint;
        [SerializeField] Transform m_JamIcon;
        [SerializeField] Transform m_ShakeRoot;
        [SerializeField] StackVisual m_OutputStack;
        [SerializeField] float m_InteractRadius = 2.6f;

        StoreManager m_Store;
        AudioService m_Audio;
        ProductDefinition m_Product;
        MachineSave m_State;
        PlayerHoleController m_Hole;

        float m_Progress;
        float m_JamTimer;
        float m_SinceJam;
        float m_ShakePhase;
        bool m_PuzzleRequested;

        public string MachineId => m_MachineId;
        public string ProductId => m_ProductId;
        public bool IsJammed => m_State != null && m_State.isJammed;
        public float Progress => m_Product == null ? 0f : Mathf.Clamp01(m_Progress / Mathf.Max(0.05f, m_Product.craftSeconds));
        public double InputBuffer => m_State?.inputBuffer ?? 0d;
        public double OutputBuffer => m_State?.outputBuffer ?? 0d;
        public Transform OutputPoint => m_OutputPoint != null ? m_OutputPoint : transform;

        public void Configure(GameConfig config, string machineId, string productId, float rateMultiplier)
        {
            m_Config = config;
            m_MachineId = machineId;
            m_ProductId = productId;
            m_RateMultiplier = rateMultiplier;
        }

        public void BindVisuals(Transform outputPoint, Transform jamIcon, Transform shakeRoot, StackVisual outputStack)
        {
            m_OutputPoint = outputPoint;
            m_JamIcon = jamIcon;
            m_ShakeRoot = shakeRoot;
            m_OutputStack = outputStack;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Audio = ServiceLocator.Get<AudioService>();
            m_Hole = ServiceLocator.Get<PlayerHoleController>();
            m_Product = m_Config != null ? m_Config.FindProduct(m_ProductId) : null;
            m_State = m_Store != null ? m_Store.GetMachineState(m_MachineId) : new MachineSave();
            m_Store?.RegisterMachine(this);

            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null) bus.puzzleCompleted?.Register(OnPuzzleCompleted);

            ResetJamTimer();
            RefreshJamIcon();
        }

        void OnDestroy()
        {
            m_Store?.UnregisterMachine(this);
            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null) bus.puzzleCompleted?.Unregister(OnPuzzleCompleted);
        }

        void Update()
        {
            if (m_Config == null || m_Product == null || m_State == null) return;
            float deltaTime = Time.deltaTime;

            if (m_State.isJammed)
            {
                AnimateJam(deltaTime);
                CheckPlayerProximity();
                return;
            }

            TickJamRoll(deltaTime);
            PullInput();
            Craft(deltaTime);
            PushOutput();
        }

        void PullInput()
        {
            if (m_Store == null) return;
            float capacity = m_Config.store.machineInputPerProduct * 4f;
            if (m_State.inputBuffer >= capacity) return;
            double wanted = capacity - m_State.inputBuffer;
            double taken = m_Store.ConsumeUpTo(wanted);
            if (taken > 0d)
            {
                m_State.inputBuffer += taken;
                m_Store.PersistMachineState();
            }
        }

        void Craft(float deltaTime)
        {
            if (m_State.outputBuffer >= m_Config.store.machineOutputCapacity) return;
            if (m_State.inputBuffer < m_Product.inputMass) return;

            float speed = m_Config.store.machineBaseRate * m_RateMultiplier * (m_Store != null ? m_Store.MachineSpeedMultiplier : 1f);
            float tierBoost = 1f + 0.35f * Mathf.Max(0, m_State.tier - 1);
            m_Progress += deltaTime * speed * tierBoost;

            if (m_Progress < m_Product.craftSeconds) return;
            m_Progress = 0f;
            m_State.inputBuffer -= m_Product.inputMass;
            m_State.outputBuffer += 1d;
            m_Store?.PersistMachineState();

            if (m_OutputStack != null) m_OutputStack.SetCount(Mathf.RoundToInt((float)m_State.outputBuffer));
        }

        void PushOutput()
        {
            if (m_State.outputBuffer < 1d || m_Store == null) return;
            var shelf = m_Store.FindShelfWithSpace(m_ProductId);
            if (shelf == null) return;

            m_State.outputBuffer -= 1d;
            if (m_OutputStack != null) m_OutputStack.SetCount(Mathf.RoundToInt((float)m_State.outputBuffer));
            shelf.ReserveIncoming();

            var pools = ServiceLocator.Get<PoolService>();
            var go = pools?.Spawn("fx_product_box", OutputPoint.position, Quaternion.identity);
            var flight = go != null ? go.GetComponent<FlyingItem>() : null;

            float travel = Vector3.Distance(OutputPoint.position, shelf.DropPoint.position) / Mathf.Max(0.5f, m_Config.store.conveyorSpeed);
            if (flight != null)
            {
                var target = shelf;
                flight.Launch(OutputPoint.position, shelf.DropPoint.position, 1.4f, travel, () => target.DeliverIncoming(m_ProductId), 0.6f);
            }
            else
            {
                shelf.DeliverIncoming(m_ProductId);
            }
        }

        // -------------------------------------------------------------- jamming

        void ResetJamTimer()
        {
            float perMinute = m_Config != null ? m_Config.store.jamChancePerMinute : 0.5f;
            float resistance = 1f + (m_Store != null ? m_Store.JamResistance : 0f);
            float meanSeconds = perMinute <= 0.001f ? 9999f : 60f / perMinute * resistance;
            m_JamTimer = meanSeconds * Random.Range(0.6f, 1.5f);
            m_SinceJam = 0f;
        }

        void TickJamRoll(float deltaTime)
        {
            m_SinceJam += deltaTime;
            if (m_Config.store.jamChancePerMinute <= 0.001f) return;
            if (m_SinceJam < m_Config.store.minSecondsBetweenJams) return;

            m_JamTimer -= deltaTime;
            if (m_JamTimer > 0f) return;

            // Only jam a machine that is actually working; jamming an idle machine feels unfair.
            if (m_State.inputBuffer < m_Product.inputMass) { m_JamTimer = 4f; return; }
            Jam();
        }

        public void Jam()
        {
            if (m_State == null || m_State.isJammed) return;
            m_State.isJammed = true;
            m_PuzzleRequested = false;
            m_Store?.PersistMachineState();
            RefreshJamIcon();
            m_Audio?.PlaySfx(AudioService.SfxJam, 1f, 0.9f);
            m_Config?.events?.machineJammed?.Raise(m_MachineId);
            m_Config?.events?.toast?.Raise(new ToastRequest(DisplayName + " jammed!", ToastStyle.Warning));
        }

        public void Repair()
        {
            if (m_State == null || !m_State.isJammed) return;
            m_State.isJammed = false;
            m_Store?.PersistMachineState();
            RefreshJamIcon();
            ResetJamTimer();
            m_Audio?.PlaySfx(AudioService.SfxRepair, 1f, 0.9f);
            m_Config?.events?.machineRepaired?.Raise(m_MachineId);
            if (m_ShakeRoot != null) m_ShakeRoot.localPosition = Vector3.zero;
        }

        string DisplayName => string.IsNullOrEmpty(m_ProductId) ? "Machine" : m_ProductId.Replace("product_", "").Replace('_', ' ');

        void CheckPlayerProximity()
        {
            if (m_PuzzleRequested || m_Hole == null)
            {
                if (m_Hole == null) m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }
            if (m_PuzzleRequested) return;

            Vector3 offset = m_Hole.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > m_InteractRadius * m_InteractRadius) return;

            m_PuzzleRequested = true;
            m_Config?.events?.puzzleRequested?.Raise(m_MachineId);
        }

        void OnPuzzleCompleted(PuzzleResult result)
        {
            if (result.machineId != m_MachineId) return;
            m_PuzzleRequested = false;
            if (result.solved) Repair();
        }

        void AnimateJam(float deltaTime)
        {
            m_ShakePhase += deltaTime * 26f;
            if (m_ShakeRoot != null)
            {
                float amount = 0.035f;
                m_ShakeRoot.localPosition = new Vector3(Mathf.Sin(m_ShakePhase) * amount, 0f, Mathf.Cos(m_ShakePhase * 1.3f) * amount);
            }
            if (m_JamIcon != null)
            {
                m_JamIcon.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f) * 0.12f);
                m_JamIcon.rotation = Quaternion.Euler(0f, Time.time * 40f, 0f);
            }
        }

        void RefreshJamIcon()
        {
            if (m_JamIcon != null) m_JamIcon.gameObject.SetActive(IsJammed);
            if (!IsJammed && m_ShakeRoot != null) m_ShakeRoot.localPosition = Vector3.zero;
        }

        public void UpgradeTier()
        {
            if (m_State == null) return;
            m_State.tier++;
            m_Store?.PersistMachineState();
        }
    }
}
