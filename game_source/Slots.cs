using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using UnityEngine;

public class Slots : MonoBehaviour
{
	private void Awake()
	{
		this.aSlotNames = new List<string>();
		this.coUs = base.GetComponent<CondOwner>();
		this.aSlots = new TrackingCollection<Slot>(delegate(bool Any)
		{
			if (this.coUs != null)
			{
				this.coUs.HasSubCOs = Any;
			}
		});
		if (Slots.OnSlotContentUpdated == null)
		{
			Slots.OnSlotContentUpdated = new SlotUpdatedEvent();
		}
	}

	public void Destroy()
	{
		if (this.aSlots != null)
		{
			foreach (Slot slot in this.aSlots)
			{
				slot.Destroy();
			}
			this.aSlots.Clear();
			this.aSlots = null;
		}
		this.aSlotNames = null;
		this.coUs = null;
	}

	public void AddSlot(JsonSlot jslot)
	{
		if (jslot == null || jslot.strName == null)
		{
			return;
		}
		this.aSlotNames.Add(jslot.strName);
		this.aSlots.Add(new Slot(jslot));
		this.aSlots[this.aSlots.Count - 1].compSlots = this;
	}

	public void RemoveSlot(string strSlot)
	{
		if (strSlot == null)
		{
			return;
		}
		foreach (Slot slot in this.aSlots)
		{
			if (slot.strName == strSlot)
			{
				CondOwner condOwner = slot.GetOutermostCO();
				while (condOwner != null)
				{
					if (condOwner.objCOParent != null)
					{
						Ship ship = condOwner.ship;
						condOwner = condOwner.objCOParent.DropCO(condOwner, true, null, 0f, 0f, true, null);
						if (condOwner != null)
						{
							condOwner.RemoveFromCurrentHome(false);
							if (ship != null)
							{
								ship.AddCO(condOwner, true);
							}
							else
							{
								condOwner.Destroy();
							}
						}
					}
					condOwner = slot.GetOutermostCO();
				}
				this.aSlots.Remove(slot);
				slot.compSlots = null;
				break;
			}
		}
		this.aSlotNames.Remove(strSlot);
	}

