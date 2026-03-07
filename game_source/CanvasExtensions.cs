using System;
using UnityEngine;

public static class CanvasExtensions
{
	public static Vector3 WorldToCanvasSpace(this Canvas canvas, Vector3 worldPoint)
	{
		Camera camera = canvas.worldCamera;
		if (!camera && CrewSim.objInstance)
		{
			camera = CrewSim.objInstance.camHighlight;
		}
		if (!camera)
		{
			camera = UnityEngine.Object.FindObjectOfType<Camera>();
		}
		Vector3 vector = camera.WorldToViewportPoint(worldPoint);
		RectTransform rectTransform = canvas.transform as RectTransform;
		Vector2 v = new Vector2(vector.x * rectTransform.sizeDelta.x - rectTransform.sizeDelta.x * 0.5f, vector.y * rectTransform.sizeDelta.y - rectTransform.sizeDelta.y * 0.5f);
		return v.xy0();
	}

	public static Camera camera;
}
