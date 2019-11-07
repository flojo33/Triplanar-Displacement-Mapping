using System;
using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
{
    /// <summary>
    /// Controls the generation and positioning of terrain tiles and stores all settings.
    /// </summary>
    public class TerrainController : MonoBehaviour
    {
        public RenderTexture textureArray;
        public Vector4[] offsets;

        [Header("Generator")] public TerrainGeneratorSettings terrainGeneratorSettings;

        public bool gpuTerrainCompute;

        [Header("Chunks/Loading")] public GameObject terrainTilePrefab;

        public Transform loadingCenter;

        private Dictionary<IntegerCoordinate2D, TerrainTile> _tileBuffer;
        private Queue<IntegerCoordinate2D> _unusedTileQueue;
        private IntegerCoordinate2D[] _loadingCircle;

        [Range(0.0f, 1.0f)] public float timeBetweenChunkLoads;
        private float _lastChunkLoad;

        [Range(1, 100)] public int loadRadius;

        [Range(0, 10)] public int loadBorder = 2;

        private Material _terrainMaterial;

        [Header("Material Settings")]
        public float normalFactor = 0.5f;
        public float divergenceFactor = 4f;
        [Range(1, 16)]
        public float blendExponent;
        [Range(0, 0.576f)]
        public float blendOffset;

        [Range(0, 1)] public float splatHeightBlendStrength;

        [Range(0, 1)] public float triplanarHeightBlendStrength;

        [Range(0, 1.0f)] public float displacementStrength;
        public float displacementDistance;

        [Range(0, 1)] public float displacementTessellationStrength;

        [Range(0, 3)] public float tessellationEdgeLength = 0.75f;
        [Range(0, 3)] public float shadowTessellationEdgeLength = 1.5f;
        
        public float tessellationFalloffStartDistance = 300.0f;
        public float tessellationFalloffDistance = 50.0f;
        public float shadowTessellationFalloffStartDistance = 50.0f;
        public float shadowTessellationFalloffDistance = 20.0f;

        public bool singleBlend;

        public TriplanarSplatCompactMaterial materialData;
        
        private Transform _child;

        //Private Variables
        private IntegerCoordinate2D _intermediateCoordinate2D;
        private int _lastCenterX, _lastCenterY, _currentLoadStartIndex;

        #region MaterialIndices

        private static readonly int SplatArray = Shader.PropertyToID("_SplatArray");
        private static readonly int TextureOffsetArray = Shader.PropertyToID("_SplatArrayOffsets");
        private static readonly int BlendExponentId = Shader.PropertyToID("_BlendExponent");
        private static readonly int BlendOffsetId = Shader.PropertyToID("_BlendOffset");
        private static readonly int SplatHeightBlendStrengthId = Shader.PropertyToID("_SplatHeightBlendStrength");
        private static readonly int DivergenceFactorId = Shader.PropertyToID("_divergenceOffset");

        private static readonly int TriplanarHeightBlendStrengthId =
            Shader.PropertyToID("_TriplanarHeightBlendStrength");

        private static readonly int DisplacementStrengthId = Shader.PropertyToID("_DisplacementStrength");
        private static readonly int DisplacementDistanceId = Shader.PropertyToID("_DisplacementDistance");

        private static readonly int DisplacementTessellationStrengthId =
            Shader.PropertyToID("_DisplacementTessellationStrength");

        private static readonly int TessellationEdgeLengthId = Shader.PropertyToID("_TessellationEdgeLength");

        private static readonly int ShadowTessellationEdgeLengthId =
            Shader.PropertyToID("_TessellationShadowEdgeLength");

        private static readonly int TessellationFalloffStartDistanceId =
            Shader.PropertyToID("_TessellationFalloffStartDistance");

        private static readonly int TessellationFalloffDistanceId = Shader.PropertyToID("_TessellationFalloffDistance");

        private static readonly int ShadowTessellationFalloffStartDistanceId =
            Shader.PropertyToID("_ShadowTessellationFalloffStartDistance");

        private static readonly int ShadowTessellationFalloffDistanceId =
            Shader.PropertyToID("_ShadowTessellationFalloffDistance");

        private static readonly int MipMapLevels = Shader.PropertyToID("_mipMapLevels");

        #endregion

        private void OnValidate()
        {
            if (materialData == null)
            {
                materialData = new TriplanarSplatCompactMaterial();
            }
        }

        public void Build()
        {
            UpdateMaterial();

            
            _loadingCircle = TerrainUtilities.GenerateLoadingCircle(loadRadius);
            if (_tileBuffer != null)
            {
                foreach (var tile in _tileBuffer)
                {
                    Destroy(tile.Value.gameObject);
                }
            }

            _tileBuffer = new Dictionary<IntegerCoordinate2D, TerrainTile>();
            _unusedTileQueue = new Queue<IntegerCoordinate2D>();
            _lastCenterX = int.MaxValue;
            _lastCenterY = int.MaxValue;
        }

        public void Start()
        {
            TerrainGpuGenerator.CleanUp();
            if (_child == null)
            {
                _child = new GameObject("Chunks").transform;
                _child.parent = transform;
            }
            Build();
        }

        public void Update()
        {
            var centerPosition = loadingCenter.position;
            var tileSize = (terrainGeneratorSettings.size - 1) * terrainGeneratorSettings.gridSize;
            var centerX = Mathf.FloorToInt((centerPosition.x + 0.5f * tileSize) / (tileSize));
            var centerY = Mathf.FloorToInt((centerPosition.z + 0.5f * tileSize) / (tileSize));
            if (centerX != _lastCenterX || centerY != _lastCenterY)
            {
                _currentLoadStartIndex = 0;
                _lastCenterX = centerX;
                _lastCenterY = centerY;
            }

            if (_currentLoadStartIndex == 0)
            {
                _unusedTileQueue.Clear();
                foreach (var tile in _tileBuffer)
                {
                    if (Mathf.Abs(tile.Key.x - centerX) > loadRadius + loadBorder ||
                        Mathf.Abs(tile.Key.y - centerY) > loadRadius + loadBorder)
                    {
                        _unusedTileQueue.Enqueue(tile.Key);
                    }
                }
            }

            for (var i = _currentLoadStartIndex; i < _loadingCircle.Length; i++)
            {
                if (Time.time - _lastChunkLoad < timeBetweenChunkLoads)
                    break;
                _intermediateCoordinate2D =
                    new IntegerCoordinate2D(_loadingCircle[i].x + centerX, _loadingCircle[i].y + centerY);
                if (CheckChunkAt(_intermediateCoordinate2D))
                {
                    _lastChunkLoad = Time.time;
                }

                _currentLoadStartIndex = i;
            }
        }

        private bool ChunkExistsAt(IntegerCoordinate2D coordinate2D)
        {
            return _tileBuffer.TryGetValue(coordinate2D, out _);
        }

        private bool CheckChunkAt(IntegerCoordinate2D coordinate2D)
        {
            if (ChunkExistsAt(coordinate2D)) return false;
            GenerateTile(coordinate2D);
            return true;
        }

        private void GenerateTile(IntegerCoordinate2D coordinate2D)
        {
            TerrainTile newTile;
            if (_unusedTileQueue.Count > 0)
            {
                var key = _unusedTileQueue.Dequeue();
                newTile = _tileBuffer[key];
                _tileBuffer.Remove(key);
            }
            else
            {
                newTile = Instantiate(terrainTilePrefab, _child).GetComponent<TerrainTile>();
            }

            newTile.Build(this, coordinate2D.x, coordinate2D.y);
            _tileBuffer.Add(coordinate2D, newTile);
        }

        public Material GetMaterial()
        {
            return _terrainMaterial;
        }

        public TerrainTile GetExistingTile(int chunkX, int chunkY)
        {
            var coordinate = new IntegerCoordinate2D(chunkX, chunkY);
            return ChunkExistsAt(coordinate) ? _tileBuffer[coordinate] : null;
        }

        public IEnumerator GetChunkData(int chunkX, int chunkY, TerrainTile tile)
        {
            var coordinate = new IntegerCoordinate2D(chunkX, chunkY);
            if (ChunkExistsAt(coordinate))
            {
                tile.data = _tileBuffer[coordinate].data;
                yield return null;
            }
            else
            {
                yield return gpuTerrainCompute
                    ? TerrainGpuGenerator.GetChunkData(coordinate, this, tile)
                    : TerrainCpuGenerator.GetChunkData(coordinate, this, tile);
            }
        }

        private void UpdateMaterial()
        {
            var terrainShader = Shader.Find("TriplanarDisplacementMapping/TriplanarDisplacementMapping");
            _terrainMaterial = new Material(terrainShader);
            if (textureArray != null)
            {
                textureArray.Release();
                textureArray = null;
            }

            textureArray = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32)
            {
                useMipMap = true,
                wrapMode = TextureWrapMode.Repeat,
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = 36
            };
            //4 splats * 3 Textures * 3 triplanar faces,
            textureArray.Create();
            offsets = new Vector4[24];
            for (var i = 0; i < 4; i++)
            {
                TriplanarCompactMaterial currentSplat;
                switch (i)
                {
                    case 0:
                        currentSplat = materialData.splat1;
                        break;
                    case 1:
                        currentSplat = materialData.splat2;
                        break;
                    case 2:
                        currentSplat = materialData.splat3;
                        break;
                    case 3:
                        currentSplat = materialData.splat4;
                        break;
                    default:
                        throw new Exception("Invalid texture splat element!");
                }

                float heightScale;
                if (singleBlend)
                {
                    heightScale = displacementDistance * normalFactor;
                }
                else
                {
                    heightScale = displacementDistance * displacementStrength * normalFactor;
                }
                
                //Top
                offsets[i * 6 + 0] = currentSplat.top.GetDisplacementScaleOffsetVector();
                offsets[i * 6 + 1] = currentSplat.top.GetTextureScaleOffsetVector();
                currentSplat.top.GetAlbedoMap(textureArray, i * 9 + 0);
                currentSplat.top.GetDetailMap(textureArray, i * 9 + 1);
                currentSplat.top.GetSurfaceMap(textureArray, i * 9 + 2, heightScale);

                //Sides
                offsets[i * 6 + 2] = currentSplat.sides.GetDisplacementScaleOffsetVector();
                offsets[i * 6 + 3] = currentSplat.sides.GetTextureScaleOffsetVector();
                currentSplat.sides.GetAlbedoMap(textureArray, i * 9 + 3);
                currentSplat.sides.GetDetailMap(textureArray, i * 9 + 4);
                currentSplat.sides.GetSurfaceMap(textureArray, i * 9 + 5, heightScale);

                //Bottom
                offsets[i * 6 + 4] = currentSplat.bottom.GetDisplacementScaleOffsetVector();
                offsets[i * 6 + 5] = currentSplat.bottom.GetTextureScaleOffsetVector();
                currentSplat.bottom.GetAlbedoMap(textureArray, i * 9 + 6);
                currentSplat.bottom.GetDetailMap(textureArray, i * 9 + 7);
                currentSplat.bottom.GetSurfaceMap(textureArray, i * 9 + 8, heightScale);
            }

            //Set Material Properties, Offsets, Scales and Textures.
            _terrainMaterial.SetFloat(BlendExponentId, blendExponent);
            _terrainMaterial.SetFloat(BlendOffsetId, blendOffset);
            _terrainMaterial.SetFloat(DivergenceFactorId, divergenceFactor);
            _terrainMaterial.SetFloat(SplatHeightBlendStrengthId, splatHeightBlendStrength);
            _terrainMaterial.SetFloat(TriplanarHeightBlendStrengthId, triplanarHeightBlendStrength);
            _terrainMaterial.SetFloat(DisplacementStrengthId, displacementStrength);
            _terrainMaterial.SetFloat(DisplacementDistanceId, displacementDistance);
            _terrainMaterial.SetFloat(DisplacementTessellationStrengthId, displacementTessellationStrength);
            _terrainMaterial.SetFloat(ShadowTessellationEdgeLengthId, shadowTessellationEdgeLength);
            _terrainMaterial.SetFloat(TessellationEdgeLengthId, tessellationEdgeLength);
            _terrainMaterial.SetFloat(TessellationFalloffStartDistanceId, tessellationFalloffStartDistance);
            _terrainMaterial.SetFloat(TessellationFalloffDistanceId, tessellationFalloffDistance);
            _terrainMaterial.SetFloat(ShadowTessellationFalloffStartDistanceId, shadowTessellationFalloffStartDistance);
            _terrainMaterial.SetFloat(ShadowTessellationFalloffDistanceId, shadowTessellationFalloffDistance);
            _terrainMaterial.SetInt(MipMapLevels, (int)Mathf.Log(textureArray.width, 2));
            _terrainMaterial.SetTexture(SplatArray, textureArray);
            /*
            if (terrainGeneratorSettings.triplanar)
            {
                Shader.EnableKeyword("TRIPLANAR_ON");
                Shader.DisableKeyword("TRIPLANAR_OFF");
            }
            else
            {
                Shader.EnableKeyword("TRIPLANAR_OFF");
                Shader.DisableKeyword("TRIPLANAR_ON");
            }
            if (singleBlend)
            {
                Shader.EnableKeyword("SINGLE_BLEND_ON");
                Shader.DisableKeyword("SINGLE_BLEND_OFF");
            }
            else
            {
                Shader.EnableKeyword("SINGLE_BLEND_OFF");
                Shader.DisableKeyword("SINGLE_BLEND_ON");
            }
            */
            /*if (terrainGeneratorSettings.tessellation)
            {
                Shader.EnableKeyword("TESSELLATION_ON");
                Shader.DisableKeyword("TESSELLATION_OFF");
            }
            else
            {
                Shader.EnableKeyword("TESSELLATION_OFF");
                Shader.DisableKeyword("TESSELLATION_ON");
            }*/
            _terrainMaterial.SetVectorArray(TextureOffsetArray, offsets);
        }

        public void ToggleKeyword(string keyword,  bool toggled)
        {
            if (toggled)
            {
                Shader.EnableKeyword(keyword);
            }
            else
            {
                Shader.DisableKeyword(keyword);
            }
        }
        
        public void SetFloatValue(string id,  float value)
        {
            if (_tileBuffer == null) return;
            foreach (var tile in _tileBuffer)
            {
                tile.Value.GetComponent<MeshRenderer>().material.SetFloat(id,value);
            }
        }
        
        
        public void SetMainComputeShaderData(ComputeShader shader, int kernelIndex)
        {
            //Set Material Properties, Offsets, Scales and Textures.
            shader.SetFloat(BlendExponentId, blendExponent);
            shader.SetFloat(BlendOffsetId, blendOffset);
            shader.SetFloat(DivergenceFactorId, divergenceFactor);
            shader.SetFloat(SplatHeightBlendStrengthId, splatHeightBlendStrength);
            shader.SetFloat(TriplanarHeightBlendStrengthId, triplanarHeightBlendStrength);
            shader.SetFloat(DisplacementStrengthId, displacementStrength);
            shader.SetFloat(DisplacementDistanceId, displacementDistance);
            shader.SetFloat(DisplacementTessellationStrengthId, displacementTessellationStrength);
            shader.SetFloat(ShadowTessellationEdgeLengthId, shadowTessellationEdgeLength);
            shader.SetFloat(TessellationEdgeLengthId, tessellationEdgeLength);
            shader.SetFloat(TessellationFalloffStartDistanceId, tessellationFalloffStartDistance);
            shader.SetFloat(TessellationFalloffDistanceId, tessellationFalloffDistance);
            shader.SetFloat(ShadowTessellationFalloffStartDistanceId, shadowTessellationFalloffStartDistance);
            shader.SetFloat(ShadowTessellationFalloffDistanceId, shadowTessellationFalloffDistance);
            shader.SetTexture(kernelIndex,SplatArray, textureArray);
            shader.SetVectorArray(TextureOffsetArray, offsets);
        }

        private void OnApplicationQuit()
        {
            TerrainGpuGenerator.CleanUp();
        }
    }
}