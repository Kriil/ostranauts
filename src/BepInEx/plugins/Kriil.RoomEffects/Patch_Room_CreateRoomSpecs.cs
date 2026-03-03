using HarmonyLib;
using UnityEngine;

namespace Kriil.Ostranauts.RoomEffects;

// Sets various room bonuses when room specs are created
[HarmonyPatch(typeof(Room), nameof(Room.CreateRoomSpecs))]
internal static class Patch_Room_CreateRoomSpecs
{
	public static void Postfix(Room __instance)
	{
		RoomEffectUtils.RefreshShipWideBonuses(__instance);
		RoomEffectUtils.RefreshRoomBonuses(__instance);
	}
}
