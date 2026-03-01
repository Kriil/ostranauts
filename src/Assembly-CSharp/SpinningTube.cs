using System;
using UnityEngine;

public class SpinningTube : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		base.transform.Rotate(Vector3.forward, this.m_velocity * Time.deltaTime * this.m_multiplier);
		this.m_velocity += Time.deltaTime * UnityEngine.Random.Range(-this.m_accelspeed, this.m_accelspeed) + Time.deltaTime * this.m_bias;
		if (this.m_velocity > this.m_maximum)
		{
			this.m_velocity = this.m_maximum;
		}
		if (this.m_velocity < -this.m_maximum)
		{
			this.m_velocity = -this.m_maximum;
		}
	}

	public float m_multiplier = 5f;

	public float m_velocity = 1f;

	public float m_accelspeed = 0.5f;

	public float m_bias = 0.1f;

	public float m_maximum = 3f;
}
