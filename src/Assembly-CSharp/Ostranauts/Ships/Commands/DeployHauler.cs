using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class DeployHauler : BaseCommand
	{
		public DeployHauler(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			this._pilot = pilot;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Deploying Hauler";
			}
		}

		public override string[] SaveData
		{
			get
			{
				string[] result;
				if (this._haulerTarget != null)
				{
					(result = new string[1])[0] = this._haulerTarget.strRegID;
				}
				else
				{
					result = null;
				}
				return result;
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				this._haulerTarget = CrewSim.system.GetShipByRegID(value.FirstOrDefault<string>());
			}
		}

		public override CommandCode RunCommand()
		{
			if (this._haulerTarget == null)
			{
				return CommandCode.Skipped;
			}
			foreach (KeyValuePair<string, List<AIShip>> keyValuePair in AIShipManager.dictAIs)
			{
				if (!(keyValuePair.Key != AIShipManager.strATCLast))
				{
					foreach (AIShip aiship in keyValuePair.Value)
					{
						if (aiship.AIType == AIType.HaulerRetriever && aiship.Ship.shipScanTarget == this._haulerTarget)
						{
							return CommandCode.Skipped;
						}
					}
				}
			}
			if (!AIShip.IsDockingAreaClear(AIShipManager.ShipATCLast, 1.0026880659097515E-07))
			{
				return CommandCode.Ongoing;
			}
			AIShip aiship2 = AIShipManager.SpawnAI(AIType.HaulerRetriever, AIShipManager.strATCLast);
			if (aiship2 != null)
			{
				if (AIShipManager.BingoFuelCheck(aiship2.Ship, this._haulerTarget, base.ShipUs, this._pilot.MaxSpeed(null)))
				{
					base.ShipUs.Comms.SendMessage("SHIPHaulerTargetNotReachable", this._haulerTarget.strRegID, null);
					CrewSim.DockAndDespawn(aiship2.Ship, base.ShipUs, null);
					return CommandCode.Cancelled;
				}
				base.ShipUs.Comms.SendMessage("SHIPSetCourseForPickup", aiship2.Ship.strRegID, this._haulerTarget.strRegID);
			}
			return CommandCode.Finished;
		}

		private IAICharacter _pilot;

		private Ship _haulerTarget;
	}
}