	public bool SlotItem(string strSlot, CondOwner co, bool bAuto = true)
	{
		if (co == null || strSlot == null)
		{
			return false;
		}
		JsonSlotEffects jsonSlotEffects = null;
		if (!co.mapSlotEffects.TryGetValue(strSlot, out jsonSlotEffects))
		{
			return false;
		}
		if (jsonSlotEffects.strSlotPrimary != strSlot)
		{
			return this.SlotItem(jsonSlotEffects.strSlotPrimary, co, bAuto);
		}
		Slot slot = this.GetSlot(strSlot);
		if (slot == null)
		{
			return false;
		}
		if (slot.compSlots != this)
		{
			return slot.compSlots.SlotItem(strSlot, co, bAuto);
		}
		if (Array.IndexOf<CondOwner>(slot.aCOs, co) >= 0)
		{
			Slots.OnSlotContentUpdated.Invoke(this.coUs, null);
			return true;
		}
		if (slot.OpenSpaces(co, bAuto) < co.StackCount)
		{
			if (!(slot.strName == "drag"))
			{
				return false;
			}
			if (!slot.CanFit(co, bAuto, false))
			{
				CondOwner objCO = this.UnSlotItem("drag", null, false);
				if (this.coUs.DropCO(objCO, false, null, 0f, 0f, true, null) != null)
				{
					this.coUs.LogMessage(this.coUs.strNameFriendly + DataHandler.GetString("ERROR_DRAG_SWAP", false), "Bad", this.coUs.strName);
					return false;
				}
			}
		}
		if (jsonSlotEffects.aSlotsSecondary != null)
		{
			foreach (string strSlot2 in jsonSlotEffects.aSlotsSecondary)
			{
				Slot slot2 = this.GetSlot(strSlot2);
				if (slot2 == null)
				{
					return false;
				}
				if (slot2.OpenSpaces(co, bAuto) < co.StackCount)
				{
					return false;
				}
			}
		}
		co.ValidateParent();
		int num = 0;
		if (num >= slot.aCOs.Length)
		{
			return false;
		}
		CondOwner condOwner = slot.aCOs[num];
		if (condOwner == null)
		{
			slot.aCOs[num] = co;
			this.ApplySlotEffects(slot, co, jsonSlotEffects, false);
			if (this.coUs == this)
			{
				Debug.Log("ERROR: Assigning self as own parent.");
			}
			co.objCOParent = this.coUs;
			co.slotNow = slot;
			co.AddCondAmount("IsSlotted", 1.0, 0.0, 0f);
			if (strSlot == "drag")
			{
				Draggable draggable = co.gameObject.GetComponent<Draggable>();
				if (draggable == null)
				{
					draggable = co.gameObject.AddComponent<Draggable>();
				}
				draggable.Init(this.coUs);
			}
			else
			{
				co.tf.SetParent(co.objCOParent.tf);
				co.tf.localPosition = new Vector3(0f, 0f, Container.fZSubOffset);
				co.Visible = false;
			}
			if (co.objCOParent.ship != null)
			{
				co.objCOParent.ship.AddCO(co, false);
			}
			co.strSourceCO = null;
			co.strSourceInteract = null;
			this.coUs.AddMass(co.GetTotalMass(), false);
			CondOwner co2 = this.coUs;
			if (this.coUs.objCOParent != null)
			{
				co2 = this.coUs.RootParent(null);
			}
			if (CrewSim.inventoryGUI.IsCOShown(co2))
			{
				CrewSim.inventoryGUI.PaperDollImageCG.GetComponent<GUIPaperDollManager>().CreateNewPaperDollImage(co, strSlot);
			}
			if (jsonSlotEffects.aSlotsSecondary != null)
			{
				foreach (string text in jsonSlotEffects.aSlotsSecondary)
				{
					Slot slot3 = this.GetSlot(text);
					if (slot3 == null)
					{
						Debug.LogError(string.Concat(new object[]
						{
							"ERROR: Slot ",
							text,
							" disappeared while slotting ",
							co
						}));
					}
					else
					{
						for (int k = 0; k < slot3.aCOs.Length; k++)
						{
							if (!(slot3.aCOs[k] != null))
							{
								slot3.aCOs[k] = co;
								break;
							}
						}
					}
				}
			}
			CondOwner objCOParent = this.coUs;
			while (objCOParent != null)
			{
				if (objCOParent.HasCond("IsHuman") || objCOParent.HasCond("IsRobot"))
				{
					co.AddCondAmount("IsCarried", 1.0, 0.0, 0f);
					co.VisitCOs(new CondOwnerVisitorAddCond
					{
						strCond = "IsCarried",
						fAmount = 1.0
					}, true);
					break;
				}
				objCOParent = objCOParent.objCOParent;
			}
			co.ValidateParent();
			Slots.OnSlotContentUpdated.Invoke(this.coUs, co);
			return true;
		}
		if (!condOwner.bSlotLocked && condOwner.CanStackOnItem(co) >= co.StackCount)
		{
			this.UnSlotItem(condOwner, false);
			condOwner.StackCO(co);
			this.SlotItem(slot.strName, co, true);
			Slots.OnSlotContentUpdated.Invoke(this.coUs, co);
			return true;
		}
		return false;
	}

	public CondOwner UnSlotItem(CondOwner co, bool bForce = false)
	{
		if (co == null)
		{
			return null;
		}
		co.ValidateParent();
		foreach (JsonSlotEffects jsonSlotEffects in co.mapSlotEffects.Values)
		{
			List<CondOwner> cos = this.GetCOs(jsonSlotEffects.strSlotPrimary, true, null);
			if (cos.IndexOf(co) >= 0)
			{
				return this.UnSlotItem(jsonSlotEffects.strSlotPrimary, co, bForce);
			}
		}
		return null;
	}

