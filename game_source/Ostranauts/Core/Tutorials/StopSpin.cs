using System;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public class StopSpin : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Stop Spin";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Hold \"" + GUIActionKeySelector.commandShipAttitude.KeyName + "\" until the ship stops spinning.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Spin Stopped.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "BearingShow";
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
			if (CrewSimTut.playerShipRef != null && (double)Mathf.Abs(CrewSimTut.playerShipRef.objSS.fW) < 0.01)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
