using System;

// Save payload for one placed item or crew entity instance.
// Likely: this is embedded in ship saves rather than loaded from
// StreamingAssets/data/items directly; strName should still match an item
// definition id from that folder so runtime code can rebuild the instance.
[Serializable]
public class JsonItem
{
	// Item definition id. Likely maps to StreamingAssets/data/items by strName.
	public string strName { get; set; }

	public float fX { get; set; }

	public float fY { get; set; }

	public float fRotation { get; set; }

	// Parent ids tie the placed item to containers, slots, or owning entities.
	public string strID { get; set; }

	public string strParentID { get; set; }

	public string strSlotParentID { get; set; }

	public bool? bForceLoad { get; set; }

	public JsonGUIPropMap[] aGPMSettings { get; set; }

	// Convenience check used during load to force-spawn entries whose nullable flag is set.
	public bool ForceLoad()
	{
		return this.bForceLoad != null && this.bForceLoad.Value;
	}

	// Likely used by placement/import code to detect duplicate template entries.
	public bool Matches(JsonItem itemB)
	{
		return !(this.strName != itemB.strName) && Math.Abs(this.fX - itemB.fX) <= 0.01f && Math.Abs(this.fY - itemB.fY) <= 0.01f;
	}

	// Applies template rotation and offset before the item is instantiated into a room/ship grid.
	public void Translate(float offsetX, float offsetY, int rotateCWCount)
	{
		for (int i = 0; i < rotateCWCount; i++)
		{
			float fY = this.fY;
			float fY2 = -this.fX;
			this.fX = fY;
			this.fY = fY2;
			this.fRotation += 90f;
			this.fRotation %= 360f;
		}
		this.fX += offsetX;
		this.fY += offsetY;
	}

	// Produces a shallow copy for ship templates, save duplication, or procedural placement.
	public JsonItem Clone()
	{
		JsonItem jsonItem = new JsonItem
		{
			strName = this.strName,
			fX = this.fX,
			fY = this.fY,
			fRotation = this.fRotation,
			strID = this.strID,
			strParentID = this.strParentID,
			strSlotParentID = this.strSlotParentID,
			bForceLoad = new bool?(this.ForceLoad())
		};
		if (this.aGPMSettings != null)
		{
			jsonItem.aGPMSettings = (JsonGUIPropMap[])this.aGPMSettings.Clone();
		}
		return jsonItem;
	}

	public override string ToString()
	{
		return this.strName;
	}
}
