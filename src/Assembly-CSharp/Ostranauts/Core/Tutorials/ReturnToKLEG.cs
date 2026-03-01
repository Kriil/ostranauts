using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class ReturnToKLEG : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Return to K-LEG";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Fly back and dock with OKLG station in order to sell your salvage.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Returned to OKLG for salvage sale.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "SellSalvageAtKiosk";
			}
		}

		public void CheckPlayerDock(Ship playerShip, Ship OKLG)
		{
			if (playerShip != null && CrewSimTut.playerShipRef != null && playerShip.strRegID == CrewSimTut.playerShipRef.strRegID)
			{
				if (OKLG != null)
				{
					Debug.Log(OKLG.strRegID);
				}
				if (OKLG != null && OKLG.strRegID == "OKLG")
				{
					base.Finished = true;
				}
			}
		}

		public override void Process()
		{
			if (CrewSim.coPlayer.ship.publicName == "K-Leg: Port Azikiwe")
			{
				base.Finished = true;
			}
			base.Process();
		}

		public override void AddInitialListeners()
		{
			Ship.OnDock.AddListener(new UnityAction<Ship, Ship>(this.CheckPlayerDock));
		}

		public override void RemoveAllListeners()
		{
			Ship.OnDock.RemoveListener(new UnityAction<Ship, Ship>(this.CheckPlayerDock));
		}
	}
}
