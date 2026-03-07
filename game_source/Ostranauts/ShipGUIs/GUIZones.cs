using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs.Zones;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs
{
	[RequireComponent(typeof(CanvasGroup))]
	public class GUIZones : MonoBehaviour
	{
		protected void Awake()
		{
			this.Init();
		}

		private void Start()
		{
			if (GUIActionKeySelector.OnKeyDown != null)
			{
				GUIActionKeySelector.OnKeyDown.AddListener(new UnityAction<KeyCode>(this.ToggleMenuVisibility));
			}
			if (CrewSim.OnTileSelectionUpdated != null)
			{
				CrewSim.OnTileSelectionUpdated.AddListener(new UnityAction<List<CondOwner>>(this.OnTilesSelected));
			}
		}

		private void OnDestroy()
		{
			if (GUIActionKeySelector.OnKeyDown != null)
			{
				GUIActionKeySelector.OnKeyDown.RemoveListener(new UnityAction<KeyCode>(this.ToggleMenuVisibility));
			}
			if (CrewSim.OnTileSelectionUpdated != null)
			{
				CrewSim.OnTileSelectionUpdated.RemoveListener(new UnityAction<List<CondOwner>>(this.OnTilesSelected));
			}
		}

		private void Init()
		{
			this._canvasGroup.alpha = 0f;
			this._canvasGroup.interactable = false;
			this._canvasGroup.blocksRaycasts = false;
			this._btnAddNewZone.onClick.AddListener(new UnityAction(this.OnAddNewZoneClicked));
			this._btnAddNewZone.interactable = false;
			this._closeMenuButton.onClick.AddListener(delegate()
			{
				this.ToggleMenuVisibility(KeyCode.N);
			});
			GUIZones.bEditorMode = CrewSim.bShipEdit;
			this.Ranks = new Dictionary<string, string>();
			if (GUIZones.bEditorMode)
			{
				foreach (string text in DataHandler.dictPersonSpecs.Keys)
				{
					this.Ranks[text] = text;
				}
			}
			else
			{
				Loot loot = DataHandler.GetLoot("TXTZoneList");
				foreach (string text2 in loot.GetLootNames(null, false, null))
				{
					this.Ranks[text2] = text2;
				}
			}
			base.transform.Find("lblOwner").gameObject.SetActive(GUIZones.bEditorMode);
			this.Categories = this.GenerateCategoryDictionary("TXTCategories");
			this.TriggerConds = this.GenerateCategoryDictionary("TXTTriggerCategories");
		}

		private void OnAddNewZoneClicked()
		{
			JsonZone jsonZone = this._tempJsonZone.Clone();
			this.CreateNewZoneEntry(jsonZone);
			this.UpdateTileConditions(jsonZone, null);
			if (this._dictZoneEntries.ContainsKey(jsonZone.strName))
			{
				this._dictZoneEntries[jsonZone.strName] = jsonZone;
			}
			else
			{
				this._dictZoneEntries.Add(jsonZone.strName, jsonZone);
			}
			this._btnAddNewZone.interactable = false;
			this._tempJsonZone = null;
			this._titleContainer.SetActive(this._dictZoneEntries.Count == 0);
			if (CrewSim.coPlayer != null)
			{
				CrewSim.coPlayer.AddCondAmount("TutorialZonesComplete", 1.0, 0.0, 0f);
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			}
		}

		private Dictionary<string, string> GenerateCategoryDictionary(string lootName)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			List<string> allLootNames = DataHandler.GetLoot(lootName).GetAllLootNames();
			if (allLootNames != null)
			{
				foreach (string key in allLootNames)
				{
					JsonCond jsonCond;
					if (DataHandler.dictConds.TryGetValue(key, out jsonCond))
					{
						dictionary[key] = jsonCond.strNameFriendly;
					}
				}
			}
			return dictionary;
		}

		private void CreateNewZoneEntry(JsonZone jz)
		{
			if (this._zoneListTemplate == null)
			{
				this._zoneListTemplate = Resources.Load<Transform>("GUIShip/GUIZones/ZoneListEntry");
			}
			if (jz == null)
			{
				Debug.LogError("Nothing selected");
				return;
			}
			ZoneListEntry component = UnityEngine.Object.Instantiate<Transform>(this._zoneListTemplate, this._zoneListParent).GetComponent<ZoneListEntry>();
			component.Setup(jz, this);
		}

		public static void CloseMenu()
		{
			if (GUIZones.instance == null || GUIZones.instance._canvasGroup.alpha == 0f)
			{
				return;
			}
			GUIZones.instance.ToggleMenuVisibility(KeyCode.N);
		}

		private void ToggleMenuVisibility(KeyCode pressedKey)
		{
			if (pressedKey != KeyCode.N)
			{
				return;
			}
			EventSystem.current.SetSelectedGameObject(null);
			this._selectedZone = null;
			bool flag = this._canvasGroup.alpha <= 0f;
			if (flag)
			{
				if (CrewSim.inventoryGUI != null && CrewSim.inventoryGUI.IsInventoryVisible)
				{
					CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
				}
				if (CrewSim.coPlayer != null)
				{
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					{
						CrewSim.coPlayer.ZeroCondAmount("TutorialZonesNoDorm");
					}
					else if (CrewSim.coPlayer.HasCond("TutorialZonesNoDorm"))
					{
						CrewSim.coPlayer.LogMessage(DataHandler.GetString("GUI_ZONE_TUTORIAL_FORBID", false), "Bad", CrewSim.coPlayer.strName);
						return;
					}
				}
				this._wasGamePaused = CrewSim.Paused;
				IEnumerable<KeyValuePair<string, JsonZone>> enumerable = CrewSim.GetAllLoadedShips().SelectMany((Ship x) => x.mapZones);
				foreach (KeyValuePair<string, JsonZone> keyValuePair in enumerable)
				{
					if (GUIZones.bEditorMode || !(keyValuePair.Value.strPersonSpec != GUIZones.DefaultPS) || (keyValuePair.Value.strPersonSpec != null && this.Ranks.ContainsKey(keyValuePair.Value.strPersonSpec)))
					{
						if (!GUIZones.bEditorMode)
						{
							if (keyValuePair.Value.aTileConds.Any((string x) => x.Contains("Trigger")))
							{
								continue;
							}
						}
						JsonZone jsonZone = keyValuePair.Value.Clone();
						this.CreateNewZoneEntry(jsonZone);
						this._dictZoneEntries[jsonZone.strName] = jsonZone;
					}
				}
				if (CrewSim.coPlayer != null)
				{
					CrewSim.coPlayer.SetCondAmount("TutorialZonesStart", 1.0, 0.0);
					CrewSim.coPlayer.SetCondAmount("TutorialZonesOpened", 1.0, 0.0);
					MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
					if (!CrewSim.coPlayer.HasCond("TutorialZonesComplete"))
					{
						Objective objective = new Objective(CrewSim.coPlayer, "Create a Zone", "TIsTutorialZonesComplete");
						objective.strDisplayDesc = "Select tiles on the ground and press the ADD NEW ZONE button";
						objective.strDisplayDescComplete = "Zone created";
						objective.bTutorial = true;
						MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
					}
				}
			}
			else
			{
				CrewSim.objInstance.UnselectNonHumanTargets();
				this._dictZoneEntries.Clear();
				this._tempJsonZone = null;
				IEnumerator enumerator2 = this._zoneListParent.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						object obj = enumerator2.Current;
						Transform transform = (Transform)obj;
						UnityEngine.Object.Destroy(transform.gameObject);
					}
				}
				finally
				{
					IDisposable disposable;
					if ((disposable = (enumerator2 as IDisposable)) != null)
					{
						disposable.Dispose();
					}
				}
				CanvasManager.CleanupDropDownBlockers(base.gameObject);
			}
			CrewSim.Paused = (flag || this._wasGamePaused);
			CrewSim.ZoneMenuOpen = flag;
			if (flag)
			{
				CrewSim.OnRightClick.Invoke(null);
				GUIZones.instance = this;
			}
			else
			{
				GUIZones.instance = null;
			}
			TileUtils.ToggleShipTileVisibility(flag, this.GetDockedShipTiles(), false);
			if (flag)
			{
				CanvasManager.ShowCanvasGroup(this._canvasGroup);
			}
			else
			{
				CanvasManager.HideCanvasGroup(this._canvasGroup);
			}
			this._titleContainer.SetActive(this._dictZoneEntries.Count == 0);
		}

		private List<Tile> GetDockedShipTiles()
		{
			List<Tile> list = new List<Tile>();
			foreach (Ship ship in CrewSim.GetAllLoadedShips())
			{
				if (ship.aTiles != null)
				{
					list.AddRange(ship.aTiles);
				}
			}
			return list;
		}

		public static Ship GetLoadedShipByRegId(string regId)
		{
			return CrewSim.GetLoadedShipByRegId(regId);
		}

		private void OnTilesSelected(List<CondOwner> selectedCOs)
		{
			bool held = GUIActionKeySelector.commandZoneAlternate.Held;
			if (selectedCOs != null && selectedCOs.Count != 0)
			{
				if (!selectedCOs.Any((CondOwner x) => x.ship == null))
				{
					if (selectedCOs.Count > 1)
					{
						if ((from x in selectedCOs
						where x.GetComponent<Tile>() != null
						select x into item
						select item.ship.strRegID into x
						where !string.IsNullOrEmpty(x)
						select x).Distinct<string>().Skip(1).Any<string>())
						{
							this._btnAddNewZone.interactable = false;
							this._selectedTilesLabel.text = "multiple ships selected";
							return;
						}
					}
					List<int> list = new List<int>();
					this._tempJsonZone = new JsonZone
					{
						aTiles = new int[0],
						aTileConds = new string[0],
						strRegID = selectedCOs[0].ship.strRegID,
						categoryConds = new string[0]
					};
					bool flag = false;
					foreach (CondOwner condOwner in selectedCOs)
					{
						if (!(condOwner == null))
						{
							Tile component = condOwner.GetComponent<Tile>();
							if (!(component == null))
							{
								if (list.IndexOf(component.Index) < 0)
								{
									if (component.jZone != null)
									{
										if (this._selectedZone == null || !(this._selectedZone.strName == component.jZone.strName))
										{
											this._tempJsonZone = component.jZone.Clone();
											flag = true;
											list = this._tempJsonZone.aTiles.ToList<int>();
											break;
										}
									}
									this._tempJsonZone.strRegID = condOwner.ship.strRegID;
									list.Add(component.Index);
								}
							}
						}
					}
					if (flag || list.Count == 0)
					{
						this._tempJsonZone = null;
						this._btnAddNewZone.interactable = false;
						this._selectedTilesLabel.text = ((!flag) ? (list.Count + " tiles selected") : "Zone selected");
						return;
					}
					if (this._selectedZone != null)
					{
						if (held)
						{
							this._selectedZone.RemoveTiles(list);
							this._selectedTilesLabel.text = "Zone reduced";
							GUIZones.DeleteTilesFromZone(this._selectedZone, true);
						}
						else
						{
							if (this._selectedZone.strRegID != this._tempJsonZone.strRegID)
							{
								this._selectedTilesLabel.text = "Zone not expanded. Tiles on different ship.";
								return;
							}
							this._selectedZone.AddTiles(list);
							this._selectedTilesLabel.text = "Zone expanded";
						}
						this.UpdateTileConditions(this._selectedZone, null);
						this._tempJsonZone = null;
						this._btnAddNewZone.interactable = false;
						return;
					}
					this._tempJsonZone.aTiles = list.ToArray();
					this._tempJsonZone.strName = this.GetNewZoneName("zone");
					if (CrewSim.bShipEdit)
					{
						this._tempJsonZone.strPersonSpec = GUIZones.DefaultPSEditor;
					}
					else
					{
						this._tempJsonZone.strPersonSpec = GUIZones.DefaultPS;
					}
					if (CrewSim.bShipEdit)
					{
						this._tempJsonZone.strTargetPSpec = GUIZones.DefaultPSEditor;
					}
					else
					{
						this._tempJsonZone.strTargetPSpec = GUIZones.DefaultPSGroup;
					}
					this._tempJsonZone.zoneColor = this.GetNewZoneColor();
					this._btnAddNewZone.interactable = true;
					this._selectedTilesLabel.text = list.Count + " tiles selected";
					return;
				}
			}
			this._btnAddNewZone.interactable = false;
			this._selectedTilesLabel.text = "0 tiles selected";
		}

		private string GetNewZoneName(string baseName = "zone")
		{
			int num = 1;
			string text = baseName + " " + num;
			while (CrewSim.shipCurrentLoaded.mapZones.ContainsKey(text) || this._dictZoneEntries.ContainsKey(text))
			{
				num++;
				text = baseName + " " + num;
			}
			return text;
		}

		private Color GetNewZoneColor()
		{
			return (this.AvailableColors.Count <= 1) ? Color.magenta : this.AvailableColors[UnityEngine.Random.Range(0, this.AvailableColors.Count)];
		}

		private void OnCTChange(TMP_Dropdown ddl)
		{
			if (CrewSim.aSelected.Count == 0)
			{
				return;
			}
			JsonZone jsonZone = null;
			TMP_InputField component = base.transform.Find("tboxName").GetComponent<TMP_InputField>();
			if (CrewSim.shipCurrentLoaded.mapZones.TryGetValue(component.text, out jsonZone))
			{
				jsonZone.strPersonSpec = ddl.options[ddl.value].text;
			}
		}

		public static void DeleteTilesFromZone(JsonZone jz, bool bOldTiles = false)
		{
			Ship loadedShipByRegId = GUIZones.GetLoadedShipByRegId(jz.strRegID);
			if (loadedShipByRegId == null)
			{
				Debug.Log("No ship found for zone, cannot delete tiles");
				return;
			}
			List<Tile> aTiles = loadedShipByRegId.aTiles;
			if (aTiles == null)
			{
				Debug.Log("No player ship tiles found, cannot delete tiles");
				return;
			}
			int[] array = jz.aTiles;
			if (bOldTiles)
			{
				array = jz.aOldTiles;
			}
			int[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				int tileIndex = array2[i];
				Tile tile = aTiles.FirstOrDefault((Tile ti) => ti.Index == tileIndex);
				if (!(tile == null))
				{
					CondOwner coProps = tile.coProps;
					Tile component = coProps.GetComponent<Tile>();
					if (!(component == null))
					{
						component.SetColor(Tile.clrDefault);
						foreach (string text in jz.aTileConds)
						{
							if (!string.IsNullOrEmpty(text))
							{
								component.coProps.ZeroCondAmount(text);
							}
						}
						component.jZone = null;
					}
				}
			}
			jz.aOldTiles = new int[0];
		}

		private void RenameZone(JsonZone jz, string oldName)
		{
			if (jz == null || string.IsNullOrEmpty(jz.strRegID) || string.IsNullOrEmpty(oldName))
			{
				return;
			}
			Ship loadedShipByRegId = GUIZones.GetLoadedShipByRegId(jz.strRegID);
			if (loadedShipByRegId != null && loadedShipByRegId.mapZones.Remove(oldName))
			{
				loadedShipByRegId.mapZones[jz.strName] = jz;
				foreach (int index in jz.aTiles)
				{
					CondOwner coProps = loadedShipByRegId.aTiles[index].coProps;
					Tile component = coProps.GetComponent<Tile>();
					if (!(component == null) && component.jZone != null)
					{
						component.jZone = jz;
					}
				}
				return;
			}
			Debug.LogWarning("No matching zone in ship to rename");
		}

		private void UpdateTile(CondOwner co, JsonZone jz, List<string> aCondRemoves = null)
		{
			Tile component = co.GetComponent<Tile>();
			if (component == null)
			{
				return;
			}
			component.SetColor(Tile.clrDefault);
			if (aCondRemoves != null)
			{
				foreach (string text in aCondRemoves)
				{
					if (!string.IsNullOrEmpty(text))
					{
						component.coProps.ZeroCondAmount(text);
					}
				}
			}
			if (jz.aTileConds != null)
			{
				foreach (string text2 in jz.aTileConds)
				{
					if (!string.IsNullOrEmpty(text2) && (!component.coProps.HasCond(text2) || component.coProps.GetCondAmount(text2) <= 0.0))
					{
						component.coProps.AddCondAmount(text2, 1.0, 0.0, 0f);
					}
				}
				component.SetColor(jz.zoneColor);
				component.jZone = jz;
				co.ship.mapZones[jz.strName] = jz;
			}
		}

		public void UpdateTileConditions(JsonZone jz, List<string> aCondRemoves = null)
		{
			Ship loadedShipByRegId = GUIZones.GetLoadedShipByRegId(jz.strRegID);
			if (loadedShipByRegId == null || loadedShipByRegId.aTiles == null)
			{
				Debug.LogError("No ship tiles loaded");
				return;
			}
			foreach (int index in jz.aTiles)
			{
				CondOwner coProps = loadedShipByRegId.aTiles[index].coProps;
				this.UpdateTile(coProps, jz, aCondRemoves);
			}
		}

		public void DeleteZone(string zoneToDelete)
		{
			JsonZone jsonZone = null;
			if (this._dictZoneEntries.TryGetValue(zoneToDelete, out jsonZone))
			{
				GUIZones.DeleteTilesFromZone(jsonZone, false);
				Ship loadedShipByRegId = GUIZones.GetLoadedShipByRegId(jsonZone.strRegID);
				if (loadedShipByRegId != null)
				{
					loadedShipByRegId.mapZones.Remove(jsonZone.strName);
				}
				this._dictZoneEntries.Remove(zoneToDelete);
				this._titleContainer.SetActive(this._dictZoneEntries.Count == 0);
			}
		}

		public string RenameZone(string oldName, string newZoneName)
		{
			string text = (!this._dictZoneEntries.ContainsKey(newZoneName)) ? newZoneName : this.GetNewZoneName(newZoneName);
			JsonZone jsonZone;
			if (this._dictZoneEntries.TryGetValue(oldName, out jsonZone))
			{
				jsonZone.strName = text;
				this.RenameZone(jsonZone, oldName);
				this._dictZoneEntries.Remove(oldName);
				this._dictZoneEntries[text] = jsonZone;
			}
			return text;
		}

		public void SelectZone(string zoneName, bool selected)
		{
			JsonZone selectedZone;
			if (this._dictZoneEntries.TryGetValue(zoneName, out selectedZone))
			{
				if (selected)
				{
					this._selectedZone = selectedZone;
				}
				else
				{
					this._selectedZone = null;
				}
			}
			else
			{
				Debug.LogWarning("Tried to select unrecognised zone: " + zoneName);
			}
			foreach (ZoneListEntry zoneListEntry in this._zoneListParent.GetComponentsInChildren<ZoneListEntry>())
			{
				zoneListEntry.AutoDeselect(zoneName);
			}
		}

		[SerializeField]
		public List<Color> AvailableColors;

		public Dictionary<string, string> Ranks = new Dictionary<string, string>();

		public Dictionary<string, string> Categories = new Dictionary<string, string>();

		public Dictionary<string, string> TriggerConds = new Dictionary<string, string>();

		public static GUIZones instance;

		public static bool bEditorMode;

		[SerializeField]
		private Transform _zoneListParent;

		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private Button _btnAddNewZone;

		[SerializeField]
		private TMP_Text _selectedTilesLabel;

		[SerializeField]
		private GameObject _titleContainer;

		[SerializeField]
		private Button _closeMenuButton;

		private const KeyCode ZONEKEY = KeyCode.N;

		public static string DefaultPS = "ZonePlayer";

		public static string DefaultPSEditor = "ZoneAnyone";

		public static string DefaultPSGroup = "ZoneCaptainAndCrew";

		private Dictionary<string, JsonZone> _dictZoneEntries = new Dictionary<string, JsonZone>();

		private Transform _zoneListTemplate;

		private JsonZone _tempJsonZone;

		private JsonZone _selectedZone;

		private bool _wasGamePaused;
	}
}
