using UnityEngine;

namespace Terrain
{
    /// <summary>
    /// This struct contains all data required by a chunk in order for it to be rendered.
    /// </summary>
    public struct TerrainTileData
    {
        public TerrainTileVertexData[] locationData;
        public Texture2D splatTexture, tessellationTexture;
    }
    public struct TerrainTileVertexData
    {
        public Vector3 position;
        public Vector3 normal;
    }
}