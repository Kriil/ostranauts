using System;
using Ostranauts.Ships.Comms;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class GainClearance : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Gain Clearance to Dock";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Select \"Hail Ship\", choose the closest \"?\", and initiate docking procedure to dock with the derelict.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Gained Docking Clearance.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "DockWithDerelict";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		private void CheckClearance(ShipMessage dto)
		{
			if (dto == null || CrewSim.shipCurrentLoaded == null || dto.ReceiverRegId != CrewSim.shipCurrentLoaded.strRegID)
			{
				return;
			}
			if (CrewSim.shipCurrentLoaded.Comms != null && CrewSim.shipCurrentLoaded.Comms.HasClearanceWithTarget(CrewSimTut.tutorialShipInstanceRef.strRegID))
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			StarSystem.OnNewShipCommsMessage.AddListener(new UnityAction<ShipMessage>(this.CheckClearance));
		}

		public override void RemoveAllListeners()
		{
			StarSystem.OnNewShipCommsMessage.RemoveListener(new UnityAction<ShipMessage>(this.CheckClearance));
		}
	}
}
