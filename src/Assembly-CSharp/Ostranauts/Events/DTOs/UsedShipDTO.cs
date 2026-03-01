using System;

namespace Ostranauts.Events.DTOs
{
	public class UsedShipDTO : ShipPurchaseDTO
	{
		public UsedShipDTO(Ship ship) : base(ship.strRegID)
		{
			this.ShipName = ship.publicName;
			this.Model = ship.model;
			this.Make = ship.make;
			this.TransactionType = TransactionTypes.Mortgage;
		}

		public UsedShipDTO(JsonShip jShip) : base(jShip.strRegID)
		{
			this.ShipName = jShip.publicName;
			this.Model = jShip.model;
			this.Make = jShip.make;
			this.TransactionType = TransactionTypes.Mortgage;
		}
	}
}
