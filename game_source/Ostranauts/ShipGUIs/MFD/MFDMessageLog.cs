using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.Ships.Comms;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDMessageLog : MFDPage
	{
		public MFDMessageLog()
		{
			List<ShipMessage> messages = base.ShipUs.Comms.GetMessages(null);
			if (messages.Count > 0)
			{
				this.Title = "Showing Logs";
				this.Left = new List<string>
				{
					string.Empty,
					string.Empty,
					"LOG ENTRIES FOUND",
					string.Empty,
					"DISPLAYING",
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty
				};
				MonoSingleton<GUIMessageDisplay>.Instance.ShowLog(base.ShipUs.Comms.GetMessages(null));
			}
			else
			{
				this.Title = "No logs found";
				MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
			}
			base.UpdateDisplay();
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
					string.Empty,
					string.Empty,
					"RETURN TO",
					"MAIN MENU>"
				};
			}
		}
	}
}
