using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;

namespace VoidMart.Data
{
    /// <summary>
    /// In-memory save model.  The JSON it produces matches the schema in section 10 of the tech
    /// spec field-for-field; the optional <c>progress</c> and <c>settings</c> blocks are additive
    /// extensions (older saves without them still load, and external tools can ignore them).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int saveVersion = CurrentVersion;
        public long timestamp;
        public ProfileSave profile = new ProfileSave();
        public HoleSave hole = new HoleSave();
        public StoreSave store = new StoreSave();
        public MetaSave meta = new MetaSave();
        public ProgressSave progress = new ProgressSave();
        public SettingsSave settings = new SettingsSave();

        public static SaveData CreateDefault(GameConfig config)
        {
            var data = new SaveData
            {
                timestamp = Now(),
                profile =
                {
                    playerId = "user_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    softCurrency = config != null ? config.economy.startingCash : 0d,
                    hardCurrency = config != null ? config.economy.startingGems : 0,
                    lifetimeEarnings = 0d,
                    noAdsActive = false
                },
                hole =
                {
                    level = 1,
                    currentRadius = config != null ? config.hole.initialRadius : 1.2f,
                    capacityMax = config != null ? config.hole.maxCapacity : 50f,
                    equippedSkin = "skin_default"
                }
            };
            if (config != null)
            {
                foreach (var node in config.startingUnlockedNodeIds)
                    if (!string.IsNullOrEmpty(node)) data.store.unlockedNodeIds.Add(node);
            }
            return data;
        }

        public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public double SecondsSinceSaved()
        {
            double delta = Now() - timestamp;
            return delta < 0d ? 0d : delta;
        }

        /// <summary>Clamps every field into a legal range before the payload is trusted.</summary>
        public SaveData Validate(GameConfig config)
        {
            if (saveVersion <= 0) saveVersion = CurrentVersion;
            profile ??= new ProfileSave();
            hole ??= new HoleSave();
            store ??= new StoreSave();
            meta ??= new MetaSave();
            progress ??= new ProgressSave();
            settings ??= new SettingsSave();

            if (string.IsNullOrEmpty(profile.playerId))
                profile.playerId = "user_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            profile.softCurrency = Sanitize(profile.softCurrency, 0d);
            profile.lifetimeEarnings = Sanitize(profile.lifetimeEarnings, 0d);
            if (profile.hardCurrency < 0) profile.hardCurrency = 0;

            float minRadius = config != null ? config.hole.initialRadius : 1f;
            hole.level = Mathf.Max(1, hole.level);
            hole.currentRadius = Mathf.Max(minRadius, Sanitize(hole.currentRadius, minRadius));
            hole.capacityMax = Mathf.Max(1f, Sanitize(hole.capacityMax, 50f));
            if (string.IsNullOrEmpty(hole.equippedSkin)) hole.equippedSkin = "skin_default";

            store.activeDistrict = Mathf.Max(0, store.activeDistrict);
            store.unlockedNodeIds ??= new List<string>();
            store.machineStates ??= new Dictionary<string, MachineSave>();

            meta.interstitialCooldown = Mathf.Max(0f, Sanitize(meta.interstitialCooldown, 0f));
            meta.appLaunches = Mathf.Max(0, meta.appLaunches);

            progress.playerLevel = Mathf.Max(1, progress.playerLevel);
            progress.xp = Mathf.Max(0f, Sanitize(progress.xp, 0f));
            progress.upgradeLevels ??= new Dictionary<string, int>();
            progress.productStock ??= new Dictionary<string, double>();
            progress.nodeProgress ??= new Dictionary<string, double>();
            progress.robotsHired = Mathf.Max(0, progress.robotsHired);

            settings.musicVolume = Mathf.Clamp01(Sanitize(settings.musicVolume, 0.7f));
            settings.sfxVolume = Mathf.Clamp01(Sanitize(settings.sfxVolume, 1f));
            return this;
        }

        static double Sanitize(double v, double fallback) => double.IsNaN(v) || double.IsInfinity(v) || v < 0d ? fallback : v;
        static float Sanitize(float v, float fallback) => float.IsNaN(v) || float.IsInfinity(v) ? fallback : v;

        // ------------------------------------------------------------------ JSON

        public string ToJson(bool pretty = false) => ToNode().ToJson(pretty);

