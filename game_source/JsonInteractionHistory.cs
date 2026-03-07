using System;
using Ostranauts.Condowner;
using Ostranauts.Core.Models;
using UnityEngine;

[Serializable]
public class JsonInteractionHistory
{
	public string strValues { get; set; }

	public string[] mapScores { get; set; }

	public InteractionHistory GetData()
	{
		InteractionHistory interactionHistory = new InteractionHistory(JsonInteractionHistory.ParseJson(this.strValues));
		if (this.mapScores != null)
		{
			foreach (string combinedString in this.mapScores)
			{
				CondScore condScore = new CondScore(JsonInteractionHistory.ParseJson(combinedString));
				if (!string.IsNullOrEmpty(condScore.strName))
				{
					interactionHistory.mapScores[condScore.strName] = condScore;
				}
			}
		}
		return interactionHistory;
	}

	public static InteractionHistoryDTO ParseJson(string combinedString)
	{
		string[] array = combinedString.Split(new char[]
		{
			'|'
		});
		if (array.Length != 3)
		{
			Debug.LogWarning("Could not parse " + combinedString);
			return null;
		}
		InteractionHistoryDTO interactionHistoryDTO = new InteractionHistoryDTO
		{
			Name = array[0]
		};
		int iterationCounter;
		if (int.TryParse(array[1], out iterationCounter))
		{
			interactionHistoryDTO.IterationCounter = iterationCounter;
		}
		else
		{
			Debug.LogWarning("Could not parse " + array[0]);
		}
		interactionHistoryDTO.TotalValue = float.Parse(array[2]);
		return interactionHistoryDTO;
	}
}
