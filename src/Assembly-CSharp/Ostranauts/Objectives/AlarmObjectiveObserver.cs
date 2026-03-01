using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Objectives
{
	[RequireComponent(typeof(GUILamp))]
	public class AlarmObjectiveObserver : MonoBehaviour
	{
		private void Awake()
		{
			if (this._lamp == null)
			{
				this._lamp = base.GetComponent<GUILamp>();
			}
		}

		private void Start()
		{
			ObjectiveTracker.OnAlarm.AddListener(new UnityAction<AlarmObjective>(this.OnAlarmRaised));
			ObjectiveTracker.OnObjectiveComplete.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnObjectiveClosed.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
		}

		private void OnDestroy()
		{
			ObjectiveTracker.OnAlarm.RemoveListener(new UnityAction<AlarmObjective>(this.OnAlarmRaised));
			ObjectiveTracker.OnObjectiveComplete.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnObjectiveClosed.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
		}

		private void OnObjectiveClosed(Objective obj)
		{
			if (obj == null || this._activeObjective == null)
			{
				return;
			}
			AlarmObjective alarmObjective = obj as AlarmObjective;
			if (alarmObjective == null || alarmObjective.AlarmType != this._alarmType)
			{
				return;
			}
			this._lamp.State = 0;
			this._activeObjective = null;
		}

		private void OnAlarmRaised(AlarmObjective obj)
		{
			if (obj.AlarmType != this._alarmType)
			{
				return;
			}
			this._activeObjective = obj;
			this._lamp.State = 2;
		}

		[SerializeField]
		private GUILamp _lamp;

		[SerializeField]
		private AlarmType _alarmType;

		private Objective _activeObjective;
	}
}
