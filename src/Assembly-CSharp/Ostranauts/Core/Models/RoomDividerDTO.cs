using System;
using UnityEngine;

namespace Ostranauts.Core.Models
{
	public class RoomDividerDTO
	{
		public RoomDividerDTO(Vector2 pos, Room a, Room b)
		{
			this.Position = pos;
			this.RoomA = a;
			this.RoomB = b;
		}

		public bool HasNullValues()
		{
			return this.RoomA == null || this.RoomA.CO == null || this.RoomB == null || this.RoomB.CO == null;
		}

		public Vector2 Position;

		public Room RoomA;

		public Room RoomB;
	}
}
