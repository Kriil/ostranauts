using System;
using UnityEngine;

public class FollowNode : MonoBehaviour
{
	private void Awake()
	{
	}

	private void Update()
	{
		if (this.tfTarget == null)
		{
			return;
		}
		Vector3 vector = this.TF.position - this.tfTarget.position;
		if (vector.magnitude > 1f)
		{
			vector.Normalize();
			this.TF.position = (this.vPullWorld = new Vector3(this.tfTarget.position.x + vector.x, this.tfTarget.position.y + vector.y, this.TF.position.z));
		}
		else
		{
			this.TF.position = this.vPullWorld;
		}
	}

	public void Init(Transform tfTarget)
	{
		if (tfTarget == null)
		{
			return;
		}
		this.tfTarget = tfTarget;
		this.vPullWorld = this.TF.position;
	}

	public Transform TF
	{
		get
		{
			if (this.tf == null)
			{
				this.tf = base.transform;
			}
			return this.tf;
		}
	}

	private Transform tfTarget;

	private Vector3 vPullWorld;

	private Transform tf;
}
