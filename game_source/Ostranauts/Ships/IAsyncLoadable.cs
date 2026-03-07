using System;
using System.Collections;

namespace Ostranauts.Ships
{
	public interface IAsyncLoadable
	{
		JsonShip json { get; set; }

		bool FullyLoaded { get; }

		IEnumerator Init(int iteratorCounter);

		void Destroy(bool isDespawning = true);

		void SaveChangedCOs(ref Ship original);
	}
}
