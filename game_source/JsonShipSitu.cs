using System;
using UnityEngine;

// Serialized ship/body physics state.
// This is the save DTO for ShipSitu: position, velocity, rotation, path history,
// and orbital lock state used by navigation, docking, and star-system simulation.
public class JsonShipSitu
{
	// Parent body-or-ship reference used for local orbital frames.
	public string boPORShip { get; set; }

	public double vPosx { get; set; }

	public double vPosy { get; set; }

	public double vBOOffsetx { get; set; }

	public double vBOOffsety { get; set; }

	public double vVelX { get; set; }

	public double vVelY { get; set; }

	public double fPathLastEpoch { get; set; }

	// Acceleration components from different subsystems (input, RCS, external forces, lift, drag).
	public Vector2 vAccIn { get; set; }

	public Vector2 vAccRCS { get; set; }

	public Vector2 vAccEx { get; set; }

	public double[] aPathRecentT { get; set; }

	public double[] aPathRecentX { get; set; }

	public double[] aPathRecentY { get; set; }

	public Vector2 vAccLift { get; set; }

	public Vector2 vAccDrag { get; set; }

	public float fRot { get; set; }

	public float fW { get; set; }

	public float fA { get; set; }

	public bool bBOLocked { get; set; }

	public bool bOrbitLocked { get; set; }

	public bool bIsBO { get; set; }

	public bool bIsRegion { get; set; }

	public bool bIsNoFees { get; set; }

	// `jnd` likely contains the UI/navigation-facing nav data shown in orbit displays.
	public int size { get; set; }

	public JsonNavData jnd { get; set; }

	public bool bIgnoreGrav { get; set; }
}
