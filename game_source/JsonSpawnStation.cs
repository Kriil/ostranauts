using System;

// Spawn/template payload for stations or large static ships in a star system.
// These records are consumed by StarSystem during generation or save restore
// to place stations around a parent body and set station-specific rules.
public class JsonSpawnStation
{
	// `strName` is the internal id; `strPublicName` is the player-facing station name.
	public string strName { get; set; }

	public string strNameParent { get; set; }

	public string strPublicName { get; set; }

	// Likely references a ship/station template id from `data/ships`.
	public string strShipType { get; set; }

	public float fDegreesCW { get; set; }

	public float fEccentricity { get; set; }

	public double fOrbitalPeriodYears { get; set; }

	public float fRadiusKM { get; set; }

	public float fMassKG { get; set; }

	public float fRotationPeriodDays { get; set; }

	public double fPeriapsisAU { get; set; }

	public double fApoapsisAU { get; set; }

	public string strOrbitType { get; set; }

	// Region/fees/construction flags affect navigation, docking, or economy behavior.
	public bool bIsRegion { get; set; }

	public bool bIsNoFees { get; set; }

	public bool bIsUnderConstruction { get; set; }

	public int nConstructionProgress { get; set; }

	public bool bNoCollisions { get; set; }

	public string strParallax { get; set; }

	public float fDamageCap { get; set; }

	public string[] aFactions { get; set; }

	// Law/owner/classification fields likely map to faction or station governance data.
	public string strLaw { get; set; }

	public string strOwner { get; set; }

	public string strClassification { get; set; }

	public string[] aStartingConds { get; set; }

	public bool bDrawTrack { get; set; }

	// Parses the string form used in JSON into the runtime orbit enum.
	public JsonSpawnStation.OrbitType Orbit
	{
		get
		{
			if (string.IsNullOrEmpty(this.strOrbitType) || !Enum.IsDefined(typeof(JsonSpawnStation.OrbitType), this.strOrbitType))
			{
				return JsonSpawnStation.OrbitType.GROUND;
			}
			return (JsonSpawnStation.OrbitType)Enum.Parse(typeof(JsonSpawnStation.OrbitType), this.strOrbitType);
		}
	}

	// Parses the string form into the ship classification enum used by runtime logic.
	public Ship.TypeClassification Classification
	{
		get
		{
			if (string.IsNullOrEmpty(this.strClassification) || !Enum.IsDefined(typeof(Ship.TypeClassification), this.strClassification))
			{
				return Ship.TypeClassification.None;
			}
			return (Ship.TypeClassification)Enum.Parse(typeof(Ship.TypeClassification), this.strClassification);
		}
	}

	public JsonSpawnStation Clone()
	{
		return new JsonSpawnStation
		{
			strName = this.strName,
			strNameParent = this.strNameParent,
			strPublicName = this.strPublicName,
			strShipType = this.strShipType,
			fDegreesCW = this.fDegreesCW,
			fEccentricity = this.fEccentricity,
			fOrbitalPeriodYears = this.fOrbitalPeriodYears,
			fRadiusKM = this.fRadiusKM,
			fMassKG = this.fMassKG,
			fRotationPeriodDays = this.fRotationPeriodDays,
			fPeriapsisAU = this.fPeriapsisAU,
			fApoapsisAU = this.fApoapsisAU,
			strOrbitType = this.strOrbitType,
			bIsRegion = this.bIsRegion,
			bIsNoFees = this.bIsNoFees,
			bIsUnderConstruction = this.bIsUnderConstruction,
			nConstructionProgress = this.nConstructionProgress,
			bNoCollisions = this.bNoCollisions,
			strParallax = this.strParallax,
			fDamageCap = this.fDamageCap,
			aFactions = this.aFactions,
			strLaw = this.strLaw,
			strOwner = this.strOwner,
			strClassification = this.strClassification,
			bDrawTrack = this.bDrawTrack
		};
	}

	public override string ToString()
	{
		return this.strName;
	}

	public enum OrbitType
	{
		GROUND,
		ORBIT,
		GEO,
		EX
	}
}
