using System;
using System.Collections.Generic;
using Ostranauts.Utils.Models;
using UnityEngine;

// Runtime orbital-body model used by StarSystem.
// This stores the exact orbital parameters for stars, planets, moons, and even
// temporary ship-derived orbits, then computes position/velocity on demand.
public class BodyOrbit
{
	// Constructs a new orbit from template-like values during star-system generation.
	public BodyOrbit(string strName, double fPerihelionAU, double fAphelionAU, double fAriesDeg, double fEccentricity, double fOrbitalPeriodYr, double fRadiusKM, double fMassKG, double fRotationPeriodDays, BodyOrbit boParent)
	{
		this.boParent = boParent;
		float num = 87658.125f;
		if (fEccentricity == 0.0 && Math.Abs(fPerihelionAU - fAphelionAU) > 1E-07)
		{
			fEccentricity = this.CalculateEccentricity(fAphelionAU, fPerihelionAU);
		}
		this.strName = strName;
		this.fPerh = fPerihelionAU;
		this.fAph = fAphelionAU;
		this.fAxis1 = this.fPerh + this.fAph;
		this.fEcc = fEccentricity;
		this.fAxis2 = this.fAxis1 * Math.Sqrt(1.0 - this.fEcc * this.fEcc);
		this.fAngle = fAriesDeg % 360.0;
		this.fPeriod = fOrbitalPeriodYr * 31556926.0;
		if (fRotationPeriodDays == 0.0)
		{
			fRotationPeriodDays = fOrbitalPeriodYr * 365.0;
		}
		this.fRotationPeriod = fRotationPeriodDays * (double)num;
		this.fRadius = fRadiusKM / 149597872.0;
		this.fMass = fMassKG;
		this.nDrawFlagsTrack = 0;
		this.nDrawFlagsBody = 0;
		this.dTimeCalcLast = -1000.0;
		this.dVelCalcLast = -1000.0;
		this.UpdateTime(-500.0, true, true);
	}

	// Rehydrates a saved orbit from JsonBodyOrbitSave during game load.
	public BodyOrbit(JsonBodyOrbitSave jbo)
	{
		this.nDrawFlagsBody = 0;
		this.nDrawFlagsTrack = 0;
		this.strName = jbo.strName;
		this.fPerh = jbo.fPerh;
		this.fAph = jbo.fAph;
		this.fAxis1 = jbo.fAxis1;
		this.fEcc = jbo.fEcc;
		this.fAxis2 = jbo.fAxis2;
		this.fAngle = jbo.fAngle;
		this.fPeriod = jbo.fPeriod;
		this.fRadius = jbo.fRadius;
		this.fMass = jbo.fMass;
		this.fRotationPeriod = jbo.fRotationPeriod;
		this.nDrawFlagsTrack = jbo.nDrawFlagsTrack;
		this.nDrawFlagsBody = jbo.nDrawFlagsBody;
		this.fParallaxRadius = jbo.fParallaxRadius;
		if (this.fParallaxRadius == 0.0)
		{
			this.fParallaxRadius = 6.68E-09;
		}
		this.fGravParallaxRadius = jbo.fGravParallaxRadius;
		if (this.fGravParallaxRadius == 0.0)
		{
			this.fGravParallaxRadius = 6.68E-09;
		}
		this.fVisibilityRangeMod = ((jbo.fVisibilityRangeMod == 0.0) ? 1.0 : jbo.fVisibilityRangeMod);
		this.fVisibilityRangeModGrav = ((jbo.fVisibilityRangeModGrav == 0.0) ? 1.0 : jbo.fVisibilityRangeModGrav);
		this.strParallax = jbo.strParallax;
		this.strGravParallax = jbo.strGravParallax;
		if (jbo.aAtmospheres != null)
		{
			this.aAtmospheres = jbo.aAtmospheres;
		}
		this.fPeriodShift = jbo.fPeriodShift;
		if (jbo.nOrbitDirection != 0)
		{
			this.nOrbitDirection = jbo.nOrbitDirection;
		}
		this.dTimeCalcLast = -1000.0;
		this.dVelCalcLast = -1000.0;
		this.UpdateTime(0.0, true, true);
	}

