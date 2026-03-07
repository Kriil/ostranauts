using System;
using Ostranauts.UI.MegaToolTip;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit2 : TutorialBeat
	{
		public HallwayConduit2()
		{
			this.SetPersistentRef("IsTutorialHallwayConduit", this.conduit);
			CrewSim.OnRightClick.Invoke(null);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Loose Conduit";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "There's a loose conduit in the hallway. Reinstall it for a karmic reward. Start by right-clicking it.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Conduit right-clicked.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit3";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.conduit.CO;
			}
		}

		private void OnMTTSelectionChanged(CondOwner CO)
		{
			if (CO && this.COTarget && CO.strID == this.COTarget.strID)
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

		public UniqueIDCOPair conduit = new UniqueIDCOPair();
	}
}
