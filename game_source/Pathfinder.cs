using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.Pathing;
using Ostranauts.Tools.ExtensionMethods;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Events;

// Runtime movement/pathing controller for a CondOwner. Likely bridges crew or
// item movement requests into Jump Point Search, tile checks, and walk anims.
public class Pathfinder : MonoBehaviour
{
	// Current movement destination CondOwner. Updating this also forwards the
	// target into the active path-search provider.
	public CondOwner coDest
	{
		get
		{
			return this._coDest;
		}
		set
		{
			this._coDest = value;
			if (this._pathSearchProvider != null)
			{
				this._pathSearchProvider.coDest = this._coDest;
			}
		}
	}

	// Unity setup: registers this pathfinder with CrewSim, resolves the fatigue
	// cost from loot data, and prepares the search provider.
	private void Awake()
	{
		CrewSim.pathfinders.Add(this);
		this.debugPath = new List<GameObject>();
		if (Pathfinder.fFatigue < 0.0)
		{
			Loot loot = DataHandler.GetLoot("CTWalkFatiguePer");
			List<CondTrigger> ctloot = loot.GetCTLoot(null, null);
			if (ctloot.Count > 0)
			{
				Pathfinder.fFatigue = (double)ctloot[0].fCount;
			}
			else
			{
				Pathfinder.fFatigue = 0.0;
			}
		}
		if (this.coUs == null)
		{
			this.coUs = base.GetComponent<CondOwner>();
		}
		if (this._pathSearchProvider == null)
		{
			this._pathSearchProvider = new JumpPointSearch();
		}
		this._pathSearchProvider.coUs = this.coUs;
		this._pathSearchProvider.pf = this;
	}

	// Local teardown helper for removing debug path visuals and deregistering.
	public void Destroy()
	{
		CrewSim.pathfinders.Remove(this);
		if (this.goPath)
		{
			UnityEngine.Object.Destroy(this.goPath);
		}
	}

	// Cancels the current walk interaction by zeroing its remaining duration.
	private void BumpInteraction(Interaction ia)
	{
		this.HideFootprints();
		ia.fDuration = 0.0;
		this.coUs.SetTicker("Walk", 0f);
	}

	// Switches between normal and zero-g walk/idle animations based on the tile.
	public string GetAnimFor(string strAnimIn)
	{
		string result = strAnimIn;
		if (this.tilCurrent != null && TileUtils.CTShipTile.Triggered(this.tilCurrent.coProps, null, true) && !Tile.IsEVATile(this.tilCurrent))
		{
			if (strAnimIn == "Walk")
			{
				result = "Walk";
			}
			else if (strAnimIn == "Idle")
			{
				result = "Idle";
			}
		}
		else if (strAnimIn == "Walk")
		{
			result = "Spaced1";
		}
		else if (strAnimIn == "Idle")
		{
			result = "Spaced1";
		}
		return result;
	}

	// Registers one trigger-zone listener used by callers that care about path
	// movement crossing special map zones.
	public void AddTriggerListener(UnityAction<JsonZone> caller)
	{
		if (this.OnTriggerZoneEnter == null)
		{
			this.OnTriggerZoneEnter = new TriggerZoneEvent();
		}
		this.OnTriggerZoneEnter.RemoveListener(caller);
		this.OnTriggerZoneEnter.AddListener(caller);
	}

	// Removes a previously registered trigger-zone listener.
	public void RemoveTriggerListener(UnityAction<JsonZone> caller)
	{
		if (this.OnTriggerZoneEnter != null)
		{
			this.OnTriggerZoneEnter.RemoveListener(caller);
		}
	}

	// Rebuilds the current tile reference from world position, with a nearby-tile
	// fallback when the exact tile cannot be resolved.
	public void ReacquireTILCurrent()
	{
		this.tilCurrent = this.coUs.ship.GetTileAtWorldCoords1(this.TF.position.x, this.TF.position.y, true, true);
		if (this.tilCurrent == null)
		{
			this.tilCurrent = Pathfinder.GetAnyNearbyTile(this.coUs);
		}
	}

	// Manual driver for systems that want to tick pathing outside Unity Update.
	public void UpdateManual()
	{
		this.Update();
	}

	// Keeps the actor's current walk/idle animation aligned with ship interior
	// versus EVA movement mode.
	private bool SetWalkAnimation()
	{
		bool result = false;
		bool flag = false;
		if (TileUtils.CTShipTile.Triggered(this.tilCurrent.coProps, null, true) && !Tile.IsEVATile(this.tilCurrent))
		{
			if (this.coUs.strWalkAnim == "Spaced1")
			{
				this.coUs.strWalkAnim = "Walk";
				flag = true;
			}
			if (this.coUs.strIdleAnim == "Spaced1")
			{
				this.coUs.strIdleAnim = "Idle";
				flag = true;
			}
		}
		else
		{
			if (this.coUs.strWalkAnim == "Walk")
			{
				this.coUs.strWalkAnim = "Spaced1";
				flag = true;
			}
			if (this.coUs.strIdleAnim == "Idle")
			{
				this.coUs.strIdleAnim = "Spaced1";
				flag = true;
			}
			result = true;
		}
		if (flag)
		{
			this.coUs.RefreshAnim();
		}
		return result;
	}

