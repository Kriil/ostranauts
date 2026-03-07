using System;
using Ostranauts.Events.DTOs;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnTradeShipevent : UnityEvent<ShipPurchaseDTO>
	{
	}
}
