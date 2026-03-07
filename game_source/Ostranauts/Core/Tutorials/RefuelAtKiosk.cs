using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class RefuelAtKiosk : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Refuel at a Fuel Kiosk";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Visit the orange \"Docking and Refuel Services\" kiosk and refuel your ship.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Ship refueled.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "TutorialEnd";
			}
		}

		public void Complete()
		{
			base.Finished = true;
		}

		public override void AddInitialListeners()
		{
			GUIStationRefuel.OnGUIRefuelSuccess.AddListener(new UnityAction(this.Complete));
		}

		public override void RemoveAllListeners()
		{
			GUIStationRefuel.OnGUIRefuelSuccess.RemoveListener(new UnityAction(this.Complete));
		}
	}
}
