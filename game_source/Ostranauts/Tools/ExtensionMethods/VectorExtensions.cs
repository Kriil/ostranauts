using System;
using UnityEngine;

namespace Ostranauts.Tools.ExtensionMethods
{
	public static class VectorExtensions
	{
		public static Vector2 ToVector2(this Vector3 v3)
		{
			return new Vector2(v3.x, v3.y);
		}

		public static Vector3 ToVector3(this Vector2 v2, float z)
		{
			return new Vector3(v2.x, v2.y, z);
		}
	}
}
