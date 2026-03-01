using System;
using Ostranauts.UI.MegaToolTip;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class ReachBridgeTest : TutorialBeat
	{
		public ReachBridgeTest()
		{
			TooltipPreviewButton.OnPreviewButtonClicked.AddListener(new UnityAction<CondOwner>(this.OnMTTSelectionChanged));
		}

		public override string ObjectiveName
		{
			get
			{
				return "Access The Bridge";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Restore power to the door down the long hallway. \"Borrow\" excess conduit, if necessary.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "That's the one.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ReachBridge2";
			}
		}

		private void OnMTTSelectionChanged(CondOwner CO)
		{
			if (CO != null && CO.HasCond("TutorialDerelictConduit2"))
			{
				base.Finished = true;
			}
		}

		public override void OnFinish()
		{
			TooltipPreviewButton.OnPreviewButtonClicked.RemoveListener(new UnityAction<CondOwner>(this.OnMTTSelectionChanged));
			base.OnFinish();
		}
	}
}
