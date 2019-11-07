using UnityEngine;

namespace Terrain.Collisions
{
	/// <summary>
	/// Attached to a rigidbody or other physics requiring gameObject to tell the terrain that the object requires
	/// collision data.
	/// </summary>
	public class TerrainCollisionUser : MonoBehaviour
	{
		private Transform _transform;
		private void Awake()
		{
			_transform = transform;
			var controller = FindObjectOfType<TerrainCollisionController>();
			if (controller != null)
			{
				FindObjectOfType<TerrainCollisionController>().RegisterUser(this);
			}
		}

		private void OnDestroy()
		{
			var controller = FindObjectOfType<TerrainCollisionController>();
			if (controller != null)
			{
				controller.UnregisterUser(this);
			}
		}

		public IntegerCoordinate2D GetCurrentCenter(float chunkSize)
		{
			var position = _transform.position;
			var centerX = Mathf.FloorToInt((position.x) / chunkSize);
			var centerY = Mathf.FloorToInt((position.z) / chunkSize);
			return new IntegerCoordinate2D(centerX, centerY);
		}
	}
}