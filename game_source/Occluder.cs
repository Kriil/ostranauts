using System;
using UnityEngine;

public class Occluder : IComparable<Occluder>
{
	public Occluder(float angleLeft, float angleRight)
	{
		this.fAngleLeft = angleLeft;
		this.fAngleRight = angleRight;
	}

	public void SetAngles(float angleLeft, float angleRight)
	{
		this.fAngleLeft = angleLeft;
		this.fAngleRight = angleRight;
	}

	public void UpdatePositionLeft()
	{
		this.vPositionLeft = Occluder.FromAR(this.fAngleLeft, this.fRadiusLeft);
	}

	public void UpdatePositionRight()
	{
		this.vPositionRight = Occluder.FromAR(this.fAngleRight, this.fRadiusRight);
	}

	public static Vector2 FromAR(float angle, float radius)
	{
		return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
	}

	public int CompareTo(Occluder s)
	{
		if (this.fAngleRight != s.fAngleRight)
		{
			return (this.fAngleRight >= s.fAngleRight) ? 1 : -1;
		}
		return 0;
	}

	public static float sfSign;

	public float fAngleLeft;

	public float fAngleRight;

	public float fRadiusLeft;

	public float fRadiusRight;

	public Block block;

	public Vector2 vPositionLeft;

	public Vector2 vPositionRight;
}
