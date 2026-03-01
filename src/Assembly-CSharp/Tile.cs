using System;
using System.Collections.Generic;
using System.Text;
using Ostranauts.Pathing;
using Ostranauts.Ships.Rooms;
using UnityEngine;

// One ship/world grid tile. Tiles appear to carry per-cell CondOwner state,
// pathing metadata, zone flags, and debug/render helpers for placement logic.
public class Tile : MonoBehaviour
{
	// Tile-level CondOwner storing wall/portal/obstruction and zone Conditions.
	public CondOwner coProps
	{
		get
		{
			return this._coProps;
		}
		set
		{
			this._coProps = value;
			if (this._coProps != null)
			{
				this.IsPortal = this._coProps.HasCond("IsPortal", false);
				this.IsWall = this._coProps.HasCond("IsWall", false);
				this.bPassable = !this._coProps.HasCond("IsObstruction", false);
			}
			else
			{
				this.IsPortal = false;
				this.IsWall = false;
				this.bPassable = false;
			}
		}
	}

	public bool IsPortal { get; private set; }

	public bool IsWall { get; private set; }

	public Vector2Int Position
	{
		get
		{
			return new Vector2Int(this.tf.position.x, this.tf.position.y);
		}
	}

	private static bool IsInStrongGravField
	{
		get
		{
			return Tile._gravDistanceToBO < Tile._gravStrongFieldRadius;
		}
	}

	// Unity setup: caches renderer state and normalizes the tile render depth.
	public void Awake()
	{
		this.tf = base.transform;
		this.rend = base.GetComponent<Renderer>();
		this.bPassable = false;
		this.aConnectedPowerCOs = new List<Powered>();
		this.mapPaths = new Dictionary<Pathfinder, float>();
		this.mr = base.gameObject.GetComponent<MeshRenderer>();
		this.mtlMain = this.mr.sharedMaterial;
		this.SetColor(Tile.clrDefault);
		this.tf.position = new Vector3(this.tf.position.x, this.tf.position.y, -2f);
	}

	// Tracks weak/strong gravity transitions from the active BodyOrbit so EVA and
	// tile hazard logic can respond to nearby gravity wells.
	public static Tile.GravField SetTileGravitationalForces(BodyOrbit bo, double distanceToBO)
	{
		if (bo != null && bo.GravRadius != 0.0)
		{
			Tile._gravWeakFieldRadius = bo.GravRadius;
			Tile._gravStrongFieldRadius = ((bo.fParallaxRadius <= bo.GravRadius) ? bo.fParallaxRadius : bo.GravRadius);
		}
		Tile._gravDistanceToBO = distanceToBO;
		if (!Tile._gravStrongFieldWarning && Tile.IsInStrongGravField)
		{
			Tile._gravStrongFieldWarning = true;
			Tile._gravWeakFieldWarning = false;
			return Tile.GravField.WeakToStrongTransition;
		}
		if (Tile._gravStrongFieldWarning && !Tile.IsInStrongGravField)
		{
			Tile._gravStrongFieldWarning = false;
			Tile._gravWeakFieldWarning = true;
			return Tile.GravField.StrongToWeakTransition;
		}
		if (!Tile._gravWeakFieldWarning && !Tile.IsInStrongGravField && distanceToBO < Tile._gravWeakFieldRadius)
		{
			Tile._gravStrongFieldWarning = false;
			Tile._gravWeakFieldWarning = true;
			return Tile.GravField.NoneToWeakTransition;
		}
		if (Tile._gravWeakFieldWarning && distanceToBO > Tile._gravWeakFieldRadius)
		{
			Tile._gravStrongFieldWarning = false;
			Tile._gravWeakFieldWarning = false;
			return Tile.GravField.WeakToNoneTransition;
		}
		return Tile.GravField.None;
	}

