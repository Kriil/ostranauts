using System;

namespace Ostranauts.Core.Models
{
	public class SpawnGeoStationDTO : SpawnStationDTO
	{
		public SpawnGeoStationDTO(string strName, float fW, float fEccentricity, float fR, float fM, string strShipType, BodyOrbit boParent)
		{
			this.JsonSpawnStation = new JsonSpawnStation
			{
				strName = strName,
				strShipType = strShipType,
				fDegreesCW = fW,
				fRadiusKM = fR,
				fMassKG = fM,
				fEccentricity = fEccentricity,
				strOwner = "UNREGISTERED",
				strClassification = Ship.TypeClassification.OrbitalStation.ToString()
			};
			this.BoParent = boParent;
		}

		public SpawnGeoStationDTO(JsonSpawnStation jss, BodyOrbit bo) : base(jss, bo)
		{
			if (this.JsonSpawnStation.Classification == Ship.TypeClassification.None)
			{
				this.JsonSpawnStation.strClassification = Ship.TypeClassification.OrbitalStation.ToString();
			}
		}
	}
}
