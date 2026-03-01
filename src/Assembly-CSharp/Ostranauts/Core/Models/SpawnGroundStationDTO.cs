using System;

namespace Ostranauts.Core.Models
{
	public class SpawnGroundStationDTO : SpawnStationDTO
	{
		public SpawnGroundStationDTO(string strName, float fW, float fR, float fM, string strShipType, BodyOrbit boParent)
		{
			this.JsonSpawnStation = new JsonSpawnStation
			{
				strName = strName,
				strShipType = strShipType,
				fDegreesCW = fW,
				fRadiusKM = fR,
				fMassKG = fM,
				strOwner = "UNREGISTERED",
				strClassification = Ship.TypeClassification.GroundStation.ToString()
			};
			this.BoParent = boParent;
		}

		public SpawnGroundStationDTO(JsonSpawnStation jss, BodyOrbit bo) : base(jss, bo)
		{
			if (this.JsonSpawnStation.Classification == Ship.TypeClassification.None)
			{
				this.JsonSpawnStation.strClassification = Ship.TypeClassification.GroundStation.ToString();
			}
		}
	}
}
