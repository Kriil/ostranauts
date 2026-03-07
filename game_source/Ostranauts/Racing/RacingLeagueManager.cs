using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Racing.Models;
using Ostranauts.Ships.Commands;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

namespace Ostranauts.Racing
{
	public class RacingLeagueManager : MonoSingleton<RacingLeagueManager>
	{
		public LeagueData GetActiveLeagueForUser()
		{
			return this._activeLeagueData;
		}

		public void StartNewLeague(JsonRacingLeague jLeague, CondOwner coUser)
		{
			if (jLeague == null)
			{
				return;
			}
			List<Racer> list = this.FindRaceParticipants(jLeague.strPsPecParticipant);
			list.Add(new Racer(coUser));
			this._activeLeagueData = new LeagueData
			{
				JsonRacingLeague = jLeague,
				Participants = list
			};
		}

		public void ReportResults(JsonRaceTrack jTrack, List<LapTime> lapTimes, List<RaceResult> aiResults, bool isPractice)
		{
			RaceResult raceResult = this.ValidatePlayerTimes(jTrack, lapTimes, CrewSim.coPlayer.strName);
			this.UpdatePersonalBest(jTrack.strName, raceResult.LapTimes);
			if (isPractice)
			{
				return;
			}
			List<RaceResult> list = new List<RaceResult>();
			if (aiResults != null)
			{
				list.AddRange(aiResults);
			}
			list.Add(raceResult);
			this.ScoreRaceResults(list);
			this._activeLeagueData.AddResult(list);
			if (this._activeLeagueData.GetCurrentTrack() == null)
			{
				CrewSim.coPlayer.AddCondAmount("IsRaceLeagueFinished", 1.0, 0.0, 0f);
			}
		}

		private RaceResult ValidatePlayerTimes(JsonRaceTrack jTrack, List<LapTime> lapTimes, string playerName)
		{
			if (lapTimes.Count < jTrack.nLaps)
			{
				for (int i = lapTimes.Count; i <= jTrack.nLaps; i++)
				{
					lapTimes.Add(new LapTime
					{
						TotalTime = double.PositiveInfinity
					});
				}
			}
			return new RaceResult
			{
				TrackName = jTrack.strName,
				PilotName = playerName,
				LapTimes = lapTimes.ToArray()
			};
		}

		private List<RaceResult> GetAIResults(string trackName, List<Racer> participants)
		{
			List<RaceResult> list = new List<RaceResult>();
			JsonRaceTrack raceTrack = DataHandler.GetRaceTrack(trackName);
			if (raceTrack == null)
			{
				return null;
			}
			foreach (Racer racer in participants)
			{
				if (!(racer.Name == CrewSim.coPlayer.strName))
				{
					List<LapTime> list2 = this.CalculateAILapTimes(raceTrack, racer);
					RaceResult item = new RaceResult
					{
						TrackName = raceTrack.strName,
						PilotName = racer.Name,
						LapTimes = list2.ToArray()
					};
					list.Add(item);
				}
			}
			return list;
		}

		private List<LapTime> CalculateAILapTimes(JsonRaceTrack jTrack, Racer participant)
		{
			List<LapTime> list = new List<LapTime>();
			if (!participant.IsValid())
			{
				for (int i = 0; i < jTrack.nLaps; i++)
				{
					list.Add(new LapTime
					{
						TotalTime = double.PositiveInfinity,
						WayPointTimes = new List<Tuple<string, double>>()
					});
				}
			}
			else
			{
				LapTime adjustedLapTime = this.GetAdjustedLapTime(jTrack);
				for (int j = 0; j < jTrack.nLaps; j++)
				{
					LapTime lapTime = new LapTime();
					List<Tuple<string, double>> list2 = new List<Tuple<string, double>>();
					double num = 0.0;
					for (int k = 0; k < adjustedLapTime.WayPointTimes.Count; k++)
					{
						Tuple<string, double> tuple = adjustedLapTime.WayPointTimes[k];
						if (k == 0)
						{
							list2.Add(new Tuple<string, double>(tuple.Item1, 0.0));
						}
						else
						{
							double num2 = this.CalculateSegmentTime(tuple.Item2 - num, participant.StatPiloting);
							double num3 = num + num2;
							num = num3;
							list2.Add(new Tuple<string, double>(tuple.Item1, num3));
						}
					}
					lapTime.WayPointTimes = list2;
					if (jTrack.RaceTrackType == JsonRaceTrack.TrackType.PointToPoint)
					{
						lapTime.TotalTime = list2.Last<Tuple<string, double>>().Item2;
					}
					else
					{
						double segmentTime = adjustedLapTime.TotalTime - adjustedLapTime.WayPointTimes.Last<Tuple<string, double>>().Item2;
						double item = lapTime.WayPointTimes.Last<Tuple<string, double>>().Item2;
						double num4 = this.CalculateSegmentTime(segmentTime, participant.StatPiloting);
						lapTime.TotalTime = item + num4;
					}
					list.Add(lapTime);
				}
			}
			return list;
		}

