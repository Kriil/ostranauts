using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Zones
{
	public class ZoneListEntry : MonoBehaviour
	{
		private void Awake()
		{
			this._colorSelectionMenuCanvasGroup.alpha = 0f;
			this._colorSelectionMenuCanvasGroup.interactable = false;
			this._colorSelectionMenuCanvasGroup.blocksRaycasts = false;
			this._toggleColorMenuButton.onClick.AddListener(new UnityAction(this.OnToggleColorMenu));
			this._toggleEditMode.onClick.AddListener(new UnityAction(this.OnToggleEditMode));
		}

		private void AddToggle(string zoneName, string zoneConditionName, bool isOn)
		{
			Toggle chkToggle = UnityEngine.Object.Instantiate<Transform>(this._template, this._selectionContainer).GetComponentInChildren<Toggle>();
			chkToggle.name = zoneName;
			chkToggle.isOn = isOn;
			chkToggle.transform.parent.Find("Label").GetComponent<TMP_Text>().text = zoneName;
			chkToggle.onValueChanged.AddListener(delegate(bool A_1)
			{
				this.OnZoneTypeChanged(chkToggle);
			});
			this._dictZones.Add(chkToggle, zoneConditionName);
		}

		private void OnZoneTypeChanged(Toggle chk)
		{
			if (this._ignoreInput)
			{
				return;
			}
			this._ignoreInput = true;
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			if (this._dictZones[chk] == null && chk.isOn)
			{
				foreach (KeyValuePair<Toggle, string> keyValuePair in this._dictZones)
				{
					if (keyValuePair.Value != null)
					{
						list2.Add(keyValuePair.Value);
					}
					keyValuePair.Key.isOn = (keyValuePair.Value == null);
				}
			}
			else if (!chk.isOn && this._dictZones[chk] != null)
			{
				foreach (KeyValuePair<Toggle, string> keyValuePair2 in this._dictZones)
				{
					if (keyValuePair2.Value != null && keyValuePair2.Value == this._dictZones[chk])
					{
						list2.Add(keyValuePair2.Value);
					}
					if (keyValuePair2.Key.isOn && keyValuePair2.Value != null)
					{
						list.Add(keyValuePair2.Value);
					}
				}
			}
			else
			{
				foreach (KeyValuePair<Toggle, string> keyValuePair3 in this._dictZones)
				{
					if (keyValuePair3.Key.isOn && keyValuePair3.Value != null)
					{
						list.Add(keyValuePair3.Value);
					}
					if (this._dictZones[keyValuePair3.Key] == null)
					{
						keyValuePair3.Key.isOn = false;
					}
				}
			}
			this._jsonZone.aTileConds = list.ToArray();
			this._guiZones.UpdateTileConditions(this._jsonZone, list2);
			this.SetCategoryDropdownButtonInteractibility(this._jsonZone.aTileConds.Any((string x) => x.Contains("IsZoneStockpile") || x.Contains("IsZoneBarter")));
			if (this._jsonZone.aTileConds.Contains("IsZoneTrigger"))
			{
				if (!this._jsonZone.strName.Contains("trigger"))
				{
					this.OnZoneNameChanged("trigger" + this._jsonZone.strName);
				}
				this.OverwriteCategories();
				this.SetCategoryDropdownButtonInteractibility(true);
			}
			this._ignoreInput = false;
		}

		private void OnTriggerOwnerChanged(Toggle chk)
		{
			this._jsonZone.bTriggerOnOwner = chk.isOn;
			this._guiZones.UpdateTileConditions(this._jsonZone, null);
		}

		private void OverwriteCategories()
		{
			for (int i = this._categoryContainer.transform.childCount - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(this._categoryContainer.transform.GetChild(i).gameObject);
			}
			using (Dictionary<string, string>.Enumerator enumerator = this._guiZones.TriggerConds.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZoneListEntry.<OverwriteCategories>c__AnonStorey1 <OverwriteCategories>c__AnonStorey = new ZoneListEntry.<OverwriteCategories>c__AnonStorey1();
					<OverwriteCategories>c__AnonStorey.triggerCond = enumerator.Current;
					<OverwriteCategories>c__AnonStorey.$this = this;
					Toggle chkToggle = UnityEngine.Object.Instantiate<Transform>(this._template, this._categoryContainer).GetComponentInChildren<Toggle>();
					chkToggle.name = <OverwriteCategories>c__AnonStorey.triggerCond.Key;
					chkToggle.isOn = this._jsonZone.categoryConds.Any((string itm) => itm == <OverwriteCategories>c__AnonStorey.triggerCond.Key);
					chkToggle.transform.parent.Find("Label").GetComponent<TMP_Text>().text = <OverwriteCategories>c__AnonStorey.triggerCond.Value;
					chkToggle.onValueChanged.AddListener(delegate(bool isOn)
					{
						<OverwriteCategories>c__AnonStorey.$this.OnCategoryChanged(chkToggle);
					});
					this._dictCategories.Add(chkToggle, <OverwriteCategories>c__AnonStorey.triggerCond.Key);
				}
			}
		}

		private void OnZoneNameChanged(string newName)
		{
			if (string.Equals(newName, this._jsonZone.strName, StringComparison.CurrentCultureIgnoreCase))
			{
				return;
			}
			string text = this._guiZones.RenameZone(this._jsonZone.strName, newName.ToLower());
			this._jsonZone.strName = text;
			this._nameInputField.text = text;
		}

		private void OnDeleteZonePressed()
		{
			this._guiZones.DeleteZone(this._jsonZone.strName);
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private void OnToggleColorMenu()
		{
			bool flag = this._colorSelectionMenuCanvasGroup.alpha <= 0f;
			this._colorSelectionMenuCanvasGroup.alpha = (float)((!flag) ? 0 : 1);
			this._colorSelectionMenuCanvasGroup.interactable = flag;
			this._colorSelectionMenuCanvasGroup.blocksRaycasts = flag;
			if (this._dictColors.Count == 0)
			{
				this.SpawnColorMenuItems();
			}
		}

		private void OnToggleEditMode()
		{
			this._selected = !this._selected;
			if (this._selected)
			{
				this._toggleEditMode.GetComponent<Image>().color = Color.green;
			}
			else
			{
				this._toggleEditMode.GetComponent<Image>().color = Color.white;
			}
			this._guiZones.SelectZone(this._jsonZone.strName, this._selected);
		}

		private void SpawnColorMenuItems()
		{
			this._dictColors = new Dictionary<Button, Color>();
			foreach (Color color in this._guiZones.AvailableColors)
			{
				if (this._colorButtonTemplate == null)
				{
					this._colorButtonTemplate = Resources.Load<Transform>("GUIShip/GUIZones/ColorButton");
				}
				Button colorButton = UnityEngine.Object.Instantiate<Transform>(this._colorButtonTemplate, this._colorButtonContainer).GetComponentInChildren<Button>();
				colorButton.GetComponent<Image>().color = color;
				colorButton.onClick.AddListener(delegate()
				{
					this.OnNewColorSelected(colorButton);
				});
				this._dictColors[colorButton] = color;
			}
		}

		private void OnNewColorSelected(Button clickedButton)
		{
			Color color = clickedButton.GetComponent<Image>().color;
			this._toggleColorMenuButton.GetComponent<Image>().color = color;
			this._jsonZone.zoneColor = color;
			this.OnToggleColorMenu();
			this._guiZones.UpdateTileConditions(this._jsonZone, null);
		}

		private void OnOwnerChanged(int selectedIndex)
		{
			string empty = string.Empty;
			if (this._guiZones.Ranks.TryGetValue(this._ddlOwner.options[selectedIndex].text, out empty))
			{
				this._jsonZone.strPersonSpec = empty;
			}
			this._guiZones.UpdateTileConditions(this._jsonZone, null);
		}

		private void OnRankChanged(int selectedIndex)
		{
			string empty = string.Empty;
			if (this._guiZones.Ranks.TryGetValue(this._roleDropDown.options[selectedIndex].text, out empty))
			{
				this._jsonZone.strTargetPSpec = empty;
			}
			this._guiZones.UpdateTileConditions(this._jsonZone, null);
		}

		private void OnToggleCategoryMenu()
		{
			this._categoryDropdown.SetActive(!this._categoryDropdown.activeSelf);
			this._backgroundButton.gameObject.SetActive(this._categoryDropdown.activeSelf);
			if (!this._categoryDropdown.activeSelf || this._categoryContainer.transform.childCount > 0)
			{
				return;
			}
			using (Dictionary<string, string>.Enumerator enumerator = this._guiZones.Categories.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZoneListEntry.<OnToggleCategoryMenu>c__AnonStorey4 <OnToggleCategoryMenu>c__AnonStorey = new ZoneListEntry.<OnToggleCategoryMenu>c__AnonStorey4();
					<OnToggleCategoryMenu>c__AnonStorey.category = enumerator.Current;
					<OnToggleCategoryMenu>c__AnonStorey.$this = this;
					Toggle chkToggle = UnityEngine.Object.Instantiate<Transform>(this._template, this._categoryContainer).GetComponentInChildren<Toggle>();
					chkToggle.name = <OnToggleCategoryMenu>c__AnonStorey.category.Key;
					chkToggle.isOn = this._jsonZone.categoryConds.Any((string itm) => itm == <OnToggleCategoryMenu>c__AnonStorey.category.Key);
					chkToggle.transform.parent.Find("Label").GetComponent<TMP_Text>().text = <OnToggleCategoryMenu>c__AnonStorey.category.Value;
					chkToggle.onValueChanged.AddListener(delegate(bool isOn)
					{
						<OnToggleCategoryMenu>c__AnonStorey.$this.OnCategoryChanged(chkToggle);
					});
					this._dictCategories.Add(chkToggle, <OnToggleCategoryMenu>c__AnonStorey.category.Key);
				}
			}
		}

		private void OnCategoryChanged(Toggle categoryToggle)
		{
			List<string> list = this._jsonZone.categoryConds.ToList<string>();
			if (categoryToggle.isOn && list.All((string itm) => itm != categoryToggle.name))
			{
				list.Add(categoryToggle.name);
			}
			else if (!categoryToggle.isOn && list.Any((string itm) => itm == categoryToggle.name))
			{
				list.Remove(categoryToggle.name);
			}
			this._jsonZone.categoryConds = list.ToArray();
			this.UpdateCategoryLabel();
			this._guiZones.UpdateTileConditions(this._jsonZone, null);
		}

		private void OnBackgroundClick()
		{
			if (this._categoryDropdown.activeSelf)
			{
				this.OnToggleCategoryMenu();
			}
		}

		private void UpdateCategoryLabel()
		{
			if (this._jsonZone.categoryConds == null || this._jsonZone.categoryConds.Length == 0)
			{
				this._categoryLabel.text = "Allowed items: all";
				return;
			}
			this._categoryLabel.text = string.Empty;
			for (int i = 0; i < this._jsonZone.categoryConds.Length; i++)
			{
				string str;
				if (this._guiZones.Categories.TryGetValue(this._jsonZone.categoryConds[i], out str))
				{
					TMP_Text categoryLabel = this._categoryLabel;
					categoryLabel.text += str;
				}
				else if (this._guiZones.TriggerConds.TryGetValue(this._jsonZone.categoryConds[i], out str))
				{
					TMP_Text categoryLabel2 = this._categoryLabel;
					categoryLabel2.text += str;
				}
				if (i < this._jsonZone.categoryConds.Length - 1)
				{
					TMP_Text categoryLabel3 = this._categoryLabel;
					categoryLabel3.text += "/";
				}
			}
			if (this._categoryLabel.text.Length > 22)
			{
				this._categoryLabel.text = this._categoryLabel.text.Substring(0, 22) + "...";
			}
		}

		private void SetCategoryDropdownButtonInteractibility(bool enable)
		{
			this._toggleCategoryMenuButton.interactable = enable;
			this._buttonOverlayImage.SetActive(!enable);
		}

		public void Setup(JsonZone jsonZone, GUIZones guiZone)
		{
			this._jsonZone = jsonZone;
			this._guiZones = guiZone;
			if (this._template == null)
			{
				this._template = Resources.Load<Transform>("GUIShip/GUIZones/chkZoneFlag");
			}
			this._nameInputField.text = jsonZone.strName;
			this._nameInputField.onEndEdit.AddListener(new UnityAction<string>(this.OnZoneNameChanged));
			this.AddToggle("Haul", "IsZoneStockpile", jsonZone.aTileConds.Contains("IsZoneStockpile"));
			this.AddToggle("Barter", "IsZoneBarter", jsonZone.aTileConds.Contains("IsZoneBarter"));
			this.AddToggle("Forbid", "IsZoneForbid", jsonZone.aTileConds.Contains("IsZoneForbid"));
			if (GUIZones.bEditorMode)
			{
				Toggle chkToggle = UnityEngine.Object.Instantiate<Transform>(this._template, this._selectionContainer).GetComponentInChildren<Toggle>();
				chkToggle.name = "TrgOnOwner";
				chkToggle.isOn = jsonZone.bTriggerOnOwner;
				chkToggle.transform.parent.Find("Label").GetComponent<TMP_Text>().text = "TrgOnOwner";
				chkToggle.onValueChanged.AddListener(delegate(bool A_1)
				{
					this.OnTriggerOwnerChanged(chkToggle);
				});
				this.AddToggle("Trigger", "IsZoneTrigger", jsonZone.aTileConds.Contains("IsZoneTrigger"));
			}
			this._toggleColorMenuButton.GetComponent<Image>().color = jsonZone.zoneColor;
			this._ddlOwner.ClearOptions();
			this._ddlOwner.AddOptions(this._guiZones.Ranks.Keys.ToList<string>());
			this._ddlOwner.value = ((jsonZone.strPersonSpec != null) ? this._guiZones.Ranks.Values.ToList<string>().IndexOf(jsonZone.strPersonSpec) : 0);
			this._ddlOwner.onValueChanged.AddListener(new UnityAction<int>(this.OnOwnerChanged));
			this._ddlOwner.RefreshShownValue();
			this._ddlOwner.gameObject.SetActive(GUIZones.bEditorMode);
			this._roleDropDown.ClearOptions();
			this._roleDropDown.AddOptions(this._guiZones.Ranks.Keys.ToList<string>());
			this._roleDropDown.value = ((jsonZone.strTargetPSpec != null) ? this._guiZones.Ranks.Values.ToList<string>().IndexOf(jsonZone.strTargetPSpec) : 0);
			this._roleDropDown.onValueChanged.AddListener(new UnityAction<int>(this.OnRankChanged));
			this._roleDropDown.RefreshShownValue();
			this._deleteZoneButton.onClick.AddListener(new UnityAction(this.OnDeleteZonePressed));
			this._toggleCategoryMenuButton.onClick.AddListener(new UnityAction(this.OnToggleCategoryMenu));
			this._backgroundButton.onClick.AddListener(new UnityAction(this.OnBackgroundClick));
			this._backgroundButton.gameObject.SetActive(false);
			this.UpdateCategoryLabel();
			this.SetCategoryDropdownButtonInteractibility(jsonZone.aTileConds.Any((string x) => x.Contains("IsZoneStockpile") || x.Contains("IsZoneBarter") || x.Contains("IsZoneTrigger")));
			if (jsonZone.aTileConds.Contains("IsZoneTrigger"))
			{
				this.OverwriteCategories();
				this.SetCategoryDropdownButtonInteractibility(true);
			}
			this._tt2.SetData(DataHandler.GetString("GUI_ZONE_EDITBTN_TITLE", false), DataHandler.GetString("GUI_ZONE_EDITBTN_BODY1", false) + GUIActionKeySelector.commandZoneAlternate.KeyName + DataHandler.GetString("GUI_ZONE_EDITBTN_BODY2", false), false);
		}

		public void AutoDeselect(string strName)
		{
			if (this._jsonZone.strName == strName)
			{
				return;
			}
			this._selected = false;
			this._toggleEditMode.GetComponent<Image>().color = Color.white;
		}

		[SerializeField]
		private TMP_InputField _nameInputField;

		[SerializeField]
		private Button _deleteZoneButton;

		[SerializeField]
		private Transform _selectionContainer;

		[Header("Edit")]
		[SerializeField]
		private Button _toggleEditMode;

		[SerializeField]
		private bool _selected;

		[SerializeField]
		private Tooltippable2 _tt2;

		[Header("Color Menu")]
		[SerializeField]
		private Button _toggleColorMenuButton;

		[SerializeField]
		private CanvasGroup _colorSelectionMenuCanvasGroup;

		[SerializeField]
		private Transform _colorButtonContainer;

		[Header("Owner Dropdown")]
		[SerializeField]
		private TMP_Dropdown _ddlOwner;

		[Header("Role Dropdown")]
		[SerializeField]
		private TMP_Dropdown _roleDropDown;

		[Header("Category Dropdown")]
		[SerializeField]
		private Transform _categoryContainer;

		[SerializeField]
		private Button _toggleCategoryMenuButton;

		[SerializeField]
		private GameObject _categoryDropdown;

		[SerializeField]
		private Button _backgroundButton;

		[SerializeField]
		private TMP_Text _categoryLabel;

		[SerializeField]
		private GameObject _buttonOverlayImage;

		private Dictionary<Toggle, string> _dictZones = new Dictionary<Toggle, string>();

		private Dictionary<Toggle, string> _dictCategories = new Dictionary<Toggle, string>();

		private bool _ignoreInput;

		private Transform _template;

		private JsonZone _jsonZone;

		private Transform _colorButtonTemplate;

		private GUIZones _guiZones;

		private Dictionary<Button, Color> _dictColors = new Dictionary<Button, Color>();
	}
}
