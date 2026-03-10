using HarmonyLib;

namespace Ostranauts.RoomEffects;

// Recompute both ship-wide and per-room bonuses whenever room classification updates.
[HarmonyPatch(typeof(Room), nameof(Room.CreateRoomSpecs))]
internal static class Patch_Room_CreateRoomSpecs
{
	public static void Postfix(Room __instance)
	{
		RoomEffectUtils.RefreshShipWideBonuses(__instance);
		RoomEffectUtils.RefreshRoomBonuses(__instance);
	}
}
