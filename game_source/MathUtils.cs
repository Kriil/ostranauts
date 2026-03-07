using System;
using System.Collections.Generic;
using System.Text;
using Ostranauts.Utils.Models;
using UnityEngine;

// Shared gameplay math/time utility library.
// This mixes geometry helpers, finance/time formatting, ship motion helpers,
// and date conversion functions used by UI and simulation code.
public class MathUtils
{
	// Wraps an angle into the [0, 360) range used by most ship/UI rotation code.
	public static float NormalizeAngleDegrees(float angleDegrees)
	{
		float num = angleDegrees - 360f * Mathf.Floor(angleDegrees / 360f);
		if ((double)num < -0.0001)
		{
			Debug.Log(string.Concat(new object[]
			{
				"ERROR: Angle out of range! ",
				angleDegrees,
				" Result: ",
				num
			}));
		}
		if (num >= 360f)
		{
			num -= 360f;
		}
		return num;
	}

	// Developer sanity check for the custom epoch/month/day calendar formatting.
	public static void DebugTimeTest()
	{
		double num = 65627498854.02778;
		Debug.LogWarning(MathUtils.GetUTCFromS(num));
		int monthFromS = MathUtils.GetMonthFromS(num);
		int dayOfMonthFromS = MathUtils.GetDayOfMonthFromS(num);
		string @string = DataHandler.GetString("MONTH" + monthFromS, false);
		Debug.LogWarning(string.Concat(new object[]
		{
			monthFromS,
			" day of month",
			dayOfMonthFromS,
			" m ",
			@string
		}));
		for (int i = 0; i < 360; i++)
		{
			num += 87658.125;
			monthFromS = MathUtils.GetMonthFromS(num);
			dayOfMonthFromS = MathUtils.GetDayOfMonthFromS(num);
			@string = DataHandler.GetString("MONTH" + monthFromS, false);
			Debug.LogWarning(MathUtils.GetUTCFromS(num));
			Debug.LogWarning(string.Concat(new object[]
			{
				monthFromS,
				" day of month",
				dayOfMonthFromS,
				" m ",
				@string
			}));
		}
	}

	// Coarse orientation helpers used by placement and sprite-facing logic.
	public static bool IsRotationHorizontal(float angleDegrees)
	{
		float num = MathUtils.NormalizeAngleDegrees(angleDegrees);
		return num < 45f || (num >= 135f && (num < 225f || (num >= 315f && (num >= 360f || true))));
	}

	public static bool IsRotationVertical(float angleDegrees)
	{
		return !MathUtils.IsRotationHorizontal(angleDegrees);
	}

	// 2D vector rotation helpers.
	public static Vector2 Rotate(Vector2 v, float fRads)
	{
		return new Vector2(v.x * Mathf.Cos(fRads) - v.y * Mathf.Sin(fRads), v.x * Mathf.Sin(fRads) + v.y * Mathf.Cos(fRads));
	}

	// Unclear: this in-place double variant may reflect decompilation damage,
	// because `x` is overwritten before `y` is computed.
	public static void Rotate(ref double x, ref double y, float fRads)
	{
		x = x * (double)Mathf.Cos(fRads) - y * (double)Mathf.Sin(fRads);
		y = x * (double)Mathf.Sin(fRads) + y * (double)Mathf.Cos(fRads);
	}

	// Rescales vectors without changing direction.
	public static void SetLength(ref double u, ref double v, double desiredLength)
	{
		double num = u * u + v * v;
		if (num == 0.0)
		{
			return;
		}
		double num2 = desiredLength / Math.Sqrt(num);
		u *= num2;
		v *= num2;
	}

	public static Point SetLength(Point point, double desiredLength)
	{
		double num = point.X;
		double num2 = point.Y;
		double num3 = num * num + num2 * num2;
		if (num3 == 0.0)
		{
			return point;
		}
		double num4 = desiredLength / Math.Sqrt(num3);
		num *= num4;
		num2 *= num4;
		return new Point(num, num2);
	}

	public static void SetLength(ref float u, ref float v, float desiredLength)
	{
		float num = u * u + v * v;
		if (num == 0f)
		{
			return;
		}
		float num2 = desiredLength / Mathf.Sqrt(num);
		u *= num2;
		v *= num2;
	}

