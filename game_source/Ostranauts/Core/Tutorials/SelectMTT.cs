using System;
using Ostranauts.UI.MegaToolTip;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class SelectMTT : TutorialBeat
	{
		public SelectMTT()
		{
			CrewSim.OnRightClick.Invoke(null);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Open the Mega Tooltip.";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Right-click any object to open the Mega Tooltip (MTT).";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "MTT opened.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "SelectCompartment";
			}
		}

		private void OnMTTSelectionChanged(CondOwner CO)
		{
			if (CO != null)
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			TooltipPreviewButton.OnPreviewButtonClicked.AddListener(new UnityAction<CondOwner>(this.OnMTTSelectionChanged));
		}

		public override void RemoveAllListeners()
		{
			TooltipPreviewButton.OnPreviewButtonClicked.RemoveListener(new UnityAction<CondOwner>(this.OnMTTSelectionChanged));
		}
	}
}
