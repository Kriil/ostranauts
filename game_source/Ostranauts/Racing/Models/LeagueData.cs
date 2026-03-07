using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Racing.Models
{
	public class LeagueData
	{
		public LeagueData()
		{
		}

		public LeagueData(JsonRacingLeagueSave jSave)
		{
			if (jSave == null || jSave.aRacerSaves == null)
			{
				return;
			}
			this.JsonRacingLeague = DataHandler.GetRaceLeague(jSave.strLeagueName);
			this.Participants = new List<Racer>();
			foreach (JsonRacerSave jRacer in jSave.aRacerSaves)
			{
				this.Participants.Add(new Racer(jRacer));
			}
		}

		public string LeagueName
		{
			get
			{
				return this.JsonRacingLeague.strName;
			}
		}

		public void AddResult(List<RaceResult> results)
		{
			if (results == null || this.Participants == null)
			{
				return;
			}
			using (List<Racer>.Enumerator enumerator = this.Participants.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Racer participant = enumerator.Current;
					RaceResult raceResult = results.FirstOrDefault((RaceResult x) => x.PilotName == participant.Name);
					if (raceResult != null)
					{
						participant.RaceResults.Add(raceResult);
					}
				}
			}
		}

		public List<RaceResult> GetRaceResults(string trackName)
		{
			return this.Participants.SelectMany((Racer x) => from y in x.RaceResults
			where y.TrackName == trackName
			select y).ToList<RaceResult>();
		}

		public string GetCurrentTrack()
		{
			if (this.JsonRacingLeague == null || this.JsonRacingLeague.aTracks == null)
			{
				return null;
			}
			string[] aTracks = this.JsonRacingLeague.aTracks;
			for (int i = 0; i < aTracks.Length; i++)
			{
				string trackName = aTracks[i];
				if (!this.Participants.Any((Racer x) => x.RaceResults.Any((RaceResult y) => y.TrackName == trackName)))
				{
					return trackName;
				}
			}
			return null;
		}

		public JsonRacingLeagueSave GetJson()
		{
			JsonRacingLeagueSave jsonRacingLeagueSave = new JsonRacingLeagueSave
			{
				strLeagueName = this.LeagueName
			};
			if (this.Participants != null)
			{
				List<JsonRacerSave> list = new List<JsonRacerSave>();
				foreach (Racer racer in this.Participants)
				{
					JsonRacerSave json = racer.GetJson();
					if (json != null)
					{
						list.Add(json);
					}
				}
				jsonRacingLeagueSave.aRacerSaves = list.ToArray();
			}
			return jsonRacingLeagueSave;
		}

		public JsonRacingLeague JsonRacingLeague;

		public List<Racer> Participants;
	}
}