	// Convenience wrappers for callers that want position/velocity as Point structs.
	public Point dPosReal
	{
		get
		{
			return new Point(this.dXReal, this.dYReal);
		}
	}

	public Point dVel
	{
		get
		{
			return new Point(this.dVelX, this.dVelY);
		}
	}

	private static double SolveBigESlow(double t, double dEccentricity)
	{
		double result = 0.0;
		double num = 100000000.0;
		for (int i = -1000; i < 3000; i++)
		{
			double num2 = (double)(i * 2) * 3.141592653589793 / 1000.0;
			double num3 = num2 - dEccentricity * Math.Sin(num2) - t;
			num3 *= num3;
			if (num > num3)
			{
				num = num3;
				result = num2;
			}
		}
		return result;
	}

	private static double SolveBigEOld(double t, double fEccentricity)
	{
		double num = t;
		for (int i = 0; i < 10; i++)
		{
			double num2 = num - fEccentricity * Math.Sin(num) - t;
			if (-1E-06 < num2 && num2 < 1E-06)
			{
				return num;
			}
			double num3 = 1.0 - fEccentricity * Math.Cos(num);
			num -= num2 / num3;
		}
		return num;
	}

	// Solves Kepler's equation for eccentric anomaly.
	// This is the main orbit solver used by UpdateTime.
	private static double SolveBigE(double meanAnomaly, double eccentricity)
	{
		double num;
		if (eccentricity < 0.8)
		{
			num = meanAnomaly;
		}
		else
		{
			num = 3.141592653589793;
		}
		for (int i = 0; i < 10; i++)
		{
			double num2 = num - eccentricity * Math.Sin(num) - meanAnomaly;
			if (Math.Abs(num2) < 1E-06)
			{
				double num3 = Math.Floor(meanAnomaly / 6.283185307179586);
				double num4 = num % 6.283185307179586;
				if (num4 < 0.0)
				{
					num4 += 6.283185307179586;
				}
				return num4 + 6.283185307179586 * num3;
			}
			double num5 = 1.0 - eccentricity * Math.Cos(num);
			if (Math.Abs(num5) < 1E-10)
			{
				num5 = ((num5 < 0.0) ? -1E-10 : 1E-10);
			}
			num -= num2 / num5;
		}
		double num6 = Math.Floor(meanAnomaly / 6.283185307179586);
		double num7 = num % 6.283185307179586;
		if (num7 < 0.0)
		{
			num7 += 6.283185307179586;
		}
		return num7 + 6.283185307179586 * num6;
	}

	// Advances the orbit to a given epoch and optionally derives velocity.
	// This is the core position update path used by StarSystem, ship guidance,
	// and any system that queries body positions or parent-relative motion.
	public void UpdateTime(double dTime, bool bCorrectTimes = true, bool bCalcV = true)
	{
		if (bCalcV)
		{
			double num = this.dVelCalcLast - dTime;
			if (num > 1.5 || num < -1.5)
			{
				this.dVelCalcLast = dTime;
				this.UpdateTime(dTime - 1.0, bCorrectTimes, false);
				double num2 = this.dXReal;
				double num3 = this.dYReal;
				this.UpdateTime(dTime, bCorrectTimes, false);
				this.dVelX = this.dXReal - num2;
				this.dVelY = this.dYReal - num3;
			}
		}
		if (this.dTimeCalcLast == dTime)
		{
			return;
		}
		double num4 = 6.283185307179586 * ((dTime + this.fPeriodShift) % this.fPeriod) / this.fPeriod;
		double num5 = num4;
		if (bCorrectTimes)
		{
			num5 = BodyOrbit.SolveBigE(num4, this.fEcc);
		}
		double num6 = this.fAxis1 / 2.0 * Math.Cos(num5);
		double dY = this.fAxis2 / 2.0 * (double)this.nOrbitDirection * Math.Sin(num5);
		this.ConvertTrackToReal(num6 - (this.fAxis1 / 2.0 - this.fPerh), dY, ref this.dXReal, ref this.dYReal);
		if (this.boParent != null)
		{
			this.boParent.UpdateTime(dTime, bCorrectTimes, false);
			this.dXReal += this.boParent.dXReal;
			this.dYReal += this.boParent.dYReal;
		}
		this.dTimeCalcLast = dTime;
	}

