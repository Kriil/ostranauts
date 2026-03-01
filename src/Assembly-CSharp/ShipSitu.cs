using System;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Utils.Models;
using UnityEngine;

// Runtime ship kinematics/state container.
// This stores position, velocity, acceleration, rotation, orbit locks, and
// optional nav-path data for ships and other moving objects.
public class ShipSitu
{
	// Default zeroed kinematic state for newly created runtime objects.
	public ShipSitu()
	{
		this.strBOPORShip = null;
		this.vPosx = 0.0;
		this.vPosy = 0.0;
		this.vBOOffsetx = 0.0;
		this.vBOOffsety = 0.0;
		double num = 0.0;
		this.vVelY = num;
		this.vVelX = num;
		this.vAccIn = new Vector2(0f, 0f);
		this.vAccRCS = new Vector2(0f, 0f);
		this.vAccEx = new Vector2(0f, 0f);
		this.vAccLift = new Vector2(0f, 0f);
		this.vAccDrag = new Vector2(0f, 0f);
		this.fRot = 0f;
		this.fW = 0f;
		this.fA = 0f;
		this.InitPath();
	}

	// Rehydrates a live ShipSitu from the save DTO.
	public ShipSitu(JsonShipSitu jss)
	{
		this.vPosx = jss.vPosx;
		this.vPosy = jss.vPosy;
		this.vBOOffsetx = jss.vBOOffsetx;
		this.vBOOffsety = jss.vBOOffsety;
		this.vVelX = jss.vVelX;
		this.vVelY = jss.vVelY;
		this.vAccIn.x = jss.vAccIn.x;
		this.vAccIn.y = jss.vAccIn.y;
		this.vAccRCS.x = jss.vAccRCS.x;
		this.vAccRCS.y = jss.vAccRCS.y;
		this.vAccEx.x = jss.vAccEx.x;
		this.vAccEx.y = jss.vAccEx.y;
		this.vAccLift.x = jss.vAccLift.x;
		this.vAccLift.y = jss.vAccLift.y;
		this.vAccDrag.x = jss.vAccDrag.x;
		this.vAccDrag.y = jss.vAccDrag.y;
		this.fRot = jss.fRot;
		this.fW = jss.fW;
		this.fA = jss.fA;
		this.bBOLocked = jss.bBOLocked;
		this.bOrbitLocked = jss.bOrbitLocked;
		this.bIsBO = jss.bIsBO;
		this.bIsRegion = jss.bIsRegion;
		this.bIsNoFees = jss.bIsNoFees;
		this.strBOPORShip = jss.boPORShip;
		this.Size = jss.size;
		this.bIgnoreGrav = jss.bIgnoreGrav;
		if (jss.aPathRecentX != null)
		{
			this.aPathRecent = new List<Tuple<double, Point>>();
			for (int i = 0; i < jss.aPathRecentT.Length; i++)
			{
				this.aPathRecent.Insert(0, new Tuple<double, Point>(jss.aPathRecentT[i], new Point(jss.aPathRecentX[i], jss.aPathRecentY[i])));
			}
		}
		else
		{
			this.InitPath();
		}
		if (jss.jnd != null)
		{
			this.NavData = new NavData(jss.jnd);
		}
	}

	// Copy constructor for temporary prediction or state snapshots.
	public ShipSitu(ShipSitu situ)
	{
		this.CopyFrom(situ, false);
	}

	// Ship size is used by collision/nav displays and is serialized through JsonShipSitu.
	public int Size { get; set; }

	// Ensures the recent-path history list exists.
	private void InitPath()
	{
		if (this.aPathRecent == null)
		{
			this.aPathRecent = new List<Tuple<double, Point>>();
		}
	}

	// Minimal cleanup for the decompiled class's manual lifetime handling.
	public void destroy()
	{
		this.strBOPORShip = null;
	}

