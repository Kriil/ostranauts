using System;
using UnityEngine;

namespace Ostranauts.Pathing
{
	public struct Vector2Int
	{
		public Vector2Int(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public Vector2Int(float x, float y)
		{
			this.x = MathUtils.RoundToInt(x);
			this.y = MathUtils.RoundToInt(y);
		}

		public static float Distance(Vector2Int a, Vector2Int b)
		{
			int num = a.x - b.x;
			int num2 = a.y - b.y;
			return Mathf.Sqrt((float)(num * num + num2 * num2));
		}

		public static bool operator ==(Vector2Int a, Vector2Int b)
		{
			return a.x == b.x && a.y == b.y;
		}

		public static bool operator !=(Vector2Int a, Vector2Int b)
		{
			return a.x != b.x || a.y != b.y;
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"( ",
				this.x,
				" | ",
				this.y,
				" )"
			});
		}

		public int x;

		public int y;
	}
}
