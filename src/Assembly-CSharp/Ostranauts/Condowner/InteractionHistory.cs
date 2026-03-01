using System;
using System.Collections.Generic;
using Ostranauts.Core.Models;

namespace Ostranauts.Condowner
{
	public class InteractionHistory
	{
		public InteractionHistory()
		{
			this.mapScores = new Dictionary<string, CondScore>();
		}

		public InteractionHistory(string strN)
		{
			this.strName = strN;
			this.mapScores = new Dictionary<string, CondScore>();
			this.nIterations = 0;
			this.fTotalValue = 0f;
		}

		public InteractionHistory(InteractionHistoryDTO dto)
		{
			this.mapScores = new Dictionary<string, CondScore>();
			if (dto == null)
			{
				return;
			}
			this.strName = dto.Name;
			this.nIterations = dto.IterationCounter;
			this.fTotalValue = dto.TotalValue;
		}

		public string strName { get; set; }

		public int nIterations { get; set; }

		public float fTotalValue { get; set; }

		public float fAverage
		{
			get
			{
				return (this.nIterations != 0) ? (this.fTotalValue / (float)this.nIterations) : 0f;
			}
		}

		public Dictionary<string, CondScore> mapScores { get; set; }

		public InteractionHistory Clone()
		{
			InteractionHistory interactionHistory = new InteractionHistory(this.strName);
			interactionHistory.nIterations = this.nIterations;
			interactionHistory.fTotalValue = this.fTotalValue;
			foreach (KeyValuePair<string, CondScore> keyValuePair in this.mapScores)
			{
				interactionHistory.mapScores[keyValuePair.Key] = keyValuePair.Value.Clone();
			}
			return interactionHistory;
		}

		public JsonInteractionHistory GetJson()
		{
			JsonInteractionHistory jsonInteractionHistory = new JsonInteractionHistory();
			jsonInteractionHistory.strValues = string.Concat(new object[]
			{
				this.strName,
				"|",
				this.nIterations,
				"|",
				this.fTotalValue
			});
			List<string> list = new List<string>();
			foreach (CondScore condScore in this.mapScores.Values)
			{
				list.Add(string.Concat(new object[]
				{
					condScore.strName,
					"|",
					condScore.nInteractions,
					"|",
					condScore.fTotalValue
				}));
			}
			jsonInteractionHistory.mapScores = list.ToArray();
			return jsonInteractionHistory;
		}

		public void Destroy()
		{
			this.mapScores.Clear();
			this.mapScores = null;
		}

		public void AddInteractionScore(float fScore, bool bNew)
		{
			if (bNew)
			{
				this.nIterations++;
			}
			this.fTotalValue += fScore;
		}

		public void AddCondScore(string strCondition, float fScore, bool bNew)
		{
			CondScore condScore;
			if (this.mapScores.TryGetValue(strCondition, out condScore))
			{
				condScore.AddCondScore(fScore, bNew);
			}
			else
			{
				this.mapScores[strCondition] = new CondScore(strCondition);
			}
		}

		public override string ToString()
		{
			return this.strName;
		}

		public string Print(string strPrefix)
		{
			string text = string.Empty;
			foreach (string key in this.mapScores.Keys)
			{
				string text2 = text;
				text = string.Concat(new object[]
				{
					text2,
					strPrefix,
					",",
					this.strName,
					",",
					this.nIterations,
					",",
					this.fTotalValue,
					",",
					this.fAverage,
					",",
					this.mapScores[key].Print(),
					"\n"
				});
			}
			return text;
		}
	}
}
