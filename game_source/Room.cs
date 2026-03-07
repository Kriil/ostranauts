using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships.Rooms;
using TMPro;
using UnityEngine;

public class Room
{
	public Room(CondOwner co = null)
	{
		if (co == null)
		{
			this.coRoom = DataHandler.GetCondOwner("Compartment");
		}
		else
		{
			this.coRoom = co;
		}
		this.coRoom.currentRoom = this;
		this.Void = false;
		this.bOuter = false;
		this.aTiles = new List<Tile>();
		if (CrewSim.bDebugShow)
		{
			this.txtID = UnityEngine.Object.Instantiate<TMP_Text>(Resources.Load<TMP_Text>("prefabTextFloatUI"), this.coRoom.transform);
			this.txtID.text = this.coRoom.strNameFriendly + " " + this.coRoom.strID.Substring(0, 6);
			this.txtID.fontSize = 11f;
			this.txtID.gameObject.name = this.txtID.text;
		}
		this.strLastID = this.coRoom.strID;
	}

	public double RoomValue { get; private set; }

	public Tile GetRandomWalkableTile()
	{
		if (Time.realtimeSinceStartup - this.fTimeLastTileShuffle > this.fTimeBetweenShuffles || this.aTilesRandom == null)
		{
			this.aTilesRandom = this.aTiles.ToArray();
			MathUtils.ShuffleArray<Tile>(this.aTilesRandom);
			this.fTimeLastTileShuffle = Time.realtimeSinceStartup;
		}
		foreach (Tile tile in this.aTilesRandom)
		{
			if (tile.bPassable)
			{
				return tile;
			}
		}
		return null;
	}

	public RoomSpec GetRoomSpec()
	{
		if (this._roomSpec != null)
		{
			return this._roomSpec;
		}
		this.CreateRoomSpecs();
		RoomSpec result;
		if ((result = this._roomSpec) == null)
		{
			result = (this._roomSpec = DataHandler.dictRoomSpec["Blank"]);
		}
		return result;
	}

	public void SyncAtmoVoid(JsonAtmosphere jsonAtmosphere)
	{
		if (!this.bVoid || this.coRoom == null || this.coRoom.GasContainer == null)
		{
			return;
		}
		this.coRoom.GasContainer.SyncAtmo(jsonAtmosphere);
	}

	public void CreateRoomSpecs()
	{
		if (Room.aRmSpecs == null)
		{
			Room.aRmSpecs = (from rs in new List<RoomSpec>(DataHandler.dictRoomSpec.Values)
			orderby rs.nPriority descending
			select rs).ToList<RoomSpec>();
		}
		foreach (RoomSpec roomSpec in Room.aRmSpecs)
		{
			if (roomSpec != null && !roomSpec.IsBlank)
			{
				if (roomSpec.Matches(this))
				{
					this._roomSpec = roomSpec;
					break;
				}
			}
		}
		if (this._roomSpec == null)
		{
			this._roomSpec = DataHandler.dictRoomSpec["Blank"];
		}
		this.CalculateRoomValue();
	}

	public void RemoveTile(Tile til)
	{
		if (til == null)
		{
			return;
		}
		this.aTiles.Remove(til);
		this.aTilesRandom = null;
	}

	public void Destroy()
	{
		if (this.aTiles != null)
		{
			this.aTiles.Clear();
			this.aTiles = null;
		}
		if (this.coRoom != null)
		{
			this.coRoom.Destroy();
			this.coRoom = null;
		}
	}

