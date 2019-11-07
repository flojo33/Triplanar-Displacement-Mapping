using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
{
    /// <summary>
    /// Generates terrain height data on the GPU using compute shaders.
    /// OBSOLETE: not used anymore because it is slower than calculating the data directly on the CPU.
    /// Could be improved in future versions to allow the mesh to be rendered directly from the GPU. This could speed
    /// up the process drastically.
    /// </summary>
    public static class TerrainGpuGenerator
    {
        private static ComputeBuffer _dataBuffer;
        private static TerrainTileVertexData[] _inputData;

        public static IEnumerator GetChunkData(IntegerCoordinate2D coordinate2D, TerrainController controller,
            TerrainTile tile)
        {
            //Working = true;
            var settings = controller.terrainGeneratorSettings;
            var shader = settings.terrainComputeShader;
            //Debug.Log("Starting GPU Compute "+Time.frameCount);
            var dataSize = settings.size + 2;
            var kernel1 = settings.terrainComputeShader.FindKernel("TerrainGeneratorPass1");
            var kernel2 = settings.terrainComputeShader.FindKernel("TerrainGeneratorPass2");
            if (_inputData == null || _inputData.Length != dataSize * dataSize)
            {
                _inputData = new TerrainTileVertexData[dataSize * dataSize];
                _dataBuffer =
                    new ComputeBuffer(_inputData.Length, 24); //2 * Vector3 = 2 * (3 * float) = 2 * (3 * 4) = 24
                _dataBuffer.SetData(_inputData);
                shader.SetInt("_size", dataSize);
                TerrainUtilities.TransferTerrainGeneratorSettingsToComputeShader(shader, settings);
                shader.SetBuffer(kernel1, "_Data", _dataBuffer);
                shader.SetBuffer(kernel2, "_Data", _dataBuffer);
                shader.SetFloat("_BlendExponent", controller.blendExponent);
                shader.SetFloat("_BlendOffset", controller.blendOffset);
                shader.SetVectorArray("_maxTessellationStrengths",
                    controller.materialData.GetMaxTessellationStrengthArray());
            }

            //Debug.Log("StartX: "+coordinate2D.x * (settings.size - 1));
            settings.terrainComputeShader.SetInt("_startX", coordinate2D.x * (settings.size - 1));
            settings.terrainComputeShader.SetInt("_startY", coordinate2D.y * (settings.size - 1));
            if (tile.data.splatTexture != null)
            {
                Object.Destroy(tile.data.splatTexture);
            }

            //Init splat RenderTexture
            var splatTexture = new RenderTexture(dataSize, dataSize, 0)
            {
                enableRandomWrite = true, useMipMap = true, filterMode = FilterMode.Bilinear
            };
            splatTexture.Create();

            settings.terrainComputeShader.SetTexture(kernel2, "_splatTexture", splatTexture);

            if (tile.data.tessellationTexture != null)
            {
                Object.Destroy(tile.data.tessellationTexture);
            }

            //Init tessellation Texture
            var tessellationTexture = new RenderTexture(dataSize, dataSize, 0)
            {
                enableRandomWrite = true, useMipMap = true, filterMode = FilterMode.Point
            };
            tessellationTexture.Create();

            settings.terrainComputeShader.SetTexture(kernel2, "_tessellationTexture", tessellationTexture);

            settings.terrainComputeShader.Dispatch(kernel1, dataSize * dataSize / 8, 1, 1);
            settings.terrainComputeShader.Dispatch(kernel2, dataSize * dataSize / 8, 1, 1);

            var request = AsyncGPUReadback.Request(_dataBuffer);
            yield return new WaitUntil(() => request.done);
            _inputData = request.GetData<TerrainTileVertexData>().ToArray();
            //Combine all data into output struct
            tile.data = new TerrainTileData
            {
                locationData = _inputData//, splatTexture = splatTexture, tessellationTexture = tessellationTexture
            };
            yield return null;
        }

        public static void CleanUp()
        {
            _inputData = null;
            if (_dataBuffer == null) return;
            _dataBuffer.Release();
            _dataBuffer.Dispose();
        }
    }
}