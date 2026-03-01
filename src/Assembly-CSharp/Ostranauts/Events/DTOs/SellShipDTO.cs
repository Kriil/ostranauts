using System;

namespace Ostranauts.Events.DTOs
{
	public class SellShipDTO : UsedShipDTO
	{
		public SellShipDTO(Ship ship) : base(ship)
		{
			this.TransactionType = TransactionTypes.Sell;
		}
	}
}
