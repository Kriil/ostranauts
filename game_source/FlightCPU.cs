using System;
using System.Collections.Generic;
using UnityEngine;

public static class FlightCPU
{
	public static int GetCurrentWaypoint(float fFlightTime, List<WaypointShip> aWPs, ShipSitu objSS)
	{
		for (int i = aWPs.Count - 1; i >= 0; i--)
		{
			if (fFlightTime >= aWPs[i].fTime)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool MatchSitu(Ship objShip, ShipSitu objSSDes)
	{
		if (!FlightCPU.MatchRot(objShip.objSS, objSSDes))
		{
			return false;
		}
		objShip.objSS.vAccIn.x = objSSDes.vAccIn.x;
		objShip.objSS.vAccIn.y = objSSDes.vAccIn.y;
		return true;
	}

	private static bool MatchRot(ShipSitu objSSAct, ShipSitu objSSDes)
	{
		float f = objSSAct.fRot - objSSDes.fRot;
		float f2 = objSSAct.fW - objSSDes.fW;
		if (Mathf.Abs(f) > FlightCPU.fErrRot || Mathf.Abs(f2) > FlightCPU.fErrW)
		{
			objSSAct.fW = objSSDes.fW;
			objSSAct.fRot = objSSDes.fRot;
			return false;
		}
		return true;
	}

	public static ShipSitu GetDockingNavPoint(ShipSitu originSitu, ShipSitu targetSitu, double fEpoch, bool useTargetBo = true)
	{
		if (originSitu == null || targetSitu == null)
		{
			return null;
		}
		BodyOrbit bodyOrbit = (!useTargetBo) ? CrewSim.system.GetNearestBO(originSitu, fEpoch, false) : CrewSim.system.GetNearestBO(targetSitu, fEpoch, false);
		double vPosx = targetSitu.vPosx;
		double vPosy = targetSitu.vPosy;
		if (FlightCPU.GetDockingNavPoint(originSitu, bodyOrbit, ref vPosx, ref vPosy))
		{
			return null;
		}
		ShipSitu shipSitu = new ShipSitu();
		shipSitu.CopyFrom(targetSitu, false);
		shipSitu.vPosx = vPosx;
		shipSitu.vPosy = vPosy;
		shipSitu.LockToBO(bodyOrbit, fEpoch);
		return shipSitu;
	}

	public static bool GetDockingNavPoint(ShipSitu situ, BodyOrbit boMainOccluder, ref double fNavX, ref double fNavY)
	{
		if (situ == null || boMainOccluder == null)
		{
			return true;
		}
		float num = 2f;
		double num2 = fNavX - boMainOccluder.dXReal;
		double num3 = fNavY - boMainOccluder.dYReal;
		double num4 = MathUtils.GetMagnitude(num2, num3);
		num4 -= boMainOccluder.fRadius;
		double num5 = MathUtils.GetDistance(situ.vPosx, situ.vPosy, boMainOccluder.dXReal, boMainOccluder.dYReal);
		num5 -= boMainOccluder.fRadius;
		double num6;
		if (num4 > boMainOccluder.fRadius * 1.5 || num5 < boMainOccluder.fRadius * 2.0)
		{
			num6 = MathUtils.FirstLineSegmentCircleIntersect(situ.vPosx, situ.vPosy, fNavX, fNavY, boMainOccluder.dXReal, boMainOccluder.dYReal, boMainOccluder.fRadius);
			if (num6 >= 1.0)
			{
				return true;
			}
		}
		MathUtils.SetLength(ref num2, ref num3, boMainOccluder.fRadius);
		double num7 = boMainOccluder.dXReal + (double)num * num2;
		double num8 = boMainOccluder.dYReal + (double)num * num3;
		num6 = MathUtils.FirstLineSegmentCircleIntersect(situ.vPosx, situ.vPosy, num7, num8, boMainOccluder.dXReal, boMainOccluder.dYReal, boMainOccluder.fRadius);
		if (num6 >= 1.0)
		{
			if (num7 == fNavX && num8 == fNavY)
			{
				return true;
			}
			fNavX = num7;
			fNavY = num8;
			return false;
		}
		else
		{
			double num9 = boMainOccluder.dXReal - (double)num * num3;
			double num10 = boMainOccluder.dYReal + (double)num * num2;
			num6 = MathUtils.FirstLineSegmentCircleIntersect(situ.vPosx, situ.vPosy, num9, num10, boMainOccluder.dXReal, boMainOccluder.dYReal, boMainOccluder.fRadius);
			if (num6 >= 1.0)
			{
				if (num9 == fNavX && num10 == fNavY)
				{
					return true;
				}
				fNavX = num9;
				fNavY = num10;
				return false;
			}
			else
			{
				double num11 = boMainOccluder.dXReal + (double)num * num3;
				double num12 = boMainOccluder.dYReal - (double)num * num2;
				num6 = MathUtils.FirstLineSegmentCircleIntersect(situ.vPosx, situ.vPosy, num11, num12, boMainOccluder.dXReal, boMainOccluder.dYReal, boMainOccluder.fRadius);
				if (num6 < 1.0)
				{
					return true;
				}
				if (num11 == fNavX && num12 == fNavY)
				{
					return true;
				}
				fNavX = num11;
				fNavY = num12;
				return false;
			}
		}
	}

	private static float fErrRot = 0.03f;

	private static float fErrW = 0.03f;
}
