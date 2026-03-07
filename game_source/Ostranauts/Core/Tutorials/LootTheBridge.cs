using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public class LootTheBridge : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Loot The Bridge";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Whoever's this was, they don't need it anymore. Take what you can from the lockers.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "When you're ready, return to KLEG.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ReturnToKLEG";
			}
		}

		public override void Process()
		{
			this.timer -= Time.deltaTime;
			if (this.timer <= 0f)
			{
				base.Finished = true;
			}
			base.Process();
		}

		public List<string> tags = new List<string>();

		public List<string> ids = new List<string>();

		public List<CondOwner> condOwners = new List<CondOwner>();

		private float timer = 10f;
	}
}
