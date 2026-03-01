using System;
using Ostranauts.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules.SubElements
{
	public class NumbElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
	{
		private void Awake()
		{
		}

		private void OnDestroy()
		{
			if (this._isShowingTooltip)
			{
				this.OnPointerExit(null);
			}
		}

		public void SetData(string strNameFriendly, string strCondName, string strData, string strDesc, Color color)
		{
			this._condName = strCondName;
			this._txtCondName.text = strNameFriendly + ":";
			this._txtCondName.color = Color.black;
			this._txtCondAmount.text = strData;
			this._txtCondAmount.color = color;
			this._pnlBackground.color = color;
			this._condNameFriendly = strNameFriendly;
			this._condDesc = strDesc.Replace("[us]", GUIMegaToolTip.Selected.FriendlyName);
		}

		public void SetData(string strNameFriendly, Tuple<string, string> condNames, Func<string, string, string> updateCallback, string strDesc, Color color)
		{
			this._updateCallBack = updateCallback;
			this.SetData(strNameFriendly, condNames.Item1, updateCallback(condNames.Item1, condNames.Item2), strDesc, color);
			this._condName2 = condNames.Item2;
		}

		public void ForceMeshUpdate()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			this._txtCondName.ForceMeshUpdate();
			this._txtCondAmount.ForceMeshUpdate();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			this._isShowingTooltip = true;
			GUITooltip2.SetToolTip(this._condNameFriendly, this._condDesc, true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			this._isShowingTooltip = false;
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		}

		public void UpdateElement()
		{
			if (this._updateCallBack == null)
			{
				return;
			}
			this._txtCondAmount.text = this._updateCallBack(this._condName, this._condName2);
		}

		public string CondName
		{
			get
			{
				return this._condName;
			}
		}

		[SerializeField]
		private TMP_Text _txtCondName;

		[SerializeField]
		private Image _pnlBackground;

		[SerializeField]
		private TMP_Text _txtCondAmount;

		[SerializeField]
		private string _condNameFriendly;

		[SerializeField]
		private string _condDesc;

		[SerializeField]
		private string _condName;

		private string _condName2;

		private bool _isShowingTooltip;

		private Func<string, string, string> _updateCallBack;
	}
}
