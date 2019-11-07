using System;

namespace Assets
{
    /// <summary>
    /// Contains three Compact Materials one for the top one for the sides and one for the bottom of a mesh.
    /// </summary>
    [Serializable]
    public class TriplanarCompactMaterial
    {
        public CompactMaterial top, sides, bottom;
    }
}