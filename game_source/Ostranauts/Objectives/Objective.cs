using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Tutorials;

namespace Ostranauts.Objectives
{
	// Runtime objective model. Represents standard goals, plot-backed goals, and
	// tutorial beats, and knows how to serialize itself into JsonObjective.
	public class Objective
	{
		// Basic objective constructor for condition-trigger-backed goals.
		public Objective(CondOwner coTarget, string strDisplayName, string strCT)
		{
			this.bTutorial = false;
			this.CO = coTarget;
			this.strCT = strCT;
			this.strDisplayName = strDisplayName;
			this.fTimeStart = CrewSim.fTotalGameSecUnscaled;
			this.aLinkedCOs = new List<string>();
			if (Info.tutorialKeys.ContainsKey(strDisplayName))
			{
				this.InfoNodeToOpen = Info.tutorialKeys[strDisplayName];
			}
		}

		// Plot-backed objective constructor used when a plot phase becomes a tracker entry.
		public Objective(JsonPlotSave jps)
		{
			this.bTutorial = false;
			this.aLinkedCOs = new List<string>();
			CondOwner condOwner = null;
			DataHandler.mapCOs.TryGetValue(jps.strCOFocusID, out condOwner);
			this.CO = condOwner;
			foreach (KeyValuePair<string, string> keyValuePair in jps.dictCOTokens)
			{
				if (!this.aLinkedCOs.Contains(keyValuePair.Value))
				{
					this.aLinkedCOs.AddRange(jps.dictCOTokens.Values);
				}
			}
			this.strDisplayName = jps.GetPlotFriendlyName();
			this.strDisplayDesc = jps.GetCurrentPhaseTitle(this.COFocus, "<color=#FFCC00>", "</color>");
			this.strDisplayDescComplete = this.strDisplayDesc;
			this.strPlotName = jps.strPlotName;
			this.fTimeStart = CrewSim.fTotalGameSecUnscaled;
		}

		public Objective(CondOwner coTarget, string strDisplayName, string strCT, string shipCOID = "") : this(coTarget, strDisplayName, strCT)
		{
			this.strShipCOID = shipCOID;
		}

		// Helper that wraps a TutorialBeat in an Objective and registers it with the tracker.
		public static Objective MakeTutorialObjective(TutorialBeat tutorialBeat)
		{
			Objective objective = new Objective(tutorialBeat.COTarget, tutorialBeat.ObjectiveName, tutorialBeat.CTString);
			objective.strDisplayDesc = tutorialBeat.ObjectiveDesc;
			objective.strDisplayDescComplete = tutorialBeat.ObjectiveDescComplete;
			objective.bTutorial = true;
			objective.TutorialBeat = tutorialBeat;
			MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
			return objective;
		}

		// Duplicate-detection helper used by ObjectiveTracker.
		public bool Matches(Objective obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj.TutorialBeat != null || this.TutorialBeat != null)
			{
				return obj.TutorialBeat == this.TutorialBeat;
			}
			return obj.strCOID == this.strCOID && obj.strCT == this.strCT && obj.strPlotName == this.strPlotName && obj.strDisplayName == this.strDisplayName;
		}

		// Serializes the live objective into the save payload form.
		public JsonObjective GetJSON()
		{
			JsonObjective jsonObjective = new JsonObjective();
			if (this.CO != null)
			{
				jsonObjective.objectiveCOStrName = this.CO.strName;
			}
			jsonObjective.objectiveCOStrID = this.strCOID;
			if (this.CT != null)
			{
				jsonObjective.objectiveCTStrName = this.CT.strName;
			}
			if (this.CTFocus != null)
			{
				jsonObjective.objectiveCTFocusStrName = this.CTFocus.strName;
			}
			jsonObjective.strCOFocusID = this.strCOFocusID;
			jsonObjective.shipCOID = this.strShipCOID;
			jsonObjective.strDisplayDesc = this.strDisplayDesc;
			jsonObjective.strDisplayName = this.strDisplayName;
			jsonObjective.strPlotName = this.strPlotName;
			jsonObjective.bTutorial = this.bTutorial;
			jsonObjective.fTimeStart = this.fTimeStart;
			jsonObjective.bFinished = this.Finished;
			if (this.TutorialBeat != null)
			{
				jsonObjective.strTutorialBeat = this.TutorialBeat.ToString();
			}
			return jsonObjective;
		}

