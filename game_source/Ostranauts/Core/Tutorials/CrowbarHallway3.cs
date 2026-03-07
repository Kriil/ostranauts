using System;

namespace Ostranauts.Core.Tutorials
{
	public class CrowbarHallway3 : TutorialBeat
	{
		public CrowbarHallway3()
		{
			this.SetPersistentRef("IsTutorialHallwaySwitch", this.hallwaySwitch);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Investigate Hallway Switch";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Try toggling the switch at the end of the hallway.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Curiosity sated.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "CrowbarHallway4";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.hallwaySwitch.CO;
			}
		}

		public override void Process()
		{
			if (this.hallwaySwitch.CO && !this.hallwaySwitch.CO.HasCond("IsOff"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		private UniqueIDCOPair hallwaySwitch = new UniqueIDCOPair();
	}
}
