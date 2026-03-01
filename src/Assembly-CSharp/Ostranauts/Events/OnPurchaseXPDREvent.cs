using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnPurchaseXPDREvent : UnityEvent<string, double>
	{
	}
}
