using HarmonyLib;

namespace Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Room), nameof(Room.AddToRoom))]
internal static class Patch_Room_AddToRoom
{
	public static void Postfix(Room __instance)
	{
		RoomEffectUtils.RefreshRoomBonuses(__instance);
	}
}