		private double CalculateSegmentTime(double segmentTime, double statPiloting)
		{
			if (statPiloting > 1.6)
			{
				statPiloting = 1.6;
			}
			double num = segmentTime * (1.100000023841858 - 0.20000000298023224 * statPiloting) * (double)UnityEngine.Random.Range(0.98f, 1.02f);
			if (num <= 0.0)
			{
				num = segmentTime;
			}
			return num;
		}

		private LapTime GetAdjustedLapTime(JsonRaceTrack jTrack)
		{
			LapTime personalBestForTrack = this.GetPersonalBestForTrack(jTrack.strName);
			if (personalBestForTrack == null || personalBestForTrack.TotalTime <= 0.0 || double.IsPositiveInfinity(personalBestForTrack.TotalTime))
			{
				return new LapTime(jTrack);
			}
			LapTime lapTime = new LapTime();
			lapTime.TotalTime = (personalBestForTrack.TotalTime + jTrack.fAvgLapTime) / 2.0;
			List<Tuple<string, double>> list = new List<Tuple<string, double>>();
			int i = 0;
			while (i < personalBestForTrack.WayPointTimes.Count)
			{
				Tuple<string, double> playerPbWayPointTime = personalBestForTrack.WayPointTimes[i];
				JsonTrackWaypoint jsonTrackWaypoint = jTrack.aWaypoints.FirstOrDefault((JsonTrackWaypoint x) => x.ID == playerPbWayPointTime.Item1);
				if (jsonTrackWaypoint != null)
				{
					goto IL_D7;
				}
				if (i < jTrack.aWaypoints.Length)
				{
					jsonTrackWaypoint = jTrack.aWaypoints[i];
				}
				if (jsonTrackWaypoint != null)
				{
					goto IL_D7;
				}
				Debug.LogWarning("No matching WP ID! This shouldn't happen");
				IL_110:
				i++;
				continue;
				IL_D7:
				double item = (playerPbWayPointTime.Item2 + jsonTrackWaypoint.fWayPointTime) / 2.0;
				list.Add(new Tuple<string, double>(playerPbWayPointTime.Item1, item));
				goto IL_110;
			}
			lapTime.WayPointTimes = list;
			return lapTime;
		}

		private void ScoreRaceResults(List<RaceResult> results)
		{
			List<RaceResult> list = (from x in results
			orderby x.TotalTime
			select x).ToList<RaceResult>();
			for (int i = 0; i < list.Count; i++)
			{
				list[i].FinishingPosition = i + 1;
				list[i].PointsEarned = this._scorePointsKey[i];
			}
		}

		private void UpdatePersonalBest(string trackName, LapTime[] lapTimes)
		{
			if (lapTimes == null || lapTimes.Length == 0)
			{
				return;
			}
			LapTime newLapTime = (from x in lapTimes
			orderby x.TotalTime
			select x).First<LapTime>();
			this.UpdatePersonalBest(trackName, newLapTime);
		}

		public double UpdatePersonalBest(string trackName, LapTime newLapTime)
		{
			if (newLapTime == null || newLapTime.TotalTime <= 0.0 || double.IsPositiveInfinity(newLapTime.TotalTime))
			{
				return 0.0;
			}
			LapTime lapTime;
			if (!this._dictPersonalBest.TryGetValue(trackName, out lapTime))
			{
				this._dictPersonalBest[trackName] = newLapTime;
				return newLapTime.TotalTime;
			}
			if (newLapTime.TotalTime < lapTime.TotalTime)
			{
				this._dictPersonalBest[trackName] = newLapTime;
				return lapTime.TotalTime;
			}
			return 0.0;
		}

		public LapTime GetPersonalBestForTrack(string trackName)
		{
			LapTime lapTime;
			if (this._dictPersonalBest.TryGetValue(trackName, out lapTime) && lapTime != null)
			{
				return lapTime;
			}
			return null;
		}

		private List<Racer> GenerateRaceParticipants()
		{
			List<Racer> list = new List<Racer>();
			for (int i = 0; i < 9; i++)
			{
				string strGender = (UnityEngine.Random.Range(0f, 100f) >= 50f) ? "IsFemale" : "IsMale";
				string str;
				string str2;
				DataHandler.GetFullName(strGender, out str, out str2);
				list.Add(new Racer(str + " " + str2, (double)UnityEngine.Random.Range(8.5f, 11f)));
			}
			return list;
		}

		private List<Racer> FindRaceParticipants(string pspecName)
		{
			List<Racer> list = new List<Racer>();
			JsonPersonSpec personSpec = DataHandler.GetPersonSpec(pspecName);
			List<PersonSpec> persons = StarSystem.GetPersons(personSpec, null, false, null, null);
			List<Tuple<double, CondOwner>> list2 = new List<Tuple<double, CondOwner>>();
			if (persons != null)
			{
				foreach (PersonSpec personSpec2 in persons)
				{
					CondOwner co = personSpec2.GetCO();
					if (!(co == null))
					{
						list2.Add(new Tuple<double, CondOwner>(co.GetCondAmount("StatPiloting"), co));
					}
				}
			}
			foreach (Tuple<double, CondOwner> tuple in list2.Randomize<Tuple<double, CondOwner>>())
			{
				if (list.Count >= 9)
				{
					break;
				}
				list.Add(new Racer(tuple.Item2));
			}
			return list;
		}

