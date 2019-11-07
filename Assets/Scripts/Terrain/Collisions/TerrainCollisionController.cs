using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain.Collisions
{
	/// <summary>
	/// Controls and manages all terrain collision tiles. TerrainCollisionUsers register with this class to notify
	/// the controller of position changes.
	/// </summary>
	[RequireComponent(typeof(TerrainController))]
	public class TerrainCollisionController : MonoBehaviour
	{
		public ComputeShader terrainCollisionComputeShader;
		[Range(2, 64)] public int collisionBlockVertexCount = 1;
		
		private bool _setupCompleted;

		private IntegerCoordinate2D[] _loadingCircle;

		[NonSerialized]
		public TerrainController terrainController;

		[NonSerialized]
		public Transform child;

		[Range(1, 3)] public int preloadRadius = 1;

		private Dictionary<IntegerCoordinate2D, TerrainCollisionTile> _collisionTileBuffer;

		private List<TerrainCollisionUser> _collisionUsers;

		private HashSet<IntegerCoordinate2D> _currentLoadingCenters;
		private Dictionary<IntegerCoordinate2D, LoadingState> _currentCenterLoadingStates;
		private IntegerCoordinate2D _intermediateCoordinate2D;

		public float tessellationEdgeLength;

		private class LoadingState {
			public int loadingCircleIndex;
		}

		private void CheckSetup()
		{
			if (_setupCompleted) return;
			child = new GameObject("Colliders").transform;
			child.parent = transform;
			terrainController = GetComponent<TerrainController>();
			_loadingCircle = TerrainUtilities.GenerateLoadingCircle(preloadRadius);
			_collisionUsers = new List<TerrainCollisionUser>();
			_collisionTileBuffer = new Dictionary<IntegerCoordinate2D, TerrainCollisionTile>();
			_currentCenterLoadingStates = new Dictionary<IntegerCoordinate2D, LoadingState>();
			_currentLoadingCenters = new HashSet<IntegerCoordinate2D>();
			_setupCompleted = true;
		}
		
		public void RegisterUser(TerrainCollisionUser user)
		{
			CheckSetup();
			_collisionUsers.Add(user);
		}
		
		public void UnregisterUser(TerrainCollisionUser user)
		{
			CheckSetup();
			_collisionUsers.Remove(user);
		}

		private void Start()
		{
			CheckSetup();
		}

		private void UpdateLoadingCenters()
		{
			var spacing = (collisionBlockVertexCount-1) * terrainController.terrainGeneratorSettings.gridSize;
			var newLoadingCenters = new HashSet<IntegerCoordinate2D>();
			foreach (var collisionUser in _collisionUsers)
			{
				var center = collisionUser.GetCurrentCenter(spacing);
				if (!_currentLoadingCenters.Remove(center))
				{
					if (newLoadingCenters.Add(center))
					{
						_currentCenterLoadingStates.Add(center, new LoadingState {loadingCircleIndex = 0});
					}
				}
				else
				{
					newLoadingCenters.Add(center);
				}
			}

			foreach (var removedLoadingCenter in _currentLoadingCenters)
			{
				_currentCenterLoadingStates.Remove(removedLoadingCenter);
			}

			_currentLoadingCenters.Clear();
			_currentLoadingCenters = newLoadingCenters;
		}

		private void FixedUpdate()
		{
			UpdateLoadingCenters();
			var loadsThisFrame = 0;
			foreach (var loadingState in _currentCenterLoadingStates)
			{
				var center = loadingState.Key;
				for(var i = loadingState.Value.loadingCircleIndex; i < _loadingCircle.Length; i++)
				{
					if (loadsThisFrame > 2) break;
					loadingState.Value.loadingCircleIndex = i;
					_intermediateCoordinate2D = new IntegerCoordinate2D(_loadingCircle[i].x + center.x, _loadingCircle[i].y + center.y);
					if (CheckTile(_intermediateCoordinate2D))
					{
						loadsThisFrame++;
					}
				}
			}
		}

		private bool CheckTile(IntegerCoordinate2D coordinate)
		{
			if (_collisionTileBuffer.TryGetValue(coordinate, out _)) return false;
			CreateTile(coordinate);
			return true;
		}

		private void CreateTile(IntegerCoordinate2D coordinate)
		{
			_collisionTileBuffer.Add(coordinate, TerrainCollisionTile.Instantiate(coordinate, this));
		}
	}
}