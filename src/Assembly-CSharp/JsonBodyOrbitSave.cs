using System;

// Serialized body-orbit runtime state.
// This is the save DTO written for existing BodyOrbit instances so StarSystem
// can reconstruct exact orbital parameters, parallax, and visibility state.
public class JsonBodyOrbitSave
{
	// Body id plus saved orbit geometry.
	public string strName { get; set; }

	public double fPerh { get; set; }

	public double fAph { get; set; }

	public double fAxis1 { get; set; }

	public double fEcc { get; set; }

	public double fAxis2 { get; set; }

	public double fAngle { get; set; }

	public double fPeriod { get; set; }

	public double fRadius { get; set; }

	public double fMass { get; set; }

	public double fRotationPeriod { get; set; }

	public int nDrawFlagsTrack { get; set; }

	public int nDrawFlagsBody { get; set; }

	public double fParallaxRadius { get; set; }

	public double fGravParallaxRadius { get; set; }

	public double fPeriodShift { get; set; }

	public int nOrbitDirection { get; set; }

	public double fVisibilityRangeMod { get; set; }

	public double fVisibilityRangeModGrav { get; set; }

	public string strParallax { get; set; }

	public string strGravParallax { get; set; }

	public JsonAtmosphere[] aAtmospheres { get; set; }

	// Keeps logs/debug output compact by returning the body id.
	public override string ToString()
	{
		return this.strName;
	}
}
