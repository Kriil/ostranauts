using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Heater), "Heat")]
internal static class Patch_Heater_Heat
{
	public static void Prefix(Heater __instance, ref double fTimePassed)
	{
		if (__instance == null)
		{
			return;
		}

		CondOwner co = __instance.GetComponent<CondOwner>();
		if (co?.currentRoom?.CO == null)
		{
			return;
		}

		double bonus = 0.0;

		if (co.HasCond("IsHeater"))
		{
			bonus = co.currentRoom.CO.GetCondAmount("StatRoomHeatSpeedBonus");
		}
		else if (co.HasCond("IsCooler"))
		{
			bonus = co.currentRoom.CO.GetCondAmount("StatRoomCoolSpeedBonus");
		}

		if (bonus == 0.0)
		{
			return;
		}

		// Interpret room stat as additive multiplier:
		// 0.25 => 1.25x
		fTimePassed *= 1.0 + bonus;
	}
}