	public CondOwner UnSlotItem(string strSlot, CondOwner co = null, bool bForce = false)
	{
		if (strSlot == null)
		{
			return null;
		}
		if (co != null)
		{
			co.ValidateParent();
		}
		Slot slot = this.GetSlot(strSlot);
		if (slot == null)
		{
			return null;
		}
		if (slot.compSlots != this)
		{
			return slot.compSlots.UnSlotItem(strSlot, co, bForce);
		}
		if (slot.aCOs == null)
		{
			return null;
		}
		CondOwner condOwner = null;
		for (int i = 0; i < slot.aCOs.Length; i++)
		{
			if (co == null)
			{
				if (slot.aCOs[i] != null && (bForce || !slot.aCOs[i].bSlotLocked))
				{
					condOwner = slot.aCOs[i];
					break;
				}
			}
			else if (co == slot.aCOs[i] && (bForce || !co.bSlotLocked))
			{
				condOwner = co;
				break;
			}
		}
		if (!(condOwner != null))
		{
			return null;
		}
		JsonSlotEffects jsonSlotEffects = null;
		if (!condOwner.mapSlotEffects.TryGetValue(strSlot, out jsonSlotEffects))
		{
			return null;
		}
		if (jsonSlotEffects.strSlotPrimary != strSlot)
		{
			return this.UnSlotItem(jsonSlotEffects.strSlotPrimary, co, bForce);
		}
		int num = Array.IndexOf<CondOwner>(slot.aCOs, condOwner);
		if (num >= 0)
		{
			slot.aCOs[num] = null;
		}
		else
		{
			Debug.LogError(string.Concat(new string[]
			{
				"ERROR: Unslotting ",
				condOwner.strName,
				" from ",
				slot.strName,
				", but it disappeared before we could remove it!"
			}));
		}
		if (strSlot == "drag")
		{
			Draggable component = condOwner.GetComponent<Draggable>();
			if (component != null)
			{
				component.Exit(this.coUs);
			}
		}
		else
		{
			condOwner.tf.position = this.coUs.tf.position;
		}
		condOwner.slotNow = null;
		if (condOwner.ship != null)
		{
			condOwner.ship.RemoveCO(condOwner, false);
		}
		this.ApplySlotEffects(slot, condOwner, jsonSlotEffects, true);
		condOwner.ZeroCondAmount("IsSlotted");
		condOwner.tf.SetParent(null);
		Item item = condOwner.Item;
		if (item != null)
		{
			item.ResetTransforms(condOwner.tf.position.x, condOwner.tf.position.y);
		}
		condOwner.objCOParent = null;
		this.coUs.AddMass(-condOwner.GetTotalMass(), false);
		CondOwner condOwner2 = this.coUs;
		if (condOwner2.objCOParent != null)
		{
			condOwner2 = condOwner2.RootParent(null);
		}
		if (CrewSim.inventoryGUI.IsCOShown(condOwner2))
		{
			CrewSim.inventoryGUI.PaperDollManager.DestroySlots(condOwner);
		}
		if (jsonSlotEffects.aSlotsSecondary != null)
		{
			foreach (string text in jsonSlotEffects.aSlotsSecondary)
			{
				Slot slot2 = this.GetSlot(text);
				if (slot2 == null)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"ERROR: Slot ",
						text,
						" disappeared while unslotting ",
						co
					}));
				}
				else
				{
					int num2 = 0;
					if (num2 < slot2.aCOs.Length)
					{
						if (slot2.aCOs[num2] == condOwner)
						{
							slot2.aCOs[num2] = null;
						}
					}
				}
			}
		}
		condOwner.ZeroCondAmount("IsCarried");
		condOwner.VisitCOs(new CondOwnerVisitorZeroCond
		{
			strCond = "IsCarried"
		}, true);
		if (co != null)
		{
			co.ValidateParent();
		}
		condOwner.ValidateParent();
		Slots.OnSlotContentUpdated.Invoke(this.coUs, null);
		return condOwner;
	}

	public static void ApplyIAEffects(CondOwner coSlot, CondOwner co, JsonSlotEffects jse, bool bRemove, bool bParent)
	{
		if (co == null || coSlot == null || jse == null)
		{
			return;
		}
		if (co.HasCond("IsHuman") || co.HasCond("IsRobot"))
		{
			return;
		}
		string strName = jse.strIASlot;
		if (bRemove)
		{
			if (!bParent && co.compSlots != null)
			{
				foreach (Slot slot in co.compSlots.GetSlotsChildFirst(true))
				{
					foreach (CondOwner condOwner in slot.aCOs)
					{
						if (!(condOwner == null))
						{
							JsonSlotEffects jse2 = null;
							if (condOwner.mapSlotEffects.TryGetValue(slot.strName, out jse2))
							{
								Slots.ApplyIAEffects(coSlot, condOwner, jse2, bRemove, true);
							}
						}
					}
				}
			}
			if (bParent)
			{
				strName = jse.strIAUnslotParents;
			}
			else
			{
				strName = jse.strIAUnslot;
			}
		}
		else if (bParent)
		{
			strName = jse.strIASlotParents;
		}
		Interaction interaction = DataHandler.GetInteraction(strName, null, false);
		if (interaction != null)
		{
			interaction.objUs = co;
			interaction.objThem = coSlot;
			if (interaction.Triggered(interaction.objUs, interaction.objThem, false, false, false, true, null))
			{
				interaction.ApplyChain(null);
			}
		}
		if (coSlot.objCOParent != null)
		{
			Slots.ApplyIAEffects(coSlot.objCOParent, co, jse, bRemove, true);
		}
		if (!bRemove && !bParent && co.compSlots != null)
		{
			List<Slot> slotsChildFirst = co.compSlots.GetSlotsChildFirst(true);
			slotsChildFirst.Reverse();
			foreach (Slot slot2 in slotsChildFirst)
			{
				foreach (CondOwner condOwner2 in slot2.aCOs)
				{
					if (!(condOwner2 == null))
					{
						JsonSlotEffects jse3 = null;
						if (condOwner2.mapSlotEffects.TryGetValue(slot2.strName, out jse3))
						{
							Slots.ApplyIAEffects(coSlot, condOwner2, jse3, bRemove, true);
						}
					}
				}
			}
		}
	}

	private void ApplySlotEffects(Slot slot, CondOwner co, JsonSlotEffects jse, bool bRemove = false)
	{
		if (co == null || slot == null || jse == null)
		{
			return;
		}
		co.ValidateParent();
		if (co.mapSlotEffects.TryGetValue(slot.strName, out jse))
		{
			Slots.ApplyIAEffects(slot.compSlots.coUs, co, jse, bRemove, false);
			Crew crew = this.coUs.Crew;
			if (crew == null)
			{
				crew = base.GetComponentInParent<Crew>();
			}
			Slots.ApplyMeshEffects(slot, co, crew, bRemove);
			if (jse.aSlotsAdded != null)
			{
				foreach (string text in jse.aSlotsAdded)
				{
					if (bRemove)
					{
						this.RemoveSlot(text);
					}
					else
					{
						JsonSlot slot2 = DataHandler.GetSlot(text);
						if (slot2 != null)
						{
							this.AddSlot(slot2);
						}
					}
				}
			}
		}
	}

	public static void ApplyMeshEffects(Slot slot, CondOwner co, Crew crew, bool bRemove = false)
	{
		if (co == null || slot == null)
		{
			return;
		}
		JsonSlotEffects jsonSlotEffects = null;
		if (co.mapSlotEffects.TryGetValue(slot.strName, out jsonSlotEffects) && crew != null && jsonSlotEffects.mapMeshTextures != null)
		{
			Dictionary<string, string> dictionary = DataHandler.ConvertStringArrayToDict(jsonSlotEffects.mapMeshTextures, null);
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				string[] array = keyValuePair.Value.Split(new char[]
				{
					':'
				});
				if (array.Length > 0)
				{
					string strValue = array[0];
					string strValueNorm = array[0] + "n";
					if (array.Length > 1)
					{
						strValueNorm = array[1];
					}
					crew.OuterParts(keyValuePair.Key, slot, strValue, strValueNorm, bRemove);
				}
			}
		}
		if (co.compSlots != null && !co.HasCond("DisallowPaperDoll"))
		{
			foreach (Slot slot2 in co.compSlots.GetSlotsDepthFirst(false))
			{
				if (slot2 != null && slot2.aCOs != null)
				{
					foreach (CondOwner condOwner in slot2.aCOs)
					{
						if (!(condOwner == null))
						{
							Slots.ApplyMeshEffects(slot2, condOwner, crew, bRemove);
						}
					}
				}
			}
		}
	}

	public Slot GetSlot(string strSlot)
	{
		if (strSlot == null)
		{
			return null;
		}
		int num = this.aSlotNames.IndexOf(strSlot);
		if (num < 0)
		{
			if (num < 0)
			{
				foreach (Slot slot in this.aSlots)
				{
					for (int i = 0; i < slot.aCOs.Length; i++)
					{
						CondOwner condOwner = slot.aCOs[i];
						if (!(condOwner == null))
						{
							if (!(condOwner.compSlots == null))
							{
								Slot slot2 = condOwner.compSlots.GetSlot(strSlot);
								if (slot2 != null)
								{
									return slot2;
								}
							}
						}
					}
				}
			}
			return null;
		}
		if (this.aSlots[num] == null)
		{
			return null;
		}
		return this.aSlots[num];
	}

	public List<Slot> GetSlotsHeldFirst(bool bDeep)
	{
		List<Slot> list = new List<Slot>(this.aSlots);
		if (bDeep)
		{
			foreach (Slot slot in this.aSlots)
			{
				list.AddRange(slot.GetSlots(bDeep, false));
			}
		}
		List<Slot> list2 = list;
		if (Slots.<>f__mg$cache0 == null)
		{
			Slots.<>f__mg$cache0 = new Comparison<Slot>(Slots.SortSlotHeldFirst);
		}
		list2.Sort(Slots.<>f__mg$cache0);
		return list;
	}

	public List<Slot> GetSlotsDepthFirst(bool bDeep)
	{
		List<Slot> list = new List<Slot>(this.aSlots);
		if (bDeep)
		{
			foreach (Slot slot in this.aSlots)
			{
				list.AddRange(slot.GetSlots(bDeep, false));
			}
		}
		List<Slot> list2 = list;
		if (Slots.<>f__mg$cache1 == null)
		{
			Slots.<>f__mg$cache1 = new Comparison<Slot>(Slots.SortBySlotDepth);
		}
		list2.Sort(Slots.<>f__mg$cache1);
		return list;
	}

	public List<Slot> GetSlotsChildFirst(bool bDeep)
	{
		List<Slot> list = new List<Slot>(this.aSlots);
		if (bDeep)
		{
			foreach (Slot slot in this.aSlots)
			{
				list.InsertRange(0, slot.GetSlots(bDeep, true));
			}
		}
		List<Slot> list2 = list;
		if (Slots.<>f__mg$cache2 == null)
		{
			Slots.<>f__mg$cache2 = new Comparison<Slot>(Slots.SortBySlotDepthInverse);
		}
		list2.Sort(Slots.<>f__mg$cache2);
		return list;
	}

	private static int SortBySlotDepth(Slot s1, Slot s2)
	{
		if (s1 == null || s2 == null)
		{
			return 0;
		}
		return s1.nDepth.CompareTo(s2.nDepth);
	}

	private static int SortBySlotDepthInverse(Slot s1, Slot s2)
	{
		if (s1 == null || s2 == null)
		{
			return 0;
		}
		return -s1.nDepth.CompareTo(s2.nDepth);
	}

	private static int SortSlotHeldFirst(Slot s1, Slot s2)
	{
		if (s1 == null || s2 == null)
		{
			return 0;
		}
		if (s1.bHoldSlot)
		{
			if (s2.bHoldSlot)
			{
				return 0;
			}
			return -1;
		}
		else
		{
			if (s2.bHoldSlot)
			{
				return 1;
			}
			return Slots.SortBySlotDepth(s1, s2);
		}
	}

	public Slot GetSlotForCO(CondOwner co)
	{
		if (co == null)
		{
			return null;
		}
		foreach (string strSlot in co.mapSlotEffects.Keys)
		{
			List<CondOwner> cos = this.GetCOs(strSlot, true, null);
			if (cos.IndexOf(co) >= 0)
			{
				return this.GetSlot(strSlot);
			}
		}
		return null;
	}

	public CondOwner GetOutermostCO(string strSlotName)
	{
		Slot slot = this.GetSlot(strSlotName);
		if (slot == null)
		{
			return null;
		}
		return slot.GetOutermostCO();
	}

	public List<CondOwner> GetCOs(string strSlot = null, bool bAllowLocked = false, CondTrigger objCondTrig = null)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (strSlot != null)
		{
			Slot slot = this.GetSlot(strSlot);
			if (slot != null)
			{
				foreach (CondOwner condOwner in slot.aCOs)
				{
					if (condOwner != null)
					{
						if (objCondTrig == null)
						{
							list.Add(condOwner);
						}
						else if (objCondTrig.Triggered(condOwner, null, false))
						{
							list.Add(condOwner);
						}
						CondOwner.NullSafeAddRange(ref list, condOwner.GetCOs(bAllowLocked, objCondTrig));
					}
				}
			}
		}
		else
		{
			foreach (Slot slot2 in this.aSlots)
			{
				foreach (CondOwner condOwner2 in slot2.aCOs)
				{
					if (condOwner2 != null)
					{
						if (objCondTrig == null)
						{
							list.Add(condOwner2);
						}
						else if (objCondTrig.Triggered(condOwner2, null, false))
						{
							list.Add(condOwner2);
						}
						CondOwner.NullSafeAddRange(ref list, condOwner2.GetCOs(bAllowLocked, objCondTrig));
					}
				}
			}
		}
		return list;
	}

	public static SlotUpdatedEvent OnSlotContentUpdated;

	private List<string> aSlotNames;

	private TrackingCollection<Slot> aSlots;

	private CondOwner coUs;

	[CompilerGenerated]
	private static Comparison<Slot> <>f__mg$cache0;

	[CompilerGenerated]
	private static Comparison<Slot> <>f__mg$cache1;

	[CompilerGenerated]
	private static Comparison<Slot> <>f__mg$cache2;

	public enum SortOrder
	{
		BY_DEPTH,
		HELD_FIRST,
		CHILD_FIRST
	}
}