        public JsonNode ToNode()
        {
            var root = JsonNode.NewObject();
            root["saveVersion"] = JsonNode.From(saveVersion);
            root["timestamp"] = JsonNode.From(timestamp);

            var p = JsonNode.NewObject();
            p["playerId"] = JsonNode.From(profile.playerId);
            p["softCurrency"] = JsonNode.From(profile.softCurrency);
            p["hardCurrency"] = JsonNode.From(profile.hardCurrency);
            p["lifetimeEarnings"] = JsonNode.From(profile.lifetimeEarnings);
            p["noAdsActive"] = JsonNode.From(profile.noAdsActive);
            root["profile"] = p;

            var h = JsonNode.NewObject();
            h["level"] = JsonNode.From(hole.level);
            h["currentRadius"] = JsonNode.From(hole.currentRadius);
            h["capacityMax"] = JsonNode.From(hole.capacityMax);
            h["equippedSkin"] = JsonNode.From(hole.equippedSkin);
            root["hole"] = h;

            var s = JsonNode.NewObject();
            s["activeDistrict"] = JsonNode.From(store.activeDistrict);
            var ids = JsonNode.NewArray();
            for (int i = 0; i < store.unlockedNodeIds.Count; i++) ids.Add(JsonNode.From(store.unlockedNodeIds[i]));
            s["unlockedNodeIds"] = ids;
            var machines = JsonNode.NewObject();
            foreach (var kv in store.machineStates)
            {
                var m = JsonNode.NewObject();
                m["tier"] = JsonNode.From(kv.Value.tier);
                m["isJammed"] = JsonNode.From(kv.Value.isJammed);
                m["inputBuffer"] = JsonNode.From(kv.Value.inputBuffer);
                m["outputBuffer"] = JsonNode.From(kv.Value.outputBuffer);
                machines[kv.Key] = m;
            }
            s["machineStates"] = machines;
            root["store"] = s;

            var mt = JsonNode.NewObject();
            mt["interstitialCooldown"] = JsonNode.From(meta.interstitialCooldown);
            mt["appLaunches"] = JsonNode.From(meta.appLaunches);
            mt["reviewPrompted"] = JsonNode.From(meta.reviewPrompted);
            root["meta"] = mt;

            var pr = JsonNode.NewObject();
            pr["playerLevel"] = JsonNode.From(progress.playerLevel);
            pr["xp"] = JsonNode.From(progress.xp);
            pr["robotsHired"] = JsonNode.From(progress.robotsHired);
            pr["tutorialStep"] = JsonNode.From(progress.tutorialStep);
            pr["totalPropsEaten"] = JsonNode.From(progress.totalPropsEaten);
            pr["puzzlesSolved"] = JsonNode.From(progress.puzzlesSolved);
            var ups = JsonNode.NewObject();
            foreach (var kv in progress.upgradeLevels) ups[kv.Key] = JsonNode.From(kv.Value);
            pr["upgradeLevels"] = ups;
            var stock = JsonNode.NewObject();
            foreach (var kv in progress.productStock) stock[kv.Key] = JsonNode.From(kv.Value);
            pr["productStock"] = stock;
            var nodes = JsonNode.NewObject();
            foreach (var kv in progress.nodeProgress) nodes[kv.Key] = JsonNode.From(kv.Value);
            pr["nodeProgress"] = nodes;
            root["progress"] = pr;

            var st = JsonNode.NewObject();
            st["musicVolume"] = JsonNode.From(settings.musicVolume);
            st["sfxVolume"] = JsonNode.From(settings.sfxVolume);
            st["hapticsEnabled"] = JsonNode.From(settings.hapticsEnabled);
            st["qualityLevel"] = JsonNode.From(settings.qualityLevel);
            root["settings"] = st;

            return root;
        }

        public static SaveData FromJson(string json)
        {
            var root = JsonNode.Parse(json);
            return root == null ? null : FromNode(root);
        }

