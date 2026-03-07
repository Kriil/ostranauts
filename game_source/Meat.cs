using System;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using UnityEngine;

public class Meat : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.co = base.GetComponent<CondOwner>();
		if (this.CO != null)
		{
			this._fFuel = this.CO.GetCondAmount("StatMeatFuel");
		}
		if (this._fFuel == 0.0)
		{
			this._fFuel = (double)UnityEngine.Random.value;
		}
		this.fTimeOfNextSignalCheck = StarSystem.fEpoch + (double)(UnityEngine.Random.value * 0.5f);
	}

	public void UpdateManual()
	{
		if (this.bTesting)
		{
			return;
		}
		if (!this.bNeedsCheck || StarSystem.fEpoch < this.fTimeOfNextSignalCheck)
		{
			return;
		}
		this.fTimeOfNextSignalCheck = StarSystem.fEpoch + (double)(UnityEngine.Random.value * 0.5f);
		switch (CrewSim.eMeatState)
		{
		case MeatState.Inert:
		case MeatState.Dormant:
			return;
		case MeatState.Spread:
			this.SpreadCheck();
			break;
		case MeatState.Decay:
			this.co.AddCondAmount("StatDamage", (double)(UnityEngine.Random.value * 0.1f), 0.0, 0f);
			this.SpreadCheck();
			break;
		case MeatState.Eradicate:
			this.co.AddCondAmount("StatDamage", (double)UnityEngine.Random.value, 0.0, 0f);
			break;
		case MeatState.Hell:
			this.CatchUp();
			if (this.bCanSpread)
			{
				this.SpreadCheck();
				this.bCanSpread = true;
				this.SpreadCheck();
			}
			break;
		default:
			Debug.LogWarning("Meatstate somehow unknown value, doing nothing!");
			break;
		}
	}

	public void SpreadCheck()
	{
		this.CatchUp();
		if (!this.bCanSpread)
		{
			return;
		}
		if (this.CO == null || this.CO.ship == null || this.co.tf == null)
		{
			return;
		}
		Tile tileAtWorldCoords = this.CO.ship.GetTileAtWorldCoords1(this.CO.tf.position.x, this.CO.tf.position.y, true, true);
		Tile[] array = TileUtils.GetSurroundingTilesCardinalFirst(tileAtWorldCoords, false, false);
		Tile tile = array[MathUtils.Rand(0, array.Length, MathUtils.RandType.Flat, null)];
		if (tile == null || tile.tf == null)
		{
			Debug.Log("Meat was unable to find nearby tile.");
			return;
		}
		Tuple<Vector2, Vector2> airlockBounds = TileUtils.GetAirlockBounds(this.CO.ship);
		List<CondOwner> list = new List<CondOwner>();
		this.CO.ship.GetCOsAtWorldCoords1(tile.tf.position, this.TriggerFight, true, false, list);
		if (list.Count > 0)
		{
			CondOwner condOwner = list[MathUtils.Rand(0, list.Count, MathUtils.RandType.Flat, null)];
			if (condOwner != null)
			{
				Interaction interaction = DataHandler.GetInteraction("ACTMeleeMeatCrush", null, false);
				this.CO.QueueInteraction(condOwner, interaction, false);
				return;
			}
		}
		if (this.TriggerFuel.Triggered(tile.coProps, null, true))
		{
			list = new List<CondOwner>();
			this.CO.ship.GetCOsAtWorldCoords1(tile.tf.position, this.TriggerFuel, true, false, list);
			bool flag = tile.room == null || tile.room.IsAirless;
			for (int i = list.Count - 1; i >= 0; i--)
			{
				CondOwner condOwner2 = list[i];
				if (!(condOwner2 == null))
				{
					bool flag2 = true;
					if (flag && MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.3)
					{
						flag2 = false;
					}
					else if (!flag && MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.1)
					{
						flag2 = false;
					}
					Meat component = condOwner2.GetComponent<Meat>();
					if (component != null)
					{
						if (flag2)
						{
							component.Fuel += this._fSpreadThreshold * (double)MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
						}
						else
						{
							condOwner2.RemoveFromCurrentHome(true);
							condOwner2.Destroy();
						}
					}
				}
			}
			this.bCanSpread = false;
			return;
		}
		List<Ship> allDockedShips = this.CO.ship.GetAllDockedShips();
		Ship ship = this.CO.ship;
		if (tile != null && this.TriggerSpread.Triggered(tile.coProps, null, true))
		{
			if (!TileUtils.IsTileAboveAirlock(tile, airlockBounds))
			{
				if (allDockedShips.Count == 0)
				{
					return;
				}
				foreach (Ship ship2 in allDockedShips)
				{
					Tuple<Vector2, Vector2> airlockBounds2 = TileUtils.GetAirlockBounds(ship2);
					if (TileUtils.IsTileAboveAirlock(tile, airlockBounds2))
					{
						ship = ship2;
						tile = ship.GetTileAtWorldCoords1(tile.tf.position.x, tile.tf.position.y, false, true);
						if (tile == null || !this.TriggerSpread.Triggered(tile.coProps, null, true))
						{
							return;
						}
						break;
					}
				}
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger("CTPLOT_Meat_Support");
			bool flag3 = false;
			array = TileUtils.GetSurroundingTiles(tile, false, false);
			foreach (Tile tile2 in array)
			{
				if (!(tile2 == null))
				{
					if (condTrigger.Clone().Triggered(tile2.coProps, null, true))
					{
						flag3 = true;
						break;
					}
				}
			}
			if (!flag3)
			{
				this.bCanSpread = false;
				return;
			}
			List<CondOwner> list2 = new List<CondOwner>();
			list2.AddRange(DataHandler.GetLoot("ItmMeatRand").GetCOLoot(null, false, null));
			foreach (CondOwner condOwner3 in list2)
			{
				if (!(condOwner3 == null))
				{
					condOwner3.tf.position = new Vector3(tile.tf.position.x, tile.tf.position.y, -2.5f);
					if (ship != null)
					{
						ship.AddCO(condOwner3, true);
					}
				}
			}
			this.bCanSpread = false;
			return;
		}
	}

	public void CatchUp()
	{
		if (this.bCanSpread)
		{
			return;
		}
		double num;
		if (this.fTimeOfLastSignalCheck != 0.0)
		{
			num = StarSystem.fEpoch - this.fTimeOfLastSignalCheck;
		}
		else
		{
			num = (double)Time.deltaTime;
		}
		this.fTimeOfLastSignalCheck = StarSystem.fEpoch;
		num *= Meat._fStandardGrowthRate * (double)UnityEngine.Random.value;
		this.Grow(num);
	}

	public void Grow(double growth)
	{
		if (this.bCanSpread || this.CO == null)
		{
			return;
		}
		double condAmount = this.CO.GetCondAmount("StatMeatFuelBonus");
		this.CO.ZeroCondAmount("StatMeatFuelBonus");
		this._fFuel -= growth + condAmount;
		if (this._fFuel < 0.0)
		{
			this._fFuel += this._fSpreadThreshold;
			this.bCanSpread = true;
		}
		this.CO.SetCondAmount("StatMeatFuel", this._fFuel, 0.0);
	}

	public void SpreadFast(int nTiles)
	{
		if (this.CO == null)
		{
			return;
		}
		Debug.Log("Spreading meat fast! " + nTiles);
		Tile tileAtWorldCoords = this.CO.ship.GetTileAtWorldCoords1(this.CO.tf.position.x, this.CO.tf.position.y, true, true);
		List<Tile> list = TileUtils.GetFloodTilesAround(tileAtWorldCoords, this.CO.ship.aTiles.Count, this.TriggerSpread);
		list = list.GetRange(0, Mathf.Min(nTiles, list.Count));
		Debug.Log("Found tiles: " + list.Count);
		Tuple<Vector2, Vector2> airlockBounds = TileUtils.GetAirlockBounds(this.CO.ship);
		foreach (Tile tile in list)
		{
			if (TileUtils.IsTileAboveAirlock(tile, airlockBounds))
			{
				List<CondOwner> list2 = new List<CondOwner>();
				list2.AddRange(this.LootMeatRand.GetCOLoot(null, false, null));
				foreach (CondOwner condOwner in list2)
				{
					condOwner.tf.position = new Vector3(tile.tf.position.x, tile.tf.position.y, -2.5f);
					this.CO.ship.AddCO(condOwner, true);
				}
			}
		}
	}

	public double Fuel
	{
		get
		{
			return this._fFuel;
		}
		set
		{
			this._fFuel = value;
		}
	}

	public CondOwner CO
	{
		get
		{
			if (this.co == null)
			{
				this.co = base.GetComponent<CondOwner>();
			}
			return this.co;
		}
	}

	public Loot LootMeatRand
	{
		get
		{
			if (Meat._lootMeatRand == null)
			{
				Meat._lootMeatRand = DataHandler.GetLoot("ItmMeatRand");
			}
			return Meat._lootMeatRand;
		}
	}

	public CondTrigger TriggerSpread
	{
		get
		{
			if (Meat._triggerSpread == null)
			{
				Meat._triggerSpread = DataHandler.GetCondTrigger("CTPLOT_Meat_Spreadable");
			}
			return Meat._triggerSpread;
		}
	}

	public CondTrigger TriggerFuel
	{
		get
		{
			if (Meat._triggerFuel == null)
			{
				Meat._triggerFuel = DataHandler.GetCondTrigger("CTPLOT_Meat_Fuelable");
			}
			return Meat._triggerFuel;
		}
	}

	public CondTrigger TriggerFight
	{
		get
		{
			if (Meat._triggerFight == null)
			{
				Meat._triggerFight = DataHandler.GetCondTrigger("CTPLOT_Meat_Attackable");
			}
			return Meat._triggerFight;
		}
	}

	private CondOwner co;

	private double fTimeOfLastSignalCheck;

	private double fTimeOfNextSignalCheck;

	public bool bNeedsCheck = true;

	public bool bCanSpread;

	public static double _fStandardGrowthRate = 0.009999999776482582;

	private double _fSpreadThreshold = 1.0;

	public bool bTesting;

	[SerializeField]
	private double _fFuel;

	public static Loot _lootMeatRand;

	public static CondTrigger _triggerSpread;

	public static CondTrigger _triggerFuel;

	public static CondTrigger _triggerFight;
}
