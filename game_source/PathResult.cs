using System;
using System.Collections.Generic;

public class PathResult
{
	public PathResult()
	{
	}

	public PathResult(bool airlocksAllowed)
	{
		this.AirlocksAllowed = airlocksAllowed;
	}

	public PathResult(Tile tilOrig, Tile tilDest)
	{
		this._tilOrig = tilOrig;
		this._tilDest = tilDest;
		this.CheckDisembarkSimple();
	}

	public float PathLength
	{
		get
		{
			if (!this.HasPath)
			{
				return -1f;
			}
			if (this._pathLength >= 0f)
			{
				return this._pathLength;
			}
			if (this.aTiles == null)
			{
				return -1f;
			}
			return (float)this.aTiles.Count;
		}
		set
		{
			this._pathLength = value;
		}
	}

	public bool AirlocksAllowed { get; private set; }

	public void Destroy()
	{
		this._tilOrig = null;
		this._tilDest = null;
		if (this.aTiles != null)
		{
			this.aTiles.Clear();
			this.aTiles = null;
		}
	}

	public void AddTile(Tile til)
	{
		if (til == null)
		{
			return;
		}
		if (this.aTiles == null)
		{
			this.aTiles = new List<Tile>();
		}
		this.aTiles.Add(til);
	}

	public void SetTiles(List<Tile> aTilesSet)
	{
		this.aTiles = aTilesSet;
	}

	public void CheckDisembarkSimple()
	{
		if (this._tilOrig != null && this._tilOrig.coProps != null && this._tilDest != null && this._tilDest.coProps != null)
		{
			this.bDisembark = (this._tilOrig.coProps.ship != this._tilDest.coProps.ship);
		}
	}

	public bool Disembark
	{
		get
		{
			return this.bDisembark;
		}
		set
		{
			this.bDisembark = value;
		}
	}

	public Tile Origin
	{
		get
		{
			return this._tilOrig;
		}
		set
		{
			this._tilOrig = value;
		}
	}

	public Tile Dest
	{
		get
		{
			return this._tilDest;
		}
		set
		{
			this._tilDest = value;
		}
	}

	public List<Tile> Tiles
	{
		get
		{
			return this.aTiles;
		}
	}

	public bool HasPath
	{
		get
		{
			return this.aTiles != null;
		}
	}

	public string FailReason(CondOwner coUs)
	{
		string text = string.Empty;
		if (this.bDisembarkBlocked)
		{
			text = text + coUs.strName + DataHandler.GetString("AI_PATHFIND_NO_DISEMBARK", false);
		}
		else if (this.bAirlockBlocked)
		{
			if (!coUs.HasCond("IsAirtight"))
			{
				text = text + coUs.strName + DataHandler.GetString("AI_PATHFIND_NO_HELMET", false);
			}
			else
			{
				text = text + coUs.strName + DataHandler.GetString("AI_PATHFIND_NO_AIRLOCK_PERM", false);
			}
		}
		else if (this.bForbidZoneBlocked)
		{
			text = text + coUs.strName + DataHandler.GetString("AI_PATHFIND_FORBIDDENZONE_BLOCK", false);
		}
		else if (this.bGravBlocked)
		{
			text = text + coUs.strName + DataHandler.GetString("AI_PATHFIND_NO_EVA_GRAV", false);
		}
		return text;
	}

	private Tile _tilOrig;

	private Tile _tilDest;

	public bool bAirlockBlocked;

	private bool bDisembark;

	public bool bDisembarkBlocked;

	public bool bGravBlocked;

	public bool bForbidZoneBlocked;

	private List<Tile> aTiles;

	private float _pathLength = -1f;
}
