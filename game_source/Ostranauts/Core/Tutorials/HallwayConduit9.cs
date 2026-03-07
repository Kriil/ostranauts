using System;
using Ostranauts.Objectives;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit9 : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Continue To Your Ship";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "That was an instructive diversion. You've learned much. Continue onwards to your ship at the dock.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return string.Empty;
			}
		}

		public override string NextDefault
		{
			get
			{
				return null;
			}
		}

		public override void Process()
		{
			this.timer -= Time.deltaTime;
			if (this.timer <= 0f)
			{
				if (base.AssociatedObjective != null)
				{
					ObjectiveTracker.OnObjectiveClosed.Invoke(base.AssociatedObjective);
				}
				base.Finished = true;
			}
			base.Process();
		}

		public float timer = 7f;
	}
}
