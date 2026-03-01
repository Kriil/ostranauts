using System;
using UnityEngine;
using UnityEngine.UI;

public static class CanvasScalerExtensions
{
	public static Vector3 WorldToCanvasSpace(this CanvasScaler scaler, Vector3 worldPoint)
	{
		if (!CanvasScalerExtensions.camera && CrewSim.objInstance)
		{
			CanvasScalerExtensions.camera = CrewSim.objInstance.camHighlight;
		}
		if (!CanvasScalerExtensions.camera)
		{
			CanvasScalerExtensions.camera = UnityEngine.Object.FindObjectOfType<Camera>();
		}
		Vector3 vector = CanvasScalerExtensions.camera.WorldToViewportPoint(worldPoint);
		Vector2 v = new Vector2(vector.x * scaler.referenceResolution.x - scaler.referenceResolution.x * 0.5f, vector.y * scaler.referenceResolution.y - scaler.referenceResolution.y * 0.5f);
		return v;
	}

	public static Camera camera;
}