	// Copies motion state from another ShipSitu.
	// When `bKinematicsOnly` is true, orbit-lock metadata is left untouched.
	public void CopyFrom(ShipSitu objSS, bool bKinematicsOnly)
	{
		this.vPosx = objSS.vPosx;
		this.vPosy = objSS.vPosy;
		this.vVelX = objSS.vVelX;
		this.vVelY = objSS.vVelY;
		if ((this.bBOLocked || this.bIsBO) && this.strBOPORShip != null && bKinematicsOnly)
		{
			Debug.Log("Tried to copy ShipSitu, but we're BOLocked");
			return;
		}
		this.vBOOffsetx = objSS.vBOOffsetx;
		this.vBOOffsety = objSS.vBOOffsety;
		this.vAccIn.x = objSS.vAccIn.x;
		this.vAccIn.y = objSS.vAccIn.y;
		this.vAccRCS.x = objSS.vAccRCS.x;
		this.vAccRCS.y = objSS.vAccRCS.y;
		this.vAccEx.x = objSS.vAccEx.x;
		this.vAccEx.y = objSS.vAccEx.y;
		this.fRot = objSS.fRot;
		this.fW = objSS.fW;
		this.fA = objSS.fA;
		this.fAccelMagSquaredLast = objSS.fAccelMagSquaredLast;
		if (bKinematicsOnly)
		{
			return;
		}
		this.bBOLocked = objSS.bBOLocked;
		this.bOrbitLocked = objSS.bOrbitLocked;
		this.bIsBO = objSS.bIsBO;
		this.bIsRegion = objSS.bIsRegion;
		this.bIsNoFees = objSS.bIsNoFees;
		this.strBOPORShip = objSS.strBOPORShip;
		this.Size = objSS.Size;
		this.bIgnoreGrav = objSS.bIgnoreGrav;
	}

	// Locks the object to a specific body, keeping a relative offset while matching the body's velocity.
	public void LockToBO(BodyOrbit bo, double fEpoch = -1.0)
	{
		if (bo == null)
		{
			return;
		}
		if (fEpoch < 0.0)
		{
			fEpoch = StarSystem.fEpoch;
		}
		bo.UpdateTime(fEpoch, true, true);
		this.vBOOffsetx = this.vPosx - bo.dXReal;
		this.vBOOffsety = this.vPosy - bo.dYReal;
		this.vVelX = bo.dVelX;
		this.vVelY = bo.dVelY;
		this.fW = 0f;
		this.fA = 0f;
		this.bBOLocked = true;
		this.strBOPORShip = bo.strName;
	}

	// Releases the body-lock while preserving orbit-lock if one is still active.
	public void UnlockFromBO()
	{
		this.bBOLocked = false;
		if (!this.bOrbitLocked)
		{
			this.strBOPORShip = null;
		}
	}