	// Checks whether a ship has enough energy to escape this body's gravity well.
	public bool IsEscaping(Ship ship)
	{
		Point point = ship.objSS.vPos - this.vPos;
		Point point2 = ship.objSS.vVel - this.vVel;
		double num = MathUtils.GetMagnitude(point.X, point.Y) * 149597863936.0;
		double num2 = MathUtils.GetMagnitude(point2.X, point2.Y) * 149597863936.0;
		double num3 = 0.5 * num2 * num2;
		double num4 = -6.674080038626684E-11 * this.fMass / num;
		return num3 + num4 >= 0.0;
	}

	// Builds a temporary orbit estimate from a ship's current state around a gravitating body.
	// Likely used by nav displays and path prediction rather than persistent save data.
	public static BodyOrbit CreateBOFromShip(Ship ship, BodyOrbit boGreatestGrav)
	{
		if (boGreatestGrav == null)
		{
			return null;
		}
		ship.objSS.UpdateTime(StarSystem.fEpoch, false);
		Point vPos = ship.objSS.vPos;
		Point vVel = ship.objSS.vVel;
		double magnitude = MathUtils.GetMagnitude(vPos * 149597863936.0, boGreatestGrav.vPos * 149597863936.0);
		Point v = vPos - boGreatestGrav.vPos;
		Point point = vVel - boGreatestGrav.vVel;
		point *= 149597863936.0;
		v *= 149597863936.0;
		double x = v.X * point.Y - v.Y * point.X;
		double num = 6.674080038626684E-11 * boGreatestGrav.fMass;
		double num2 = -(num * magnitude) / (magnitude * point.magnitude * point.magnitude - 2.0 * num);
		double num3 = Math.Pow(point.magnitude, 2.0) / 2.0 - num / magnitude;
		double num4 = Math.Sqrt(1.0 + 2.0 * num3 * Math.Pow(x, 2.0) / Math.Pow(num, 2.0));
		if (num4 < 0.0 || num4 >= 1.0)
		{
			return null;
		}
		Point point2 = (Math.Pow(point.magnitude, 2.0) / num - 1.0 / magnitude) * v - v.Dot(point) / num * point;
		double num5 = Math.Atan2(point2.Y, point2.X);
		num5 = 57.29577951308232 * num5;
		double num6 = num4 * num2;
		double num7 = num2 + num6;
		double num8 = num2 - num6;
		double num9 = 6.283185307179586 * Math.Sqrt(Math.Pow(num2, 3.0) / num);
		num7 /= 149597863936.0;
		num8 /= 149597863936.0;
		BodyOrbit bodyOrbit = new BodyOrbit(ship.strRegID, num8, num7, num5, num4, num9 / 31556926.0, (double)((float)ship.objSS.Size / 1000f), ship.Mass, 1.0, boGreatestGrav);
		bodyOrbit.nDrawFlagsBody = 1;
		bodyOrbit.nDrawFlagsTrack = 2;
		boGreatestGrav.UpdateTime(StarSystem.fEpoch, true, true);
		bodyOrbit.nOrbitDirection = ((!BodyOrbit.IsOrbitingClockwise(ship.objSS.vPos, ship.objSS.vVel, boGreatestGrav.vPos, boGreatestGrav.vVel)) ? 1 : -1);
		bodyOrbit.UpdateTime(StarSystem.fEpoch, true, true);
		double num10 = bodyOrbit.fPeriod - StarSystem.fEpoch % bodyOrbit.fPeriod;
		bodyOrbit.fPeriodShift = num10 + bodyOrbit.GetTimeAtOrbitPosition(ship.objSS.vPosx, ship.objSS.vPosy);
		BodyOrbit.ImprovePeriodShift(ship.objSS.vPos, bodyOrbit);
		bodyOrbit.dTimeCalcLast -= 2.0;
		bodyOrbit.dVelCalcLast -= 2.0;
		bodyOrbit.UpdateTime(StarSystem.fEpoch, true, true);
		double magnitude2 = MathUtils.GetMagnitude(ship.objSS.vVel, bodyOrbit.vVel);
		if (magnitude2 > 6.68458691177598E-10)
		{
			Debug.LogWarning("too fast " + MathUtils.GetDistUnits(magnitude2));
			return null;
		}
		return bodyOrbit;
	}

