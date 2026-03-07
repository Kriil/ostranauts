using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class Lurk : BaseCommand
	{
		public Lurk(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			this._aiPilot = pilot;
		}

		public override string[] SaveData
		{
			get
			{
				return null;
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				string text = value.FirstOrDefault<string>();
				if (!string.IsNullOrEmpty(text))
				{
					this._target = CrewSim.system.GetShipByRegID(text);
				}
			}
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "...<static>...";
			}
		}

		private bool InPosition
		{
			get
			{
				return base.ShipUs.objSS.bBOLocked;
			}
		}

		public override CommandCode RunCommand()
		{
			if (!this.InPosition)
			{
				BodyOrbit nearestBO = CrewSim.system.GetNearestBO(base.ShipUs.objSS, StarSystem.fEpoch, false);
				if (nearestBO == null || base.ShipUs.objSS.GetDistance(nearestBO.dXReal, nearestBO.dYReal) * 149597872.0 < 150.0)
				{
					base.ShipUs.objSS.UnlockFromBO();
					return CommandCode.Cancelled;
				}
			}
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			if (base.ShipUs.LoadState > Ship.Loaded.Shallow && allDockedShips.Count > 0)
			{
				this._target = allDockedShips.FirstOrDefault<Ship>();
			}
			if (this._target != null)
			{
				this.ActivatePirate(this._target);
				this._target = null;
				BeatManager.ResetTensionTimer();
				CrewSim.coPlayer.LogMessage("Remote ship scan activity detected.", "Bad", "Game");
				AudioManager.am.SuggestMusic("Danger", false);
				return CommandCode.Finished;
			}
			if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch)
			{
				if (base.ShipUs.bXPDRAntenna)
				{
					base.ShipUs.bXPDRAntenna = false;
					GUIOrbitDraw.TriggerShipRedraw(base.ShipUs.strRegID);
				}
				return CommandCode.Ongoing;
			}
			List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(AIShipManager.strATCLast, AIType.Scav);
			Tuple<double, Ship> tuple = new Tuple<double, Ship>(double.PositiveInfinity, null);
			foreach (AIShip aiship in shipsOfTypeForRegion)
			{
				if (aiship.Ship != null && !aiship.Ship.bDestroyed && !aiship.Ship.IsDocked() && !aiship.Ship.HideFromSystem)
				{
					double rangeTo = base.ShipUs.GetRangeTo(aiship.Ship);
					if (rangeTo < tuple.Item1)
					{
						tuple = new Tuple<double, Ship>(rangeTo, aiship.Ship);
					}
				}
			}
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (selectedCrew != null && selectedCrew.ship != null && base.ShipUs.GetRangeTo(selectedCrew.ship) < tuple.Item1)
			{
				tuple = new Tuple<double, Ship>(base.ShipUs.GetRangeTo(selectedCrew.ship), selectedCrew.ship);
			}
			if (tuple.Item2 != null && !tuple.Item2.IsDocked())
			{
				if (tuple.Item2 == base.ShipUs.shipUndock)
				{
					this.ActivatePirate(null);
					this._target = null;
					return CommandCode.Finished;
				}
				int num = (tuple.Item2 == null || !tuple.Item2.IsFlyingDark()) ? 30 : 15;
				if (tuple.Item1 * 149597872.0 < (double)num)
				{
					this.ActivatePirate(tuple.Item2);
					return CommandCode.Finished;
				}
			}
			if (!base.ShipUs.objSS.bBOLocked)
			{
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				BodyOrbit nearestBO2 = CrewSim.system.GetNearestBO(base.ShipUs.objSS, StarSystem.fEpoch, false);
				if (nearestBO2 != null)
				{
					base.ShipUs.objSS.LockToBO(nearestBO2, -1.0);
				}
				base.ShipUs.bXPDRAntenna = false;
				GUIOrbitDraw.TriggerShipRedraw(base.ShipUs.strRegID);
			}
			base.ShipUs.fAIPauseTimer = StarSystem.fEpoch + 30.0;
			return CommandCode.Ongoing;
		}

		private void ActivatePirate(Ship target)
		{
			base.ShipUs.bXPDRAntenna = true;
			base.ShipUs.shipScanTarget = target;
			base.ShipUs.shipSituTarget = null;
			base.ShipUs.objSS.UnlockFromBO();
			GUIOrbitDraw.TriggerShipRedraw(base.ShipUs.strRegID);
		}

		public override CommandCode ResolveInstantly()
		{
			return this.RunCommand();
		}

		private Ship _target;

		private IAICharacter _aiPilot;
	}
}
