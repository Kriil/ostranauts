using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Ships;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Trade
{
	public class GUITradeBase : GUIData
	{
		protected override void Awake()
		{
			base.Awake();
		}

		protected void SetShipDropDown()
		{
			if (this.ddShipSelection == null)
			{
				return;
			}
			List<string> ownedDockedShips = Ship.GetOwnedDockedShips(this._coUser, this.COSelf.ship, true);
			List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
			foreach (string text in ownedDockedShips)
			{
				list.Add(new Dropdown.OptionData(text));
			}
			this.ddShipSelection.ClearOptions();
			this.ddShipSelection.AddOptions(list);
			if (list.Count == 0)
			{
				this.ddShipSelection.interactable = false;
				this.chkSellToZone.isOn = false;
				this.chkSellToZone.interactable = false;
			}
		}

		protected string GetSelectedBarterZoneShipId()
		{
			if (this.ddShipSelection == null || this.ddShipSelection.options.Count == 0)
			{
				return null;
			}
			return this.ddShipSelection.options[this.ddShipSelection.value].text;
		}

		protected void GetZones()
		{
			this.bZonesLoading = false;
			List<Ship> list = new List<Ship>
			{
				this.COSelf.ship
			};
			list.AddRange(this.COSelf.ship.GetAllDockedShips());
			List<string> ownedDockedShips = Ship.GetOwnedDockedShips(this._coUser, this.COSelf.ship, true);
			using (List<string>.Enumerator enumerator = ownedDockedShips.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string ownedShipregId = enumerator.Current;
					if (!list.Any((Ship x) => x.strRegID == ownedShipregId))
					{
						Ship item;
						if (MonoSingleton<AsyncShipLoader>.Instance.GetShip(ownedShipregId, out item))
						{
							list.Add(item);
						}
						else if (MonoSingleton<AsyncShipLoader>.Instance.IsShipLoading(ownedShipregId))
						{
							this.bZonesLoading = true;
						}
					}
				}
			}
			this._mapZonesSelf.Clear();
			this._mapZonesUser.Clear();
			foreach (Ship ship in list)
			{
				Ship ship2 = ship;
				if (ship2.LoadState > Ship.Loaded.Shallow || MonoSingleton<AsyncShipLoader>.Instance.GetShip(ship.strRegID, out ship2))
				{
					List<JsonZone> zones = ship2.GetZones("IsZoneBarter", this.COSelf, false, false);
					foreach (JsonZone key in zones)
					{
						this._mapZonesSelf[key] = ship2;
					}
					if (!(CrewSim.system.GetShipOwner(ship2.strRegID) != CrewSim.coPlayer.strID))
					{
						zones = ship2.GetZones("IsZoneBarter", this._coUser, false, false);
						foreach (JsonZone key2 in zones)
						{
							this._mapZonesUser[key2] = ship2;
						}
					}
				}
			}
		}

		protected List<string> BuyItems(List<string> aItemIDs, float fPrice, Dictionary<JsonZone, Ship> _mapZonesUser)
		{
			List<string> list = new List<string>();
			if (aItemIDs == null)
			{
				return list;
			}
			Trader component = this.COSelf.GetComponent<Trader>();
			List<string> list2 = new List<string>();
			foreach (string key in aItemIDs)
			{
				this.bFits = false;
				this.strItem = null;
				CondOwner condOwner = null;
				DataHandler.mapCOs.TryGetValue(key, out condOwner);
				if (condOwner == null)
				{
					this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_ITEM", false) + condOwner.FriendlyName;
					AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
					this.bFits = false;
					break;
				}
				Item component2 = condOwner.GetComponent<Item>();
				if (component2 != null)
				{
					component2.fLastRotation = 0f;
				}
				List<CondOwner> list3 = new List<CondOwner>
				{
					condOwner
				};
				List<CondOwner> list4 = new List<CondOwner>();
				if (this.strItem == null)
				{
					this.strItem = condOwner.FriendlyName;
				}
				Ship ship = condOwner.ship;
				CondOwner objCOParent = condOwner.objCOParent;
				condOwner.PopHeadFromStack();
				this.bFits = false;
				CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsDataItem");
				bool flag = condTrigger.Triggered(condOwner, null, true);
				if (this.chkSellToZone.isOn && _mapZonesUser != null && _mapZonesUser.Count != 0 && !flag)
				{
					string selectedShip = this.GetSelectedBarterZoneShipId();
					List<JsonZone> list5 = _mapZonesUser.Keys.ToList<JsonZone>();
					if (selectedShip != null)
					{
						list5.RemoveAll((JsonZone z) => z == null || z.strRegID != selectedShip);
					}
					else
					{
						list5.RemoveAll((JsonZone z) => z == null);
					}
					list5.Sort();
					foreach (JsonZone jsonZone in list5)
					{
						if (jsonZone != null)
						{
							Ship ship2 = _mapZonesUser[jsonZone];
							if (ship2 == null)
							{
								Debug.Log("GUITrade, value was null for zone: " + ((jsonZone == null) ? "Key" : jsonZone.strName));
							}
							else
							{
								int count = list3.Count;
								List<CondOwner> cosInZone = ship2.GetCOsInZone(jsonZone, GUITradeBase.ctLootSpawnOK, true, true);
								CondTrigger ctLooseFurniture = DataHandler.GetCondTrigger("TIsStorageFurnitureLoose");
								if (ctLooseFurniture != null && cosInZone.Count > 0)
								{
									cosInZone.RemoveAll((CondOwner x) => ctLooseFurniture.Triggered(x, null, true));
								}
								list4 = TileUtils.DropCOsNearby(list3, ship2, jsonZone, cosInZone, GUITradeBase.ctLootSpawnOK, false, true);
								if (list4.Count != count)
								{
									list.Add(ship2.strRegID);
								}
								if (list4.Count == 0)
								{
									this.RecordTransaction(fPrice, false);
									string text = DataHandler.GetString("GUI_TRADE_SENT_TO_ZONE", false) + jsonZone.strName;
									this._coUser.LogMessage(text, "Neutral", this._coUser.strName);
									text = DataHandler.GetString("NAV_LOG_TRADE_DELIVERED", false) + jsonZone.strName + DataHandler.GetString("NAV_LOG_TERMINATOR", false);
									if (!list2.Contains(text))
									{
										Ship shipByRegID = CrewSim.system.GetShipByRegID(ship2.strRegID);
										if (shipByRegID != null)
										{
											shipByRegID.LogAdd(text, StarSystem.fEpoch, true);
											list2.Add(text);
										}
									}
									break;
								}
								list3 = list4;
							}
						}
					}
				}
				if (!this.bFits)
				{
					list4.Clear();
					CondOwner condOwner2 = this._coUser.AddCO(condOwner, false, true, false);
					if (condOwner2 != null)
					{
						list4.Add(condOwner2);
					}
					else
					{
						this.RecordTransaction(fPrice, false);
					}
				}
				if (!this.bFits)
				{
					this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_SPACE", false) + condOwner.FriendlyName;
					TMP_Text tmp_Text = this.txtError;
					tmp_Text.text = tmp_Text.text + DataHandler.GetString("GUI_TRADE_ERROR_NO_SPACE_2", false) + this._coUser.FriendlyName;
					TMP_Text tmp_Text2 = this.txtError;
					tmp_Text2.text += DataHandler.GetString("GUI_TRADE_ERROR_NO_SPACE_3", false);
					if (this.chkSellToZone.isOn && flag)
					{
						TMP_Text tmp_Text3 = this.txtError;
						tmp_Text3.text = tmp_Text3.text + "\n" + DataHandler.GetString("GUI_TRADE_ERROR_DATAITEM", false);
					}
					AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
					foreach (CondOwner condOwner3 in list4)
					{
						if (objCOParent != null)
						{
							objCOParent.AddCO(condOwner3, false, true, true);
						}
						else if (ship != null)
						{
							ship.AddCO(condOwner3, true);
						}
					}
					break;
				}
			}
			return list;
		}

		protected virtual void RecordTransaction(float salesPrice, bool areWeSelling)
		{
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			GUITradeBase.ctLootSpawnOK = DataHandler.GetCondTrigger("TIsLootSpawnOK");
			if (GUITradeBase.ctInfinite == null)
			{
				GUITradeBase.ctInfinite = DataHandler.GetCondTrigger("TIsInfiniteContainer");
			}
			this._coUser = this.COSelf.GetInteractionCurrent().objThem;
		}

		public static readonly string ASYNCIDENTIFIER = "AsyncItem";

		[SerializeField]
		public Toggle chkSellToZone;

		[SerializeField]
		protected Dropdown ddShipSelection;

		[SerializeField]
		protected TMP_Text txtError;

		protected CondOwner _coUser;

		protected Dictionary<JsonZone, Ship> _mapZonesSelf = new Dictionary<JsonZone, Ship>();

		protected Dictionary<JsonZone, Ship> _mapZonesUser = new Dictionary<JsonZone, Ship>();

		private static CondTrigger ctLootSpawnOK;

		public static CondTrigger ctInfinite;

		protected string strItem;

		protected bool bFits;

		protected bool bUpdateFunds;

		protected bool bZonesLoading;
	}
}
