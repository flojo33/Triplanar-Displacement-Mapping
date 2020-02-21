using UnityEngine;

namespace Terrain
{
    /// <summary>
    /// This struct contains all data required by a chunk in order for it to be rendered.
    /// </summary>
    public struct TerrainTileData
    {
        public TerrainTileVertexData[] locationData;
        public Color[] splats;
    }
    public struct TerrainTileVertexData
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 tessellationStrength;
    }
}