	private void Update()
	{
		this.coUs.SetCrewZOffset();
		if (this.coUs.ship == null)
		{
			Debug.Log("Pathfinder on null ship: " + this.coUs);
			return;
		}
		if (this.tilCurrent == null)
		{
			this.ReacquireTILCurrent();
			if (this.tilCurrent == null)
			{
				this.coUs.AICancelCurrent();
				return;
			}
		}
		if (this.coUs.currentRoom == null || this.coUs.currentRoom.CO == null)
		{
			this.coUs.currentRoom = this.tilCurrent.room;
		}
		if (this.strDestShip != null)
		{
			Ship ship = this.coUs.ship;
			if (this.strDestShip != ship.strRegID)
			{
				ship = CrewSim.system.GetShipByRegID(this.strDestShip);
			}
			if (ship != null)
			{
				if (ship.aTiles.Count > this.nDestTile)
				{
					Tile tile = ship.aTiles[this.nDestTile];
					CondOwner condOwner = null;
					if (this.strDestCO != null)
					{
						DataHandler.mapCOs.TryGetValue(this.strDestCO, out condOwner);
					}
					if (this.coUs.aQueue.Count > 0)
					{
						Tile tilDestNew = tile;
						float fRange = this.coUs.aQueue[0].fTargetPointRange;
						CondOwner coDest = condOwner;
						bool bAllowAirlocks = this.coUs.HasAirlockPermission(this.coUs.aQueue[0].bManual);
						this.SetGoal2(tilDestNew, fRange, coDest, 0f, 0f, bAllowAirlocks);
					}
					else
					{
						Tile tilDestNew = tile;
						float fRange = 0f;
						CondOwner coDest = condOwner;
						bool bAllowAirlocks = this.coUs.HasAirlockPermission(false);
						this.SetGoal2(tilDestNew, fRange, coDest, 0f, 0f, bAllowAirlocks);
					}
				}
			}
			else
			{
				Debug.Log("Warning: Destination ship " + this.strDestShip + " not found for pathfinder " + this.coUs.strID);
			}
			this.strDestShip = null;
			this.strDestCO = null;
			this.nDestTile = -1;
		}
		if (this.tilCurrent.coProps.ship != this.coUs.ship && !this.coUs.HasCond("IsSlotted"))
		{
			string strRegID = this.coUs.ship.strRegID;
			this.coUs.ship.RemoveCO(this.coUs, false);
			if (this.tilCurrent.coProps.ship != null)
			{
				this.tilCurrent.coProps.ship.AddCO(this.coUs, true);
			}
			else
			{
				Debug.Log(string.Concat(new object[]
				{
					this.coUs,
					" just walked off ship ",
					strRegID,
					" and onto null!"
				}));
			}
		}
		Interaction interactionCurrent = this.coUs.GetInteractionCurrent();
		if (interactionCurrent == null || interactionCurrent.strName != "Walk")
		{
			this.HideFootprints();
			return;
		}
		if (this.fRangeGoal < 0f)
		{
			this.HideFootprints();
			this.coUs.ClearInteraction(interactionCurrent, false);
			return;
		}
		if (this.currentPath.Count <= 0)
		{
			this.HideFootprints();
			this.coUs.ClearInteraction(interactionCurrent, false);
			return;
		}
		if (this.tilDest == null)
		{
			this.tilDest = this.tilCurrent;
		}
		if (this.ShowDebugLogs & this.tilCurrent != null)
		{
			this.tilCurrent.SetColor(Color.green);
			this.tilCurrent.ToggleVis(true);
		}
		int num = Math.Max(0, this.currentPath.IndexOf(this.tilCurrentPath));
		bool flag = this.SetWalkAnimation();
		if (num <= this.currentPath.Count - 1)
		{
			Tile tile2 = this.currentPath[num];
			if (num + 1 <= this.currentPath.Count - 1)
			{
				tile2 = this.currentPath[num + 1];
			}
			if (tile2 == null || tile2.tf == null)
			{
				this.coUs.AICancelCurrent();
				return;
			}
			Vector2 a = new Vector2(tile2.tf.position.x, tile2.tf.position.y);
			if (this.ShowDebugLogs && tile2 != null)
			{
				tile2.SetColor(Color.red);
				tile2.ToggleVis(true);
			}
			bool flag2 = true;
			if (tile2 == this.tilCurrent)
			{
				this.BumpInteraction(interactionCurrent);
				flag2 = false;
			}
			else if (tile2.coProps.HasCond("IsPortal") && tile2.coProps.HasCond("IsWall"))
			{
				float num2 = Vector2.Distance(this.tilCurrent.tf.position.ToVector2(), tile2.tf.position.ToVector2());
				if ((double)num2 < 1.5)
				{
					List<CondOwner> list = new List<CondOwner>();
					tile2.coProps.ship.GetCOsAtWorldCoords1(tile2.tf.position, null, true, false, list);
					foreach (CondOwner condOwner2 in list)
					{
						if (condOwner2.HasCond("IsPortal") && !condOwner2.HasCond("IsOpen") && !condOwner2.HasCond("IsTile"))
						{
							flag2 = false;
							if (condOwner2.GetInteractionCurrent() == null)
							{
								Interaction interaction = DataHandler.GetInteraction("SysExtraTimeZero", null, false);
								interaction.bManual = interactionCurrent.bManual;
								this.coUs.QueueInteraction(this.coUs, interaction, true);
								interaction = DataHandler.GetInteraction("MSPortalOpenStart", null, false);
								interaction.bManual = interactionCurrent.bManual;
								this.coUs.QueueInteraction(condOwner2, interaction, true);
								break;
							}
						}
					}
				}
			}
			else if (tile2.coProps.HasCond("IsObstruction") && !tile2.coProps.HasCond("IsOpen"))
			{
				this.BumpInteraction(interactionCurrent);
				flag2 = false;
			}
			if (flag2)
			{
				a.x -= this.TF.position.x;
				a.y -= this.TF.position.y;
				float num3 = a.magnitude + 1E-08f;
				float num4 = (float)(1.0 - this.coUs.GetCondAmount("StatMovSpeedPenalty"));
				float num5 = CrewSim.TimeElapsedScaled() * 50f * 1.31f;
				if (flag)
				{
					num5 *= 0.5f;
				}
				if (CrewSim.GetSelectedCrew() == this.coUs)
				{
					AudioManager.am.Grounded = !flag;
				}
				num4 = Mathf.Max(num4, 0.05f);
				this.coUs.AddCondAmount("StatFatigue", Pathfinder.fFatigue * (double)CrewSim.TimeElapsedScaled(), 0.0, 0f);
				if (!CrewSim.Paused)
				{
					float num6;
					if (this.coUs.HasCond("IsDragging"))
					{
						Vector3 vector = new Vector3(0f, 1f, 0f);
						CondOwner outermostCO = this.coUs.compSlots.GetSlot("drag").GetOutermostCO();
						if (outermostCO != null)
						{
							vector = outermostCO.tf.position - this.tf.position;
						}
						else
						{
							this.coUs.ZeroCondAmount("IsDragging");
						}
						num6 = Mathf.Atan2(-vector.x, vector.y) * 57.295776f;
						num4 *= 0.5f;
					}
					else
					{
						num6 = Mathf.Atan2(-a.x, a.y) * 57.295776f;
					}
					float num7 = this.TF.rotation.eulerAngles.z;
					float num8 = MathUtils.NormalizeAngleDegrees(num6 + 180f - num7) - 180f;
					if (Mathf.Abs(num8) > 30f)
					{
						num4 *= 0.25f;
					}
					float num9 = 500f * CrewSim.TimeElapsedScaled();
					num7 += MathUtils.Clamp(num8, -num9, num9);
					if (a.x == 0f && a.y == 0f)
					{
						this.BumpInteraction(interactionCurrent);
					}
					else
					{
						this.TF.rotation = Quaternion.AngleAxis(num7, Vector3.forward);
					}
				}
				a *= Mathf.Min(num4 * num5 / num3, 16f);
				this.coUs.SetCrewZOffset();
				this.TF.Translate(a.x / 16f, a.y / 16f, 0f, Space.World);
				Tile tileAtWorldCoords = this.coUs.ship.GetTileAtWorldCoords1(this.TF.position.x, this.TF.position.y, true, true);
				if (tileAtWorldCoords != this.tilCurrent)
				{
					bool flag3 = this.tilCurrent != null && !this.tilCurrent.bPassable;
					if (tileAtWorldCoords == null || (!tileAtWorldCoords.bPassable && !tileAtWorldCoords.IsPortal && !flag3))
					{
						Vector3 vector2 = Vector3.zero;
						if (tileAtWorldCoords != null && this.tilCurrent != null)
						{
							vector2 = this.tilCurrent.tf.position - tileAtWorldCoords.tf.position;
						}
						else if (this.tilCurrent != null)
						{
							vector2 = this.tilCurrent.tf.position - this.TF.position;
						}
						float num10 = Mathf.Abs(vector2.x);
						float num11 = Mathf.Abs(vector2.y);
						float num12 = 0.1f;
						if (num10 > num11)
						{
							a = new Vector2(-a.x, 0f);
						}
						else if (num10 < num11)
						{
							a = new Vector2(0f, -a.y);
						}
						else
						{
							a = new Vector2(vector2.x * num12, vector2.y * num12);
						}
						this.TF.Translate(a.x / 16f, a.y / 16f, 0f, Space.World);
						tileAtWorldCoords = this.tilCurrent;
					}
					if (tileAtWorldCoords != this.tilCurrent && tileAtWorldCoords != null)
					{
						if (this.coUs.currentRoom != tileAtWorldCoords.room)
						{
							if (this.coUs.currentRoom != null)
							{
								this.coUs.currentRoom.RemoveFromRoom(this.coUs);
							}
							if (tileAtWorldCoords.room != null)
							{
								tileAtWorldCoords.room.AddToRoom(this.coUs, true);
							}
							this.coUs.currentRoom = tileAtWorldCoords.room;
						}
						if (this.OnTriggerZoneEnter != null && tileAtWorldCoords.IsTriggerZone(this.coUs))
						{
							this.OnTriggerZoneEnter.Invoke(tileAtWorldCoords.jZone);
						}
						if (this.ShowDebugLogs)
						{
							this.tilCurrent.ToggleVis(false);
							tileAtWorldCoords.ToggleVis(true);
						}
						this.tilCurrent = tileAtWorldCoords;
						if (this.currentPath.Contains(tileAtWorldCoords))
						{
							this.tilCurrentPath = tileAtWorldCoords;
							if (this.ShowDebugLogs && tileAtWorldCoords == this.tilDest)
							{
								TileUtils.ToggleShipTileVisibility(false, CrewSim.coPlayer.ship.aTiles, false);
							}
						}
						else if (this.currentPath.Count > num + 1)
						{
							this.currentPath.Insert(num + 1, tileAtWorldCoords);
							this.tilCurrentPath = tileAtWorldCoords;
						}
						if (this.tilCurrent == this.tilDest || this.HasTargetMoved())
						{
							this.BumpInteraction(interactionCurrent);
						}
					}
				}
			}
		}
		else
		{
			this.HideFootprints();
			interactionCurrent.fDuration = 0.0;
			this.coUs.SetTicker("Walk", 0f);
		}
		if (this.goPath && this.goPath.activeSelf)
		{
			this.goPath.transform.position = this.lastPosition;
			this.goPath.transform.rotation = this.lastRotation;
		}
	}

