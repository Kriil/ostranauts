using System;
using UnityEngine;

public static class Vector2Extensions
{
	public static Vector3 xy0(this Vector2 v)
	{
		return new Vector3(v.x, v.y);
	}
}
