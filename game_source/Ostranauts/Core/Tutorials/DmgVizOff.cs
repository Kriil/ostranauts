using System;

namespace Ostranauts.Core.Tutorials
{
	public class DmgVizOff : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Hide Wear & Tear Info";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press '" + GUIActionKeySelector.commandTogglePowerVis.KeyName + "' and then the DEFAULT button to hide wear & damage overlay again.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Much better.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "NavUseShow";
			}
		}

		public override void Process()
		{
			if (!MonoSingleton<GUIItemList>.Instance.m_display)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
