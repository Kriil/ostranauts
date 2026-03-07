using System;
using UnityEngine;

public class Utils : MonoBehaviour
{
	private void Awake()
	{
		if (base.transform.parent == null)
		{
			UnityEngine.Object.DontDestroyOnLoad(this);
		}
	}

	private void Update()
	{
		if (this.m_spawnMariner)
		{
			ParanormalSpawner.SpawnMarinerNearestDark();
			this.m_spawnMariner = false;
		}
	}

	private static Camera GetAnyCamera
	{
		get
		{
			if (!Utils.AnyCamera)
			{
				Utils.AnyCamera = UnityEngine.Object.FindObjectOfType<Camera>();
			}
			return Utils.AnyCamera;
		}
	}

	public static Vector3 WorldToCanvasSpace(Canvas canvas, Vector3 worldPoint)
	{
		Vector2 vector = canvas.worldCamera.WorldToViewportPoint(worldPoint);
		RectTransform rectTransform = canvas.transform as RectTransform;
		Vector2 v = new Vector2(vector.x * rectTransform.sizeDelta.x - rectTransform.sizeDelta.x * 0.5f, vector.y * rectTransform.sizeDelta.y - rectTransform.sizeDelta.y * 0.5f);
		return v.xy0();
	}

	public static Vector3 Resolution
	{
		get
		{
			return new Vector3((float)Screen.width, (float)Screen.height);
		}
	}

	public static Vector3 HalfResolution
	{
		get
		{
			return new Vector3((float)Screen.width, (float)Screen.height) / 2f;
		}
	}

	public bool m_spawnMariner;

	private static Camera AnyCamera;
}
