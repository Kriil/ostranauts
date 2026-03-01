using System;
using System.Collections.Generic;
using UnityEngine;

// Applies a COOverlay to an existing CondOwner.
// COOverlay is the display/variant layer for a CondOwner: damaged, loose,
// recolored, or otherwise altered presentation that can swap UI text, art,
// interactions, and slot behavior without replacing the base CondOwner.
public class COOverlay : MonoBehaviour
{
	// Runtime overlay application entrypoint.
	// Data linkage: `strCOO` should be a cooverlay id from StreamingAssets/data/cooverlays.
	// The overlay then patches the live CondOwner with loot, display text, images,
	// GUI prop maps, interaction replacements, and slot-effect overrides.
	public void Init(string strCOO)
	{
		CondOwner component = base.GetComponent<CondOwner>();
		if (component == null)
		{
			return;
		}
		JsonCOOverlay cooverlay = DataHandler.GetCOOverlay(strCOO);
		if (cooverlay == null)
		{
			return;
		}
		this.strName = strCOO;
		Loot loot = DataHandler.GetLoot(cooverlay.strCondLoot);
		bool bLogConds = component.bLogConds;
		component.bLogConds = false;
		loot.ApplyCondLoot(component, 1f, null, 0f);
		component.bLogConds = bLogConds;
		component.strName = cooverlay.strName;
		if (!string.IsNullOrEmpty(cooverlay.strNameFriendly))
		{
			component.strNameFriendly = cooverlay.strNameFriendly;
		}
		if (!string.IsNullOrEmpty(cooverlay.strNameShort))
		{
			component.strNameShort = cooverlay.strNameShort;
		}
		if (!string.IsNullOrEmpty(cooverlay.strNameShort))
		{
			component.strNameShortLCase = cooverlay.strNameShort.ToLower();
		}
		if (!string.IsNullOrEmpty(cooverlay.strDesc))
		{
			component.strDesc = cooverlay.strDesc;
		}
		if (!string.IsNullOrEmpty(cooverlay.strPortraitImg))
		{
			component.strPortraitImg = cooverlay.strPortraitImg;
		}
		component.gameObject.name = component.ToString();
		component.strCODef = cooverlay.strName;
		Dictionary<string, string> dictionary = DataHandler.ConvertStringArrayToDict(cooverlay.mapGUIPropMaps, null);
		foreach (KeyValuePair<string, string> keyValuePair in dictionary)
		{
			Dictionary<string, string> guipropMap = DataHandler.GetGUIPropMap(keyValuePair.Value);
			if (guipropMap != null)
			{
				component.mapGUIPropMaps[keyValuePair.Key] = guipropMap;
			}
		}
		foreach (KeyValuePair<string, string> keyValuePair2 in cooverlay.InteractionReplacements)
		{
			int num = component.aInteractions.IndexOf(keyValuePair2.Key);
			if (num >= 0)
			{
				component.aInteractions[num] = keyValuePair2.Value;
			}
		}
		component.CheckInteractionFlag();
		component.mapSlotEffects = new Dictionary<string, JsonSlotEffects>();
		if (cooverlay.mapSlotEffects != null)
		{
			Dictionary<string, string> dictionary2 = DataHandler.ConvertStringArrayToDict(cooverlay.mapSlotEffects, null);
			foreach (KeyValuePair<string, string> keyValuePair3 in dictionary2)
			{
				JsonSlotEffects slotEffect = DataHandler.GetSlotEffect(keyValuePair3.Value);
				if (slotEffect != null)
				{
					component.mapSlotEffects[keyValuePair3.Key] = slotEffect;
					slotEffect.strSlotPrimary = keyValuePair3.Key;
					if (slotEffect.aSlotsSecondary != null)
					{
						foreach (string key in slotEffect.aSlotsSecondary)
						{
							component.mapSlotEffects[key] = slotEffect;
						}
					}
				}
			}
		}
		if (cooverlay.mapAltItemDefs != null)
		{
			component.mapAltItemDefs = DataHandler.ConvertStringArrayToDict(cooverlay.mapAltItemDefs, null);
		}
		Item component2 = base.GetComponent<Item>();
		component2.SetAlt(cooverlay.strImg, cooverlay.strImgNorm, cooverlay.strImgDamaged, cooverlay.strDmgColor, cooverlay.objAnimation);
		if (cooverlay.aDestSwaps != null)
		{
			Destructable component3 = component.GetComponent<Destructable>();
			if (component3 != null)
			{
				for (int j = 0; j < cooverlay.aDestSwaps.Length - 1; j += 2)
				{
					if (cooverlay.aDestSwaps[j] == null || cooverlay.aDestSwaps[j + 1] == null)
					{
						break;
					}
					component3.SwapDmgLoot(cooverlay.aDestSwaps[j], cooverlay.aDestSwaps[j + 1]);
				}
			}
		}
	}

	// Defers job-action lookup back to the base CondOwner definition referenced by the overlay.
	public List<string> GetJobActions(string strJobType)
	{
		List<string> result = new List<string>();
		JsonCondOwner jsonCondOwner = null;
		JsonCOOverlay cooverlay = DataHandler.GetCOOverlay(this.strName);
		if (cooverlay != null && DataHandler.dictCOs.TryGetValue(cooverlay.strCOBase, out jsonCondOwner))
		{
			return jsonCondOwner.GetJobActions(strJobType.ToLower());
		}
		return result;
	}

	// Asks the overlay definition for a mode-switch target.
	// Likely used when an item changes state and should swap to a different CO/overlay variant.
	public string ModeSwitch(string strTargetCO)
	{
		if (strTargetCO == null)
		{
			return null;
		}
		JsonCOOverlay cooverlay = DataHandler.GetCOOverlay(this.strName);
		if (cooverlay == null)
		{
			return null;
		}
		return cooverlay.GetModeSwitch(strTargetCO);
	}

	public string strName;
}
