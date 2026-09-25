using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.EditorTools;
using VoidMart.Gameplay;
using VoidMart.Services;
using VoidMart.Store;
using VoidMart.Tweaker;

namespace VoidMart.EditorTools.MasterTool
{
    /// <summary>
    /// Void Mart Master Control — one window to build the game and tune every value, colour and
    /// asset in it.  The Tweakables tab is generated from the same [Tweakable] attributes the
    /// in-game debug panel uses, so annotating a field once surfaces it in both places.
    /// </summary>
    public class VoidMartMasterWindow : EditorWindow
    {
        enum Tab
        {
            Dashboard, Tweakables, Balance, Hole, City, Store, Content, Nodes,
            Puzzle, Theme, Audio, Interface, Monetization, Assets, Debug
        }

        static readonly string[] TabLabels =
        {
            "Dashboard", "Tweakables", "Balance", "Hole & Camera", "City", "Store", "Content", "Store Nodes",
            "Puzzle", "Theme & Art", "Audio", "Interface", "Monetization", "Assets", "Debug"
        };

        [SerializeField] Tab m_Tab = Tab.Dashboard;
        [SerializeField] Vector2 m_Scroll;
        [SerializeField] string m_Filter = "";
        [SerializeField] int m_AssetCategory;

        GameConfig m_Config;
        SerializedObject m_Serialized;
        GUIStyle m_HeaderStyle;
        GUIStyle m_SubtleStyle;
        GUIStyle m_TabStyle;
        GUIStyle m_ActiveTabStyle;
        readonly Dictionary<string, bool> m_Folds = new Dictionary<string, bool>(32);

        [MenuItem("Tools/Void Mart/Master Control %#v", false, 1)]
        public static void Open()
        {
            var window = GetWindow<VoidMartMasterWindow>(false, "Void Mart", true);
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Void Mart");
            Refresh();
        }

        void OnFocus() => Refresh();

        void Refresh()
        {
            if (m_Config == null) m_Config = VoidMartBuilder.FindConfig();
            m_Serialized = m_Config != null ? new SerializedObject(m_Config) : null;
        }

        void BuildStyles()
        {
            if (m_HeaderStyle != null) return;

            m_HeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            m_SubtleStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            m_TabStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 26,
                padding = new RectOffset(10, 6, 4, 4)
            };
            m_ActiveTabStyle = new GUIStyle(m_TabStyle) { fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            BuildStyles();

            if (m_Config == null)
            {
                DrawMissingConfig();
                return;
            }

            if (m_Serialized == null || m_Serialized.targetObject == null) Refresh();
            m_Serialized.UpdateIfRequiredOrScript();

            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawBody();
            EditorGUILayout.EndHorizontal();

            if (m_Serialized.hasModifiedProperties) m_Serialized.ApplyModifiedProperties();
        }

        void DrawMissingConfig()
        {
            EditorGUILayout.Space(30);
            EditorGUILayout.LabelField("VOID MART", m_HeaderStyle);
            EditorGUILayout.LabelField("No generated project found yet.", m_SubtleStyle);
            EditorGUILayout.Space(12);

            if (GUILayout.Button("Build Everything (One-Click Setup)", GUILayout.Height(42)))
            {
                m_Config = VoidMartBuilder.BuildEverything(true);
                Refresh();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "This generates configuration assets, procedural meshes, textures, the display font, " +
                "the soundtrack, prefabs and both scenes, then adds them to the build list.",
                MessageType.Info);
        }

        void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(170));
            EditorGUILayout.LabelField("VOID MART", m_HeaderStyle);
            EditorGUILayout.LabelField("Master Control", m_SubtleStyle);
            EditorGUILayout.Space(6);

