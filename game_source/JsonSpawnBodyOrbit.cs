using System;

// Spawn/template payload for a generated body orbit.
// StarSystem consumes these during initialization to add stars, planets, moons,
// and other orbital bodies that are generated rather than restored from full saves.
public class JsonSpawnBodyOrbit
{
	// Internal body id and the main orbital parameters used to create the body.
	public string strName { get; set; }

	public float fPeriapsisAU { get; set; }

	public float fApoapsisAU { get; set; }

	public float fDegreesCW { get; set; }

	public float fEccentricity { get; set; }

	public float fOrbitalPeriodYears { get; set; }

	public float fRadiusKM { get; set; }

	public float fMassKG { get; set; }

	public float fRotationPeriodDays { get; set; }

	public string strNameParent { get; set; }

	// Draw flags and parallax/visibility settings control how the body is rendered in nav views.
	public int nDrawFlagsTrack { get; set; }

	public int nDrawFlagsBody { get; set; }

	public double fParallaxRadiusKM { get; set; }

	public double fGravParallaxRadiusKM { get; set; }

	public float fVisibilityRangeMod { get; set; }

	public float fVisibilityRangeModGrav { get; set; }

	public string strParallax { get; set; }

	public string strGravParallax { get; set; }

	public JsonAtmosphere[] aAtmosphericValues { get; set; }

	// Keeps logs/debug output compact by returning the body id.
	public override string ToString()
	{
		return this.strName;
	}
}
