using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Room), nameof(Room.RemoveFromRoom))]
internal static class Patch_Room_RemoveFromRoom
{
	public static void Postfix(Room __instance)
	{
		RoomEffectUtils.RefreshRoomBonuses(__instance);
	}
}
