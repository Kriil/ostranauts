using System;

namespace Ostranauts.Ships
{
	public interface IShip
	{
		void AddCO(CondOwner objICO, bool bTiles);

		void RemoveCO(CondOwner objCO, bool bForce);
	}
}