	private static bool IsOrbitingClockwise(Point shipPos, Point shipVel, Point boPos, Point boVel)
	{
		Point point = shipPos - boPos;
		Point point2 = shipVel - boVel;
		double num = point.X * point2.Y - point.Y * point2.X;
		return num < 0.0;
	}

	private static void ImprovePeriodShift(Point shipPosition, BodyOrbit boPath)
	{
		double fEpoch = StarSystem.fEpoch;
		double num = fEpoch - 600.0;
		double num2 = fEpoch + 600.0;
		double num3 = fEpoch;
		int i = 0;
		double num4 = fEpoch;
		double num5 = 99999999.0;
		while (i < 100)
		{
			boPath.UpdateTime(num3, true, true);
			double magnitude = MathUtils.GetMagnitude(boPath.vPos, shipPosition);
			if (magnitude <= 3.342293553032505E-08)
			{
				num4 = num3;
				break;
			}
			if (magnitude < num5)
			{
				num5 = magnitude;
				num4 = num3;
			}
			double num6 = num3 - (num2 - num) / 4.0;
			double num7 = num3 + (num2 - num) / 4.0;
			boPath.UpdateTime(num6, true, true);
			Point vPos = boPath.vPos;
			boPath.UpdateTime(num7, true, true);
			Point vPos2 = boPath.vPos;
			double magnitude2 = MathUtils.GetMagnitude(vPos, shipPosition);
			double magnitude3 = MathUtils.GetMagnitude(vPos2, shipPosition);
			if (magnitude2 < magnitude3)
			{
				num2 = num3;
				num3 = num6;
			}
			else
			{
				num = num3;
				num3 = num7;
			}
			if (num2 - num < 0.0001)
			{
				break;
			}
			i++;
		}
		double num8 = num4 - fEpoch;
		boPath.fPeriodShift = (boPath.fPeriodShift + num8) % boPath.fPeriod;
	}

	private double GetTimeAtOrbitPosition(double x, double y)
	{
		if (this.boParent != null)
		{
			x -= this.boParent.dXReal;
			y -= this.boParent.dYReal;
		}
		double num = this.fAngle * 0.017453292519943295;
		double x2 = x * Math.Cos(-num) - y * Math.Sin(-num);
		double y2 = x * Math.Sin(-num) + y * Math.Cos(-num);
		double num2 = Math.Atan2(y2, x2);
		if (this.nOrbitDirection == -1)
		{
			num2 = -num2;
		}
		if (num2 < 0.0)
		{
			num2 += 6.283185307179586;
		}
		double num3 = 2.0 * Math.Atan(Math.Sqrt((1.0 - this.fEcc) / (1.0 + this.fEcc)) * Math.Tan(num2 / 2.0));
		double num4 = num3 - this.fEcc * Math.Sin(num3);
		double num5 = num4 * this.fPeriod / 6.283185307179586;
		num5 %= this.fPeriod;
		if (num5 < 0.0)
		{
			num5 += this.fPeriod;
		}
		return num5;
	}

	private double CalculateEccentricity(double apoapsis, double periapsis)
	{
		return (apoapsis - periapsis) / (apoapsis + periapsis);
	}

	private void ConvertTrackToReal(double dX, double dY, ref double realX, ref double realY)
	{
		double num = this.fAngle * 0.017453292519943295;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		realX = dX * num2 - dY * num3;
		realY = dX * num3 + dY * num2;
	}

