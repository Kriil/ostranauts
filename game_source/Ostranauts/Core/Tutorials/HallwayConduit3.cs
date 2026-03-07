using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit3 : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Reinstall Conduit";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Select the Install action from the Quick Action Bar.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Placeholder mode entered.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit4";
			}
		}

		private void OnQuickActionButton(GUIQuickActionButton qab)
		{
			if (qab != null)
			{
				Interaction ia = qab.IA;
				if (ia == null)
				{
					return;
				}
				if (ia.objThem == null)
				{
					return;
				}
				if (!ia.objThem.HasCond("IsTutorialHallwayConduit"))
				{
					return;
				}
				CrewSimTut.OverrideHallwayConduit(ia.objThem);
				if (ia.strName.ToLower().Contains("install"))
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
