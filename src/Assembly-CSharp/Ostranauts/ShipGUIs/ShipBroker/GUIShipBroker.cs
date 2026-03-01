using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.Events.DTOs;
using Ostranauts.Ships;
using Ostranauts.Tools.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class GUIShipBroker : GUIData
	{
		private new void Awake()
		{
			base.Awake();
			this.srUsedShips.gameObject.SetActive(true);
			this.srRealEstates.gameObject.SetActive(false);
			this.srDerelicts.gameObject.SetActive(false);
			this.srSell.gameObject.SetActive(false);
			this.chkBuyShips.onValueChanged.AddListener(delegate(bool isOn)
			{
				this.ToggleVisibility(isOn, this.srUsedShips);
			});
			this.chkDerelicts.onValueChanged.AddListener(delegate(bool isOn)
			{
				this.ToggleVisibility(isOn, this.srDerelicts);
			});
			this.chkSell.onValueChanged.AddListener(delegate(bool isOn)
			{
				this.ToggleVisibility(isOn, this.srSell);
			});
			this.chkBuyRealEstate.onValueChanged.AddListener(delegate(bool isOn)
			{
				this.ToggleVisibility(isOn, this.srRealEstates);
			});
			this.chkBuyShips.isOn = true;
			GUIShipBroker.OnTradeShip.AddListener(new UnityAction<ShipPurchaseDTO>(this.OnTransactionButtonPressed));
			this.bPausesGame = true;
		}

		private void OnDestroy()
		{
			GUIShipBroker.OnTradeShip.RemoveListener(new UnityAction<ShipPurchaseDTO>(this.OnTransactionButtonPressed));
			if (this._specialOfferShip != null && CrewSim.system.GetShipOwner(this._specialOfferShip.strRegID) != this._coUser.strID)
			{
				this._specialOfferShip.Destroy(false);
				this._specialOfferShip = null;
			}
		}

		private void OnPurchaseConfirm(ShipPurchaseDTO shipDto)
		{
			bool flag = false;
			if (shipDto is ApartmentDTO)
			{
				flag = true;
				Ship ship = CrewSim.system.SpawnShip(shipDto.ShipName, shipDto.RegId, Ship.Loaded.Shallow, Ship.Damage.Used, this._coUser.strID, 100, true);
				ship.publicName = this.COSelf.ship.publicName + " | " + ship.json.designation;
				ship.HideFromSystem = true;
				ship.objSS.vPosx = this.COSelf.ship.objSS.vPos.X;
				ship.objSS.vPosy = this.COSelf.ship.objSS.vPos.Y;
				BodyOrbit bo = CrewSim.system.GetBO(this.COSelf.ship.objSS.strBOPORShip);
				ship.objSS.LockToBO(bo, -1.0);
				ship.objSS.bIsBO = true;
				ship.ToggleVis(false, true);
				this.UpdateResidenceConds(false);
			}
			CrewSim.system.RegisterShipOwner(shipDto.RegId, this._coUser.strID);
			this._coUser.ClaimShip(shipDto.RegId);
			Ship shipByRegID = CrewSim.system.GetShipByRegID(shipDto.RegId);
			shipByRegID.HideFromSystem = flag;
			if (shipByRegID.DMGStatus == Ship.Damage.Derelict)
			{
				shipByRegID.objSS.UnlockFromBO();
				shipByRegID.DMGStatus = Ship.Damage.Used;
				shipByRegID.bBreakInUsed = true;
			}
			else if (shipByRegID.DMGStatus != Ship.Damage.Derelict && !flag)
			{
				Ship ship2 = this.COSelf.ship;
				if (ship2.IsStationHidden(false))
				{
					Ship nearestStation = CrewSim.system.GetNearestStation(ship2.objSS.vPosx, ship2.objSS.vPosy, false);
					if (nearestStation != null)
					{
						ship2 = nearestStation;
					}
				}
				if (ship2.CanBeDockedWith())
				{
					if (ship2.LoadState <= Ship.Loaded.Shallow)
					{
						shipByRegID.Dock(ship2, false);
					}
					else
					{
						CrewSim.DockShip(ship2, shipByRegID.strRegID);
					}
				}
				else
				{
					shipByRegID.objSS.vPosx = ship2.objSS.vPosx;
					shipByRegID.objSS.vPosy = ship2.objSS.vPosy;
					Vector2 a = MathUtils.GetPushbackVector(shipByRegID, ship2);
					a *= 20f;
					shipByRegID.objSS.vPosx = ship2.objSS.vPosx + (double)(a.x / 149597870f);
					shipByRegID.objSS.vPosy = ship2.objSS.vPosy + (double)(a.y / 149597870f);
					CrewSim.system.SetSituToRandomSafeCoords(shipByRegID.objSS, 2.005376119253889E-08, 3.3422935320898144E-08, shipByRegID.objSS.vPosx, shipByRegID.objSS.vPosy, MathUtils.RandType.Low);
					shipByRegID.objSS.vVelX = ship2.objSS.vVelX;
					shipByRegID.objSS.vVelY = ship2.objSS.vVelY;
					shipByRegID.objSS.vAccEx = Vector2.zero;
					ShipSitu objSS = shipByRegID.objSS;
					bool includePlaceHolder = !ship2.IsGroundStation();
					objSS.LockToBO(-1.0, includePlaceHolder);
				}
			}
			shipByRegID.json = shipByRegID.GetJSON(shipByRegID.strRegID, false, null);
			this.UpdateCash(shipDto);
			this.RemoveListEntry(shipDto);
			this.UpdateShipLists(shipDto);
		}

		private void OnSellConfirm(ShipPurchaseDTO shipDto)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(shipDto.RegId);
			bool flag = false;
			if (shipByRegID != null)
			{
				shipByRegID.HideFromSystem = true;
				flag = shipByRegID.objSS.bIsBO;
				if (shipByRegID.IsDockedFull())
				{
					List<Ship> allDockedShipsFull = shipByRegID.GetAllDockedShipsFull();
					CrewSim.UndockShip(allDockedShipsFull.First<Ship>(), shipByRegID, true, false);
				}
			}
			CrewSim.system.RegisterShipOwner(shipDto.RegId, this.COSelf.strID);
			this._coUser.UnclaimShip(shipDto.RegId);
			MonoSingleton<AsyncShipLoader>.Instance.Unload(shipDto.RegId);
			this.UpdateCash(shipDto);
			this.RemoveListEntry(shipDto);
			if (flag)
			{
				Ship shipByRegID2 = CrewSim.system.GetShipByRegID(shipDto.RegId);
				shipByRegID2.Destroy(false);
				this.UpdateResidenceConds(true);
			}
			else
			{
				this.UpdateShipLists(shipDto);
			}
		}

		private void OnTransactionButtonPressed(ShipPurchaseDTO shipDto)
		{
			if (shipDto == null)
			{
				return;
			}
			if (shipDto.TransactionType != TransactionTypes.Sell)
			{
				shipDto.Callback = delegate()
				{
					this.OnPurchaseConfirm(shipDto);
				};
				ConfirmBuyShipPopup confirmBuyShipPopup = UnityEngine.Object.Instantiate<ConfirmBuyShipPopup>(this._prefabPopupConfirmBuyShipPopup, base.transform);
				confirmBuyShipPopup.ShowPanel(shipDto, this._coUser.GetCondAmount(Ledger.CURRENCY));
			}
			else
			{
				shipDto.Callback = delegate()
				{
					this.OnSellConfirm(shipDto);
				};
				ConfirmSellShipPopup confirmSellShipPopup = UnityEngine.Object.Instantiate<ConfirmSellShipPopup>(this.prefabPopupConfirmSellShip, base.transform);
				confirmSellShipPopup.ShowPanel(shipDto, this._coUser.GetCondAmount(Ledger.CURRENCY));
			}
		}

		private void ToggleVisibility(bool isOn, ScrollRect sr)
		{
			sr.gameObject.SetActive(isOn);
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			this._coUser = coSelf.GetInteractionCurrent().objThem;
			this._trader = coSelf.GetComponent<Trader>();
			this.SetTrade();
		}

		private float GetVendorTransactionPriceModifier(bool vendorBuys)
		{
			string strName = (!vendorBuys) ? "DiscountSell" : "DiscountBuy";
			float num = (float)this.COSelf.GetCondAmount(strName);
			if (num == 0f)
			{
				num = 1f;
			}
			return num;
		}

		private void SetTrade()
		{
			this.txtUpdate.text = ((!(this._trader != null)) ? string.Empty : this._trader.GetPriceUpdateInfo());
			this.SetupApartments();
			this.SetupVendorShips();
			this.SetupDerelicts();
			this.SetupPlayerShips();
			this.SetStartingPage();
		}

		private void SetStartingPage()
		{
			if (this.chkBuyRealEstate.gameObject.activeSelf)
			{
				this.chkBuyRealEstate.isOn = true;
				this.ToggleVisibility(true, this.srRealEstates);
			}
			else if (this.chkBuyShips.gameObject.activeSelf)
			{
				this.chkBuyShips.isOn = true;
				this.ToggleVisibility(true, this.srUsedShips);
			}
			else if (this.chkDerelicts.gameObject.activeSelf)
			{
				this.chkDerelicts.isOn = true;
				this.ToggleVisibility(true, this.srUsedShips);
			}
			else
			{
				this.chkSell.isOn = true;
				this.ToggleVisibility(true, this.srSell);
			}
		}

		private void SetupApartments()
		{
			float vendorTransactionPriceModifier = this.GetVendorTransactionPriceModifier(false);
			List<string> shipLootByType = this._trader.GetShipLootByType("station");
			if (shipLootByType == null || shipLootByType.Count == 0)
			{
				this.chkBuyRealEstate.gameObject.SetActive(false);
				this.ToggleVisibility(false, this.srRealEstates);
				return;
			}
			string str = "|RES_";
			int num = 1;
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIs" + this.COSelf.ship.strRegID + "StrataLegal");
			bool allowedToBuy = condTrigger == null || condTrigger.Triggered(this._coUser, null, true);
			foreach (string text in shipLootByType)
			{
				if (text != null)
				{
					JsonShip jsonShip = DataHandler.GetShip(text).Clone();
					string text2 = this.COSelf.ship.strRegID + str;
					while (CrewSim.system.GetShipByRegID(this.COSelf.ship.strRegID + str + num.ToString()) != null)
					{
						num++;
					}
					text2 += num.ToString();
					jsonShip.strRegID = text2;
					num++;
					double num2 = 0.0;
					if (jsonShip.aRooms != null)
					{
						foreach (JsonRoom jsonRoom in jsonShip.aRooms)
						{
							num2 += jsonRoom.roomValue;
						}
					}
					jsonShip.publicName = jsonShip.designation;
					UsedShipListEntry usedShipListEntry = UnityEngine.Object.Instantiate<UsedShipListEntry>(this._usedShipListEntryPrefab, this.srRealEstates.content);
					usedShipListEntry.SetData(jsonShip, (float)num2 * vendorTransactionPriceModifier * 10f, allowedToBuy);
					this._shipDict[text2] = usedShipListEntry.gameObject;
				}
			}
		}

		private void UpdateResidenceConds(bool remove)
		{
			if (this._trader == null || string.IsNullOrEmpty(this._trader.strLootResidence))
			{
				return;
			}
			Loot loot = DataHandler.GetLoot(this._trader.strLootResidence);
			loot.ApplyCondLoot(this._coUser, (!remove) ? 1f : -1f, null, 0f);
		}

		private void SetupPlayerShips()
		{
			float vendorTransactionPriceModifier = this.GetVendorTransactionPriceModifier(true);
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(this._coUser.strID);
			foreach (string regId in shipsForOwner)
			{
				this.AddSellableShip(regId, vendorTransactionPriceModifier);
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(this.srSell.GetComponent<RectTransform>());
		}

		private void AddSellableShip(string regId, float priceModifier)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(regId);
			if (shipByRegID == null)
			{
				return;
			}
			SellShipEntry sellShipEntry = UnityEngine.Object.Instantiate<SellShipEntry>(this._sellShipEntryPrefab, this.srSell.content);
			double quotedPrice = this.GetQuotedPrice(shipByRegID, 1f);
			sellShipEntry.SetData(shipByRegID, (float)quotedPrice * priceModifier);
			this._shipDict[regId] = sellShipEntry.gameObject;
		}

		private void SetupDerelicts()
		{
			float vendorTransactionPriceModifier = this.GetVendorTransactionPriceModifier(false);
			List<Ship> list = this.GetDerelicts(this.COSelf.ship.strRegID);
			list = this.OrderByVisit(list);
			foreach (Ship ship in list)
			{
				if (ship.fLastVisit <= 0.0)
				{
					double num = this.COSelf.ship.objSS.GetDistance(ship.objSS) * 149597872.0;
					if (num > 120.0)
					{
						continue;
					}
				}
				int randomNr = DerelictShipEntry.HashIdIntoNumber(ship.strRegID);
				float randomPriceModifier = DerelictShipEntry.GetRandomPriceModifier(randomNr);
				double quotedPrice = this.GetQuotedPrice(ship, randomPriceModifier);
				DerelictShipEntry derelictShipEntry = UnityEngine.Object.Instantiate<DerelictShipEntry>(this._derelictShipListEntryPrefab, this.srDerelicts.content);
				derelictShipEntry.SetData(ship, (float)quotedPrice * vendorTransactionPriceModifier);
				this._shipDict[ship.strRegID] = derelictShipEntry.gameObject;
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsBrokerHidesDerelicts");
			if (list.Count == 0 || condTrigger.Triggered(this.COSelf, null, true))
			{
				this.chkDerelicts.gameObject.SetActive(false);
				this.ToggleVisibility(false, this.srDerelicts);
			}
		}

		private double GetQuotedPrice(Ship ship, float fSetPriceModifier)
		{
			double num = 0.0;
			string propMapData = base.GetPropMapData(ship.strRegID);
			if (double.TryParse(propMapData, out num))
			{
				ship.fLastQuotedPrice = num;
				base.SetPropMapData(ship.strRegID, null);
			}
			else
			{
				if (ship.fLastQuotedPrice > 0.0)
				{
					num = ship.fLastQuotedPrice;
				}
				else
				{
					float num2 = 1f;
					if (ship.IsDerelict())
					{
						num2 = 1.1f - ship.fBreakInMultiplier;
						if (num2 <= 0f)
						{
							num2 = 0.1f;
						}
					}
					num = ship.GetShipValue() * (double)num2 * (double)fSetPriceModifier;
				}
				ship.fLastQuotedPrice = num;
			}
			return num;
		}

		private void SetupVendorShips()
		{
			float vendorTransactionPriceModifier = this.GetVendorTransactionPriceModifier(false);
			Dictionary<string, Dictionary<string, Texture2D>> dictionary = new Dictionary<string, Dictionary<string, Texture2D>>();
			this.AddSpecialOfferShip(vendorTransactionPriceModifier);
			List<Ship> vendorShips = this.GetVendorShips(this.COSelf.strID);
			if (vendorShips == null || vendorShips.Count == 0)
			{
				this.chkBuyShips.gameObject.SetActive(false);
				this.ToggleVisibility(false, this.srUsedShips);
				return;
			}
			foreach (Ship ship in vendorShips)
			{
				if (ship != null)
				{
					this.AddPurchasableShip(ship, vendorTransactionPriceModifier, ref dictionary);
				}
			}
		}

		private void AddPurchasableShip(Ship ship, float priceModifier, ref Dictionary<string, Dictionary<string, Texture2D>> imgDict)
		{
			UsedShipListEntry usedShipListEntry = UnityEngine.Object.Instantiate<UsedShipListEntry>(this._usedShipListEntryPrefab, this.srUsedShips.content);
			Dictionary<string, Texture2D> loadedImages;
			if (imgDict != null && imgDict.TryGetValue(ship.json.strName, out loadedImages))
			{
				usedShipListEntry.SetData(ship, (float)ship.GetShipValue() * priceModifier, loadedImages);
			}
			else
			{
				Dictionary<string, Texture2D> value = usedShipListEntry.SetData(ship, (float)ship.GetShipValue() * priceModifier, null);
				imgDict.TryAdd(ship.json.strName, value);
			}
			this._shipDict[ship.strRegID] = usedShipListEntry.gameObject;
		}

		private void UpdateShipLists(ShipPurchaseDTO shipDTO)
		{
			if (shipDTO.TransactionType != TransactionTypes.Sell)
			{
				this.AddSellableShip(shipDTO.RegId, this.GetVendorTransactionPriceModifier(true));
			}
			else
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(shipDTO.RegId);
				Dictionary<string, Dictionary<string, Texture2D>> dictionary = new Dictionary<string, Dictionary<string, Texture2D>>();
				this.AddPurchasableShip(shipByRegID, this.GetVendorTransactionPriceModifier(false), ref dictionary);
			}
		}

		private void AddSpecialOfferShip(float priceModifier)
		{
			List<string> lootNames = DataHandler.GetLoot("RandomShipBrokerSpecialOffer" + this.COSelf.ship.strRegID).GetLootNames(null, false, null);
			if (lootNames == null || lootNames.Count == 0)
			{
				lootNames = DataHandler.GetLoot("RandomShipBrokerSpecialOffer").GetLootNames(null, false, null);
			}
			if (lootNames == null || lootNames.Count == 0)
			{
				return;
			}
			JsonShip ship = DataHandler.GetShip(lootNames[0]);
			Ship ship2 = CrewSim.system.SpawnShip(ship.strName, null, Ship.Loaded.Shallow, Ship.Damage.New, "vendor-temp", 100, false);
			ship2.HideFromSystem = true;
			ship2.gameObject.SetActive(true);
			UsedShipListEntry usedShipListEntry = UnityEngine.Object.Instantiate<UsedShipListEntry>(this._usedShipListEntryPrefab, this.srUsedShips.content);
			usedShipListEntry.transform.SetAsFirstSibling();
			float price = (float)ship2.GetShipValue() * priceModifier;
			usedShipListEntry.SetSpecialOfferData(ship2, price);
			usedShipListEntry.gameObject.SetActive(CrewSim.system.GetShipsForOwner(this._coUser.strID).Count == 0);
			this._specialOfferShip = ship2;
			this._shipDict[ship2.strRegID] = usedShipListEntry.gameObject;
		}

		private List<Ship> GetVendorShips(string shipOwnerId)
		{
			List<Ship> list = new List<Ship>();
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(shipOwnerId);
			if (shipsForOwner != null)
			{
				foreach (string strRegID in shipsForOwner)
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
					if (shipByRegID != null)
					{
						list.Add(shipByRegID);
					}
				}
			}
			return list;
		}

		private List<Ship> GetDerelicts(string shipOwnerId)
		{
			List<Ship> list = new List<Ship>();
			string shipOwner = CrewSim.system.GetShipOwner(shipOwnerId);
			foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
			{
				if (!keyValuePair.Value.IsStation(false) && !keyValuePair.Value.IsStationHidden(false) && keyValuePair.Value.DMGStatus == Ship.Damage.Derelict)
				{
					string shipOwner2 = CrewSim.system.GetShipOwner(keyValuePair.Key);
					if (shipOwner2 == shipOwner)
					{
						list.Add(keyValuePair.Value);
					}
				}
			}
			return list;
		}

		private List<Ship> OrderByVisit(List<Ship> shipList)
		{
			if (shipList == null)
			{
				return new List<Ship>();
			}
			List<Ship> list = (from o in shipList
			where o.fLastVisit == 0.0
			select o).ToList<Ship>();
			foreach (Ship item in list)
			{
				shipList.Remove(item);
			}
			shipList = (from x in shipList
			orderby x.fLastVisit descending
			select x).ToList<Ship>();
			shipList.AddRange(list);
			return shipList;
		}

		private void UpdateCash(ShipPurchaseDTO shipDto)
		{
			double num = shipDto.TransactionPrice;
			if (shipDto.TransactionType == TransactionTypes.Sell)
			{
				LedgerLI mortgageForShip = Ledger.GetMortgageForShip(shipDto.RegId);
				if (mortgageForShip != null)
				{
					num = (double)Ledger.AddDownPayment(this._coUser, shipDto.RegId, (float)num);
				}
				if (num != 0.0)
				{
					this._coUser.AddCondAmount(Ledger.CURRENCY, num, 0.0, 0f);
					this.COSelf.AddCondAmount(Ledger.CURRENCY, -num, 0.0, 0f);
					Ledger.UpdateLedger(this._coUser, this.COSelf.FriendlyName, num, "Ship Sale: " + shipDto.RegId);
				}
			}
			else
			{
				this._coUser.AddCondAmount(Ledger.CURRENCY, -num, 0.0, 0f);
				this.COSelf.AddCondAmount(Ledger.CURRENCY, num, 0.0, 0f);
				Ledger.UpdateLedger(this._coUser, this.COSelf.FriendlyName, -num, "Ship Purchase: " + shipDto.RegId);
				if (shipDto.TransactionType == TransactionTypes.Mortgage)
				{
					LedgerLI li = new LedgerLI(this.COSelf.FriendlyName, this._coUser.strID, (float)(shipDto.ShipValue - num), DataHandler.GetString("GUI_FINANCE_MORTGAGE01", false) + shipDto.RegId, "$", StarSystem.fEpoch, false, LedgerLI.Frequency.Mortgage);
					Ledger.AddLI(li);
				}
			}
		}

		private void RemoveListEntry(ShipPurchaseDTO shipDto)
		{
			GameObject gameObject;
			if (this._shipDict.TryGetValue(shipDto.RegId, out gameObject))
			{
				ScrollRect componentInParent = gameObject.GetComponentInParent<ScrollRect>();
				UnityEngine.Object.Destroy(gameObject);
				if (componentInParent != null)
				{
					LayoutRebuilder.MarkLayoutForRebuild(componentInParent.GetComponent<RectTransform>());
				}
			}
			if (this._specialOfferShip != null && this._specialOfferShip.strRegID == shipDto.RegId)
			{
				this._specialOfferShip = null;
			}
			if (CrewSim.system.GetShipsForOwner(this._coUser.strID).Count == 0)
			{
				this.ShowHiddenShips();
			}
		}

		private void ShowHiddenShips()
		{
			foreach (KeyValuePair<string, GameObject> keyValuePair in this._shipDict)
			{
				if (!(keyValuePair.Value == null) && !keyValuePair.Value.activeSelf)
				{
					keyValuePair.Value.SetActive(true);
					ScrollRect componentInParent = keyValuePair.Value.GetComponentInParent<ScrollRect>();
					if (componentInParent != null)
					{
						LayoutRebuilder.MarkLayoutForRebuild(componentInParent.GetComponent<RectTransform>());
					}
				}
			}
		}

		public override void SaveAndClose()
		{
			base.StopAllCoroutines();
			GUIShipBroker.OnCloseMenu.Invoke();
			GUIShipBroker.OnTradeShip.RemoveListener(new UnityAction<ShipPurchaseDTO>(this.OnTransactionButtonPressed));
			base.SaveAndClose();
		}

		public static readonly OnLoadBrokerShipEvent OnVisitShip = new OnLoadBrokerShipEvent();

		public static readonly OnTradeShipevent OnTradeShip = new OnTradeShipevent();

		public static readonly UnityEvent OnCloseMenu = new UnityEvent();

		[Header("Scrollrects")]
		[SerializeField]
		private ScrollRect srRealEstates;

		[SerializeField]
		private ScrollRect srUsedShips;

		[SerializeField]
		private ScrollRect srDerelicts;

		[SerializeField]
		private ScrollRect srSell;

		[Header("Toggles")]
		[SerializeField]
		private Toggle chkBuyRealEstate;

		[SerializeField]
		private Toggle chkBuyShips;

		[SerializeField]
		private Toggle chkDerelicts;

		[SerializeField]
		private Toggle chkSell;

		[SerializeField]
		private TMP_Text txtUpdate;

		[Header("Prefabs")]
		[SerializeField]
		private UsedShipListEntry _usedShipListEntryPrefab;

		[SerializeField]
		private DerelictShipEntry _derelictShipListEntryPrefab;

		[SerializeField]
		private SellShipEntry _sellShipEntryPrefab;

		[Header("Purchase Popup")]
		[SerializeField]
		private ConfirmSellShipPopup prefabPopupConfirmSellShip;

		[SerializeField]
		private ConfirmBuyShipPopup _prefabPopupConfirmBuyShipPopup;

		private CondOwner _coUser;

		private Trader _trader;

		private readonly Dictionary<string, GameObject> _shipDict = new Dictionary<string, GameObject>();

		private Ship _specialOfferShip;
	}
}
