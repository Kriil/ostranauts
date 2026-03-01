using System;
using System.Collections.Generic;

namespace Ostranauts.Ships.Rooms
{
	// Runtime room-classification spec built from JsonRoomSpec. Converts the JSON
	// requirement/forbid arrays into CondTrigger lists used to classify live rooms.
	public class RoomSpec
	{
		// Rehydrates the runtime matcher and synthesizes temporary Loot tables so the
		// existing condtrig parsing path can be reused.
		public RoomSpec(JsonRoomSpec jsonRoomSpec)
		{
			this.strName = jsonRoomSpec.strName;
			this.strNameFriendly = jsonRoomSpec.strNameFriendly;
			this.iconPath = jsonRoomSpec.strIconName;
			this.nMinTileSize = jsonRoomSpec.nMinTileSize;
			this.nMaxTileSize = jsonRoomSpec.nMaxTileSize;
			this.nPriority = jsonRoomSpec.nPriority;
			if (jsonRoomSpec.aReqs != null && jsonRoomSpec.aReqs.Length > 0)
			{
				this._lootReq = new Loot();
				this._lootReq.strName = jsonRoomSpec.strName;
				this._lootReq.strType = "trigger";
				this._lootReq.aCOs = jsonRoomSpec.aReqs;
				DataHandler.dictLoot[this._lootReq.strName] = this._lootReq;
				this._requiredCTs = this._lootReq.GetCTLoot(null, null);
			}
			if (jsonRoomSpec.aForbids != null && jsonRoomSpec.aForbids.Length > 0)
			{
				this._lootForbid = new Loot();
				this._lootForbid.strName = jsonRoomSpec.strName;
				this._lootForbid.strType = "trigger";
				this._lootForbid.aCOs = jsonRoomSpec.aForbids;
				DataHandler.dictLoot[this._lootForbid.strName] = this._lootForbid;
				this._forbiddenCTs = this._lootForbid.GetCTLoot(null, null);
			}
			this.ValueModifier = jsonRoomSpec.fValueModifier;
			this.bAllowVoid = jsonRoomSpec.bAllowVoid;
		}

		public string strName { get; private set; }

		public string strNameFriendly { get; private set; }

		public float ValueModifier { get; private set; }

		public bool IsBlank
		{
			get
			{
				return this.strName == "Blank";
			}
		}

		public static bool IsBlankSpec(string roomSpec)
		{
			return roomSpec == "Blank";
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"ID: ",
				this.strName,
				" Friendly: ",
				this.strNameFriendly,
				" Min: ",
				this.nMinTileSize,
				" Max: ",
				this.nMaxTileSize
			});
		}

		private bool IsWithinMinMaxTileCount(int tileCount)
		{
			return (this.nMinTileSize == -1 || tileCount >= this.nMinTileSize) && (this.nMaxTileSize == -1 || tileCount <= this.nMaxTileSize);
		}

		// Tests whether a live Room satisfies the required/forbidden condtrigs and
		// tile-count/void constraints of this spec.
		public bool Matches(Room room)
		{
			if (room == null || room.aCos == null || room.aTiles == null || this._requiredCTs == null || this._requiredCTs.Count == 0)
			{
				return false;
			}
			if (this.bAllowVoid != room.Void)
			{
				return false;
			}
			if (!this.IsWithinMinMaxTileCount(room.aTiles.Count))
			{
				return false;
			}
			List<CondTrigger> list = new List<CondTrigger>();
			foreach (CondTrigger condTrigger in this._requiredCTs)
			{
				list.Add(condTrigger.Clone());
			}
			foreach (CondOwner condOwner in room.aCos)
			{
				if (!(condOwner == null) && !condOwner.HasCond("IsFloorGrate", false))
				{
					if (this._forbiddenCTs != null)
					{
						foreach (CondTrigger condTrigger2 in this._forbiddenCTs)
						{
							if (condTrigger2.Triggered(condOwner, null, false))
							{
								return false;
							}
						}
					}
					for (int i = list.Count - 1; i >= 0; i--)
					{
						if (list[i] == null)
						{
							list.RemoveAt(i);
						}
						else
						{
							if (list[i].Triggered(condOwner, null, false))
							{
								list[i].fCount -= (float)condOwner.StackCount;
							}
							if (list[i].fCount <= 0f)
							{
								list.RemoveAt(i);
							}
						}
					}
				}
			}
			return list.Count == 0;
		}

		public readonly string iconPath;

		private readonly int nMinTileSize;

		private readonly int nMaxTileSize;

		public readonly int nPriority;

		private readonly bool bAllowVoid;

		private readonly Loot _lootReq;

		private readonly Loot _lootForbid;

		private List<CondTrigger> _requiredCTs;

		private List<CondTrigger> _forbiddenCTs;
	}
}
