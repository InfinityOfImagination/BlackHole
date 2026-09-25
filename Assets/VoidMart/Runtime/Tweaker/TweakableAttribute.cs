using System;

namespace VoidMart.Tweaker
{
    /// <summary>
    /// Marks a field or property as live-tunable.  Picked up by <see cref="RuntimeTweakerService"/>
    /// for the in-game debug panel (3-finger double tap) and by the editor Master Control window,
    /// so a value only ever has to be annotated once to appear in both places.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class TweakableAttribute : Attribute
    {
        public string Category { get; }
        public float Min { get; }
        public float Max { get; }
        public string Label { get; set; }

        public TweakableAttribute(string category, float min = 0f, float max = 100f)
        {
            Category = category;
            Min = min;
            Max = max;
        }
    }
}
