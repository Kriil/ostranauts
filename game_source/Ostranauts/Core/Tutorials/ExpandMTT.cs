using System;
using Ostranauts.UI.MegaToolTip;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class ExpandMTT : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Expand Mega Tooltip";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click 'Show more' to view detailed information about the selected item, such as gas composition, temperature, and pressure.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "MTT expanded.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "DmgVizShow";
			}
		}

		private void OnShowMore()
		{
			base.Finished = true;
			ModuleHost.ToggleShowMore.RemoveListener(new UnityAction(this.OnShowMore));
		}

		public override void AddInitialListeners()
		{
			ModuleHost.ToggleShowMore.AddListener(new UnityAction(this.OnShowMore));
		}

		public override void RemoveAllListeners()
		{
			ModuleHost.ToggleShowMore.RemoveListener(new UnityAction(this.OnShowMore));
		}
	}
}
