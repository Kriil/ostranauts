using System;

// Base data definition for a Slot type.
// Slots define attachment/carry points and grid rules for installed or held items.
public class JsonSlot
{
	// Registry id plus UI-facing label and images for hitbox/icon display.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strHitboxImage { get; set; }

	public string strIconImage { get; set; }

	public string strCTAutoSlot { get; set; }

	public int nItems { get; set; }

	public int nDepth { get; set; }

	public float fAlignX { get; set; }

	public float fAlignY { get; set; }

	public bool bAlignSlot { get; set; }

	public bool bHoldSlot { get; set; }

	public bool bCarried { get; set; }

	public bool bAllowStacks { get; set; }

	public bool bHide { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
