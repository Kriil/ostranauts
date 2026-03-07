using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class UpdateShipSelectionEvent : UnityEvent<string>
	{
	}
}
