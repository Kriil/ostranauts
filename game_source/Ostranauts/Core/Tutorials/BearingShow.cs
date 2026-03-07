using System;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	// Tutorial beat for teaching the player to point the ship at a target station.
	// This appears in the early flight tutorial and watches the nav display's BRG
	// readout plus angular velocity until the ship is aimed at OKLG.
	public class BearingShow : TutorialBeat
	{
		// Objective text shown in the tutorial tracker/UI.
		public override string ObjectiveName
		{
			get
			{
				return "Aim at OKLG";
			}
		}

		// Instructional body text for the current beat.
		public override string ObjectiveDesc
		{
			get
			{
				return "Use rotation controls until the ship is pointing towards OKLG (BRG near 0, with OKLG targeted).";
			}
		}

		// Completion text shown once the alignment condition is satisfied.
		public override string ObjectiveDescComplete
		{
			get
			{
				return "Aimed at OKLG.";
			}
		}

		// Default next beat if no explicit branch overrides it.
		public override string NextDefault
		{
			get
			{
				return "RearThrust";
			}
		}

		// Uses the player's nav station as the highlighted tutorial target.
		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		// Completes when angular velocity is near zero and the orbit display bearing is near 0/360.
		public override void Process()
		{
			if (CrewSimTut.playerShipRef != null)
			{
				ShipSitu objSS = CrewSimTut.playerShipRef.objSS;
				if (objSS != null && GUIOrbitDraw.Instance && (double)Mathf.Abs(objSS.fW) <= 0.01 && (Mathf.Abs(GUIOrbitDraw.Instance.fBRG) <= 5f || Mathf.Abs(GUIOrbitDraw.Instance.fBRG) >= 355f))
				{
					base.Finished = true;
				}
			}
			base.Process();
		}
	}
}
