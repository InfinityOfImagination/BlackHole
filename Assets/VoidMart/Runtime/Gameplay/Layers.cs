using UnityEngine;

namespace VoidMart.Gameplay
{
    /// <summary>Layer / tag names created by the one-click builder, resolved once at runtime.</summary>
    public static class Layers
    {
        public const string GroundName = "VMGround";
        public const string SwallowableName = "VMSwallowable";
        public const string SubGroundName = "SubGround";
        public const string PlayerName = "VMPlayer";
        public const string StoreName = "VMStore";

        static int s_Ground = -1, s_Swallowable = -1, s_SubGround = -1, s_Player = -1, s_Store = -1;

        public static int Ground => Resolve(ref s_Ground, GroundName);
        public static int Swallowable => Resolve(ref s_Swallowable, SwallowableName);
        public static int SubGround => Resolve(ref s_SubGround, SubGroundName);
        public static int Player => Resolve(ref s_Player, PlayerName);
        public static int Store => Resolve(ref s_Store, StoreName);

        public static int SwallowableMask => 1 << Swallowable;

        static int Resolve(ref int cache, string layerName)
        {
            if (cache >= 0) return cache;
            int index = LayerMask.NameToLayer(layerName);
            cache = index >= 0 ? index : 0;
            return cache;
        }

        public static void Invalidate()
        {
            s_Ground = s_Swallowable = s_SubGround = s_Player = s_Store = -1;
        }

        public static void SetRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            var transformRoot = root.transform;
            for (int i = 0; i < transformRoot.childCount; i++)
                SetRecursively(transformRoot.GetChild(i).gameObject, layer);
        }
    }
}
