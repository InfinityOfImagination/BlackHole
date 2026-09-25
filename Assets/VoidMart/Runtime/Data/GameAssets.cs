using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Data
{
    /// <summary>
    /// Registry of everything the generators produce.  Runtime code asks for assets by string key
    /// ("mesh_car_sedan", "sfx_cash", "icon_gem"), which keeps prefabs free of hard-wired links and
    /// lets the Master Tool swap any asset without touching a scene.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAssets", menuName = "VoidMart/Game Assets", order = 1)]
    public class GameAssets : ScriptableObject
    {
        [Serializable] public class MeshEntry { public string key; public Mesh mesh; }
        [Serializable] public class MaterialEntry { public string key; public Material material; }
        [Serializable] public class SpriteEntry { public string key; public Sprite sprite; }
        [Serializable] public class ClipEntry { public string key; public AudioClip clip; }
        [Serializable] public class PrefabEntry { public string key; public GameObject prefab; }

        public List<MeshEntry> meshes = new List<MeshEntry>();
        public List<MaterialEntry> materials = new List<MaterialEntry>();
        public List<SpriteEntry> sprites = new List<SpriteEntry>();
        public List<ClipEntry> clips = new List<ClipEntry>();
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();

        [Header("Singletons")]
        public VMFontAsset displayFont;
        public Material skyboxMaterial;
        public Material holeMaskMaterial;
        public Material pitInteriorMaterial;

        Dictionary<string, Mesh> m_MeshLookup;
        Dictionary<string, Material> m_MaterialLookup;
        Dictionary<string, Sprite> m_SpriteLookup;
        Dictionary<string, AudioClip> m_ClipLookup;
        Dictionary<string, GameObject> m_PrefabLookup;

        void OnEnable() => InvalidateCaches();

        public void InvalidateCaches()
        {
            m_MeshLookup = null;
            m_MaterialLookup = null;
            m_SpriteLookup = null;
            m_ClipLookup = null;
            m_PrefabLookup = null;
        }

        static Dictionary<string, TValue> Build<TEntry, TValue>(List<TEntry> list, Func<TEntry, string> key, Func<TEntry, TValue> value)
        {
            var dict = new Dictionary<string, TValue>(Mathf.Max(8, list.Count));
            for (int i = 0; i < list.Count; i++)
            {
                string k = key(list[i]);
                if (string.IsNullOrEmpty(k)) continue;
                dict[k] = value(list[i]);
            }
            return dict;
        }

        public Mesh GetMesh(string key)
        {
            m_MeshLookup ??= Build(meshes, e => e.key, e => e.mesh);
            return key != null && m_MeshLookup.TryGetValue(key, out var v) ? v : null;
        }

        public Material GetMaterial(string key)
        {
            m_MaterialLookup ??= Build(materials, e => e.key, e => e.material);
            return key != null && m_MaterialLookup.TryGetValue(key, out var v) ? v : null;
        }

        public Sprite GetSprite(string key)
        {
            m_SpriteLookup ??= Build(sprites, e => e.key, e => e.sprite);
            return key != null && m_SpriteLookup.TryGetValue(key, out var v) ? v : null;
        }

        public AudioClip GetClip(string key)
        {
            m_ClipLookup ??= Build(clips, e => e.key, e => e.clip);
            return key != null && m_ClipLookup.TryGetValue(key, out var v) ? v : null;
        }

        public GameObject GetPrefab(string key)
        {
            m_PrefabLookup ??= Build(prefabs, e => e.key, e => e.prefab);
            return key != null && m_PrefabLookup.TryGetValue(key, out var v) ? v : null;
        }

        // ------------------------------------------------------------ authoring

        public void SetMesh(string key, Mesh mesh) => Set(meshes, key, e => e.key, (e, m) => e.mesh = m, mesh, k => new MeshEntry { key = k });
        public void SetMaterial(string key, Material material) => Set(materials, key, e => e.key, (e, m) => e.material = m, material, k => new MaterialEntry { key = k });
        public void SetSprite(string key, Sprite sprite) => Set(sprites, key, e => e.key, (e, m) => e.sprite = m, sprite, k => new SpriteEntry { key = k });
        public void SetClip(string key, AudioClip clip) => Set(clips, key, e => e.key, (e, m) => e.clip = m, clip, k => new ClipEntry { key = k });
        public void SetPrefab(string key, GameObject prefab) => Set(prefabs, key, e => e.key, (e, m) => e.prefab = m, prefab, k => new PrefabEntry { key = k });

        static void Set<TEntry, TValue>(List<TEntry> list, string key, Func<TEntry, string> getKey,
            Action<TEntry, TValue> setValue, TValue value, Func<string, TEntry> factory)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (getKey(list[i]) != key) continue;
                setValue(list[i], value);
                return;
            }
            var entry = factory(key);
            setValue(entry, value);
            list.Add(entry);
        }

        public void ClearAll()
        {
            meshes.Clear();
            materials.Clear();
            sprites.Clear();
            clips.Clear();
            prefabs.Clear();
            InvalidateCaches();
        }
    }
}
