using System;

namespace Ostranauts.Core.Models
{
	public class SpawnStationDTO
	{
		public SpawnStationDTO()
		{
		}

		public SpawnStationDTO(JsonSpawnStation jss, BodyOrbit bo)
		{
			this.JsonSpawnStation = jss.Clone();
			this.BoParent = bo;
		}

		public SpawnStationDTO(string strName, float fPerihelion, float fAphelion, float fW, float fEccentricity, float fOrbitalPeriod, float fR, float fM, string strShipType, BodyOrbit boParent, int constructionProgress = 100, Ship.TypeClassification classification = Ship.TypeClassification.OrbitalStation)
		{
			this.JsonSpawnStation = new JsonSpawnStation
			{
				strName = strName,
				strShipType = strShipType,
				fDegreesCW = fW,
				fEccentricity = fEccentricity,
				fOrbitalPeriodYears = (double)fOrbitalPeriod,
				fRadiusKM = fR,
				fMassKG = fM,
				fPeriapsisAU = (double)fPerihelion,
				fApoapsisAU = (double)fAphelion,
				nConstructionProgress = constructionProgress,
				strOwner = "UNREGISTERED",
				strClassification = classification.ToString()
			};
			this.BoParent = boParent;
		}

		public JsonSpawnStation JsonSpawnStation;

		public BodyOrbit BoParent;
	}
}
