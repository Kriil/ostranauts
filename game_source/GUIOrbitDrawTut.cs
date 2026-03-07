using System;
using Ostranauts.ShipGUIs.Utilities;
using UnityEngine;

public class GUIOrbitDrawTut : MonoBehaviour
{
	public void SetNewGameObjectives(GUIOrbitDraw god, CondOwner coUser, ShipDraw sdNS)
	{
		GUIOrbitDrawTut.godInstance = god;
	}

	public static GUIOrbitDraw godInstance;

	private bool bDebugOutput;

	private float fTutorialWait = 1f;

	private bool bPause;
}
