using System;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class FlyToAutoPilot : BaseCommand
	{
		public FlyToAutoPilot(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
			this._flyToPath = new FlyToPath(pilot);
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (base.ShipUs.shipScanTarget == null || base.ShipUs.fAIPauseTimer > StarSystem.fEpoch) ? "Calculating target coordinates" : ("Flying to " + base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override string[] SaveData
		{
			get
			{
				return this._flyToPath.SaveData;
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				this._flyToPath.SaveData = value;
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.shipScanTarget != null && base.ShipUs.shipSituTarget != null)
			{
				base.ShipUs.shipSituTarget = base.ShipUs.shipScanTarget.objSS;
			}
			else if (base.ShipUs.shipScanTarget == null && base.ShipUs.shipSituTarget == null)
			{
				TargetData target = this._ai.GetTarget();
				if (target == null)
				{
					return CommandCode.Cancelled;
				}
				base.ShipUs.shipScanTarget = target.Ship;
				base.ShipUs.shipSituTarget = target.Situ;
			}
			if (!this.RequirementsMet())
			{
				CondOwner condOwner = base.ShipUs.aNavs.FirstOrDefault<CondOwner>();
				if (condOwner != null)
				{
					AlarmObjective objective = new AlarmObjective(AlarmType.nav_autopilot, condOwner, DataHandler.GetString("OBJV_NAV_AUTOPILOT_DESC", false));
					MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
				}
				base.ShipUs.LogAdd(DataHandler.GetString("NAV_LOG_AP_DISABLED", false), StarSystem.fEpoch, true);
				base.ShipUs.objSS.ResetNavData();
				return CommandCode.Cancelled;
			}
			if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch)
			{
				base.ShipUs.objSS.ResetNavData();
				return CommandCode.Cancelled;
			}
			return this._flyToPath.RunCommand();
		}

		private bool RequirementsMet()
		{
			if (!base.ShipUs.objSS.HasNavData())
			{
				return true;
			}
			Tuple<NavDataPoint, NavDataPoint> currentNavPoints = base.ShipUs.objSS.NavData.GetCurrentNavPoints(StarSystem.fEpoch);
			if (currentNavPoints == null)
			{
				return true;
			}
			bool flag = Math.Abs(currentNavPoints.Item2.FuelLevel - currentNavPoints.Item1.FuelLevel) > 0.0001;
			if (flag && (currentNavPoints.Item2.FuelLevel < 0.0 || base.ShipUs.GetRCSRemain() < currentNavPoints.Item2.FuelLevel || base.ShipUs.RCSCount == 0f))
			{
				return false;
			}
			bool flag2 = Math.Abs(currentNavPoints.Item2.TorchFuelLevel - currentNavPoints.Item1.TorchFuelLevel) > 0.0001;
			if (flag2 && (!base.ShipUs.bFusionReactorRunning || currentNavPoints.Item2.FuelLevel < 0.0 || base.ShipUs.fShallowFusionRemain < currentNavPoints.Item2.FuelLevel))
			{
				base.ShipUs.objSS.ResetNavData();
				return false;
			}
			return true;
		}

		public override CommandCode ResolveInstantly()
		{
			if (base.ShipUs.shipSituTarget == null)
			{
				return CommandCode.Cancelled;
			}
			base.ShipUs.shipSituTarget.UpdateTime(StarSystem.fEpoch, false);
			if (base.ShipUs.IsAIShip)
			{
				base.PlaceWithinDockingRange(base.ShipUs.shipSituTarget);
			}
			base.ShipUs.objSS.ResetNavData();
			return CommandCode.Finished;
		}

		private readonly IAICharacter _ai;

		private ICommand _flyToPath;
	}
}
