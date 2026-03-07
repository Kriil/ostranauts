using System;
using UnityEngine;

public class Draggable : MonoBehaviour
{
	private void Update()
	{
		if (this.m_target == null)
		{
			return;
		}
		if (this.m_target.ship == null)
		{
			return;
		}
		if (this.m_target.ship.LoadState != Ship.Loaded.Full)
		{
			return;
		}
		if (!this.m_initialised)
		{
			this.Init(this.m_target);
		}
		Vector3 upwards = this.m_target.transform.position - base.transform.position;
		float z = base.transform.position.z;
		upwards = new Vector3(upwards.x, upwards.y, 0f);
		float num = Time.deltaTime * this.m_acceleration;
		if (upwards.magnitude > this.m_midPoint + this.m_objRadius + this.m_backlash)
		{
			if (upwards.magnitude > this.m_armLength + this.m_objRadius + num)
			{
				base.transform.position = this.m_target.transform.position - upwards.normalized * (this.m_armLength + this.m_objRadius);
			}
			else
			{
				base.transform.position += upwards.normalized * num;
			}
		}
		else
		{
			if (upwards.magnitude >= this.m_midPoint + this.m_objRadius - this.m_backlash)
			{
				return;
			}
			if (upwards.magnitude < this.m_bodyLength + this.m_objRadius - num)
			{
				base.transform.position = this.m_target.transform.position - upwards.normalized * (this.m_bodyLength + this.m_objRadius);
			}
			else
			{
				base.transform.position -= upwards.normalized * num;
			}
		}
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, z);
		Quaternion b = Quaternion.LookRotation(Vector3.forward, upwards);
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, this.m_rotateAccel * Time.deltaTime);
	}

	public void Init(CondOwner target)
	{
		if (this.m_initialised && this.m_target == target)
		{
			return;
		}
		if (target == null)
		{
			return;
		}
		if (this.m_target != null)
		{
			this.Exit(this.m_target);
		}
		base.enabled = true;
		this.m_target = target;
		this.m_midPoint = (this.m_armLength + this.m_bodyLength) / 2f;
		if (!this.m_target.bFreezeCondRules)
		{
			this.m_target.strWalkAnim = "Pull";
			this.m_target.strIdleAnim = "Pull_Single";
			this.m_target.RefreshAnim();
		}
		this.m_target.AddCondAmount("IsDragging", 1.0, 0.0, 0f);
		CondOwner component = base.GetComponent<CondOwner>();
		if (component != null)
		{
			component.tf.SetParent(this.m_target.transform.parent);
			component.Visible = false;
			component.Visible = true;
			component.AddCondAmount("IsDragged", 1.0, 0.0, 0f);
			component.ship = this.m_target.ship;
			component.objCOParent = this.m_target;
		}
		if ((double)base.transform.localPosition.z > -0.1)
		{
			base.transform.localPosition = new Vector3(base.transform.localPosition.x, base.transform.localPosition.y, -0.25f);
		}
		this.m_initialised = true;
		Item component2 = base.GetComponent<Item>();
		if (component2 != null)
		{
			this.m_objRadius = 0.25f * (float)Mathf.Max(new int[]
			{
				component2.nWidthInTiles,
				component2.nHeightInTiles,
				1
			});
		}
	}

	public void Exit(CondOwner target = null)
	{
		this.m_initialised = false;
		CondOwner component = base.GetComponent<CondOwner>();
		if (target != null)
		{
			target.ZeroCondAmount("IsDragging");
			target.strWalkAnim = "Walk";
			target.strIdleAnim = "Idle";
			target.RefreshAnim();
			if (this.m_target == target)
			{
				this.m_target = null;
				this.m_initialised = false;
				if (component != null)
				{
					component.ZeroCondAmount("IsDragged");
				}
			}
		}
		base.enabled = false;
	}

	public float ArmLength
	{
		get
		{
			return this.m_armLength;
		}
		set
		{
			this.m_armLength = value;
			this.m_midPoint = (this.m_armLength + this.m_bodyLength) / 2f;
		}
	}

	public float BodyLength
	{
		get
		{
			return this.m_bodyLength;
		}
		set
		{
			this.m_bodyLength = value;
			this.m_midPoint = (this.m_armLength + this.m_bodyLength) / 2f;
		}
	}

	private CondOwner m_target;

	private float m_acceleration = 1f;

	private float m_rotateAccel = 4f;

	private float m_backlash = 0.2f;

	private float m_armLength = 1.5f;

	private float m_bodyLength = 0.5f;

	private float m_midPoint;

	private float m_objRadius = 0.33f;

	private bool m_initialised;
}
