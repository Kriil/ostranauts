using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.ShipGUIs.Trade;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using UnityEngine;

public class Trader : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.COSelf = base.GetComponent<CondOwner>();
	}

	private void Update()
	{
		if (StarSystem.fEpoch - this.fTimeOfLastRestockCheck < 3.0 || this.COSelf == null || this.COSelf.ship == null || CrewSim.system == null)
		{
			return;
		}
		if (!this.bInit)
		{
			this.Init();
			return;
		}
		if (this.COSelf.HasCond(this.strRestockCond) || this.COSelf.HasCond("ForceRestock"))
		{
			this.Restock();
		}
		if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("IsDueReactorPart"))
		{
			this.BonusReactorPart();
		}
		this.fTimeOfLastRestockCheck = StarSystem.fEpoch;
	}

	private bool EvaluateReacorSpawn()
	{
		if (this.COSelf == null || this.COSelf.HasCond("IsNotTradingReactorParts") || this.CTSell == null || this.CTSell.IsBlank())
		{
			return false;
		}
		CondOwner coPlayer = CrewSim.coPlayer;
		return !(coPlayer == null) && coPlayer.ship != null;
	}

	private int AddReactorPartToInventory(List<string> lootCandidates)
	{
		if (lootCandidates.Count == 0)
		{
			return -1;
		}
		string strName = lootCandidates.Randomize<string>().First<string>();
		Loot loot = DataHandler.GetLoot(strName);
		List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
		foreach (CondOwner condOwner in coloot)
		{
			if (!(condOwner == null))
			{
				if (this.CTSell == null || this.CTSell.IsBlank() || !this.CTSell.Triggered(condOwner, null, true))
				{
					foreach (CondOwner condOwner2 in coloot)
					{
						condOwner2.Destroy();
					}
					return 0;
				}
				List<CondOwner> inventory = this.GetInventory();
				foreach (CondOwner condOwner3 in inventory)
				{
					if (!(condOwner3 == null) && !(condOwner3.strName != condOwner.strName))
					{
						foreach (CondOwner condOwner4 in coloot)
						{
							condOwner4.Destroy();
						}
						return 1;
					}
				}
			}
		}
		this.AddNewItems(coloot);
		return 1;
	}

	private int AddNewItems(List<CondOwner> aCOs)
	{
		if (aCOs == null)
		{
			return 0;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawnOK");
		int num = 0;
		foreach (CondOwner condOwner in aCOs)
		{
			if (!(condOwner == null))
			{
				bool flag = false;
				Item component = condOwner.GetComponent<Item>();
				if (component != null)
				{
					component.fLastRotation = 0f;
				}
				List<CondOwner> aCOs2 = new List<CondOwner>
				{
					condOwner
				};
				List<CondOwner> list = new List<CondOwner>();
				foreach (KeyValuePair<JsonZone, Ship> keyValuePair in this.mapZonesUs)
				{
					List<CondOwner> cosInZone = keyValuePair.Value.GetCOsInZone(keyValuePair.Key, condTrigger, true, true);
					list = TileUtils.DropCOsNearby(aCOs2, keyValuePair.Value, keyValuePair.Key, cosInZone, condTrigger, true, false);
					if (list.Count == 0)
					{
						flag = true;
						break;
					}
					aCOs2 = list;
				}
				if (!flag)
				{
					list.Clear();
					CondOwner condOwner2 = this.COSelf.AddCO(condOwner, false, true, true);
					if (condOwner2 != null)
					{
						list.Add(condOwner2);
					}
					else
					{
						flag = true;
					}
				}
				if (!flag)
				{
					num++;
					condOwner.Destroy();
				}
			}
		}
		return num;
	}

	private List<CondOwner> GetInventory()
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsBarter");
		List<CondOwner> cosSafe = this.COSelf.GetCOsSafe(true, condTrigger);
		List<Ship> list = new List<Ship>
		{
			this.COSelf.ship
		};
		list.AddRange(this.COSelf.ship.GetAllDockedShips());
		this.mapZonesUs.Clear();
		foreach (Ship ship in list)
		{
			List<JsonZone> zones = ship.GetZones("IsZoneBarter", this.COSelf, false, false);
			foreach (JsonZone key in zones)
			{
				this.mapZonesUs[key] = ship;
			}
		}
		List<CondOwner> list2 = new List<CondOwner>();
		foreach (KeyValuePair<JsonZone, Ship> keyValuePair in this.mapZonesUs)
		{
			list2 = keyValuePair.Value.GetCOsInZone(keyValuePair.Key.strName, condTrigger, true);
			foreach (CondOwner item in list2)
			{
				if (cosSafe.IndexOf(item) < 0)
				{
					cosSafe.Add(item);
				}
			}
			List<CondOwner> cosInZone = keyValuePair.Value.GetCOsInZone(keyValuePair.Key.strName, GUITradeBase.ctInfinite, true);
			foreach (CondOwner condOwner in cosInZone)
			{
				list2 = condOwner.GetCOs(true, null);
				if (list2 != null)
				{
					foreach (CondOwner condOwner2 in list2)
					{
						if (!(condOwner2.objCOParent != condOwner))
						{
							cosSafe.Add(condOwner2);
						}
					}
				}
			}
		}
		return cosSafe;
	}

	private void BonusReactorPart()
	{
		if (!this.EvaluateReacorSpawn())
		{
			return;
		}
		CondOwner coPlayer = CrewSim.coPlayer;
		Loot loot = DataHandler.GetLoot("TXTBonusFusionLooseNames");
		Loot loot2 = DataHandler.GetLoot("TXTBonusFusionCondNames");
		CondTrigger condTrigger = new CondTrigger();
		condTrigger.strName = loot2.strName;
		condTrigger.aReqs = loot2.GetLootNames(null, false, null).ToArray();
		condTrigger.bAND = false;
		string[] array = loot.GetLootNames(null, false, null).ToArray();
		List<CondOwner> list = new List<CondOwner>();
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(coPlayer.strName);
		foreach (string strRegID in shipsForOwner)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
			if (shipByRegID != null && !shipByRegID.bDestroyed && shipByRegID.LoadState > Ship.Loaded.Shallow)
			{
				if (shipByRegID.bFusionReactorRunning)
				{
					coPlayer.ZeroCondAmount("IsDueReactorPart");
					return;
				}
				list.AddRange(shipByRegID.GetCOs(condTrigger, true, false, true));
			}
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int num = 0;
		while (num < array.Length && num < condTrigger.aReqs.Length)
		{
			dictionary[condTrigger.aReqs[num]] = array[num];
			num++;
		}
		if ((float)list.Count < (float)dictionary.Count * 0.62f)
		{
			coPlayer.ZeroCondAmount("IsDueReactorPart");
			return;
		}
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, string> keyValuePair in dictionary)
		{
			string key = keyValuePair.Key;
			bool flag = false;
			foreach (CondOwner condOwner in list)
			{
				if (condOwner.HasCond(key))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list2.Add(keyValuePair.Value);
			}
		}
		if (this.AddReactorPartToInventory(list2) == 0)
		{
			this.COSelf.AddCondAmount("IsNotTradingReactorParts", 1.0, 0.0, 0f);
			return;
		}
		coPlayer.ZeroCondAmount("IsDueReactorPart");
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
	}

	public double GetLastUpdateTime()
	{
		return this.fTimeOfLastRestock;
	}

	public double GetNextUpdateTime()
	{
		Dictionary<string, string> dictionary = null;
		if (this.COSelf != null && this.COSelf.mapGUIPropMaps != null && this.COSelf.mapGUIPropMaps.TryGetValue("Trader", out dictionary))
		{
			string strTicker = null;
			dictionary.TryGetValue("strTicker", out strTicker);
			double tickerTimeleft = this.COSelf.GetTickerTimeleft(strTicker);
			if (tickerTimeleft >= 0.0)
			{
				return tickerTimeleft;
			}
		}
		return 0.0;
	}

	private void Init()
	{
		Dictionary<string, string> dictionary = null;
		if (this.COSelf.mapGUIPropMaps.TryGetValue("Trader", out dictionary))
		{
			this.bInit = this.UpdateGPMs();
		}
	}

	public bool UpdateGPMs()
	{
		bool result = true;
		Dictionary<string, string> dictionary = null;
		if (this.COSelf.mapGUIPropMaps.TryGetValue("Trader", out dictionary))
		{
			if (!dictionary.TryGetValue("strLoot", out this.strLoot))
			{
				result = false;
			}
			if (!dictionary.TryGetValue("strRestockCond", out this.strRestockCond))
			{
				result = false;
			}
			dictionary.TryGetValue("strLootCTsBuy", out this.strLootCTsBuy);
			dictionary.TryGetValue("strLootCTsSell", out this.strLootCTsSell);
			string strName = null;
			dictionary.TryGetValue("strCTBuyLast", out strName);
			this.ctBuy = DataHandler.GetCondTrigger(strName);
			dictionary.TryGetValue("strCTSellLast", out strName);
			this.ctSell = DataHandler.GetCondTrigger(strName);
			dictionary.TryGetValue("strLootResidence", out this.strLootResidence);
			dictionary.TryGetValue("strLootBuyDiscount", out this.strLootBuyDiscount);
			dictionary.TryGetValue("strLootSellDiscount", out this.strLootSellDiscount);
			string strName2 = null;
			dictionary.TryGetValue("strTicker", out strName2);
			JsonTicker ticker = DataHandler.GetTicker(strName2);
			if (ticker != null)
			{
				this.COSelf.AddTicker(ticker);
			}
			if (!this.COSelf.HasCond("DiscountBuy") || !this.COSelf.HasCond("DiscountSell"))
			{
				this.UpdateDiscounts();
			}
		}
		return result;
	}

	private void UpdateDiscounts()
	{
		Dictionary<string, string> dictionary = null;
		if (this.COSelf.mapGUIPropMaps.TryGetValue("Trader", out dictionary))
		{
			dictionary.TryGetValue("strLootBuyDiscount", out this.strLootBuyDiscount);
			dictionary.TryGetValue("strLootSellDiscount", out this.strLootSellDiscount);
		}
		this.COSelf.ZeroCondAmount("DiscountBuy");
		this.COSelf.ZeroCondAmount("DiscountSell");
		Loot loot = DataHandler.GetLoot(this.strLootBuyDiscount);
		loot.ApplyCondLoot(this.COSelf, 1f, null, 0f);
		loot = DataHandler.GetLoot(this.strLootSellDiscount);
		loot.ApplyCondLoot(this.COSelf, 1f, null, 0f);
		if (!this.COSelf.HasCond("DiscountBuy"))
		{
			this.COSelf.SetCondAmount("DiscountBuy", 1.0, 0.0);
		}
		if (!this.COSelf.HasCond("DiscountSell"))
		{
			this.COSelf.SetCondAmount("DiscountSell", 1.0, 0.0);
		}
	}

	private void Restock()
	{
		this.CancelCurrentInteraction();
		this.ctBuy = this.UpdateCT(this.strLootCTsBuy, "strCTBuyLast");
		this.ctSell = this.UpdateCT(this.strLootCTsSell, "strCTSellLast");
		this.UpdateDiscounts();
		this.RemoveCurrentItems(null, 0);
		this.RemoveOwnedShips();
		this.AddNewItems(this.strLoot);
		this.AddNewShips();
		this.COSelf.SetCondAmount(this.strRestockCond, 0.0, 0.0);
		this.COSelf.ZeroCondAmount("ForceRestock");
		this.fTimeOfLastRestock = StarSystem.fEpoch;
		Debug.Log(string.Concat(new string[]
		{
			"#Info# Restocked ",
			this.COSelf.strID,
			" with ",
			this.strLoot,
			" at ",
			StarSystem.sUTCEpoch
		}));
	}

	private int AddNewItems(string cosToSpawn)
	{
		Loot loot = DataHandler.GetLoot(cosToSpawn);
		List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
		return this.AddNewItems(coloot);
	}

	private void RemoveCurrentItems(string itemToRemove = null, int count = 0)
	{
		int num = (count == 0) ? 10000 : count;
		List<CondOwner> inventory = this.GetInventory();
		foreach (CondOwner condOwner in inventory)
		{
			if (condOwner.slotNow == null)
			{
				if (itemToRemove != null)
				{
					if (!(condOwner.strName != itemToRemove))
					{
						if (MarketManager.ShowDebugLogs)
						{
							Debug.Log("#Market# Destroyed item " + condOwner.strName);
						}
						condOwner.RemoveFromCurrentHome(false);
						condOwner.Destroy();
						num++;
						if (num >= 0)
						{
							break;
						}
					}
				}
				else
				{
					condOwner.RemoveFromCurrentHome(false);
					condOwner.Destroy();
				}
			}
		}
	}

	private void RemoveOwnedShips()
	{
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(this.COSelf.strID);
		if (shipsForOwner == null)
		{
			return;
		}
		foreach (string strRegID in shipsForOwner)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
			if (shipByRegID != null && (this.COSelf.ship == null || this.COSelf.ship != shipByRegID) && shipByRegID.LoadState != Ship.Loaded.Full && shipByRegID.HideFromSystem)
			{
				shipByRegID.Destroy(false);
			}
		}
	}

	private void AddNewShips()
	{
		List<string> shipLootByType = this.GetShipLootByType("ship");
		if (shipLootByType == null)
		{
			return;
		}
		foreach (string strName in shipLootByType)
		{
			JsonShip ship = DataHandler.GetShip(strName);
			if (ship != null)
			{
				Ship ship2 = CrewSim.system.SpawnShip(ship.strName, null, Ship.Loaded.Shallow, Ship.Damage.Used, this.COSelf.strID, 100, false);
				ship2.ShipCO.AddCondAmount("IsVendorShip", 1.0, 0.0, 0f);
				ship2.HideFromSystem = true;
				ship2.gameObject.SetActive(false);
			}
		}
	}

	private CondTrigger UpdateCT(string lootCT, string propkey)
	{
		List<string> lootNames = DataHandler.GetLoot(lootCT).GetLootNames(lootCT, false, null);
		CondTrigger condTrigger;
		if (lootNames.Count > 0)
		{
			condTrigger = DataHandler.GetCondTrigger(lootNames[0]);
		}
		else
		{
			condTrigger = DataHandler.GetCondTrigger(null);
		}
		this.COSelf.mapGUIPropMaps["Trader"][propkey] = condTrigger.strName;
		return condTrigger;
	}

	private void CancelCurrentInteraction()
	{
		Interaction interactionCurrent = this.COSelf.GetInteractionCurrent();
		if (interactionCurrent != null && interactionCurrent.objThem != this.COSelf)
		{
			CondOwner objThem = interactionCurrent.objThem;
			this.COSelf.AICancelAll(null);
			if (objThem != null)
			{
				objThem.LogMessage(this.COSelf.FriendlyName + DataHandler.GetString("GUI_TRADE_ERROR_RESTOCKING", false), "Neutral", this.COSelf.strName);
				objThem.AICancelAll(null);
			}
		}
	}

	public List<string> GetShipLootByType(string lootType)
	{
		Loot loot = DataHandler.GetLoot(this.strLoot);
		if (loot.strType != "ship" && loot.strType != lootType)
		{
			return null;
		}
		return loot.GetLootNames(null, false, lootType);
	}

	public string GetPriceUpdateInfo()
	{
		string @string = DataHandler.GetString("GUI_TRADE_LAST_UPDATE", false);
		string str = DataHandler.GetString("GUI_TRADE_LAST_UPDATE_NA", false);
		if (this.GetNextUpdateTime() > 0.0)
		{
			str = MathUtils.GetUTCFromS(this.GetNextUpdateTime() * 3600.0 + StarSystem.fEpoch);
		}
		return @string + str;
	}

	public void DebugRestock()
	{
		this.Restock();
	}

	public CondTrigger CTBuy
	{
		get
		{
			return this.ctBuy;
		}
	}

	public CondTrigger CTSell
	{
		get
		{
			return this.ctSell;
		}
	}

	protected CondOwner COSelf;

	private Dictionary<JsonZone, Ship> mapZonesUs = new Dictionary<JsonZone, Ship>();

	private bool bInit;

	private string strLoot;

	private string strRestockCond;

	private string strLootCTsBuy;

	private string strLootCTsSell;

	private string strLootBuyDiscount;

	private string strLootSellDiscount;

	public string strLootResidence;

	private CondTrigger ctBuy;

	private CondTrigger ctSell;

	private double fTimeOfLastRestockCheck;

	private double fTimeOfLastRestock;
}
