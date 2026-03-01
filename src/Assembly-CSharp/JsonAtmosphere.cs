using System;

[Serializable]
// One atmospheric layer/profile for a body orbit.
// These entries are embedded in body spawn/save DTOs and likely drive hazard,
// gas composition, pressure, and temperature logic for navigation/encounter systems.
public class JsonAtmosphere
{
	// Layer id and altitude ceiling for this atmosphere band.
	public string strName { get; set; }

	public float fMaxAltitude { get; set; }

	public float fCO2 { get; set; }

	public float fCH4 { get; set; }

	public float fNH3 { get; set; }

	public float fN2 { get; set; }

	public float fH2SO4 { get; set; }

	public float fO2 { get; set; }

	public float fH2O { get; set; }

	public float fH2 { get; set; }

	public float fHe2 { get; set; }

	public float fTemp { get; set; }

	public float fMicrometeoroidChance { get; set; }

	// Sums all stored gas partial pressures into a total pressure value.
	public float GetTotalKPA()
	{
		return this.fCO2 + this.fCH4 + this.fNH3 + this.fN2 + this.fH2SO4 + this.fO2 + this.fH2O + this.fH2 + this.fHe2;
	}
}
