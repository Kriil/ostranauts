using System;
using UnityEngine;

namespace Ostranauts.Events.DTOs
{
	public class ShipPurchaseDTO
	{
		public ShipPurchaseDTO()
		{
		}

		protected ShipPurchaseDTO(string regID)
		{
			this.RegId = regID;
		}

		public string RegId = string.Empty;

		public double ShipValue;

		public string ShipName = string.Empty;

		public string Model = string.Empty;

		public string Make = string.Empty;

		public Texture Image;

		public bool IsSilhouette;

		public Action Callback;

		public TransactionTypes TransactionType;

		public double TransactionPrice;

		public bool IsSpecialOffer;
	}
}
