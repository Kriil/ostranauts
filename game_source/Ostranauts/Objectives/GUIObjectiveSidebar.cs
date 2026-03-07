using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Objectives
{
	public class GUIObjectiveSidebar : ObjectivesHost
	{
		protected override void Start()
		{
			ObjectiveTracker.OnObjectiveComplete.AddListener(new UnityAction<Objective>(this.OnObjectiveCompleted));
			ObjectiveTracker.OnObjectiveClosed.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnShipSubscriptionUpdated.AddListener(new UnityAction<List<Objective>>(base.OnShipSubscriptionUpdated));
			ObjectiveTracker.OnMuteToggled.AddListener(new UnityAction<bool>(this.OnMuteToggled));
			if (!ObjectiveTracker.MuteObjectives)
			{
				ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.OnObjectiveAdded));
				ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.AnimateObjective));
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			ObjectiveTracker.OnObjectiveAdded.RemoveListener(new UnityAction<Objective>(this.AnimateObjective));
		}

		private void Update()
		{
			this._tfSpawnPosition.localPosition = -this._tfSpawnPosition.parent.localPosition;
			for (int i = 0; i < this.queuedFlyIns.Count; i++)
			{
				if (this.queuedFlyIns[i] == null)
				{
					this.queuedFlyIns.RemoveAt(i);
					i--;
				}
				else
				{
					ObjectivePanel objectivePanel = this.queuedFlyIns[i];
					objectivePanel.transform.localPosition = ((i == this.queuedFlyIns.Count - 1) ? Vector3.Lerp(objectivePanel.transform.localPosition, new Vector3(0f, this.fixedFlyinSpawnHeight - (objectivePanel.transform as RectTransform).sizeDelta.y / 2f), 0.2f) : Vector3.Lerp(objectivePanel.transform.localPosition, new Vector3((float)((this.queuedFlyIns.Count - 1 - i) * 5 + 15), this.fixedFlyinSpawnHeight - (objectivePanel.transform as RectTransform).sizeDelta.y / 2f + (float)((this.queuedFlyIns.Count - 1 - i) * 3)), 0.2f));
					float num = (this.queuedFlyIns.Count <= 1) ? this._timeAtQueueHeadSolo : this._timeAtQueueHeadWithOthers;
					if (i == this.queuedFlyIns.Count - 1 && objectivePanel.headOfQueueTimer < num)
					{
						objectivePanel.headOfQueueTimer += Time.unscaledDeltaTime;
					}
					else if (i == this.queuedFlyIns.Count - 1 && objectivePanel.headOfQueueTimer >= num)
					{
						base.StartCoroutine(this.FlyIn(this.queuedFlyIns[i]));
						this.queuedFlyIns.RemoveAt(i);
						i--;
					}
				}
			}
			if (this.mousedOverPanel)
			{
				Vector2 vector;
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._scrollRect.transform as RectTransform, Input.mousePosition, CrewSim.objInstance.UICamera, out vector))
				{
					if (this.iconCG.alpha != 1f)
					{
						this.iconCG.alpha = 1f;
					}
					RectTransform rectTransform = this._scrollRect.transform as RectTransform;
					vector.y = Mathf.Clamp(vector.y, -rectTransform.sizeDelta.y / 2f + this.iconRect.sizeDelta.y / 2f + 5f, rectTransform.sizeDelta.y / 2f - this.iconRect.sizeDelta.y / 2f - 5f);
					this.iconRect.localPosition = new Vector3(268f, vector.y);
				}
			}
			else if (this.iconCG.alpha == 1f)
			{
				this.iconCG.alpha = 0f;
			}
		}

		private void OnMuteToggled(bool mute)
		{
			if (mute)
			{
				ObjectiveTracker.OnObjectiveAdded.RemoveListener(new UnityAction<Objective>(this.OnObjectiveAdded));
				ObjectiveTracker.OnObjectiveAdded.RemoveListener(new UnityAction<Objective>(this.AnimateObjective));
			}
			else
			{
				ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.OnObjectiveAdded));
				ObjectiveTracker.OnObjectiveAdded.AddListener(new UnityAction<Objective>(this.AnimateObjective));
			}
		}

		private void OnObjectiveCompleted(Objective obj)
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
					ObjectivePanel component = keyValuePair.Key.GetComponent<ObjectivePanel>();
					if (component != null)
					{
						component.CompleteObjective();
					}
					else
					{
						UnityEngine.Object.Destroy(keyValuePair.Key);
					}
					break;
				}
			}
		}

		private void AnimateObjective(Objective obj)
		{
			if (obj == null || obj.Finished || (obj.bTutorial && !ObjectiveTracker.ShowTutorials))
			{
				return;
			}
			ObjectivePanel objectivePanel;
			if (obj.strPlotName == null)
			{
				ObjectivePanel component = UnityEngine.Object.Instantiate<GameObject>(this._objectiveFlyInPrefab, this._tfSpawnPosition).GetComponent<ObjectivePanel>();
				component.isFlyIn = true;
				component.SetData(obj, false);
				objectivePanel = component;
			}
			else
			{
				ObjectivePlotPanel component2 = UnityEngine.Object.Instantiate<GameObject>(this._objectiveFlyInPrefabPlot, this._tfSpawnPosition).GetComponent<ObjectivePlotPanel>();
				component2.isFlyIn = true;
				component2.SetData(obj, false, true);
				objectivePanel = component2;
			}
			if (objectivePanel._tutorialInfoIcon)
			{
				objectivePanel._tutorialInfoIcon.SetActive(false);
			}
			if (this.mostRecentSidebarPanel != null)
			{
				objectivePanel.sidebarCounterpartToFlyin = this.mostRecentSidebarPanel.transform;
			}
			else
			{
				Debug.LogWarning("null mostRecentSidebarPanel found, cannot set sidebarCounterpartToFlyin.");
			}
			objectivePanel.transform.localScale = this._magnifyingScale;
			objectivePanel.ResizeForText();
			this.queuedFlyIns.Insert(0, objectivePanel);
			objectivePanel.transform.SetSiblingIndex(0);
			objectivePanel._cg.interactable = false;
			objectivePanel._cg.blocksRaycasts = false;
			RectTransform rectTransform = objectivePanel.transform as RectTransform;
			int num = 0;
			Transform transform = rectTransform;
			Vector3 localPosition;
			if (num > 0)
			{
				Vector3 vector = new Vector3((float)(num * 5 + 15), this.fixedFlyinSpawnHeight - rectTransform.sizeDelta.y / 2f + (float)(num * 3));
				rectTransform.localPosition = vector;
				localPosition = vector;
			}
			else
			{
				Vector3 vector = new Vector3(0f, this.fixedFlyinSpawnHeight - rectTransform.sizeDelta.y / 2f);
				rectTransform.localPosition = vector;
				localPosition = vector;
			}
			transform.localPosition = localPosition;
		}

		private IEnumerator FlyIn(ObjectivePanel objPanel)
		{
			objPanel.transform.localScale = this._magnifyingScale;
			CanvasGroup flyinFade = objPanel._cg;
			Vector3 offset = (objPanel.transform as RectTransform).sizeDelta / 2f;
			Vector3 _destination = objPanel.transform.parent.InverseTransformPoint(this._tfObjectiveContainer.position) + offset;
			float startTime = Time.unscaledTime;
			Vector3 origin = new Vector3(0f, this.fixedFlyinSpawnHeight - (objPanel.transform as RectTransform).sizeDelta.y / 2f);
			float step = 0f;
			float fadeInStart = 0.15f;
			float fadeInSpeed = 6f;
			while (step < 1f)
			{
				step = Mathf.Min((Time.unscaledTime - startTime) / this._animationDuration, 1f);
				objPanel.transform.localPosition = Vector3.Lerp(origin, _destination, Mathf.SmoothStep(0f, 1f, step));
				objPanel.transform.localScale = Vector3.one * Mathf.SmoothStep(1.2f, 1f, step);
				float alphaStep = 1f - Mathf.Clamp01((step - fadeInStart) * fadeInSpeed);
				if (flyinFade)
				{
					flyinFade.alpha = alphaStep;
				}
				yield return null;
			}
			ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent.Invoke(objPanel);
			UnityEngine.Object.Destroy(objPanel.gameObject);
			yield break;
		}

		[SerializeField]
		private Transform _tfSpawnPosition;

		[SerializeField]
		protected GameObject _objectiveFlyInPrefab;

		[SerializeField]
		protected GameObject _objectiveFlyInPrefabPlot;

		private Vector3 _destination = Vector3.zero;

		private readonly Vector3 _magnifyingScale = new Vector3(1.2f, 1.2f, 1.2f);

		private readonly float _animationDuration = 0.55f;

		private readonly float _timeAtQueueHeadWithOthers = 0.3f;

		private readonly float _timeAtQueueHeadSolo = 0.3f;

		private readonly float fixedFlyinSpawnHeight = 280f;

		private List<ObjectivePanel> queuedFlyIns = new List<ObjectivePanel>();

		public ObjectivePanel mostRecentSidebarPanel;
	}
}
