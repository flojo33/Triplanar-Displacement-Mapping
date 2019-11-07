using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    /// <summary>
    /// Tile of the complete terrain. This class creates a mesh and splat map from height data from the
    /// Terrain Controller.
    /// </summary>
    public class TerrainTile : MonoBehaviour
    {
        private TerrainController _terrainController;
        public TerrainTileData data;
        private static readonly int SplatMapId = Shader.PropertyToID("_SplatMap");
        private static readonly int TessellationMapId = Shader.PropertyToID("_TessellationMap");
        private int _chunkX, _chunkY;
        private Mesh _tempMesh;
        private List<Vector3> _tempVertices, _tempNormals;
        private List<int> _tempTriangles;
        private List<Vector2> _tempUvs;
        public bool isBuilt;
        private Material _material;
        
        public void Build(TerrainController controller, int chunkX, int chunkY)
        {
            isBuilt = false;
            _chunkX = chunkX;
            _chunkY = chunkY;
            _terrainController = controller;
            var currentTransform = transform;
            currentTransform.position =
                new Vector3(chunkX * (_terrainController.terrainGeneratorSettings.size - 1) * _terrainController.terrainGeneratorSettings.gridSize, 0,
                    chunkY * (_terrainController.terrainGeneratorSettings.size - 1) * _terrainController.terrainGeneratorSettings.gridSize);
        
            currentTransform.name = "chunk ("+chunkX+" "+chunkY+")";
            StartCoroutine(BuildMesh());
        }

        private void UpdateTempData(int size)
        {
            var arraySize = size * size;
            if (_tempVertices != null && _tempVertices.Count == arraySize) return;
            _tempMesh = new Mesh();
            _tempVertices = new List<Vector3>(arraySize);
            _tempNormals = new List<Vector3>(arraySize);
            _tempTriangles = new List<int>((size - 1) * (size - 1) * 2 * 3);
            _tempUvs = new List<Vector2>(arraySize);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    _tempVertices.Add(Vector3.zero);
                    _tempNormals.Add(Vector3.zero);
                    _tempUvs.Add(new Vector2((x + 1.5f) / (size + 2.0f), (y + 1.5f) / (size + 2.0f)));
                    if (x >= size - 1 || y >= size - 1) continue;
                    _tempTriangles.Add((x + 0) + size * (y + 0));
                    _tempTriangles.Add((x + 0) + size * (y + 1));
                    _tempTriangles.Add((x + 1) + size * (y + 1));
                
                    _tempTriangles.Add((x + 0) + size * (y + 0));
                    _tempTriangles.Add((x + 1) + size * (y + 1));
                    _tempTriangles.Add((x + 1) + size * (y + 0));
                }
            }
        }
    
        private IEnumerator BuildMesh()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            if (data.splatTexture != null)
            {
                if (data.splatTexture.GetType() == typeof(RenderTexture))
                {
                   // ((RenderTexture)data.splatTexture).Release();
                }
                Destroy (data.tessellationTexture);
            }
            if (data.tessellationTexture != null)
            {
                if (data.tessellationTexture.GetType() == typeof(RenderTexture))
                {
                   // ((RenderTexture)data.tessellationTexture).Release();
                }
                Destroy (data.tessellationTexture);
            }
            yield return _terrainController.GetChunkData(_chunkX, _chunkY, this);
            var size = _terrainController.terrainGeneratorSettings.size;
            var dataSize = size + 2;
            UpdateTempData(size);
            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    _tempVertices[x + y * size] = data.locationData[(x + 1) + dataSize * (y + 1)].position;
                    _tempNormals[x + y * size]  = data.locationData[(x + 1) + dataSize * (y + 1)].normal;
                }
            }

            _tempMesh.SetVertices(_tempVertices);
            _tempMesh.SetNormals(_tempNormals);
            _tempMesh.SetTriangles(_tempTriangles,0);
            _tempMesh.SetUVs(0, _tempUvs);
            _tempMesh.RecalculateBounds();
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = _tempMesh;
            var meshCollider = GetComponent<MeshCollider>();
            meshCollider.sharedMesh = _tempMesh;
            meshRenderer.enabled = true;
            if (_material == null)
            {
                _material = _terrainController.GetMaterial();
                meshRenderer.material = _material;
            }
            meshRenderer.material.SetTexture(SplatMapId, data.splatTexture);
            meshRenderer.material.SetTexture(TessellationMapId, data.tessellationTexture);
            isBuilt = true;
        }
    }
}