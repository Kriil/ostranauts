using System;

[Serializable]
public class JsonRoomSpec
{
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strIconName { get; set; }

	public int nMinTileSize { get; set; }

	public int nMaxTileSize { get; set; }

	public int nPriority { get; set; }

	public string[] aReqs { get; set; }

	public string[] aForbids { get; set; }

	public float fValueModifier { get; set; }

	public bool bAllowVoid { get; set; }
}
