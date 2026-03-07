using System;

namespace Ostranauts.Core.Tutorials
{
	public class PickUpToolbox : TutorialBeat
	{
		public PickUpToolbox()
		{
			this.SetPersistentRef("IsStartingToolbox", this.toolbox);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Pick Up Your Toolbox";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Place the nearby yellow toolbox in your hand slot.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Tools acquired.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "NavWalk";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.toolbox.CO;
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
			if (this.toolbox.CO && this.toolbox.CO.slotNow != null && this.toolbox.CO.slotNow.strName.Contains("held"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		public UniqueIDCOPair toolbox = new UniqueIDCOPair();
	}
}