	// Computes a lateral pushback direction after collisions or close passes.
	// When the other object is a body orbit, this uses the star-system hierarchy
	// to push tangentially around the parent body instead of ship heading.
	public static Vector2 GetPushbackVector(ShipSitu objSSUs, ShipSitu objSSThem)
	{
		Vector2 vIn = new Vector2(1f, 0f);
		if (objSSThem.bIsBO)
		{
			BodyOrbit bo = CrewSim.system.GetBO(objSSThem.strBOPORShip);
			if (bo != null)
			{
				BodyOrbit bodyOrbit = null;
				Point a;
				if (CrewSim.system.dictBOHierarchy.TryGetValue(bo, out bodyOrbit))
				{
					a = objSSUs.vPos - bodyOrbit.dPosReal;
					vIn = new Vector2(Convert.ToSingle(objSSUs.vPosx - bodyOrbit.dXReal), Convert.ToSingle(objSSUs.vPosy - bodyOrbit.dYReal));
				}
				else
				{
					a = objSSUs.vPos - bo.dPosReal;
					vIn = new Vector2(Convert.ToSingle(objSSUs.vPosx - bo.dXReal), Convert.ToSingle(objSSUs.vPosy - bo.dYReal));
				}
				vIn = Quaternion.Euler(0f, 0f, 90f) * a.ToVector2();
				double angleBetweenVectors = MathUtils.GetAngleBetweenVectors(a, new Point((double)vIn.x, (double)vIn.y), false);
				if (angleBetweenVectors < 90.0)
				{
					vIn = Quaternion.Euler(0f, 0f, -90f) * a.ToVector2();
				}
			}
		}
		else
		{
			vIn.x = (float)Math.Sin((double)objSSUs.fRot);
			vIn.y = -(float)Math.Cos((double)objSSUs.fRot);
		}
		return MathUtils.NormalizeVector(vIn);
	}

	// Safer vector normalization wrapper for gameplay code.
	public static Vector2 NormalizeVector(Vector2 vIn)
	{
		float magnitude = vIn.magnitude;
		if (magnitude < 1E-05f)
		{
			vIn.x /= magnitude;
			vIn.y /= magnitude;
		}
		vIn = vIn.normalized;
		return vIn;
	}

	public static Vector2 GetPushbackVector(Ship objShipUs, Ship objShipThem)
	{
		return MathUtils.GetPushbackVector(objShipUs.objSS, objShipThem.objSS);
	}

	// Angle helper with an optional clockwise/counterclockwise 360-degree result.
	public static double GetAngleBetweenVectors(Point a, Point b, bool full360 = false)
	{
		double num = a.Dot(b);
		double num2 = Math.Acos(num / (a.magnitude * b.magnitude));
		if (full360)
		{
			double num3 = MathUtils.Cross(a, b);
			if (num3 < 0.0)
			{
				return 360.0 - num2 * 57.29577951308232;
			}
		}
		return num2 * 57.29577951308232;
	}

	// Loan/payment helpers used by the game's debt or mortgage systems.
	public static float MortgagePaymentPerShift(float fPrincipal)
	{
		float num = Mathf.Pow(1.0021f, 720f);
		return 0.0021f * fPrincipal * num / (num - 1f);
	}

	public static float MortgagePaymentPerShift(LedgerLI li)
	{
		float num = (float)(StarSystem.fEpoch - li.fTime);
		float num2 = 15552000f - num;
		int num3 = (int)(num2 / 87658.125f);
		int num4 = (int)((num2 - (float)num3 * 87658.125f) / 21600f);
		num4 += num3 * 4;
		float num5 = Mathf.Pow(1.0021f, (float)num4);
		return 0.0021f * li.fAmount * num5 / (num5 - 1f);
	}

	// Converts seconds into the game's compact duration string format.
	public static string GetDurationFromS(double dfAmount, int nPrecision = 4)
	{
		int yearFromS = MathUtils.GetYearFromS(dfAmount);
		double num = dfAmount % 31556926.0;
		int num2 = (int)(num / 87658.12777777777);
		num %= 87658.12777777777;
		int num3 = (int)(num / 3600.0);
		num %= 3600.0;
		int num4 = (int)(num / 60.0);
		num %= 60.0;
		int value = (int)num;
		MathUtils.sb.Length = 0;
		if (yearFromS > 0)
		{
			MathUtils.sb.Append(yearFromS);
			MathUtils.sb.Append("y");
			MathUtils.sb.Append(" ");
		}
		if (num2 > 0 && nPrecision >= 1)
		{
			MathUtils.sb.Append(num2);
			MathUtils.sb.Append("d");
			MathUtils.sb.Append(" ");
		}
		if (num3 > 0 && nPrecision >= 2)
		{
			MathUtils.sb.Append(num3);
			MathUtils.sb.Append("h");
			MathUtils.sb.Append(" ");
		}
		if (num4 > 0 && nPrecision >= 3)
		{
			MathUtils.sb.Append(num4);
			MathUtils.sb.Append("m");
			MathUtils.sb.Append(" ");
		}
		if (nPrecision >= 4)
		{
			MathUtils.sb.Append(value);
			MathUtils.sb.Append("s");
		}
		return MathUtils.sb.ToString();
	}

