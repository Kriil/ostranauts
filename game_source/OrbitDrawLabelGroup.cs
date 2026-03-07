using System;
using Ostranauts.ShipGUIs.Utilities;
using TMPro;
using UnityEngine;

public class OrbitDrawLabelGroup
{
	public static bool operator true(OrbitDrawLabelGroup label)
	{
		return label != null;
	}

	public static bool operator false(OrbitDrawLabelGroup label)
	{
		return label == null;
	}

	public void Destroy()
	{
		this.shipDraw = null;
		this.labelRect = null;
		this.label = null;
		this.cg = null;
	}

	public ShipDraw shipDraw;

	public RectTransform labelRect;

	public TextMeshProUGUI label;

	public CanvasGroup cg;

	public bool active;

	public Vector3 offset;

	public float alphaTarget;

	public const float smoothBlink = 0.016f;

	public const float smoothFade = 0.032f;

	public Bounds lastDrawnBounds;

	public Bounds desiredBounds;

	public Bounds offsetBounds;
}
