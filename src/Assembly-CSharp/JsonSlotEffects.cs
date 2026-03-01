using System;

// Visual/behavior overrides applied when a specific slot state is active.
// Likely loaded from StreamingAssets/data/slot_effects and attached through
// CondOwner overlays or slot configuration.
public class JsonSlotEffects
{
	// Registry id plus the primary slot this effect targets.
	public string strName { get; set; }

	public string strSlotPrimary { get; set; }

	public string strSlotImage { get; set; }

	public string strSlotImageUnder { get; set; }

	public string strIASlot { get; set; }

	public string strIAUnslot { get; set; }

	public string strIASlotParents { get; set; }

	public string strIAUnslotParents { get; set; }

	public string[] aSlotsSecondary { get; set; }

	public string[] aSlotsAdded { get; set; }

	public string[] mapMeshTextures { get; set; }

	public bool bMirror { get; set; }

	public bool bWholeBody { get; set; }

	public float[] aTextAnchors { get; set; }

	// Returns a detached copy so runtime code can mutate per-instance slot effects safely.
	public JsonSlotEffects Clone()
	{
		JsonSlotEffects jsonSlotEffects = new JsonSlotEffects();
		jsonSlotEffects.strName = this.strName;
		jsonSlotEffects.strSlotPrimary = jsonSlotEffects.strSlotPrimary;
		jsonSlotEffects.strSlotImage = this.strSlotImage;
		jsonSlotEffects.strSlotImageUnder = this.strSlotImageUnder;
		jsonSlotEffects.strIASlot = this.strIASlot;
		jsonSlotEffects.strIAUnslot = this.strIAUnslot;
		jsonSlotEffects.strIASlotParents = this.strIASlotParents;
		jsonSlotEffects.strIAUnslotParents = this.strIAUnslotParents;
		jsonSlotEffects.bMirror = this.bMirror;
		jsonSlotEffects.bWholeBody = this.bWholeBody;
		if (this.aTextAnchors != null)
		{
			jsonSlotEffects.aTextAnchors = new float[this.aTextAnchors.Length];
			Array.Copy(this.aTextAnchors, jsonSlotEffects.aTextAnchors, this.aTextAnchors.Length);
		}
		if (this.aSlotsSecondary != null)
		{
			jsonSlotEffects.aSlotsSecondary = new string[this.aSlotsSecondary.Length];
			Array.Copy(this.aSlotsSecondary, jsonSlotEffects.aSlotsSecondary, this.aSlotsSecondary.Length);
		}
		if (this.aSlotsAdded != null)
		{
			jsonSlotEffects.aSlotsAdded = new string[this.aSlotsAdded.Length];
			Array.Copy(this.aSlotsAdded, jsonSlotEffects.aSlotsAdded, this.aSlotsAdded.Length);
		}
		if (this.mapMeshTextures != null)
		{
			jsonSlotEffects.mapMeshTextures = new string[this.mapMeshTextures.Length];
			Array.Copy(this.mapMeshTextures, jsonSlotEffects.mapMeshTextures, this.mapMeshTextures.Length);
		}
		return jsonSlotEffects;
	}

	public override string ToString()
	{
		return this.strName;
	}
}
