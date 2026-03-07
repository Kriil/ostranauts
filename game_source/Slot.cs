using System;
using System.Collections.Generic;
using UnityEngine;

public class Slot
{
	public Slot(JsonSlot jslot)
	{
		this.strName = jslot.strName;
		this.strNameFriendly = jslot.strNameFriendly;
		this.strHitboxImage = jslot.strHitboxImage;
		this.strIconImage = jslot.strIconImage;
		this.aCOs = new CondOwner[jslot.nItems];
		this.nDepth = jslot.nDepth;
		this.bHoldSlot = jslot.bHoldSlot;
		this.bAlignSlot = jslot.bAlignSlot;
		this.bCarried = jslot.bCarried;
		this.bAllowStacks = jslot.bAllowStacks;
		this.bHide = jslot.bHide;
		if (!string.IsNullOrEmpty(jslot.strCTAutoSlot))
		{
			this.ctAutoSlot = DataHandler.GetCondTrigger(jslot.strCTAutoSlot);
			if (this.ctAutoSlot.IsBlank())
			{
				this.ctAutoSlot = null;
			}
		}
		this.ptAlign = new Vector2(jslot.fAlignX, jslot.fAlignY);
	}

	public CondOwner AddCO(CondOwner coAdd, bool bEquip, bool bOverflow, bool bIgnoreLocks)
	{
		if (coAdd == null)
		{
			return null;
		}
		CondOwner condOwner = coAdd;
		foreach (CondOwner condOwner2 in this.aCOs)
		{
			if (!(condOwner2 == null) && !(condOwner2.Crew != null))
			{
				condOwner = condOwner2.AddCO(condOwner, bEquip, bOverflow, bIgnoreLocks);
				if (condOwner == null)
				{
					break;
				}
			}
		}
		return condOwner;
	}

	public int OpenSpaces(CondOwner coIn, bool bAuto = true)
	{
		if (bAuto && !this.CanAutoSlot(coIn))
		{
			return 0;
		}
		int num = 0;
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (condOwner == null)
			{
				if (this.bAllowStacks && coIn != null)
				{
					num += coIn.nStackLimit;
				}
				else
				{
					num++;
				}
			}
			else if (this.bAllowStacks)
			{
				num += condOwner.CanStackOnItem(coIn);
			}
		}
		return num;
	}

	public bool CanFit(CondOwner coFit, bool bAuto = true, bool bSub = false)
	{
		if (this.aCOs == null)
		{
			return false;
		}
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (this.bHoldSlot && condOwner == null && coFit.mapSlotEffects.ContainsKey(this.strName) && (!bAuto || this.CanAutoSlot(coFit)))
			{
				return true;
			}
			if (condOwner != null && condOwner.objContainer != null && condOwner.objContainer.CanFit(coFit, bAuto, bSub))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanAutoSlot(CondOwner co)
	{
		return !(co == null) && (this.ctAutoSlot == null || this.ctAutoSlot.Triggered(co, null, true));
	}

	public void Destroy()
	{
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (condOwner != null)
			{
				condOwner.Destroy();
			}
		}
		this.aCOs = null;
		this.compSlots = null;
	}

	public void VisitCOs(CondOwnerVisitor visitor, bool bAllowLocked)
	{
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (!(condOwner == null))
			{
				visitor.Visit(condOwner);
				condOwner.VisitCOs(visitor, bAllowLocked);
			}
		}
	}

	public CondOwner GetCORef(CondOwner co)
	{
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (!(condOwner == null))
			{
				if (condOwner == co)
				{
					return co;
				}
				CondOwner coref = condOwner.GetCORef(co);
				if (coref != null)
				{
					return coref;
				}
			}
		}
		return null;
	}

	public CondOwner RemoveCO(CondOwner co, bool bForce = false)
	{
		CondOwner condOwner = null;
		foreach (CondOwner condOwner2 in this.aCOs)
		{
			if (!(condOwner2 == null))
			{
				condOwner = condOwner2.RemoveCO(co, bForce);
				if (condOwner != null)
				{
					break;
				}
			}
		}
		return condOwner;
	}

	public CondOwner GetOutermostCO()
	{
		for (int i = this.aCOs.Length - 1; i >= 0; i--)
		{
			CondOwner condOwner = this.aCOs[i];
			if (condOwner != null)
			{
				return condOwner;
			}
		}
		return null;
	}

	public List<Slot> GetSlots(bool bDeep, bool bChildFirst)
	{
		List<Slot> list = new List<Slot>();
		if (this.aCOs == null)
		{
			return list;
		}
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (!(condOwner == null))
			{
				if (bChildFirst)
				{
					list.InsertRange(0, condOwner.GetSlots(bDeep, Slots.SortOrder.CHILD_FIRST));
				}
				else
				{
					list.AddRange(condOwner.GetSlots(bDeep, Slots.SortOrder.HELD_FIRST));
				}
			}
		}
		return list;
	}

	public override string ToString()
	{
		return this.strName;
	}

	public string FriendlyName
	{
		get
		{
			if (this.strNameFriendly != null)
			{
				return this.strNameFriendly;
			}
			return this.strName;
		}
	}

	public string strName;

	public string strNameFriendly;

	public string strHitboxImage;

	public string strIconImage;

	public CondOwner[] aCOs;

	public Vector2 ptAlign;

	public int nDepth;

	public bool bAlignSlot;

	public bool bHoldSlot;

	public bool bCarried;

	public bool bAllowStacks;

	public bool bHide;

	private CondTrigger ctAutoSlot;

	public Slots compSlots;
}
