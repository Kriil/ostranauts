using System;
using Ostranauts.ShipGUIs.MFD;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnRequestMFDChangeEvent : UnityEvent<MFDPage>
	{
	}
}
