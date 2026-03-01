using System;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
	private void Awake()
	{
		this.tf = base.transform;
		this.UpdateStats();
	}

	public void UpdateStats()
	{
		this.x = this.tf.position.x;
		this.y = this.tf.position.y;
	}

	public void GetSegments(ref List<Segment> segments)
	{
		int num = 4;
		while (segments.Count < num)
		{
			segments.Add(new Segment());
		}
		while (segments.Count > num)
		{
			segments.RemoveAt(segments.Count - 1);
		}
		segments[0].Set(this.x - this.rx, this.y + this.ry, this.x + this.rx, this.y + this.ry);
		segments[1].Set(this.x + this.rx, this.y + this.ry, this.x + this.rx, this.y - this.ry);
		segments[2].Set(this.x + this.rx, this.y - this.ry, this.x - this.rx, this.y - this.ry);
		segments[3].Set(this.x - this.rx, this.y - this.ry, this.x - this.rx, this.y + this.ry);
	}

	public void RotateCW()
	{
		float num = this.rx;
		this.rx = this.ry;
		this.ry = num;
		this.UpdateStats();
	}

	public Transform TF
	{
		get
		{
			return this.tf;
		}
	}

	public float x;

	public float y;

	public float rx = 1f;

	public float ry = 1f;

	private Transform tf;

	public bool bVisible;

	public bool bIsWall;

	public bool bIsGlass;
}
