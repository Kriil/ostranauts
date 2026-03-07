using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.Comms;
using UnityEngine;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDComms : MFDPage
	{
		public MFDComms(string regId, bool isATCChannel, bool hail)
		{
			this._targetRegID = regId;
			this._isATCChannel = isATCChannel;
			Ship shipByRegID = CrewSim.system.GetShipByRegID(regId);
			CondOwner condOwner;
			if (shipByRegID.Comms.GetCaptain(out condOwner))
			{
				this._contact = condOwner.strName;
			}
			else
			{
				this._contact = ((!(condOwner == null)) ? (shipByRegID.strRegID + " ARS 2000 - Automated Response Service") : "Unknown");
			}
			string text = string.Empty;
			List<ShipMessage> messages;
			if (isATCChannel)
			{
				text = "<align=\"center\">----------- Listening to " + regId + " public ATC channel -------------</align>";
				this._contact = "ATC Regional Control - " + regId;
				messages = shipByRegID.Comms.GetMessages(null);
				this._availableIAs.Add(new Interaction
				{
					strTitle = "OPEN CHANNEL TO " + regId
				});
			}
			else
			{
				this._availableIAs = shipByRegID.Comms.GetAvailableInteractions(base.CoUs, null);
				ShipInfo shipInfo = ShipInfo.GetShipInfo(base.ShipUs, shipByRegID, GUIDockSys.DictGPM);
				text = ((!shipInfo.Known) ? string.Empty : ("Connected with " + this._contact + " of the " + shipByRegID.publicName));
				this._contact = shipInfo.strRegID;
				messages = base.ShipUs.Comms.GetMessages(regId);
			}
			if (MFDComms.OpenedShipComms != null)
			{
				MFDComms.OpenedShipComms.Invoke(this._targetRegID);
			}
			MonoSingleton<GUIMessageDisplay>.Instance.ShowPanel(text, messages);
			this.ShowOptions(false);
			if (hail)
			{
				this.Hail(messages, condOwner);
			}
		}

		public MFDComms(ShipMessage dto)
		{
			dto.Read = true;
			this._targetRegID = dto.SenderRegId;
			Ship shipByRegID = CrewSim.system.GetShipByRegID(dto.SenderRegId);
			if (shipByRegID == null)
			{
				return;
			}
			CondOwner condOwner;
			if (shipByRegID.Comms.GetCaptain(out condOwner))
			{
				this._contact = condOwner.strName;
			}
			else
			{
				this._contact = ((!(condOwner == null)) ? (shipByRegID.strRegID + " ARS 2000 - Automated Response Service") : "Unknown");
			}
			this._availableIAs = this.BuildInvIas(dto.Interaction, (!(dto.Interaction.objThem != base.CoUs)) ? dto.Interaction.objUs : dto.Interaction.objThem);
			MonoSingleton<GUIMessageDisplay>.Instance.ShowPanel(string.Empty, new List<ShipMessage>
			{
				dto
			});
			this.ShowOptions(false);
		}

		private bool Hail(List<ShipMessage> log, CondOwner captain)
		{
			if (log.Any((ShipMessage x) => x.Interaction != null && x.Interaction.strName.Contains("SHIPHail") && StarSystem.fEpoch - x.AvailableTime < 600.0))
			{
				return false;
			}
			Interaction interaction = DataHandler.GetInteraction("SHIPHailMaster", null, false);
			if (interaction == null)
			{
				return false;
			}
			interaction.objUs = base.CoUs;
			interaction.objThem = captain;
			interaction = Comms.GetDeepestVisibleReply(interaction);
			if (!interaction.Triggered(false, false, false))
			{
				return false;
			}
			ShipMessage mfdMessage = base.ShipUs.Comms.SendMessage(interaction);
			MonoSingleton<GUIMessageDisplay>.Instance.AddMessage(mfdMessage);
			return true;
		}

		private void ShowWaitingForResponse(string contact, Interaction ia)
		{
			Tuple<List<string>, List<string>> tuple = base.StringListToPageLayout(new List<Interaction>(), 0);
			this.Title = "CONNECTED WITH - " + contact;
			this.Left.Clear();
			this.Left.Add("Message sent");
			this.Left.Add(string.Empty);
			this.Left.Add((ia == null || ia.aInverse == null || ia.aInverse.Length <= 0) ? string.Empty : "Waiting for response");
			this.Right = tuple.Item2;
			base.UpdateDisplay();
		}

		private void ShowOptions(bool enableBack = false)
		{
			Tuple<List<string>, List<string>> tuple = base.StringListToPageLayout((from Ia in this._availableIAs
			select Ia.strTitle).ToList<string>(), this._currentSubPage, false);
			this.Title = "CONNECTED WITH - " + this._contact;
			this.Left = tuple.Item1;
			if (this._currentSubPage == 0 && this.Left.Count == 10 && this.Left[9] == string.Empty)
			{
				this.Left[9] = "<SHOW ON NAV MAP";
			}
			if (base.ShipUs.Comms.HasClearanceWithTarget(this._targetRegID))
			{
				this.Left.Add("CLEARANCE AVAILABLE");
				this.Left.Add("<DOCKING");
			}
			else if (enableBack)
			{
				this.Left.Add(string.Empty);
				this.Left.Add("<BACK");
			}
			this.Right = tuple.Item2;
			base.UpdateDisplay();
		}

		public override void OnUIRefresh(ShipMessage shipMessage)
		{
			if (shipMessage == null || shipMessage.SenderRegId == base.ShipUs.strRegID)
			{
				return;
			}
			if (this._isATCChannel && shipMessage.SenderRegId != this._targetRegID && shipMessage.ReceiverRegId != this._targetRegID)
			{
				return;
			}
			if (!this._isATCChannel && shipMessage.ReceiverRegId != base.ShipUs.strRegID)
			{
				return;
			}
			bool flag = false;
			if (shipMessage.ReceiverRegId == base.ShipUs.strRegID)
			{
				shipMessage.Read = true;
				Interaction interaction = shipMessage.Interaction;
				flag = interaction.strName.Contains("Hail");
				if (Comms.ContainsClearance(interaction.LootCTsThem))
				{
					MonoSingleton<GUIMessageDisplay>.Instance.HidePanelDelayed();
				}
				if (interaction.aInverse != null && interaction.aInverse.Length > 0)
				{
					this._availableIAs = this.BuildInvIas(interaction, interaction.objUs);
				}
				else if (!flag)
				{
					this._availableIAs.Clear();
				}
			}
			MonoSingleton<GUIMessageDisplay>.Instance.AddMessage(shipMessage);
			this.ShowOptions(!flag);
		}

		private void ApplyInteraction(int index)
		{
			if (this._availableIAs == null || index >= this._availableIAs.Count || this._availableIAs[index] == null)
			{
				return;
			}
			Interaction interaction = this._availableIAs[index];
			if (!interaction.Triggered(false, false, false))
			{
				Debug.LogWarning("Could not select comm reply " + interaction);
				return;
			}
			ShipMessage mfdMessage = base.ShipUs.Comms.SendMessage(interaction);
			this.ShowWaitingForResponse(this._targetRegID, interaction);
			this._availableIAs.Clear();
			MonoSingleton<GUIMessageDisplay>.Instance.AddMessage(mfdMessage);
		}

		private List<Interaction> BuildInvIas(Interaction reply, CondOwner coThem)
		{
			List<Interaction> list = new List<Interaction>();
			if (reply.aInverse == null)
			{
				return list;
			}
			foreach (string text in reply.aInverse)
			{
				if (text.Contains("|"))
				{
					int num = text.IndexOf("|");
					string text2 = text.Substring(num + 1, text.Length - 1 - num);
					Debug.LogWarning(text2);
					List<Ship> shipsBySubString = CrewSim.system.GetShipsBySubString(coThem.ship.strRegID + "|");
					if (!shipsBySubString.Any((Ship x) => x.strRegID == coThem.ship.strRegID))
					{
						shipsBySubString.Add(coThem.ship);
					}
					foreach (Ship ship in shipsBySubString)
					{
						CondOwner condOwner = null;
						ship.Comms.GetCaptain(out condOwner);
						if (condOwner != null)
						{
							Interaction interAction = this.GetInterAction(text2, condOwner);
							if (interAction != null)
							{
								interAction.strTitle = condOwner.ship.strRegID + " - " + interAction.strTitle;
								list.Add(interAction);
							}
						}
					}
				}
				else
				{
					Interaction interAction2 = this.GetInterAction(text, coThem);
					if (interAction2 != null)
					{
						list.Add(interAction2);
					}
				}
			}
			return list;
		}

		private Interaction GetInterAction(string inv, CondOwner coThem)
		{
			Interaction interaction = DataHandler.GetInteraction(inv, null, false);
			if (interaction != null)
			{
				interaction.objUs = base.CoUs;
				interaction.objThem = coThem;
				if (interaction.Triggered(false, false, false))
				{
					return interaction;
				}
			}
			return null;
		}

		public override MFDPage OnButtonDown(int btnIndex)
		{
			if (btnIndex == 4 && this._currentSubPage == 0)
			{
				GUIOrbitDraw.UpdateShipSelection.Invoke(this._targetRegID);
				CrewSim.SwitchUI("strGUIPrefabLeft");
			}
			else if ((btnIndex == 4 || btnIndex == 10) && this._availableIAs.Count > 6)
			{
				if (btnIndex == 4)
				{
					this._currentSubPage--;
				}
				else
				{
					this._currentSubPage++;
				}
				this._currentSubPage = Mathf.Clamp(this._currentSubPage, 0, Mathf.FloorToInt((float)this._availableIAs.Count / 8f));
				this.ShowOptions(false);
			}
			else
			{
				if (btnIndex == this._mainMenuButton)
				{
					MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
					return new MFDMainMenu();
				}
				if (btnIndex == 5)
				{
					if (base.ShipUs.Comms.HasClearanceWithTarget(this._targetRegID))
					{
						return new MFDDockInfo();
					}
					Ship shipByRegID = CrewSim.system.GetShipByRegID(this._targetRegID);
					this._availableIAs = shipByRegID.Comms.GetAvailableInteractions(base.CoUs, null);
					this.ShowOptions(false);
				}
				else
				{
					if (this._isATCChannel && btnIndex == 0)
					{
						GUIOrbitDraw.UpdateShipSelection.Invoke(this._targetRegID);
						return new MFDShipSelect(300000);
					}
					int num = base.PageSelectionToIndex(btnIndex, this._currentSubPage);
					if (num >= 0 && num < this._availableIAs.Count)
					{
						this.ApplyInteraction(base.PageSelectionToIndex(btnIndex, this._currentSubPage));
						CrewSim.Paused = false;
					}
				}
			}
			return this;
		}

		private int _currentSubPage;

		private List<Interaction> _availableIAs = new List<Interaction>();

		private readonly string _targetRegID;

		private string _contact;

		private bool _isATCChannel;

		public static UnityEventString OpenedShipComms = new UnityEventString();
	}
}
