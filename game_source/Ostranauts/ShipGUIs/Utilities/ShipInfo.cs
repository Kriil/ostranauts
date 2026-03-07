using System;
using System.Collections.Generic;

namespace Ostranauts.ShipGUIs.Utilities
{
	public class ShipInfo
	{
		public ShipInfo(Ship ship, bool bForceReveal = false)
		{
			if (ship == null)
			{
				return;
			}
			this._strRegID = ship.strRegID;
			if (bForceReveal || ship.IsLocalAuthority || (!ship.IsDerelict() && !ship.IsUnderConstruction))
			{
				if (bForceReveal)
				{
					this.strRegID = ship.strRegID;
					this.make = ship.make;
					this.model = ship.model;
					this.year = ship.year;
					this.origin = ship.origin;
					this.designation = ship.designation;
					this.publicName = ship.publicName;
					this.dimensions = ship.dimensions;
				}
				else
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(ship.strXPDR);
					if (shipByRegID != null)
					{
						this.strRegID = shipByRegID.strRegID;
						this.make = shipByRegID.make;
						this.model = shipByRegID.model;
						this.year = shipByRegID.year;
						this.origin = shipByRegID.origin;
						this.designation = shipByRegID.designation;
						this.publicName = shipByRegID.publicName;
						this.dimensions = shipByRegID.dimensions;
					}
				}
				if (this.strRegID == null)
				{
					this.strRegID = "?";
				}
			}
		}

		public ShipInfo(string strID, string strGPMInfo)
		{
			if (strID == null || strGPMInfo == null)
			{
				return;
			}
			this._strRegID = strID;
			string[] array = strGPMInfo.Split(new char[]
			{
				';'
			});
			if (array.Length < 8)
			{
				return;
			}
			this.strRegID = array[0];
			this.make = array[1];
			this.model = array[2];
			this.year = array[3];
			this.origin = array[4];
			this.designation = array[5];
			this.publicName = array[6];
			this.dimensions = array[7];
		}

		public string GetGPMString()
		{
			string str = this.strRegID + ";";
			str = str + this.make + ";";
			str = str + this.model + ";";
			str = str + this.year + ";";
			str = str + this.origin + ";";
			str = str + this.designation + ";";
			str = str + this.publicName + ";";
			return str + this.dimensions;
		}

		public bool Known
		{
			get
			{
				return !string.IsNullOrEmpty(this.strRegID) && this.strRegID != "?";
			}
		}

		public static void SetShipInfo(ShipInfo si, Dictionary<string, string> dict)
		{
			if (si == null || dict == null)
			{
				return;
			}
			dict["Contact_" + si._strRegID] = si.GetGPMString();
		}

		public static ShipInfo GetShipInfo(Ship nsShip, Ship ship, Dictionary<string, string> dict)
		{
			if (ship == null || dict == null)
			{
				return null;
			}
			string strGPMInfo = null;
			if (dict.TryGetValue("Contact_" + ship.strRegID, out strGPMInfo))
			{
				return new ShipInfo(ship.strRegID, strGPMInfo);
			}
			bool bForceReveal = nsShip == ship;
			return new ShipInfo(ship, bForceReveal);
		}

		public string _strRegID;

		public string strRegID = "?";

		public string make = "?";

		public string model = "?";

		public string year = "?";

		public string origin = "?";

		public string designation = "?";

		public string publicName = "?";

		public string dimensions = "?";

		public bool isTutorialDerelict;

		public const string UNKNOWN = "?";
	}
}
