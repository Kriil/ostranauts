using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.ShipGUIs.Trade.Models;
using Ostranauts.Ships;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Trade
{
	// Main barter/trade terminal. Handles buy/sell mode, trade rows, unavailable
	// inventory discovery, tooltips, and transaction acceptance/reset flow.
	public class GUITrade : GUITradeBase
	{
		// The CondOwner currently feeding the dedicated trade tooltip window.
		public CondOwner CoTooltip { get; set; }

		// Initializes the base trade UI and marks this screen as pause-on-open.
		protected override void Awake()
		{
			base.Awake();
			this.bPausesGame = true;
		}

		// Wires button/toggle handlers and creates the dedicated trade tooltip.
		private void Start()
		{
			this.btnAccept.onClick.AddListener(new UnityAction(this.OnBuy));
			this.btnAccept.onClick.AddListener(new UnityAction(this.OnSell));
			this.btnCancel.onClick.AddListener(new UnityAction(this.OnCancel));
			this.btnExpandHide.onClick.AddListener(new UnityAction(this.OnExpandHide));
			this.btnReset.onClick.AddListener(new UnityAction(this.OnReset));
			this.chkBuy.onValueChanged.AddListener(delegate(bool _)
			{
				this.ToggleTradeMode(this.chkBuy);
			});
			this.chkSell.onValueChanged.AddListener(delegate(bool _)
			{
				this.ToggleTradeMode(this.chkSell);
			});
			this.tooltip = UnityEngine.Object.Instantiate<GameObject>(this.tooltipPrefab, CanvasManager.instance.goCanvasControlPanels.transform).GetComponent<GUITooltip>();
			this.tooltip.SetWindow(GUITooltip.TooltipWindow.Trade);
		}

		// Keeps the trade tooltip synced with the currently hovered row/item.
		private void Update()
		{
			if (this.CoTooltip != null)
			{
				this.tooltip.SetTooltip(this.CoTooltip, GUITooltip.TooltipWindow.Trade);
				CrewSim.objInstance.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
			}
			else
			{
				this.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
			}
		}

		// Builds the synthetic "not for sale here" catalog by testing all known CO
		// defs/overlays against the vendor buy filter.
		private void GatherUnavailableItems()
		{
			CondTrigger ctbuy = this.ctBarter;
			Trader component = this.COSelf.GetComponent<Trader>();
			if (component != null && component.CTBuy != null)
			{
				ctbuy = component.CTBuy;
			}
			List<string> list = new List<string>(DataHandler.dictCOs.Keys);
			list.AddRange(DataHandler.dictCOOverlays.Keys);
			base.StartCoroutine(this.BuildUnavailableList(list, this.COSelf, ctbuy));
		}

		// Coroutine form of the unavailable-item scan so the UI can stay responsive
		// while probing many definitions.
		private IEnumerator BuildUnavailableList(List<string> unavailableIDs, CondOwner coVendor, CondTrigger ctThemBuy)
		{
			int count = 0;
			foreach (string id in unavailableIDs)
			{
				CondOwner co = DataHandler.GetCondOwner(id);
				if (!(co == null))
				{
					if (!this._unavailableData.DataRows.ContainsKey(co.FriendlyName) && ctThemBuy.Triggered(co, null, true))
					{
						TradeUnavailableDataRow tradeUnavailableDataRow = new TradeUnavailableDataRow(coVendor, co, ctThemBuy);
						this._unavailableData.DataRows.Add(tradeUnavailableDataRow.name, tradeUnavailableDataRow);
					}
					co.Destroy();
					count++;
					if (count > 40)
					{
						yield return null;
					}
				}
			}
			this._unavailableData.HasFinishedLoading = true;
			yield break;
		}

		// Rebuilds the trade list for the current vendor and buy/sell mode.
		private void SetTrade()
		{
			IEnumerator enumerator = this.pnlListContent.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					UnityEngine.Object.Destroy(transform.gameObject);
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			this._dictTradeRows.Clear();
			this._salesList.Clear();
			this._totalVal = 0f;
			this.txtTotal.text = DataHandler.GetString("GUI_TRADE_TOTAL_NORMAL", false);
			this.txtTotalVal.text = this._totalVal.ToString("n");
			string friendlyName = this.COSelf.FriendlyName;
			string str = (!this.chkSell.isOn) ? DataHandler.GetString("GUI_TRADE_TITLE_BUY", false) : DataHandler.GetString("GUI_TRADE_TITLE_SELL", false);
			this.txtThem.text = str + friendlyName;
			Trader component = this.COSelf.GetComponent<Trader>();
			if (component != null)
			{
				component.UpdateGPMs();
			}
			CondTrigger ctsell = this.ctBarter;
			if (component != null && component.CTSell != null)
			{
				ctsell = component.CTSell;
			}
			CondTrigger ctbuy = this.ctBarter;
			if (component != null && component.CTBuy != null)
			{
				ctbuy = component.CTBuy;
			}
			this.txtUpdate.text = ((!(component != null)) ? string.Empty : component.GetPriceUpdateInfo());
			List<TradeRowData> list = new List<TradeRowData>();
			if (this.chkSell.isOn)
			{
				List<CondOwner> cos = this._coUser.GetCOs(true, GUITrade.ctSellables);
				if (cos != null)
				{
					foreach (CondOwner condOwner in cos)
					{
						if (!condOwner.IsInsideContainer())
						{
							list.Add(new TradeRowData(condOwner, ctbuy, this.COSelf));
						}
					}
				}
			}
			List<TradeRowData> list2 = new List<TradeRowData>();
			if (this.chkBuy.isOn)
			{
				List<CondOwner> list3 = new List<CondOwner>();
				List<CondOwner> cos2 = this.COSelf.GetCOs(true, ctsell);
				if (cos2 != null)
				{
					foreach (CondOwner condOwner2 in cos2)
					{
						if (list3.IndexOf(condOwner2) < 0)
						{
							CondOwner.NullSafeAddRange(ref list3, condOwner2.GetCOs(true, ctsell));
							list2.Add(new TradeRowData(condOwner2, ctsell, this.COSelf));
						}
					}
				}
				List<CondOwner> cos3 = this.COSelf.GetCOs(true, GUITradeBase.ctInfinite);
				if (cos3 != null)
				{
					foreach (CondOwner condOwner3 in cos3)
					{
						cos2 = condOwner3.GetCOs(true, ctsell);
						list3.Clear();
						if (cos2 != null)
						{
							foreach (CondOwner condOwner4 in cos2)
							{
								if (list3.IndexOf(condOwner4) < 0)
								{
									if (!(condOwner4.objCOParent != condOwner3))
									{
										if (ctsell.Triggered(condOwner4, null, true))
										{
											list2.Add(new TradeRowData(condOwner4, ctsell, this.COSelf));
										}
									}
								}
							}
						}
					}
				}
			}
			base.GetZones();
			base.SetShipDropDown();
			List<CondOwner> list4 = new List<CondOwner>();
			HashSet<string> hashSet = new HashSet<string>();
			foreach (KeyValuePair<JsonZone, Ship> keyValuePair in this._mapZonesUser)
			{
				if (!this.chkSell.isOn)
				{
					break;
				}
				BarterZoneShip barterZoneShip = keyValuePair.Value as BarterZoneShip;
				list4 = ((barterZoneShip == null) ? keyValuePair.Value.GetCOsInZone(keyValuePair.Key, GUITrade.ctSellables, true, true) : barterZoneShip.GetCOsInLocalZone(keyValuePair.Key, GUITrade.ctSellables));
				if (list4 != null && list4.Count > 0)
				{
					List<CondOwner> list5 = (from x in list4
					where !x.IsInsideContainer()
					select x).ToList<CondOwner>();
					if (list5 != null)
					{
						List<CondOwner> list6 = new List<CondOwner>();
						foreach (CondOwner condOwner5 in list5)
						{
							if (!hashSet.Contains(condOwner5.strID))
							{
								hashSet.Add(condOwner5.strID);
								list6.Add(condOwner5);
							}
						}
						list5 = list6;
					}
					if (list5 != null && list5.Count != 0)
					{
						list.Add(new TradeRowData(keyValuePair.Key, list5, ctbuy, this.COSelf));
					}
				}
			}
			foreach (KeyValuePair<JsonZone, Ship> keyValuePair2 in this._mapZonesSelf)
			{
				if (!this.chkBuy.isOn)
				{
					break;
				}
				list4 = keyValuePair2.Value.GetCOsInZone(keyValuePair2.Key.strName, ctsell, true);
				if (list4 != null && list4.Count > 0)
				{
					foreach (CondOwner condOwner6 in list4)
					{
						list2.Add(new TradeRowData(condOwner6, ctsell, this.COSelf));
					}
				}
				List<CondOwner> cosInZone = keyValuePair2.Value.GetCOsInZone(keyValuePair2.Key.strName, GUITradeBase.ctInfinite, true);
				foreach (CondOwner condOwner7 in cosInZone)
				{
					list4 = condOwner7.GetCOs(true, ctsell);
					if (list4 != null && list4.Count > 0)
					{
						foreach (CondOwner condOwner8 in list4)
						{
							if (!(condOwner8.objCOParent != condOwner7))
							{
								if (ctsell.Triggered(condOwner8, null, true))
								{
									list2.Add(new TradeRowData(condOwner8, ctsell, this.COSelf));
								}
							}
						}
					}
				}
			}
			if (this.chkSell.isOn && this.bZonesLoading)
			{
				TradeRowData item = new TradeRowData(new JsonZone
				{
					strName = "Remote ship zones not loaded yet. Please close UI and try again.",
					zoneColor = Color.white
				}, new List<CondOwner>(), ctbuy, this.COSelf);
				list.Add(item);
			}
			this.BuildSellList(list);
			this.BuildBuyList(list2);
			if (this.chkSell.isOn)
			{
				GUIUnavailableContainer guiunavailableContainer = UnityEngine.Object.Instantiate<GUIUnavailableContainer>(this.TradeRowUnavailableContainer, this.pnlListContent);
				guiunavailableContainer.SetData(DataHandler.GetString("GUI_TRADE_WANTED_ITEMS", false), ref this._unavailableData, this);
			}
		}

		private void BuildSellList(List<TradeRowData> tradeRows)
		{
			foreach (TradeRowData tradeRowData in tradeRows)
			{
				if (!(tradeRowData.CoItem != null) || ((!tradeRowData.CoItem.bSlotLocked || tradeRowData.CoItem.slotNow == null) && !tradeRowData.CoItem.IsInsideContainer()))
				{
					if (tradeRowData.ContaineredCOs != null && (tradeRowData.ContaineredCOs.Count > 0 || tradeRowData.JsonZone != null) && !tradeRowData.IsEmptyOrPoweredOffDataContainer && (tradeRowData.CoItem == null || tradeRowData.CoItem.aStack.Count == 0))
					{
						string text = (!(tradeRowData.CoItem != null)) ? tradeRowData.JsonZone.strName : tradeRowData.CoItem.strID;
						if (!this._dictTradeRows.ContainsKey(text))
						{
							GUITradeRowBase guitradeRowBase = UnityEngine.Object.Instantiate<GUITradeRowContainer>(this.TradeRowContainerPrefab, this.pnlListContent);
							guitradeRowBase.SetCOs(tradeRowData, this);
							this._dictTradeRows.Add(text, guitradeRowBase);
						}
						else
						{
							Debug.LogWarning("Skipping duplicate key: " + text);
						}
					}
					else
					{
						this.AddSingleRow(tradeRowData, ref this._dictTradeRows, this.pnlListContent);
					}
				}
			}
		}

		private void BuildBuyList(List<TradeRowData> tradeRows)
		{
			foreach (TradeRowData tradeRowData in tradeRows)
			{
				if (!(tradeRowData.CoItem == null) && (!(tradeRowData.CoItem != null) || !tradeRowData.CoItem.bSlotLocked || tradeRowData.CoItem.slotNow == null))
				{
					this.AddSingleRow(tradeRowData, ref this._dictTradeRows, this.pnlListContent);
				}
			}
		}

		public void AddSingleRow(TradeRowData trd, ref Dictionary<string, GUITradeRowBase> tradeRowDict, Transform listParent)
		{
			string key = trd.CoItem.strCODef + trd.CoItem.GetTotalPrice(trd.CtVendor, false, false);
			GUITradeRowBase guitradeRowBase;
			if (tradeRowDict.TryGetValue(key, out guitradeRowBase))
			{
				guitradeRowBase.IncreaseCount(trd.CoItem.strID);
			}
			else
			{
				guitradeRowBase = UnityEngine.Object.Instantiate<GUITradeRowSingle>(this.TradeRowSinglePrefab, listParent);
				guitradeRowBase.SetCOs(trd, this);
				tradeRowDict.Add(key, guitradeRowBase);
			}
			guitradeRowBase.AddStackedItems(trd);
		}

		private void OnReset()
		{
			for (int i = this._salesList.Count - 1; i >= 0; i--)
			{
				if (this._salesList.Count > i && this._salesList[i] != null)
				{
					this._salesList[i].SetToMin();
				}
			}
			this._salesList.Clear();
			this.CalcTotal();
		}

		private void ToggleTradeMode(Toggle triggeredToggle)
		{
			if (!triggeredToggle.isOn)
			{
				return;
			}
			if (this.chkBuy.isOn)
			{
				this.txtNetUnits.text = DataHandler.GetString("GUI_TRADE_NET_LBL_BUY", false);
			}
			else if (this.chkSell.isOn)
			{
				this.txtNetUnits.text = DataHandler.GetString("GUI_TRADE_NET_LBL_SELL", false);
			}
			this.SetTrade();
		}

		private void OnSell()
		{
			if (!this.chkSell.isOn || this._coUser == null)
			{
				return;
			}
			this.txtError.gameObject.SetActive(true);
			this.bFits = false;
			this.bUpdateFunds = false;
			double condAmount = this._coUser.GetCondAmount(Ledger.CURRENCY);
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawnOK");
			this.strDesc = DataHandler.GetString("GUI_TRADE_LOG_BUY", false);
			this.bComma = false;
			foreach (GUITradeRowBase guitradeRowBase in this._salesList)
			{
				if (!(guitradeRowBase == null))
				{
					List<string> idsOfTradedCOs = guitradeRowBase.GetIDsOfTradedCOs();
					if (idsOfTradedCOs != null)
					{
						for (int i = 0; i < idsOfTradedCOs.Count; i++)
						{
							this.bFits = false;
							string text = idsOfTradedCOs[i];
							CondOwner condOwner = null;
							DataHandler.mapCOs.TryGetValue(text, out condOwner);
							if (condOwner == null)
							{
								List<string> ownedDockedShips = Ship.GetOwnedDockedShips(this._coUser, this.COSelf);
								foreach (string shipReg in ownedDockedShips)
								{
									Ship ship;
									if (MonoSingleton<AsyncShipLoader>.Instance.GetShip(shipReg, out ship) && ship != null)
									{
										condOwner = ship.GetCOByID(text);
										break;
									}
								}
								if (condOwner == null)
								{
									this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_ITEM", false) + " " + text;
									AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
									this.bFits = false;
									break;
								}
							}
							if (condOwner == null)
							{
								Debug.LogWarning("Could not find co for Sell transaction. Id: " + text);
								break;
							}
							List<CondOwner> aCOs = new List<CondOwner>
							{
								condOwner
							};
							List<CondOwner> list = new List<CondOwner>();
							this.strItem = condOwner.FriendlyName;
							Ship ship2 = condOwner.ship;
							CondOwner objCOParent = condOwner.objCOParent;
							condOwner.PopHeadFromStack();
							this.bFits = false;
							foreach (KeyValuePair<JsonZone, Ship> keyValuePair in this._mapZonesSelf)
							{
								if (keyValuePair.Key == null || keyValuePair.Value == null)
								{
									Debug.LogWarning("Null Zone/ship pair encountered, skipping");
								}
								else
								{
									List<CondOwner> cosInZone = keyValuePair.Value.GetCOsInZone(keyValuePair.Key, condTrigger, true, true);
									list = TileUtils.DropCOsNearby(aCOs, keyValuePair.Value, keyValuePair.Key, cosInZone, condTrigger, true, false);
									if (list.Count == 0)
									{
										this.RecordTransaction(guitradeRowBase.Price, true);
										break;
									}
									aCOs = list;
								}
							}
							if (!this.bFits)
							{
								list.Clear();
								CondOwner condOwner2 = this.COSelf.AddCO(condOwner, false, true, true);
								if (condOwner2 != null)
								{
									list.Add(condOwner2);
								}
								else
								{
									this.RecordTransaction(guitradeRowBase.Price, condOwner);
								}
							}
							if (!this.bFits)
							{
								this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_SPACE", false) + condOwner.FriendlyName;
								TMP_Text txtError = this.txtError;
								txtError.text = txtError.text + DataHandler.GetString("GUI_TRADE_ERROR_NO_SPACE_2", false) + this.COSelf.FriendlyName;
								TMP_Text txtError2 = this.txtError;
								txtError2.text += DataHandler.GetString("GUI_TRADE_ERROR_NO_SPACE_3", false);
								AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
								foreach (CondOwner objCO in list)
								{
									if (objCOParent != null)
									{
										objCOParent.AddCO(objCO, false, true, true);
									}
									else
									{
										ship2.RemoveCO(objCO, false);
									}
								}
								break;
							}
						}
						if (!this.bFits)
						{
							break;
						}
					}
				}
			}
			if (this.bUpdateFunds)
			{
				double condAmount2 = this._coUser.GetCondAmount(Ledger.CURRENCY);
				double num = condAmount2 - condAmount;
				LedgerLI li;
				if (num >= 0.0)
				{
					li = new LedgerLI(this._coUser.FriendlyName, this.COSelf.FriendlyName, Convert.ToSingle(num), this.strDesc, GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
				}
				else
				{
					li = new LedgerLI(this.COSelf.FriendlyName, this._coUser.FriendlyName, Convert.ToSingle(-num), this.strDesc, GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
				}
				Ledger.AddLI(li);
				string friendlyName = this.COSelf.FriendlyName;
				if (num < 0.0)
				{
					this._coUser.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_PAID", false) + friendlyName + ": " + (-num).ToString("n"), "Bad", this._coUser.strName);
				}
				else if (num > 0.0)
				{
					this._coUser.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_RECEIVED", false) + friendlyName + ": " + num.ToString("n"), "Good", this._coUser.strName);
				}
			}
			if (this.bFits)
			{
				this.txtError.gameObject.SetActive(false);
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptPos", false, false);
			}
			MonoSingleton<AsyncShipLoader>.Instance.SaveAsyncShips<BarterZoneShip>();
			this.RebuildList();
			GUITrade.OnGUITradeSale.Invoke();
		}

		private void RebuildList()
		{
			this._salesList.Clear();
			if (this._dictTradeRows.Count == 0)
			{
				return;
			}
			GUITradeRowBase[] array = this._dictTradeRows.Values.ToArray<GUITradeRowBase>();
			for (int i = array.Length - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(array[i].gameObject);
			}
			this._dictTradeRows.Clear();
			this.SetTrade();
		}

		private void OnExpandHide()
		{
			bool flag = true;
			IEnumerator enumerator = this.pnlListContent.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					GUITradeRowContainer component = transform.GetComponent<GUITradeRowContainer>();
					if (component != null && component.IsExpanded)
					{
						flag = false;
						break;
					}
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			if (flag)
			{
				IEnumerator enumerator2 = this.pnlListContent.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						object obj2 = enumerator2.Current;
						Transform transform2 = (Transform)obj2;
						GUITradeRowContainer component2 = transform2.GetComponent<GUITradeRowContainer>();
						if (component2 != null && !component2.IsExpanded)
						{
							component2.Expand();
						}
					}
				}
				finally
				{
					IDisposable disposable2;
					if ((disposable2 = (enumerator2 as IDisposable)) != null)
					{
						disposable2.Dispose();
					}
				}
			}
			else if (!flag)
			{
				IEnumerator enumerator3 = this.pnlListContent.GetEnumerator();
				try
				{
					while (enumerator3.MoveNext())
					{
						object obj3 = enumerator3.Current;
						Transform transform3 = (Transform)obj3;
						GUITradeRowContainer component3 = transform3.GetComponent<GUITradeRowContainer>();
						if (component3 != null && component3.IsExpanded)
						{
							component3.Hide();
						}
					}
				}
				finally
				{
					IDisposable disposable3;
					if ((disposable3 = (enumerator3 as IDisposable)) != null)
					{
						disposable3.Dispose();
					}
				}
			}
		}

		private void OnBuy()
		{
			if (this.chkSell.isOn || !this.chkBuy.isOn)
			{
				return;
			}
			this.txtError.gameObject.SetActive(true);
			if ((double)this._totalVal > this._coUser.GetCondAmount(Ledger.CURRENCY))
			{
				this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_FUNDS", false);
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
				return;
			}
			this.bFits = true;
			this.bUpdateFunds = false;
			double condAmount = this._coUser.GetCondAmount(Ledger.CURRENCY);
			this.strDesc = DataHandler.GetString("GUI_TRADE_LOG_BUY", false);
			this.bComma = false;
			this.strItem = null;
			foreach (GUITradeRowBase guitradeRowBase in this._salesList)
			{
				base.BuyItems(guitradeRowBase.GetIDsOfTradedCOs(), guitradeRowBase.Price, this._mapZonesUser);
			}
			if (this.bUpdateFunds)
			{
				Ledger.UpdateLedger(this._coUser, this.COSelf.strNameFriendly, this._coUser.GetCondAmount(Ledger.CURRENCY) - condAmount, this.strDesc);
			}
			if (this.bFits)
			{
				this.txtError.gameObject.SetActive(false);
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptPos", false, false);
			}
			MonoSingleton<AsyncShipLoader>.Instance.SaveAsyncShips<BarterZoneShip>();
			this.RebuildList();
		}

		protected override void RecordTransaction(float salesPrice, bool areWeSelling)
		{
			this._coUser.AddCondAmount(Ledger.CURRENCY, (double)((!areWeSelling) ? (-(double)salesPrice) : salesPrice), 0.0, 0f);
			this.COSelf.AddCondAmount(Ledger.CURRENCY, (double)((!areWeSelling) ? salesPrice : (-(double)salesPrice)), 0.0, 0f);
			this.bUpdateFunds = true;
			this.bFits = true;
			if (this.strDesc.IndexOf(this.strItem, StringComparison.Ordinal) < 0)
			{
				if (!this.bComma)
				{
					this.bComma = true;
				}
				else
				{
					this.strDesc += ", ";
				}
				this.strDesc += this.strItem;
			}
		}

		private void OnCancel()
		{
			CrewSim.LowerUI(false);
		}

		private void CalcTotal()
		{
			this._totalVal = 0f;
			foreach (GUITradeRowBase guitradeRowBase in this._salesList)
			{
				this._totalVal += guitradeRowBase.TransactionCost;
			}
			if (this._totalVal >= 0f)
			{
				this.txtTotal.text = DataHandler.GetString("GUI_TRADE_TOTAL_NORMAL", false);
				this.txtTotalVal.text = this._totalVal.ToString("n");
			}
			else
			{
				this.txtTotal.text = DataHandler.GetString("GUI_TRADE_TOTAL_NEGATIVE", false);
				this.txtTotalVal.text = (-this._totalVal).ToString("n");
			}
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			this.ctBarter = DataHandler.GetCondTrigger("TIsBarter");
			GUITrade.ctSellables = DataHandler.GetCondTrigger("TIsSellable");
			this.GatherUnavailableItems();
			this.SetTrade();
		}

		public override void SaveAndClose()
		{
			if (this.dictPropMap == null)
			{
				return;
			}
			if (this.tooltip != null)
			{
				UnityEngine.Object.Destroy(this.tooltip.gameObject);
			}
			base.SaveAndClose();
		}

		public void AddToSale(GUITradeRowBase co, int amount)
		{
			int num = this._salesList.IndexOf(co);
			if (num != -1)
			{
				if (amount <= 0)
				{
					this._salesList.Remove(co);
				}
			}
			else
			{
				this._salesList.Add(co);
			}
			this.CalcTotal();
		}

		[Header("Interactables")]
		[SerializeField]
		private Button btnAccept;

		[SerializeField]
		private Button btnCancel;

		[SerializeField]
		private Button btnReset;

		[SerializeField]
		private Button btnExpandHide;

		[SerializeField]
		private Toggle chkBuy;

		[SerializeField]
		public Toggle chkSell;

		[Header("Text")]
		[SerializeField]
		private TMP_Text txtNetUnits;

		[SerializeField]
		private TMP_Text txtTotal;

		[SerializeField]
		private TMP_Text txtTotalVal;

		[SerializeField]
		private TMP_Text txtThem;

		[SerializeField]
		private TMP_Text txtUpdate;

		[Header("Other")]
		[SerializeField]
		private Transform pnlListContent;

		[SerializeField]
		private GameObject tooltipPrefab;

		[SerializeField]
		public GUITradeRowSingle TradeRowSinglePrefab;

		[SerializeField]
		public GUITradeRowContainer TradeRowContainerPrefab;

		[SerializeField]
		public GUIUnavailableContainer TradeRowUnavailableContainer;

		[HideInInspector]
		public GUITradeRowBase deepestTradeRowHovered;

		private GUITooltip tooltip;

		private float _totalVal;

		private TradeUnavailableDataContainer _unavailableData = new TradeUnavailableDataContainer();

		private Dictionary<string, GUITradeRowBase> _dictTradeRows = new Dictionary<string, GUITradeRowBase>();

		private readonly List<GUITradeRowBase> _salesList = new List<GUITradeRowBase>();

		private string strDesc;

		private bool bComma;

		private CondTrigger ctBarter;

		public static CondTrigger ctSellables;

		public static OnGUITradeSale OnGUITradeSale = new OnGUITradeSale();
	}
}
