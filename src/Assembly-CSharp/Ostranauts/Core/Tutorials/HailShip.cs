using System;
using Ostranauts.ShipGUIs.MFD;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class HailShip : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Hail Ship";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click the \"Hail Ship\" button on the right side of the Comms console. Select \"OKLG\" from the list of contacts.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Opened channel to OKLG.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "RequestClearance";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public void CheckContactingOKLG(string reg)
		{
			if (reg == "OKLG")
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			MFDComms.OpenedShipComms.AddListener(new UnityAction<string>(this.CheckContactingOKLG));
		}

		public override void RemoveAllListeners()
		{
			MFDComms.OpenedShipComms.RemoveListener(new UnityAction<string>(this.CheckContactingOKLG));
		}
	}
}
