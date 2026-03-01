using System;

namespace Ostranauts.Core.Tutorials
{
	public class TravelToDerelict : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Travel to Docking Range";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Travel to within RNG < 5 km of your target, and slow down until VREL is below 100 m/s (with destination targeted).";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Traveled to Within Docking Range.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "MatchSpeed";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public override void Process()
		{
			if (GUIOrbitDraw.CrossHairTarget != null && GUIOrbitDraw.Instance && GUIOrbitDraw.Instance.dRNG <= 3.342293553032505E-08 && GUIOrbitDraw.Instance.fVRel < 6.684587064179629E-10 && GUIOrbitDraw.CrossHairTarget.Ship != null && GUIOrbitDraw.CrossHairTarget.Ship == CrewSimTut.tutorialShipInstanceRef)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
