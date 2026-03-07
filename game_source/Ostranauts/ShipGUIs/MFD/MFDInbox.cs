using System;
using System.Collections.Generic;
using Ostranauts.Ships.Comms;
using UnityEngine;

namespace Ostranauts.ShipGUIs.MFD
{
	public class MFDInbox : MFDPage
	{
		public MFDInbox()
		{
			this._unreadMessages = base.ShipUs.Comms.GetUnreadMessages();
			foreach (ShipMessage shipMessage in this._unreadMessages)
			{
				if (!this._inboxContacts.Contains(shipMessage.SenderRegId))
				{
					this._inboxContacts.Add(shipMessage.SenderRegId);
				}
			}
			base.PopulateMFD(this._inboxContacts, 0, null);
		}

		protected override string Title
		{
			get
			{
				return "Recieved Messages:";
			}
		}

		public override MFDPage OnButtonDown(int btnIndex)
		{
			if ((btnIndex == 4 || btnIndex == 10) && this._inboxContacts.Count > 8)
			{
				if (btnIndex == 4)
				{
					this._currentsubPage--;
				}
				else
				{
					this._currentsubPage++;
				}
				this._currentsubPage = Mathf.Clamp(this._currentsubPage, 0, Mathf.FloorToInt((float)this._inboxContacts.Count / 8f));
				base.PopulateMFD(this._inboxContacts, this._currentsubPage, null);
				return this;
			}
			if (btnIndex == this._mainMenuButton)
			{
				return new MFDMainMenu();
			}
			string messagesByContact = this._inboxContacts[base.PageSelectionToIndex(btnIndex, this._currentsubPage)];
			return new MFDComms(this._unreadMessages.Find((ShipMessage x) => x.SenderRegId == messagesByContact));
		}

		private readonly List<string> _inboxContacts = new List<string>();

		private readonly List<ShipMessage> _unreadMessages;

		private int _currentsubPage;
	}
}
