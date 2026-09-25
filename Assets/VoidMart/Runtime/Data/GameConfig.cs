using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Tweaker;

namespace VoidMart.Data
{
    /// <summary>
    /// The single source of truth for every tunable number, colour and asset key in Void Mart.
    /// The one-click builder reads it to generate art, audio, prefabs and scenes; the Master
    /// Control window edits it; the runtime reads it every frame.  Change a value here and the
    /// whole game changes with it.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "VoidMart/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Identity")]
        public string gameTitle = "VOID MART";
        public string tagline = "EAT & EARN";
        public string buildVersion = "1.0.0";

        [Header("Sections")]
        public ThemeConfig theme = new ThemeConfig();
        public HoleConfig hole = new HoleConfig();
        public CameraConfig cameraRig = new CameraConfig();
        public CityConfig city = new CityConfig();
        public StoreConfig store = new StoreConfig();
        public EconomyConfig economy = new EconomyConfig();
        public PuzzleConfig puzzle = new PuzzleConfig();
        public AudioConfig audio = new AudioConfig();
        public UIConfig ui = new UIConfig();
        public MonetizationConfig monetization = new MonetizationConfig();
        public PersistenceConfig persistence = new PersistenceConfig();
        public ArtConfig art = new ArtConfig();

        [Header("Content tables")]
        public List<PropDefinition> props = new List<PropDefinition>();
        public List<ProductDefinition> products = new List<ProductDefinition>();
        public List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();
        public List<string> startingUnlockedNodeIds = new List<string> { "hopper_01", "crusher_machine_01", "display_shelf_01", "checkout_counter_01" };

        [Header("Generated links (filled by the builder)")]
        public GameEventBus events;
        public GameAssets assets;
        public List<FurnishingNodeData> furnishingNodes = new List<FurnishingNodeData>();

        public PropDefinition FindProp(string id)
        {
            for (int i = 0; i < props.Count; i++) if (props[i].id == id) return props[i];
            return null;
        }

        public ProductDefinition FindProduct(string id)
        {
            for (int i = 0; i < products.Count; i++) if (products[i].id == id) return products[i];
            return null;
        }

        public UpgradeDefinition FindUpgrade(string id)
        {
            for (int i = 0; i < upgrades.Count; i++) if (upgrades[i].id == id) return upgrades[i];
            return null;
        }

