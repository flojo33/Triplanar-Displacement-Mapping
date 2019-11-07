using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain.Collisions
{
	/// <summary>
	/// Terrain Collision tile element. Requires a collision tile coordinate an a link to its creating terrain
	/// collision controller to be generated. It then fetches the height data from the existing terrain tiles
	/// and creates a tessellated mesh. This mesh is then displaced using the GPU collision generator.
	/// </summary>
	[RequireComponent(typeof(MeshCollider))]
	public class TerrainCollisionTile : MonoBehaviour
	{
		private IntegerCoordinate2D _location;
		private TerrainCollisionController _controller;
		
		private static Vector3[] _initialVertices, _initialNormals;
		private static Vector4[] _initialSplats;
		private static int[] _initialTriangles;
		private Vector3[] _usedTerrainTiles;

		private List<int> _triangles;
		private List<Vector3> _vertices;
		private List<Vector3> _normals;
		private List<Vector4> _splats;
		public float[] displacements;

		private float _physicalSize;
		
		//Create a new collision Tile.
		public static TerrainCollisionTile Instantiate(IntegerCoordinate2D location, TerrainCollisionController controller)
		{
			var gameObject = new GameObject();
			gameObject.transform.parent = controller.child;
			var collider = gameObject.AddComponent<MeshCollider>();
			collider.cookingOptions = MeshColliderCookingOptions.None;
			//collider.convex = true;
			var tile = gameObject.AddComponent<TerrainCollisionTile>();
			tile.SetLocation(location, controller);
			return tile;
		}

		private void SetLocation(IntegerCoordinate2D location, TerrainCollisionController controller)
		{
			_location = location;
			var currentTransform = transform;
			currentTransform.name = "Collision Tile ("+_location.x+" "+_location.y+")";
			_controller = controller;
			_physicalSize = (_controller.collisionBlockVertexCount - 1) * controller.terrainController.terrainGeneratorSettings.gridSize;
			currentTransform.position = new Vector3(_physicalSize * _location.x, 0 , _physicalSize * _location.y);
			StartCoroutine(Build());
		}

		private IEnumerator Build()
		{
			//Compute Tile data
			
			//Build Mesh
			var size = _controller.collisionBlockVertexCount;
			GeneratePredefinedGrid(size);
			
			_triangles = new List<int>();
			_vertices = new List<Vector3>();
			_normals = new List<Vector3>();
			_splats = new List<Vector4>();
			
			while (!BuildMesh())
			{
				yield return new WaitForEndOfFrame();
			}
			var baseOffset = new Vector3(_location.x * _physicalSize,0, _location.y * _physicalSize);
			var data = new TerrainCollisionGpuGenerator.CollisionBaseData[_vertices.Count];
			for (var i = 0; i < _vertices.Count; i++)
			{
				data[i] = new TerrainCollisionGpuGenerator.CollisionBaseData()
				{
					position = _vertices[i] + baseOffset,
					normal = _normals[i],
					splat = _splats[i]
				};
			}
			
			yield return TerrainCollisionGpuGenerator.GetChunkData(data, _controller, this);
			for (var i = 0; i < displacements.Length; i++)
			{
				_vertices[i] = _vertices[i] + _normals[i] * displacements[i];
			}
			
			//Displacement Stage
			var mesh = new Mesh();
			mesh.SetVertices(_vertices);
			mesh.SetTriangles(_triangles,0);
			GetComponent<MeshCollider>().sharedMesh = mesh;
			//Register that the tile is now done building.
			yield return null;
		}

		/// <summary>
		/// Debug draw the colliders bounds when selected.
		/// </summary>
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position + new Vector3(_physicalSize/2,100,_physicalSize/2), new Vector3(_physicalSize, 200, _physicalSize) );
			foreach (var t in _usedTerrainTiles)
			{
				Gizmos.DrawLine(transform.position, t);
			}
		}

		#region mesh

		private bool BuildMesh()
		{
			var terrainTileSquares = _controller.terrainController.terrainGeneratorSettings.size - 1;
			var collisionTileVertexCount = _controller.collisionBlockVertexCount;
			var collisionTileSquares = collisionTileVertexCount - 1;
			var chunkOffsetX = collisionTileSquares * _location.x;
			var chunkOffsetY = collisionTileSquares * _location.y;
			var startTerrainTileX = Mathf.FloorToInt(chunkOffsetX / (float)terrainTileSquares);
			var startTerrainTileY = Mathf.FloorToInt(chunkOffsetY / (float)terrainTileSquares);
			var endTerrainTileX = Mathf.CeilToInt((chunkOffsetX + collisionTileSquares) / (float)terrainTileSquares);
			var endTerrainTileY = Mathf.CeilToInt((chunkOffsetY + collisionTileSquares) / (float)terrainTileSquares);
			var mainTileCountX = endTerrainTileX - startTerrainTileX + 1;
			var mainTileCountY = endTerrainTileY - startTerrainTileY + 1;
			var mainTiles = new TerrainTileData[mainTileCountX * mainTileCountY];
			_usedTerrainTiles = new Vector3[mainTileCountX * mainTileCountY];
			var dataSize = terrainTileSquares + 3;
			var startTerrainTileIndexX = startTerrainTileX * terrainTileSquares;
			var startTerrainTileIndexY = startTerrainTileY * terrainTileSquares;
			var startOffsetX = chunkOffsetX - startTerrainTileIndexX;
			var startOffsetY = chunkOffsetY - startTerrainTileIndexY;
			
			
			for (var x = 0; x < mainTileCountX; x++)
			{
				for (var y = 0; y < mainTileCountY; y++)
				{
					var tile = _controller.terrainController.GetExistingTile(x + startTerrainTileX,y + startTerrainTileY);
					if (tile == null || !tile.isBuilt)
					{
						//Debug.Log("Could not load collision tile because the terrain data has not been initialized!");
						return false;
					}
					mainTiles[x + y * mainTileCountX] = tile.data;
					_usedTerrainTiles[x + y * mainTileCountX] = tile.transform.position;
				}
			}
			
			for (var x = 0; x < _controller.collisionBlockVertexCount; x++)
			{
				for (var y = 0; y < _controller.collisionBlockVertexCount; y++)
				{
					var tileX = Mathf.FloorToInt((chunkOffsetX + x) / (float)terrainTileSquares) - startTerrainTileX;
					var tileY = Mathf.FloorToInt((chunkOffsetY + y) / (float)terrainTileSquares) - startTerrainTileY;
					var innerOffsetX = 1 + ((startOffsetX + x) % terrainTileSquares);
					var innerOffsetY = 1 + ((startOffsetY + y) % terrainTileSquares);
					
						var height = mainTiles[tileX + tileY * mainTileCountX].locationData[innerOffsetX + dataSize * innerOffsetY].position.y;
						_initialVertices[x + _controller.collisionBlockVertexCount * y] = new Vector3(
							x * _controller.terrainController.terrainGeneratorSettings.gridSize, height,
							y * _controller.terrainController.terrainGeneratorSettings.gridSize);
						_initialNormals[x + _controller.collisionBlockVertexCount * y] =
							mainTiles[tileX + tileY * mainTileCountX]
								.locationData[innerOffsetX + dataSize * innerOffsetY].normal;
						var color = mainTiles[tileX + tileY * mainTileCountX].splatTexture.GetPixel(innerOffsetX, innerOffsetY);
						_initialSplats[x + _controller.collisionBlockVertexCount * y] = new Vector4(color.r, color.g, color.b, color.a);
							

				}
			}
			
			//Definition Stage
			
			//Tessellation Stage
			for (var i = 0; i < _initialTriangles.Length / 3; i++)
			{
				AddTessellatedTriangle(i, _triangles, _vertices, _normals, _splats);
			}
			return true;
		}
		
		/// <summary>
		/// Tessellates a given triangle using a similar method to the GPU based tessellation method.
		/// </summary>
		/// <param name="id">The triangle to tessellate</param>
		/// <param name="triangles">The triangle vertex index list</param>
		/// <param name="vertices">The Vertex List</param>
		/// <param name="normals">The Normals List for each vertex</param>
		/// <param name="splats">The Splat Data List for each vertex</param>
		private void AddTessellatedTriangle(int id, List<int> triangles, List<Vector3> vertices, List<Vector3> normals, List<Vector4> splats)
		{
			var outputStartIndex = vertices.Count;
			var p1 = _initialVertices[_initialTriangles[id * 3 + 0]];
			var p2 = _initialVertices[_initialTriangles[id * 3 + 1]];
			var p3 = _initialVertices[_initialTriangles[id * 3 + 2]];
			var n1 = _initialNormals[_initialTriangles[id * 3 + 0]];
			var n2 = _initialNormals[_initialTriangles[id * 3 + 1]];
			var n3 = _initialNormals[_initialTriangles[id * 3 + 2]];
			var s1 = _initialSplats[_initialTriangles[id * 3 + 0]];
			var s2 = _initialSplats[_initialTriangles[id * 3 + 1]];
			var s3 = _initialSplats[_initialTriangles[id * 3 + 2]];
			var edgeSubdivisions = new int[3];
			var innerSubdivisions = 0;
			for (var i = 0; i < 3; i++)
			{
				edgeSubdivisions[i] = GetSubdivisions(id, i);
				innerSubdivisions += edgeSubdivisions[i];
			}

			innerSubdivisions /= 6;
			var center = (p1+p2+p3)/3.0f;
			var centerNormal = (n1+n2+n3)/3.0f;
			var centerSplat = (s1+s2+s3)/3.0f;
			
			var centralVectors = new []
			{
				p1-center,
				p2-center,
				p3-center
			};
			var centralNormalVectors = new []
			{
				n1-centerNormal,
				n2-centerNormal,
				n3-centerNormal
			};
			var centralSplatVectors = new []
			{
				s1-centerSplat,
				s2-centerSplat,
				s3-centerSplat
			};

			var innerBaseStartIndex = 0;
			var currentEdgePoints = new Vector3[3];
			var currentNormalPoints = new Vector3[3];
			var currentSplatPoints = new Vector4[3];
			var vertexCount = 0;
			for (var ring = 0; ring < innerSubdivisions; ring++)
			{
				if (ring == innerSubdivisions - 1) innerBaseStartIndex = vertices.Count;
				var centerDistance = (ring + 0.5f) / (innerSubdivisions + .5f);
				var subdivisionCount = (ring + 1) * 2;
				for (var i = 0; i < 3; i++)
				{
					currentEdgePoints[i] = centralVectors[i] * centerDistance + center;
					currentNormalPoints[i] = centralNormalVectors[i] * centerDistance + centerNormal;
					currentSplatPoints[i] = centralSplatVectors[i] * centerDistance + centerSplat;
				}
				for (var edge = 0; edge < 3; edge++)
				{
					for (var subdivision = 0; subdivision < subdivisionCount-1; subdivision++)
					{
						var value = subdivision / (subdivisionCount - 1.0f);
						
						vertices.Add(currentEdgePoints[edge] + value * (currentEdgePoints[(edge+1)%3] - currentEdgePoints[edge]));
						normals.Add(currentNormalPoints[edge] + value * (currentNormalPoints[(edge+1)%3] - currentNormalPoints[edge]));
						splats.Add(currentSplatPoints[edge] + value * (currentSplatPoints[(edge+1)%3] - currentSplatPoints[edge]));
						vertexCount++;
					}
				}

				var newVertexCount = 3 * (subdivisionCount - 1);
				if (ring == 0)
				{
					triangles.Add(outputStartIndex + 0);
					triangles.Add(outputStartIndex + 1);
					triangles.Add(outputStartIndex + 2);
				}
				else
				{
					var innerEdgeSubdivisionCount = subdivisionCount - 2;
					var startIndex = vertexCount - newVertexCount;
					var innerStartIndex = startIndex - 3 * (innerEdgeSubdivisionCount - 1);
					for (var edge = 0; edge < 3; edge++)
					{
						triangles.Add(outputStartIndex + startIndex + edge * (subdivisionCount - 1));
						triangles.Add(outputStartIndex + startIndex + edge * (subdivisionCount - 1) + 1);
						triangles.Add(outputStartIndex + innerStartIndex + edge * (innerEdgeSubdivisionCount - 1));
						
						for (var centralElement = 0; centralElement < subdivisionCount - 3; centralElement++)
						{
							var oOffset = edge * (subdivisionCount - 1) + 1 + centralElement;
							var o1 = startIndex + oOffset;
							var o2 = startIndex + oOffset+1;
							var iOffset = edge * (innerEdgeSubdivisionCount - 1) + centralElement;
							var i1 = innerStartIndex + iOffset;
							var i2 = innerStartIndex + (iOffset + 1) % ((innerEdgeSubdivisionCount-1)*3);
							triangles.Add(outputStartIndex + o1);
							triangles.Add(outputStartIndex + o2);
							triangles.Add(outputStartIndex + i1);
							triangles.Add(outputStartIndex + i1);
							triangles.Add(outputStartIndex + o2);
							triangles.Add(outputStartIndex + i2);
						}
						
						triangles.Add(outputStartIndex + startIndex + edge * (subdivisionCount - 1) + (subdivisionCount - 1) - 1);
						triangles.Add(outputStartIndex + startIndex + (edge * (subdivisionCount - 1) + (subdivisionCount - 1))%((subdivisionCount-1)*3));
						triangles.Add(outputStartIndex + innerStartIndex + (edge * (innerEdgeSubdivisionCount - 1) + (innerEdgeSubdivisionCount - 1))%((innerEdgeSubdivisionCount-1)*3));
					}
				}
			}
			
			//Connect to outer rings
			var outerInnerSubdivisions = (innerSubdivisions) * 2 + 1;
			var outerBaseStartIndex = vertices.Count;
			for (var i = 0; i < 3; i++)
			{
				var outerCount = edgeSubdivisions[i];
				var innerCount = outerInnerSubdivisions;
				var innerStartIndex = innerBaseStartIndex + i * (outerInnerSubdivisions - 2);
				var outerStartIndex = vertices.Count;
				for (var j = 0; j < outerCount-1; j++)
				{
					var value = j / (float)(outerCount - 1);
					vertices.Add(center + centralVectors[i] + value * (centralVectors[(i+1)%3] - centralVectors[i]));
					normals.Add(centerNormal + centralNormalVectors[i] + value * (centralNormalVectors[(i+1)%3] - centralNormalVectors[i]));
					splats.Add(centerSplat + centralSplatVectors[i] + value * (centralSplatVectors[(i+1)%3] - centralSplatVectors[i]));
				}
				var currentOuterIndex = 0;
				var currentInnerIndex = 0;
				while (currentOuterIndex != outerCount - 1 && currentInnerIndex != innerCount - 1)
				{
					var nextOuter = currentOuterIndex + 1;
					var nextInner = currentInnerIndex + 1;
					var fillInnerNext = false;
					if (nextOuter == outerCount)
					{
						//Fill inner
						fillInnerNext = true;
					}
					else
					{
						if (nextInner != innerCount)
						{
							fillInnerNext = nextInner / (float)(innerCount - 1) < nextOuter / (float)(outerCount - 1);
						}
					}

					triangles.Add( CheckRoundIndex(innerCount - 2, innerBaseStartIndex, currentInnerIndex, i, innerStartIndex));
					triangles.Add( CheckRoundIndex(outerCount - 1, outerBaseStartIndex, currentOuterIndex, i, outerStartIndex));
					if (fillInnerNext)
					{
						currentInnerIndex = nextInner;
						triangles.Add(CheckRoundIndex(innerCount - 2, innerBaseStartIndex, currentInnerIndex, i, innerStartIndex));
					}
					else
					{
						currentOuterIndex = nextOuter;
						triangles.Add(CheckRoundIndex(outerCount - 1, outerBaseStartIndex, currentOuterIndex, i, outerStartIndex));
					}
				}
			}
		}

		private static int CheckRoundIndex(int max, int min, int cur, int id, int normalOffset)
		{
			if (id != 2) return normalOffset + cur;
			return cur >= max ? min : normalOffset + cur;
		}

		private int GetSubdivisions(int triangle, int edge)
		{
			var p1 = _initialTriangles[triangle * 3 + (edge) % 3];
			var p2 = _initialTriangles[triangle * 3 + (edge + 1) % 3];
			var length = Vector3.Distance(_initialVertices[p1], _initialVertices[p2]);
			return Mathf.CeilToInt(length / _controller.tessellationEdgeLength);
		}
		
		private static int _currentPredefinedSize;
		
		private static void GeneratePredefinedGrid(int gridSize)
		{
			if (gridSize == _currentPredefinedSize) return;
			_currentPredefinedSize = gridSize;
			var triangleCount = gridSize - 1;
			_initialVertices = new Vector3[gridSize * gridSize];
			_initialNormals = new Vector3[gridSize * gridSize];
			_initialSplats = new Vector4[gridSize * gridSize];
			_initialTriangles = new int[triangleCount * triangleCount * 6];
			
			for (var y = 0; y < gridSize; y++)
			{
				for (var x = 0; x < gridSize; x++)
				{
					//_initialVertices[x + y * gridSize] = new Vector3(x, 0, y);
					if (x < triangleCount && y < triangleCount)
					{
						_initialTriangles[6 * (x + y * (gridSize-1)) + 0] = (x + 0) + (y + 0) * gridSize;
						_initialTriangles[6 * (x + y * (gridSize-1)) + 1] = (x + 0) + (y + 1) * gridSize;
						_initialTriangles[6 * (x + y * (gridSize-1)) + 2] = (x + 1) + (y + 1) * gridSize;
						_initialTriangles[6 * (x + y * (gridSize-1)) + 3] = (x + 0) + (y + 0) * gridSize;
						_initialTriangles[6 * (x + y * (gridSize-1)) + 4] = (x + 1) + (y + 1) * gridSize;
						_initialTriangles[6 * (x + y * (gridSize-1)) + 5] = (x + 1) + (y + 0) * gridSize;
					}
				}
			}
		}

		#endregion
	}
}