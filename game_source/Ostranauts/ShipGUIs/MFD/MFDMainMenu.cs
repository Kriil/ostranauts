using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.Ships.Comms;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDMainMenu : MFDPage
	{
		public MFDMainMenu()
		{
			MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
			this.RebuildMenu();
		}

		protected override string Title
		{
			get
			{
				return "MAIN MENU";
			}
		}

		protected override List<string> Left
		{
			get
			{
				return this._left;
			}
		}

		protected override List<string> Right
		{
			get
			{
				return this._right;
			}
		}

		private void RebuildMenu()
		{
			this._left.Clear();
			this._left.Add("ATC CHANNEL: " + CollisionManager.strATCClosest);
			this._left.AddRange(this._leftDefault);
			Ship ship = null;
			if (base.ShipUs != null)
			{
				base.ShipUs.GetAllDockedShips().FirstOrDefault<Ship>();
			}
			this._left.Add((ship == null) ? string.Empty : ("DOCKED WITH: " + ship.strRegID));
			this._left.AddRange(new string[]
			{
				"<DOCK INFO",
				string.Empty
			});
			if (base.ShipUs != null && base.ShipUs.Comms.HasUnreadMessage())
			{
				this._left.Add("<UNREAD MESSAGES");
			}
			this._right.Clear();
			this._right.AddRange(this._rightDefault);
			base.UpdateDisplay();
		}

		public override void OnUIRefresh(ShipMessage shipMessage)
		{
			this.RebuildMenu();
		}

		public override MFDPage OnButtonDown(int btnIndex)
		{
			if (btnIndex == 0)
			{
				return new MFDComms(CollisionManager.strATCClosest, true, false);
			}
			if (btnIndex == 1)
			{
				return new MFDMessageLog();
			}
			if (btnIndex == 2)
			{
				return new MFDDockInfo();
			}
			if (btnIndex == 3 && base.ShipUs != null && base.ShipUs.Comms.HasUnreadMessage())
			{
				return new MFDInbox();
			}
			if (btnIndex == 6)
			{
				return new MFDShipSelect(300000);
			}
			return this;
		}

		private readonly List<string> _leftDefault = new List<string>
		{
			"<LOCAL CHANNEL",
			"-----------------------------------------------------------",
			"<MESSAGE LOG"
		};

		private List<string> _left = new List<string>();

		private readonly List<string> _rightDefault = new List<string>
		{
			string.Empty,
			"HAIL SHIP>",
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty
		};

		private List<string> _right = new List<string>();

		private string _orbitdrawShipSelection;
	}
}
