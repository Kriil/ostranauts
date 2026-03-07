using System;
using Ostranauts.Ships.AIPilots.Interfaces;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class Dock : BaseCommand
	{
		public Dock(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			this._ai = pilot;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (base.ShipUs.shipScanTarget == null) ? "Docking" : ("Docking with " + base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.IsDockedFull() || base.ShipUs.shipScanTarget == null)
			{
				this.Cleanup();
				return CommandCode.Skipped;
			}
			if (!this.HasValidTargetLock() || !base.ShipUs.NavAIManned)
			{
				return CommandCode.Ongoing;
			}
			Ship shipScanTarget = base.ShipUs.shipScanTarget;
			float num = (float)base.ShipUs.GetRangeTo(shipScanTarget);
			double dX = shipScanTarget.objSS.vVelX - base.ShipUs.objSS.vVelX;
			double dY = shipScanTarget.objSS.vVelY - base.ShipUs.objSS.vVelY;
			double magnitude = MathUtils.GetMagnitude(dX, dY);
			double num2 = magnitude * 149597872.0 * 1000.0;
			float num3 = Mathf.Max(base.GetHandOffDistance(CollisionManager.GetCollisionDistanceAU(base.ShipUs, shipScanTarget)), 3.3422936E-08f);
			if (num >= num3)
			{
				if (AIShipManager.ShowDebugLogs)
				{
					Debug.Log(string.Concat(new string[]
					{
						"#AI# ",
						base.ShipUs.strRegID,
						" Skipped docking; Range: ",
						MathUtils.GetDistUnits((double)num),
						" fvrelm: ",
						MathUtils.GetDistUnits(num2)
					}));
				}
				return CommandCode.Skipped;
			}
			if (shipScanTarget.objSS.bIsBO)
			{
				if (!this.IsCollisionImminent(num2) && !base.ShipUs.Comms.AIGetClearance(shipScanTarget))
				{
					return CommandCode.Ongoing;
				}
				if (CrewSim.coPlayer.ship == base.ShipUs)
				{
					CrewSim.DockShip(base.ShipUs, shipScanTarget.strRegID);
					base.ShipUs.Dock(shipScanTarget, false);
					base.ShipUs.AIRefuel();
					base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
					this.Cleanup();
					return CommandCode.Finished;
				}
				CrewSim.DockAndDespawn(base.ShipUs, shipScanTarget, null);
				this.Cleanup();
				return CommandCode.Finished;
			}
			else
			{
				if (!this.CanDockWith(base.ShipUs, shipScanTarget))
				{
					if (!base.ShipUs.IsLocalAuthority)
					{
						base.ShipUs.shipScanTarget = null;
						base.ShipUs.shipSituTarget = null;
					}
					return CommandCode.Cancelled;
				}
				CondOwner selectedCrew = CrewSim.GetSelectedCrew();
				if (selectedCrew != null && selectedCrew.ship == shipScanTarget)
				{
					CrewSim.DockShip(shipScanTarget, base.ShipUs.strRegID);
				}
				else if (base.ShipUs.LoadState >= Ship.Loaded.Edit)
				{
					CrewSim.DockShip(base.ShipUs, shipScanTarget.strRegID);
				}
				else
				{
					base.ShipUs.Dock(shipScanTarget, false);
				}
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				this.Cleanup();
				return CommandCode.Finished;
			}
		}

		private bool IsCollisionImminent(double fVRelm)
		{
			return fVRelm > 100.0;
		}

		private bool HasValidTargetLock()
		{
			if (base.ShipUs.shipScanTarget.bDestroyed || base.ShipUs.shipScanTarget.objSS == null)
			{
				base.ShipUs.shipScanTarget = CrewSim.system.GetShipByRegID(base.ShipUs.shipScanTarget.strRegID);
				if (base.ShipUs.shipScanTarget == null || base.ShipUs.shipScanTarget.objSS == null || base.ShipUs.shipScanTarget.bDestroyed)
				{
					base.ShipUs.shipScanTarget = null;
					return false;
				}
			}
			return true;
		}

		private bool CanDockWith(Ship ship, Ship shipTarget)
		{
			return ship != shipTarget && base.ShipUs.CanBeDockedWith() && shipTarget.CanBeDockedWith();
		}

		public override CommandCode ResolveInstantly()
		{
			if (base.ShipUs.shipScanTarget == null || !this.HasValidTargetLock())
			{
				return CommandCode.Cancelled;
			}
			if (CrewSim.coPlayer.ship == base.ShipUs)
			{
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				if (base.ShipUs.IsAIShip)
				{
					CrewSim.DockAndDespawn(base.ShipUs, base.ShipUs.shipScanTarget, null);
				}
				else
				{
					CrewSim.DockShip(base.ShipUs, base.ShipUs.shipScanTarget.strRegID);
				}
				this.Cleanup();
				return CommandCode.Finished;
			}
			if (base.ShipUs.shipScanTarget.objSS.bIsBO)
			{
				CrewSim.DockAndDespawn(base.ShipUs, base.ShipUs.shipScanTarget, null);
				this.Cleanup();
				return CommandCode.Finished;
			}
			if (this.CanDockWith(base.ShipUs, base.ShipUs.shipScanTarget))
			{
				base.ShipUs.Dock(base.ShipUs.shipScanTarget, false);
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				this.Cleanup();
				return CommandCode.Finished;
			}
			return CommandCode.Cancelled;
		}

		private void Cleanup()
		{
			base.ShipUs.shipScanTarget = null;
			base.ShipUs.shipSituTarget = null;
			foreach (CondOwner condOwner in base.ShipUs.GetPeople(false))
			{
				condOwner.SetCondAmount("IsPledgeUndockDone", 1.0, 0.0);
			}
		}

		public static readonly float TargetDockedHandoffDistance = 1.0026881E-07f;

		private readonly IAICharacter _ai;
	}
}
