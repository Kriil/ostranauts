using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.ShipGUIs.Utilities;
using UnityEngine;

[Serializable]
public class JsonRaceTrack
{
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strDescription { get; set; }

	public string strTrackType { get; set; }

	public int nLaps { get; set; }

	public float fOrbitHeightKM { get; set; }

	public JsonTrackWaypoint[] aWaypoints { get; set; }

	public double fAvgLapTime { get; set; }

	public JsonRaceTrack.TrackType RaceTrackType
	{
		get
		{
			if (string.IsNullOrEmpty(this.strTrackType) || !Enum.IsDefined(typeof(JsonRaceTrack.TrackType), this.strTrackType))
			{
				return JsonRaceTrack.TrackType.Circuit;
			}
			return (JsonRaceTrack.TrackType)Enum.Parse(typeof(JsonRaceTrack.TrackType), this.strTrackType);
		}
	}

	public Texture GetTrackTexture()
	{
		List<Vector2> list = new List<Vector2>();
		foreach (JsonTrackWaypoint jsonTrackWaypoint in this.aWaypoints)
		{
			list.Add(new Vector2(jsonTrackWaypoint.fAx, jsonTrackWaypoint.fAy));
			list.Add(new Vector2(jsonTrackWaypoint.fBx, jsonTrackWaypoint.fBy));
		}
		float num = list.Max((Vector2 x) => x.x);
		float num2 = list.Min((Vector2 x) => x.x);
		float num3 = list.Max((Vector2 x) => x.y);
		float num4 = list.Min((Vector2 x) => x.y);
		int num5 = Mathf.FloorToInt(256f / (num - num2 + 2f));
		int num6 = Mathf.FloorToInt(256f / (num3 - num4 + 2f));
		List<Vector2> floorVectors = SilhouetteUtility.ScaleFloorplan(list, (num5 >= num6) ? num6 : num5);
		return SilhouetteUtility.GenerateTexture(floorVectors, new Color(0.76953125f, 0.26171875f, 0.01953125f, 1f), new Vector2(256f, 256f));
	}

	private void SetIds()
	{
		if (this.aWaypoints == null)
		{
			return;
		}
		for (int i = 0; i < this.aWaypoints.Length; i++)
		{
			JsonTrackWaypoint jsonTrackWaypoint = this.aWaypoints[i];
			if (string.IsNullOrEmpty(jsonTrackWaypoint.ID))
			{
				jsonTrackWaypoint.ID = this.strName + "_" + i;
			}
		}
	}

	public static void GenerateWaypointIds(Dictionary<string, JsonRaceTrack> dictRaceTracks)
	{
		if (dictRaceTracks == null)
		{
			return;
		}
		foreach (KeyValuePair<string, JsonRaceTrack> keyValuePair in dictRaceTracks)
		{
			if (keyValuePair.Value != null)
			{
				keyValuePair.Value.SetIds();
			}
		}
	}

	public enum TrackType
	{
		Circuit,
		Orientation,
		PointToPoint
	}
}
