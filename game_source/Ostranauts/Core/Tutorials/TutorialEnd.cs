using System;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public class TutorialEnd : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Tutorial Complete";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "You have reached the end of the tutorial. From here, the stars your destination.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Good luck.";
			}
		}

		public override void Process()
		{
			this.time += Time.deltaTime;
			if (this.time > 8f)
			{
				base.Finished = true;
			}
			base.Process();
		}

		public float time;
	}
}
