using System;

namespace Ostranauts.Core.Tutorials
{
	public class CrowbarHallway4 : TutorialBeat
	{
		public CrowbarHallway4()
		{
			this.SetPersistentRef("IsTutorialHallwayCrowbar", this.crowbar);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Investigate Further";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Now the door is powered, pick up your reward on the far side.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Finally something to crow about.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "CrowbarHallway5";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.crowbar.CO;
			}
		}

		public override void Process()
		{
			if (this.crowbar.CO && CrewSim.coPlayer && this.crowbar.CO.RootParent(null) == CrewSim.coPlayer)
			{
				base.Finished = true;
			}
			base.Process();
		}

		private UniqueIDCOPair crowbar = new UniqueIDCOPair();

		private bool switchExisted;
	}
}
