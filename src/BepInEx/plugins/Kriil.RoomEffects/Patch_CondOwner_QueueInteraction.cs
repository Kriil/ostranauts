using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.QueueInteraction))]
internal static class Patch_CondOwner_QueueInteraction
{
	public static void Postfix(CondOwner __instance, Interaction objInteraction, bool __result)
	{
		if (!__result || objInteraction == null)
		{
			return;
		}

		float oldDuration = (float)objInteraction.fDuration;
		float newDuration = RoomEffectUtils.ModifyQueuedInteractionDuration(objInteraction, oldDuration);
		if (newDuration >= oldDuration)
		{
			return;
		}

		objInteraction.fDuration = newDuration;
		objInteraction.fDurationOrig = newDuration;

		if (__instance.aQueue.Count > 0 && __instance.aQueue[0] == objInteraction)
		{
			__instance.SetTicker(objInteraction.strName, newDuration);
		}
	}
}
