using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class Zoom : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Adjust the NAV Station Zoom";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Zoom the NAV Station's view in or out with with the mouse scrollwheel. Hold the shift key to zoom faster.\n\nZoom in until the range is under 6km.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Things are coming into focus.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "TargetOKLG";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public void ReceiveZoomDouble(double d)
		{
			if (d < 4.010752238507778E-08)
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			GUIOrbitDraw.OnZoom = (UnityAction<double>)Delegate.Combine(GUIOrbitDraw.OnZoom, new UnityAction<double>(this.ReceiveZoomDouble));
		}

		public override void RemoveAllListeners()
		{
			GUIOrbitDraw.OnZoom = (UnityAction<double>)Delegate.Remove(GUIOrbitDraw.OnZoom, new UnityAction<double>(this.ReceiveZoomDouble));
		}
	}
}
