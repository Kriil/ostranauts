using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class UnpaidDockingFees : TutorialBeat
	{
		public UnpaidDockingFees()
		{
			GUIRosterRow.Opened.AddListener(new UnityAction(this.RespondToDelegate));
		}

		public override string ObjectiveName
		{
			get
			{
				return "Unpaid Docking Fees";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Visit the Fuel kiosk to pay your docking fees";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Docking fees paid!";
			}
		}

		public override string NextDefault
		{
			get
			{
				return string.Empty;
			}
		}

		public override string CTString
		{
			get
			{
				return "TIsRefuelKiosk";
			}
		}

		public void RespondToDelegate()
		{
			base.Finished = true;
		}

		public override void Process()
		{
			base.Process();
		}
	}
}
