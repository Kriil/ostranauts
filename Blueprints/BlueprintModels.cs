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
	public JsonItem[] aItems;
}

[Serializable]
public sealed class BlueprintPart
{
	public JsonItem Item;
	public string InstallInteractionName;
	public string UninstallInteractionName;
	public string TargetCOID;
}
