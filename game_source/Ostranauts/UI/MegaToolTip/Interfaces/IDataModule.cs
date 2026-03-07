using System;

namespace Ostranauts.UI.MegaToolTip.Interfaces
{
	public interface IDataModule
	{
		bool IsMarkedForDestroy();

		void SetData(CondOwner co);

		void Destroy();
	}
}
