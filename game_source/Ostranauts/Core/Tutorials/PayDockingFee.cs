using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class PayDockingFee : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Pay Docking Fee";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click the capsule button displaying your money and pay your docking fees through the finances window.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Docking fees paid.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "RefuelAtKiosk";
			}
		}

		public void Complete()
		{
			base.Finished = true;
		}

		public override void AddInitialListeners()
		{
			Ledger.onLedgerPaid.AddListener(new UnityAction(this.Complete));
		}

		public override void RemoveAllListeners()
		{
			Ledger.onLedgerPaid.RemoveListener(new UnityAction(this.Complete));
		}
	}
}