	// Applies one JsonZone to this tile by removing prior zone Conditions,
	// adding the new zone conds, and tinting the tile debug color.
	public void SetZone(JsonZone jz, List<string> aCondRemoves = null)
	{
		if (aCondRemoves != null)
		{
			foreach (string text in aCondRemoves)
			{
				if (!string.IsNullOrEmpty(text))
				{
					this.coProps.ZeroCondAmount(text);
				}
			}
		}
		if (jz.aTileConds != null && jz.aTileConds.Length > 0)
		{
			foreach (string text2 in jz.aTileConds)
			{
				if (!string.IsNullOrEmpty(text2) && (!this.coProps.HasCond(text2) || this.coProps.GetCondAmount(text2) <= 0.0))
				{
					this.coProps.AddCondAmount(text2, 1.0, 0.0, 0f);
				}
			}
			this.SetColor(jz.zoneColor);
			this.jZone = jz;
			return;
		}
		this.SetColor(Tile.clrDefault);
	}

	// Debug visibility toggle tied to the global tile-visibility setting.
	public void ToggleVis()
	{
		if (this.bShipTile && this.rend != null && this.rend.enabled != TileUtils.bShowTiles)
		{
			this.rend.enabled = TileUtils.bShowTiles;
		}
	}

	// Explicit debug visibility override.
	public void ToggleVis(bool showDebug)
	{
		this.rend.enabled = showDebug;
	}

	// Local cleanup helper for cached references.
	public void Destroy()
	{
		this.mtlMain = null;
		this.coProps = null;
		this.rend = null;
		this.room = null;
		if (this.aConnectedPowerCOs != null)
		{
			this.aConnectedPowerCOs.Clear();
			this.aConnectedPowerCOs = null;
		}
		if (this.mapPaths != null)
		{
			this.mapPaths.Clear();
			this.mapPaths = null;
		}
	}

	// Likely used by EVA/pathing checks when exposed tiles are inside a strong
	// gravity field.
	public bool IsEvaTileWithGravitation()
	{
		return Tile.IsInStrongGravField && (this.coProps.HasCond("IsEVATile", false) || (!TileUtils.CTShipTile.Triggered(this.coProps, null, true) && !this.coProps.HasCond("IsFloorFlex", false)));
	}

	// Returns true if this tile is a matching forbid zone for the actor.
	public bool IsForbidden(CondOwner coCheck)
	{
		bool result = false;
		if (this.coProps.HasCond("IsZoneForbid", false) && this.jZone != null)
		{
			result = this.jZone.Matches(coCheck, false);
		}
		return result;
	}

	// Returns true if this tile is a matching trigger zone for the actor.
	public bool IsTriggerZone(CondOwner coCheck)
	{
		return this.coProps.HasCond("IsZoneTrigger", false) && this.jZone.Matches(coCheck, true);
	}

	// Applies a debug tint to the tile mesh.
	public void SetColor(float fR, float fG, float fB)
	{
		this.SetColor(new Color(fR, fG, fB));
	}

	// Applies a debug tint to the tile mesh via a material property block.
	public void SetColor(Color c)
	{
		if (this.clr == c)
		{
			return;
		}
		this.clr = c;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		this.mr.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor("_TintColor", this.clr);
		this.mr.SetPropertyBlock(materialPropertyBlock);
	}

	public void SetMat(string strImg)
	{
		string key = "TileMat" + strImg;
		if (DataHandler.dictMaterials.TryGetValue(key, out this.mtlMain))
		{
			this.mr.sharedMaterial = this.mtlMain;
			return;
		}
		this.mtlMain = new Material(this.mr.sharedMaterial);
		this.mtlMain.SetTexture("_MainTex", DataHandler.LoadPNG(strImg + ".png", false, false));
		this.mtlMain.name = strImg;
		DataHandler.dictMaterials[key] = this.mtlMain;
		this.mr.sharedMaterial = this.mtlMain;
	}

	public void ShowTileWithFullAlphaColor(bool full)
	{
		Color color = Color.magenta;
		if (this.clr.a < 0.5f)
		{
			color = ((!full) ? new Color(this.clr.r, this.clr.g, this.clr.b, 0.1f) : Tile.clrDefault);
		}
		else
		{
			color = this.clr;
		}
		this.SetColor(color);
	}

	public void UpdateFlags()
	{
		this.coProps = base.GetComponent<CondOwner>();
		if (this.coProps != null)
		{
			if (CrewSim.bDebug01 && this.coProps.HasCond("IsItemTile"))
			{
				this.SetColor(Color.red);
			}
			else if (this.jZone == null)
			{
				this.SetColor(Tile.clrDefault);
			}
		}
	}

