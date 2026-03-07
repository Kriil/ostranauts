using System;
using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
	private void Start()
	{
		this.startPos = base.transform.localPosition;
		this.watchPos = this.Watching.transform.position;
	}

	private void Update()
	{
		Vector3 vector = this.Watching.transform.position - this.watchPos;
		vector.x *= this.rate.x;
		vector.y *= this.rate.y;
		vector.z = 0f;
		vector = this.startPos - vector;
		base.transform.localPosition = vector;
	}

	public GameObject Watching;

	public Vector2 rate = new Vector2(0.1f, 0.1f);

	private Vector3 startPos;

	private Vector3 watchPos;
}
