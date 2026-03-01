using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.Components
{
	public class MultiSelectDropDown : MonoBehaviour
	{
		private void Awake()
		{
			if (this._template == null)
			{
				this._template = Resources.Load<Transform>("GUIShip/chkMultiSelect");
			}
			this._toggleMenuButton.onClick.AddListener(new UnityAction(this.OnToggleDropDownMenu));
			this._backgroundButton.onClick.AddListener(new UnityAction(this.OnBackgroundClick));
		}

		public void Init(List<MultiSelectDTO> options, UnityAction<List<MultiSelectDTO>> callback)
		{
			this._options = options;
			this._callback = callback;
			this._backgroundButton.gameObject.SetActive(false);
			this.UpdateHeaderLabel();
			this.SetDropdownButtonInteractivity(true);
		}

		public void Reset()
		{
			this.OnBackgroundClick();
		}

		private void OnBackgroundClick()
		{
			if (this._goDropdown.activeSelf)
			{
				this.OnToggleDropDownMenu();
			}
		}

		private void OnToggleDropDownMenu()
		{
			this._goDropdown.SetActive(!this._goDropdown.activeSelf);
			this._backgroundButton.gameObject.SetActive(this._goDropdown.activeSelf);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAClick02", false, false);
			if (!this._goDropdown.activeSelf || this._tfContainer.transform.childCount > 0)
			{
				return;
			}
			foreach (MultiSelectDTO multiSelectDTO in this._options)
			{
				Toggle chkOption = UnityEngine.Object.Instantiate<Transform>(this._template, this._tfContainer).GetComponentInChildren<Toggle>();
				chkOption.name = multiSelectDTO.Id;
				chkOption.isOn = multiSelectDTO.IsOn;
				chkOption.transform.parent.Find("Label").GetComponent<TMP_Text>().text = multiSelectDTO.FriendlyName;
				chkOption.onValueChanged.AddListener(delegate(bool isOn)
				{
					this.OnOptionToggled(chkOption);
				});
				this._dictToggles.Add(chkOption, multiSelectDTO.Id);
			}
		}

		private void OnOptionToggled(Toggle toggle)
		{
			string name = this._dictToggles[toggle];
			MultiSelectDTO multiSelectDTO = this._options.FirstOrDefault((MultiSelectDTO x) => x.Id == name);
			if (multiSelectDTO == null)
			{
				return;
			}
			multiSelectDTO.IsOn = toggle.isOn;
			this.UpdateHeaderLabel();
			this._callback(this._options);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAClick02", false, false);
		}

		private void UpdateHeaderLabel()
		{
			if (this._headerLabel == null)
			{
				return;
			}
			this._headerLabel.text = string.Empty;
			int num = 0;
			foreach (MultiSelectDTO multiSelectDTO in this._options)
			{
				if (multiSelectDTO.IsOn)
				{
					TMP_Text headerLabel = this._headerLabel;
					headerLabel.text += multiSelectDTO.FriendlyName;
				}
				if (num < this._options.Count - 1)
				{
					TMP_Text headerLabel2 = this._headerLabel;
					headerLabel2.text += "/";
				}
				num++;
			}
			if (this._headerLabel.text.Length > 22)
			{
				this._headerLabel.text = this._headerLabel.text.Substring(0, 22) + "...";
			}
		}

		private void SetDropdownButtonInteractivity(bool enable)
		{
			this._toggleMenuButton.interactable = enable;
			this._buttonOverlayImage.SetActive(!enable);
		}

		[SerializeField]
		private Transform _tfContainer;

		[SerializeField]
		private Button _toggleMenuButton;

		[SerializeField]
		private GameObject _goDropdown;

		[SerializeField]
		private Button _backgroundButton;

		[SerializeField]
		private TMP_Text _headerLabel;

		[SerializeField]
		private GameObject _buttonOverlayImage;

		private Transform _template;

		private Dictionary<Toggle, string> _dictToggles = new Dictionary<Toggle, string>();

		private List<MultiSelectDTO> _options = new List<MultiSelectDTO>();

		private UnityAction<List<MultiSelectDTO>> _callback;
	}
}
