using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.Ships.Comms;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDStatus : MFDPage
	{
		public MFDStatus()
		{
			this.BuildMenu();
		}

		protected override List<string> Right
		{
			get
			{
				return new List<string>
				{
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					"-----------------------------------------------------------",
					string.Empty,
					"RETURN TO",
					"MAIN MENU>"
				};
			}
		}

		private void BuildMenu()
		{
			Clearance clearance = base.ShipUs.Comms.Clearance;
			if (clearance != null)
			{
				MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
				this.Title = "DOCKING MESSAGE - " + clearance.TargetRegId;
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
				this.Title = "NO CLEARANCE";
				this.Left = new List<string>
				{
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					"-----------------------------------------------------------",
					string.Empty,
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
	}
}
