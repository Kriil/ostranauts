using System;
using UnityEngine;

public class RandomLamp : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (this.m_chosenTime <= 0f)
		{
			this.m_chosenTime = UnityEngine.Random.Range(this.m_minTime, this.m_maxTime);
			this.m_lamp.State = 1;
		}
		this.m_chosenTime -= Time.deltaTime;
	}

	public GUILamp m_lamp;

	public float m_minTime = 10f;

	public float m_maxTime = 60f;

	public float m_chosenTime;
}
