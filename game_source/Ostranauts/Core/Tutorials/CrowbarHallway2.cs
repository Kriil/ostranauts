using System;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public class CrowbarHallway2 : TutorialBeat
	{
		public CrowbarHallway2()
		{
			this.SetPersistentRef("IsTutorialHallwaySwitch", this.TutorialHallwaySwitch);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Exploring The Station";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "This isn't the direct route to your ship. But sometimes venturing off the beaten path pays dividends.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Hmm. What's that switch?";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "CrowbarHallway3";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.TutorialHallwaySwitch.CO;
			}
		}

		public override void Process()
		{
			if (this.timer > 0f)
			{
				this.timer -= Time.deltaTime;
			}
			if (this.timer <= 0f)
			{
				base.Finished = true;
			}
			base.Process();
		}

		private UniqueIDCOPair TutorialHallwaySwitch = new UniqueIDCOPair();

		private float timer = 7.5f;
	}
}