	public static bool CheckPressure(float pressureA, float pressureB)
	{
		return pressureA + 1f < pressureB / 2f;
	}

	public static bool CheckPressure(Vector2 position, float deltaX, float deltaY, float roomPressure, Ship ship)
	{
		if (ship == null)
		{
			return false;
		}
		Vector2 vPos = new Vector2(position.x + deltaX, position.y + deltaY);
		Room roomAtWorldCoords = ship.GetRoomAtWorldCoords1(vPos, true);
		if (roomAtWorldCoords == null)
		{
			return false;
		}
		float num = (float)GasExchange.GetStatGasPressure(roomAtWorldCoords.CO);
		return Pathfinder.CheckPressure(roomPressure, num) || Pathfinder.CheckPressure(num, roomPressure);
	}

	public static bool CheckDoorPressure(Vector2 vPosition, Ship objShip, Room room)
	{
		if (room == null || objShip == null)
		{
			return false;
		}
		bool flag = false;
		float roomPressure = (float)GasExchange.GetStatGasPressure(room.CO);
		flag |= Pathfinder.CheckPressure(vPosition, 1f, 0f, roomPressure, objShip);
		if (flag)
		{
			return true;
		}
		flag |= Pathfinder.CheckPressure(vPosition, -1f, 0f, roomPressure, objShip);
		if (flag)
		{
			return true;
		}
		flag |= Pathfinder.CheckPressure(vPosition, 0f, 1f, roomPressure, objShip);
		return flag || (flag | Pathfinder.CheckPressure(vPosition, 0f, -1f, roomPressure, objShip));
	}

