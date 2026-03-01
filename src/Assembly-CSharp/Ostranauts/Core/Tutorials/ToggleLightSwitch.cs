using System;

namespace Ostranauts.Core.Tutorials
{
	public class ToggleLightSwitch : TutorialBeat
	{
		public ToggleLightSwitch()
		{
			this.SetPersistentRef("IsTutorialDormSwitch", this.dormLightSwitch);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Toggle the Switch";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Right click on the nearby Power Switch. Select Toggle Power to turn on the lights.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Lights On.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "CollectEquipment";
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

		public override void Process()
		{
			if (this.dormLightSwitch.CO && !this.dormLightSwitch.CO.HasCond("IsOff"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		public UniqueIDCOPair dormLightSwitch = new UniqueIDCOPair();
	}
}