	public static void ResetTemperatureString()
	{
		MathUtils.strTemp = string.Empty;
		MathUtils.fTempLast = -1.0;
	}

	public static string GetTemperatureString(double dfAmount)
	{
		if (dfAmount == MathUtils.fTempLast && !string.IsNullOrEmpty(MathUtils.strTemp))
		{
			return MathUtils.strTemp;
		}
		MathUtils.sb.Length = 0;
		double num = dfAmount - 273.15;
		MathUtils.TemperatureUnit temperatureUnit = DataHandler.dictSettings["UserSettings"].TemperatureUnit();
		if (temperatureUnit != MathUtils.TemperatureUnit.K)
		{
			if (temperatureUnit == MathUtils.TemperatureUnit.C)
			{
				MathUtils.sb.Append(num.ToString("n2"));
				MathUtils.sb.Append("C");
			}
		}
		else
		{
			MathUtils.sb.Append(dfAmount.ToString("n2"));
			MathUtils.sb.Append("K");
		}
		MathUtils.fTempLast = dfAmount;
		MathUtils.strTemp = MathUtils.sb.ToString();
		return MathUtils.strTemp;
	}

	public static string GetUTCFromS(double dfAmount)
	{
		int yearFromS = MathUtils.GetYearFromS(dfAmount);
		double num = dfAmount % 31556926.0;
		int monthFromS = MathUtils.GetMonthFromS(dfAmount);
		num %= 2629743.8333333335;
		int dayOfMonthFromS = MathUtils.GetDayOfMonthFromS(dfAmount);
		num %= 87658.125;
		int num2 = (int)(num / 3600.0);
		num %= 3600.0;
		int num3 = (int)(num / 60.0);
		num %= 60.0;
		int num4 = (int)num;
		MathUtils.sb.Length = 0;
		if (yearFromS != MathUtils.aLastUTC[0])
		{
			MathUtils.aLastUTC[0] = yearFromS;
			MathUtils.aLastUTCStrings[0] = yearFromS.ToString("0000");
		}
		if (monthFromS != MathUtils.aLastUTC[1])
		{
			MathUtils.aLastUTC[1] = monthFromS;
			MathUtils.aLastUTCStrings[1] = monthFromS.ToString("00");
		}
		if (dayOfMonthFromS != MathUtils.aLastUTC[2])
		{
			MathUtils.aLastUTC[2] = dayOfMonthFromS;
			MathUtils.aLastUTCStrings[2] = dayOfMonthFromS.ToString("00");
		}
		if (num2 != MathUtils.aLastUTC[3])
		{
			MathUtils.aLastUTC[3] = num2;
			MathUtils.aLastUTCStrings[3] = num2.ToString("00");
		}
		if (num3 != MathUtils.aLastUTC[4])
		{
			MathUtils.aLastUTC[4] = num3;
			MathUtils.aLastUTCStrings[4] = num3.ToString("00");
		}
		if (num4 != MathUtils.aLastUTC[5])
		{
			MathUtils.aLastUTC[5] = num4;
			MathUtils.aLastUTCStrings[5] = num4.ToString("00");
		}
		MathUtils.DateFormat dateFormat = DataHandler.dictSettings["UserSettings"].DateFormat();
		if (dateFormat != MathUtils.DateFormat.YYYY_MM_DD)
		{
			if (dateFormat != MathUtils.DateFormat.DD_MM_YYYY)
			{
				if (dateFormat == MathUtils.DateFormat.MM_DD_YYYY)
				{
					MathUtils.sb.Append(MathUtils.aLastUTCStrings[1]);
					MathUtils.sb.Append("-");
					MathUtils.sb.Append(MathUtils.aLastUTCStrings[2]);
					MathUtils.sb.Append("-");
					MathUtils.sb.Append(MathUtils.aLastUTCStrings[0]);
				}
			}
			else
			{
				MathUtils.sb.Append(MathUtils.aLastUTCStrings[2]);
				MathUtils.sb.Append("-");
				MathUtils.sb.Append(MathUtils.aLastUTCStrings[1]);
				MathUtils.sb.Append("-");
				MathUtils.sb.Append(MathUtils.aLastUTCStrings[0]);
			}
		}
		else
		{
			MathUtils.sb.Append(MathUtils.aLastUTCStrings[0]);
			MathUtils.sb.Append("-");
			MathUtils.sb.Append(MathUtils.aLastUTCStrings[1]);
			MathUtils.sb.Append("-");
			MathUtils.sb.Append(MathUtils.aLastUTCStrings[2]);
		}
		MathUtils.sb.Append(" ");
		MathUtils.sb.Append(MathUtils.aLastUTCStrings[3]);
		MathUtils.sb.Append(":");
		MathUtils.sb.Append(MathUtils.aLastUTCStrings[4]);
		MathUtils.sb.Append(":");
		MathUtils.sb.Append(MathUtils.aLastUTCStrings[5]);
		return MathUtils.sb.ToString();
	}

