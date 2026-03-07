using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.ShipGUIs.Utilities;
using UnityEngine;
using Vectrosity;

namespace Ostranauts.ShipGUIs.NavStation
{
	public static class NavIcon
	{
		public static List<Vector2> Cross(float fScale, List<Vector2> aPoints = null)
		{
			float num = 12f * fScale;
			if (aPoints == null)
			{
				aPoints = new List<Vector2>();
				aPoints.Add(new Vector2(0f, num));
				aPoints.Add(new Vector2(0f, -num));
				aPoints.Add(new Vector2(-num, 0f));
				aPoints.Add(new Vector2(num, 0f));
			}
			else
			{
				aPoints[0].Set(0f, num);
				aPoints[1].Set(0f, -num);
				aPoints[2].Set(-num, 0f);
				aPoints[3].Set(num, 0f);
			}
			return aPoints;
		}

		public static List<Vector2> BracketSquared(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(-0.7f, -1.2f));
			list.Add(new Vector2(-1.7f, -1.2f));
			list.Add(new Vector2(-1.7f, -1.2f));
			list.Add(new Vector2(-1.7f, 1.2f));
			list.Add(new Vector2(-1.7f, 1.2f));
			list.Add(new Vector2(-0.7f, 1.2f));
			list.Add(new Vector2(0.7f, -1.2f));
			list.Add(new Vector2(1.7f, -1.2f));
			list.Add(new Vector2(1.7f, -1.2f));
			list.Add(new Vector2(1.7f, 1.2f));
			list.Add(new Vector2(1.7f, 1.2f));
			list.Add(new Vector2(0.7f, 1.2f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> Diamond(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(1f, 0f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> Circle(float fRad)
		{
			List<Vector2> list = new List<Vector2>();
			for (int i = 0; i < 24; i++)
			{
				float f = (float)i * 0.2617994f;
				Vector2 item = new Vector2(fRad * Mathf.Cos(f), fRad * Mathf.Sin(f));
				list.Add(item);
				if (i > 0)
				{
					list.Add(item);
				}
			}
			list.Add(list.First<Vector2>());
			return list;
		}

		public static Vector2[] Circle(float fRad, int nPoints)
		{
			Vector2[] array = new Vector2[nPoints + 1];
			for (int i = 0; i < nPoints; i++)
			{
				float f = (float)i * (6.2831855f / (float)nPoints);
				array[i] = new Vector2(fRad * Mathf.Cos(f), fRad * Mathf.Sin(f));
			}
			array[nPoints] = array[0];
			return array;
		}

		public static List<Vector2> Asterisk()
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0.7f, 0.7f));
			list.Add(new Vector2(-0.7f, -0.7f));
			list.Add(new Vector2(-0.7f, 0.7f));
			list.Add(new Vector2(0.7f, -0.7f));
			NavIcon.ConstrainRadius(ref list, (float)GUIOrbitDraw.DERELICTSIZE);
			list.Add(list[0]);
			return list;
		}

		public static List<Vector2> Bracket(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(-0.7f, -1f));
			list.Add(new Vector2(-1.5f, 0f));
			list.Add(new Vector2(-1.5f, 0f));
			list.Add(new Vector2(-0.7f, 1f));
			list.Add(new Vector2(0.7f, -1f));
			list.Add(new Vector2(1.5f, 0f));
			list.Add(new Vector2(1.5f, 0f));
			list.Add(new Vector2(0.7f, 1f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> GetSilhouette(Ship ship, float fRadiusMeters)
		{
			if (ship.SilhouettePoints == null)
			{
				ship.SilhouettePoints = SilhouetteUtility.GenerateVectorPoints(ship.FloorPlan);
			}
			List<Vector2> silhouettePoints = ship.SilhouettePoints;
			if (silhouettePoints == null || silhouettePoints.Count == 0)
			{
				return NavIcon.Ship(fRadiusMeters);
			}
			NavIcon.ConstrainRadius(ref silhouettePoints, fRadiusMeters);
			silhouettePoints.Add(silhouettePoints[0]);
			return silhouettePoints;
		}

		public static List<Vector2> Ship(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(0f, 1.5f));
			list.Add(new Vector2(-0.5f, -0.5f));
			list.Add(new Vector2(-0.5f, -0.5f));
			list.Add(new Vector2(0f, -0.3f));
			list.Add(new Vector2(0.5f, -0.5f));
			list.Add(new Vector2(0f, -0.3f));
			list.Add(new Vector2(0f, 1.5f));
			list.Add(new Vector2(0.5f, -0.5f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> ShipActiveTorch(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(0f, 1.5f));
			list.Add(new Vector2(-0.5f, -0.5f));
			list.Add(new Vector2(-0.5f, -0.5f));
			list.Add(new Vector2(0f, -0.3f));
			list.Add(new Vector2(0.5f, -0.5f));
			list.Add(new Vector2(0f, -0.3f));
			list.Add(new Vector2(0f, 1.5f));
			list.Add(new Vector2(0.5f, -0.5f));
			list.Add(new Vector2(0f, -0.8f));
			list.Add(new Vector2(-0.25f, -0.4f));
			list.Add(new Vector2(0f, -0.8f));
			list.Add(new Vector2(0.25f, -0.4f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> GroundStation(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(0f, -0.7f));
			list.Add(new Vector2(-0.7f, 0f));
			list.Add(new Vector2(-0.7f, 0f));
			list.Add(new Vector2(0.7f, 0f));
			list.Add(new Vector2(0.7f, 0f));
			list.Add(new Vector2(0f, -0.7f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> OrbitalStation(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(0f, 0.7f));
			list.Add(new Vector2(-0.7f, 0f));
			list.Add(new Vector2(-0.7f, 0f));
			list.Add(new Vector2(0.7f, 0f));
			list.Add(new Vector2(0.7f, 0f));
			list.Add(new Vector2(0f, 0.7f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> OrbitalStationUnfinished(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(0.6f, 0.4f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(-0.6f, 0.4f));
			list.Add(new Vector2(-0.6f, -0.4f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0.6f, -0.4f));
			list.Add(new Vector2(0f, 0.7f));
			list.Add(new Vector2(-0.3f, 0.4f));
			list.Add(new Vector2(0.3f, 0.4f));
			list.Add(new Vector2(0f, 0.7f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> GroundStationUnfinished(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(0.6f, 0.4f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(-0.6f, 0.4f));
			list.Add(new Vector2(-0.6f, -0.4f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0.6f, -0.4f));
			list.Add(new Vector2(0f, -0.7f));
			list.Add(new Vector2(-0.3f, -0.4f));
			list.Add(new Vector2(0.3f, -0.4f));
			list.Add(new Vector2(0f, -0.7f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> OtherKnown(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(-1f, 1f));
			list.Add(new Vector2(-1f, -1f));
			list.Add(new Vector2(-1f, -1f));
			list.Add(new Vector2(1f, -1f));
			list.Add(new Vector2(1f, -1f));
			list.Add(new Vector2(1f, 1f));
			list.Add(new Vector2(1f, 1f));
			list.Add(new Vector2(-1f, 1f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static List<Vector2> Outpost(float fRadiusMeters)
		{
			List<Vector2> list = new List<Vector2>();
			list.Add(new Vector2(-1f, 1f));
			list.Add(new Vector2(-1f, -1f));
			list.Add(new Vector2(-1f, -1f));
			list.Add(new Vector2(1f, -1f));
			list.Add(new Vector2(1f, -1f));
			list.Add(new Vector2(1f, 1f));
			list.Add(new Vector2(1f, 1f));
			list.Add(new Vector2(-1f, 1f));
			list.Add(new Vector2(-1f, 0f));
			list.Add(new Vector2(-2f, 0f));
			list.Add(new Vector2(1f, 0f));
			list.Add(new Vector2(2f, 0f));
			list.Add(new Vector2(0f, 1f));
			list.Add(new Vector2(0f, 2f));
			list.Add(new Vector2(0f, -1f));
			list.Add(new Vector2(0f, -2f));
			NavIcon.ConstrainRadius(ref list, fRadiusMeters);
			return list;
		}

		public static VectorLine SetupVectorLine(string strName, Color c, GameObject go, List<Vector2> aVerts)
		{
			VectorLine vectorLine = new VectorLine(strName, aVerts, GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
			vectorLine.color = c;
			vectorLine.SetCanvas(go, false);
			return vectorLine;
		}

		private static void ConstrainRadius(ref List<Vector2> aPoints, float fDesiredRadius)
		{
			float num = 0f;
			for (int i = 0; i < aPoints.Count; i++)
			{
				num = Math.Max(num, aPoints[i].magnitude);
			}
			if (num > 0f)
			{
				for (int j = 0; j < aPoints.Count; j++)
				{
					List<Vector2> list;
					int index;
					(list = aPoints)[index = j] = list[index] * (fDesiredRadius / num);
				}
			}
		}
	}
}
