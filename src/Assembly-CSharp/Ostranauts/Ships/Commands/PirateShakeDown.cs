using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class PirateShakeDown : BaseCommand
	{
		public PirateShakeDown(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			base.ShipUs.fAIPauseTimer = StarSystem.fEpoch;
			this._ctIsDead = DataHandler.GetCondTrigger("TIsDead");
			this._ctItemsOfInterest = DataHandler.GetCondTrigger("TIsRobbedItem");
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Investigating " + ((base.ShipUs.shipScanTarget == null) ? "Patrolling" : base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override CommandCode RunCommand()
		{
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			Ship ship = allDockedShips.FirstOrDefault<Ship>();
			if (allDockedShips.Count == 0 || ship == null)
			{
				return CommandCode.Skipped;
			}
			if (this._idling)
			{
				if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch)
				{
					return CommandCode.Ongoing;
				}
				this._idling = false;
				return CommandCode.Finished;
			}
			else
			{
				if (ship.LoadState > Ship.Loaded.Shallow)
				{
					return this.HandlePlayer(ship);
				}
				if (!this._idling)
				{
					this.StealFuel(ship);
					this.StealItems(ship);
					base.ShipUs.fAIPauseTimer = StarSystem.fEpoch + (double)MathUtils.Rand(100, 650, MathUtils.RandType.Flat, null);
					this._idling = true;
					return CommandCode.Ongoing;
				}
				this._idling = false;
				return CommandCode.Finished;
			}
		}

		public override CommandCode ResolveInstantly()
		{
			if (base.ShipUs.LoadState > Ship.Loaded.Shallow)
			{
				return CommandCode.Ongoing;
			}
			if (this._idling)
			{
				return this.RunCommand();
			}
			this.RunCommand();
			this._idling = false;
			base.ShipUs.fAIPauseTimer = 0.0;
			return CommandCode.Finished;
		}

		private void StealItems(Ship dockedShip)
		{
			CondOwner condOwner = base.ShipUs.GetPeople(false).FirstOrDefault<CondOwner>();
			if (condOwner == null)
			{
				return;
			}
			int num = 3;
			foreach (CondOwner condOwner2 in dockedShip.GetPeople(false))
			{
				List<CondOwner> cos = condOwner2.GetCOs(false, this._ctItemsOfInterest);
				if (cos != null)
				{
					int num2 = cos.Count - 1;
					while (num2 >= 0 && num > 0)
					{
						cos[num2].RemoveFromCurrentHome(false);
						CondOwner condOwner3 = condOwner.AddCO(cos[num2], true, true, true);
						if (condOwner3 != null)
						{
							condOwner3 = condOwner2.AddCO(cos[num2], true, true, true);
						}
						if (condOwner3 != null)
						{
							condOwner3.Destroy();
						}
						num--;
						num2--;
					}
				}
			}
		}

		private CommandCode HandlePlayer(Ship dockedShip)
		{
			bool flag = false;
			bool flag2 = true;
			bool flag3 = true;
			bool flag4 = false;
			List<CondOwner> list = new List<CondOwner>();
			foreach (CondOwner condOwner in base.ShipUs.GetPeople(true))
			{
				if (condOwner.HasCond("IsFactionViolenceCooldown") && condOwner.ship != base.ShipUs)
				{
					flag = true;
				}
				if (condOwner.HasFaction(AIShipManager.strATCLast + "Pirates"))
				{
					list.Add(condOwner);
					if (!this._ctIsDead.Triggered(condOwner, null, true))
					{
						flag3 = false;
					}
					flag = condOwner.HasCond("IsSubdued");
					if (condOwner.HasCond("IsRobberRobbing") && !condOwner.HasCond("IsSubdued"))
					{
						flag2 = false;
					}
					if (condOwner.bAlive && condOwner.ship != base.ShipUs)
					{
						flag2 = false;
					}
				}
				else if (!flag4)
				{
					flag4 = (condOwner.ship == base.ShipUs);
				}
			}
			if (flag3)
			{
				AIShipManager.UnregisterShip(base.ShipUs);
				return CommandCode.Finished;
			}
			if (flag && flag2 && !flag4)
			{
				base.ShipUs.shipScanTarget = null;
				bool flag5 = false;
				foreach (CondOwner condOwner2 in list)
				{
					if (condOwner2.HasCond("IsSubdued"))
					{
						flag5 = true;
						condOwner2.ZeroCondAmount("IsSubdued");
					}
				}
				if (!flag5)
				{
					this.StealFuel(dockedShip);
				}
				return CommandCode.Finished;
			}
			return CommandCode.Ongoing;
		}

		private void StealFuel(Ship dockedShip)
		{
			float num = (float)dockedShip.GetRCSRemain();
			float num2 = base.ShipUs.RefuelRCS(num);
			dockedShip.RemoveGasMass(num - num2);
			base.ShipUs.Comms.SendMessage("SHIPStealFuel", dockedShip.strRegID, null);
		}

		private CondTrigger _ctIsDead;

		private CondTrigger _ctItemsOfInterest;

		private bool _idling;

		private Tuple<double, string> _arrivalData;
	}
}
