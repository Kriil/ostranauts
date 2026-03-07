using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Ships;
using UnityEngine;

// Global ship collision/proximity pass. Queues loaded ships, checks approach
// distances, raises proximity warnings, and processes actual collisions.
public class CollisionManager
{
	// Clears the queued ship batches used by the incremental collision pass.
	public static void ClearQueue()
	{
		CollisionManager._shipQueue.Clear();
	}

	// Runs the next batch of ship collision checks. When the queue empties, it is
	// refilled from the currently loaded ships.
	public static void RunQueue()
	{
		if (CollisionManager._shipQueue.Count() == 0)
		{
			CollisionManager._collisionMemory.Clear();
			CollisionManager._shipQueue.Fill(CrewSim.system.GetAllLoadedShips());
		}
		IEnumerable<Ship> enumerable = CollisionManager._shipQueue.Dequeue();
		if (enumerable == null)
		{
			return;
		}
		foreach (Ship ship in enumerable)
		{
			if (ship.bDestroyed)
			{
				CollisionManager.UnregisterShip(ship);
			}
			else if (ship.LoadState < Ship.Loaded.Edit)
			{
				CollisionManager.CheckCollisions(ship);
			}
		}
	}

	// Adds a ship to the incremental collision-check queue.
	public static void RegisterShip(Ship ship)
	{
		CollisionManager._shipQueue.Enqueue(ship);
	}

	public static void UnregisterShip(Ship ship)
	{
		CollisionManager._shipQueue.UnregisterShip(ship);
	}

	// Moves a ship toward the front of the collision-check queue.
	public static void PrioritizeShip(Ship ship)
	{
		CollisionManager._shipQueue.PrioritizeShip(ship);
	}

