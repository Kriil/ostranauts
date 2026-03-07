using System;
using UnityEngine;

public class SeasonalMacguffin : MonoBehaviour
{
	public bool GetSeason(Vector2 date)
	{
		return date.x >= this.m_startTime.x && date.x <= this.m_endTime.x && (date.x != this.m_startTime.x || date.y >= this.m_startTime.y) && (date.x != this.m_endTime.x || date.y <= this.m_endTime.y);
	}

	public Vector2 m_startTime = new Vector2(12f, 1f);

	public Vector2 m_endTime = new Vector2(12f, 31f);
}