	// Convenience overload that picks the nearest body from the current star system.
	public void LockToBO(double fEpoch = -1.0, bool includePlaceHolder = false)
	{
		if (fEpoch < 0.0)
		{
			fEpoch = StarSystem.fEpoch;
		}
		BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this, fEpoch, includePlaceHolder);
		this.LockToBO(nearestBO, fEpoch);
	}

	// Locks to an orbital frame without marking the object as fully body-locked.
	// Likely used by nav/autopilot behavior that follows an orbit while still allowing rotation.
	public void LockToOrbit(BodyOrbit bo, double fEpoch = -1.0)
	{
		if (bo != null)
		{
			if (fEpoch < 0.0)
			{
				fEpoch = StarSystem.fEpoch;
			}
			bo.UpdateTime(fEpoch, true, true);
			this.vBOOffsetx = this.vPosx - bo.dXReal;
			this.vBOOffsety = this.vPosy - bo.dYReal;
			this.vVelX = bo.dVelX;
			this.vVelY = bo.dVelY;
			this.bOrbitLocked = true;
			this.strBOPORShip = bo.strName;
		}
	}

	// Clears any plotted nav-path/autopilot state.
	public void ResetNavData()
	{
		this.NavData = null;
	}

	// Convenience check for whether this object is following plotted nav data.
	public bool HasNavData()
	{
		return this.NavData != null;
	}

	// Returns distance to the current nav target if one exists.
	public double GetDistanceToDestination(double fEpoch)
	{
		if (!this.HasNavData())
		{
			return 0.0;
		}
		return this.NavData.GetDistanceToTarget(fEpoch);
	}

	// Core movement integrator.
	// This advances nav-driven motion first, then handles body/orbit-locked states,
	// otherwise integrates free-flight position/velocity/rotation from acceleration.
	public void TimeAdvance(double fTime, bool bIgnoreAccel = false)
	{
		if (this.HasNavData() && fTime > 0.0 && this.NavData.TimeAdvance(this))
		{
			return;
		}
		if ((this.bBOLocked || this.bIsBO || this.bOrbitLocked) && this.strBOPORShip != null)
		{
			BodyOrbit bo = CrewSim.system.GetBO(this.strBOPORShip);
			if (bo != null)
			{
				this.vPosx = bo.dXReal + this.vBOOffsetx;
				this.vPosy = bo.dYReal + this.vBOOffsety;
				this.vVelX = bo.dVelX;
				this.vVelY = bo.dVelY;
			}
			if (this.bOrbitLocked)
			{
				float num = 0f;
				if (!bIgnoreAccel)
				{
					num = this.fA;
				}
				this.fRot += (float)((double)this.fW * fTime + (double)num * (0.5 * fTime * fTime));
				float num2 = this.fW;
				this.fW += (float)((double)num * fTime);
				if (this.fW * num2 < 0f)
				{
					this.fW = 0f;
					this.fA = 0f;
				}
			}
			return;
		}
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		if (!bIgnoreAccel)
		{
			num3 = this.vAccIn.x + this.vAccRCS.x + this.vAccLift.x + this.vAccDrag.x;
			num4 = this.vAccIn.y + this.vAccRCS.y + this.vAccLift.y + this.vAccDrag.y;
			if (!this.bIgnoreGrav)
			{
				num3 += this.vAccEx.x;
				num4 += this.vAccEx.y;
			}
			num5 = this.fA;
			this.fAccelMagSquaredLast = (double)(num3 * num3 + num4 * num4);
		}
		double num6 = 0.5 * fTime * fTime;
		this.vPosx += this.vVelX * fTime + (double)num3 * num6;
		this.vPosy += this.vVelY * fTime + (double)num4 * num6;
		this.fRot += (float)((double)this.fW * fTime + (double)num5 * num6);
		this.vVelX += (double)num3 * fTime;
		this.vVelY += (double)num4 * fTime;
		float num7 = this.fW;
		this.fW += (float)((double)num5 * fTime);
		if (this.fW * num7 < 0f)
		{
			this.fW = 0f;
			this.fA = 0f;
		}
	}

	// Computes atmospheric lift and drag against the currently referenced body.
	// This depends on the body's JsonAtmosphere data and is skipped while body/orbit locked.
	public void CalculateLiftDrag(double liftCoefficient, double dragCoeffFront, double dragCoeffSide, double mass, Vector2 gravAcc)
	{
		this.vAccLift = Vector2.zero;
		this.vAccDrag = Vector2.zero;
		if (this.bBOLocked || this.bOrbitLocked || (liftCoefficient == 0.0 && dragCoeffFront == 0.0 && dragCoeffSide == 0.0) || string.IsNullOrEmpty(this.strBOPORShip))
		{
			return;
		}
		BodyOrbit bo = CrewSim.system.GetBO(this.strBOPORShip);
		if (bo == null)
		{
			return;
		}
		double magnitude = MathUtils.GetMagnitude(bo.vPos, this.vPos);
		JsonAtmosphere atmosphereAtDistance = bo.GetAtmosphereAtDistance(magnitude);
		double gasDensity = GasContainer.GetGasDensity(atmosphereAtDistance);
		Point point = this.vVel - bo.vVel;
		double num = MathUtils.GetMagnitude(this.vVel, bo.vVel) / 6.6845869117759804E-12;
		Point directionVector = this.GetDirectionVector(false);
		double angleBetweenVectors = MathUtils.GetAngleBetweenVectors(point, directionVector, false);
		Point perpendicular = (this.vPos - bo.vPos).perpendicular;
		double angleBetweenVectors2 = MathUtils.GetAngleBetweenVectors(perpendicular, point, false);
		double angleBetweenVectors3 = MathUtils.GetAngleBetweenVectors(perpendicular, directionVector, false);
		double num2 = 0.5 * gasDensity * num * num * liftCoefficient * Math.Cos(angleBetweenVectors * 0.01745329238474369) * Math.Cos(angleBetweenVectors3 * 0.01745329238474369);
		num2 = Math.Abs(num2);
		float magnitude2 = gravAcc.magnitude;
		double num3 = mass * (double)magnitude2 / 6.6845869117759804E-12;
		double num4 = num2 / mass * 6.6845869117759804E-12;
		num4 = Math.Min((double)(magnitude2 * 10f), num4);
		this.vAccLift = (float)(-(float)num4) * MathUtils.NormalizeVector(gravAcc);
		float num5 = Mathf.Lerp((float)dragCoeffFront, (float)dragCoeffSide, Mathf.Sin((float)angleBetweenVectors * 0.017453292f));
		double num6 = 0.5 * gasDensity * num * num * (double)num5;
		double num7 = num6 / mass;
		num7 = MathUtils.Clamp(num7, 0.0, 2000.0) * 6.6845869117759804E-12;
		Point point2 = -num7 * point.normalized;
		this.vAccDrag = new Vector2((float)point2.X, (float)point2.Y);
	}

	// Predicts future position without permanently mutating the current state.
	public Point GetPredictedPosition(float advancedTime)
	{
		if ((this.bBOLocked || this.bOrbitLocked || this.bIsBO) && this.strBOPORShip != null)
		{
			BodyOrbit bo = CrewSim.system.GetBO(this.strBOPORShip);
			if (bo != null)
			{
				double dTimeCalcLast = bo.dTimeCalcLast;
				bo.UpdateTime((double)advancedTime + dTimeCalcLast, true, false);
				Point result = new Point(bo.dXReal + this.vBOOffsetx, bo.dYReal + this.vBOOffsety);
				bo.UpdateTime(dTimeCalcLast, true, false);
				return result;
			}
		}
		float num = this.vAccIn.x + this.vAccEx.x + this.vAccRCS.x + this.vAccLift.x + this.vAccDrag.x;
		float num2 = this.vAccIn.y + this.vAccEx.y + this.vAccRCS.y + this.vAccLift.y + this.vAccDrag.y;
		float num3 = 0.5f * advancedTime * advancedTime;
		double x = this.vPosx + (this.vVelX * (double)advancedTime + (double)(num * num3));
		double y = this.vPosy + (this.vVelY * (double)advancedTime + (double)(num2 * num3));
		return new Point(x, y);
	}

	// Distance helper in AU-space coordinates.
	public double GetDistance(double xau, double yau)
	{
		return MathUtils.GetDistance(xau, yau, this.vPosx, this.vPosy);
	}

	public double GetDistance(ShipSitu situ)
	{
		return this.GetDistance(situ.vPosx, situ.vPosy);
	}

	public Point GetDirectionVector(bool invert)
	{
		Vector2 vector = (!invert) ? Vector2.up : Vector2.down;
		float num = (float)Math.Sin((double)this.fRot);
		float num2 = (float)Math.Cos((double)this.fRot);
		return new Point((double)(vector.x * num2 - vector.y * num), (double)(vector.x * num + vector.y * num2));
	}

	public float GetRadiusAU()
	{
		return (float)this.Size * 6.684587E-12f;
	}

	public static Point GetDirection(ShipSitu origin, ShipSitu destination)
	{
		double x = destination.vPosx - origin.vPosx;
		double y = destination.vPosy - origin.vPosy;
		return new Point(x, y);
	}

	public void UpdateTime(double t, bool ignoreAccel = false)
	{
		if ((this.bBOLocked || this.bOrbitLocked || this.bIsBO) && this.strBOPORShip != null)
		{
			BodyOrbit bo = CrewSim.system.GetBO(this.strBOPORShip);
			if (bo != null)
			{
				bo.UpdateTime(t, true, true);
			}
			this.TimeAdvance(0.0, ignoreAccel);
		}
		else if (this.ssDockedHeavier != null)
		{
			this.ssDockedHeavier.UpdateTime(t, false);
			this.PlaceOrbitPosition(this.ssDockedHeavier);
		}
	}

	public void SetRelativePosition(double t)
	{
		Point point = new Point(this.vPosx, this.vPosy);
		BodyOrbit bo = CrewSim.system.GetBO(this.strBOPORShip);
		if (bo == null)
		{
			return;
		}
		BodyOrbit bodyOrbit = CrewSim.system.GetNearestBO(this, StarSystem.fEpoch, false);
		if (bodyOrbit == null || bodyOrbit.strName == "Sol")
		{
			bodyOrbit = bo;
		}
		Point vPos = bodyOrbit.vPos;
		this.LockToBO(bo, -1.0);
		Point vPos2 = bo.vPos;
		this.UpdateTime(t, false);
		Point vPos3 = bo.vPos;
		Point vPos4 = bodyOrbit.vPos;
		Point vPos5 = this.vPos;
		Point point2 = vPos5 + (vPos - vPos4);
		this.UpdateTime(StarSystem.fEpoch, false);
		this.vPosx = point2.X;
		this.vPosy = point2.Y;
		this.LockToBO(bodyOrbit, -1.0);
	}

	public void Pushback(Ship shipUs, Ship shipThem)
	{
		Vector2 pushbackVector = MathUtils.GetPushbackVector(shipUs, shipThem);
		this.vVelX += (double)pushbackVector.x * 3.342293532089815E-11;
		this.vVelY += (double)pushbackVector.y * 3.342293532089815E-11;
		this.PlaceOrbitPosition(shipThem.objSS);
	}

	public void PlaceOrbitPosition(ShipSitu bigShip)
	{
		if ((this.bBOLocked || this.bOrbitLocked || this.bIsBO) && this.strBOPORShip != null)
		{
			return;
		}
		float num = bigShip.GetRadiusAU() + this.GetRadiusAU() + 6.684587E-11f;
		Vector2 pushbackVector = MathUtils.GetPushbackVector(this, bigShip);
		pushbackVector.Normalize();
		this.vBOOffsetx = (double)(pushbackVector.x * num);
		this.vBOOffsety = (double)(pushbackVector.y * num);
		this.vPosx = bigShip.vPosx + this.vBOOffsetx;
		this.vPosy = bigShip.vPosy + this.vBOOffsety;
		float num2 = Mathf.Atan2(pushbackVector.y, pushbackVector.x);
		num2 = 1.5707964f - num2;
		this.fRot = 3.1415927f - num2;
	}

	public void SetSize(int length)
	{
		this.Size = ((!this.bIsBO && length > 1) ? (length * 20) : ShipSitu.MINSTATIONSIZE);
	}

	public double GetPORRange()
	{
		BodyOrbit bo = CrewSim.system.GetBO(this.strBOPORShip);
		if (bo == null)
		{
			return double.PositiveInfinity;
		}
		return this.GetDistance(bo.dXReal, bo.dYReal) - bo.fRadius;
	}

	public double GetRangeTo(ShipSitu objSSThem)
	{
		if (objSSThem == null)
		{
			return double.PositiveInfinity;
		}
		return this.GetDistance(objSSThem.vPosx, objSSThem.vPosy);
	}

	public void LogPath()
	{
		this.aPathRecent.Insert(0, new Tuple<double, Point>(StarSystem.fEpoch, new Point(this.vPosx, this.vPosy)));
		if (this.aPathRecent.Count > this.nPathNodes)
		{
			this.aPathRecent.RemoveAt(this.aPathRecent.Count - 1);
		}
	}

	public JsonShipSitu GetJSON()
	{
		JsonShipSitu jsonShipSitu = new JsonShipSitu();
		jsonShipSitu.boPORShip = this.strBOPORShip;
		jsonShipSitu.vPosx = this.vPosx;
		jsonShipSitu.vPosy = this.vPosy;
		jsonShipSitu.vBOOffsetx = this.vBOOffsetx;
		jsonShipSitu.vBOOffsety = this.vBOOffsety;
		jsonShipSitu.vVelX = this.vVelX;
		jsonShipSitu.vVelY = this.vVelY;
		jsonShipSitu.vAccIn = this.vAccIn;
		jsonShipSitu.vAccRCS = this.vAccRCS;
		jsonShipSitu.vAccEx = this.vAccEx;
		jsonShipSitu.fRot = this.fRot;
		jsonShipSitu.fW = this.fW;
		jsonShipSitu.fA = this.fA;
		jsonShipSitu.bBOLocked = this.bBOLocked;
		jsonShipSitu.bOrbitLocked = this.bOrbitLocked;
		jsonShipSitu.bIsBO = this.bIsBO;
		jsonShipSitu.bIsRegion = this.bIsRegion;
		jsonShipSitu.bIsNoFees = this.bIsNoFees;
		jsonShipSitu.size = this.Size;
		jsonShipSitu.vAccLift = this.vAccLift;
		jsonShipSitu.vAccDrag = this.vAccDrag;
		jsonShipSitu.bIgnoreGrav = this.bIgnoreGrav;
		jsonShipSitu.aPathRecentT = new double[this.aPathRecent.Count];
		jsonShipSitu.aPathRecentX = new double[this.aPathRecent.Count];
		jsonShipSitu.aPathRecentY = new double[this.aPathRecent.Count];
		for (int i = 0; i < this.aPathRecent.Count; i++)
		{
			jsonShipSitu.aPathRecentT[i] = this.aPathRecent[i].Item1;
			jsonShipSitu.aPathRecentX[i] = this.aPathRecent[i].Item2.X;
			jsonShipSitu.aPathRecentY[i] = this.aPathRecent[i].Item2.Y;
		}
		if (this.NavData != null)
		{
			jsonShipSitu.jnd = this.NavData.GetJSON();
		}
		return jsonShipSitu;
	}

	public void ResetAccelerations()
	{
		this.vAccIn = Vector2.zero;
		this.vAccEx = Vector2.zero;
		this.vAccRCS = Vector2.zero;
		this.vAccLift = Vector2.zero;
		this.vAccDrag = Vector2.zero;
	}

	public double vVelX
	{
		get
		{
			return this._vVelX;
		}
		set
		{
			if (double.IsNaN(value) || Math.Abs(value - this._vVelX) > 0.5)
			{
				Debug.Log("WARNING: Massive X Impulse! " + value);
			}
			this._vVelX = value;
		}
	}

	public double vVelY
	{
		get
		{
			return this._vVelY;
		}
		set
		{
			if (double.IsNaN(value) || Math.Abs(value - this._vVelY) > 0.5)
			{
				Debug.Log("WARNING: Massive Y Impulse! " + value);
			}
			this._vVelY = value;
		}
	}

	public Point vPos
	{
		get
		{
			return new Point(this.vPosx, this.vPosy);
		}
	}

	public Point vVel
	{
		get
		{
			return new Point(this.vVelX, this.vVelY);
		}
	}

	public bool IsAccelerating
	{
		get
		{
			return this.vAccIn.x != 0f || this.vAccIn.y != 0f || this.vAccRCS.x != 0f || this.vAccRCS.y != 0f || this.vAccEx.x != 0f || this.vAccEx.y != 0f;
		}
	}

	public override string ToString()
	{
		return this.vPos.ToString() + "; Vel: " + this.vVel.ToString();
	}

	public static readonly int MINSTATIONSIZE = 1500;

	public static readonly int MINSHIPSIZE = 200;

	public string strBOPORShip;

	public double vPosx;

	public double vPosy;

	public double vBOOffsetx;

	public double vBOOffsety;

	private double _vVelX;

	private double _vVelY;

	public double fAccelMagSquaredLast;

	public Vector2 vAccIn;

	public Vector2 vAccRCS;

	public Vector2 vAccEx;

	public Vector2 vAccLift;

	public Vector2 vAccDrag;

	public float fRot;

	public float fW;

	public float fA;

	public bool bBOLocked;

	public bool bOrbitLocked;

	public bool bIsBO;

	public bool bIsRegion;

	public bool bIsNoFees = true;

	public NavData NavData;

	public List<Tuple<double, Point>> aPathRecent;

	private int nPathNodes = 20;

	public const float PATH_STEP_TIME = 2f;

	public bool IsRegionBorderPoint;

	public bool bIgnoreGrav;

	public ShipSitu ssDockedHeavier;
}
