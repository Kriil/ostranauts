using HarmonyLib;
using UnityEngine;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Room), nameof(Room.CreateRoomSpecs))]
internal static class Patch_Room_CreateRoomSpecs
{
	public static void Postfix(Room __instance)
	{
		Ship ship = __instance?.CO?.ship;
		CondOwner shipCo = ship?.ShipCO;
		if (ship == null || shipCo == null || ship.aRooms == null)
		{
			return;
		}

		bool hasEngineering = false;

		foreach (Room room in ship.aRooms)
		{
			if (room == null || room.Void)
			{
				continue;
			}

			var spec = room.GetRoomSpec();
			if (spec != null && spec.strName == "Engineering")
			{
				hasEngineering = true;
				break;
			}
		}

		shipCo.SetCondAmount("StatShipEngineeringWorkBonus", hasEngineering ? Plugin.EngineeringWorkBonus.Value: 0.0, 0.0);
		return;
	}
}