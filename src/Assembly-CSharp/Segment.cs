using System;
using UnityEngine;

public class Segment
{
	public void Set(float x1, float y1, float x2, float y2)
	{
		this.pos1.x = x1;
		this.pos1.y = y1;
		this.pos2.x = x2;
		this.pos2.y = y2;
	}

	public Vector2 pos1;

	public Vector2 pos2;
}
