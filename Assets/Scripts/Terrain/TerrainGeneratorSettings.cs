using System;
using UnityEngine;

namespace Terrain
{
	/// <summary>
	/// Contains all settings required for terrain generation.
	/// </summary>
	[Serializable]
	public struct TerrainGeneratorSettings
	{
		public int size;
    
		public float gridSize;

		public ComputeShader terrainComputeShader;
		
		public Vector2 offset;

		public float temperatureScale;

		public float snowBlend, sandBlend, temperatureBlend;

		public float scale1, scale2, scale3, scale4, scale5, scale6, scale7;

		public float pow1, pow2, pow3, pow4, pow5, pow6, pow7;

		public float height1, height3, height4, height5, weightMultiplier2;
		public float multiplierScale;

		[Range(0, 1)]
		public float sandRandom, snowRandom, temperatureRandom;

		public bool splat;
	}
}