	public static string GetTimeFromS(double dfAmount)
	{
		int yearFromS = MathUtils.GetYearFromS(dfAmount);
		double num = dfAmount % 31556926.0;
		int monthFromS = MathUtils.GetMonthFromS(dfAmount);
		num %= 2629743.8333333335;
		int dayOfMonthFromS = MathUtils.GetDayOfMonthFromS(dfAmount);
		num %= 87658.125;
		int num2 = (int)(num / 3600.0);
		num %= 3600.0;
		int num3 = (int)(num / 60.0);
		num %= 60.0;
		int num4 = (int)num;
		MathUtils.sb.Length = 0;
		if (yearFromS != MathUtils.aLastUTC[0])
		{
			MathUtils.aLastUTC[0] = yearFromS;
			MathUtils.aLastUTCStrings[0] = yearFromS.ToString("0000");
		}
		if (monthFromS != MathUtils.aLastUTC[1])
		{
			MathUtils.aLastUTC[1] = monthFromS;
			MathUtils.aLastUTCStrings[1] = monthFromS.ToString("00");
		}
		if (dayOfMonthFromS != MathUtils.aLastUTC[2])
		{
			MathUtils.aLastUTC[2] = dayOfMonthFromS;
			MathUtils.aLastUTCStrings[2] = dayOfMonthFromS.ToString("00");
		}
		if (num2 != MathUtils.aLastUTC[3])
		{
			MathUtils.aLastUTC[3] = num2;
			MathUtils.aLastUTCStrings[3] = num2.ToString("00");
		}
		if (num3 != MathUtils.aLastUTC[4])
		{
			MathUtils.aLastUTC[4] = num3;
			MathUtils.aLastUTCStrings[4] = num3.ToString("00");
		}
		if (num4 != MathUtils.aLastUTC[5])
		{
			MathUtils.aLastUTC[5] = num4;
			MathUtils.aLastUTCStrings[5] = num4.ToString("00");
		}
		MathUtils.sb.Append(MathUtils.aLastUTCStrings[3]);
		MathUtils.sb.Append(":");
		MathUtils.sb.Append(MathUtils.aLastUTCStrings[4]);
		MathUtils.sb.Append(":");
		MathUtils.sb.Append(MathUtils.aLastUTCStrings[5]);
		return MathUtils.sb.ToString();
	}

	public static int GetYearFromS(double dfAmount)
	{
		return (int)(dfAmount / 31556926.0);
	}

	public static int GetDayOfYearFromS(double dfAmount)
	{
		double num = dfAmount % 31556926.0;
		return 1 + (int)(num / 87658.125);
	}

	public static int GetDayOfMonthFromS(double dfAmount)
	{
		double num = dfAmount % 31556926.0;
		num %= 2629743.8333333335;
		return 1 + (int)(num / 87658.125);
	}

	public static int GetHourFromS(double dfAmount)
	{
		double num = dfAmount % 31556926.0;
		num %= 87658.125;
		return (int)(num / 3600.0);
	}

	public static int GetShiftFromS(double dfAmount)
	{
		int hourFromS = MathUtils.GetHourFromS(dfAmount);
		int num = (int)((float)hourFromS / 6f);
		if (num >= 4)
		{
			num = 3;
		}
		return num + 1;
	}

	public static int GetMonthFromS(double dfAmount)
	{
		double num = dfAmount % 31556926.0;
		return Mathf.CeilToInt(Convert.ToSingle(num / 2629743.8333333335));
	}

	public static string GetTimeUnits(float fAmount, string strNaN)
	{
		if (float.IsNaN(fAmount))
		{
			return strNaN;
		}
		string result = Mathf.RoundToInt(fAmount).ToString() + "s";
		float num = Mathf.Abs(fAmount);
		if (float.IsInfinity(fAmount))
		{
			result = fAmount.ToString("#.00");
		}
		else if (num > 31556926f)
		{
			result = (fAmount / 31556926f).ToString("#.00") + "y";
		}
		else if (num > 2629743.8f)
		{
			result = (fAmount / 2629743.8f).ToString("#.00") + "mo";
		}
		else if (num > 87658.125f)
		{
			result = (fAmount / 87658.125f).ToString("#.00") + "d";
		}
		else if (num > 3600f)
		{
			result = (fAmount / 3600f).ToString("#.00") + "h";
		}
		else if (num > 60f)
		{
			result = (fAmount / 60f).ToString("#.00") + "m";
		}
		return result;
	}

