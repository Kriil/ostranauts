using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Objectives
{
	public class GUIAlarmObjectiveSidebar : ObjectivesHost
	{
		protected override void Start()
		{
			ObjectiveTracker.OnAlarm.AddListener(new UnityAction<AlarmObjective>(this.OnObjectiveAdded));
			ObjectiveTracker.OnObjectiveClosed.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			ObjectiveTracker.OnAlarm.RemoveListener(new UnityAction<AlarmObjective>(this.OnObjectiveAdded));
			ObjectiveTracker.OnObjectiveClosed.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
		}

		protected override void OnObjectiveAdded(Objective obj)
		{
			if (!(obj is AlarmObjective))
			{
				return;
			}
			foreach (GameObject gameObject in this._goObjectiveMap.Keys.ToArray<GameObject>())
			{
				if (gameObject == null)
				{
					this._goObjectiveMap.Remove(gameObject);
				}
			}
			foreach (KeyValuePair<GameObject, Objective> keyValuePair in this._goObjectiveMap)
			{
				if (!(keyValuePair.Key == null))
				{
					if (keyValuePair.Value.Matches(obj))
					{
						keyValuePair.Key.GetComponent<ObjectivePanel>().Extend(this._objectiveDisplayDuration);
						return;
					}
				}
			}
			ObjectivePanel component = UnityEngine.Object.Instantiate<GameObject>(this._alarmObjectivePrefab, this._tfObjectiveContainer).GetComponent<ObjectivePanel>();
			component.SetData(obj, this._objectiveDisplayDuration);
			component.sidebar = this;
			component.ResizeForText();
			this._goObjectiveMap.Add(component.gameObject, obj);
		}

		private void Update()
		{
			if (this.mousedOverPanel)
			{
				RectTransform rectTransform = this.mousedOverPanel.transform as RectTransform;
				Vector2 vector;
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, CrewSim.objInstance.UICamera, out vector))
				{
					if (this.iconCG.alpha != 1f)
					{
						this.iconCG.alpha = 1f;
					}
					vector.y = Mathf.Clamp(vector.y, -rectTransform.sizeDelta.y / 2f + this.iconRect.sizeDelta.y / 2f + 5f, rectTransform.sizeDelta.y / 2f - this.iconRect.sizeDelta.y / 2f - 5f);
					this.iconRect.localPosition = new Vector3(this._alarmLayoutGroup.transform.localPosition.x + 268f, this.mousedOverPanel.transform.localPosition.y + this._alarmLayoutGroup.transform.localPosition.y + vector.y);
				}
			}
			else if (this.iconCG.alpha == 1f)
			{
				this.iconCG.alpha = 0f;
			}
		}

		protected override void OnObjectiveClosed(Objective obj)
		{
			base.OnObjectiveClosed(obj);
			if (obj is AlarmObjective)
			{
				MonoSingleton<ObjectiveTracker>.Instance.UserSquelchedAlarm(obj as AlarmObjective);
			}
		}

		[SerializeField]
		protected GameObject _alarmObjectivePrefab;

		private readonly float _objectiveDisplayDuration = 4f;
	}
}