	public static void AddToRoom(Tile tile, CondOwner co, bool addEffects)
	{
		if (tile == null || co == null)
		{
			return;
		}
		if (tile.room == null)
		{
			Vector2 pos = co.GetPos("use", false);
			tile = co.ship.GetTileAtWorldCoords1(pos.x, pos.y, false, true);
			if (tile == null || tile.room == null)
			{
				return;
			}
		}
		tile.room.AddToRoom(co, addEffects);
	}

	public RoomSpec GetRoomDef()
	{
		if (this.room == null)
		{
			return null;
		}
		return this.room.GetRoomSpec();
	}

	public override string ToString()
	{
		string text = string.Concat(new object[]
		{
			this.nIndex,
			": ",
			this.tf.position.x,
			",",
			this.tf.position.y,
			"; ",
			this.strDebug,
			"; "
		});
		foreach (string text2 in this.coProps.mapConds.Keys)
		{
			if (text2[0] != 'T')
			{
				text = text + text2 + "\n";
			}
		}
		return text;
	}

	public static bool IsDoor(float x, float y, CondOwner coUs)
	{
		Tile tileAtWorldCoords = coUs.ship.GetTileAtWorldCoords1(x, y, true, true);
		return tileAtWorldCoords != null && tileAtWorldCoords.IsPortal;
	}

	public bool IsWalkable(CondOwner coUs, PathResult pr)
	{
		if (coUs == null || coUs.ship == null)
		{
			return false;
		}
		if (this.IsForbidden(coUs))
		{
			pr.bForbidZoneBlocked = true;
			return false;
		}
		if (this.IsWall && !this.IsPortal)
		{
			return false;
		}
		if (this.IsPortal)
		{
			if (this.coProps.HasCond("IsPortalStuck"))
			{
				return false;
			}
			if (!pr.AirlocksAllowed && this.IsWall && Pathfinder.CheckDoorPressure(this.tf.position, this.coProps.ship, this.room))
			{
				pr.bAirlockBlocked = true;
				return false;
			}
		}
		if (!this.bPassable && this.coProps.HasCond("IsFixture"))
		{
			return false;
		}
		if (this.IsEvaTileWithGravitation())
		{
			pr.bGravBlocked = true;
			return false;
		}
		return true;
	}

	public static bool IsEVATile(Tile tile)
	{
		return !(tile == null) && !(tile.coProps == null) && (tile.coProps.HasCond("IsEVATile") || tile.coProps.HasCond("IsFloorFlex"));
	}

	public int Index
	{
		get
		{
			return this.nIndex;
		}
		set
		{
			if (value == this.nIndex)
			{
				return;
			}
			this.nIndex = value;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.nIndex);
			stringBuilder.Append(";");
			stringBuilder.Append(this.strDebug);
			StringBuilder stringBuilder2 = new StringBuilder();
			if (this.tf != null)
			{
				stringBuilder2.Append(this.tf.position.x);
				stringBuilder2.Append(",");
				stringBuilder2.Append(this.tf.position.y);
			}
			this.coProps.mapInfo["Index"] = stringBuilder.ToString();
			this.coProps.mapInfo["Coords"] = stringBuilder2.ToString();
		}
	}

	private MeshRenderer mr;

	private Material mtlMain;

	private int nIndex = -1;

	public Room room;

	public bool bPathChecked;

	public bool bPassable;

	public bool bShipTile;

	private CondOwner _coProps;

	public Transform tf;

	private Renderer rend;

	public List<Powered> aConnectedPowerCOs;

	public Dictionary<Pathfinder, float> mapPaths;

	public JsonZone jZone;

	private Color clr;

	public static Color clrDefault = new Color(0.5f, 0.5f, 0.5f, 0.3f);

	public string strStackableCO;

	public string strDebug = string.Empty;

	private static double _gravWeakFieldRadius = 1.614E-11;

	private static double _gravStrongFieldRadius = 4E-11;

	private static double _gravDistanceToBO;

	private static bool _gravWeakFieldWarning;

	private static bool _gravStrongFieldWarning;

	public enum GravField
	{
		None,
		NoneToWeakTransition,
		WeakToStrongTransition,
		StrongToWeakTransition,
		WeakToNoneTransition
	}
}
