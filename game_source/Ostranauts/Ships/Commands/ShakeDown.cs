using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.AIPilots.Interfaces;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class ShakeDown : BaseCommand
	{
		public ShakeDown(IAICharacter pilot)
		{
			this._pilot = pilot;
			base.ShipUs = pilot.ShipUs;
			if (ShakeDown._ctShakeDownDone == null)
			{
				ShakeDown._ctShakeDownDone = DataHandler.GetCondTrigger("TIsShakedownDone");
			}
			if (ShakeDown._ctIsLeo == null)
			{
				ShakeDown._ctIsLeo = DataHandler.GetCondTrigger("TIsLEO");
			}
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Investigating " + ((base.ShipUs.shipScanTarget == null) ? "Patrolling" : base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override CommandCode ResolveInstantly()
		{
			if (base.ShipUs.LoadState > Ship.Loaded.Shallow)
			{
				return CommandCode.Ongoing;
			}
			Loot loot = DataHandler.GetLoot("CONDRemoveLEOAccuseOKLG");
			foreach (CondOwner condOwner in base.ShipUs.GetPeople(true))
			{
				if (ShakeDown._ctIsLeo.Triggered(condOwner, null, true))
				{
					loot.ApplyCondLoot(condOwner, 1f, null, 0f);
					CrewSim.MoveCO(condOwner, base.ShipUs, false);
				}
			}
			AIShip.ClearShipTarget(base.ShipUs);
			this._arrivalData = null;
			return CommandCode.Finished;
		}

		public override CommandCode RunCommand()
		{
			List<Ship> allDockedShipsFull = base.ShipUs.GetAllDockedShipsFull();
			if (allDockedShipsFull.Count > 0 && base.ShipUs.LoadState < Ship.Loaded.Edit && !base.ShipUs.NavAIManned)
			{
				Loot loot = DataHandler.GetLoot("CONDRemoveLEOAccuseOKLG");
				foreach (Ship ship in allDockedShipsFull)
				{
					List<CondOwner> people = ship.GetPeople(false);
					foreach (CondOwner condOwner in people)
					{
						if (ShakeDown._ctIsLeo.Triggered(condOwner, null, true))
						{
							loot.ApplyCondLoot(condOwner, 1f, null, 0f);
							CrewSim.MoveCO(condOwner, base.ShipUs, false);
						}
					}
				}
			}
			if ((base.ShipUs.shipScanTarget != null && base.ShipUs.shipScanTarget.IsStation(false)) || (base.ShipUs.shipScanTarget == null && allDockedShipsFull.Count == 0))
			{
				return CommandCode.Skipped;
			}
			Ship ship2 = base.ShipUs.shipScanTarget ?? allDockedShipsFull.FirstOrDefault<Ship>();
			if (ship2 == null)
			{
				return CommandCode.Skipped;
			}
			float num = (float)base.ShipUs.GetRangeTo(ship2);
			bool flag = ship2.IsPlayerShip();
			if (this._arrivalData == null || this._arrivalData.Item2 != ship2.strRegID)
			{
				this._arrivalData = new Tuple<double, string>(StarSystem.fEpoch, ship2.strRegID);
				if (flag)
				{
					List<CondOwner> people2 = base.ShipUs.GetPeople(true);
					bool flag2 = false;
					foreach (CondOwner condOwner2 in people2)
					{
						if (ShakeDown._ctIsLeo.Triggered(condOwner2, null, true))
						{
							JsonPledge pledge;
							if (!flag2)
							{
								condOwner2.AddCondAmount("IsShakedownModeActive", 1.0, 0.0, 0f);
								pledge = DataHandler.GetPledge("PoliceShakedown");
								Pledge2 pledge2 = PledgeFactory.Factory(condOwner2, pledge, null);
								condOwner2.AddPledge(pledge2);
								flag2 = true;
							}
							condOwner2.ZeroCondAmount("IsLEOArresting");
							condOwner2.ZeroCondAmount("IsLEOAttacking");
							pledge = DataHandler.GetPledge("AICrimeArrestOKLGLEODo");
							List<Pledge2> pledgesOfType = condOwner2.GetPledgesOfType(pledge);
							foreach (Pledge2 pledge3 in pledgesOfType)
							{
								condOwner2.RemovePledge(pledge3);
							}
						}
					}
				}
			}
			if (flag || base.ShipUs.IsPlayerShip())
			{
				return this.HandlePlayer(ship2, num);
			}
			if (num <= Dock.TargetDockedHandoffDistance)
			{
				ship2.dLastScanTime = StarSystem.fEpoch;
				AIShip.ClearShipTarget(base.ShipUs);
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				return CommandCode.Finished;
			}
			AIShip.ClearShipTarget(base.ShipUs);
			return CommandCode.Finished;
		}

		private CommandCode HandlePlayer(Ship target, float range)
		{
			float num = CollisionManager.GetCollisionDistanceAU(base.ShipUs, target);
			if (target.IsDocked())
			{
				num = Mathf.Max(CollisionManager.GetCollisionDistanceAU(base.ShipUs, target) * 2f, 3.3422936E-08f);
			}
			bool flag = target.CanBeDockedWith();
			bool flag2 = base.ShipUs.IsDockedWith(target);
			double dX = target.objSS.vVelX - base.ShipUs.objSS.vVelX;
			double dY = target.objSS.vVelY - base.ShipUs.objSS.vVelY;
			double magnitude = MathUtils.GetMagnitude(dX, dY);
			double num2 = magnitude * 149597872.0 * 1000.0;
			if (flag2)
			{
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				bool flag3 = false;
				bool flag4 = true;
				bool flag5 = false;
				foreach (CondOwner condOwner in base.ShipUs.GetPeople(true))
				{
					if (ShakeDown._ctShakeDownDone.Triggered(condOwner, null, true))
					{
						flag3 = true;
					}
					if (ShakeDown._ctIsLeo.Triggered(condOwner, null, true))
					{
						if (condOwner.bAlive && condOwner.ship != base.ShipUs)
						{
							flag4 = false;
						}
					}
					else if (!flag5)
					{
						flag5 = (condOwner.ship == base.ShipUs);
					}
				}
				if (flag3 && flag4 && !flag5 && base.ShipUs.NavAIManned)
				{
					Loot loot = DataHandler.GetLoot("CONDRemoveLEOAccuseOKLG");
					foreach (CondOwner coUs in base.ShipUs.GetPeople(true))
					{
						loot.ApplyCondLoot(coUs, 1f, null, 0f);
					}
					AIShip.ClearShipTarget(base.ShipUs);
					base.ShipUs.shipUndock = target;
					target.dLastScanTime = StarSystem.fEpoch;
					return CommandCode.Finished;
				}
				return CommandCode.Ongoing;
			}
			else
			{
				if (range < num * 2f && num2 < 100.0)
				{
					bool flag6 = true;
					foreach (CondOwner objOwner in target.GetPeople(false))
					{
						if (ShakeDown._ctShakeDownDone.Triggered(objOwner, null, true))
						{
							flag = false;
							flag6 = false;
							target.dLastScanTime = StarSystem.fEpoch;
							break;
						}
					}
					CommandCode result = CommandCode.Ongoing;
					if (flag)
					{
						base.ShipUs.objSS.CopyFrom(target.objSS, true);
						AudioManager.am.SuggestMusic("Explore", false);
						CrewSim.coPlayer.LogMessage(DataHandler.GetString("COMMS_PATROL_BOARDING", false), "Dialogue", base.ShipUs.strRegID);
						CrewSim.DockShip(target, base.ShipUs.strRegID);
						BeatManager.ResetTensionTimer();
						target.dLastScanTime = StarSystem.fEpoch;
						base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
						result = CommandCode.Ongoing;
					}
					else if (this.WaitTooLong() && flag6)
					{
						this.ApplyFine(target);
						AIShip.ClearShipTarget(base.ShipUs);
						base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
						target.dLastScanTime = StarSystem.fEpoch;
						result = CommandCode.Finished;
					}
					else
					{
						base.ShipUs.objSS.vVelX = base.ShipUs.shipScanTarget.objSS.vVelX;
						base.ShipUs.objSS.vVelY = base.ShipUs.shipScanTarget.objSS.vVelY;
						base.ShipUs.objSS.fW = 0f;
					}
					List<Ship> allDockedShips = target.GetAllDockedShips();
					foreach (Ship ship in allDockedShips)
					{
						ship.dLastScanTime = StarSystem.fEpoch;
					}
					return result;
				}
				if (this.WaitTooLong())
				{
					this.ApplyFine(target);
					AIShip.ClearShipTarget(base.ShipUs);
					base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
					target.dLastScanTime = StarSystem.fEpoch;
					return CommandCode.Finished;
				}
				return this.ReApproachTarget(range, target);
			}
		}

		private bool WaitTooLong()
		{
			return StarSystem.fEpoch - this._arrivalData.Item1 > 300.0 || AIShipManager.BingoFuelCheck(base.ShipUs, AIShipManager.ShipATCLast, this._pilot.MaxSpeed(new bool?(false)));
		}

		private CommandCode ReApproachTarget(float range, Ship target)
		{
			if (target.IsDocked() && range < 1.0026881E-07f)
			{
				base.ShipUs.objSS.fW = 0f;
				return CommandCode.Ongoing;
			}
			FlyTo flyTo = new FlyTo(this._pilot);
			CommandCode commandCode = flyTo.RunCommand();
			if ((commandCode & CommandCode.ResultNegative) == commandCode)
			{
				return commandCode;
			}
			return CommandCode.Ongoing;
		}

		private void ApplyFine(Ship target)
		{
			bool flag = false;
			Loot loot = DataHandler.GetLoot("CONDRemoveLEOAccuseOKLG");
			foreach (CondOwner condOwner in base.ShipUs.GetPeople(false))
			{
				if (this.ctAccusingAny.Triggered(condOwner, null, true) && !flag)
				{
					Loot loot2 = DataHandler.GetLoot("CrimeOKLGArrestNoUndock");
					CondOwner condOwner2;
					if (target.Comms.GetCaptain(out condOwner2))
					{
						loot2.ApplyCondLoot(condOwner2, 1f, null, 0f);
						Ledger.AddLI("ENCPoliceShakedownFineNoUndock", condOwner2, condOwner);
						base.ShipUs.Comms.SendMessage("SHIPLeoFined", target.strRegID, null);
						flag = true;
					}
				}
				loot.ApplyCondLoot(condOwner, 1f, null, 0f);
			}
		}

		private CondTrigger ctAccusingAny
		{
			get
			{
				if (ShakeDown._ctAccusingAny == null)
				{
					ShakeDown._ctAccusingAny = DataHandler.GetCondTrigger("TIsAccusingAny");
				}
				return ShakeDown._ctAccusingAny;
			}
		}

		private IAICharacter _pilot;

		private static CondTrigger _ctShakeDownDone;

		private static CondTrigger _ctIsLeo;

		private Tuple<double, string> _arrivalData;

		private static CondTrigger _ctAccusingAny;
	}
}