        public static SaveData FromNode(JsonNode root)
        {
            var data = new SaveData
            {
                saveVersion = root["saveVersion"]?.AsInt(CurrentVersion) ?? CurrentVersion,
                timestamp = root["timestamp"]?.AsLong(Now()) ?? Now()
            };

            var p = root["profile"];
            if (p != null)
            {
                data.profile.playerId = p["playerId"]?.AsString("") ?? "";
                data.profile.softCurrency = p["softCurrency"]?.AsDouble() ?? 0d;
                data.profile.hardCurrency = p["hardCurrency"]?.AsInt() ?? 0;
                data.profile.lifetimeEarnings = p["lifetimeEarnings"]?.AsDouble() ?? 0d;
                data.profile.noAdsActive = p["noAdsActive"]?.AsBool() ?? false;
            }

            var h = root["hole"];
            if (h != null)
            {
                data.hole.level = h["level"]?.AsInt(1) ?? 1;
                data.hole.currentRadius = h["currentRadius"]?.AsFloat(1.2f) ?? 1.2f;
                data.hole.capacityMax = h["capacityMax"]?.AsFloat(50f) ?? 50f;
                data.hole.equippedSkin = h["equippedSkin"]?.AsString("skin_default") ?? "skin_default";
            }

            var s = root["store"];
            if (s != null)
            {
                data.store.activeDistrict = s["activeDistrict"]?.AsInt() ?? 0;
                var ids = s["unlockedNodeIds"];
                if (ids != null)
                    for (int i = 0; i < ids.Count; i++) data.store.unlockedNodeIds.Add(ids[i].AsString(""));
                var machines = s["machineStates"];
                if (machines != null)
                {
                    foreach (var key in machines.Keys)
                    {
                        var m = machines[key];
                        data.store.machineStates[key] = new MachineSave
                        {
                            tier = m["tier"]?.AsInt(1) ?? 1,
                            isJammed = m["isJammed"]?.AsBool() ?? false,
                            inputBuffer = m["inputBuffer"]?.AsDouble() ?? 0d,
                            outputBuffer = m["outputBuffer"]?.AsDouble() ?? 0d
                        };
                    }
                }
            }

            var mt = root["meta"];
            if (mt != null)
            {
                data.meta.interstitialCooldown = mt["interstitialCooldown"]?.AsFloat() ?? 0f;
                data.meta.appLaunches = mt["appLaunches"]?.AsInt() ?? 0;
                data.meta.reviewPrompted = mt["reviewPrompted"]?.AsBool() ?? false;
            }

            var pr = root["progress"];
            if (pr != null)
            {
                data.progress.playerLevel = pr["playerLevel"]?.AsInt(1) ?? 1;
                data.progress.xp = pr["xp"]?.AsFloat() ?? 0f;
                data.progress.robotsHired = pr["robotsHired"]?.AsInt() ?? 0;
                data.progress.tutorialStep = pr["tutorialStep"]?.AsInt() ?? 0;
                data.progress.totalPropsEaten = pr["totalPropsEaten"]?.AsInt() ?? 0;
                data.progress.puzzlesSolved = pr["puzzlesSolved"]?.AsInt() ?? 0;
                var ups = pr["upgradeLevels"];
                if (ups != null)
                    foreach (var key in ups.Keys) data.progress.upgradeLevels[key] = ups[key].AsInt();
                var stock = pr["productStock"];
                if (stock != null)
                    foreach (var key in stock.Keys) data.progress.productStock[key] = stock[key].AsDouble();
                var nodes = pr["nodeProgress"];
                if (nodes != null)
                    foreach (var key in nodes.Keys) data.progress.nodeProgress[key] = nodes[key].AsDouble();
            }

            var st = root["settings"];
            if (st != null)
            {
                data.settings.musicVolume = st["musicVolume"]?.AsFloat(0.7f) ?? 0.7f;
                data.settings.sfxVolume = st["sfxVolume"]?.AsFloat(1f) ?? 1f;
                data.settings.hapticsEnabled = st["hapticsEnabled"]?.AsBool(true) ?? true;
                data.settings.qualityLevel = st["qualityLevel"]?.AsInt(1) ?? 1;
            }

            return data;
        }
    }

    [Serializable]
    public class ProfileSave
    {
        public string playerId = "";
        public double softCurrency;
        public int hardCurrency;
        public double lifetimeEarnings;
        public bool noAdsActive;
    }

    [Serializable]
    public class HoleSave
    {
        public int level = 1;
        public float currentRadius = 1.2f;
        public float capacityMax = 50f;
        public string equippedSkin = "skin_default";
    }

    [Serializable]
    public class StoreSave
    {
        public int activeDistrict;
        public List<string> unlockedNodeIds = new List<string>();
        public Dictionary<string, MachineSave> machineStates = new Dictionary<string, MachineSave>();
    }

    [Serializable]
    public class MachineSave
    {
        public int tier = 1;
        public bool isJammed;
        public double inputBuffer;
        public double outputBuffer;
    }

    [Serializable]
    public class MetaSave
    {
        public float interstitialCooldown;
        public int appLaunches;
        public bool reviewPrompted;
    }

    /// <summary>Additive extension block (not part of the published schema).</summary>
    [Serializable]
    public class ProgressSave
    {
        public int playerLevel = 1;
        public float xp;
        public int robotsHired;
        public int tutorialStep;
        public int totalPropsEaten;
        public int puzzlesSolved;
        public Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
        public Dictionary<string, double> productStock = new Dictionary<string, double>();
        [Tooltip("Partial cash already sunk into an unfinished furnishing pad, keyed by NodeID.")]
        public Dictionary<string, double> nodeProgress = new Dictionary<string, double>();
    }

    [Serializable]
    public class SettingsSave
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;
        public bool hapticsEnabled = true;
        public int qualityLevel = 1;
    }
}
