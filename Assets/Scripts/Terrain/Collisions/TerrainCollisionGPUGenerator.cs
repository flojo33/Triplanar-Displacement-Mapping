using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain.Collisions
{
    /// <summary>
    /// Calculate vertex offsets using the GPU for terrain collider generation.
    /// </summary>
	public static class TerrainCollisionGpuGenerator
	{
        public static IEnumerator GetChunkData(CollisionBaseData[] inputData, TerrainCollisionController controller, TerrainCollisionTile tile)
        {
            var settings = controller.terrainController.terrainGeneratorSettings;
            var shader = controller.terrainCollisionComputeShader;
            
            var kernel = shader.FindKernel("ComputeOffsets");
            
            var dataBuffer = new ComputeBuffer(inputData.Length, 40); //See CollisionBaseData summary
            dataBuffer.SetData(inputData);
            
            var outputData = new float[inputData.Length];
            for(var i = 0; i < inputData.Length; i++)
            {
                outputData[i] = 10;
            }
            var outputBuffer = new ComputeBuffer(outputData.Length, 4); //Float = 4 Bytes
            outputBuffer.SetData(outputData);
            
            TerrainUtilities.TransferTerrainGeneratorSettingsToComputeShader(shader, settings);
            
            shader.SetBuffer(kernel, "_BaseData", dataBuffer);
            shader.SetBuffer(kernel, "_FullData", outputBuffer);
            shader.SetInt("_size", inputData.Length);
            
            //Obtain base shader data.
            controller.terrainController.SetMainComputeShaderData(shader, kernel);
            
            //Dispatch the main Kernel
            shader.Dispatch(kernel, Mathf.CeilToInt(inputData.Length / 8.0f), 1, 1);

            //Wait for the requests to finish asynchronously
            var request = AsyncGPUReadback.Request(outputBuffer);
            yield return new WaitUntil(() => request.done);
            
            //Write results back to tile
            tile.displacements = request.GetData<float>().ToArray();
            
            //Clean up GPU buffers
            dataBuffer.Release();
            dataBuffer.Dispose();
            outputBuffer.Release();
            outputBuffer.Dispose();
            yield return null;
        }
        /// <summary>
        /// Contains the data required by the Shader Kernel to compute each points offset.
        /// The total dat a size of the struct is
        /// 2 * Vector3 + 1 * Vector4 = 2 * (3 * float) + (4 * float) = 2 * (3 * 4) + (4 * 4) = 40
        /// </summary>
        public struct CollisionBaseData
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector4 splat;
        }

	}
}