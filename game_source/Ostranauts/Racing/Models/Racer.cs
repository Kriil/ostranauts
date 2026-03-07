using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Racing.Models
{
	public class Racer
	{
		public Racer(string name, double statPiloting)
		{
			this.Name = name;
			this._statPiloting = statPiloting;
		}

		public Racer(CondOwner co)
		{
			this._coPilot = co;
			this.Name = co.strName;
		}

		public Racer(JsonRacerSave jRacer)
		{
			this.Name = jRacer.strPilotName;
			this._statPiloting = jRacer.fStatPiloting;
			this.RaceResults = new List<RaceResult>();
			if (jRacer.aRaceResults != null)
			{
				foreach (JsonRaceResultSave jResult in jRacer.aRaceResults)
				{
					this.RaceResults.Add(new RaceResult(jResult));
				}
			}
		}

		public double StatPiloting
		{
			get
			{
				if (this._coPilot == null)
				{
					this._coPilot = DataHandler.GetCondOwner(null, this.Name, null, true, null, null, null, null);
				}
				if (this._coPilot != null)
				{
					this._statPiloting = this._coPilot.GetCondAmount("StatPiloting");
				}
				return this._statPiloting;
			}
		}

		public int Points
		{
			get
			{
				if (this.RaceResults == null)
				{
					return 0;
				}
				return this.RaceResults.Sum((RaceResult result) => result.PointsEarned);
			}
		}

		public bool IsValid()
		{
			if (Racer._ctAlive == null)
			{
				Racer._ctAlive = DataHandler.GetCondTrigger("TIsHumanAwake");
			}
			if (this._coPilot == null)
			{
				this._coPilot = DataHandler.GetCondOwner(null, this.Name, null, true, null, null, null, null);
			}
			return this._coPilot != null && Racer._ctAlive.Triggered(this._coPilot, null, true);
		}

		public JsonRacerSave GetJson()
		{
			JsonRacerSave jsonRacerSave = new JsonRacerSave();
			jsonRacerSave.strPilotName = this.Name;
			jsonRacerSave.fStatPiloting = this._statPiloting;
			if (this.RaceResults != null)
			{
				List<JsonRaceResultSave> list = new List<JsonRaceResultSave>();
				foreach (RaceResult raceResult in this.RaceResults)
				{
					list.Add(raceResult.GetJson());
				}
				jsonRacerSave.aRaceResults = list.ToArray();
			}
			return jsonRacerSave;
		}

		public readonly string Name;

		private CondOwner _coPilot;

		private double _statPiloting;

		private static CondTrigger _ctAlive;

		public List<RaceResult> RaceResults = new List<RaceResult>();
	}
}