	public static bool CheckPressure(Vector2 vPosition, Ship objShip, Room room)
	{
		if (room == null || objShip == null)
		{
			return false;
		}
		bool flag = false;
		List<CondOwner> list = new List<CondOwner>();
		objShip.GetCOsAtWorldCoords1(vPosition, null, true, false, list);
		foreach (CondOwner condOwner in list)
		{
			if (condOwner.HasCond("IsPortal") && !condOwner.HasCond("IsOpen") && !condOwner.HasCond("IsTile"))
			{
				float roomPressure = (float)GasExchange.GetStatGasPressure(room.CO);
				flag |= Pathfinder.CheckPressure(vPosition, 1f, 0f, roomPressure, objShip);
				flag |= Pathfinder.CheckPressure(vPosition, -1f, 0f, roomPressure, objShip);
				flag |= Pathfinder.CheckPressure(vPosition, 0f, 1f, roomPressure, objShip);
				flag |= Pathfinder.CheckPressure(vPosition, 0f, -1f, roomPressure, objShip);
			}
		}
		return flag;
	}

	public static Tile GetAnyNearbyTile(CondOwner co)
	{
		Tile tileAtWorldCoords = co.ship.GetTileAtWorldCoords1(co.tf.position.x + 1f, co.tf.position.y, true, true);
		if (tileAtWorldCoords == null)
		{
			tileAtWorldCoords = co.ship.GetTileAtWorldCoords1(co.tf.position.x - 1f, co.tf.position.y, true, true);
		}
		if (tileAtWorldCoords == null)
		{
			tileAtWorldCoords = co.ship.GetTileAtWorldCoords1(co.tf.position.x, co.tf.position.y + 1f, true, true);
		}
		if (tileAtWorldCoords == null)
		{
			tileAtWorldCoords = co.ship.GetTileAtWorldCoords1(co.tf.position.x, co.tf.position.y - 1f, true, true);
		}
		return tileAtWorldCoords;
	}

