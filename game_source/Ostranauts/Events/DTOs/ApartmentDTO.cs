using System;

namespace Ostranauts.Events.DTOs
{
	public class ApartmentDTO : UsedShipDTO
	{
		public ApartmentDTO(JsonShip ship) : base(ship)
		{
			this.ShipName = ship.strName;
		}
	}
}