        public FurnishingNodeData FindNode(string id)
        {
            for (int i = 0; i < furnishingNodes.Count; i++)
                if (furnishingNodes[i] != null && furnishingNodes[i].NodeID == id) return furnishingNodes[i];
            return null;
        }
    }

    // ===================================================================== theme

    [Serializable]
    public class ThemeConfig
    {
        [Tooltip("Art direction preset name. Drives every generated texture, material and mesh tint.")]
        public string presetName = "Neon Dusk Retail";

        [Header("Brand")]
        public Color voidInk = new Color(0.043f, 0.039f, 0.078f);      // #0B0A14
        public Color voidIndigo = new Color(0.176f, 0.106f, 0.306f);   // #2D1B4E
        public Color electricViolet = new Color(0.263f, 0.220f, 0.792f); // #4338CA
        public Color neonCyan = new Color(0f, 0.898f, 1f);             // #00E5FF
        public Color hotMagenta = new Color(1f, 0.239f, 0.651f);       // #FF3DA6
        public Color sunsetAmber = new Color(1f, 0.690f, 0.125f);      // #FFB020
        public Color mint = new Color(0.239f, 0.863f, 0.592f);         // #3DDC97
        public Color coral = new Color(1f, 0.420f, 0.420f);            // #FF6B6B
        public Color paper = new Color(0.973f, 0.980f, 0.988f);        // #F8FAFC
        public Color cream = new Color(1f, 0.957f, 0.878f);            // #FFF4E0

        [Header("Street (desaturated so collectibles pop)")]
        public Color asphalt = new Color(0.165f, 0.184f, 0.271f);
        public Color sidewalk = new Color(0.455f, 0.482f, 0.576f);
        public Color roadLine = new Color(0.898f, 0.906f, 0.937f);
        public Color concreteA = new Color(0.533f, 0.573f, 0.659f);
        public Color concreteB = new Color(0.416f, 0.451f, 0.545f);
        public Color concreteC = new Color(0.616f, 0.639f, 0.706f);
        public Color foliage = new Color(0.247f, 0.463f, 0.416f);

        [Header("Store (bright commercial pastels)")]
        public Color storeFloorA = new Color(1f, 0.957f, 0.878f);
        public Color storeFloorB = new Color(0.965f, 0.902f, 0.812f);
        public Color storeWall = new Color(0.988f, 0.937f, 0.898f);
        public Color machineBody = new Color(0.678f, 0.847f, 0.902f);
        public Color machineAccent = new Color(1f, 0.690f, 0.125f);
        public Color shelfWood = new Color(0.878f, 0.737f, 0.549f);

        [Header("Sky")]
        public Color skyTop = new Color(0.106f, 0.082f, 0.200f);
        public Color skyHorizon = new Color(1f, 0.549f, 0.420f);
        public Color skyGround = new Color(0.125f, 0.114f, 0.180f);
        [Range(0f, 1f)] public float skyHorizonHeight = 0.42f;
        [Range(0.2f, 8f)] public float skyHorizonSharpness = 2.2f;

        [Header("Lighting")]
        public Color sunColor = new Color(1f, 0.878f, 0.769f);
        [Range(0f, 4f)] public float sunIntensity = 1.35f;
        public Vector3 sunEuler = new Vector3(48f, 34f, 0f);
        public Color ambientSky = new Color(0.353f, 0.361f, 0.478f);
        public Color ambientEquator = new Color(0.259f, 0.251f, 0.341f);
        public Color ambientGround = new Color(0.141f, 0.129f, 0.192f);
        public Color fogColor = new Color(0.180f, 0.176f, 0.290f);
        public bool fogEnabled = true;
        [Range(0f, 0.08f)] public float fogDensity = 0.012f;

        [Header("Clay shading")]
        [Range(0f, 1f)] public float clayWrap = 0.55f;
        [Range(0f, 1f)] public float clayShadeStrength = 0.45f;
        [Range(0f, 1f)] public float rimStrength = 0.35f;
        [Range(0.5f, 8f)] public float rimPower = 3.2f;
        [Range(0f, 1f)] public float specular = 0.18f;

        public Color[] AccentRamp => new[] { neonCyan, hotMagenta, sunsetAmber, mint, electricViolet, coral };
    }

    // ====================================================================== hole

    [Serializable]
    public class HoleConfig
    {
        [Tweakable("Hole_Physics", 1f, 25f)]
        [Tooltip("Top speed in metres/second at joystick full tilt.")]
        public float moveSpeed = 8.5f;

        [Tweakable("Hole_Physics", 1f, 60f)]
        public float acceleration = 34f;

        [Tweakable("Hole_Physics", 1f, 40f)]
        public float deceleration = 26f;

        [Tweakable("Hole_Physics", 90f, 1440f)]
        public float turnSpeed = 720f;

        [Tweakable("Hole_Physics", 0.5f, 10f)]
        public float initialRadius = 1.2f;

        [Tweakable("Hole_Physics", 10f, 500f)]
        public float maxCapacity = 50f;

        [Tweakable("Hole_Physics", 0.5f, 6f)]
        [Tooltip("Multiplier applied to the radius to get the suction trigger field.")]
        public float suctionRadiusMultiplier = 2.1f;

        [Tweakable("Hole_Physics", 0f, 60f)]
        public float suctionForce = 16f;

        [Tweakable("Hole_Physics", 0f, 2f)]
        [Tooltip("Fraction of the hole radius an object's footprint may exceed and still be eaten.")]
        public float swallowTolerance = 0.92f;

        [Tweakable("Hole_Feel", 0.05f, 1.5f)]
        [Tooltip("Seconds an object takes to disappear down the hole.")]
        public float swallowDuration = 0.36f;

        [Tweakable("Hole_Feel", 1f, 6f)]
        [Tooltip("Scale(t) = lerp(1, 0, t^power) from the consumption physics spec.")]
        public float swallowScalePower = 2.5f;

        [Tweakable("Hole_Feel", 0f, 3f)]
        public float swallowSpin = 1.4f;

        [Tweakable("Hole_Growth", 0f, 0.2f)]
        [Tooltip("Radius gained per unit of swallowed mass (before the growth curve).")]
        public float radiusPerMass = 0.018f;

        [Tweakable("Hole_Growth", 0.1f, 1f)]
        [Tooltip("Growth falls off with size: gain *= pow(base/current, falloff).")]
        public float growthFalloff = 0.55f;

        [Tweakable("Hole_Growth", 1f, 40f)]
        public float maxRadius = 9f;

        [Tweakable("Hole_Feel", 0f, 1f)]
        public float growthPunch = 0.22f;

        [Tooltip("Radius thresholds that unlock each prop tier. Index 0 = tier 1.")]
        public float[] tierRadius = { 0f, 1.6f, 2.6f, 4.0f, 6.0f };

        [Tweakable("Hole_Fever", 1f, 6f)]
        public float feverSizeMultiplier = 3f;

        [Tweakable("Hole_Fever", 5f, 120f)]
        public float feverDuration = 30f;

        [Tweakable("Hole_Fever", 1f, 40f)]
        public float feverAutoSuctionRadius = 14f;

        [Tweakable("Hole_Feel", 0f, 2f)]
        public float unloadSecondsPerUnit = 0.02f;
    }

    [Serializable]
    public class CameraConfig
    {
        [Tweakable("Camera", 20f, 70f)] public float fieldOfView = 35f;
        [Tweakable("Camera", 30f, 80f)] public float pitch = 55f;
        [Tweakable("Camera", -180f, 180f)] public float yaw = 0f;
        [Tweakable("Camera", 5f, 60f)] public float baseDistance = 17f;
        [Tweakable("Camera", 0f, 12f)] public float distancePerRadius = 3.1f;
        [Tweakable("Camera", 0.5f, 20f)] public float followSharpness = 6f;
        [Tweakable("Camera", 0f, 8f)] public float zoomSharpness = 2.2f;
        [Tweakable("Camera", 0f, 4f)] public float lookAhead = 1.1f;
        [Tweakable("Camera", 0f, 3f)] public float shakeScale = 1f;
        public Vector3 targetOffset = new Vector3(0f, 0.4f, 0f);
    }

    // ====================================================================== city

    [Serializable]
    public class CityConfig
    {
        [Tweakable("City", 1f, 12f)] public int blocksX = 4;
        [Tweakable("City", 1f, 12f)] public int blocksZ = 4;
        [Tweakable("City", 10f, 60f)] public float blockSize = 26f;
        [Tweakable("City", 4f, 24f)] public float roadWidth = 9f;
        [Tweakable("City", 0f, 3f)] public float sidewalkHeight = 0.22f;
        public int randomSeed = 20260925;

        [Tweakable("City", 0f, 400f)] public int propBudget = 190;
        [Tweakable("City", 0f, 60f)] public int vehicleBudget = 26;
        [Tweakable("City", 0f, 80f)] public int buildingBudget = 34;
        [Tweakable("City", 0f, 40f)] public int pedestrianBudget = 18;

        [Tweakable("City", 0f, 60f)] public float respawnSeconds = 14f;
        [Tweakable("City", 0f, 1f)] public float respawnFraction = 0.35f;
        [Tooltip("Keeps the player inside the district.")]
        public float boundaryPadding = 6f;
    }

    // ===================================================================== store

    [Serializable]
    public class StoreConfig
    {
        [Tweakable("Store", 5f, 400f)] public float hopperCapacity = 120f;
        [Tweakable("Store", 1f, 200f)] public float unloadRate = 46f;
        [Tweakable("Store", 0.1f, 20f)] public float machineBaseRate = 2.4f;
        [Tweakable("Store", 0.5f, 20f)] public float machineInputPerProduct = 3.2f;
        [Tweakable("Store", 1f, 60f)] public float machineOutputCapacity = 14f;
        [Tweakable("Store", 0f, 1f)] public float jamChancePerMinute = 0.55f;
        [Tweakable("Store", 5f, 300f)] public float minSecondsBetweenJams = 26f;
        [Tweakable("Store", 0f, 1f)] public float jamThroughputPenalty = 1f;

        [Tweakable("Store", 0.5f, 12f)] public float conveyorSpeed = 2.4f;
        [Tweakable("Store", 1f, 80f)] public float shelfCapacity = 18f;
        [Tweakable("Store", 0.1f, 20f)] public float restockRate = 3.4f;

        [Tweakable("Store", 0.2f, 20f)] public float customerSpawnInterval = 2.6f;
        [Tweakable("Store", 1f, 40f)] public int maxCustomers = 12;
        [Tweakable("Store", 0.5f, 8f)] public float customerMoveSpeed = 2.3f;
        [Tweakable("Store", 1f, 60f)] public float customerPatience = 26f;
        [Tweakable("Store", 0.1f, 6f)] public float checkoutSeconds = 0.9f;
        [Tweakable("Store", 1f, 20f)] public int itemsPerCustomer = 3;

        [Tweakable("Store", 0.5f, 12f)] public float robotSpeed = 3.2f;
        [Tweakable("Store", 1f, 40f)] public int robotCarryCapacity = 8;
        [Tweakable("Store", 0f, 16f)] public int maxRobots = 4;

        [Tweakable("Store", 0.2f, 10f)] public float cashPickupRadius = 2.2f;
        [Tweakable("Store", 0.2f, 4f)] public float billFlightSeconds = 0.55f;
        [Tweakable("Store", 0f, 8f)] public float billArcHeight = 2.4f;

        [Tweakable("Store", 0.05f, 4f)] public float purchaseTickInterval = 0.09f;
        [Tweakable("Store", 0.5f, 6f)]
        [Tooltip("Cash drains from the wallet on an exponential curve while standing in a zone.")]
        public float purchaseDrainExponent = 2.1f;
        [Tweakable("Store", 0.2f, 20f)] public float purchaseMinSeconds = 1.4f;
    }

    // =================================================================== economy

    [Serializable]
    public class EconomyConfig
    {
        [Tweakable("Economy", 0f, 100000f)] public double startingCash = 0d;
        [Tweakable("Economy", 0f, 10000f)] public int startingGems = 25;

        [Tweakable("Economy", 0.1f, 20f)] public double valuePerMass = 1.15d;
        [Tweakable("Economy", 1f, 8f)] public double tierValueMultiplier = 2.35d;
        [Tweakable("Economy", 0.5f, 10f)] public double craftValueMultiplier = 2.8d;
        [Tweakable("Economy", 0f, 10f)] public double sellPriceMultiplier = 1d;

        [Tweakable("Economy", 1f, 3f)] public double nodeCostGrowth = 1.55d;
        [Tweakable("Economy", 1f, 3f)] public double upgradeCostGrowth = 1.42d;

        [Tweakable("Economy", 0f, 500f)] public float xpPerProp = 4f;
        [Tweakable("Economy", 0f, 500f)] public float xpPerSale = 7f;
        [Tweakable("Economy", 10f, 4000f)] public float xpBase = 120f;
        [Tweakable("Economy", 1f, 3f)] public float xpGrowth = 1.35f;

        [Tweakable("Economy", 0f, 1f)] public double offlineEarningRate = 0.45d;
        [Tweakable("Economy", 0f, 86400f)] public double offlineCapSeconds = 7200d;
        [Tweakable("Economy", 0f, 600f)] public double offlineMinimumSeconds = 60d;

        public double NodeCost(double baseCost, int tier) => baseCost * Math.Pow(nodeCostGrowth, Math.Max(0, tier));
        public double UpgradeCost(double baseCost, int level) => baseCost * Math.Pow(upgradeCostGrowth, Math.Max(0, level));
        public float XpForLevel(int level) => xpBase * Mathf.Pow(xpGrowth, Mathf.Max(0, level - 1));
    }

    // ==================================================================== puzzle

    [Serializable]
    public class PuzzleConfig
    {
        [Tweakable("Puzzle", 4f, 12f)] public int gridWidth = 8;
        [Tweakable("Puzzle", 4f, 12f)] public int gridHeight = 8;
        [Tweakable("Puzzle", 1f, 30f)] public float timeLimit = 5f;
        [Tweakable("Puzzle", 1f, 6f)] public int trayPieces = 3;
        [Tweakable("Puzzle", 0f, 1f)] public float prefillDensity = 0.42f;
        [Tweakable("Puzzle", 1f, 8f)] public int linesToWin = 1;
        [Tweakable("Puzzle", 0f, 10000f)] public double rewardBase = 140d;
        [Tweakable("Puzzle", 0f, 10000f)] public double rewardPerLine = 260d;
        [Tweakable("Puzzle", 1f, 5f)] public double comboMultiplier = 1.6d;
        [Tweakable("Puzzle", 0f, 10f)] public float bonusTimePerLine = 0.9f;
        [Tweakable("Puzzle", 0f, 4f)] public float introSeconds = 0.65f;
        [Tweakable("Puzzle", 0f, 1f)] public float slowMotionScale = 0.15f;
        public bool pauseWorldDuringPuzzle = true;
    }

    // ===================================================================== audio

    [Serializable]
    public class AudioConfig
    {
        [Tweakable("Audio", 0f, 1f)] public float masterVolume = 1f;
        [Tweakable("Audio", 0f, 1f)] public float musicVolume = 0.62f;
        [Tweakable("Audio", 0f, 1f)] public float sfxVolume = 0.9f;
        [Tweakable("Audio", 0.05f, 6f)] public float musicCrossfadeSeconds = 1.6f;

        [Header("Swallow pitch ladder (spec 3)")]
        [Tweakable("Audio", 0f, 1f)] public float pitchStepSemitones = 0.03f;
        [Tweakable("Audio", 0f, 24f)] public float pitchCapSemitones = 12f;
        [Tweakable("Audio", 0.1f, 10f)] public float pitchResetSeconds = 1.5f;
        [Tweakable("Audio", 0f, 0.5f)] public float suctionPitchVariance = 0.05f;
        [Tweakable("Audio", 1f, 16f)] public int cashMaxVoices = 6;

        [Header("Synthesis (regenerates the WAV assets)")]
        [Tweakable("Audio", 60f, 200f)] public float musicBpm = 104f;
        [Range(0, 11)] public int musicRootNote = 9;          // A
        public bool musicMinorKey = true;
        [Tweakable("Audio", 4f, 64f)] public int musicBars = 8;
        [Range(0f, 1f)] public float musicSwing = 0.14f;
        [Range(0f, 1f)] public float reverbAmount = 0.28f;
        public int sampleRate = 44100;
    }

    // ======================================================================== ui

    [Serializable]
    public class UIConfig
    {
        [Tweakable("UI", 200f, 2000f)] public float referenceWidth = 1080f;
        [Tweakable("UI", 200f, 2400f)] public float referenceHeight = 1920f;
        [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

        [Header("Joystick")]
        [Tweakable("UI", 40f, 400f)] public float joystickRadius = 150f;
        [Tweakable("UI", 20f, 300f)] public float joystickKnobRadius = 64f;
        [Tweakable("UI", 0f, 1f)] public float joystickDeadZone = 0.08f;
        public bool floatingJoystick = true;
        [Tweakable("UI", 0f, 1f)] public float joystickOpacity = 0.55f;

        [Header("Transitions")]
        [Tweakable("UI", 0.05f, 2f)] public float modalSlideSeconds = 0.42f;
        [Tweakable("UI", 0.05f, 2f)] public float modalPopSeconds = 0.55f;
        [Tweakable("UI", 0.05f, 2f)] public float fadeSeconds = 0.25f;
        [Tweakable("UI", 0.1f, 4f)] public float irisSeconds = 0.75f;
        [Tweakable("UI", 0.5f, 4f)] public float irisMaxRadiusScale = 1.45f;

        [Header("Feel")]
        [Tweakable("UI", 0f, 1f)] public float hudPunchScale = 0.14f;
        [Tweakable("UI", 0f, 4f)] public float floatingTextSeconds = 1.1f;
        [Tweakable("UI", 0f, 6f)] public float floatingTextRise = 2.4f;
        [Tweakable("UI", 0f, 6f)] public float toastSeconds = 2.2f;
        public bool hapticsEnabled = true;
    }

    // ============================================================= monetization

    [Serializable]
    public class MonetizationConfig
    {
        public bool adsEnabled = true;
        [Tweakable("Ads", 0f, 600f)] public float interstitialCooldown = 180f;
        [Tweakable("Ads", 0f, 600f)] public float firstInterstitialDelay = 240f;
        [Tweakable("Ads", 1f, 10f)] public double offlineAdMultiplier = 2d;
        [Tweakable("Ads", 1f, 10f)] public double rewardedCashMultiplier = 3d;
        [Tweakable("Ads", 0f, 600f)] public float feverOfferInterval = 75f;
        [Tweakable("Ads", 0f, 7200f)] public double offlineAdMinSeconds = 600d;
        public string rewardedFeverId = "rv_fever_mode";
        public string rewardedOfflineId = "rv_double_offline";
        public string interstitialLevelClearId = "int_level_clear";
        public string iapNoAdsId = "iap_no_ads";
        public string iapStarterPackId = "iap_starter_pack";
        [Tweakable("Ads", 0f, 100000f)] public int starterPackGems = 5000;
        public string starterPackSkin = "skin_neon_void";
        [Tweakable("Ads", 0f, 3600f)] public float starterPackOfferAfterSeconds = 900f;
    }

    [Serializable]
    public class PersistenceConfig
    {
        [Tweakable("Save", 5f, 600f)] public float autosaveSeconds = 60f;
        public string fileName = "voidmart.sav";
        public bool encryptSaves = true;
        public bool writePlainTextMirrorInEditor = true;
    }

    // ======================================================================= art

    [Serializable]
    public class ArtConfig
    {
        [Header("Texture generation")]
        public int uiAtlasSize = 1024;
        public int iconSize = 192;
        public int groundTextureSize = 512;
        [Range(0f, 1f)] public float textureGrain = 0.06f;
        [Range(0f, 1f)] public float gradientStrength = 0.35f;
        [Range(0f, 32f)] public float panelCornerRadius = 28f;
        [Range(0f, 16f)] public float panelOutline = 4f;
        [Range(0f, 32f)] public float panelShadow = 12f;

        [Header("Display font")]
        [Range(16, 128)] public int fontPixelHeight = 72;
        [Range(0.04f, 0.4f)] public float fontStrokeWeight = 0.17f;
        [Range(0f, 0.5f)] public float fontRounding = 0.22f;
        [Range(0f, 0.4f)] public float fontTracking = 0.06f;

        [Header("Mesh generation")]
        [Range(0f, 0.4f)] public float meshBevel = 0.06f;
        [Range(3, 32)] public int cylinderSides = 12;
        [Range(0f, 0.3f)] public float vertexColourJitter = 0.045f;
        public bool flatShading = true;
    }

    // ============================================================== content rows

    public enum PropCategory { Litter, StreetFurniture, Vehicle, Structure, Pedestrian }

    [Serializable]
    public class PropDefinition
    {
        public string id = "prop";
        public string displayName = "Prop";
        public string meshKey = "prop_cone";
        [Range(1, 5)] public int tier = 1;
        public float mass = 1f;
        public float baseValue = 2f;
        public PropCategory category = PropCategory.Litter;
        public Color tint = Color.white;
        public Color accent = Color.white;
        public Vector2 scaleRange = new Vector2(0.92f, 1.12f);
        [Range(0f, 10f)] public float spawnWeight = 1f;
        [Tooltip("Approximate footprint radius used by the swallow check.")]
        public float footprint = 0.5f;
    }

    [Serializable]
    public class ProductDefinition
    {
        public string id = "product";
        public string displayName = "Product";
        public string meshKey = "product_box";
        public string iconKey = "icon_box";
        public Color tint = Color.white;
        [Tooltip("Raw mass consumed to craft one unit.")]
        public float inputMass = 3f;
        public float craftSeconds = 1.1f;
        public double basePrice = 12d;
        [Range(1, 5)] public int unlockTier = 1;
    }

    public enum UpgradeEffect
    {
        HoleSpeed, HoleCapacity, HoleRadius, SuctionPower, UnloadSpeed,
        MachineSpeed, JamResistance, SellPrice, CustomerRate, RobotCount, OfflineRate
    }

    [Serializable]
    public class UpgradeDefinition
    {
        public string id = "upg";
        public string displayName = "Upgrade";
        [TextArea(1, 3)] public string description = "";
        public string iconKey = "icon_bolt";
        public UpgradeEffect effect = UpgradeEffect.HoleSpeed;
        public double baseCost = 150d;
        [Range(1, 200)] public int maxLevel = 25;
        [Tooltip("Additive amount granted per level (interpreted per effect).")]
        public float perLevel = 0.35f;
        public Color tint = Color.white;

        public double CostAt(EconomyConfig economy, int level) => economy.UpgradeCost(baseCost, level);
        public float ValueAt(int level) => perLevel * level;
    }
}
