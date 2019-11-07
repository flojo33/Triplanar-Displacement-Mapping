using UnityEngine;

namespace Assets
{
    /// <summary>
    /// Contains all textures for a Material and combines them using the GPU for best performance.
    /// </summary>
    [CreateAssetMenu(fileName = "CompactMaterial", menuName = "Compact Material", order = 1)]
    public class CompactMaterial : ScriptableObject
    {
        public Texture2D albedo;
        public Color albedoColor = Color.white;
        public Texture2D emission;
        public Color emissionColor = Color.black;
        public Texture2D occlusion;
    
        public Texture2D specular;
        [Range(0,1)]
        public float specularScale = 1;
        [Range(0,10)]
        public float specularPower = 1;
    
    
        public Texture2D metallic;
        [Range(0,1)]
        public float metallicScale = 1;

        public Texture2D detailNormalMap;
        [Range(0,1)]
        public float detailNormalStrength = 1;
    
        public Texture2D height;
        [Range(0,1)]
        public float heightScale = 1;
        [Range(-1,1)]
        public float heightOffset;
        
        //[Range(0,10)]
        //public float normalMultiplier = 1;
        
        [Range(0,1)]
        public float maxTessellation = 1;

        public Vector2 displacementOffset = Vector2.zero;
        public Vector2 displacementScale = Vector2.one;
        
        public Vector2 textureOffset = Vector2.zero;
        public Vector2 textureScale = Vector2.one;
    
        private RenderTexture _albedoMap;
        private Material _albedoRenderMaterial;
        private RenderTexture _detailMap;
        private Material _detailRenderMaterial;
        private RenderTexture _surfaceMap;
        private Material _surfaceRenderMaterial;

        private Texture2D _demoTexture;
        
        private static readonly int SplatAlbedoMap = Shader.PropertyToID("_AlbedoMap");
        private static readonly int SplatAlbedoColor = Shader.PropertyToID("_AlbedoColor");
        private static readonly int SplatEmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int SplatEmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int SplatOcclusionMap = Shader.PropertyToID("_OcclusionMap");
        private static readonly int SplatSpecularMap = Shader.PropertyToID("_SpecularMap");
        private static readonly int SplatSpecularStrength = Shader.PropertyToID("_SpecularStrength");
        private static readonly int SplatSpecularPower = Shader.PropertyToID("_SpecularPower");
        private static readonly int SplatMetallicMap = Shader.PropertyToID("_MetallicMap");
        private static readonly int SplatMetallicStrength = Shader.PropertyToID("_MetallicStrength");
        private static readonly int SplatHeightMap = Shader.PropertyToID("_HeightMap");
        private static readonly int SplatHeightStrength = Shader.PropertyToID("_HeightStrength");
        private static readonly int SplatHeightOffset = Shader.PropertyToID("_HeightOffset");
        private static readonly int SplatMaxHeight = Shader.PropertyToID("_HeightMax");
        private static readonly int SplatDetailNormalMap = Shader.PropertyToID("_DetailNormalMap");
        private static readonly int SplatDetailNormalStrength = Shader.PropertyToID("_DetailNormalStrength");
        public RenderTexture GetAlbedoMap()
        {
            if (_albedoMap == null)
            {
                _albedoMap = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32)
                {
                    useMipMap = true, wrapMode = TextureWrapMode.Repeat
                };
            }
            var cr = RenderTexture.active;
            GetAlbedoMap(_albedoMap, 0);
            RenderTexture.active = cr;
            return _albedoMap;
        }
        public void GetAlbedoMap(RenderTexture array, int id)
        {
            if (_albedoRenderMaterial == null)
            {
                _albedoRenderMaterial = new Material(Shader.Find("Processing/AlbedoBlit"));
            }
            _albedoRenderMaterial.SetTexture(SplatAlbedoMap,albedo);
            _albedoRenderMaterial.SetColor(SplatAlbedoColor,albedoColor);
            _albedoRenderMaterial.SetTexture(SplatEmissionMap,emission);
            _albedoRenderMaterial.SetColor(SplatEmissionColor,emissionColor);
            _albedoRenderMaterial.SetTexture(SplatOcclusionMap,occlusion);
            Graphics.Blit(null, array, _albedoRenderMaterial, 0, id);
            Graphics.SetRenderTarget(null);
        }
    
        public RenderTexture GetDetailMap()
        {
            if (_detailMap == null)
            {
                _detailMap = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32)
                {
                    useMipMap = true, wrapMode = TextureWrapMode.Repeat
                };
            }
            var cr = RenderTexture.active;
            GetDetailMap(_detailMap, 0);
            RenderTexture.active = cr;
            return _detailMap;
        }
        public void GetDetailMap(RenderTexture array, int id)
        {
            if (_detailRenderMaterial == null)
            {
                _detailRenderMaterial = new Material(Shader.Find("Processing/DetailBlit"));
            }
            _detailRenderMaterial.SetTexture(SplatSpecularMap,specular);
            _detailRenderMaterial.SetFloat(SplatSpecularStrength, specularScale);
            _detailRenderMaterial.SetFloat(SplatSpecularPower, specularPower);
            _detailRenderMaterial.SetTexture(SplatMetallicMap,metallic);
            _detailRenderMaterial.SetFloat(SplatMetallicStrength, metallicScale);
            _detailRenderMaterial.SetTexture(SplatDetailNormalMap,detailNormalMap);
            _detailRenderMaterial.SetFloat(SplatDetailNormalStrength, detailNormalStrength);

            Graphics.Blit(null, array, _detailRenderMaterial, 0, id);
            Graphics.SetRenderTarget(null);
        }

        public RenderTexture GetSurfaceMap()
        {
            var cr = RenderTexture.active;
            GetSurfaceMap(_surfaceMap, 0, 10);
            RenderTexture.active = cr;
            return _surfaceMap;
        }
    
        public void GetSurfaceMap(RenderTexture array, int id, float maxTerrainDisplacement)
        {
            if (_surfaceMap == null)
            {
                _surfaceMap = new RenderTexture(1024, 1024, 0)
                {
                    useMipMap = true, wrapMode = TextureWrapMode.Repeat
                };
            }
            if (_surfaceRenderMaterial == null)
            {
                _surfaceRenderMaterial = new Material(Shader.Find("Processing/SurfaceBlit"));
            }
        
            _surfaceRenderMaterial.SetTexture(SplatHeightMap,height);
            _surfaceRenderMaterial.SetFloat(SplatHeightOffset, heightOffset);
            _surfaceRenderMaterial.SetFloat(SplatMaxHeight, maxTerrainDisplacement);
            _surfaceRenderMaterial.SetFloat(SplatHeightStrength, heightScale);
        
            Graphics.Blit(_surfaceMap, array, _surfaceRenderMaterial, 0,id);
            Graphics.SetRenderTarget(null);
        }

        public Vector4 GetDisplacementScaleOffsetVector()
        {
            return new Vector4(displacementScale.x, displacementScale.y, displacementOffset.x, displacementOffset.y);
        }
        public Vector4 GetTextureScaleOffsetVector()
        {
            return new Vector4(textureScale.x, textureScale.y, textureOffset.x, textureOffset.y);
        }
    }
}