	public JsonRoom GetJSONSave()
	{
		JsonRoom jsonRoom = new JsonRoom();
		jsonRoom.bVoid = this.bVoid;
		jsonRoom.strID = this.coRoom.strID;
		jsonRoom.roomSpec = ((this._roomSpec == null) ? null : this._roomSpec.strName);
		if (this.aCos != null)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < this.aTiles.Count; i++)
			{
				list.Add(this.aTiles[i].Index);
			}
			jsonRoom.aTiles = list.ToArray();
		}
		jsonRoom.roomValue = this.RoomValue;
		return jsonRoom;
	}

	public override string ToString()
	{
		if (this.coRoom != null)
		{
			return "Room_" + this.coRoom.ToString();
		}
		return this.strLastID;
	}

	public void ShowID(bool bShow)
	{
		if (this.txtID == null)
		{
			return;
		}
		this.txtID.gameObject.SetActive(bShow);
		if (bShow)
		{
			this.txtID.text = string.Concat(new object[]
			{
				this.CO.strNameFriendly,
				"; Doors: ",
				this.dictDoors.Count,
				"; ",
				this.CO.strID.Substring(0, 6)
			});
		}
	}

	private void UpdateID()
	{
		if (this.txtID != null && this.txtID.gameObject.activeInHierarchy)
		{
			this.txtID.text = string.Concat(new object[]
			{
				this.CO.strNameFriendly,
				"; Doors: ",
				this.dictDoors.Count,
				"; ",
				this.CO.strID.Substring(0, 6)
			});
			this.strLastID = this.CO.strID;
		}
	}

	public void CalculateRoomValue()
	{
		this.RoomValue = 0.0;
		float num = (this._roomSpec == null) ? 1f : this._roomSpec.ValueModifier;
		foreach (CondOwner condOwner in this.aCos)
		{
			this.RoomValue += condOwner.GetBasePrice(true) * (double)num;
		}
	}

	public void AddToRoom(CondOwner co, bool addEffects = true)
	{
		if (co == null || this.CO == null || this.aCos.Contains(co))
		{
			return;
		}
		if (co.HasCond("IsInstalled", false))
		{
			this.aCos.Add(co);
			if (addEffects)
			{
				if (co.HasCond("IsModeSwitching", false))
				{
					co.ZeroCondAmount("IsModeSwitching");
				}
				else
				{
					this.CreateRoomSpecs();
				}
			}
		}
		if (addEffects)
		{
			this.AddEffectsToRoom(co);
			if (!CrewSimTut.HasCompletedHelmetAtmoTutorial && CrewSim.coPlayer == co)
			{
				CrewSimTut.CheckHelmetAtmoTutorial();
			}
		}
	}

	public void RemoveFromRoom(CondOwner co)
	{
		if (co == null || this.CO == null)
		{
			return;
		}
		if (this.aCos.Remove(co) && !co.HasCond("IsModeSwitching", false))
		{
			if (this.IsRoomSpecValid())
			{
				this.CalculateRoomValue();
			}
			else
			{
				this.CreateRoomSpecs();
			}
		}
		this.RemoveEffectsFromRoom(co);
	}

	private bool IsRoomSpecValid()
	{
		return this._roomSpec != null && !this._roomSpec.IsBlank && this._roomSpec.Matches(this);
	}

	private void AddEffectsToRoom(CondOwner co)
	{
		this.UpdateEffectsOnRoom(co, 1f);
	}

	private void RemoveEffectsFromRoom(CondOwner co)
	{
		this.UpdateEffectsOnRoom(co, -1f);
	}

	private void UpdateEffectsOnRoom(CondOwner co, float amount)
	{
		if (co == null)
		{
			return;
		}
		if (this.CO == null)
		{
			Debug.Log("ERROR: Removing effects from room with null CO!");
			Debug.Break();
			return;
		}
		if (this.CO == co)
		{
			Debug.Log("ERROR: Room removing effects from self!");
			return;
		}
		foreach (Condition condition in co.mapConds.Values)
		{
			if (condition.bRoom)
			{
				this.CO.AddCondAmount(condition.strName, (double)amount * condition.fCount, 0.0, 0f);
			}
		}
		this.UpdateID();
	}

	public void ResetBorderCOCache()
	{
		if (this.CO == null || this.CO.GasContainer == null)
		{
			return;
		}
		this.CO.GasContainer.CachedBorderCOs.Clear();
	}

	public CondOwner CO
	{
		get
		{
			return this.coRoom;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.coRoom.currentRoom = null;
			this.coRoom.Destroy();
			this.coRoom = value;
			this.coRoom.currentRoom = this;
			this.strLastID = this.coRoom.strID;
			this.Void = (this.coRoom.GetCondAmount("StatVolume") == 0.0);
			this.bOuter = false;
		}
	}

	public bool Void
	{
		get
		{
			return this.bVoid;
		}
		set
		{
			if (this.bVoid && !value)
			{
				double condAmount = this.coRoom.GetCondAmount("StatVolume");
				this.coRoom.SetCondAmount("StatVolume", condAmount - 1E+99, 0.0);
			}
			this.bVoid = value;
			if (this.bVoid)
			{
				this.coRoom.strName = DataHandler.GetString("ROOM_NAME_OUTSIDE", false);
				this.coRoom.SetCondAmount("StatVolume", 1E+99, 0.0);
			}
			else
			{
				this.coRoom.strName = DataHandler.GetString("ROOM_NAME_INSIDE", false);
				this.bOuter = false;
			}
		}
	}

	public bool IsAirless
	{
		get
		{
			return this.CO == null || this.CO.HasCond("DcGasPressure01");
		}
	}

	private bool bVoid;

	public bool bOuter;

	public List<Tile> aTiles;

	private Tile[] aTilesRandom;

	private float fTimeLastTileShuffle;

	private float fTimeBetweenShuffles = 5f;

	private CondOwner coRoom;

	private TMP_Text txtID;

	private string strLastID;

	public UniqueList<CondOwner> aCos = new UniqueList<CondOwner>();

	public Dictionary<string, int> dictDoors = new Dictionary<string, int>();

	private static List<RoomSpec> aRmSpecs;

	private RoomSpec _roomSpec;
}