	public JsonBodyOrbitSave GetJSONSave()
	{
		JsonBodyOrbitSave jsonBodyOrbitSave = new JsonBodyOrbitSave();
		jsonBodyOrbitSave.strName = this.strName;
		jsonBodyOrbitSave.fPerh = this.fPerh;
		jsonBodyOrbitSave.fAph = this.fAph;
		jsonBodyOrbitSave.fAxis1 = this.fAxis1;
		jsonBodyOrbitSave.fEcc = this.fEcc;
		jsonBodyOrbitSave.fAxis2 = this.fAxis2;
		jsonBodyOrbitSave.fAngle = this.fAngle;
		jsonBodyOrbitSave.fPeriod = this.fPeriod;
		jsonBodyOrbitSave.fRadius = this.fRadius;
		jsonBodyOrbitSave.fMass = this.fMass;
		jsonBodyOrbitSave.fRotationPeriod = this.fRotationPeriod;
		jsonBodyOrbitSave.nDrawFlagsTrack = this.nDrawFlagsTrack;
		jsonBodyOrbitSave.nDrawFlagsBody = this.nDrawFlagsBody;
		jsonBodyOrbitSave.strParallax = this.strParallax;
		jsonBodyOrbitSave.strGravParallax = this.strGravParallax;
		jsonBodyOrbitSave.fParallaxRadius = this.fParallaxRadius;
		jsonBodyOrbitSave.fGravParallaxRadius = this.fGravParallaxRadius;
		jsonBodyOrbitSave.fVisibilityRangeMod = this.fVisibilityRangeMod;
		jsonBodyOrbitSave.fVisibilityRangeModGrav = this.fVisibilityRangeModGrav;
		jsonBodyOrbitSave.fPeriodShift = this.fPeriodShift;
		jsonBodyOrbitSave.nOrbitDirection = this.nOrbitDirection;
		if (this.aAtmospheres != null)
		{
			jsonBodyOrbitSave.aAtmospheres = this.aAtmospheres;
		}
		return jsonBodyOrbitSave;
	}

	public bool IsMoon()
	{
		return this.boParent != null && this.boParent.boParent != null;
	}

	public bool IsPlaceholder()
	{
		return this.nDrawFlagsBody == 1;
	}

	public bool IsShipOrbit()
	{
		return this.IsPlaceholder() && this.strName.Contains("-");
	}

	private double FindInAtmoDistance()
	{
		if (this.aAtmospheres != null && this.aAtmospheres.Length > 0)
		{
			int num = 0;
			for (int i = this.aAtmospheres.Length - 1; i >= 0; i--)
			{
				if (this.aAtmospheres[i].GetTotalKPA() >= BodyOrbit.AtmoKPaThreshold)
				{
					num = (int)this.aAtmospheres[i].fMaxAltitude;
					break;
				}
			}
			for (int j = num; j > 0; j -= 5)
			{
				JsonAtmosphere atmosphereAtDistance = this.GetAtmosphereAtDistance((double)((float)j / 149597870f));
				if (atmosphereAtDistance.GetTotalKPA() >= BodyOrbit.AtmoKPaThreshold)
				{
					return (double)((float)j / 149597870f);
				}
			}
		}
		return 0.0;
	}

