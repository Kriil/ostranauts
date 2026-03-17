using System;

namespace Ostranauts.Blueprints;

[Serializable]
public sealed class BlueprintData
{
	public string strName;
	public string strSourceShip;
	public string strCreatedAtUtc;
	public int nRotationSteps;
	public float fOriginX;
	public float fOriginY;
	public float fWidth;
	public float fHeight;
	public BlueprintItemData[] aItems;
}

[Serializable]
public sealed class BlueprintItemData
{
	public string strName;
	public string strSourceCODef;
	public float fX;
	public float fY;
	public float fRotation;

	public BlueprintItemData Clone()
	{
		return new BlueprintItemData
		{
			strName = strName,
			strSourceCODef = strSourceCODef,
			fX = fX,
			fY = fY,
			fRotation = fRotation
		};
	}
}

[Serializable]
public sealed class BlueprintPart
{
	public BlueprintItemData Item;
	public string SourceCODef;
	public string InstallInteractionName;
	public string UninstallInteractionName;
	public string TargetCOID;
}