	public static float GetMagnitude(float dX, float dY)
	{
		return Mathf.Sqrt(dX * dX + dY * dY);
	}

	public static double GetMagnitude(double dX, double dY)
	{
		return Math.Sqrt(dX * dX + dY * dY);
	}

	public static double GetMagnitude(Point p1, Point p2)
	{
		return MathUtils.GetDistance(p1.X, p1.Y, p2.X, p2.Y);
	}

	public static float GetDistance(float x0, float y0, float x1, float y1)
	{
		return MathUtils.GetMagnitude(x1 - x0, y1 - y0);
	}

	public static int GetClosestIndex(Transform t, List<CondOwner> objects)
	{
		float num = float.PositiveInfinity;
		int result = 0;
		for (int i = 0; i < objects.Count; i++)
		{
			float distance = MathUtils.GetDistance(t.position.x, t.position.y, objects[i].tf.position.x, objects[i].tf.position.y);
			if (distance < num)
			{
				num = distance;
				result = i;
			}
		}
		return result;
	}

	public static int GetClosestIndex(Transform t, List<Transform> objects)
	{
		float num = float.PositiveInfinity;
		int result = 0;
		for (int i = 0; i < objects.Count; i++)
		{
			float distance = MathUtils.GetDistance(t.position.x, t.position.y, objects[i].position.x, objects[i].position.y);
			if (distance < num)
			{
				num = distance;
				result = i;
			}
		}
		return result;
	}

	public static float GetDistanceSquared(CondOwner coA, CondOwner coB)
	{
		return MathUtils.GetDistanceSquared(coA.tfVector2Position, coB.tfVector2Position);
	}

	public static float GetDistanceSquared(Vector2 pointA, Vector2 pointB)
	{
		return (pointB - pointA).sqrMagnitude;
	}

	public static float GetDistance(CondOwner coA, CondOwner coB)
	{
		if (coA == null || coB == null)
		{
			return -1f;
		}
		return MathUtils.GetDistance(coA.tfVector2Position.x, coA.tfVector2Position.y, coB.tfVector2Position.x, coB.tfVector2Position.y);
	}

	public static double GetDistance(double x0, double y0, double x1, double y1)
	{
		return MathUtils.GetMagnitude(x1 - x0, y1 - y0);
	}

	public static double GetDistance(ShipSitu ss1, ShipSitu ss2)
	{
		return MathUtils.GetDistance(ss1.vPosx, ss1.vPosy, ss2.vPosx, ss2.vPosy);
	}

	public static bool AreLinesIntersecting(Line line1, Line line2)
	{
		return MathUtils.AreLinesIntersecting(line1.A, line1.B, line2.A, line2.B);
	}