            var values = (Tab[])Enum.GetValues(typeof(Tab));
            for (int i = 0; i < values.Length; i++)
            {
                var style = m_Tab == values[i] ? m_ActiveTabStyle : m_TabStyle;
                if (GUILayout.Button(TabLabels[i], style)) m_Tab = values[i];
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Rebuild Everything", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Void Mart", "Re-run the full generator? Existing assets are updated in place.", "Rebuild", "Cancel"))
                {
                    VoidMartBuilder.BuildEverything(true);
                    Refresh();
                }
            }
            EditorGUILayout.EndVertical();
        }

        void DrawBody()
        {
            EditorGUILayout.BeginVertical();
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            switch (m_Tab)
            {
                case Tab.Dashboard: DrawDashboard(); break;
                case Tab.Tweakables: DrawTweakables(); break;
                case Tab.Balance: DrawSection("economy", "Economy", "Currency, XP curve and offline earnings."); break;
                case Tab.Hole: DrawHole(); break;
                case Tab.City: DrawCity(); break;
                case Tab.Store: DrawSection("store", "Storefront", "Throughput, jams, customers and helpers."); break;
                case Tab.Content: DrawContent(); break;
                case Tab.Nodes: DrawNodes(); break;
                case Tab.Puzzle: DrawPuzzle(); break;
                case Tab.Theme: DrawTheme(); break;
                case Tab.Audio: DrawAudio(); break;
                case Tab.Interface: DrawSection("ui", "Interface", "Layout, joystick and transition timings."); break;
                case Tab.Monetization: DrawMonetization(); break;
                case Tab.Assets: DrawAssets(); break;
                case Tab.Debug: DrawDebug(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------ dashboard

        void DrawDashboard()
        {
            EditorGUILayout.LabelField("Project status", m_HeaderStyle);
            var assets = m_Config.assets;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Row("Game title", m_Config.gameTitle + "  —  " + m_Config.tagline);
            Row("Theme preset", m_Config.theme.presetName);
            Row("Meshes", assets != null ? assets.meshes.Count.ToString() : "0");
            Row("Sprites", assets != null ? assets.sprites.Count.ToString() : "0");
            Row("Audio clips", assets != null ? assets.clips.Count.ToString() : "0");
            Row("Prefabs", assets != null ? assets.prefabs.Count.ToString() : "0");
            Row("Props / products / upgrades",
                $"{m_Config.props.Count} / {m_Config.products.Count} / {m_Config.upgrades.Count}");
            Row("Store nodes", m_Config.furnishingNodes.Count.ToString());
            Row("Event bus", m_Config.events != null && m_Config.events.IsComplete ? "complete" : "incomplete");
            Row("Boot scene", System.IO.File.Exists(SceneFactory.BootScenePath) ? "built" : "missing");
            Row("Game scene", System.IO.File.Exists(SceneFactory.GameScenePath) ? "built" : "missing");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Rebuild individual systems", m_HeaderStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Meshes", GUILayout.Height(30))) Run(() => VoidMartBuilder.BuildMeshes(m_Config));
            if (GUILayout.Button("Art & font", GUILayout.Height(30))) Run(() =>
            {
                VoidMartBuilder.BuildArt(m_Config);
                MaterialFactory.BuildAll(m_Config, m_Config.assets);
            });
            if (GUILayout.Button("Audio", GUILayout.Height(30))) Run(() => VoidMartBuilder.BuildAudio(m_Config));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Content tables", GUILayout.Height(30))) Run(() => VoidMartBuilder.BuildContent(m_Config));
            if (GUILayout.Button("Prefabs", GUILayout.Height(30))) Run(() => PrefabFactory.BuildAll(m_Config, m_Config.assets));
            if (GUILayout.Button("Store nodes", GUILayout.Height(30))) Run(() => VoidMartBuilder.BuildNodes(m_Config));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild scenes", GUILayout.Height(30))) Run(() =>
            {
                SceneFactory.BuildBoot(m_Config, m_Config.assets);
                SceneFactory.BuildGame(m_Config, m_Config.assets);
                ProjectSetup.ApplyBuildSettings(SceneFactory.BootScenePath, SceneFactory.GameScenePath);
            });
            if (GUILayout.Button("Open Game scene", GUILayout.Height(30))) VoidMartBuilder.OpenGameScene();
            if (GUILayout.Button(EditorApplication.isPlaying ? "Stop" : "Play", GUILayout.Height(30)))
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Identity", m_HeaderStyle);
            EditorGUILayout.PropertyField(m_Serialized.FindProperty("gameTitle"));
            EditorGUILayout.PropertyField(m_Serialized.FindProperty("tagline"));
            EditorGUILayout.PropertyField(m_Serialized.FindProperty("buildVersion"));
        }

        static void Row(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void Run(Action action)
        {
            try
            {
                action();
                AssetWriter.Save();
                Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ----------------------------------------------------------- tweakables

        void DrawTweakables()
        {
            EditorGUILayout.LabelField("Every [Tweakable] value", m_HeaderStyle);
            EditorGUILayout.LabelField(
                "The same fields the in-game 3-finger debug panel exposes. Edits here are saved into the config asset.",
                m_SubtleStyle);
            m_Filter = EditorGUILayout.TextField("Filter", m_Filter);
            EditorGUILayout.Space(6);

            var groups = new SortedDictionary<string, List<(string path, FieldInfo field, TweakableAttribute attribute)>>(StringComparer.Ordinal);

            foreach (var sectionField in typeof(GameConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object section = sectionField.GetValue(m_Config);
                if (section == null || !sectionField.FieldType.IsClass || sectionField.FieldType == typeof(string)) continue;

                foreach (var field in sectionField.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attribute = field.GetCustomAttribute<TweakableAttribute>();
                    if (attribute == null) continue;

                    string category = string.IsNullOrEmpty(attribute.Category) ? "General" : attribute.Category;
                    if (!groups.TryGetValue(category, out var list))
                    {
                        list = new List<(string, FieldInfo, TweakableAttribute)>();
                        groups.Add(category, list);
                    }
                    list.Add((sectionField.Name + "." + field.Name, field, attribute));
                }
            }

            foreach (var group in groups)
            {
                bool any = false;
                foreach (var entry in group.Value)
                    if (Matches(entry.field.Name) || Matches(group.Key)) { any = true; break; }
                if (!any) continue;

                if (!Fold(group.Key, group.Key + $"  ({group.Value.Count})")) continue;

                EditorGUI.indentLevel++;
                foreach (var entry in group.Value)
                {
                    if (!Matches(entry.field.Name) && !Matches(group.Key)) continue;
                    var property = m_Serialized.FindProperty(entry.path);
                    if (property == null) continue;

                    string label = ObjectNames.NicifyVariableName(entry.field.Name);
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Float:
                            property.floatValue = EditorGUILayout.Slider(label, property.floatValue, entry.attribute.Min, entry.attribute.Max);
                            break;
                        case SerializedPropertyType.Integer:
                            property.intValue = EditorGUILayout.IntSlider(label, property.intValue,
                                Mathf.RoundToInt(entry.attribute.Min), Mathf.RoundToInt(entry.attribute.Max));
                            break;
                        default:
                            EditorGUILayout.PropertyField(property, new GUIContent(label));
                            break;
                    }
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }
        }

        bool Matches(string text) =>
            string.IsNullOrEmpty(m_Filter) || text.IndexOf(m_Filter, StringComparison.OrdinalIgnoreCase) >= 0;

        bool Fold(string key, string label)
        {
            m_Folds.TryGetValue(key, out bool open);
            if (!m_Folds.ContainsKey(key)) open = true;
            open = EditorGUILayout.Foldout(open, label, true, EditorStyles.foldoutHeader);
            m_Folds[key] = open;
            return open;
        }

        // ------------------------------------------------------------- sections

        void DrawSection(string propertyName, string title, string help)
        {
            EditorGUILayout.LabelField(title, m_HeaderStyle);
            if (!string.IsNullOrEmpty(help)) EditorGUILayout.LabelField(help, m_SubtleStyle);
            EditorGUILayout.Space(4);

            var property = m_Serialized.FindProperty(propertyName);
            if (property == null)
            {
                EditorGUILayout.HelpBox("Missing property '" + propertyName + "'.", MessageType.Warning);
                return;
            }

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        void DrawHole()
        {
            DrawSection("hole", "Black hole", "Movement, suction, growth and fever mode.");
            EditorGUILayout.Space(8);
            DrawSection("cameraRig", "Camera", "Fixed isometric tilt, follow and zoom.");

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Push values to the live player", GUILayout.Height(26)))
                {
                    var hole = ServiceLocator.Get<PlayerHoleController>();
                    if (hole != null) hole.SyncFromConfig();
                }
            }
            EditorGUILayout.LabelField("Tier thresholds are radii; a prop is edible once its footprint fits inside the hole.", m_SubtleStyle);
        }

        void DrawCity()
        {
            DrawSection("city", "City district", "Block grid, prop budgets and respawn.");

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-roll seed", GUILayout.Height(26)))
            {
                m_Serialized.ApplyModifiedProperties();
                m_Config.city.randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
                EditorUtility.SetDirty(m_Config);
                m_Serialized.Update();
                if (EditorApplication.isPlaying)
                    ServiceLocator.Get<CityGenerator>()?.Rebuild(m_Config.city.randomSeed);
            }
            if (GUILayout.Button("Rebuild street texture", GUILayout.Height(26)))
            {
                Run(() =>
                {
                    VoidMartBuilder.BuildArt(m_Config);
                    MaterialFactory.BuildAll(m_Config, m_Config.assets);
                });
            }
            EditorGUILayout.EndHorizontal();

            float pitch = m_Config.city.blockSize + m_Config.city.roadWidth;
            EditorGUILayout.HelpBox(
                $"District is {m_Config.city.blocksX * pitch:0} x {m_Config.city.blocksZ * pitch:0} metres. " +
                $"The street texture tiles once per block, so changing the block size means rebuilding the art.",
                MessageType.None);
        }

        void DrawContent()
        {
            EditorGUILayout.LabelField("Content tables", m_HeaderStyle);
            EditorGUILayout.LabelField("Edit rows freely; press the regenerate buttons if you add ids that need new art.", m_SubtleStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(m_Serialized.FindProperty("props"), new GUIContent("Props"), true);
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(m_Serialized.FindProperty("products"), new GUIContent("Products"), true);
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(m_Serialized.FindProperty("upgrades"), new GUIContent("Upgrades"), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset tables to defaults", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Void Mart", "Replace the props, products and upgrades tables with the authored defaults?", "Reset", "Cancel"))
                    Run(() => VoidMartBuilder.BuildContent(m_Config));
            }
            if (GUILayout.Button("Rebuild prop prefabs", GUILayout.Height(26)))
                Run(() => PrefabFactory.BuildAll(m_Config, m_Config.assets));
            EditorGUILayout.EndHorizontal();
        }

        void DrawNodes()
        {
            EditorGUILayout.LabelField("Store nodes", m_HeaderStyle);
            EditorGUILayout.LabelField("Each node is a ScriptableObject: position, cost, dependencies and what it produces.", m_SubtleStyle);
            EditorGUILayout.Space(6);

            for (int i = 0; i < m_Config.furnishingNodes.Count; i++)
            {
                var node = m_Config.furnishingNodes[i];
                if (node == null) continue;

                if (!Fold("node_" + node.NodeID, $"{node.displayName}   ({node.NodeID})")) continue;

                EditorGUI.indentLevel++;
                var serialized = new SerializedObject(node);
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("Category"));
                EditorGUILayout.PropertyField(serialized.FindProperty("BaseCost"));
                EditorGUILayout.PropertyField(serialized.FindProperty("TierLevel"));
                EditorGUILayout.PropertyField(serialized.FindProperty("localPosition"));
                EditorGUILayout.PropertyField(serialized.FindProperty("yaw"));
                EditorGUILayout.PropertyField(serialized.FindProperty("zoneSize"));
                EditorGUILayout.PropertyField(serialized.FindProperty("productId"));
                EditorGUILayout.PropertyField(serialized.FindProperty("rateMultiplier"));
                EditorGUILayout.PropertyField(serialized.FindProperty("capacityMultiplier"));
                EditorGUILayout.PropertyField(serialized.FindProperty("DependentNodeIDs"), true);
                EditorGUILayout.PropertyField(serialized.FindProperty("PrefabToSpawn"));
                EditorGUILayout.PropertyField(serialized.FindProperty("unlockedFromStart"));
                if (serialized.hasModifiedProperties) serialized.ApplyModifiedProperties();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                if (GUILayout.Button("Select asset", GUILayout.Width(110))) Selection.activeObject = node;
                double cost = m_Config.economy.NodeCost(node.BaseCost, node.TierLevel);
                EditorGUILayout.LabelField($"Effective cost: {NumberFormat.Money(cost)}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Regenerate nodes from the authored layout", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Void Mart", "Overwrite every store node with the authored layout?", "Regenerate", "Cancel"))
                    Run(() => VoidMartBuilder.BuildNodes(m_Config));
            }
        }

        void DrawPuzzle()
        {
            DrawSection("puzzle", "Unjam puzzle", "The five-second block-sorting interruption.");
            EditorGUILayout.Space(8);

            var pieceSet = AssetDatabase.LoadAssetAtPath<VoidMart.Puzzle.PuzzlePieceSet>(AssetWriter.ConfigFolder + "/PuzzlePieceSet.asset");
            if (pieceSet != null)
            {
                EditorGUILayout.ObjectField("Piece set", pieceSet, typeof(VoidMart.Puzzle.PuzzlePieceSet), false);
                if (GUILayout.Button("Reset piece shapes to defaults", GUILayout.Height(24)))
                {
                    Undo.RecordObject(pieceSet, "Reset puzzle shapes");
                    pieceSet.ResetToDefaults();
                    EditorUtility.SetDirty(pieceSet);
                }
            }

            EditorGUILayout.HelpBox(
                "Boards are generated so that exactly one tray piece always completes a line - the interruption " +
                "should feel like a burst of reward, never a difficulty spike.", MessageType.None);
        }

        void DrawTheme()
        {
            EditorGUILayout.LabelField("Theme & art direction", m_HeaderStyle);
            EditorGUILayout.LabelField("Colours feed the meshes, textures, materials and UI. Rebuild the art after changing them.", m_SubtleStyle);
            EditorGUILayout.Space(6);

            DrawSection("theme", "Palette", "");
            EditorGUILayout.Space(8);
            DrawSection("art", "Generation settings", "Texture sizes, font weight and mesh detail.");

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild art from palette", GUILayout.Height(30)))
            {
                Run(() =>
                {
                    VoidMartBuilder.BuildMeshes(m_Config);
                    VoidMartBuilder.BuildArt(m_Config);
                    MaterialFactory.BuildAll(m_Config, m_Config.assets);
                    PrefabFactory.BuildAll(m_Config, m_Config.assets);
                });
            }
            if (GUILayout.Button("Rebuild materials only", GUILayout.Height(30)))
                Run(() => MaterialFactory.BuildAll(m_Config, m_Config.assets));
            EditorGUILayout.EndHorizontal();
        }

        void DrawAudio()
        {
            DrawSection("audio", "Audio", "Mix levels, the swallow pitch ladder and music synthesis.");

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Re-synthesise music and SFX", GUILayout.Height(30)))
                Run(() => VoidMartBuilder.BuildAudio(m_Config));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Generated clips", m_HeaderStyle);
            var assets = m_Config.assets;
            if (assets == null) return;

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < assets.clips.Count; i++)
            {
                var entry = assets.clips[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.key, GUILayout.Width(180));
                entry.clip = (AudioClip)EditorGUILayout.ObjectField(entry.clip, typeof(AudioClip), false);
                if (GUILayout.Button("Play", GUILayout.Width(50))) PlayClip(entry.clip);
                if (GUILayout.Button("Stop", GUILayout.Width(50))) StopClips();
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                assets.InvalidateCaches();
                EditorUtility.SetDirty(assets);
            }
        }

        static void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            var type = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = type?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (method != null) method.Invoke(null, new object[] { clip, 0, false });
            else Debug.Log("[VoidMart] Preview unavailable in this editor version; select the clip to audition it.");
        }

        static void StopClips()
        {
            var type = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = type?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, null);
        }

        void DrawMonetization()
        {
            DrawSection("monetization", "Monetization", "Placements, cooldowns and reward multipliers.");
            EditorGUILayout.Space(8);
            DrawSection("persistence", "Persistence", "Autosave cadence and encryption.");
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Ads and IAP sit behind IAdService / IIapService. The mock implementations ship with the project; " +
                "register a real mediation adaptor in ServiceInstaller to go live.", MessageType.None);
        }

        // --------------------------------------------------------------- assets

        void DrawAssets()
        {
            var assets = m_Config.assets;
            if (assets == null)
            {
                EditorGUILayout.HelpBox("No GameAssets registry yet — run the build.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Generated asset registry", m_HeaderStyle);
            EditorGUILayout.LabelField("Swap any entry for your own asset; the game looks everything up by key.", m_SubtleStyle);
            m_AssetCategory = GUILayout.Toolbar(m_AssetCategory, new[] { "Meshes", "Sprites", "Materials", "Prefabs" });
            m_Filter = EditorGUILayout.TextField("Filter", m_Filter);
            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            switch (m_AssetCategory)
            {
                case 0:
                    foreach (var entry in assets.meshes)
                    {
                        if (!Matches(entry.key)) continue;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(entry.key, GUILayout.Width(220));
                        entry.mesh = (Mesh)EditorGUILayout.ObjectField(entry.mesh, typeof(Mesh), false);
                        EditorGUILayout.LabelField(entry.mesh != null ? entry.mesh.vertexCount + " verts" : "-",
                            EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                case 1:
                    foreach (var entry in assets.sprites)
                    {
                        if (!Matches(entry.key)) continue;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(entry.key, GUILayout.Width(220));
                        entry.sprite = (Sprite)EditorGUILayout.ObjectField(entry.sprite, typeof(Sprite), false);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                case 2:
                    foreach (var entry in assets.materials)
                    {
                        if (!Matches(entry.key)) continue;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(entry.key, GUILayout.Width(220));
                        entry.material = (Material)EditorGUILayout.ObjectField(entry.material, typeof(Material), false);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                default:
                    foreach (var entry in assets.prefabs)
                    {
                        if (!Matches(entry.key)) continue;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(entry.key, GUILayout.Width(220));
                        entry.prefab = (GameObject)EditorGUILayout.ObjectField(entry.prefab, typeof(GameObject), false);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                assets.InvalidateCaches();
                EditorUtility.SetDirty(assets);
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Mark registry dirty (save overrides)", GUILayout.Height(24)))
            {
                assets.InvalidateCaches();
                EditorUtility.SetDirty(assets);
                AssetDatabase.SaveAssets();
            }
        }

        // ---------------------------------------------------------------- debug

        void DrawDebug()
        {
            EditorGUILayout.LabelField("Runtime & save", m_HeaderStyle);

            string savePath = System.IO.Path.Combine(Application.persistentDataPath, m_Config.persistence.fileName);
            EditorGUILayout.LabelField("Save file", savePath, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reveal save folder", GUILayout.Height(26)))
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            if (GUILayout.Button("Delete save", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Void Mart", "Delete the local save file?", "Delete", "Cancel"))
                {
                    try
                    {
                        if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
                        string mirror = savePath + ".debug.json";
                        if (System.IO.File.Exists(mirror)) System.IO.File.Delete(mirror);
                        Debug.Log("[VoidMart] Save deleted.");
                    }
                    catch (Exception e) { Debug.LogWarning(e.Message); }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                EditorGUILayout.LabelField("Play-mode cheats", m_HeaderStyle);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+$10,000")) ServiceLocator.Get<EconomyService>()?.AddCash(10000d);
                if (GUILayout.Button("+1,000 gems")) ServiceLocator.Get<EconomyService>()?.AddGems(1000);
                if (GUILayout.Button("Start fever")) GameManager.Instance?.StartFever();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Jam a machine"))
                {
                    var store = ServiceLocator.Get<StoreManager>();
                    if (store != null && store.Machines.Count > 0) store.Machines[0].Jam();
                }
                if (GUILayout.Button("Fill the hole")) ServiceLocator.Get<PlayerHoleController>()?.FillLoad();
                if (GUILayout.Button("Save now")) ServiceLocator.Get<SaveService>()?.SaveNow();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Shortcuts", m_HeaderStyle);
            EditorGUILayout.LabelField("• In play mode, press F1 (or triple-tap with three fingers) for the runtime tweaker.", m_SubtleStyle);
            EditorGUILayout.LabelField("• Ctrl/Cmd + Shift + V reopens this window.", m_SubtleStyle);
        }
    }
}
