using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	public class ObjectivesHost : MonoBehaviour
	{
		private void Awake()
		{
			if (this._tfObjectiveContainer == null)
			{
				this._tfObjectiveContainer = base.transform;
			}
			if (ObjectivesHost.OnObjectiveAnimationFinished == null)
			{
				ObjectivesHost.OnObjectiveAnimationFinished = new OnObjectiveAnimationFinishedEvent();
			}
			if (ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent == null)
			{
				ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent = new OnObjectiveSingleAnimationFinishedEvent();
			}
			if (this._scrollRect)
			{
				this.iconCG = UnityEngine.Object.Instantiate<RectTransform>(this._objectiveIconTray, this._scrollRect.transform).GetComponent<CanvasGroup>();
				this.iconRect = (this.iconCG.transform as RectTransform);
			}
			if (this._alarmLayoutGroup)
			{
				this.iconCG = UnityEngine.Object.Instantiate<RectTransform>(this._objectiveIconTray, base.transform).GetComponent<CanvasGroup>();
				this.iconRect = (this.iconCG.transform as RectTransform);
			}
		}

		protected virtual void Start()
		{
			ObjectiveTracker.OnShowTutorialToggled.AddListener(new UnityAction<bool>(this.OnShowTutorialsToggled));
			ObjectivesHost.Mode displayMode = this._displayMode;
			if (displayMode != ObjectivesHost.Mode.NewOnly)
			{
				if (displayMode != ObjectivesHost.Mode.ActiveOnly)
				{
					if (displayMode == ObjectivesHost.Mode.InactiveOnly)
					{
						ObjectiveTracker.OnObjectiveComplete.AddListener(new UnityAction<Objective>(this.OnObjectiveAdded));
						ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.OnObjectiveAdded));
					}
				}
				else
				{
					ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.OnObjectiveAdded));
					ObjectiveTracker.OnObjectiveComplete.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
					ObjectiveTracker.OnShipSubscriptionUpdated.AddListener(new UnityAction<List<Objective>>(this.OnShipSubscriptionUpdated));
				}
			}
			else
			{
				ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.OnObjectiveAdded));
				ObjectiveTracker.OnObjectiveComplete.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
				ObjectiveTracker.OnObjectiveClosed.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
				ObjectiveTracker.OnShipSubscriptionUpdated.AddListener(new UnityAction<List<Objective>>(this.OnShipSubscriptionUpdated));
			}
		}

		protected virtual void OnDestroy()
		{
			ObjectiveTracker.OnObjectiveAdded.RemoveListener(new UnityAction<Objective>(this.OnObjectiveAdded));
			ObjectiveTracker.OnObjectiveComplete.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnObjectiveClosed.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnObjectiveComplete.RemoveListener(new UnityAction<Objective>(this.OnObjectiveAdded));
			ObjectiveTracker.OnShipSubscriptionUpdated.RemoveListener(new UnityAction<List<Objective>>(this.OnShipSubscriptionUpdated));
			ObjectiveTracker.OnShowTutorialToggled.RemoveListener(new UnityAction<bool>(this.OnShowTutorialsToggled));
		}

		protected virtual void OnShowTutorialsToggled(bool show)
		{
			RectTransform componentInParent = this._tfObjectiveContainer.GetComponentInParent<RectTransform>();
			if (componentInParent != null)
			{
				base.StartCoroutine(this.RefreshContainerLayout(componentInParent));
			}
		}

		private IEnumerator RefreshContainerLayout(RectTransform rect)
		{
			yield return null;
			if (rect != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
			}
			yield break;
		}

		protected virtual void OnObjectiveClosed(Objective obj)
		{
			if (obj == null)
			{
				return;
			}
			foreach (KeyValuePair<GameObject, Objective> keyValuePair in this._goObjectiveMap)
			{
				if (keyValuePair.Value.Matches(obj))
				{
					this._goObjectiveMap.Remove(keyValuePair.Key);
					if (obj.CO != null)
					{
						obj.Highlight = false;
					}
					UnityEngine.Object.Destroy(keyValuePair.Key.gameObject);
					break;
				}
			}
		}

		protected virtual void OnObjectiveAdded(Objective obj)
		{
			if ((this._displayMode != ObjectivesHost.Mode.InactiveOnly && obj.Finished) || (this._displayMode == ObjectivesHost.Mode.InactiveOnly && !obj.Finished) || obj is AlarmObjective)
			{
				return;
			}
			if (this._displayMode == ObjectivesHost.Mode.InactiveOnly && !string.IsNullOrEmpty(obj.strPlotName) && PlotManager.GetActivePlot(obj.strPlotName) != null)
			{
				return;
			}
			foreach (Objective objective in this._goObjectiveMap.Values)
			{
				if (objective.Matches(obj))
				{
					return;
				}
			}
			bool flag = !obj.bTutorial || ObjectiveTracker.ShowTutorials;
			ObjectivePanel objectivePanel;
			if (obj.strPlotName != null)
			{
				objectivePanel = UnityEngine.Object.Instantiate<ObjectivePlotPanel>(this._objectivePlotPrefab, this._tfObjectiveContainer);
				if (this._displayMode == ObjectivesHost.Mode.ActiveOnly)
				{
					objectivePanel.GetComponent<ObjectivePlotPanel>().SetData(obj, delegate()
					{
						MonoSingleton<ObjectiveTracker>.Instance.RemoveObjective(obj, ObjectiveTracker.REASON_DISMISSED, true);
					});
				}
				else
				{
					objectivePanel.GetComponent<ObjectivePlotPanel>().SetData(obj, this._displayMode == ObjectivesHost.Mode.NewOnly, this._displayMode == ObjectivesHost.Mode.NewOnly);
				}
			}
			else
			{
				objectivePanel = UnityEngine.Object.Instantiate<ObjectivePanel>(this._objectivePanelPrefab, this._tfObjectiveContainer);
				if (this._displayMode == ObjectivesHost.Mode.ActiveOnly)
				{
					objectivePanel.SetData(obj, delegate()
					{
						MonoSingleton<ObjectiveTracker>.Instance.RemoveObjective(obj, ObjectiveTracker.REASON_DISMISSED, true);
					}, true);
				}
				else
				{
					objectivePanel.SetData(obj, this._displayMode == ObjectivesHost.Mode.NewOnly);
				}
			}
			if (this is GUIAlarmObjectiveSidebar || this is GUIObjectiveSidebar)
			{
				objectivePanel.sidebar = this;
				if (obj.bTutorial && obj.InfoNodeToOpen != null)
				{
					objectivePanel.showInfoIcon = true;
				}
				else if (objectivePanel._tutorialInfoIcon)
				{
					objectivePanel._tutorialInfoIcon.SetActive(false);
				}
				objectivePanel.ResizeForText();
				if (flag)
				{
					objectivePanel.transform.SetParent(this._scrollRect.transform);
				}
				if (this is GUIObjectiveSidebar)
				{
					GUIObjectiveSidebar guiobjectiveSidebar = (GUIObjectiveSidebar)this;
					guiobjectiveSidebar.mostRecentSidebarPanel = objectivePanel;
					objectivePanel.scrollRect = this._scrollRect;
					if (this._scrollRect.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHide)
					{
						this._scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
					}
				}
			}
			else if (objectivePanel.transform.parent)
			{
				objectivePanel.transform.SetSiblingIndex(objectivePanel.transform.parent.childCount - 1);
			}
			this._goObjectiveMap.Add(objectivePanel.gameObject, obj);
			if (flag)
			{
				AudioManager.am.PlayAudioEmitter("UIObjectiveListItem", false, false);
			}
		}

		protected void OnShipSubscriptionUpdated(List<Objective> activeObjects)
		{
			for (int i = this._goObjectiveMap.Count - 1; i >= 0; i--)
			{
				GameObject key = this._goObjectiveMap.ElementAt(i).Key;
				if (key != null)
				{
					UnityEngine.Object.Destroy(key.gameObject);
				}
			}
			this._goObjectiveMap.Clear();
			foreach (Objective obj in activeObjects)
			{
				this.OnObjectiveAdded(obj);
			}
		}

		public static OnObjectiveAnimationFinishedEvent OnObjectiveAnimationFinished;

		public static OnObjectiveSingleAnimationFinishedEvent OnObjectiveSingleAnimationFinishedEvent;

		[SerializeField]
		protected ObjectivePanel _objectivePanelPrefab;

		[SerializeField]
		protected ObjectivePlotPanel _objectivePlotPrefab;

		[SerializeField]
		protected RectTransform _objectiveIconTray;

		[SerializeField]
		protected ScrollRect _scrollRect;

		[SerializeField]
		protected VerticalLayoutGroup _alarmLayoutGroup;

		[SerializeField]
		protected Transform _tfObjectiveContainer;

		[SerializeField]
		protected ObjectivesHost.Mode _displayMode = ObjectivesHost.Mode.ActiveOnly;

		protected CanvasGroup iconCG;

		protected RectTransform iconRect;

		[NonSerialized]
		public ObjectivePanel mousedOverPanel;

		protected readonly Dictionary<GameObject, Objective> _goObjectiveMap = new Dictionary<GameObject, Objective>();

		protected enum Mode
		{
			NewOnly,
			ActiveOnly,
			InactiveOnly
		}
	}
}
