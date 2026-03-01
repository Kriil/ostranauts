using System;

namespace Ostranauts.ShipGUIs.Interfaces
{
	public interface IDataWindow
	{
		void RegisterWindow();

		void UnregisterWindow();

		void CloseExternally();
	}
}
