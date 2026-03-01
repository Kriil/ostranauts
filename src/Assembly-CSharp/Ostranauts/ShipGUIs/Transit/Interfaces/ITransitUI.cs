using System;
using System.Collections.Generic;

namespace Ostranauts.ShipGUIs.Transit.Interfaces
{
	public interface ITransitUI
	{
		void SetData(List<JsonTransitConnection> connections, CondOwner coKiosk);
	}
}
