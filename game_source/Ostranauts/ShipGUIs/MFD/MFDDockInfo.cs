using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.Ships.Comms;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDDockInfo : MFDPage
	{
		public MFDDockInfo()
		{
			this.BuildMenu();
		}

		private void BuildMenu()
		{
			MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
			List<Ship> allDockedShips = CrewSim.GetSelectedCrew().ship.GetAllDockedShips();
			this._dockedShip = allDockedShips.FirstOrDefault<Ship>();
			this.Title = ((this._dockedShip == null) ? "DOCK INFO" : ("DOCKED WITH " + this._dockedShip.strRegID.ToUpper()));
			this.BuildClearance();
			this.BuildDockInfo();
			base.UpdateDisplay();
		}

		private void BuildDockInfo()
		{
			if (this._dockedShip != null)
			{
				this.Right = new List<string>
				{
					"REG ID",
					this._dockedShip.strRegID,
					"NAME",
					this._dockedShip.publicName,
					"RATING CODE",
					this._dockedShip.GetRatingString(),
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					"RETURN TO",
					"MAIN MENU>"
				};
			}
			else
			{
				this.Right = new List<string>
				{
					string.Empty,
					string.Empty,
					string.Empty,
					"NO DOCKED VESSEL",
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					"RETURN TO",
					"MAIN MENU>"
				};
			}
		}

		private void BuildClearance()
		{
			Clearance clearance = base.ShipUs.Comms.Clearance;
			if (clearance != null)
			{
				string str = string.Empty;
				if (clearance.SquawkID)
				{
					str = "-IDENT";
				}
				this.Left = new List<string>
				{
					"CLEARED TO",
					clearance.ClearanceType + " " + clearance.TargetRegId,
					"VIA",
					"PILOT",
					"DOCK",
					clearance.DockID,
					"SQUAWK" + str,
					clearance.Squak,
					"-----------------------------------------------------------",
					string.Empty,
					string.Empty,
					string.Empty
				};
			}
			else
			{
				this.Left = new List<string>
				{
					string.Empty,
					string.Empty,
					string.Empty,
					"NO CLEARANCE",
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					"-----------------------------------------------------------",
					(this._dockedShip == null) ? string.Empty : "<REQUEST CLEARANCE",
					string.Empty,
					string.Empty
				};
			}
			base.UpdateDisplay();
		}

		public override void OnUIRefresh(ShipMessage shipMessage)
		{
			this.BuildMenu();
		}

		public override MFDPage OnButtonDown(int btnIndex)
		{
			if (btnIndex == this._mainMenuButton)
			{
				return new MFDMainMenu();
			}
			if (btnIndex == 4 && this._dockedShip != null)
			{
				return new MFDComms(this._dockedShip.strRegID, false, false);
			}
			return this;
		}

		private Ship _dockedShip;
	}
}
