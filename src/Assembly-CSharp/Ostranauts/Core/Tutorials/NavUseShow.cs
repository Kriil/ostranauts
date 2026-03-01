using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class NavUseShow : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Use the Nav Station";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Right click the Nav Station, then select Use.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Strapped in.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "SwitchToComms";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public void OnOpenNavStation()
		{
			base.Finished = true;
		}

		public override void AddInitialListeners()
		{
			GUIOrbitDraw.OpenedNavStationUI.AddListener(new UnityAction(this.OnOpenNavStation));
		}

		public override void RemoveAllListeners()
		{
			GUIOrbitDraw.OpenedNavStationUI.RemoveListener(new UnityAction(this.OnOpenNavStation));
		}
	}
}
