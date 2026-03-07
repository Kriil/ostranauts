using System;
using Ostranauts.UI.MegaToolTip;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class SelectCompartment : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Check Room Atmosphere";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Right-click anywhere in the room repeatedly until the MTT displays 'Compartment'.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Compartment selected.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ExpandMTT";
			}
		}

		private void OnMTTSelectionChanged(CondOwner CO)
		{
			if (CO != null && CO.HasCond("IsRoom"))
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
