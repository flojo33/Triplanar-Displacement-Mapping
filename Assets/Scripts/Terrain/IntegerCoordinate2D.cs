namespace Terrain
{
	/// <summary>
	/// Struct that contains a 2D integer coordinate. Similar to Vector2 but with integers.
	/// </summary>
	public struct IntegerCoordinate2D
	{
		public readonly int x;
		public readonly int y;

		public int Distance => x * x + y * y;

		public IntegerCoordinate2D(int x, int y)
		{
			this.y = y;
			this.x = x;
		}

		public override bool Equals(object obj)
		{
			return obj is IntegerCoordinate2D chunkCoordinate && this == chunkCoordinate;
		}

		public static bool operator ==(IntegerCoordinate2D a, IntegerCoordinate2D b)
		{
			return a.x == b.x && a.y == b.y;
		}

		public static bool operator !=(IntegerCoordinate2D a, IntegerCoordinate2D b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return x.GetHashCode() ^ y.GetHashCode();
		}
	}
}