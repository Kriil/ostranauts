using System;
using Ostranauts.Objectives;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class MouseoverObjective : TutorialBeat
	{
		public MouseoverObjective()
		{
			Debug.Log(this.SetPersistentRef("IsTutorialDormSwitch", this.dormLightSwitch));
			Debug.Log(this.dormLightSwitch.CO);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Find Objective Target";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Left click an objective pop-up to focus on the objective's target.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Objective targeted.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ToggleLightSwitch";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.dormLightSwitch.CO;
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			if (CrewSim.coPlayer != null && !CrewSim.coPlayer.HasCond("TutorialStillInDorm"))
			{
				this.NextOverride = "NavWalk";
				return true;
			}
			return false;
		}

		public void TutorialMouseoverObjective(Objective objective)
		{
			if (objective == base.AssociatedObjective)
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			ObjectivePanel.OnFocusObjective.AddListener(new UnityAction<Objective>(this.TutorialMouseoverObjective));
		}

		public override void RemoveAllListeners()
		{
			ObjectivePanel.OnHighlightObjective.RemoveListener(new UnityAction<Objective>(this.TutorialMouseoverObjective));
		}

		public UniqueIDCOPair dormLightSwitch = new UniqueIDCOPair();
	}
}
