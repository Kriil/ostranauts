using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;
using UnityEngine;

namespace Ostranauts.Ships.AIPilots
{
	public class ScavPilot : IAICharacter
	{
		public ScavPilot(string strStationID = null, Ship ship = null)
		{
			this.ShipUs = (ship ?? this.CreateAIShip(strStationID));
			if (this.ShipUs == null)
			{
				return;
			}
			this.Commands = new List<ICommand>
			{
				new Undock(this),
				new FlyTo(this),
				new Dock(this),
				new Hold(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.Scav;
			}
		}

		public double MaxSpeed(bool? inAtmo = null)
		{
			return AIShip.CalculateMaxSpeed(this.ShipUs, inAtmo);
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyTo || lastActiveCommand is Dock)
			{
				if (this.ShipUs.objSS.HasNavData())
				{
					return lastActiveCommand;
				}
				ICommand command = this.InstaDock();
				if (command != null)
				{
					return command;
				}
			}
			else if (lastActiveCommand is Undock)
			{
				if (this.ShipUs.shipUndock != null)
				{
					this.ShipUs.shipScanTarget = this.ShipUs.shipUndock;
					this.ShipUs.shipUndock = null;
				}
				else
				{
					Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosx);
					this.ShipUs.shipScanTarget = nearestStationRegional;
				}
				ICommand command2 = this.InstaDock();
				if (command2 != null)
				{
					return command2;
				}
			}
			else if (lastActiveCommand is Hold)
			{
				if (this.ShipUs.GetRCSRemain() > 0.0 && !this.ShipUs.IsDocked())
				{
					Ship nearestStationRegional2 = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosx);
					this.ShipUs.shipScanTarget = nearestStationRegional2;
					ICommand command3 = this.InstaDock();
					if (command3 != null)
					{
						return command3;
					}
				}
				else
				{
					lastActiveCommand.ResolveInstantly();
				}
			}
			return lastActiveCommand;
		}

		private ICommand InstaDock()
		{
			ICommand command = this.Commands.First((ICommand x) => x is Dock);
			CommandCode commandCode = command.ResolveInstantly();
			if ((commandCode & CommandCode.ResultDone) == commandCode)
			{
				return this.Commands.First((ICommand x) => x is Hold);
			}
			Debug.LogWarning(this.ShipUs.strRegID + " could not dock");
			return null;
		}

		public TargetData GetTarget()
		{
			if (AIShipManager.LowFuel(this.ShipUs, this.MaxSpeed(null)))
			{
				Ship nearestStation = CrewSim.system.GetNearestStation(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy, false);
				if (nearestStation != null)
				{
					return new TargetData(nearestStation);
				}
			}
			Ship ship2;
			if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.5)
			{
				List<Ship> list = new List<Ship>();
				foreach (Ship ship in CrewSim.system.dictShips.Values)
				{
					if (this.ScavCanDockWith(this.ShipUs, ship))
					{
						float num = (float)this.ShipUs.GetRangeTo(ship);
						if ((double)num <= ScavPilot.MaxTargetRange)
						{
							list.Add(ship);
						}
					}
				}
				int i = list.Count;
				while (i > 0)
				{
					i--;
					ship2 = list[UnityEngine.Random.Range(0, list.Count)];
					if (ship2 != null && !AIShipManager.BingoFuelCheck(this.ShipUs, ship2, this.MaxSpeed(null)))
					{
						return new TargetData(ship2);
					}
				}
			}
			Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy);
			ship2 = CrewSim.system.GetShipByRegID(AIShipManager.GetRandomStationInATCRegion(nearestStationRegional.strRegID));
			return new TargetData(ship2);
		}

		private bool ScavCanDockWith(Ship ship, Ship shipTarget)
		{
			return ship != shipTarget && !shipTarget.IsLocalAuthority && shipTarget.DMGStatus == Ship.Damage.Derelict && shipTarget.DockCount > 0 && shipTarget.GetAllDockedShips().Count <= 0 && !shipTarget.ShipCO.HasCond("IsTutorialDerelict");
		}

		private Ship CreateAIShip(string strStationID)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strStationID);
			if (!CrewSim.GetSelectedCrew().ship.objSS.bBOLocked && !CrewSim.GetSelectedCrew().ship.objSS.bIsBO && AIShip.IsPlayerClose(shipByRegID))
			{
				return null;
			}
			string faction = strStationID + "Scav";
			Ship ship = AIShip.FindShipForFaction(faction);
			if (ship == null)
			{
				ship = AIShip.SpawnNewShip(faction, "RandomScavShip", Ship.Damage.Used, null, shipByRegID);
				if (ship == null)
				{
					return null;
				}
				ship.origin = strStationID;
			}
			AIShip.ResetShipState(ship);
			shipByRegID.objSS.TimeAdvance(0.0, false);
			ship.objSS.vVelX = shipByRegID.objSS.vVelX;
			ship.objSS.vVelY = shipByRegID.objSS.vVelY;
			ship.objSS.PlaceOrbitPosition(shipByRegID.objSS);
			AIShip.RandomizeSpawnPosition(ship, shipByRegID.objSS.strBOPORShip);
			AIShip.AddCrew("OKLGScavCrew", ship, strStationID, true);
			AIShipManager.NPCReport();
			ship.ToggleVis(false, true);
			ship.AIRefuel();
			ship.Comms.SendMessage("SHIPUnDockAI", strStationID, null);
			return ship;
		}

		public static readonly double MaxTargetRange = 3.3422935320898145E-05;
	}
}
