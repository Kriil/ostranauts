using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Chargen
{
	public class GUISkillToggle : MonoBehaviour
	{
		public bool IsOn
		{
			get
			{
				return this._chk.isOn;
			}
			set
			{
				this._chk.isOn = value;
			}
		}

		public void SetupToggle(string text, CondOwner coUser, int years, UnityAction<bool> callback, Condition cond)
		{
			this._label.text = text;
			this._yearCost = years;
			bool flag = cond.strAnti != null && coUser.HasCond(cond.strAnti);
			this.ToggleLockIcon(flag);
			this.SetDots();
			this._chk.isOn = (!flag && coUser.HasCond(cond.strName));
			this._toggleCallback = callback;
			this._chk.onValueChanged.AddListener(callback);
			AudioManager.AddBtnAudio(this._chk.gameObject, "ShipUIBtnJobsKioskClickIn", this._chk.interactable ? "ShipUIBtnJobsKioskClickOut" : "ShipUIBtnJobsKioskClickLocked");
			GUIEnterExitHandler component = this._chk.GetComponent<GUIEnterExitHandler>();
			component.fnOnEnter = delegate()
			{
				GUITooltip2.SetToolTip(cond.strNameFriendly, this.CreateTooltipDescr(coUser, cond, years * ((!this._chk.isOn) ? 1 : -1), DataHandler.GetCond(cond.strAnti), coUser.FriendlyName), true);
			};
			component.fnOnExit = delegate()
			{
				GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
			};
		}

		public void ToggleLockIcon(bool blocked)
		{
			this._lockIcon.gameObject.SetActive(blocked);
			this._chk.interactable = !blocked;
		}

		public void ToggleChkSilently(bool newState)
		{
			if (newState == this._chk.isOn)
			{
				return;
			}
			this._chk.onValueChanged.RemoveAllListeners();
			this._chk.isOn = newState;
			this._chk.onValueChanged.AddListener(this._toggleCallback);
		}

		private void SetDots()
		{
			for (int i = this._dots.Length - 1; i >= 0; i--)
			{
				bool flag = i <= Mathf.Abs(this._yearCost) - 1;
				this._dots[i].SetActive(flag);
				this._innerDots[i].SetActive(flag && this._yearCost < 0);
			}
		}

		private string CreateTooltipDescr(CondOwner coUser, Condition cond, int nYears, Condition condAnti, string coUserFriendlyName)
		{
			if (cond == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<b>");
			stringBuilder.Append(cond.strNameFriendly);
			stringBuilder.AppendLine("</b>");
			stringBuilder.AppendLine(GrammarUtils.GetInflectedString(cond.strDesc, cond, coUser));
			stringBuilder.AppendLine();
			stringBuilder.Append(DataHandler.GetString("GUI_CAREER_SIDEBAR_COST_1", false));
			stringBuilder.Append(nYears);
			stringBuilder.Append(DataHandler.GetString("GUI_CAREER_SIDEBAR_COST_2", false));
			if (condAnti != null)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				stringBuilder.Append(DataHandler.GetString("GUI_CAREER_SIDEBAR_ANTI", false));
				stringBuilder.Append(condAnti.strNameFriendly);
			}
			return stringBuilder.ToString();
		}

		[SerializeField]
		private TMP_Text _label;

		[SerializeField]
		private Toggle _chk;

		[SerializeField]
		private GameObject[] _dots;

		[SerializeField]
		private GameObject[] _innerDots;

		[SerializeField]
		private GameObject _lockIcon;

		private int _yearCost;

		private UnityAction<bool> _toggleCallback;
	}
}