	public JsonAtmosphere GetAtmosphereAtDistance(double distance)
	{
		if (BodyOrbit._voidAtmo == null)
		{
			BodyOrbit._voidAtmo = new JsonAtmosphere
			{
				strName = "Void",
				fTemp = 2.72548f
			};
		}
		double num = 149597872.0 * distance;
		if (this.aAtmospheres == null || this.aAtmospheres.Length == 0)
		{
			return BodyOrbit._voidAtmo;
		}
		for (int i = 0; i < this.aAtmospheres.Length; i++)
		{
			if (num <= (double)this.aAtmospheres[i].fMaxAltitude)
			{
				double num2 = (i != 0) ? ((double)this.aAtmospheres[i - 1].fMaxAltitude) : this.fRadiusKM;
				float t = Mathf.InverseLerp((float)num2, this.aAtmospheres[i].fMaxAltitude, (float)num);
				JsonAtmosphere jsonAtmosphere = (i != this.aAtmospheres.Length - 1) ? this.aAtmospheres[i + 1] : BodyOrbit._voidAtmo;
				return new JsonAtmosphere
				{
					fCO2 = Mathf.Lerp(this.aAtmospheres[i].fCO2, jsonAtmosphere.fCO2, t),
					fCH4 = Mathf.Lerp(this.aAtmospheres[i].fCH4, jsonAtmosphere.fCH4, t),
					fNH3 = Mathf.Lerp(this.aAtmospheres[i].fNH3, jsonAtmosphere.fNH3, t),
					fN2 = Mathf.Lerp(this.aAtmospheres[i].fN2, jsonAtmosphere.fN2, t),
					fH2SO4 = Mathf.Lerp(this.aAtmospheres[i].fH2SO4, jsonAtmosphere.fH2SO4, t),
					fO2 = Mathf.Lerp(this.aAtmospheres[i].fO2, jsonAtmosphere.fO2, t),
					fH2O = Mathf.Lerp(this.aAtmospheres[i].fH2O, jsonAtmosphere.fH2O, t),
					fH2 = Mathf.Lerp(this.aAtmospheres[i].fH2, jsonAtmosphere.fH2, t),
					fHe2 = Mathf.Lerp(this.aAtmospheres[i].fHe2, jsonAtmosphere.fHe2, t),
					fTemp = Mathf.Lerp(this.aAtmospheres[i].fTemp, jsonAtmosphere.fTemp, t),
					fMicrometeoroidChance = Mathf.Lerp(this.aAtmospheres[i].fMicrometeoroidChance, jsonAtmosphere.fMicrometeoroidChance, t)
				};
			}
		}
		return BodyOrbit._voidAtmo;
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.strName,
			this.dXReal,
			",",
			this.dYReal
		});
	}

	public double fRadiusKM
	{
		get
		{
			return this.fRadius * 149597872.0;
		}
	}

	public Point vPos
	{
		get
		{
			return new Point(this.dXReal, this.dYReal);
		}
	}

	public Point vVel
	{
		get
		{
			return new Point(this.dVelX, this.dVelY);
		}
	}

	public double GravRadius
	{
		get
		{
			if (this._fRadiusGrav == 0.0)
			{
				double num = 2.452500104904175;
				double num2 = Math.Sqrt(6.674080038626684E-11 * this.fMass / num);
				this._fRadiusGrav = num2 / 149597872.0 / 1000.0;
			}
			return this._fRadiusGrav;
		}
	}

	public double RadiusAtmo
	{
		get
		{
			if (this._radiusAtmo < 0.0)
			{
				this._radiusAtmo = this.FindInAtmoDistance();
			}
			return this._radiusAtmo;
		}
	}

	public static readonly float AtmoKPaThreshold = 0.05f;

	public BodyOrbit boParent;

	public List<BodyOrbit> boChildren;

	public string strName;

	public double fPerh;

	public double fAph;

	public double fAxis1;

	public double fEcc;

	public double fAxis2;

	public double fAngle;

	public double fPeriod;

	public double fRadius;

	public double fMass;

	public double fRotationPeriod;

	public double dTimeCalcLast;

	public double dVelCalcLast;

	public double fPeriodShift;

	public int nOrbitDirection = 1;

	public string strParallax;

	public string strGravParallax;

	public double fParallaxRadius;

	public double fGravParallaxRadius;

	public double fVisibilityRangeMod;

	public double fVisibilityRangeModGrav;

	public JsonAtmosphere[] aAtmospheres;

	private static JsonAtmosphere _voidAtmo;

	public int nDrawFlagsTrack;

	public int nDrawFlagsBody;

	public double dXReal;

	public double dYReal;

	public double dVelX;

	public double dVelY;

	public const int NORMAL = 0;

	public const int NEVER = 1;

	public const int ALWAYS = 2;

	private double _fRadiusGrav;

	private double _radiusAtmo = -1.0;
}
