using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnTileSelectionEvent : UnityEvent<List<CondOwner>>
	{
	}
}
