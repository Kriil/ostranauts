using System;
using UnityEngine;

public class VFXGasPuffData
{
	public VFXGasPuffData(Vector4 ptOriginToDest)
	{
		if (ptOriginToDest == Vector4.zero)
		{
			return;
		}
		if (float.IsInfinity(ptOriginToDest.x) || float.IsNaN(ptOriginToDest.x))
		{
			ptOriginToDest.x = 0f;
		}
		if (float.IsInfinity(ptOriginToDest.y) || float.IsNaN(ptOriginToDest.y))
		{
			ptOriginToDest.y = 0f;
		}
		if (float.IsInfinity(ptOriginToDest.z) || float.IsNaN(ptOriginToDest.z))
		{
			ptOriginToDest.z = 0f;
		}
		if (float.IsInfinity(ptOriginToDest.w) || float.IsNaN(ptOriginToDest.w))
		{
			ptOriginToDest.w = 0f;
		}
		this.ptOriginToDest = ptOriginToDest;
	}

	public Vector4 ptOriginToDest;
}
