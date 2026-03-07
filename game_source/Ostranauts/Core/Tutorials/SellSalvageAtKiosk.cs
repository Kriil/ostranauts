using System;
using Ostranauts.ShipGUIs.Trade;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class SellSalvageAtKiosk : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Sell Salvage at a Kiosk";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Visit the yellow licensed kiosk, or gray scrap kiosk, and sell any item.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Salvage sale complete.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "PayDockingFee";
			}
		}

		public void Complete()
		{
			base.Finished = true;
		}

		public override void AddInitialListeners()
		{
			GUITrade.OnGUITradeSale.AddListener(new UnityAction(this.Complete));
		}

		public override void RemoveAllListeners()
		{
			GUITrade.OnGUITradeSale.RemoveListener(new UnityAction(this.Complete));
		}
	}
}
