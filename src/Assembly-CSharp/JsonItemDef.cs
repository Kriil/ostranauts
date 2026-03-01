using System;

// Base data definition for an item's art and placement behavior.
// Likely loaded from StreamingAssets/data/items and consumed by Item.SetData.
[Serializable]
public class JsonItemDef
{
	// Default values here imply most item JSON can omit optional visual/fit fields.
	public JsonItemDef()
	{
		this.nLayer = 0;
		this.fZScale = 1f;
		this.nCols = 1;
		this.fDmgCut = -999f;
		this.fDmgTrim = -999f;
		this.bLerp = true;
		this.bSinew = true;
		this.aLights = new string[0];
		this.mapPoints = new string[0];
		this.aSocketAdds = new string[0];
		this.aSocketReqs = new string[0];
		this.aSocketForbids = new string[0];
		this.aShadowBoxes = new string[0];
	}

	// Registry id plus image ids used by the item renderer.
	public string strName { get; set; }

	public string strImg { get; set; }

	public string strImgNorm { get; set; }

	public string strImgDamaged { get; set; }

	public string strDmgColor { get; set; }

	public int nDmgMode { get; set; }

	public float fDmgCut { get; set; }

	public float fDmgTrim { get; set; }

	public float fDmgIntensity { get; set; }

	public float fDmgComplexity { get; set; }

	public bool bLerp { get; set; }

	public bool bSinew { get; set; }

	public bool bHasSpriteSheet { get; set; }

	public JsonItemAnimation objAnimation { get; set; }

	public string ctSpriteSheet { get; set; }

	public int nLayer { get; set; }

	public int nCols { get; set; }

	public float fZScale { get; set; }

	public string[] aLights { get; set; }

	public string[] mapPoints { get; set; }

	// Socket lists look like loot-script ids that influence where the item can be installed.
	public string[] aSocketAdds { get; set; }

	public string[] aSocketReqs { get; set; }

	public string[] aSocketForbids { get; set; }

	public string[] aShadowBoxes { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
