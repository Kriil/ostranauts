using System;

namespace Ostranauts.Events.DTOs
{
	public class DerelictDTO : ShipPurchaseDTO
	{
		public DerelictDTO(Ship ship) : base(ship.strRegID)
		{
			this.TransactionType = TransactionTypes.Cash;
		}
	}
}
