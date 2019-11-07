using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
	/// <summary>
	/// Contains utility functions for terrain generation.
	/// </summary>
	public static class TerrainUtilities
	{
		/// <summary>
		/// Create an array of IntegerCoordinate2D representing a spiral of tiles to load. The array is sorted from
		/// nearest to farthest coordinates to load.
		/// </summary>
		/// <param name="loadRadius">The integer radius of tiles to be loaded.
		/// 1 = only the center. 2 = 9 tiles around the center 3 = ...</param>
		/// <returns>Array of IntegerCoordinate2D sorted from nearest to farthest.</returns>
		public static IntegerCoordinate2D[] GenerateLoadingCircle(int loadRadius)
		{
			var chunks = new List<IntegerCoordinate2D>();

			for (var x = -loadRadius; x <= loadRadius; x++)
			{
				for (var y = -loadRadius; y <= loadRadius; y++)
				{
					chunks.Add(new IntegerCoordinate2D(x, y));
				}
			}

			chunks.Sort((a, b) => a.Distance.CompareTo(b.Distance));

			return chunks.ToArray();
		}
		/// <summary>
		/// Set the terrain settings to a compute shader. Used for Terrain generation on the GPU and for
		/// Collision calculations on the GPU.
		/// </summary>
		/// <param name="shader">The ComputeShader to set the values for.</param>
		/// <param name="settings">The settings where the values come from</param>
		public static void TransferTerrainGeneratorSettingsToComputeShader(ComputeShader shader, TerrainGeneratorSettings settings) {
			shader.SetFloat("_gridSize", settings.gridSize);
			shader.SetFloat("_pow1", settings.pow1);
			shader.SetFloat("_pow2", settings.pow2);
			shader.SetFloat("_pow3", settings.pow3);
			shader.SetFloat("_pow4", settings.pow4);
			shader.SetFloat("_pow5", settings.pow5);
			shader.SetFloat("_pow6", settings.pow6);
			shader.SetFloat("_pow7", settings.pow7);
			shader.SetFloat("_scale1", settings.scale1);
			shader.SetFloat("_scale2", settings.scale2);
			shader.SetFloat("_scale3", settings.scale3);
			shader.SetFloat("_scale4", settings.scale4);
			shader.SetFloat("_scale5", settings.scale5);
			shader.SetFloat("_scale6", settings.scale6);
			shader.SetFloat("_scale7", settings.scale7);
			shader.SetFloat("_height1", settings.height1);
			shader.SetFloat("_height3", settings.height3);
			shader.SetFloat("_height4", settings.height4);
			shader.SetFloat("_height5", settings.height5);
			shader.SetFloat("_scaleMultiplier", settings.multiplierScale);
			shader.SetFloat("_weightMultiplier", settings.weightMultiplier2);
			shader.SetFloat("_temperatureScale", settings.temperatureScale);
			shader.SetFloat("_temperatureBlend", settings.temperatureBlend);
			shader.SetFloat("_temperatureRandom", settings.temperatureRandom);
			shader.SetFloat("_snowRandom", settings.snowRandom);
			shader.SetFloat("_sandRandom", settings.sandRandom);
			shader.SetFloat("_snowBlend", settings.snowBlend);
			shader.SetFloat("_sandBlend", settings.sandBlend);
			shader.SetVector("_offset", settings.offset);
			shader.SetBool("_splat", settings.splat);
		}
	}
	
}