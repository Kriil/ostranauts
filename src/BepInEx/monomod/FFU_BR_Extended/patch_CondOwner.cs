using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FFU_Beyond_Reach;
using Ostranauts.Core.Models;
using UnityEngine;

// Runtime CondOwner extensions for FFU_BR_Extended.
// This module adds inventory-wide slot effects and stable slot ordering, which
// the README calls out as new modding parameters and strict sort helpers.
public class patch_CondOwner : CondOwner
{
	// Returns slots in the strict order expected by FFU_BR inventory patches.
	// This supports the extended `nSlotOrder` behavior so nested slot trees render
	// in a stable, moddable order instead of relying only on vanilla depth.
	public List<Slot> GetSortedSlots()
	{
		bool flag = base.compSlots == null;
		List<Slot> result;
		if (flag)
		{
			List<Slot> list;
			if ((list = CondOwner._emptySlotsResult) == null)
			{
				list = (CondOwner._emptySlotsResult = new List<Slot>());
			}
			result = list;
		}
		else
		{
			List<Slot> list2 = new List<Slot>();
			Slots compSlots = base.compSlots;
			List<Slot> refSlots;
			if (compSlots == null)
			{
				refSlots = null;
			}
			else
			{
				TrackingCollection<Slot> aSlots = compSlots.aSlots;
				refSlots = ((aSlots != null) ? aSlots.ToList<Slot>() : null);
			}
			patch_CondOwner.<GetSortedSlots>g__GetSlotsRecursive|0_1(ref list2, refSlots, FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime, true, 0);
			result = list2;
		}
		return result;
	}
	public extern void orig_SetData(JsonCondOwner jid, bool bLoot, JsonCondOwnerSave jCOSIn);

	// Runs the vanilla CondOwner setup first, then applies FFU_BR-only inventory
	// effect parsing so the extra JSON fields become live runtime behavior.
	public void SetData(patch_JsonCondOwner jid, bool bLoot, JsonCondOwnerSave jCOSIn)
	{
		this.orig_SetData(jid, bLoot, jCOSIn);
		this.ParseInvEffects(jid);
	}

	// Reads the extended `strInvSlotEffect` field and caches the referenced slot
	// effect on the live CondOwner so inventory contents can inherit it later.
	public void ParseInvEffects(patch_JsonCondOwner jid)
	{
		bool flag = jid.strInvSlotEffect != null;
		if (flag)
		{
			JsonSlotEffects slotEffect = DataHandler.GetSlotEffect(jid.strInvSlotEffect);
			bool flag2 = slotEffect != null;
			if (flag2)
			{
				bool flag3 = Container.GetSpace(this) < 1;
				if (flag3)
				{
					Debug.LogWarning("Can't assign 'invSlotEffect' for [" + this.strName + "] without inventory grid.");
				}
				else
				{
					this.jsInvSlotEffect = slotEffect;
				}
			}
		}
	}
	[CompilerGenerated]
	internal static int <GetSortedSlots>g__SortByDepth|0_0(Slot s1, Slot s2)
	{
		bool flag = !(s1 is patch_Slot) || !(s2 is patch_Slot);
		int result;
		if (flag)
		{
			result = 0;
		}
		else
		{
			result = (s1 as patch_Slot).nSlotOrder.CompareTo((s2 as patch_Slot).nSlotOrder);
		}
		return result;
	}
	// Walks nested slot trees depth-first so strict inventory sorting can honor
	// `nSlotOrder` across child containers instead of only the first slot layer.
	[CompilerGenerated]
	internal static void <GetSortedSlots>g__GetSlotsRecursive|0_1(ref List<Slot> srtSlots, List<Slot> refSlots, bool dLog, bool dSort = true, int sDepth = 0)
	{
		bool flag = refSlots != null && refSlots.Count > 0;
		if (flag)
		{
			refSlots.Sort(new Comparison<Slot>(patch_CondOwner.<GetSortedSlots>g__SortByDepth|0_0));
			foreach (Slot slot in refSlots)
			{
				if (dLog)
				{
					Debug.Log("#Info# Sorted Slot " + string.Empty.PadLeft(sDepth, '=') + "> " + slot.strName);
				}
				srtSlots.Add(slot);
				foreach (CondOwner condOwner in slot.aCOs)
				{
					List<Slot> list;
					if (condOwner == null)
					{
						list = null;
					}
					else
					{
						Slots compSlots = condOwner.compSlots;
						if (compSlots == null)
						{
							list = null;
						}
						else
						{
							TrackingCollection<Slot> aSlots = compSlots.aSlots;
							list = ((aSlots != null) ? aSlots.ToList<Slot>() : null);
						}
					}
					List<Slot> refSlots2 = list;
					patch_CondOwner.<GetSortedSlots>g__GetSlotsRecursive|0_1(ref srtSlots, refSlots2, dLog, dSort, sDepth + 1);
				}
			}
		}
	}
	public JsonSlotEffects jsInvSlotEffect;
}
