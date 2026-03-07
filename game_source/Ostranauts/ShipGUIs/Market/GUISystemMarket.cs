using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Events;
using Ostranauts.Trading;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUISystemMarket : MonoBehaviour
	{
		private void Awake()
		{
			if (GUIBulkTrader.OnMarketTransaction == null)
			{
				GUIBulkTrader.OnMarketTransaction = new OnMarketTransactionEvent();
			}
			GUIBulkTrader.OnMarketTransaction.AddListener(new UnityAction<string>(this.OnMarketTransactionHandler));
		}

		private IEnumerator Start()
		{
			yield return new WaitForSeconds(1f);
			this.BuildSystemMarket();
			yield break;
		}

		private void OnDestroy()
		{
			base.StopAllCoroutines();
			if (GUIBulkTrader.OnMarketTransaction != null)
			{
				GUIBulkTrader.OnMarketTransaction.RemoveListener(new UnityAction<string>(this.OnMarketTransactionHandler));
			}
		}

		private void BuildSystemMarket()
		{
			Dictionary<string, Dictionary<string, float>> systemMarket = MarketManager.GetSystemMarket();
			if (systemMarket == null || systemMarket.Count == 0)
			{
				return;
			}
			HashSet<string> source = new HashSet<string>(systemMarket.SelectMany((KeyValuePair<string, Dictionary<string, float>> x) => x.Value.Keys));
			this._uniqueStations = source.ToList<string>();
			this._uniqueStations.Sort();
			this._categoryList = systemMarket.Keys.ToList<string>();
			this._categoryList.Sort();
			foreach (string text in this._categoryList)
			{
				if (this.SpawnCenterPanelCells(systemMarket, text))
				{
					GUISystemMarketCategoryRow guisystemMarketCategoryRow = UnityEngine.Object.Instantiate<GUISystemMarketCategoryRow>(this.guiSystemMarketCategoryCategoryRow, this.pnlListCategories);
					DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(text);
					string data = (dataCoCollection == null) ? text : dataCoCollection.FriendlyName;
					guisystemMarketCategoryRow.SetData(data);
				}
			}
			int num = 0;
			foreach (string stationName in this._uniqueStations)
			{
				GUISystemMarketColumn guisystemMarketColumn = UnityEngine.Object.Instantiate<GUISystemMarketColumn>(this.guiSystemMarketColumn, this.pnlListStations);
				guisystemMarketColumn.SetData(stationName, num);
				num++;
			}
		}

		private bool SpawnCenterPanelCells(Dictionary<string, Dictionary<string, float>> systemMarket, string categoryName)
		{
			Dictionary<string, float> marketData;
			if (systemMarket.TryGetValue(categoryName, out marketData))
			{
				GUISystemMarketGridRow guisystemMarketGridRow = UnityEngine.Object.Instantiate<GUISystemMarketGridRow>(this.guiSystemMarketGridRow, this.pnlListCenter);
				guisystemMarketGridRow.SetData(this._uniqueStations, marketData);
				return true;
			}
			return false;
		}

		private void OnMarketTransactionHandler(string notNeeded)
		{
			if (this._updateCoroutine != null)
			{
				return;
			}
			this._updateCoroutine = base.StartCoroutine(this.RespawnCenterTiles());
		}

		private IEnumerator RespawnCenterTiles()
		{
			yield return new WaitForSeconds(0.5f);
			IEnumerator enumerator = this.pnlListCenter.GetEnumerator();
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
			yield return new WaitForSeconds(0.3f);
			Dictionary<string, Dictionary<string, float>> systemMarket = MarketManager.GetSystemMarket();
			if (systemMarket == null || systemMarket.Count == 0)
			{
				this._updateCoroutine = null;
				yield break;
			}
			foreach (string categoryName in this._categoryList)
			{
				this.SpawnCenterPanelCells(systemMarket, categoryName);
			}
			this._updateCoroutine = null;
			yield break;
		}

		[SerializeField]
		private Transform pnlListCenter;

		[SerializeField]
		private Transform pnlListCategories;

		[SerializeField]
		private Transform pnlListStations;

		[SerializeField]
		public GUISystemMarketCategoryRow guiSystemMarketCategoryCategoryRow;

		[SerializeField]
		public GUISystemMarketColumn guiSystemMarketColumn;

		[SerializeField]
		public GUISystemMarketGridRow guiSystemMarketGridRow;

		private List<string> _categoryList = new List<string>();

		private List<string> _uniqueStations = new List<string>();

		private Coroutine _updateCoroutine;
	}
}
