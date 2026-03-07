using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class RestoreNavStation : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Restore Nav Station";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Right click the nav station to RESTORE it, removing some wear & tear.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Restoring is important but takes time.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "DmgVizOff";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		private void OnQuickActionButton(GUIQuickActionButton qab)
		{
			if (qab != null)
			{
				Interaction ia = qab.IA;
				if (ia != null && ia.objThem && CrewSimTut.playerShipNavStationRef && ia.objThem.strID == CrewSimTut.playerShipNavStationRef.strID && !string.IsNullOrEmpty(ia.strTitle) && ia.strTitle == "Restore")
				{
					base.Finished = true;
				}
			}
		}

		public override void AddInitialListeners()
		{
			GUIQuickBar.OnQABButtonClicked = (UnityAction<GUIQuickActionButton>)Delegate.Combine(GUIQuickBar.OnQABButtonClicked, new UnityAction<GUIQuickActionButton>(this.OnQuickActionButton));
		}

		public override void RemoveAllListeners()
		{
			GUIQuickBar.OnQABButtonClicked = (UnityAction<GUIQuickActionButton>)Delegate.Remove(GUIQuickBar.OnQABButtonClicked, new UnityAction<GUIQuickActionButton>(this.OnQuickActionButton));
		}
	}
}
