using System;
using UnityEngine;

public class EndPoint
{
	public EndPoint()
	{
		this.pt = default(Vector2);
	}

	public EndPoint(float _x, float _y)
	{
		this.pt = default(Vector2);
		this.x = _x;
		this.y = _y;
	}

	public float x
	{
		get
		{
			return this.pt.x;
		}
		set
		{
			this.pt.x = value;
		}
	}

	public float y
	{
		get
		{
			return this.pt.y;
		}
		set
		{
			this.pt.y = value;
		}
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.pt.x,
			",",
			this.pt.y,
			" - angle: ",
			this.angle,
			"; begin: ",
			this.begin
		});
	}

	public bool begin;

	public Segment segment;

	public float angle;

	public bool visualize;

	public Vector2 pt;
}
