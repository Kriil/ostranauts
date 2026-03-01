using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;
using UnityEngine;

namespace Ostranauts.Ships.AIPilots
{
	public class PolicePilot : IAICharacter
	{
		public PolicePilot(string strStationID, Ship ship)
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
				new ShakeDown(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.Police;
			}
		}

		public double MaxSpeed(bool? inAtmo = null)
		{
			bool flag = (inAtmo == null) ? this.ShipUs.InAtmo : inAtmo.Value;
			return (!flag) ? 1.002688036766397E-08 : 5.013440183831985E-09;
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyTo && this.ShipUs.objSS.HasNavData())
			{
				return lastActiveCommand;
			}
			if (lastActiveCommand is Undock || lastActiveCommand is ShakeDown)
			{
				CommandCode commandCode = lastActiveCommand.ResolveInstantly();
				if (commandCode == CommandCode.Ongoing)
				{
					return lastActiveCommand;
				}
			}
			AIShip.ClearShipTarget(this.ShipUs);
			this.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this.ShipUs.objSS, StarSystem.fEpoch, false);
			if (nearestBO == null)
			{
				return null;
			}
			nearestBO.UpdateTime(StarSystem.fEpoch, true, true);
			CrewSim.system.SetSituToRandomSafeCoords(this.ShipUs.objSS, 3.342293532089815E-07, 1.0026880596269445E-06, nearestBO.dXReal, nearestBO.dYReal, MathUtils.RandType.Low);
			Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy);
			if (AIShipManager.BingoFuelCheck(this.ShipUs, nearestStationRegional, this.MaxSpeed(null)))
			{
				this.ShipUs.shipScanTarget = nearestStationRegional;
				ICommand command = this.Commands.First((ICommand x) => x is Dock);
				command.ResolveInstantly();
				return command;
			}
			return this.Commands.First((ICommand x) => x is FlyTo);
		}

		private Ship CreateAIShip(string strStation)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(AIShipManager.strATCLast);
			if (shipByRegID == null || AIShip.IsPlayerClose(shipByRegID))
			{
				return null;
			}
			string text = AIShipManager.strATCLast + "LEO";
			Ship ship = AIShip.FindShipForFaction(text);
			if (ship == null)
			{
				ship = AIShip.SpawnNewShip(text, "RandomPoliceShip", Ship.Damage.New, null, shipByRegID);
				if (ship == null)
				{
					return null;
				}
				ship.origin = shipByRegID.publicName;
				ship.publicName = "NASS Patrol " + MathUtils.Rand(1, 10, MathUtils.RandType.Flat, null) + MathUtils.Rand(0, 10, MathUtils.RandType.Flat, null);
			}
			AIShip.ResetShipState(ship);
			ship.IsLocalAuthority = true;
			ship.strLaw = shipByRegID.strLaw;
			ship.objSS.vVelX = shipByRegID.objSS.vVelX;
			ship.objSS.vVelY = shipByRegID.objSS.vVelY;
			ship.objSS.PlaceOrbitPosition(shipByRegID.objSS);
			float f = UnityEngine.Random.Range(0.1f, 2f);
			float num = Mathf.Cos(f);
			float num2 = Mathf.Sin(f);
			ship.objSS.vPosx = shipByRegID.objSS.vPosx + (double)(num * 4E-09f);
			ship.objSS.vPosy = shipByRegID.objSS.vPosy + (double)(num2 * 4E-09f);
			AIShip.RandomizeSpawnPosition(ship, shipByRegID.objSS.strBOPORShip);
			CondOwner condOwner = AIShip.AddCrew("OKLGLEO", ship, strStation, true);
			string strName = text + shipByRegID.strRegID;
			JsonCompany jsonCompany = CrewSim.system.GetCompany(strName);
			if (jsonCompany == null)
			{
				jsonCompany = new JsonCompany();
				jsonCompany.strName = strName;
				jsonCompany.strRegID = ship.strRegID;
			}
			condOwner.Company = jsonCompany;
			jsonCompany.mapRoster[condOwner.strID] = new JsonCompanyRules();
			condOwner.Company.SetPermissionAirlock(condOwner.strID, true);
			condOwner.Company.SetPermissionShore(condOwner.strID, true);
			condOwner.Company.SetPermissionRestore(condOwner.strID, true);
			int nUTCHour = StarSystem.nUTCHour;
			jsonCompany.mapRoster[condOwner.strID].StartWorkdayAt(nUTCHour);
			condOwner.ShiftChange(jsonCompany.GetShift(nUTCHour, condOwner), true);
			AIShipManager.NPCReport();
			ship.ToggleVis(false, true);
			ship.AIRefuel();
			ship.Comms.SendMessage("SHIPUnDockAI", shipByRegID.strRegID, null);
			return ship;
		}

		public TargetData GetTarget()
		{
			Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy);
			Ship nearestStation = CrewSim.system.GetNearestStation(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy, false);
			if (AIShipManager.LowFuel(this.ShipUs, this.MaxSpeed(null)) && nearestStation != null)
			{
				return new TargetData(nearestStation);
			}
			Dictionary<double, Ship> dictionary = new Dictionary<double, Ship>();
			double fEpoch = StarSystem.fEpoch;
			foreach (Ship ship in CrewSim.system.dictShips.Values)
			{
				if (this.LEOValidTarget(this.ShipUs, ship))
				{
					double num = fEpoch - ship.dLastScanTime;
					if (num >= 21600.0)
					{
						if (nearestStationRegional != null)
						{
							double distance = MathUtils.GetDistance(nearestStationRegional.objSS.vPosx, nearestStationRegional.objSS.vPosy, ship.objSS.vPosx, ship.objSS.vPosy);
							if (distance < AIShipManager.fPoliceIgnoreRange)
							{
								continue;
							}
						}
						if (!AIShipManager.LEOForbiddenTarget(ship))
						{
							double num2 = 1.0;
							if (!ship.IsDocked())
							{
								num2 = 2.0;
							}
							double num3 = this.ShipUs.GetRangeTo(ship);
							if (num3 <= 6.684587424388155E-05)
							{
								if (ship.IsDerelict())
								{
									num2 *= 2.0;
								}
								else if (ship.IsFlyingDark())
								{
									if (this.fAccelDetectionRate == 0.0)
									{
										this.fAccelDetectionRate = 3.278790095228326E-11;
										this.fAccelDetectionRate *= this.fAccelDetectionRate;
									}
									double num4 = 200.0;
									if (ship.objSS.fAccelMagSquaredLast != 0.0)
									{
										double num5 = MathUtils.Max(5.0 * ship.objSS.fAccelMagSquaredLast / this.fAccelDetectionRate, 1.0);
										num4 *= num5;
									}
									if (num3 * 149597872.0 >= num4 || MathUtils.Rand(0.0, num4, MathUtils.RandType.Flat, null) <= num3 * 149597872.0)
									{
										continue;
									}
									num2 *= 0.10000000149011612;
								}
								else
								{
									bool flag = false;
									if (CrewSim.system.IsOwnerWanted(ship.strXPDR, ship.objSS.vPos))
									{
										num2 *= 0.0010000000474974513;
										flag = true;
									}
									if (!flag)
									{
										if (ship.LoadState >= Ship.Loaded.Edit)
										{
											List<CondOwner> cos = ship.GetCOs(new CondTrigger
											{
												aReqs = new string[]
												{
													"IsHoursLeft",
													"IsPermitOKLGSalvage"
												}
											}, true, true, true);
											if (cos.Count > 0)
											{
												if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, "LEO_LICENSE_CHECK") < 0.8999999761581421)
												{
													this.ShipUs.Comms.SendMessage("SHIPLeoLicenseDivert", ship.strRegID, null);
													BeatManager.ResetReleaseTimer();
													ship.dLastScanTime = StarSystem.fEpoch;
													foreach (Ship ship2 in ship.GetAllDockedShips())
													{
														ship2.dLastScanTime = StarSystem.fEpoch;
													}
													continue;
												}
												Debug.LogWarning("Police checking player despite license.");
											}
											else
											{
												num2 *= 0.25;
											}
										}
										num2 *= 0.5;
									}
								}
								num3 *= num2;
								num3 += 0.0002 / (10.0 + num);
								dictionary[num3] = ship;
							}
						}
					}
				}
			}
			foreach (KeyValuePair<double, Ship> keyValuePair in from x in dictionary
			orderby x.Key
			select x)
			{
				if (!AIShipManager.BingoFuelCheck(this.ShipUs, keyValuePair.Value, nearestStationRegional, this.MaxSpeed(new bool?(false))))
				{
					AIShipManager.NotifyTarget(this.ShipUs, keyValuePair.Value);
					return new TargetData(keyValuePair.Value);
				}
			}
			return new TargetData(nearestStationRegional);
		}

		private bool LEOValidTarget(Ship ship, Ship shipTarget)
		{
			if (ship == shipTarget)
			{
				return false;
			}
			if (shipTarget.IsLocalAuthority)
			{
				return false;
			}
			if (shipTarget.objSS.bIsBO)
			{
				return false;
			}
			if (shipTarget.HideFromSystem || shipTarget.bDestroyed)
			{
				return false;
			}
			AIType shipType = AIShipManager.GetShipType(shipTarget);
			return shipType != AIType.HaulerDeployer && shipType != AIType.HaulerRetriever;
		}

		private double fAccelDetectionRate;
	}
}
