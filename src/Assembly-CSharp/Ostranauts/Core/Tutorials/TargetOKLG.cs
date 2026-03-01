using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class TargetOKLG : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Target K-Leg Station";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click on the white diamond labeled OKLG to target it and get detailed info.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "K-Leg Station Targeted";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "CalibrateCW";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public void CheckTargetOKLG(string s)
		{
			if (s == "OKLG")
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			GUIOrbitDraw.SelectedShipDraw.AddListener(new UnityAction<string>(this.CheckTargetOKLG));
		}

		public override void RemoveAllListeners()
		{
			GUIOrbitDraw.SelectedShipDraw.RemoveListener(new UnityAction<string>(this.CheckTargetOKLG));
		}
	}
}
