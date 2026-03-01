using System;
using UnityEngine;

public class Roll_Tank_Tracks : MonoBehaviour
{
	private void Start()
	{
		this.oldPosition = new Vector3(0f, 0f, 0f);
		this.rend = base.GetComponent<Renderer>();
	}

	private void Update()
	{
		this.newPosition = base.transform.position;
		this.distance = Vector3.Distance(this.oldPosition, this.newPosition);
		this.oldPosition = this.newPosition;
		this.offset = (this.offset + this.distance * this.scrollSpeed * Time.deltaTime) % 10f;
		this.rend.material.SetTextureOffset("_MainTex", new Vector2(0f, this.offset));
		this.rend.material.SetTextureOffset("_BumpMap", new Vector2(0f, this.offset));
	}

	public float scrollSpeed = 18f;

	private Vector3 oldPosition;

	private Vector3 newPosition;

	private float distance;

	private float offset;

	private Renderer rend;
}
