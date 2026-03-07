using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Tutorials;
using Ostranauts.Objectives;
using Ostranauts.Ships.AIPilots;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Ships.Comms
{
	public class Comms
	{
		public Comms(Ship ship, JsonCommData saveData = null)
		{
			this._ship = ship;
			StarSystem.OnNewShipCommsMessage.AddListener(new UnityAction<ShipMessage>(this.ReceiveMessage));
			this.SetData(saveData);
		}

		public JsonCommData GetJson()
		{
			JsonCommData jsonCommData = new JsonCommData();
			if (this.Clearance != null)
			{
				jsonCommData.strClearanceTargetRegId = this.Clearance.TargetRegId;
				jsonCommData.dClearanceIssueTimestamp = this.Clearance.IssueTimestamp;
				jsonCommData.strClearanceDockID = this.Clearance.DockID;
				jsonCommData.strClearanceSquak = this.Clearance.Squak;
				jsonCommData.strClearanceType = this.Clearance.ClearanceType;
				jsonCommData.bClearanceSquawkID = this.Clearance.SquawkID;
			}
			jsonCommData.dClearanceRequestTime = this._clearanceRequestTime;
			if (this._messageLog.Count > 0)
			{
				List<JsonShipMessage> list = new List<JsonShipMessage>();
				foreach (ShipMessage shipMessage in this._messageLog)
				{
					JsonShipMessage json = shipMessage.GetJson();
					if (json != null && json.iaMessageInteraction != null)
					{
						list.Add(json);
					}
					else
					{
						Debug.LogWarning("Could not save COMM-MSG: " + shipMessage.Interaction.strName);
					}
				}
				jsonCommData.aMessages = list.ToArray();
			}
			return jsonCommData;
		}

		public void Destroy()
		{
			this._ship = null;
			this._messageLog = null;
			this.Clearance = null;
			StarSystem.OnNewShipCommsMessage.RemoveListener(new UnityAction<ShipMessage>(this.ReceiveMessage));
		}

		private void SetData(JsonCommData saveData)
		{
			if (saveData == null)
			{
				return;
			}
			this._clearanceRequestTime = saveData.dClearanceRequestTime;
			if (!string.IsNullOrEmpty(saveData.strClearanceTargetRegId))
			{
				this.Clearance = new Clearance
				{
					TargetRegId = saveData.strClearanceTargetRegId,
					IssueTimestamp = saveData.dClearanceIssueTimestamp,
					DockID = saveData.strClearanceDockID,
					Squak = saveData.strClearanceSquak,
					ClearanceType = saveData.strClearanceType,
					SquawkID = saveData.bClearanceSquawkID
				};
			}
			this._messageLog.Clear();
			if (saveData.aMessages != null)
			{
				foreach (JsonShipMessage jsonShipMessage in saveData.aMessages)
				{
					ShipMessage shipMessage = new ShipMessage
					{
						SenderRegId = jsonShipMessage.strSenderRegId,
						ReceiverRegId = jsonShipMessage.strRecieverRegId,
						AvailableTime = jsonShipMessage.dAvailableTime,
						Read = jsonShipMessage.bRead,
						MessageText = jsonShipMessage.strMessageText
					};
					if (jsonShipMessage.iaMessageInteraction != null)
					{
						shipMessage.Interaction = DataHandler.GetInteraction(jsonShipMessage.iaMessageInteraction.strName, jsonShipMessage.iaMessageInteraction, false);
					}
					this._messageLog.Add(shipMessage);
				}
			}
		}

		public bool GetCaptain(out CondOwner captain)
		{
			captain = this._ship.ShipCO;
			List<CondOwner> people = this._ship.GetPeople(false);
			if (this._ship.IsStation(false) || this._ship.IsStationHidden(false) || this._ship.IsDerelict() || people.Count == 0)
			{
				return false;
			}
			string shipOwner = CrewSim.system.GetShipOwner(this._ship.strRegID);
			List<JsonFaction> shipFactions = this._ship.GetShipFactions();
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsSocialCore");
			using (List<CondOwner>.Enumerator enumerator = people.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CondOwner crew = enumerator.Current;
					if (condTrigger.Triggered(crew, null, true))
					{
						if (crew.strName == shipOwner || crew.OwnsShip(this._ship.strRegID))
						{
							captain = crew;
							return true;
						}
						if (shipFactions.Any((JsonFaction item) => crew.GetAllFactions().Contains(item.strName)))
						{
							captain = crew;
						}
					}
				}
			}
			return captain != this._ship.ShipCO;
		}

		public List<Interaction> GetAvailableInteractions(CondOwner caller, CondOwner captain3rdShip = null)
		{
			List<Interaction> list = new List<Interaction>();
			CondOwner objThem = null;
			string strName;
			if (this.GetCaptain(out objThem))
			{
				strName = "TXTShipStandardInteractions";
			}
			else
			{
				strName = ((!this._ship.IsStation(false)) ? "TXTShipStandardInteractionsNoPilot" : "TXTStationInteractions");
			}
			List<string> lootNames = DataHandler.GetLoot(strName).GetLootNames(null, false, null);
			foreach (string strName2 in lootNames)
			{
				Interaction interaction = DataHandler.GetInteraction(strName2, null, false);
				if (interaction != null)
				{
					interaction.objUs = caller;
					interaction.objThem = objThem;
					interaction.obj3rd = captain3rdShip;
					if (interaction.Triggered(false, false, false))
					{
						if (interaction.nLogging == Interaction.Logging.NONE)
						{
							interaction = Comms.GetDeepestVisibleReply(interaction);
						}
						list.Add(interaction);
					}
				}
			}
			return list;
		}

		public ShipMessage SendMessage(Interaction ia)
		{
			ShipMessage shipMessage = new ShipMessage
			{
				SenderRegId = this._ship.strRegID,
				ReceiverRegId = ia.objThem.ship.strRegID,
				AvailableTime = StarSystem.fEpoch + (double)this.GetDeliveryTime(ia.objThem.ship),
				Interaction = ia,
				MessageText = this.ParseMessageText(ia)
			};
			this.ApplyLoot(ia.LootCTsUs, ia.objUs, ia.objThem.ship.strRegID);
			this._messageLog.Add(shipMessage);
			this.TrimMessageLog(ia.objThem.ship.strRegID);
			CrewSim.system.SendMessage(shipMessage);
			return shipMessage;
		}

		private string ParseMessageText(Interaction ia)
		{
			return GrammarUtils.GenerateDescription(ia);
		}

		public bool SendMessage(string ia, string targetRegID, string thirdRegId = null)
		{
			if (ia == null)
			{
				return false;
			}
			CondOwner caller = null;
			this.GetCaptain(out caller);
			Ship shipByRegID = CrewSim.system.GetShipByRegID(targetRegID);
			if (shipByRegID == null)
			{
				return false;
			}
			CondOwner captain3rdShip = null;
			if (thirdRegId != null)
			{
				Ship shipByRegID2 = CrewSim.system.GetShipByRegID(thirdRegId);
				if (shipByRegID2 != null)
				{
					shipByRegID2.Comms.GetCaptain(out captain3rdShip);
				}
			}
			List<Interaction> availableInteractions = shipByRegID.Comms.GetAvailableInteractions(caller, captain3rdShip);
			foreach (Interaction interaction in availableInteractions)
			{
				if (!(interaction.strName != ia))
				{
					this.SendMessage(interaction);
					return true;
				}
			}
			return false;
		}

		public List<ShipMessage> GetMessages(string regID = null)
		{
			if (string.IsNullOrEmpty(regID))
			{
				return this._messageLog;
			}
			List<ShipMessage> list = new List<ShipMessage>();
			foreach (ShipMessage shipMessage in this._messageLog)
			{
				if (shipMessage.ReceiverRegId == regID || shipMessage.SenderRegId == regID)
				{
					list.Add(shipMessage);
				}
			}
			return list;
		}

		public List<ShipMessage> GetUnreadMessages()
		{
			List<ShipMessage> list = new List<ShipMessage>();
			foreach (ShipMessage shipMessage in this._messageLog)
			{
				if (!shipMessage.Read && !(shipMessage.ReceiverRegId != this._ship.strRegID))
				{
					list.Add(shipMessage);
				}
			}
			return list;
		}

		public bool HasUnreadMessage()
		{
			foreach (ShipMessage shipMessage in this._messageLog)
			{
				if (!shipMessage.Read && !(shipMessage.ReceiverRegId != this._ship.strRegID))
				{
					return true;
				}
			}
			return false;
		}

		private void TrimMessageLog(string otherParticipant)
		{
			List<ShipMessage> list = this.GetMessages(otherParticipant);
			if (list.Count <= 20)
			{
				return;
			}
			list = (from x in list
			orderby x.AvailableTime
			select x).ToList<ShipMessage>();
			for (int i = list.Count<ShipMessage>() - 21; i >= 0; i--)
			{
				this._messageLog.Remove(list[i]);
			}
		}

		private void ReceiveMessage(ShipMessage dto)
		{
			if (dto == null || dto.ReceiverRegId != this._ship.strRegID)
			{
				return;
			}
			this._messageLog.Add(dto);
			this.TrimMessageLog(dto.SenderRegId);
			if (dto.Interaction != null && dto.Interaction.Triggered(false, false, false))
			{
				dto.Interaction.ApplyEffects(null, false);
				this.ApplyLoot(dto.Interaction.LootCTsThem, dto.Interaction.objThem, dto.SenderRegId);
			}
			if (!this._ship.IsPlayerShip() || this._ship.IsStation(false))
			{
				this.AIReply(dto);
			}
			else
			{
				this.NotifyPlayer();
			}
		}

		private void ApplyLoot(Loot loot, CondOwner co, string regIDThem)
		{
			if (loot == null || co == null || loot.GetLootNames(null, false, null).Count == 0)
			{
				return;
			}
			List<string> lootNames = loot.GetLootNames(null, false, null);
			if (lootNames.Count == 0 || lootNames.FirstOrDefault<string>() == null)
			{
				return;
			}
			if (Comms.ContainsClearance(loot))
			{
				this.Clearance = new Clearance(this._ship.IsDocked())
				{
					TargetRegId = regIDThem,
					IssueTimestamp = StarSystem.fEpoch
				};
				if (this.Clearance.ClearanceType == "DOCK" && this._ship.LoadState >= Ship.Loaded.Edit)
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(regIDThem);
					if (shipByRegID.IsStation(false))
					{
						AudioManager.am.SuggestMusic("Docking", false);
					}
				}
			}
			else
			{
				AIShip aiship = AIShipManager.GetAIShipByRegID(co.ship.strRegID);
				if (aiship == null && this._ship.IsStation(false))
				{
					aiship = AIShipManager.AddAIToShip(this._ship, AIType.Station, AIShipManager.strATCLast, null);
				}
				string[] commandSaveData = new string[]
				{
					regIDThem
				};
				if (lootNames.Count > 1)
				{
					commandSaveData = lootNames.GetRange(1, lootNames.Count - 1).ToArray();
				}
				if (aiship != null)
				{
					aiship.AddCommandLoot(lootNames[0], commandSaveData);
				}
			}
		}

		private void AIReply(ShipMessage dto)
		{
			if (dto == null || dto.Interaction == null)
			{
				return;
			}
			dto.Read = true;
			Interaction interaction = Comms.GetDeepestVisibleReply(dto.Interaction);
			if (interaction == dto.Interaction)
			{
				return;
			}
			interaction.objUs.AddRecentlyTried(interaction.strName);
			if (this._ship.IsStation(false))
			{
				interaction = this.CheckDockingFees(dto, interaction);
			}
			this.SendMessage(interaction);
		}

		public static Interaction GetDeepestVisibleReply(Interaction topIa)
		{
			Interaction interaction = topIa;
			int i;
			for (i = 100; i > 0; i--)
			{
				Interaction reply = interaction.GetReply();
				if (reply == null || !reply.Triggered(false, false, false))
				{
					break;
				}
				interaction = reply;
				if (reply.nLogging != Interaction.Logging.NONE)
				{
					break;
				}
			}
			if (i <= 0)
			{
				Debug.LogWarning("Ship Comms couldn't find a visible reply. Check logging values");
			}
			return interaction;
		}

		private Interaction CheckDockingFees(ShipMessage dto, Interaction response)
		{
			if (response == null || !Comms.ContainsClearance(response.LootCTsThem) || !this._ship.IsDockedWith(dto.SenderRegId))
			{
				return response;
			}
			if (!Ledger.HasUnpaidDockingFees(this._ship.strRegID, dto.Interaction.objUs))
			{
				return response;
			}
			Interaction interaction = DataHandler.GetInteraction("SHIPUndockDenyUnpaidFee", null, false);
			interaction.objUs = response.objUs;
			interaction.objThem = response.objThem;
			response = interaction;
			this.CheckDockingFeeTutorial(dto.Interaction.objUs);
			return response;
		}

		private void CheckDockingFeeTutorial(CondOwner coPlayer)
		{
			if (coPlayer == null || coPlayer != CrewSim.coPlayer || coPlayer.HasCond("TutorialDockingFeesStart"))
			{
				return;
			}
			CrewSimTut.BeginTutorialBeat<UnpaidDockingFees>();
			coPlayer.AddCondAmount("TutorialDockingFeesStart", 1.0, 0.0, 0f);
		}

		private float GetDeliveryTime(Ship them)
		{
			float collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(this._ship, them);
			float num = collisionDistanceAU * 499f;
			return (num >= 1f) ? num : 1f;
		}

		private void NotifyPlayer()
		{
			if (this._ship.LoadState <= Ship.Loaded.Shallow || this._ship.NavCount == 0)
			{
				return;
			}
			AudioManager.am.PlayAudioEmitter("ShipIncomingMessage01", false, true);
			if (!this._ship.NavPlayerManned)
			{
				CondOwner condOwner = this._ship.aNavs.FirstOrDefault<CondOwner>();
				if (condOwner == null)
				{
					return;
				}
				AlarmObjective objective = new AlarmObjective(AlarmType.nav_new_message, condOwner, "New NAV Message Received");
				MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
			}
		}

		public bool HasClearanceWithTarget(string targetRegID)
		{
			return this.Clearance != null && this.Clearance.TargetRegId == targetRegID;
		}

		public bool AIGetClearance(Ship otherShip)
		{
			if (otherShip.strRegID != AIShipManager.strATCLast)
			{
				this._clearanceRequestTime = 0.0;
				return true;
			}
			if (this.Clearance != null)
			{
				bool flag = this.Clearance.TargetRegId == otherShip.strRegID;
				this.Clearance = null;
				this._clearanceRequestTime = 0.0;
				if (flag)
				{
					return true;
				}
			}
			if (StarSystem.fEpoch - this._clearanceRequestTime < 4.0)
			{
				return false;
			}
			CondOwner caller = null;
			this.GetCaptain(out caller);
			List<Interaction> availableInteractions = otherShip.Comms.GetAvailableInteractions(caller, null);
			bool flag2 = this._ship.IsDockedWith(otherShip);
			foreach (Interaction interaction in availableInteractions)
			{
				if (!flag2 || !(interaction.strName != "SHIPUnDock"))
				{
					if (flag2 || interaction.strName.IndexOf("SHIPDock") == 0)
					{
						this.SendMessage(interaction);
						this._clearanceRequestTime = StarSystem.fEpoch;
						return false;
					}
				}
			}
			return false;
		}

		public void DebugCreateClearance(string regIdOTherShip = null)
		{
			if (string.IsNullOrEmpty(regIdOTherShip))
			{
				Ship ship = this._ship.GetAllDockedShips().FirstOrDefault<Ship>();
				if (ship != null)
				{
					regIdOTherShip = ship.strRegID;
				}
			}
			this.Clearance = new Clearance
			{
				TargetRegId = regIdOTherShip,
				IssueTimestamp = StarSystem.fEpoch,
				DockID = "000",
				Squak = "0",
				ClearanceType = "Q",
				SquawkID = true
			};
		}

		public void AIAnnounceEvasion()
		{
			if (StarSystem.fEpoch - Comms._evasionAnnouncmentTime < 60.0)
			{
				return;
			}
			Comms._evasionAnnouncmentTime = StarSystem.fEpoch;
			this.SendMessage("SHIPCorrectingCourse", AIShipManager.strATCLast, null);
		}

		public static bool ContainsClearance(Loot iaLoot)
		{
			if (iaLoot == null)
			{
				return false;
			}
			List<string> lootNames = iaLoot.GetLootNames(null, false, null);
			return lootNames.Count != 0 && lootNames.FirstOrDefault<string>() != null && lootNames[0].Contains("DockingClearance");
		}

		private Ship _ship;

		private List<ShipMessage> _messageLog = new List<ShipMessage>();

		public Clearance Clearance;

		private double _clearanceRequestTime;

		private static double _evasionAnnouncmentTime;
	}
}
