using System;
using System.Collections.Generic;
using System.Text;
using Ostranauts.Core.Models;
using UnityEngine;

// Inventory/container grid attached to a CondOwner.
// This handles carrying, nested storage, stack merging, and tile-space checks
// for items that live inside another CondOwner's internal container.
public class Container : MonoBehaviour
{
	// Destroys all contained CondOwners and clears container-only references.
	public void Destroy()
	{
		for (int i = this.aCOs.Count - 1; i >= 0; i--)
		{
			this.aCOs[i].Destroy();
		}
		this.aCOs.Clear();
		this.aCOs = null;
		this.ctAllowed = null;
		this.gridLayout = null;
		this.co = null;
	}

	// Rejects invalid inserts such as self-parenting, ancestor loops, or CondTrigger failures.
	public bool AllowedCO(CondOwner coIn)
	{
		if (coIn == null || coIn == this.CO)
		{
			return false;
		}
		CondOwner objCOParent = this.CO.objCOParent;
		while (objCOParent != null)
		{
			if (coIn == objCOParent)
			{
				return false;
			}
			objCOParent = objCOParent.objCOParent;
		}
		return this.ctAllowed == null || this.ctAllowed.Triggered(coIn, null, true);
	}

	// Attempts stack merging before falling back to normal grid placement.
	private CondOwner StackOnInsideItem(CondOwner coIncoming)
	{
		if (!this.bAllowStacking)
		{
			return coIncoming;
		}
		CondOwner condOwner = coIncoming;
		foreach (CondOwner condOwner2 in this.aCOs)
		{
			condOwner = condOwner2.StackCO(coIncoming);
			if (condOwner != coIncoming)
			{
				break;
			}
		}
		for (int i = this.aCOs.Count - 1; i >= 0; i--)
		{
			CondOwner condOwner3 = this.aCOs[i];
			if (!(condOwner3.coStackHead == null))
			{
				if (this.Contains(condOwner3.coStackHead) && condOwner3.coStackHead.StackAsList.Contains(condOwner3))
				{
					this.aCOs.Remove(condOwner3);
					this.CO.AddMass(-condOwner3.GetTotalMass(), false);
				}
			}
		}
		return condOwner;
	}

	// Converts an occupant into the number of inventory tiles it consumes.
	public static int GetSpace(CondOwner co)
	{
		if (co == null)
		{
			return 0;
		}
		if (co.Item != null)
		{
			return co.Item.nWidthInTiles * co.Item.nHeightInTiles;
		}
		return 1;
	}