	public static bool AreLinesIntersecting(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
	{
		return MathUtils.AreLinesIntersecting(new Point((double)p1.x, (double)p1.y), new Point((double)p2.x, (double)p2.y), new Point((double)p3.x, (double)p3.y), new Point((double)p4.x, (double)p4.y));
	}

	public static bool AreLinesIntersecting(Point p1, Point p2, Point p3, Point p4)
	{
		bool result = false;
		double num = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
		if (num != 0.0)
		{
			double num2 = ((p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X)) / num;
			double num3 = ((p2.X - p1.X) * (p1.Y - p3.Y) - (p2.Y - p1.Y) * (p1.X - p3.X)) / num;
			if (num2 >= 0.0 && num2 <= 1.0 && num3 >= 0.0 && num3 <= 1.0)
			{
				result = true;
			}
		}
		return result;
	}

	public static Point[] FindCircleLineIntersections(double cx, double cy, double radius, Point point1, Point point2)
	{
		double num = point2.X - point1.X;
		double num2 = point2.Y - point1.Y;
		double num3 = num * num + num2 * num2;
		double num4 = 2.0 * (num * (point1.X - cx) + num2 * (point1.Y - cy));
		double num5 = (point1.X - cx) * (point1.X - cx) + (point1.Y - cy) * (point1.Y - cy) - radius * radius;
		double num6 = num4 * num4 - 4.0 * num3 * num5;
		if (num3 <= 1E-15 || num6 < 0.0)
		{
			return new Point[0];
		}
		double num7;
		if (num6 == 0.0)
		{
			num7 = -num4 / (2.0 * num3);
			return new Point[]
			{
				new Point(point1.X + num7 * num, point1.Y + num7 * num2)
			};
		}
		num7 = (-num4 + Math.Sqrt(num6)) / (2.0 * num3);
		Point point3 = new Point(point1.X + num7 * num, point1.Y + num7 * num2);
		num7 = (-num4 - Math.Sqrt(num6)) / (2.0 * num3);
		Point point4 = new Point(point1.X + num7 * num, point1.Y + num7 * num2);
		return new Point[]
		{
			point3,
			point4
		};
	}

	public static Point GetClosestPointOnLine(Point a, Point b, Point c)
	{
		Point normalized = (b - a).normalized;
		double scalar = (c - a).Dot(normalized);
		return a + normalized * scalar;
	}

	public static Point GetPointOnLine(Point a, Point b, double d)
	{
		Point normalized = (b - a).normalized;
		return normalized * d + a;
	}

	public static double GetStoppingDistance(double fVRel, double fAccel)
	{
		return fVRel * fVRel / (2.0 * fAccel);
	}

	public static string GetDistUnits(double dAmountAU)
	{
		MathUtils.sb.Length = 0;
		if (dAmountAU < 0.0)
		{
			MathUtils.sb.Append("-");
		}
		float num = Mathf.Abs((float)dAmountAU);
		float num2 = num * 149597870f;
		if (num >= 0.5f)
		{
			MathUtils.sb.Append(num.ToString("#.00"));
			MathUtils.sb.Append(" au");
		}
		else if (num2 >= 1000000f)
		{
			MathUtils.sb.Append((num2 / 1000000f).ToString("#.00"));
			MathUtils.sb.Append(" GM");
		}
		else if (num2 >= 1000f)
		{
			MathUtils.sb.Append(num2.ToString("00"));
			MathUtils.sb.Append(" km");
		}
		else if (num2 >= 1f)
		{
			MathUtils.sb.Append(num2.ToString("#.00"));
			MathUtils.sb.Append(" km");
		}
		else
		{
			MathUtils.sb.Append((num2 * 1000f).ToString("#.00"));
			MathUtils.sb.Append(" m");
		}
		return MathUtils.sb.ToString();
	}

	public static string GetTimeNAV(double dAmountS)
	{
		MathUtils.sb.Length = 0;
		if (dAmountS < 0.0)
		{
			MathUtils.sb.Append("<color=orange>???</color>");
			MathUtils.sb.Append("s");
			return MathUtils.sb.ToString();
		}
		float num = Mathf.Abs((float)dAmountS);
		if (num >= 0f)
		{
			MathUtils.sb.Append(num.ToString("n2"));
			MathUtils.sb.Append("s");
		}
		return MathUtils.sb.ToString();
	}

	public static float Clamp(float x, float low, float high)
	{
		if (x <= low)
		{
			return low;
		}
		if (high <= x)
		{
			return high;
		}
		return x;
	}

	public static double Clamp(double x, double low, double high)
	{
		if (x <= low)
		{
			return low;
		}
		if (high <= x)
		{
			return high;
		}
		return x;
	}

	public static double Max(double x, double y)
	{
		if (x <= y)
		{
			return y;
		}
		return x;
	}

	public static double Min(double x, double y)
	{
		if (x <= y)
		{
			return x;
		}
		return y;
	}

	public static int Rand(int min, int max, MathUtils.RandType nType, string strID = null)
	{
		if (MathUtils.dictRandsInt == null)
		{
			MathUtils.dictRandsInt = new Dictionary<string, int>();
		}
		int num;
		if (nType == MathUtils.RandType.Flat)
		{
			num = UnityEngine.Random.Range(min, max);
		}
		else
		{
			float num2 = UnityEngine.Random.Range(0f, 1f);
			if (nType != MathUtils.RandType.Low)
			{
				if (nType != MathUtils.RandType.High)
				{
					if (nType == MathUtils.RandType.Mid)
					{
						num2 = 2f * num2 - 1f;
						num2 = num2 * num2 * num2;
						num2 = 0.5f * num2 + 0.5f;
					}
				}
				else
				{
					num2 = 1f - num2 * num2;
				}
			}
			else
			{
				num2 *= num2;
			}
			num = MathUtils.RoundToInt((float)min + num2 * (float)(max - 1 - min));
		}
		if (strID != null)
		{
			int num3 = 100;
			while (num3 > 0 && MathUtils.dictRandsInt.ContainsKey(strID) && MathUtils.dictRandsInt[strID] == num)
			{
				num = MathUtils.Rand(min, max, nType, null);
				num3--;
			}
			MathUtils.dictRandsInt[strID] = num;
		}
		return num;
	}

	public static float Rand(float min, float max, MathUtils.RandType nType, string strID = null)
	{
		if (MathUtils.dictRandsFloat == null)
		{
			MathUtils.dictRandsFloat = new Dictionary<string, float>();
		}
		float num = UnityEngine.Random.Range(0f, 1f);
		switch (nType)
		{
		case MathUtils.RandType.Low:
			num *= num;
			break;
		case MathUtils.RandType.Mid:
			num = 2f * num - 1f;
			num = num * num * num;
			num = 0.5f * num + 0.5f;
			break;
		case MathUtils.RandType.High:
			num = 1f - num * num;
			break;
		}
		if (strID != null)
		{
			int num2 = 100;
			while (num2 > 0 && MathUtils.dictRandsFloat.ContainsKey(strID) && Mathf.Abs(MathUtils.dictRandsFloat[strID] - num) < 0.15f)
			{
				num = MathUtils.Rand(0f, 1f, nType, null);
				num2--;
			}
			MathUtils.dictRandsFloat[strID] = num;
		}
		return min + num * (max - min);
	}

	public static double Rand(double min, double max, MathUtils.RandType nType, string strID = null)
	{
		if (MathUtils.dictRandsDouble == null)
		{
			MathUtils.dictRandsDouble = new Dictionary<string, double>();
		}
		double num = (double)UnityEngine.Random.Range(0f, 1f);
		switch (nType)
		{
		case MathUtils.RandType.Low:
			num *= num;
			break;
		case MathUtils.RandType.Mid:
			num = 2.0 * num - 1.0;
			num = num * num * num;
			num = 0.5 * num + 0.5;
			break;
		case MathUtils.RandType.High:
			num = 1.0 - num * num;
			break;
		}
		if (strID != null)
		{
			int num2 = 100;
			while (num2 > 0 && MathUtils.dictRandsDouble.ContainsKey(strID) && Math.Abs(MathUtils.dictRandsDouble[strID] - num) < 0.15000000596046448)
			{
				num = (double)MathUtils.Rand(0f, 1f, nType, null);
				num2--;
			}
			MathUtils.dictRandsDouble[strID] = num;
		}
		return min + num * (max - min);
	}

	public static void ShuffleArray<T>(T[] arr)
	{
		for (int i = arr.Length - 1; i > 0; i--)
		{
			int num = UnityEngine.Random.Range(0, i + 1);
			if (num != i)
			{
				T t = arr[i];
				arr[i] = arr[num];
				arr[num] = t;
			}
		}
	}

	public static void ShuffleList<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int num = UnityEngine.Random.Range(0, i + 1);
			if (num != i)
			{
				T value = list[i];
				list[i] = list[num];
				list[num] = value;
			}
		}
	}

	public static string ColorToColorTag(Color color)
	{
		return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";
	}

	public static bool CompareEquals(double x, double y, float fThreshold = 1E-05f)
	{
		return Math.Abs(x - y) < (double)fThreshold;
	}

	public static bool CompareGT(double x, double y, float fThreshold = 1E-05f)
	{
		return y < x && !MathUtils.CompareEquals(x, y, fThreshold);
	}

	public static bool CompareLT(double x, double y, float fThreshold = 1E-05f)
	{
		return y > x && !MathUtils.CompareEquals(x, y, fThreshold);
	}

	public static bool CompareGTE(double x, double y, float fThreshold = 1E-05f)
	{
		return y < x || MathUtils.CompareEquals(x, y, fThreshold);
	}

	public static bool CompareLTE(double x, double y, float fThreshold = 1E-05f)
	{
		return y > x || MathUtils.CompareEquals(x, y, fThreshold);
	}

	public static int SolveLinear(double kConstant, double kLinear, ref double t0)
	{
		if (Math.Abs(kLinear) < 9.99994610111476E-41)
		{
			return 0;
		}
		t0 = -kConstant / kLinear;
		return 1;
	}

	public static int SolveQuadratic(double kConstant, double kLinear, double kQuadratic, ref double t0, ref double t1)
	{
		if (Math.Abs(kQuadratic) < 1.0000000031710769E-30)
		{
			return MathUtils.SolveLinear(kConstant, kLinear, ref t0);
		}
		double num = kLinear * kLinear - 4.0 * kQuadratic * kConstant;
		if (num < 0.0)
		{
			return 0;
		}
		double num2 = Math.Sqrt(num);
		if (num2 * kLinear > 0.0)
		{
			num2 = -num2;
		}
		double num3 = (-kLinear + num2) / (2.0 * kQuadratic);
		t0 = num3;
		t1 = kConstant / (kQuadratic * num3);
		return 2;
	}

	public static double FirstLineSegmentCircleIntersect(double x0, double y0, double x1, double y1, double xc, double yc, double radius)
	{
		double num = x1 - x0;
		double num2 = y1 - y0;
		double num3 = x0 - xc;
		double num4 = y0 - yc;
		double kQuadratic = num * num + num2 * num2;
		double kLinear = 2.0 * num3 * num + 2.0 * num4 * num2;
		double kConstant = num3 * num3 + num4 * num4 - radius * radius;
		double num5 = 100000000.0;
		double num6 = 100000000.0;
		MathUtils.SolveQuadratic(kConstant, kLinear, kQuadratic, ref num5, ref num6);
		if (num5 < 0.0)
		{
			num5 = 100000000.0;
		}
		if (num6 < 0.0)
		{
			num6 = 100000000.0;
		}
		return Math.Min(num5, num6);
	}

	public static double Dot(double fx, double fy, double dx, double dy)
	{
		return fx * dx + fy * dy;
	}

	public static double Dot(Point f, Point d)
	{
		return f.X * d.X + f.Y * d.Y;
	}

	public static double Cross(Point f, Point d)
	{
		return f.X * d.Y - f.Y * d.X;
	}

	public static void Swap(ref float x, ref float y)
	{
		float num = x;
		x = y;
		y = num;
	}

	public static void Swap(ref int x, ref int y)
	{
		int num = x;
		x = y;
		y = num;
	}

	public static void DictionaryKeyPlus(ref Dictionary<string, double> myDictionary, string strKey, double addend)
	{
		double num = 0.0;
		myDictionary.TryGetValue(strKey, out num);
		myDictionary[strKey] = num + addend;
	}

	public static Vector3 RotateCW(Vector3 source)
	{
		return new Vector3(source.y, -source.x, source.z);
	}

	public static void RotateCW(ref double x, ref double y)
	{
		double num = x;
		x = y;
		y = -num;
	}

	public static void RotateAngleCCW(ref double x, ref double y, double angleDegrees = 5.0)
	{
		double num = angleDegrees * 3.141592653589793 / 180.0;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		x = x * num2 - y * num3;
		y = x * num3 + y * num2;
	}

	public static void RotateCCW(ref double x, ref double y)
	{
		double num = x;
		x = -y;
		y = num;
	}

	public static void RotateAngleCW(ref double x, ref double y, double angleDegrees = 5.0)
	{
		double num = angleDegrees * 3.141592653589793 / 180.0;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		x = x * num2 + y * num3;
		y = -x * num3 + y * num2;
	}

	public static Vector3 GetClosestPosition(Vector3[] positionsToCompare, Vector3 target)
	{
		if (positionsToCompare == null || positionsToCompare.Length == 0)
		{
			return target;
		}
		Vector3 result = Vector3.zero;
		float num = float.PositiveInfinity;
		for (int i = 0; i < positionsToCompare.Length; i++)
		{
			float sqrMagnitude = (positionsToCompare[i] - target).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = positionsToCompare[i];
			}
		}
		return result;
	}

	public static int RoundToInt(float fX)
	{
		return (int)Math.Round((double)fX, MidpointRounding.AwayFromZero);
	}

	public static int RoundToInt(double fX)
	{
		return (int)Math.Round(fX, MidpointRounding.AwayFromZero);
	}

	private const float fTolerance = 0.15f;

	private static Dictionary<string, int> dictRandsInt;

	private static Dictionary<string, float> dictRandsFloat;

	private static Dictionary<string, double> dictRandsDouble;

	private static StringBuilder sb = new StringBuilder();

	private static string strTemp = string.Empty;

	private static double fTempLast = -1.0;

	private const string strDash = "-";

	private const string strColon = ":";

	private const string strSpace = " ";

	public const string strN1 = "n1";

	public const string strN2 = "n2";

	public const string str00 = "00";

	private const string str0000 = "0000";

	public const string strP00 = "#.00";

	private const string strPC = " pc";

	private const string strAU = " au";

	private const string strGM = " GM";

	private const string strKM = " km";

	private const string strM = " m";

	private const string strSS = " ss";

	private const string strMM = " mm";

	private const string strHH = " hh";

	private static int[] aLastUTC = new int[6];

	private static string[] aLastUTCStrings = new string[6];

	public enum RandType
	{
		Flat,
		Low,
		Mid,
		High
	}

	public enum DateFormat
	{
		YYYY_MM_DD,
		DD_MM_YYYY,
		MM_DD_YYYY
	}

	public enum TemperatureUnit
	{
		K,
		C
	}
}
