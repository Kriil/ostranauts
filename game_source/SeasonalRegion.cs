using System;
using System.Collections.Generic;
using UnityEngine;

public class SeasonalRegion : MonoBehaviour
{
	private void Start()
	{
	}

	private void Awake()
	{
		if (!this.m_displaying)
		{
			this.DisplayMacguffin();
		}
	}

	public void DisplayMacguffin()
	{
		Vector2 date = new Vector2((float)DateTime.Now.Month, (float)DateTime.Now.Day);
		foreach (SeasonalMacguffin seasonalMacguffin in this.m_Macguffins)
		{
			if (seasonalMacguffin.GetSeason(date))
			{
				seasonalMacguffin.gameObject.SetActive(true);
				this.m_displaying = true;
				break;
			}
		}
	}

	public List<SeasonalMacguffin> m_Macguffins = new List<SeasonalMacguffin>();

	public bool m_displaying;
}