	// Checks one ship against the rest of the system for collision or proximity
	// events, skipping ships that are hidden, safe, or intentionally ignored.
	public static void CheckCollisions(Ship shipCheck)
	{
		if (CollisionManager.ctCollisionSafe == null)
		{
			CollisionManager.ctCollisionSafe = DataHandler.GetCondTrigger("TIsCollisionSafe");
		}
		if (shipCheck == null || shipCheck.bNoCollisions || shipCheck.IsStationHidden(false))
		{
			return;
		}
		bool proximityWarning = false;
		double num = 1.0;
		bool flag = shipCheck.objSS.bBOLocked || shipCheck.objSS.bIsBO;
		shipCheck.aProxCurrent.Clear();
		float num2 = 100000000f;
		double num3 = 1.0000000138484279E+24;
		bool flag2 = shipCheck.IsPlayerShip();
		Vector2 vOffsetWorld = default(Vector2);
		string text = (from x in shipCheck.GetAllDockedShipsFull()
		select x.strRegID).FirstOrDefault<string>();
		foreach (Ship ship in CrewSim.system.dictShips.Values)
		{
			string empty = string.Empty;
			if (shipCheck == ship || (CollisionManager._collisionMemory.TryGetValue(ship.strRegID, out empty) && empty == shipCheck.strRegID))
			{
				if (flag2 && shipCheck.IsStation(false) && shipCheck.objSS.bIsRegion)
				{
					CollisionManager.strATCClosest = ship.strRegID;
					num2 = 0f;
				}
			}
			else if (!ship.bNoCollisions && (!ship.objSS.bIsBO || !shipCheck.objSS.bIsBO) && !ship.HideFromSystem && !ship.IsStationHidden(false))
			{
				if (!string.IsNullOrEmpty(text) && text == ship.strRegID)
				{
					text = null;
					if (ship.IsStation(false))
					{
						flag = true;
						if (flag2 && ship.objSS.bIsRegion)
						{
							CollisionManager.strATCClosest = ship.strRegID;
							num2 = 0f;
						}
					}
				}
				else if (!ship.objSS.bBOLocked || !shipCheck.objSS.bBOLocked)
				{
					float num4 = (float)shipCheck.objSS.GetDistance(ship.objSS.vPosx, ship.objSS.vPosy);
					if (num4 != 0f)
					{
						float collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(shipCheck, ship);
						if (num4 < collisionDistanceAU)
						{
							if ((shipCheck.LoadState >= Ship.Loaded.Edit && shipCheck.bDocked) || (ship.LoadState >= Ship.Loaded.Edit && ship.bDocked))
							{
								Debug.Log("Collision skipped: Player docked.");
							}
							else if ((shipCheck.LoadState >= Ship.Loaded.Edit && shipCheck.IsStation(true)) || (ship.LoadState >= Ship.Loaded.Edit && ship.IsStation(true)))
							{
								Debug.Log("Collision skipped: Player on station.");
							}
							else
							{
								CollisionManager.ProcessCollision(shipCheck, ship, (double)(collisionDistanceAU - num4));
								if (flag2 && (double)Time.timeScale > 1.0)
								{
									CrewSim.ResetTimeScale();
								}
							}
						}
						float num5 = 3.3422936E-08f;
						if (flag2)
						{
							double num6 = MathUtils.GetDistance(ship.objSS.vVelX, ship.objSS.vVelY, shipCheck.objSS.vVelX, shipCheck.objSS.vVelY);
							num6 /= 5.013440183831985E-09;
							num5 *= Mathf.Clamp((float)num6, 1f, 10f);
						}
						if (num4 < num5 + collisionDistanceAU)
						{
							shipCheck.aProxCurrent.Add(ship.strRegID);
							if (shipCheck.aProxIgnores.IndexOf(ship.strRegID) < 0)
							{
								num = Math.Min(num, (double)(num4 / (num5 + collisionDistanceAU)));
								proximityWarning = true;
							}
						}
						else if (shipCheck.LoadState > Ship.Loaded.Shallow && shipCheck.aProxIgnores.Count > 0)
						{
							shipCheck.aProxIgnores.Remove(ship.strRegID);
						}
					}
					if (flag2 && num4 < num2 && ship.IsStation(false) && ship.objSS.bIsRegion)
					{
						CollisionManager.strATCClosest = ship.strRegID;
						num2 = num4;
					}
				}
			}
		}
		BodyOrbit bodyOrbit = null;
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in CrewSim.system.aBOs)
		{
			if (flag2 && keyValuePair.Value.strParallax != null)
			{
				double distance = shipCheck.objSS.GetDistance(keyValuePair.Value.dXReal, keyValuePair.Value.dYReal);
				if (keyValuePair.Value.fParallaxRadius > distance && keyValuePair.Value.fParallaxRadius < num3)
				{
					num3 = distance;
					CollisionManager.strBOClosestParallax = keyValuePair.Value.strParallax;
					vOffsetWorld.x = (float)((shipCheck.objSS.vPosx - keyValuePair.Value.dXReal) / keyValuePair.Value.fParallaxRadius);
					vOffsetWorld.y = (float)((shipCheck.objSS.vPosy - keyValuePair.Value.dYReal) / keyValuePair.Value.fParallaxRadius);
				}
				else if (!string.IsNullOrEmpty(keyValuePair.Value.strGravParallax) && keyValuePair.Value.fGravParallaxRadius > distance && keyValuePair.Value.fGravParallaxRadius < num3)
				{
					num3 = distance;
					CollisionManager.strBOClosestParallax = keyValuePair.Value.strGravParallax;
					vOffsetWorld.x = (float)((shipCheck.objSS.vPosx - keyValuePair.Value.dXReal) / keyValuePair.Value.fGravParallaxRadius);
					vOffsetWorld.y = (float)((shipCheck.objSS.vPosy - keyValuePair.Value.dYReal) / keyValuePair.Value.fGravParallaxRadius);
				}
			}
			if (!flag)
			{
				if (!keyValuePair.Value.IsPlaceholder())
				{
					keyValuePair.Value.UpdateTime(StarSystem.fEpoch, true, true);
					double distance2 = shipCheck.objSS.GetDistance(keyValuePair.Value.dXReal, keyValuePair.Value.dYReal);
					if (distance2 != 0.0)
					{
						if (shipCheck.objSS.bOrbitLocked && distance2 < keyValuePair.Value.RadiusAtmo)
						{
							bodyOrbit = CrewSim.system.GetBO(shipCheck.objSS.strBOPORShip);
							shipCheck.UnlockFromOrbit(false);
							if (shipCheck.LoadState >= Ship.Loaded.Full && (double)Time.timeScale > 1.0)
							{
								CrewSim.ResetTimeScale();
							}
						}
						double collisionDistanceAU2 = CollisionManager.GetCollisionDistanceAU(shipCheck, keyValuePair.Value);
						if (distance2 < collisionDistanceAU2)
						{
							if (shipCheck.LoadState > Ship.Loaded.Shallow)
							{
								CollisionManager.ProcessCollision(shipCheck, keyValuePair.Value, collisionDistanceAU2 - distance2);
							}
							else
							{
								foreach (Ship ship2 in shipCheck.GetAllDockedShips())
								{
									ship2.Destroy(false);
								}
								shipCheck.Destroy(false);
							}
							if (flag2 && (double)Time.timeScale > 1.0)
							{
								CrewSim.ResetTimeScale();
							}
						}
						if (shipCheck.bDestroyed)
						{
							break;
						}
						if (distance2 < 3.342293553032505E-08 + collisionDistanceAU2)
						{
							shipCheck.aProxCurrent.Add(keyValuePair.Value.strName);
							if (shipCheck.aProxIgnores.IndexOf(keyValuePair.Value.strName) < 0)
							{
								num = Math.Min(num, distance2 / (3.342293553032505E-08 + collisionDistanceAU2));
								proximityWarning = true;
							}
						}
						else if (shipCheck.LoadState > Ship.Loaded.Shallow && shipCheck.aProxIgnores.Count > 0)
						{
							shipCheck.aProxIgnores.Remove(keyValuePair.Value.strName);
						}
					}
				}
			}
		}
		if (bodyOrbit != null)
		{
			CrewSim.system.RemoveBO(bodyOrbit);
		}
		if (flag2)
		{
			shipCheck.proximityWarning = proximityWarning;
			shipCheck.proximityDistanceScaled = (float)num;
			ParallaxController.vOffsetWorld = vOffsetWorld;
			ParallaxController.fRotationWorld = 57.29578f * -shipCheck.objSS.fRot;
			if (CollisionManager.strBOClosestParallax != ParallaxController.ActiveParallax && string.IsNullOrEmpty(shipCheck.strParallax))
			{
				List<Ship> allDockedShips = shipCheck.GetAllDockedShips();
				bool flag3 = true;
				foreach (Ship ship3 in allDockedShips)
				{
					if (!string.IsNullOrEmpty(ship3.strParallax) && ship3.strParallax == ParallaxController.ActiveParallax)
					{
						flag3 = false;
					}
				}
				if (flag3)
				{
					CrewSim.system.SetParallax(CollisionManager.strBOClosestParallax);
				}
			}
		}
	}

	public static float GetCollisionDistanceAU(ShipSitu a, ShipSitu b)
	{
		return a.GetRadiusAU() + b.GetRadiusAU();
	}

	public static float GetCollisionDistanceAU(Ship a, Ship b)
	{
		return a.objSS.GetRadiusAU() + b.objSS.GetRadiusAU();
	}

	public static double GetRangeToCollisionKM(Ship a, Ship b)
	{
		return CollisionManager.GetRangeToCollisionAU(a, b) * 149597872.0;
	}

	public static double GetRangeToCollisionAU(Ship a, Ship b)
	{
		double num = a.objSS.GetRangeTo(b.objSS);
		num -= (double)CollisionManager.GetCollisionDistanceAU(a, b);
		return Math.Max(0.0, num);
	}

	public static double GetCollisionDistanceAU(ShipSitu objSS, BodyOrbit b)
	{
		return (double)objSS.GetRadiusAU() + b.fRadius;
	}

	public static double GetCollisionDistanceAU(Ship a, BodyOrbit b)
	{
		return (double)a.objSS.GetRadiusAU() + b.fRadius;
	}

	private static void ProcessCollision(Ship a, Ship b, double fOverlap)
	{
		if (a == null || b == null || a.bDestroyed || b.bDestroyed)
		{
			return;
		}
		if (!a.IsPlayerShip() && !b.IsPlayerShip() && ((a.shipUndock != null && a.shipUndock.strRegID == b.strRegID) || (b.shipUndock != null && b.shipUndock.strRegID == a.strRegID)))
		{
			if (AIShipManager.ShowDebugLogs)
			{
				Debug.Log("#AI# " + a.strRegID + " skipping collision with " + b.strRegID);
			}
			return;
		}
		if (CollisionManager.IsPlayerWithDockingProtection(a, b))
		{
			return;
		}
		CollisionManager._collisionMemory[a.strRegID] = b.strRegID;
		double num = a.objSS.vPosx - b.objSS.vPosx;
		double num2 = a.objSS.vPosy - b.objSS.vPosy;
		MathUtils.SetLength(ref num, ref num2, 1.0);
		double num3 = a.objSS.vVelX - b.objSS.vVelX;
		double num4 = a.objSS.vVelY - b.objSS.vVelY;
		double num5 = a.Mass;
		double num6 = b.Mass;
		bool flag = a.objSS.bIsBO;
		bool flag2 = b.objSS.bIsBO;
		a.UnlockFromOrbit(true);
		foreach (Ship ship in a.GetAllDockedShips())
		{
			ship.UnlockFromOrbit(false);
			num5 += ship.Mass;
			if (ship.objSS == null)
			{
				Debug.LogWarning("null objSS found on ship: " + ship.ToString() + ". Ignoring mass.");
			}
			else
			{
				flag = (flag || ship.objSS.bIsBO);
			}
		}
		b.UnlockFromOrbit(true);
		foreach (Ship ship2 in b.GetAllDockedShips())
		{
			ship2.UnlockFromOrbit(false);
			num6 += ship2.Mass;
			if (ship2.objSS == null)
			{
				Debug.LogWarning("null objSS found on ship: " + ship2.ToString() + ". Ignoring mass.");
			}
			else
			{
				flag2 = (flag2 || ship2.objSS.bIsBO);
			}
		}
		if (num5 == 0.0 || num6 == 0.0)
		{
			num6 = (num5 = 1.0);
		}
		double num7 = num3 * num + num4 * num2;
		double num8 = fOverlap * CollisionManager.fBounceRange;
		double num9 = 1.0;
		double num10 = 1.0;
		if (flag && !flag2)
		{
			num9 = 0.0;
			num10 = 2.0;
			b.objSS.vVelX = a.objSS.vVelX;
			b.objSS.vVelY = a.objSS.vVelY;
			foreach (Ship ship3 in b.GetAllDockedShips())
			{
				ship3.objSS.vVelX = a.objSS.vVelX;
				ship3.objSS.vVelY = a.objSS.vVelY;
			}
			if (b.IsAIShip && b.NavAIManned)
			{
				if (b.DeltaVRemainingRCS <= 0.0)
				{
					b.AIRefuel();
				}
				if (b.IsLocalAuthority)
				{
					AIShipManager.AddAIToShip(b, AIType.Police, null, null);
				}
				else
				{
					AIShipManager.AddAIToShip(b, AIType.Scav, null, null);
				}
			}
		}
		else if (flag2 && !flag)
		{
			num10 = 0.0;
			num9 = 2.0;
			a.objSS.vVelX = b.objSS.vVelX;
			a.objSS.vVelY = b.objSS.vVelY;
			foreach (Ship ship4 in a.GetAllDockedShips())
			{
				ship4.objSS.vVelX = b.objSS.vVelX;
				ship4.objSS.vVelY = b.objSS.vVelY;
			}
			if (a.IsAIShip && a.NavAIManned)
			{
				if (a.DeltaVRemainingRCS <= 0.0)
				{
					a.AIRefuel();
				}
				if (a.IsLocalAuthority)
				{
					AIShipManager.AddAIToShip(a, AIType.Police, null, null);
				}
				else
				{
					AIShipManager.AddAIToShip(a, AIType.Scav, null, null);
				}
			}
		}
		a.objSS.vPosx += num * num8 / 2.0 * num9;
		a.objSS.vPosy += num2 * num8 / 2.0 * num9;
		b.objSS.vPosx -= num * num8 / 2.0 * num10;
		b.objSS.vPosy -= num2 * num8 / 2.0 * num10;
		if (!flag && a.bDocked)
		{
			a.objSS.fW = 0f;
		}
		if (!flag2 && b.bDocked)
		{
			b.objSS.fW = 0f;
		}
		foreach (Ship ship5 in a.GetAllDockedShips())
		{
			ship5.objSS.vPosx += num * num8 / 2.0 * num9;
			ship5.objSS.vPosy += num2 * num8 / 2.0 * num9;
			if (!flag && a.bDocked)
			{
				ship5.objSS.fW = 0f;
			}
		}
		foreach (Ship ship6 in b.GetAllDockedShips())
		{
			ship6.objSS.vPosx -= num * num8 / 2.0 * num10;
			ship6.objSS.vPosy -= num2 * num8 / 2.0 * num10;
			if (!flag2 && b.bDocked)
			{
				ship6.objSS.fW = 0f;
			}
		}
		double num11 = -0.6000000238418579 * num7;
		if (num11 > 0.0)
		{
			double num12 = 1.0;
			double num13 = num5 + num6;
			double num14 = num5 - num6;
			double num15 = num14 / num13 * a.objSS.vVelX + 2.0 * num6 / num13 * b.objSS.vVelX;
			double num16 = num14 / num13 * a.objSS.vVelY + 2.0 * num6 / num13 * b.objSS.vVelY;
			a.objSS.vVelX += (num15 - a.objSS.vVelX) * num12 * num9;
			a.objSS.vVelY += (num16 - a.objSS.vVelY) * num12 * num9;
			foreach (Ship ship7 in a.GetAllDockedShips())
			{
				ship7.objSS.vVelX += (num15 - ship7.objSS.vVelX) * num12 * num9;
				ship7.objSS.vVelY += (num16 - ship7.objSS.vVelY) * num12 * num9;
			}
			num15 = -num14 / num13 * b.objSS.vVelX + 2.0 * num5 / num13 * a.objSS.vVelX;
			num16 = -num14 / num13 * b.objSS.vVelY + 2.0 * num5 / num13 * a.objSS.vVelY;
			b.objSS.vVelX += (num15 - b.objSS.vVelX) * num12 * num10;
			b.objSS.vVelY += (num16 - b.objSS.vVelY) * num12 * num10;
			foreach (Ship ship8 in b.GetAllDockedShips())
			{
				ship8.objSS.vVelX += (num15 - ship8.objSS.vVelX) * num12 * num10;
				ship8.objSS.vVelY += (num16 - ship8.objSS.vVelY) * num12 * num10;
			}
		}
		double num17 = Math.Sqrt(num3 * num3 + num4 * num4) / 6.6845869117759804E-12;
		bool flag3 = false;
		bool isAIShip = a.IsAIShip;
		bool isAIShip2 = b.IsAIShip;
		if ((isAIShip || a.IsDerelict()) && b.IsStation(false))
		{
			flag3 = true;
		}
		else if ((isAIShip2 || b.IsDerelict()) && a.IsStation(false))
		{
			flag3 = true;
		}
		if (a.LoadState >= Ship.Loaded.Edit && isAIShip2)
		{
			if (!CrewSim.coPlayer.ship.NavPlayerManned)
			{
				flag3 = true;
			}
			else if (a.bDocked)
			{
				flag3 = true;
			}
			else if (CollisionManager.ctCollisionSafe.Triggered(CrewSim.coPlayer, null, true))
			{
				flag3 = true;
			}
		}
		if (a.LoadState >= Ship.Loaded.Edit && isAIShip)
		{
			if (!CrewSim.coPlayer.ship.NavPlayerManned)
			{
				flag3 = true;
			}
			else if (b.bDocked)
			{
				flag3 = true;
			}
			else if (CollisionManager.ctCollisionSafe.Triggered(CrewSim.coPlayer, null, true))
			{
				flag3 = true;
			}
		}
		if (a.LoadState >= Ship.Loaded.Edit && b.ShipCO != null && b.ShipCO.HasCond("IsTutorialDerelict"))
		{
			if (CrewSim.coPlayer.HasCond("TutorialNavDockingScreenWaiting2") || CrewSim.coPlayer.HasCond("TutorialNavDockingClearanceWaiting") || CrewSim.coPlayer.HasCond("TutorialNavDockWithDerelictWaiting"))
			{
				flag3 = true;
			}
		}
		else if (b.LoadState >= Ship.Loaded.Edit && a.ShipCO != null && a.ShipCO.HasCond("IsTutorialDerelict") && (CrewSim.coPlayer.HasCond("TutorialNavDockingScreenWaiting2") || CrewSim.coPlayer.HasCond("TutorialNavDockingClearanceWaiting") || CrewSim.coPlayer.HasCond("TutorialNavDockWithDerelictWaiting")))
		{
			flag3 = true;
		}
		if (flag3)
		{
			num17 = 90.0;
		}
		double num18 = num17 / 2.0;
		JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeCollision");
		bool bAudio = Wound.bAudio;
		Wound.bAudio = true;
		while (num18 > CollisionManager.dMaxSafeV)
		{
			if (attackMode == null || num18 < 0.0)
			{
				break;
			}
			float fMult = (float)num18;
			if (num18 > CollisionManager.dMaxSafeV)
			{
				fMult = (float)CollisionManager.dMaxSafeV;
			}
			a.DamageRayRandom(attackMode, fMult, null, true);
			b.DamageRayRandom(attackMode, fMult, null, true);
			num18 -= CollisionManager.dMaxSafeV;
		}
		Wound.bAudio = bAudio;
		CollisionManager.CollisionResponse(a.GetPeople(true));
		CollisionManager.CollisionResponse(b.GetPeople(true));
		double num19 = 3.0 * (num17 - CollisionManager.dMaxSafeV) / CollisionManager.dMaxSafeV;
		double num20 = num17 / CollisionManager.dMaxSafeV / 3.0;
		if (a.LoadState >= Ship.Loaded.Edit || b.LoadState >= Ship.Loaded.Edit)
		{
			CollisionManager.WoundCrew(a.GetPeople(true), num19 * num6 / num5, num20 * num6 / num5);
			CollisionManager.WoundCrew(b.GetPeople(true), num19 * num5 / num6, num20 * num5 / num6);
			if (num17 >= 2.0 * CollisionManager.dMaxSafeV)
			{
				CrewSim.objInstance.CamShake(1f);
				AudioManager.am.PlayAudioEmitter("ShipCollisionBig", false, true);
			}
			else if (num17 >= CollisionManager.dMaxSafeV)
			{
				CrewSim.objInstance.CamShake(0.5f);
				AudioManager.am.PlayAudioEmitter("ShipCollisionMed", false, true);
			}
			else
			{
				CrewSim.objInstance.CamShake(0.2f);
				AudioManager.am.PlayAudioEmitter("ShipCollisionSmall", false, true);
			}
			BeatManager.ResetTensionTimer();
		}
		if (a.fAIPauseTimer <= 5.0)
		{
			a.fAIPauseTimer = StarSystem.fEpoch + MathUtils.Rand(5.0, 10.0, MathUtils.RandType.Flat, null);
		}
		if (b.fAIPauseTimer <= 5.0)
		{
			b.fAIPauseTimer = StarSystem.fEpoch + MathUtils.Rand(5.0, 10.0, MathUtils.RandType.Flat, null);
		}
		CollisionManager.TriggerCollisionEvent(a.strRegID, b.strRegID);
		a.LogAdd(DataHandler.GetString("NAV_LOG_IMPACT", false) + b.strRegID, StarSystem.fEpoch, true);
		b.LogAdd(DataHandler.GetString("NAV_LOG_IMPACT", false) + a.strRegID, StarSystem.fEpoch, true);
		if (AIShipManager.ShowDebugLogs)
		{
			Debug.Log(string.Concat(new string[]
			{
				"#AI# Collision between ",
				a.strRegID,
				" & ",
				b.strRegID,
				" adding pause timer"
			}));
		}
	}

	private static bool IsPlayerWithDockingProtection(Ship a, Ship b)
	{
		return !(GUIDockSys.instance == null) && (a.IsPlayerShip() || b.IsPlayerShip()) && ((a.IsPlayerShip() && GUIDockSys.instance.HasActiveDockingProtection(b.strRegID)) || (b.IsPlayerShip() && GUIDockSys.instance.HasActiveDockingProtection(a.strRegID)));
	}

	private static void ProcessCollision(Ship a, BodyOrbit bo, double fOverlap)
	{
		double num = a.objSS.vPosx - bo.dXReal;
		double num2 = a.objSS.vPosy - bo.dYReal;
		MathUtils.SetLength(ref num, ref num2, 1.0);
		double num3 = a.objSS.vVelX - bo.dVelX;
		double num4 = a.objSS.vVelY - bo.dVelY;
		bool flag = a.objSS.bIsBO;
		foreach (Ship ship in a.GetAllDockedShips())
		{
			flag = (flag || ship.objSS.bIsBO);
		}
		double num5 = num3 * num + num4 * num2;
		double num6 = fOverlap * CollisionManager.fBounceRange;
		a.objSS.vPosx += num * num6;
		a.objSS.vPosy += num2 * num6;
		if (!flag && a.bDocked)
		{
			a.objSS.fW = 0f;
		}
		foreach (Ship ship2 in a.GetAllDockedShips())
		{
			ship2.objSS.vPosx += num * num6;
			ship2.objSS.vPosy += num2 * num6;
			if (!flag && a.bDocked)
			{
				ship2.objSS.fW = 0f;
			}
		}
		double num7 = -1.100000023841858 * num5;
		if (num7 > 0.0)
		{
			a.objSS.vVelX += num7 * num;
			a.objSS.vVelY += num7 * num2;
			foreach (Ship ship3 in a.GetAllDockedShips())
			{
				ship3.objSS.vVelX += num7 * num;
				ship3.objSS.vVelY += num7 * num2;
			}
		}
		double num8 = Math.Sqrt(num3 * num3 + num4 * num4) / 6.6845869117759804E-12;
		double num9 = num8;
		if (num9 > CollisionManager.dMaxSafeV)
		{
			num9 = CollisionManager.dMaxSafeV * 50.0;
		}
		JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeCollision");
		bool bAudio = Wound.bAudio;
		Wound.bAudio = true;
		while (num9 > CollisionManager.dMaxSafeV)
		{
			if (attackMode == null || num9 < 0.0)
			{
				break;
			}
			float fMult = (float)num9;
			if (num9 > CollisionManager.dMaxSafeV)
			{
				fMult = (float)CollisionManager.dMaxSafeV;
			}
			a.DamageRayRandom(attackMode, fMult, null, true);
			num9 -= CollisionManager.dMaxSafeV;
		}
		Wound.bAudio = bAudio;
		CollisionManager.CollisionResponse(a.GetPeople(true));
		double num10 = 3.0 * (num8 - CollisionManager.dMaxSafeV) / CollisionManager.dMaxSafeV;
		double num11 = num8 / CollisionManager.dMaxSafeV / 3.0;
		CollisionManager.WoundCrew(a.GetPeople(true), num10 * 10.0, num11 * 10.0);
		if (a == CrewSim.coPlayer.ship || CrewSim.coPlayer.ship.GetAllDockedShips().IndexOf(a) >= 0)
		{
			if (num8 >= 2.0 * CollisionManager.dMaxSafeV)
			{
				CrewSim.objInstance.CamShake(1f);
				AudioManager.am.PlayAudioEmitter("ShipCollisionBig", false, true);
			}
			else if (num8 >= CollisionManager.dMaxSafeV)
			{
				CrewSim.objInstance.CamShake(0.5f);
				AudioManager.am.PlayAudioEmitter("ShipCollisionMed", false, true);
			}
			else
			{
				CrewSim.objInstance.CamShake(0.2f);
				AudioManager.am.PlayAudioEmitter("ShipCollisionSmall", false, true);
			}
			BeatManager.ResetTensionTimer();
		}
		a.fAIPauseTimer = StarSystem.fEpoch + MathUtils.Rand(5.0, 10.0, MathUtils.RandType.Flat, null);
		CollisionManager.TriggerCollisionEvent(a.strRegID, bo.strName);
		a.LogAdd(DataHandler.GetString("NAV_LOG_IMPACT", false) + bo.strName, StarSystem.fEpoch, true);
		if (AIShipManager.ShowDebugLogs)
		{
			Debug.Log("#AI# Collission with BO " + a.strRegID);
		}
	}

	private static void TriggerCollisionEvent(string a, string b)
	{
		if (CollisionManager.CollisionEvent == null)
		{
			CollisionManager.CollisionEvent = new CollisionEvent();
		}
		CollisionManager.CollisionEvent.Invoke(new Tuple<string, string>(a, b));
	}

	private static void CollisionResponse(List<CondOwner> aPeople)
	{
		if (aPeople == null)
		{
			return;
		}
		foreach (CondOwner condOwner in aPeople)
		{
			Interaction interaction = DataHandler.GetInteraction("EVTCollisionBig", null, false);
			if (interaction != null && interaction.Triggered(condOwner, condOwner, false, false, false, true, null))
			{
				interaction.objUs = condOwner;
				interaction.objThem = condOwner;
				interaction.ApplyChain(null);
				if (condOwner.ship != null)
				{
					condOwner.ship.KnockDownCrew(condOwner);
				}
			}
		}
	}

	private static void WoundCrew(List<CondOwner> aCrew, double fDmgMax, double fInjuries)
	{
		if (aCrew == null || aCrew.Count == 0)
		{
			return;
		}
		if (fDmgMax <= 0.0 || fInjuries <= 0.0)
		{
			return;
		}
		Ship ship = null;
		while (fInjuries > 0.0 && fDmgMax > 0.0)
		{
			fInjuries -= 1.0;
			CondOwner condOwner = aCrew[MathUtils.Rand(0, aCrew.Count, MathUtils.RandType.Flat, null)];
			if (!(condOwner == null))
			{
				if (condOwner.ship != null && condOwner.ship.LoadState >= Ship.Loaded.Edit)
				{
					ship = condOwner.ship;
				}
				Wound woundLocation = condOwner.GetWoundLocation(true, false);
				if (!(woundLocation == null))
				{
					double num = MathUtils.Rand(0.0, fDmgMax, MathUtils.RandType.Flat, null);
					if (woundLocation.DamageLeft() < num)
					{
						num = woundLocation.DamageLeft();
					}
					woundLocation.Damage(num, 0.0, 0.0, null, string.Empty, true, null, false);
					fDmgMax -= num;
				}
			}
		}
		if (ship != null)
		{
			BeatManager.ResetTensionTimer();
		}
	}

	public static CollisionEvent CollisionEvent = new CollisionEvent();

	private static double fBounceRange = 1.2;

	public static double dMaxSafeV = 100.0;

	public static string strATCClosest = null;

	public static string strBOClosestParallax = null;

	private static CondTrigger ctCollisionSafe;

	private static readonly ShipQueue _shipQueue = new ShipQueue();

	private static readonly Dictionary<string, string> _collisionMemory = new Dictionary<string, string>();
}