	private PathResult GetPath(Tile destination, bool bAllowAirlocks)
	{
		if (this._failedAttempts.Item2 == null)
		{
			this._failedAttempts.Item2 = new List<Tile>();
		}
		if (destination == null || this.coUs == null || this.coUs.ship == null)
		{
			return new PathResult();
		}
		Tile tile = this.coUs.ship.GetTileAtWorldCoords1(this.coUs.tf.position.x, this.coUs.tf.position.y, true, true);
		if (tile == null)
		{
			if (this.tilCurrent != null)
			{
				tile = this.tilCurrent;
			}
			else
			{
				tile = Pathfinder.GetAnyNearbyTile(this.coUs);
			}
			if (tile == null)
			{
				Debug.Log("  Pathfinder.GetPath() when this.origin is null! Should never happen...");
				return new PathResult();
			}
		}
		if (tile != this._failedAttempts.Item1)
		{
			this._failedAttempts.Item2.Clear();
		}
		this._failedAttempts.Item1 = tile;
		List<PathResult> list = new List<PathResult>();
		HashSet<Tile> hashSet = new HashSet<Tile>();
		for (int i = 0; i < CrewSim.pathfinders.Count; i++)
		{
			if (!(CrewSim.pathfinders[i] == null) && !(CrewSim.pathfinders[i].coUs == null) && CrewSim.pathfinders[i].coUs.ship != null && CrewSim.pathfinders[i].coUs.ship.LoadState >= Ship.Loaded.Edit)
			{
				if (CrewSim.pathfinders[i].coUs != this.coUs)
				{
					hashSet.Add(CrewSim.pathfinders[i].tilCurrent);
					hashSet.Add(CrewSim.pathfinders[i].tilDest);
				}
			}
		}
		List<Tile> closestWalkableDestination = this.GetClosestWalkableDestination(tile, destination, hashSet, this.fRangeGoal);
		if (closestWalkableDestination.Count == 0)
		{
			closestWalkableDestination.Add(destination);
		}
		if (!tile.IsWalkable(this.coUs, new PathResult(bAllowAirlocks)))
		{
			List<Tile> pathToWalkableOrigin = this.GetPathToWalkableOrigin(tile, destination);
			if (pathToWalkableOrigin != null)
			{
				tile = pathToWalkableOrigin[pathToWalkableOrigin.Count - 1];
			}
		}
		using (List<Tile>.Enumerator enumerator = closestWalkableDestination.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				Tile tile2 = enumerator.Current;
				bool flag = false;
				bool flag2 = false;
				if (tile2 != null)
				{
					if (tile2.jZone != null && tile2.coProps != null && tile2.coProps.HasCond("IsForbiddenZone", false))
					{
						flag2 = true;
					}
					if (tile2.IsEvaTileWithGravitation())
					{
						flag = true;
					}
				}
				if (flag || flag2)
				{
					if (this._failedAttempts.Item2.Contains(tile2))
					{
						PathResult pathResult = new PathResult(tile, tile2);
						pathResult.SetTiles(null);
						pathResult.bForbidZoneBlocked = flag2;
						pathResult.bGravBlocked = flag;
						list.Add(pathResult);
					}
					else
					{
						this._failedAttempts.Item2.Add(tile2);
						PathResult pathResult = this.GetPath(destination, bAllowAirlocks);
						if (pathResult != null && !pathResult.HasPath)
						{
							this._failedAttempts.Item2.Add(pathResult.Dest);
						}
						list.Add(pathResult);
					}
				}
				else
				{
					PathResult pathResult;
					if (this.Heuristic(tile2, tile) <= this.fRangeGoal)
					{
						bool flag3 = false;
						if (this.coDest != null && this.coDest.ship != null)
						{
							Tile tileAtWorldCoords = this.coDest.ship.GetTileAtWorldCoords1(tile2.tf.position.x, tile2.tf.position.y, true, true);
							if (tileAtWorldCoords == null || Visibility.IsCondOwnerLOSVisibleBlocks(this.coDest, tile.tf.position, tileAtWorldCoords.IsWall, true))
							{
								flag3 = true;
							}
						}
						else
						{
							flag3 = true;
						}
						if (flag3)
						{
							pathResult = new PathResult(tile, tile2);
							pathResult.AddTile(tile2);
							if (tile != tile2)
							{
								pathResult.AddTile(tile);
							}
							list.Add(pathResult);
							goto IL_4D0;
						}
					}
					pathResult = this._pathSearchProvider.GetPath(tile2, bAllowAirlocks, tile);
					if (pathResult != null && !pathResult.HasPath)
					{
						this._failedAttempts.Item2.Add(tile2);
					}
					list.Add(pathResult);
				}
			}
			IL_4D0:;
		}
		if (list.Count != 0 && list[0] != null)
		{
			PathResult pathResult2 = list[0];
			for (int j = 1; j < list.Count; j++)
			{
				if (list[j] != null)
				{
					if (list[j].HasPath && list[j].PathLength < pathResult2.PathLength)
					{
						pathResult2 = list[j];
					}
				}
			}
			return pathResult2;
		}
		return new PathResult();
	}

	private float Heuristic(Tile a, Tile b)
	{
		float num = a.tf.position.x - b.tf.position.x;
		float num2 = a.tf.position.y - b.tf.position.y;
		return Mathf.Sqrt(num * num + num2 * num2);
	}

	private List<Tile> GetPathToWalkableOrigin(Tile origin, Tile destination)
	{
		List<int> list = new List<int>();
		SimplePriorityQueue<Tile> simplePriorityQueue = new SimplePriorityQueue<Tile>();
		Dictionary<Tile, Tile> dictionary = new Dictionary<Tile, Tile>();
		Dictionary<Tile, float> dictionary2 = new Dictionary<Tile, float>();
		Tile[] array = new Tile[8];
		Tile last = null;
		simplePriorityQueue.Enqueue(origin, 0f);
		dictionary[origin] = origin;
		dictionary2[origin] = 0f;
		for (int i = 0; i < 100; i++)
		{
			if (simplePriorityQueue.Count == 0)
			{
				return null;
			}
			Tile tile = simplePriorityQueue.Dequeue();
			if (!tile.coProps.HasCond("IsObstruction"))
			{
				last = tile;
				break;
			}
			TileUtils.GetSurroundingTiles(ref array, tile, false);
			int j = 0;
			while (j < array.Length)
			{
				float num = 1f;
				if (j <= 3)
				{
					goto IL_15B;
				}
				num = 1.4142135f;
				if ((j != 4 || !list.Contains(0)) && (j != 4 || !list.Contains(1)))
				{
					if ((j != 5 || !list.Contains(1)) && (j != 5 || !list.Contains(2)))
					{
						if ((j != 6 || !list.Contains(0)) && (j != 6 || !list.Contains(3)))
						{
							if ((j != 7 || !list.Contains(2)) && (j != 7 || !list.Contains(3)))
							{
								goto IL_15B;
							}
						}
					}
				}
				IL_225:
				j++;
				continue;
				IL_15B:
				if (array[j] == null)
				{
					goto IL_225;
				}
				float num2 = dictionary2[tile] + num;
				if (dictionary2.ContainsKey(array[j]) && num2 >= dictionary2[array[j]])
				{
					goto IL_225;
				}
				if (array[j].IsWall)
				{
					if (j <= 3)
					{
						list.Add(j);
					}
					goto IL_225;
				}
				if (array[j].IsForbidden(this.coUs))
				{
					if (j <= 3)
					{
						list.Add(j);
					}
					goto IL_225;
				}
				dictionary2[array[j]] = num2;
				float priority = num2 + this.Heuristic(destination, array[j]);
				simplePriorityQueue.Enqueue(array[j], priority);
				dictionary[array[j]] = tile;
				goto IL_225;
			}
		}
		return Pathfinder.BuildListFromTilesSearched(dictionary, origin, last);
	}

	public static List<Tile> BuildListFromTilesSearched(Dictionary<Tile, Tile> tilesSearched, Tile origin, Tile last)
	{
		List<Tile> list = new List<Tile>();
		for (;;)
		{
			list.Add(last);
			if (last == origin)
			{
				break;
			}
			last = tilesSearched[last];
		}
		list.Reverse();
		return list;
	}

	private List<Tile> GetClosestWalkableDestination(Tile origin, Tile destination, ICollection<Tile> occupiedTiles, float fRadius)
	{
		this._reverseFrontier.Clear();
		this._candidates.Clear();
		this._cardinalDirectionsObstructed.Clear();
		this._tileCosts.Clear();
		this._reverseFrontier.Enqueue(destination, 0f);
		this._tileCosts[destination] = 0f;
		if (Vector2.Distance(origin.tf.position, destination.tf.position) <= fRadius)
		{
			if (!(this.coDest != null) || this.coDest.ship == null)
			{
				return new List<Tile>
				{
					destination
				};
			}
			Vector3 position = destination.tf.position;
			Tile tileAtWorldCoords = this.coDest.ship.GetTileAtWorldCoords1(position.x, position.y, true, true);
			if (tileAtWorldCoords == null || Visibility.IsCondOwnerLOSVisibleBlocks(this.coDest, origin.tf.position, tileAtWorldCoords.IsWall, true))
			{
				return new List<Tile>
				{
					destination
				};
			}
		}
		int num = Mathf.CeilToInt(this.fRangeGoal);
		float num2 = float.MaxValue;
		for (int i = 0; i < 36; i++)
		{
			if (this._reverseFrontier.Count <= 0)
			{
				break;
			}
			Tile tile = this._reverseFrontier.Dequeue();
			if (!(tile.coProps == null) && this._tileCosts[tile] <= num2 && this._tileCosts[tile] - (float)num <= 1f)
			{
				float num3 = this._tileCosts[tile] - (float)num;
				if (!tile.IsWall && !tile.coProps.HasCond("IsFixture", false) && !occupiedTiles.Contains(tile) && num3 < 0.6f)
				{
					if (this.coDest == null || this.coDest.ship == null)
					{
						if (this.coDest != null && this.coDest.ship == null)
						{
							string str = (!(this.coDest.transform.parent == null)) ? this.coDest.transform.parent.name : "parent is null";
							Debug.LogError("Ship was null: " + this.coDest.gameObject.name + " on: " + str);
						}
						this._candidates.Add(tile);
						num2 = this._tileCosts[tile];
						goto IL_5ED;
					}
					Vector3 position2 = this.coDest.tf.position;
					Tile tileAtWorldCoords2 = this.coDest.ship.GetTileAtWorldCoords1(position2.x, position2.y, true, true);
					if (tileAtWorldCoords2 != null && Visibility.IsCondOwnerLOSVisibleBlocks(this.coDest, tile.tf.position, tileAtWorldCoords2.IsWall, true))
					{
						this._candidates.Add(tile);
						num2 = this._tileCosts[tile];
						goto IL_5ED;
					}
				}
				TileUtils.GetSurroundingTiles(ref this._surroundingTiles, tile, false);
				int j = 0;
				while (j < 8)
				{
					Tile tile2 = this._surroundingTiles[j];
					float num4 = 1f;
					if (j <= 3)
					{
						goto IL_45F;
					}
					num4 = 1.4142135f;
					if (!(origin != tile2))
					{
						goto IL_45F;
					}
					if ((j != 4 || !this._cardinalDirectionsObstructed.Contains(0)) && (j != 4 || !this._cardinalDirectionsObstructed.Contains(1)))
					{
						if ((j != 5 || !this._cardinalDirectionsObstructed.Contains(1)) && (j != 5 || !this._cardinalDirectionsObstructed.Contains(2)))
						{
							if ((j != 6 || !this._cardinalDirectionsObstructed.Contains(0)) && (j != 6 || !this._cardinalDirectionsObstructed.Contains(3)))
							{
								if ((j != 7 || !this._cardinalDirectionsObstructed.Contains(2)) && (j != 7 || !this._cardinalDirectionsObstructed.Contains(3)))
								{
									goto IL_45F;
								}
							}
						}
					}
					IL_5DF:
					j++;
					continue;
					IL_45F:
					if (tile2 == null)
					{
						goto IL_5DF;
					}
					float num5 = 1f;
					if (tile2.room != null)
					{
						if (this.tilDest.room != null)
						{
							if (this.tilDest.room != tile2.room)
							{
								num5 = 1.5f;
								num4 += 0.5f;
							}
						}
						else if (origin != null && origin.room != null && origin.room != tile2.room)
						{
							num5 = 1.5f;
							num4 += 0.5f;
						}
					}
					float num6 = this._tileCosts[tile] + num4;
					if (this._failedAttempts.Item1 == origin && this._failedAttempts.Item2.Contains(tile2))
					{
						num6 += 5f;
					}
					if (this._tileCosts.ContainsKey(tile2) && num6 >= this._tileCosts[tile2])
					{
						goto IL_5DF;
					}
					if (tile2.IsForbidden(this.coUs) || tile2.IsWall)
					{
						if (j <= 3)
						{
							this._cardinalDirectionsObstructed.Add(j);
						}
						goto IL_5DF;
					}
					if (!this.RoomIsReachable(tile2))
					{
						goto IL_5DF;
					}
					this._tileCosts[tile2] = num6;
					this._reverseFrontier.Enqueue(tile2, num6 + this.Heuristic(origin, tile2) * num5);
					goto IL_5DF;
				}
			}
			IL_5ED:;
		}
		return this._candidates;
	}

	private bool RoomIsReachable(Tile tile)
	{
		if (tile.room == null || tile.room == this.coUs.currentRoom)
		{
			return true;
		}
		if (tile.room.bOuter && this.coUs.currentRoom != null && this.coUs.currentRoom.bOuter)
		{
			return true;
		}
		if (tile.room.dictDoors == null || tile.room.dictDoors.Count == 0)
		{
			return false;
		}
		bool result = true;
		foreach (KeyValuePair<string, int> keyValuePair in tile.room.dictDoors)
		{
			if (tile.coProps.ship.TileIndexValid(keyValuePair.Value))
			{
				Tile tile2 = tile.coProps.ship.aTiles[keyValuePair.Value];
				if (tile2.IsPortal && (!tile2.IsWall || !tile2.coProps.HasCond("IsPortalStuck")))
				{
					return true;
				}
				result = false;
			}
		}
		return result;
	}

	public void VisualisePath(List<Tile> path)
	{
	}

	public IEnumerator DrawPathVisualisation(List<Tile> path)
	{
		for (int i = 0; i < path.Count - 1; i++)
		{
			GameObject debugPathUI = UnityEngine.Object.Instantiate<GameObject>(this.goDebugPathPrefab, path[i].tf.parent);
			debugPathUI.transform.position = path[i].tf.position + new Vector3(0f, 0f, -0.02f);
			debugPathUI.SetActive(true);
			this.debugPath.Add(debugPathUI);
			yield return new WaitForSeconds(0.03f);
		}
		yield return new WaitForSeconds(1f);
		base.StartCoroutine("DeletePathVisualisation");
		yield break;
	}

	public IEnumerator DeletePathVisualisation()
	{
		for (int i = 0; i < this.debugPath.Count; i++)
		{
			UnityEngine.Object.Destroy(this.debugPath[i]);
			yield return new WaitForSeconds(0.03f);
		}
		this.debugPath.Clear();
		yield break;
	}

	public bool InRange(Tile tilCheck = null)
	{
		if (tilCheck == null)
		{
			tilCheck = this.tilDest;
		}
		if (tilCheck == null)
		{
			return false;
		}
		Vector2 vector = new Vector2(this.TF.position.x, this.TF.position.y);
		return this.coUs.ship.GetTileAtWorldCoords1(vector.x, vector.y, true, true) == tilCheck && Visibility.IsCondOwnerLOSVisibleBlocks(this.coUs, tilCheck.tf.position, false, true);
	}

	private void ShowFootprints(float fDestX, float fDestY, bool reachable)
	{
		if (this.coUs == null)
		{
			return;
		}
		if (!this.coUs.HasCond("IsPlayer") && !this.coUs.HasCond("IsPlayerCrew"))
		{
			return;
		}
		if (!this.goPath)
		{
			this.goPath = DataHandler.GetMesh("prefabQuadGUI", null);
			this.goPath.name = "PFTarget" + this.TF.gameObject.name;
			this.goPath.transform.parent = base.transform;
			this.Renderer = this.goPath.GetComponent<Renderer>();
			this.goPath.GetComponent<MeshCollider>().enabled = false;
		}
		if (!this.goPath.activeSelf)
		{
			this.goPath.SetActive(true);
		}
		this.lastPosition = new Vector3(fDestX, fDestY, -0.02f);
		float z = Mathf.Atan2(this.TF.position.x - fDestX, fDestY - this.TF.position.y) * 57.295776f;
		this.lastRotation = Quaternion.Euler(0f, 0f, z);
		string strImg = "GUIFootprints";
		if (!reachable)
		{
			strImg = "GUIUnreachableFootprints";
		}
		this.Renderer.sharedMaterial = DataHandler.GetMaterial(this.Renderer, strImg, "blank", "blank", "blank");
	}

	public void HideFootprints()
	{
		if (this.goPath && this.goPath.activeSelf)
		{
			this.goPath.SetActive(false);
		}
	}

	public PathResult SetGoal2(Tile tilDestNew, float fRange, CondOwner coDest, float fDestX = 0f, float fDestY = 0f, bool bAllowAirlocks = false)
	{
		if (tilDestNew == null)
		{
			return new PathResult
			{
				Origin = this.tilCurrent
			};
		}
		if ((double)fDestX == 0.0 && fDestY == 0f)
		{
			fDestX = tilDestNew.tf.position.x;
			fDestY = tilDestNew.tf.position.y;
		}
		if (this.coUs == null)
		{
			this.coUs = base.gameObject.GetComponent<CondOwner>();
		}
		if (this.tilCurrent == null)
		{
			this.ReacquireTILCurrent();
		}
		this.coDest = coDest;
		PathResult pathResult;
		if (this.coUs.ship != tilDestNew.coProps.ship && !this.coUs.HasShoreLeave())
		{
			pathResult = new PathResult(this.tilCurrent, tilDestNew);
			pathResult.bDisembarkBlocked = true;
			this.ShowFootprints(fDestX, fDestY, false);
			return pathResult;
		}
		this.tilDest = tilDestNew;
		this.tilCurrentPath = null;
		this.fRangeGoal = fRange;
		if (tilDestNew == this.tilCurrent && this.InRange(null))
		{
			pathResult = new PathResult(this.tilCurrent, tilDestNew);
			pathResult.AddTile(tilDestNew);
			return pathResult;
		}
		this.ShowFootprints(fDestX, fDestY, true);
		pathResult = this.GetPath(this.tilDest, bAllowAirlocks);
		if (pathResult.Tiles == null)
		{
			this.HideFootprints();
			this.fRangeGoal = -1f;
			this.ShowFootprints(fDestX, fDestY, false);
			return pathResult;
		}
		if (pathResult.Tiles.Count == 1)
		{
			this.fRangeGoal = ((fRange < 1f) ? 1f : fRange);
			this.currentPath.Clear();
			this.tilCurrentPath = null;
			this.currentPath.Add(pathResult.Tiles[0]);
			if (pathResult.Tiles[0] == this.tilCurrent)
			{
				this.tilDest = this.tilCurrent;
			}
		}
		else if (pathResult.Tiles.Count > 0)
		{
			this.fRangeGoal = fRange;
			this.currentPath.Clear();
			this.tilCurrentPath = null;
			this.currentPath.AddRange(pathResult.Tiles);
			this.tilDest = this.currentPath[this.currentPath.Count - 1];
		}
		return pathResult;
	}

	public void ResetMemory()
	{
		this._pathMemory.Reset();
	}

	public PathResult CheckGoal(Tile tilDestNew, float fRange, CondOwner coDest, bool bAllowAirlocks)
	{
		if (tilDestNew == null)
		{
			return new PathResult
			{
				Origin = this.tilCurrent
			};
		}
		if (this.coUs == null)
		{
			this.coUs = base.gameObject.GetComponent<CondOwner>();
		}
		if (this.tilCurrent == null)
		{
			this.ReacquireTILCurrent();
		}
		this.coDest = coDest;
		if (this.coUs.ship != tilDestNew.coProps.ship && !this.coUs.HasShoreLeave())
		{
			return new PathResult(this.tilCurrent, tilDestNew)
			{
				bDisembarkBlocked = true
			};
		}
		Tile tile = this.tilDest;
		float num = this.fRangeGoal;
		PathResult pathResult;
		if (tilDestNew == this.tilCurrent && this.InRange(tilDestNew))
		{
			pathResult = new PathResult(this.tilCurrent, tilDestNew);
			pathResult.AddTile(tilDestNew);
			return pathResult;
		}
		this.tilDest = tilDestNew;
		this.fRangeGoal = fRange;
		PathResult pathResult2 = this._pathMemory.HasResult(this.tilCurrent, this.tilDest);
		if (pathResult2 != null)
		{
			return pathResult2;
		}
		Vector2 vector = new Vector2(this.tilDest.tf.position.x, this.tilDest.tf.position.y);
		this.ShowFootprints(vector.x, vector.y, true);
		pathResult = this.GetPath(this.tilDest, bAllowAirlocks);
		this.tilDest = tile;
		this.fRangeGoal = num;
		this._pathMemory.RememberResult(pathResult);
		if (pathResult.Tiles == null)
		{
			this.HideFootprints();
			return pathResult;
		}
		if (pathResult.Disembark && !this.coUs.HasShoreLeave())
		{
			return new PathResult(this.tilCurrent, tilDestNew)
			{
				bDisembarkBlocked = true
			};
		}
		return pathResult;
	}

	private bool HasTargetMoved()
	{
		if (this.coDest == null || this.tilDest == null || this.coDest.Crew == null)
		{
			return false;
		}
		float num = Mathf.Max(1f, this.fRangeGoal * 2f);
		return Vector3.Distance(this.coDest.tfVector2Position, this.tilDest.tf.position.ToVector2()) > num;
	}

	public void Reset()
	{
		if (this.coUs == null)
		{
			this.coUs = base.gameObject.GetComponent<CondOwner>();
		}
		this.tilDest = null;
		this.fRangeGoal = 0f;
		this.tilCurrentPath = null;
		this.currentPath.Clear();
	}

	public Transform TF
	{
		get
		{
			if (this.tf == null)
			{
				this.tf = base.transform;
			}
			return this.tf;
		}
	}

	private TriggerZoneEvent OnTriggerZoneEnter;

	private GameObject goPath;

	public Tile tilDest;

	public Tile tilCurrent;

	public Tile tilCurrentPath;

	public CondOwner coUs;

	private bool ShowDebugLogs;

	private CondOwner _coDest;

	public float fRangeGoal = -1f;

	private static double fFatigue = -1.0;

	private Transform tf;

	private Vector3 lastPosition;

	private Quaternion lastRotation;

	private Renderer Renderer;

	private Material ReachableMat;

	private Material NotReachableMat;

	public string strDestShip;

	public string strDestCO;

	public int nDestTile = -1;

	public readonly List<Tile> currentPath = new List<Tile>();

	private readonly Tuple<Tile, List<Tile>> _failedAttempts = new Tuple<Tile, List<Tile>>();

	public GameObject goDebugPathPrefab;

	public List<GameObject> debugPath;

	private IPathSearchProvider _pathSearchProvider;

	private readonly PathMemory _pathMemory = new PathMemory();

	private readonly SimplePriorityQueue<Tile> _reverseFrontier = new SimplePriorityQueue<Tile>();

	private readonly List<int> _cardinalDirectionsObstructed = new List<int>(8);

	private readonly Dictionary<Tile, float> _tileCosts = new Dictionary<Tile, float>(1000);

	private readonly List<Tile> _candidates = new List<Tile>();

	private Tile[] _surroundingTiles = new Tile[8];
}
