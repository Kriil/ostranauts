using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class TakeFuel : BaseCommand
	{
		public TakeFuel(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			base.ShipUs.SyncFuel();
			base.ShipUs.fAIPauseTimer = StarSystem.fEpoch + 0.5;
		}

		public override string[] SaveData
		{
			get
			{
				return new string[]
				{
					this._timer.ToString()
				};
			}
			set
			{
				string text = value.FirstOrDefault<string>();
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				double.TryParse(text, out this._timer);
			}
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Refueling";
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs == null || base.ShipUs.bDestroyed)
			{
				return CommandCode.Skipped;
			}
			if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch)
			{
				return CommandCode.Ongoing;
			}
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			Ship ship = allDockedShips.FirstOrDefault<Ship>();
			if (ship == null)
			{
				return CommandCode.Skipped;
			}
			double rcsremain = ship.GetRCSRemain();
			double rcsremain2 = base.ShipUs.GetRCSRemain();
			double getRCSMinimumFuelAmount = base.ShipUs.GetRCSMinimumFuelAmount;
			float num = Mathf.Min(30f, (float)(getRCSMinimumFuelAmount - rcsremain2));
			num = Mathf.Max(num, 0f);
			if (rcsremain2 >= getRCSMinimumFuelAmount || rcsremain <= 0.0)
			{
				base.ShipUs.Comms.SendMessage("SHIPTransferFuelComplete", ship.strRegID, null);
				base.ShipUs.fAIDockingExpire = 0.0;
				return CommandCode.Finished;
			}
			float num2 = base.ShipUs.RefuelRCS(num);
			ship.RemoveGasMass(num - num2);
			base.ShipUs.Comms.SendMessage("SHIPTransferFuelingOngoing", ship.strRegID, null);
			base.ShipUs.fAIPauseTimer = StarSystem.fEpoch + 1.0;
			return CommandCode.Ongoing;
		}

		private const float MAX_FUEL_TRANSFER_PER_CYCLE = 30f;

		private double _timer;
	}
}