		// Mirrors objective highlight state onto the focus CondOwner (and linked COs).
		public bool Highlight
		{
			get
			{
				CondOwner cofocus = this.COFocus;
				if (cofocus == null)
				{
					cofocus = this.CO;
				}
				if (cofocus != null && cofocus.HighlightObjective)
				{
					return cofocus.HighlightObjective;
				}
				foreach (string key in this.aLinkedCOs)
				{
					if (DataHandler.mapCOs.TryGetValue(key, out cofocus) && cofocus.HighlightObjective)
					{
						return true;
					}
				}
				return false;
			}
			set
			{
				string b = string.Empty;
				CondOwner cofocus = this.COFocus;
				if (cofocus == null)
				{
					cofocus = this.CO;
				}
				if (cofocus != null)
				{
					cofocus.HighlightObjective = value;
					b = cofocus.strID;
				}
				foreach (string text in this.aLinkedCOs)
				{
					if (!(text == b))
					{
						if (DataHandler.mapCOs.TryGetValue(text, out cofocus) && cofocus.HighlightObjective)
						{
							cofocus.HighlightObjective = false;
						}
					}
				}
			}
		}

		public CondOwner COFocus
		{
			get
			{
				if (this.tutorialObjectiveOverrideID != null)
				{
					CondOwner result = null;
					if (DataHandler.mapCOs.TryGetValue(this.tutorialObjectiveOverrideID, out result))
					{
						return result;
					}
				}
				if (this.CTFocus != null)
				{
					if (this.strCOFocusID == null && CrewSim.shipCurrentLoaded != null)
					{
						List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(this.CTFocus, true, true, true);
						if (cos.Count > 0)
						{
							this.strCOFocusID = cos[0].strID;
							this.coFocus = cos[0];
						}
					}
					if ((this.coFocus == null || this.coFocus.ship == null) && this.strCOFocusID != null)
					{
						DataHandler.mapCOs.TryGetValue(this.strCOFocusID, out this.coFocus);
					}
				}
				if (this.coFocus != null)
				{
					return this.coFocus;
				}
				if (this.strCOFocusID != null)
				{
					this.coFocus = this.CO;
					this.strCOFocusID = this.strCOID;
				}
				return this.CO;
			}
		}

		public bool Complete
		{
			get
			{
				if (!string.IsNullOrEmpty(this.strPlotName))
				{
					return !PlotManager.IsPlotActive(this.strPlotName);
				}
				if (this.TutorialBeat != null)
				{
					return this.TutorialBeat.Finished;
				}
				return this.CT == null || this.CT.Triggered(this.CO, null, true);
			}
		}

		private CondTrigger CT
		{
			get
			{
				if (this.ct == null && this.strCT != null)
				{
					this.ct = DataHandler.GetCondTrigger(this.strCT);
				}
				return this.ct;
			}
			set
			{
				if (value == null)
				{
					return;
				}
				this.strCT = value.strName;
				this.ct = DataHandler.GetCondTrigger(this.strCT);
			}
		}

		public CondTrigger CTFocus
		{
			get
			{
				if (this.ctFocus == null && this.strCTFocus != null)
				{
					this.ctFocus = DataHandler.GetCondTrigger(this.strCTFocus);
				}
				return this.ctFocus;
			}
			set
			{
				if (value == null)
				{
					return;
				}
				this.strCTFocus = value.strName;
				this.ctFocus = DataHandler.GetCondTrigger(this.strCTFocus);
			}
		}

		public CondOwner CO
		{
			get
			{
				if ((this.co == null || this.co.ship == null) && this.strCOID != null)
				{
					DataHandler.mapCOs.TryGetValue(this.strCOID, out this.co);
				}
				return this.co;
			}
			set
			{
				if (value == null)
				{
					return;
				}
				this.strCOID = value.strID;
				this.co = value;
			}
		}

		public override string ToString()
		{
			return this.strDisplayName;
		}

		public string strCOID;

		public string strCOFocusID;

		public string strCT;

		public string strCTFocus;

		public List<string> aLinkedCOs;

		public TutorialBeat TutorialBeat;

		public string strDisplayName;

		public string strDisplayDesc;

		public string strDisplayDescComplete;

		public bool bTutorial;

		public float fTimeStart;

		public string strShipCOID;

		public string tutorialObjectiveOverrideID;

		private CondOwner co;

		private CondOwner coFocus;

		private CondTrigger ct;

		private CondTrigger ctFocus;

		public bool Finished;

		public string strPlotName;

		public const string COLOR_TARGET01 = "<color=#FFCC00>";

		public const string COLOR_TARGET02 = "</color>";

		public InfoNode InfoNodeToOpen;
	}
}
