using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class TargetDerelict : TutorialBeat
	{
		public TargetDerelict()
		{
			if (CrewSimTut.tutorialShipInstanceRef == null)
			{
				BeatManager.GenerateTutorialDerelict();
			}
		}

		public override string ObjectiveName
		{
			get
			{
				return "Target the closest Derelict";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Zoom out. Find the Nav contact marked TUTORIAL DERELICT in the boneyard. Select it.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "TUTORIAL DERELICT Targeted.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "TravelToDerelict";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public void CheckTargetDerelict(string targetedOnGUI)
		{
			if (string.IsNullOrEmpty(targetedOnGUI))
			{
				return;
			}
			Ship shipByRegID = CrewSim.system.GetShipByRegID(targetedOnGUI);
			if (shipByRegID != null && shipByRegID.ShipCO.HasCond("IsTutorialDerelict"))
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			GUIOrbitDraw.SelectedShipDraw.AddListener(new UnityAction<string>(this.CheckTargetDerelict));
		}

		public override void RemoveAllListeners()
		{
			GUIOrbitDraw.SelectedShipDraw.RemoveListener(new UnityAction<string>(this.CheckTargetDerelict));
		}
	}
}
