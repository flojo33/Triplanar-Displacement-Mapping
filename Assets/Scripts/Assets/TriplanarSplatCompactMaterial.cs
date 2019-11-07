using System;
using UnityEngine;

namespace Assets
{
    /// <summary>
    /// Contains four TriplanarCompactMaterials. One for each splat
    /// </summary>
    [Serializable]
    public class TriplanarSplatCompactMaterial
    {
        public TriplanarCompactMaterial splat1, splat2, splat3, splat4;
        
        /// <summary>
        /// Get an array of tessellation strengths from all base materials.
        /// </summary>
        /// <returns>Array of max required tessellation factors</returns>
        public Vector4[] GetMaxTessellationStrengthArray()
        {
            var array = new Vector4[4];
            array[0] = new Vector4(
                splat1.top.maxTessellation,
                splat1.sides.maxTessellation,
                splat1.bottom.maxTessellation,
                0
                );
            array[1] = new Vector4(
                splat2.top.maxTessellation,
                splat2.sides.maxTessellation,
                splat2.bottom.maxTessellation,
                0
            );
            array[2] = new Vector4(
                splat3.top.maxTessellation,
                splat3.sides.maxTessellation,
                splat3.bottom.maxTessellation,
                0
            );
            array[3] = new Vector4(
                splat4.top.maxTessellation,
                splat4.sides.maxTessellation,
                splat4.bottom.maxTessellation,
                0
            );
            return array;
        }
    }
}