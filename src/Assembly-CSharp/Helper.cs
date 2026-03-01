using System;
using UnityEngine;

public class Helper
{
	public static float HelperGetThicknessFromBlock(Block block)
	{
		if (block == null)
		{
			return 0.5f;
		}
		if (block.bIsWall)
		{
			return Mathf.Max(block.rx, block.ry);
		}
		return 0f;
	}
}
