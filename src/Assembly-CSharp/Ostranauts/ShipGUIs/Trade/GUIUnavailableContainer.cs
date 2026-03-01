using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Trade.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Trade
{
	public class GUIUnavailableContainer : MonoBehaviour
	{
		private void Start()
		{
			this.btnToggleExpand.onClick.AddListener(new UnityAction(this.OnToggle));
		}

		private void OnToggle()
		{
			this.IsExpanded = !this.tfSubItemContainer.gameObject.activeSelf;
			if (this.IsExpanded && (this._dictTradeRowChildren.Count == 0 || !this._unavailableDataContainer.HasFinishedLoading))
			{
				if (this._listLoader == null)
				{
					this._listLoader = base.StartCoroutine(this.LoadList());
				}
				return;
			}
			int num = (!this.IsExpanded) ? 90 : 0;
			this.tfbmpdropdown.rotation = Quaternion.Euler(0f, 0f, (float)num);
			int num2 = (!this.IsExpanded) ? 1 : (this.tfSubItemContainer.childCount + 1);
			RectTransform component = base.GetComponent<RectTransform>();
			Vector2 sizeDelta = new Vector2(component.sizeDelta.x, (float)(num2 * 36));
			component.sizeDelta = sizeDelta;
			this.tfSubItemContainer.gameObject.SetActive(this.IsExpanded);
			base.StartCoroutine(this.UpdateLayoutGroup(base.GetComponentInParent<VerticalLayoutGroup>()));
		}

		private IEnumerator UpdateLayoutGroup(VerticalLayoutGroup layoutGroupComponent)
		{
			layoutGroupComponent.enabled = false;
			yield return new WaitForEndOfFrame();
			layoutGroupComponent.enabled = true;
			yield break;
		}

		private IEnumerator LoadList()
		{
			this.goLoading.SetActive(true);
			yield return null;
			if (!this._unavailableDataContainer.HasFinishedLoading)
			{
				yield return new WaitUntil(() => this._unavailableDataContainer.HasFinishedLoading);
			}
			this.BuildUnavailableList();
			this.goLoading.SetActive(false);
			this.OnToggle();
			this._listLoader = null;
			yield break;
		}

		private void BuildUnavailableList()
		{
			foreach (TradeUnavailableDataRow tradeUnavailableDataRow in this._unavailableDataContainer.DataRows.Values)
			{
				string name = tradeUnavailableDataRow.name;
				if (!this._dictTradeRowChildren.ContainsKey(name))
				{
					GUITradeRowSingle guitradeRowSingle = UnityEngine.Object.Instantiate<GUITradeRowSingle>(this._guiTrade.TradeRowSinglePrefab, this.tfSubItemContainer);
					guitradeRowSingle.SetUnavailableSingle(tradeUnavailableDataRow.name, tradeUnavailableDataRow.price, tradeUnavailableDataRow.imgPath);
					this._dictTradeRowChildren.Add(name, guitradeRowSingle);
				}
			}
		}

		public void SetData(string name, ref TradeUnavailableDataContainer unavailableDataContainer, GUITrade guiTradeRef)
		{
			this._guiTrade = guiTradeRef;
			this._unavailableDataContainer = unavailableDataContainer;
			this.txtCoName.text = name;
		}

		[SerializeField]
		private Button btnToggleExpand;

		[SerializeField]
		private TMP_Text txtCoName;

		[SerializeField]
		private GameObject goLoading;

		[SerializeField]
		private Transform tfSubItemContainer;

		[SerializeField]
		private RectTransform tfbmpdropdown;

		private Dictionary<string, GUITradeRowBase> _dictTradeRowChildren = new Dictionary<string, GUITradeRowBase>();

		private TradeUnavailableDataContainer _unavailableDataContainer;

		public bool IsExpanded;

		private GUITrade _guiTrade;

		private Coroutine _listLoader;
	}
}
