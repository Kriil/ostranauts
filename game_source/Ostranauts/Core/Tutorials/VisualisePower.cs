using System;

namespace Ostranauts.Core.Tutorials
{
	public class VisualisePower : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Visualize Power Networks";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press L to bring up your vizor controls. Select the Power visualization.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Power networks visualized.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "RestorePower";
			}
		}

		public override void Process()
		{
			if (CrewSim.PowerVizVisible)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
