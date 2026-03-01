using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Core.Tutorials;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Objectives
{
	// Global objective/tutorial manager. Tracks active objectives, alarm panels,
	// ship subscriptions, mute/tutorial settings, and save/load restoration.
	public class ObjectiveTracker : MonoSingleton<ObjectiveTracker>
	{
		// Exposes the current active objective list for UI and save code.
		public List<Objective> AllObjectives
		{
			get
			{
				return this._allObjectives;
			}
		}

		public static bool MuteObjectives
		{
			get
			{
				return DataHandler.GetUserSettings() != null && DataHandler.GetUserSettings().bMuteObjectives;
			}
		}

		public static bool ShowTutorials
		{
			get
			{
				return DataHandler.GetUserSettings() != null && DataHandler.GetUserSettings().ShowTutorials();
			}
		}

		public static bool MuteInfoModalTutorials
		{
			get
			{
				return DataHandler.GetUserSettings() != null && DataHandler.GetUserSettings().bMuteInfo;
			}
		}

		// Unity setup: initializes the shared objective-related events and binds
		// local listeners for mute/tutorial setting changes.
		private new void Awake()
		{
			base.Awake();
			if (ObjectiveTracker.OnMuteToggled == null)
			{
				ObjectiveTracker.OnMuteToggled = new OnMuteToggledEvent();
			}
			ObjectiveTracker.OnMuteToggled.AddListener(new UnityAction<bool>(this.OnMuteChanged));
			if (ObjectiveTracker.OnMuteInfoToggled == null)
			{
				ObjectiveTracker.OnMuteInfoToggled = new OnMuteInfoToggledEvent();
			}
			ObjectiveTracker.OnMuteInfoToggled.AddListener(new UnityAction<bool>(this.OnMuteInfoChanged));
			if (ObjectiveTracker.OnShowTutorialToggled == null)
			{
				ObjectiveTracker.OnShowTutorialToggled = new OnShowTutorialsEvent();
			}
			ObjectiveTracker.OnShowTutorialToggled.AddListener(delegate(bool show)
			{
				this.UpdatePDAObjectiveCounter(new bool?(show));
			});
			ObjectiveTracker.OnShowTutorialToggled.AddListener(new UnityAction<bool>(this.OnShowTutorial));
			if (ObjectiveTracker.OnObjectiveClosed == null)
			{
				ObjectiveTracker.OnObjectiveClosed = new OnObjectiveUpdatedEvent();
			}
			if (ObjectiveTracker.OnObjectiveAdded == null)
			{
				ObjectiveTracker.OnObjectiveAdded = new OnObjectiveUpdatedEvent();
			}
			if (ObjectiveTracker.OnObjectiveComplete == null)
			{
				ObjectiveTracker.OnObjectiveComplete = new OnObjectiveUpdatedEvent();
			}
			if (ObjectiveTracker.OnAlarm == null)
			{
				ObjectiveTracker.OnAlarm = new OnAlarmObjectiveEvent();
			}
			if (ObjectiveTracker.OnShipSubscriptionUpdated == null)
			{
				ObjectiveTracker.OnShipSubscriptionUpdated = new OnShipSubscriptionUpdatedEvent();
			}
		}

		// Cleans up the temporary "squelch" list that suppresses repeated alarms
		// from the same CondOwner for a short time.
		private void Update()
		{
			for (int i = 0; i < this._squelchedCOs.Count; i++)
			{
				if (Time.time > this._squelchedCOs[i].Item2 + 30f)
				{
					this._squelchedCOs.RemoveAt(i);
					i--;
				}
			}
		}

		// Rebuilds objective state from a saved game, including plot-backed goals,
		// tutorial beats, and ship-subscription alarm routing.
		public void LoadObjectives(JsonGameSave jGS)
		{
			if (jGS.subscribedShips != null)
			{
				this.subscribedShips.AddRange(jGS.subscribedShips);
			}
			if (jGS.aObjectives == null)
			{
				return;
			}
			this.bSuppressDDNA = true;
			for (int i = 0; i < jGS.aObjectives.Length; i++)
			{
				JsonObjective jsonObjective = jGS.aObjectives[i];
				if (jsonObjective.strPlotName != null)
				{
					JsonPlotSave jsonPlotSave = null;
					if (jGS.aPlots != null)
					{
						foreach (JsonPlotSave jsonPlotSave2 in jGS.aPlots)
						{
							if (jsonPlotSave2.strPlotName == jsonObjective.strPlotName)
							{
								jsonPlotSave = jsonPlotSave2;
								break;
							}
						}
					}
					if (jsonPlotSave == null && jGS.aPlotsOld != null)
					{
						foreach (JsonPlotSave jsonPlotSave3 in jGS.aPlotsOld)
						{
							if (jsonPlotSave3.strPlotName == jsonObjective.strPlotName)
							{
								jsonPlotSave = jsonPlotSave3;
								break;
							}
						}
					}
					if (jsonPlotSave != null)
					{
						Objective objective = new Objective(jsonPlotSave)
						{
							bTutorial = jsonObjective.bTutorial,
							strDisplayDesc = jsonObjective.strDisplayDesc,
							strDisplayDescComplete = jsonObjective.strDisplayDescComplete,
							strCOFocusID = jsonObjective.strCOFocusID,
							Finished = jsonObjective.bFinished
						};
						if (objective.strCOFocusID == null && jsonPlotSave.strCOFocusID != null)
						{
							objective.strCOFocusID = jsonPlotSave.strCOFocusID;
						}
						objective.fTimeStart = jsonObjective.fTimeStart;
						this.AddObjective(objective);
					}
				}
				else
				{
					CondOwner condOwner = null;
					if (jsonObjective.objectiveCOStrID != null)
					{
						DataHandler.mapCOs.TryGetValue(jsonObjective.objectiveCOStrID, out condOwner);
					}
					else
					{
						Debug.Log("null objective COID on " + jsonObjective.strDisplayName + ". Falling back on player ref.");
						condOwner = CrewSim.coPlayer;
					}
					if (condOwner != null)
					{
						Objective objective2 = new Objective(condOwner, jsonObjective.strDisplayName, jsonObjective.objectiveCTStrName, jsonObjective.shipCOID)
						{
							bTutorial = jsonObjective.bTutorial,
							strDisplayDesc = jsonObjective.strDisplayDesc,
							strDisplayDescComplete = jsonObjective.strDisplayDescComplete,
							strCOFocusID = jsonObjective.strCOFocusID,
							Finished = jsonObjective.bFinished
						};
						if (jsonObjective.objectiveCTFocusStrName != null)
						{
							objective2.CTFocus = DataHandler.GetCondTrigger(jsonObjective.objectiveCTFocusStrName);
						}
						objective2.fTimeStart = jsonObjective.fTimeStart;
						if (!string.IsNullOrEmpty(jsonObjective.strTutorialBeat) && !jsonObjective.bFinished)
						{
							Type type = Type.GetType(jsonObjective.strTutorialBeat);
							if (type != null)
							{
								TutorialBeat tutorialBeat = Activator.CreateInstance(type) as TutorialBeat;
								objective2.TutorialBeat = tutorialBeat;
								CrewSimTut.TutorialBeats.Add(tutorialBeat);
							}
						}
						this.AddObjective(objective2);
					}
				}
			}
			this.bSuppressDDNA = false;
		}

		// Adds one objective if it is not a duplicate, with extra filtering for
		// alarms tied to subscribed/loaded ships.
		public void AddObjective(Objective objective)
		{
			if (objective == null)
			{
				return;
			}
			foreach (Objective objective2 in this._allObjectives)
			{
				if (objective2.Matches(objective))
				{
					return;
				}
			}
			AlarmObjective alarmObjective = objective as AlarmObjective;
			if (alarmObjective != null && (this.subscribedShips.Contains(objective.strShipCOID) || alarmObjective.ShowAlways))
			{
				if (this.COIDIsSquelched(alarmObjective.strCOID))
				{
					return;
				}
				if (CrewSim.GetLoadedShipByRegId(objective.strShipCOID) == null)
				{
					return;
				}
				foreach (AlarmObjective alarmObjective2 in this._allAlarms)
				{
					if (alarmObjective2.Matches(alarmObjective))
					{
						return;
					}
				}
				this._allAlarms.Add(alarmObjective);
				ObjectiveTracker.OnAlarm.Invoke(alarmObjective);
			}
			else if (this.subscribedShips.Contains(objective.strShipCOID) || string.IsNullOrEmpty(objective.strShipCOID))
			{
				AudioManager.am.PlayAudioEmitter("UIObjectiveNew", false, false);
				if (!this.CanShow(objective))
				{
					return;
				}
				this._allObjectives.Add(objective);
				ObjectiveTracker.OnObjectiveAdded.Invoke(objective);
				this.UpdatePDAObjectiveCounter(null);
			}
		}

		public void AddPlotObjective(JsonPlotBeat jpb, JsonPlotSave jps)
		{
			if (jpb == null || jps == null)
			{
				return;
			}
			if (!jpb.bNoticeable)
			{
				return;
			}
			if (jpb.bNoObjective)
			{
				return;
			}
			this.AddObjective(new Objective(jps));
		}

		public void CreateCrimeWarning(CondOwner coUs, string crime, bool witnessed)
		{
			if (CrewSim.GetSelectedCrew() != coUs)
			{
				return;
			}
			string text = (!witnessed) ? DataHandler.GetString("OBJV_CRIME_COMMITTING", false) : DataHandler.GetString("OBJV_CRIME_WITNESSED", false);
			text += crime.ToUpper();
			AlarmObjective objective = new AlarmObjective(AlarmType.crime, coUs, DataHandler.GetString("OBJV_CRIME_TITLE", false), text);
			this.AddObjective(objective);
		}

		private bool CanShow(Objective objective)
		{
			for (int i = this.objectiveTimeTrack.Count - 1; i >= 0; i--)
			{
				if (objective.fTimeStart - this.objectiveTimeTrack[i].timeStamp >= 10f)
				{
					break;
				}
				if (this.objectiveTimeTrack[i].identifier == objective.strDisplayName)
				{
					return false;
				}
			}
			this.objectiveTimeTrack.RemoveAll((ObjectiveTimeStamp x) => x.identifier == objective.strDisplayName);
			this.objectiveTimeTrack.Add(new ObjectiveTimeStamp
			{
				identifier = objective.strDisplayName,
				timeStamp = objective.fTimeStart
			});
			return true;
		}

		public void AddShipSubscription(string COID)
		{
			if (this.subscribedShips.Contains(COID))
			{
				return;
			}
			this.subscribedShips.Add(COID);
			this.UpdateShipSubscription(COID);
		}

		public void RemoveShipSubscription(string COID)
		{
			if (!this.subscribedShips.Contains(COID))
			{
				return;
			}
			this.subscribedShips.Remove(COID);
			this.UpdateShipSubscription(COID);
		}

		private void OnMuteChanged(bool mute)
		{
			DataHandler.GetUserSettings().bMuteObjectives = mute;
			DataHandler.SaveUserSettings();
			AudioManager.am.PlayAudioEmitter("UIObjectiveToggle", false, false);
		}

		private void OnMuteInfoChanged(bool mute)
		{
			DataHandler.GetUserSettings().bMuteInfo = mute;
			DataHandler.SaveUserSettings();
			AudioManager.am.PlayAudioEmitter("UIObjectiveToggle", false, false);
		}

		private void OnShowTutorial(bool show)
		{
			DataHandler.GetUserSettings().SetShowTutorial(show);
			DataHandler.SaveUserSettings();
			AudioManager.am.PlayAudioEmitter("UIObjectiveToggle", false, false);
		}

		private void UpdateShipSubscription(string COID)
		{
			List<Objective> list = new List<Objective>();
			foreach (string b in this.subscribedShips)
			{
				foreach (Objective objective in this._allObjectives)
				{
					if (objective.strShipCOID == b)
					{
						list.Add(objective);
					}
				}
			}
			ObjectiveTracker.OnShipSubscriptionUpdated.Invoke(list);
			this.UpdatePDAObjectiveCounter(null);
		}

		public List<Objective> GetObjectivesByName(string strName)
		{
			List<Objective> list = new List<Objective>();
			foreach (Objective objective in this._allObjectives)
			{
				if (objective.strDisplayName == strName)
				{
					list.Add(objective);
				}
			}
			return list;
		}

		public void RemoveObjective(Objective objective, string strReason, bool bCompleted)
		{
			if (objective == null)
			{
				return;
			}
			objective.Highlight = false;
			objective.Finished = bCompleted;
			if (strReason == ObjectiveTracker.REASON_DISMISSED && objective.strPlotName != null)
			{
				PlotManager.CancelPlot(objective.strPlotName);
			}
			ObjectiveTracker.OnObjectiveComplete.Invoke(objective);
			if (bCompleted)
			{
				AudioManager.am.PlayAudioEmitter("UIObjectiveComplete", false, false);
				bool flag = objective.CO != null && objective.CO.ship != null && this.subscribedShips.Contains(objective.CO.ship.strRegID);
				if (objective.bTutorial || flag)
				{
					CondOwner selectedCrew = CrewSim.GetSelectedCrew();
					if (selectedCrew != null)
					{
						string text = objective.strDisplayDescComplete;
						if (string.IsNullOrEmpty(text))
						{
							text = objective.strDisplayName;
						}
						selectedCrew.LogMessage(DataHandler.GetString("OBJECTIVE_COMPLETE", false) + text, "Good", selectedCrew.strID);
					}
				}
			}
			this.UpdatePDAObjectiveCounter(null);
		}

		public void CompleteExistingPlotObjective(JsonPlotSave jps)
		{
			Objective objective = null;
			foreach (Objective objective2 in this._allObjectives)
			{
				if (!objective2.Finished && !(objective2.strPlotName != jps.strPlotName))
				{
					this.RemoveObjective(objective2, ObjectiveTracker.REASON_COMPLETED, true);
					objective = objective2;
					break;
				}
			}
			if (objective != null && PlotManager.GetActivePlot(jps.strPlotName) != null)
			{
				this._allObjectives.Remove(objective);
			}
		}

		private void UpdatePDAObjectiveCounter(bool? show = null)
		{
			int num = 0;
			foreach (Objective objective in this._allObjectives)
			{
				if (!(objective.CO == null) && objective.CO.ship != null && !objective.Finished)
				{
					if (objective.bTutorial)
					{
						if (show == null && !ObjectiveTracker.ShowTutorials)
						{
							continue;
						}
						if (show != null && !show.Value)
						{
							continue;
						}
					}
					if (objective.CO == CrewSim.GetSelectedCrew() || objective.strPlotName != null || this.subscribedShips.Contains(objective.CO.ship.strRegID))
					{
						num++;
					}
				}
			}
			CrewSim.guiPDA.NewObjectives = num;
		}

		public bool COIDIsSquelched(string strCOID)
		{
			for (int i = 0; i < this._squelchedCOs.Count; i++)
			{
				if (this._squelchedCOs[i].Item1 == strCOID)
				{
					return true;
				}
			}
			return false;
		}

		public void UserSquelchedAlarm(AlarmObjective alarmObjective)
		{
			if (alarmObjective.strCOID == null)
			{
				return;
			}
			Tuple<string, float> tuple = null;
			for (int i = 0; i < this._squelchedCOs.Count; i++)
			{
				if (this._squelchedCOs[i].Item1 == alarmObjective.strCOID)
				{
					tuple = this._squelchedCOs[i];
				}
			}
			if (tuple == null)
			{
				this._squelchedCOs.Add(new Tuple<string, float>(alarmObjective.strCOID, Time.time));
			}
		}

		public void CheckObjective(string strCOID)
		{
			for (int i = 0; i < this._allAlarms.Count; i++)
			{
				if (!(this._allAlarms[i].strCOID != strCOID) && this._allAlarms[i].Complete)
				{
					this.RemoveObjective(this._allAlarms[i], ObjectiveTracker.REASON_COMPLETED, true);
					this._allAlarms.RemoveAt(i);
					return;
				}
			}
			for (int j = 0; j < this._allObjectives.Count; j++)
			{
				if (!this._allObjectives[j].Finished)
				{
					bool flag = false;
					bool bCompleted = false;
					string strReason = ObjectiveTracker.REASON_ABANDONED;
					if (this._allObjectives[j].strCOID == null || !DataHandler.mapCOs.ContainsKey(this._allObjectives[j].strCOID))
					{
						flag = true;
						bCompleted = true;
						strReason = ObjectiveTracker.REASON_ABANDONED;
					}
					else if (this._allObjectives[j].Complete)
					{
						flag = true;
						bCompleted = true;
						strReason = ObjectiveTracker.REASON_COMPLETED;
					}
					if (flag)
					{
						this.RemoveObjective(this._allObjectives[j], strReason, bCompleted);
						break;
					}
				}
			}
		}

		public static string REASON_ABANDONED
		{
			get
			{
				if (ObjectiveTracker._strReasonAbandoned == null)
				{
					ObjectiveTracker._strReasonAbandoned = DataHandler.GetString("OBJECTIVE_REASON_ABANDONED", false);
				}
				return ObjectiveTracker._strReasonAbandoned;
			}
		}

		public static string REASON_COMPLETED
		{
			get
			{
				if (ObjectiveTracker._strReasonCompleted == null)
				{
					ObjectiveTracker._strReasonCompleted = DataHandler.GetString("OBJECTIVE_REASON_COMPLETED", false);
				}
				return ObjectiveTracker._strReasonCompleted;
			}
		}

		public static string REASON_DISMISSED
		{
			get
			{
				if (ObjectiveTracker._strReasonDismissed == null)
				{
					ObjectiveTracker._strReasonDismissed = DataHandler.GetString("OBJECTIVE_REASON_DISMISSED", false);
				}
				return ObjectiveTracker._strReasonDismissed;
			}
		}

		public static OnShowTutorialsEvent OnShowTutorialToggled;

		public static OnMuteToggledEvent OnMuteToggled;

		public static OnMuteInfoToggledEvent OnMuteInfoToggled;

		public static OnObjectiveUpdatedEvent OnObjectiveClosed;

		public static OnObjectiveUpdatedEvent OnObjectiveComplete;

		public static OnObjectiveUpdatedEvent OnObjectiveAdded;

		public static OnAlarmObjectiveEvent OnAlarm;

		public static OnShipSubscriptionUpdatedEvent OnShipSubscriptionUpdated;

		private readonly List<Objective> _allObjectives = new List<Objective>();

		private readonly List<AlarmObjective> _allAlarms = new List<AlarmObjective>();

		private readonly List<Tuple<string, float>> _squelchedCOs = new List<Tuple<string, float>>();

		private readonly List<ObjectiveTimeStamp> objectiveTimeTrack = new List<ObjectiveTimeStamp>();

		public Objective PAXHireTemp;

		public List<string> subscribedShips = new List<string>();

		private bool bSuppressDDNA;

		private static string _strReasonAbandoned;

		private static string _strReasonCompleted;

		private static string _strReasonDismissed;
	}
}
