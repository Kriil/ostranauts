using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class RequestClearance : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Request Undocking Clearance";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Select \"Request undocking clearance\". Finally, to undock, press the CLAMPS button.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Undocked from OKLG.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "SwitchNav";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public void Complete()
		{
			base.Finished = true;
		}

		public override void AddInitialListeners()
		{
			GUIDockSys.UndockEvent.AddListener(new UnityAction(this.Complete));
		}

		public override void RemoveAllListeners()
		{
			GUIDockSys.UndockEvent.RemoveListener(new UnityAction(this.Complete));
		}
	}
}
