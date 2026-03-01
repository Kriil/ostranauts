using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class DockWithDerelict : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Dock with Derelict";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Use the same flight controls from earlier to approach the docking ring below 100 m/s VREL, align docking rings, then click CLAMPS when the \"CLAMP ALIGN\" light is green.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Successfully Docked.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "PrepareToExploreDerelict";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public override void AddInitialListeners()
		{
			GUIDockSys.DockEvent.AddListener(new UnityAction<string>(this.CheckDockWithTutorialDerelict));
		}

		public override void RemoveAllListeners()
		{
			GUIDockSys.DockEvent.RemoveListener(new UnityAction<string>(this.CheckDockWithTutorialDerelict));
		}

		public void CheckDockWithTutorialDerelict(string regid)
		{
			if (CrewSimTut.tutorialShipInstanceRef != null && CrewSimTut.tutorialShipInstanceRef.strRegID == regid)
			{
				base.Finished = true;
			}
		}
	}
}
