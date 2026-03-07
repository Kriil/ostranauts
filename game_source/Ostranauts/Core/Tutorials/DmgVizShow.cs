using System;

namespace Ostranauts.Core.Tutorials
{
	public class DmgVizShow : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Toggle Wear & Tear Info";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press '" + GUIActionKeySelector.commandTogglePowerVis.KeyName + "' and then the DAMAGE button to see how worn-out or damaged items are.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Wear & Tear Leads to Broken Items.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "RestoreNavStation";
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			return MonoSingleton<GUIItemList>.Instance.m_display;
		}

		public override void Process()
		{
			if (MonoSingleton<GUIItemList>.Instance.m_display)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
