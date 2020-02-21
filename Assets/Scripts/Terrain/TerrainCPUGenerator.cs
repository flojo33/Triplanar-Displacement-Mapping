using System;
using System.Collections;
using UnityEngine;

namespace Terrain
{
    /// <summary>
    /// Generates terrain data on the CPU
    /// </summary>
	public static class TerrainCpuGenerator
	{
        public static IEnumerator GetChunkData(IntegerCoordinate2D coordinate2D, TerrainController controller, TerrainTile tile)
        {
            var settings = controller.terrainGeneratorSettings;
            var dataSize = settings.size + 2;
            var data = new TerrainTileVertexData[dataSize * dataSize];
            var colors = new Color[settings.size * settings.size];
            
            for (var x = 0; x < dataSize; x++)
            {
                for (var y = 0; y < dataSize; y++)
                {
                    var position = new Vector2((x - 1 + coordinate2D.x * (settings.size - 1)) * settings.gridSize, (y - 1 + coordinate2D.y * (settings.size - 1)) * settings.gridSize);
                    
                    var h1 = Perlin(position, settings.offset, settings.scale1, settings.pow1) * 
                             Perlin(position, settings.offset, settings.scale2, settings.pow2) * settings.height1;
                    var h3 = Perlin(position, settings.offset, settings.scale3, settings.pow3) * settings.height3;
                    var h4 = 0.5f * (Perlin(position, settings.offset, settings.scale4, settings.pow4) + 
                                     Perlin(position, settings.offset, settings.scale4 * 2, settings.pow4)) * settings.height4;
                    var h5 = Perlin(position, settings.offset, settings.scale5, settings.pow5) * settings.height5;
                    var mul1 = Perlin(position, settings.offset, settings.scale6, settings.pow6);
                    var mul2 = Perlin(position, settings.offset, settings.scale7, settings.pow7) * settings.weightMultiplier2;
                    var height = (h1 + h3 + h4 + h5) * 
                                 (1 + (mul1 + mul2) / (1 + settings.weightMultiplier2) * settings.multiplierScale);
                   
                    
                    var maxHeight = settings.height1 + settings.height3 + settings.height4 + settings.height5;
                    
    
                    var sandStrength = BlendSplat(1 - height/maxHeight + Perlin(position, settings.offset, settings.scale4 * 2, settings.pow4) * settings.sandRandom, settings.sandBlend, 0.99f);
                    var snowStrength = (1-sandStrength) * BlendSplat(height/maxHeight + Perlin(position, settings.offset, settings.scale4 * 2, settings.pow4) * settings.snowRandom, settings.snowBlend, 0.6f);

                    var temperature = BlendSplat((Perlin(position, settings.offset, settings.temperatureScale, 1) * ((1 + (Perlin(position, settings.offset, settings.temperatureScale * 20, 1) - 0.5f) * settings.temperatureRandom))), settings.temperatureBlend, 0.5f);
                    var remainingStrength = (1 - (snowStrength + sandStrength));
                    
                    data[x + dataSize * y].position = new Vector3((x - 1) * settings.gridSize, height, (y - 1) * settings.gridSize);
                    if (x > 0 && y > 0 && x < settings.size+1 && y < settings.size+1)
                    {
                        colors[(x-1) + settings.size * (y-1)] = new Color(remainingStrength * temperature, remainingStrength * (1 - temperature),
                            sandStrength, snowStrength);
                    }
                }
            }
            
            //Calculate Normals...
            for (var x = 0; x < settings.size; x++)
            {
                for (var y = 0; y < settings.size; y++)
                {
                    var l = data[(x + 0) + dataSize * (y + 1)].position.y;
                    var r = data[(x + 2) + dataSize * (y + 1)].position.y;
                    var b = data[(x + 1) + dataSize * (y + 0)].position.y;
                    var t = data[(x + 1) + dataSize * (y + 2)].position.y;
                    var dx = 2 * (r - l);
                    var dy = 2 * (t - b);
                    var up = -4.0f * settings.gridSize;
                    data[(x + 1) + dataSize * (y + 1)].normal = -(new Vector3(dx, up, dy) * 0.25f).normalized;
                }
            }
            
            for (var x = 0; x < settings.size; x++)
            {
                for (var y = 0; y < settings.size; y++)
                {
                    var splat = colors[x + settings.size * y];
                    var normal = data[(x+1) + dataSize * (y+1)].normal;
                    var tessellationStrength = GetTessellationStrength(splat, normal, controller.blendOffset,
                        controller.blendExponent, controller.materialData.GetMaxTessellationStrengthArray());
                    //Debug.Log(tessellationStrength);
                    data[(x+1) + dataSize * (y+1)].tessellationStrength = new Vector2(tessellationStrength, 0);
                }
            }

            tile.data = new TerrainTileData {locationData = data, splats = colors};
            yield return null;
        }
        private static float BlendSplat(float input, float factor, float offset)
        {
            return Mathf.Clamp01((input - offset) * factor + 0.5f);
        }


        private static float Perlin(Vector2 position, Vector2 offset, float scale, float pow)
        {
            return Mathf.Pow(Mathf.PerlinNoise(position.x * scale + offset.x, position.y * scale + offset.y) , pow);
        }

        private static Vector3 Abs(Vector3 vec)
        {
            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            vec.z = Mathf.Abs(vec.z);
            return vec;
        }
        private static Vector3 Max(Vector3 vec, float value)
        {
            vec.x = Mathf.Max(vec.x,value);
            vec.y = Mathf.Max(vec.y,value);
            vec.z = Mathf.Max(vec.z,value);
            return vec;
        }
        private static Vector3 Pow(Vector3 vec, float pow)
        {
            vec.x = Mathf.Pow(vec.x,pow);
            vec.y = Mathf.Pow(vec.y,pow);
            vec.z = Mathf.Pow(vec.z,pow);
            return vec;
        }
        
        private static Vector3 GetTriplanarBlend(Vector3 normal, float blendOffset, float blendExponent)
        {
            var internalBlendOffset = new Vector3(blendOffset,blendOffset, blendOffset);
            var blend = Max(Abs(normal) - internalBlendOffset,0);
            blend = Pow(blend,blendExponent);
            return blend / (blend.x + blend.y + blend.z);
        }
        
        private static float GetTessellationStrength(Color splat, Vector3 normal, float blendOffset, float blendExponent, Vector4[] maxTessellationStrengths) {
            var blend = GetTriplanarBlend(normal, blendOffset, blendExponent);
            float m = 0;
            float maxSplat = Mathf.Max(Mathf.Max(splat.r,splat.g), Mathf.Max(splat.b,splat.a));
            splat = splat / maxSplat;
            for(var i = 0; i < 4; i++)
            {
                if (!(splat[i] > 0.0f)) continue;
                if(blend.y != 0)
                {
                    m = Mathf.Sign(blend.y) > 0
                        ? Mathf.Max(m, blend.y * maxTessellationStrengths[i].x * splat[i])
                        : Mathf.Max(m, blend.y * maxTessellationStrengths[i].z * splat[i]);
                }
                if(blend.x != 0 || blend.z != 0)
                {
                    m = Mathf.Max(m, (blend.x + blend.z) * maxTessellationStrengths[i].y * splat[i]);
                }
            }
            return m;
        }
	}
}