		private void Update()
		{
			if (this._raceTrackController == null)
			{
				return;
			}
			CommandCode commandCode = this._raceTrackController.RunCommand();
			if ((commandCode & CommandCode.ResultDone) == commandCode)
			{
				this._raceTrackController = null;
			}
		}

		public void ReceiveMessage(Ship station, string[] payloads)
		{
			if (payloads == null)
			{
				return;
			}
			foreach (string text in payloads)
			{
				if (text.ToLower().Contains("practice") && this._activeLeagueData != null)
				{
					this._raceTrackController = new RaceTrackController(station, true, this._activeLeagueData.GetCurrentTrack(), null);
				}
				else if (text.ToLower().Contains("race") && this._activeLeagueData != null)
				{
					string currentTrack = this._activeLeagueData.GetCurrentTrack();
					this._raceTrackController = new RaceTrackController(station, false, currentTrack, this.GetAIResults(currentTrack, this._activeLeagueData.Participants));
				}
				else if (text.ToLower().Contains("end") && this._raceTrackController != null)
				{
					this._raceTrackController.EndSession();
					this._raceTrackController = null;
				}
			}
		}

		public static string FormatTime(double time)
		{
			if (time <= 0.0)
			{
				return "0:00.00";
			}
			int num = (int)Math.Floor(time / 60.0);
			int num2 = (int)Math.Floor(time - (double)(num * 60));
			double num3 = 100.0 * (time - Math.Floor(time));
			string text = (num > 0) ? num.ToString("N0") : "0";
			string text2 = (num2 >= 10) ? num2.ToString() : ("0" + num2.ToString());
			string text3 = (num3 >= 10.0) ? num3.ToString("N0") : ("0" + num3.ToString("N0"));
			return string.Concat(new string[]
			{
				text,
				":",
				text2,
				".",
				text3
			});
		}

		public void LeaveLeague()
		{
			if (this._raceTrackController != null)
			{
				this._raceTrackController.EndSession();
			}
			this._activeLeagueData = null;
		}

		public void InitFromSave(JsonRacingManagerSave jSave)
		{
			if (jSave == null)
			{
				return;
			}
			if (jSave.aPersonalBestTimes != null)
			{
				this._dictPersonalBest = new Dictionary<string, LapTime>();
				foreach (JsonLapTime jsonLapTime in jSave.aPersonalBestTimes)
				{
					this._dictPersonalBest[jsonLapTime.strTrackName] = new LapTime(jsonLapTime);
				}
			}
			if (jSave.objLeagueSave != null)
			{
				this._activeLeagueData = new LeagueData(jSave.objLeagueSave);
			}
			CrewSim.coPlayer.ZeroCondAmount("IsInRaceSession");
		}

		public JsonRacingManagerSave GetJson()
		{
			JsonRacingManagerSave jsonRacingManagerSave = new JsonRacingManagerSave();
			if (this._dictPersonalBest != null)
			{
				List<JsonLapTime> list = new List<JsonLapTime>();
				foreach (KeyValuePair<string, LapTime> keyValuePair in this._dictPersonalBest)
				{
					if (keyValuePair.Value != null)
					{
						JsonLapTime json = keyValuePair.Value.GetJson();
						json.strTrackName = keyValuePair.Key;
						list.Add(json);
					}
				}
				if (list.Count > 0)
				{
					jsonRacingManagerSave.aPersonalBestTimes = list.ToArray();
				}
			}
			if (this._activeLeagueData != null)
			{
				jsonRacingManagerSave.objLeagueSave = this._activeLeagueData.GetJson();
			}
			return jsonRacingManagerSave;
		}

		public static Color ColorFastestLap = new Color(0.46666667f, 0.11372549f, 0.5647059f, 1f);

		public static Color ColorGold = new Color(0.67058825f, 0.5254902f, 0.2627451f, 1f);

		public static Color ColorSilver = new Color(0.6784314f, 0.70980394f, 0.73333335f, 1f);

		public static Color ColorBronze = new Color(0.6431373f, 0.49411765f, 0.3764706f, 1f);

		public static Color ColorWaypoint = new Color(0.46875f, 0f, 0f, 0.9f);

		private LeagueData _activeLeagueData;

		private Dictionary<string, LapTime> _dictPersonalBest = new Dictionary<string, LapTime>();

		private RaceTrackController _raceTrackController;

		private readonly int[] _scorePointsKey = new int[]
		{
			10,
			6,
			4,
			3,
			2,
			1,
			0,
			0,
			0,
			0
		};

		private const int AiParticipantMax = 9;
	}
}
