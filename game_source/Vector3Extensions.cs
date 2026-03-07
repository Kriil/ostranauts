using System;
using UnityEngine;

public static class Vector3Extensions
{
	public static Vector2 xy(this Vector3 v)
	{
		return new Vector2(v.x, v.y);
	}

	public static Vector3 xy0(this Vector3 v)
	{
		return new Vector3(v.x, v.y);
	}
}