	// Checks whether the target can fit in this container, optionally recursing
	// into slots and sub-containers when the caller allows nested placement.
	public bool CanFit(CondOwner coFit, bool bAuto, bool bSub)
	{
		int num = 0;
		if (this.ctAllowed == null || this.ctAllowed.Triggered(coFit, null, true))
		{
			if (this.CO.HasCond("IsInfiniteContainer"))
			{
				return true;
			}
			num = this.gridLayout.gridMaxSpace;
		}
		int num2 = 0;
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (!(condOwner.coStackHead != null))
			{
				num2 += Container.GetSpace(condOwner);
				if (bSub)
				{
					if (condOwner.compSlots != null)
					{
						foreach (Slot slot in condOwner.compSlots.GetSlotsHeldFirst(false))
						{
							if (slot.CanFit(coFit, bAuto, bSub))
							{
								return true;
							}
						}
					}
					if (condOwner.objContainer != null && condOwner.objContainer.CanFit(coFit, bAuto, bSub))
					{
						return true;
					}
				}
			}
		}
		return num > num2;
	}

	// Primary insert path: validate, try stack merge, place in the grid, then fall back to nested storage.
	public CondOwner AddCO(CondOwner objCO)
	{
		if (objCO == null || objCO.bDestroyed)
		{
			Debug.Log("Container.AddCO(), trying to add CO that is null or destroyed. This should probably crash");
			return null;
		}
		if (objCO.coStackHead != null)
		{
			Debug.Log("Container.AddCO(), Trying to add a CO that's not a stackhead. This is probably a logic error in the caller.");
			return null;
		}
		int num = this.aCOs.IndexOf(objCO);
		if (num >= 0)
		{
			Debug.Log("ERROR: Trying to add a CondOwner that's already been added");
			Debug.Break();
			return null;
		}
		if (this.AllowedCO(objCO))
		{
			objCO = this.StackOnInsideItem(objCO);
			if (objCO == null)
			{
				this.Redraw();
				return null;
			}
			PairXY pairXY;
			if (this.CanAddSimple(objCO, out pairXY))
			{
				this.AddCOSimple(objCO, pairXY);
				objCO = null;
			}
			this.Redraw();
			if (objCO == null)
			{
				return objCO;
			}
		}
		CondOwner condOwner = objCO;
		foreach (CondOwner condOwner2 in this.aCOs)
		{
			condOwner = condOwner2.AddCO(condOwner, false, true, false);
			if (condOwner == null)
			{
				break;
			}
		}
		return condOwner;
	}

	// Applies container-related conditions and reparents the item under the carrier object in Unity.
	public void SetIsInContainer(CondOwner co)
	{
		if (this.CO == this)
		{
			Debug.Log("ERROR: Assigning self as own parent.");
		}
		co.objCOParent = this.CO;
		if (co.coStackHead == null)
		{
			co.tf.SetParent(this.CO.tf);
			co.tf.localPosition = new Vector3(0f, 0f, Container.fZSubOffset);
			co.Visible = false;
		}
		if (!this.CO.HasCond("IsHuman"))
		{
			co.AddCondAmount("IsInContainer", 1.0, 0.0, 0f);
		}
		CondOwner objCOParent = this.CO;
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
	}

	public void ClearIsInContainer(CondOwner co)
	{
		co.ZeroCondAmount("IsInContainer");
		co.ZeroCondAmount("IsCarried");
		co.VisitCOs(new CondOwnerVisitorZeroCond
		{
			strCond = "IsCarried"
		}, true);
		co.objCOParent = null;
	}

	public bool CanAddSimple(CondOwner objCO, out PairXY pairXY)
	{
		if (this.Contains(objCO))
		{
			Debug.Log("Trying to add a CO to a container, but it's already here!");
			pairXY = PairXY.GetInvalid();
			return false;
		}
		if (this.CO.HasCond("IsInfiniteContainer"))
		{
			pairXY = PairXY.GetInvalid();
			return true;
		}
		PairXY widthHeightForCO = GUIInventoryItem.GetWidthHeightForCO(objCO);
		int x = widthHeightForCO.x;
		int y = widthHeightForCO.y;
		pairXY = this.gridLayout.FindFirstUnoccupiedTile(x, y, objCO.strID);
		return pairXY.IsValid();
	}

	public void AddCOList(CondOwner objCO)
	{
		if (!this.aCOs.Contains(objCO))
		{
			this.aCOs.Add(objCO);
		}
	}

	public void AddCOSimple(CondOwner objCO, PairXY pairXY)
	{
		if (objCO == null)
		{
			return;
		}
		if (objCO.coStackHead != null)
		{
			Debug.Log("Container.AddCOSimple(), Trying to add a CO that's not a stackhead. This is probably a logic error in the caller.");
			return;
		}
		objCO.ValidateParent();
		this.aCOs.Add(objCO);
		this.SetIsInContainer(objCO);
		if (pairXY.IsValid())
		{
			objCO.pairInventoryXY = pairXY;
			foreach (CondOwner condOwner in objCO.aStack)
			{
				condOwner.pairInventoryXY = pairXY;
			}
			PairXY widthHeightForCO = GUIInventoryItem.GetWidthHeightForCO(objCO);
			int x = widthHeightForCO.x;
			int y = widthHeightForCO.y;
			for (int i = pairXY.y; i < pairXY.y + y; i++)
			{
				for (int j = pairXY.x; j < pairXY.x + x; j++)
				{
					if (j >= 0 && i >= 0)
					{
						if (j < this.gridLayout.gridMaxX)
						{
							if (i < this.gridLayout.gridMaxY)
							{
								this.gridLayout.gridID[j, i] = objCO.strID;
							}
						}
					}
				}
			}
		}
		foreach (CondOwner condOwner2 in objCO.aStack)
		{
			this.SetIsInContainer(condOwner2);
			condOwner2.ship = this.CO.ship;
		}
		this.co.AddMass(objCO.GetTotalMass(), false);
		objCO.strSourceCO = null;
		objCO.strSourceInteract = null;
		if (this.CO.ship != null)
		{
			this.CO.ship.AddCO(objCO, false);
		}
		objCO.ValidateParent();
	}

	public void RemoveCOSimple(CondOwner objCO)
	{
		if (objCO == null)
		{
			return;
		}
		objCO.ValidateParent();
		if (objCO.objCOParent.ship != null)
		{
			objCO.objCOParent.ship.RemoveCO(objCO, false);
		}
		this.aCOs.Remove(objCO);
		this.ClearIsInContainer(objCO);
		objCO.tf.SetParent(null);
		if (objCO.Item != null)
		{
			objCO.Item.ResetTransforms(this.CO.tf.position.x, this.CO.tf.position.y);
		}
		foreach (CondOwner condOwner in objCO.aStack)
		{
			this.ClearIsInContainer(condOwner);
			condOwner.ship = null;
		}
		this.gridLayout.Remove(objCO.strID);
		this.CO.AddMass(-objCO.GetTotalMass(), false);
		objCO.ValidateParent();
	}

	public CondOwner RemoveCO(CondOwner objCO, bool bForce = false)
	{
		if (objCO == null || this.aCOs.Count == 0)
		{
			return null;
		}
		if (objCO.objCOParent == this.CO)
		{
			objCO.tf.position = this.CO.tf.position;
			this.RemoveCOSimple(objCO);
			foreach (CondOwner condOwner in objCO.aStack)
			{
				this.aCOs.Remove(condOwner);
				if (condOwner.objCOParent != objCO && condOwner.objCOParent != null)
				{
					Debug.Log(string.Concat(new object[]
					{
						"ERROR: Item in ",
						objCO.strName,
						" stack has different CO parent: ",
						condOwner.objCOParent
					}));
				}
				else
				{
					this.ClearIsInContainer(condOwner);
					condOwner.tf.position = this.CO.tf.position;
				}
			}
			CrewSim.inventoryGUI.RemoveAndDestroy(objCO.strID);
			return objCO;
		}
		foreach (CondOwner condOwner2 in this.aCOs)
		{
			CondOwner condOwner3 = condOwner2.RemoveCO(objCO, bForce);
			if (condOwner3 != null)
			{
				return condOwner3;
			}
		}
		return null;
	}

	public CondOwner GetCORef(CondOwner co)
	{
		if (this == null)
		{
			Debug.Log("ERROR: Getting CO from a null");
			Debug.Break();
			return null;
		}
		if (co == null)
		{
			return null;
		}
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (condOwner == co)
			{
				return condOwner;
			}
			CondOwner coref = condOwner.GetCORef(co);
			if (coref != null)
			{
				return coref;
			}
		}
		return null;
	}

	public void VisitCOs(CondOwnerVisitor visitor, bool bAllowLocked)
	{
		if (!bAllowLocked && this.CO.HasCond("IsLocked"))
		{
			return;
		}
		foreach (CondOwner condOwner in this.aCOs)
		{
			if (!(condOwner == null))
			{
				visitor.Visit(condOwner);
				condOwner.VisitCOs(visitor, bAllowLocked);
			}
		}
	}

	public List<CondOwner> GetCOs(bool bAllowLocked, CondTrigger objCondTrig = null)
	{
		CondOwnerVisitorAddToHashSet condOwnerVisitorAddToHashSet = new CondOwnerVisitorAddToHashSet();
		CondOwnerVisitor visitor = CondOwnerVisitorCondTrigger.WrapVisitor(condOwnerVisitorAddToHashSet, objCondTrig);
		this.VisitCOs(visitor, bAllowLocked);
		return new List<CondOwner>(condOwnerVisitorAddToHashSet.aHashSet);
	}

	public string GetAltImageMatch(Dictionary<string, string> mapAltImages)
	{
		if (mapAltImages == null || mapAltImages.Keys.Count == 0 || this.aCOs.Count == 0)
		{
			return null;
		}
		foreach (CondOwner condOwner in this.aCOs)
		{
			foreach (string text in mapAltImages.Keys)
			{
				if (condOwner.HasCond(text))
				{
					return mapAltImages[text];
				}
			}
		}
		return null;
	}

	public bool Contains(CondOwner co)
	{
		return this.aCOs.IndexOf(co) >= 0;
	}

	public void DebugInv(StringBuilder sb)
	{
		sb.AppendLine(this.CO.strID + " contains:");
		foreach (CondOwner condOwner in this.aCOs)
		{
			sb.AppendLine(string.Concat(new object[]
			{
				this.CO.strID,
				"->",
				condOwner.strID,
				".bDestroyed = ",
				condOwner.bDestroyed
			}));
			condOwner.DebugInv(sb, this.CO.strID + "->");
		}
	}

	public static void Redraw(Container container)
	{
		if (container != null)
		{
			container.Redraw();
		}
	}

	public void Redraw()
	{
		if (this.co == null)
		{
			return;
		}
		GUIInventoryWindow inventoryWindow = this.InventoryWindow;
		if (inventoryWindow != null)
		{
			List<CondOwner> cos = this.GetCOs(true, null);
			List<string> list = new List<string>();
			foreach (CondOwner condOwner in cos)
			{
				if (!(condOwner.coStackHead != null))
				{
					if (!condOwner.HasCond("IsHiddenInv"))
					{
						if (condOwner.objCOParent == this.CO)
						{
							list.Add(condOwner.strID);
						}
						if (condOwner.objContainer != null)
						{
							condOwner.objContainer.Redraw();
						}
					}
				}
			}
			list.Sort(delegate(string csA, string csB)
			{
				CondOwner condOwner2;
				DataHandler.mapCOs.TryGetValue(csA, out condOwner2);
				CondOwner condOwner3;
				DataHandler.mapCOs.TryGetValue(csB, out condOwner3);
				return Container.GetSpace(condOwner3).CompareTo(Container.GetSpace(condOwner2));
			});
			inventoryWindow.RedrawWindowContents(list);
		}
		if (this.co.slotNow != null && Slots.OnSlotContentUpdated != null)
		{
			Slots.OnSlotContentUpdated.Invoke(this.co, null);
		}
		this.co.UpdateAppearance();
	}

	public GUIInventoryWindow InventoryWindow
	{
		get
		{
			foreach (GUIInventoryWindow guiinventoryWindow in CrewSim.inventoryGUI.activeWindows)
			{
				if (guiinventoryWindow.CO == this.CO && guiinventoryWindow.type == InventoryWindowType.Container)
				{
					return guiinventoryWindow;
				}
			}
			return null;
		}
	}

	public CondOwner CO
	{
		get
		{
			if (this.co == null)
			{
				this.co = base.GetComponent<CondOwner>();
				this.aCOs.Track(delegate(bool any)
				{
					if (this.co != null)
					{
						this.co.HasSubCOs = any;
					}
				});
			}
			return this.co;
		}
	}

	public bool Locked
	{
		get
		{
			return this.CO.HasCond("IsLocked");
		}
	}

	public static float fZSubOffset = 0.1f;

	private TrackingCollection<CondOwner> aCOs = new TrackingCollection<CondOwner>();

	private CondOwner co;

	public CondTrigger ctAllowed;

	public bool bAllowStacking = true;

	public GridLayout gridLayout = new GridLayout(6, 6);
}
