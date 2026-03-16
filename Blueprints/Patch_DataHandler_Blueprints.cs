using HarmonyLib;

namespace Ostranauts.Blueprints;

[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.GetCOPlaceholder))]
public static class Patch_DataHandler_BlueprintsGetCOPlaceholder
{
	[HarmonyPostfix]
	private static void Postfix(CondOwner coCursor, CondOwner __result)
	{
		if (!BlueprintRuntime.IsPlacing || coCursor == null || __result == null)
		{
			return;
		}

		Item cursorItem = coCursor.GetComponent<Item>();
		float transformRotation = coCursor.transform.rotation.eulerAngles.z;
		float rotation = cursorItem != null ? cursorItem.fLastRotation : transformRotation;
		BlueprintRuntime.ApplyPlacementRotation(__result, rotation);
		BlueprintRuntime.RegisterPlaceholderRotation(__result, rotation);
		Plugin.LogInfo(
			"Blueprint placeholder rotation copied from cursor: " +
			__result.strName +
			" -> " + rotation.ToString("0.##") +
			" degrees (cursor transform=" + transformRotation.ToString("0.##") +
			", cursor item=" + (cursorItem != null ? cursorItem.fLastRotation.ToString("0.##") : "n/a") + ")."
		);
	}
}
