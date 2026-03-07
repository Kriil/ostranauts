using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.ShipEdit
{
	// Ship editor screen for authoring/editing ship templates and construction
	// stages, including metadata fields, stage save/delete, and unique tags.
	public class GUIShipEdit : MonoSingleton<GUIShipEdit>
	{
		// Unity setup: wires all major editor buttons, text-entry handlers, and the
		// construction-stage save/delete events.
		private new void Awake()
		{
			GUIShipEdit.OnShipEditStageSaveEvent = new OnShipEditStageEvent();
			GUIShipEdit.OnShipEditStageDeleteEvent = new OnShipEditStageEvent();
			GUIShipEdit.OnShipEditStageSaveEvent.AddListener(new UnityAction<int>(this.OnSaveStage));
			GUIShipEdit.OnShipEditStageDeleteEvent.AddListener(new UnityAction<int>(this.OnDeleteStage));
			this.btnShipDetails.onClick.AddListener(new UnityAction(this.OnShipDetailsButtonDown));
			this.btnConstructionStage.onClick.AddListener(new UnityAction(this.OnConstructionStagesButtonDown));
			this.btnLoad.onClick.AddListener(delegate()
			{
				this.ResetSidePanel();
				this.ChooseShipName(new UnityAction<string>(CrewSim.objInstance.LoadShipEdit));
			});
			this.btnSave.onClick.AddListener(delegate()
			{
				this.SaveShipEdit((CrewSim.jsonShip == null) ? null : CrewSim.jsonShip.strName);
			});
			this.btnSaveAs.onClick.AddListener(delegate()
			{
				this.ResetSidePanel();
				this.ChooseShipName(new UnityAction<string>(this.SaveShipEdit));
			});
			this.btnUniques.onClick.AddListener(delegate()
			{
				this.HideUnhideUniques();
			});
			this.uniquePanel.TagSelected.onClick.AddListener(new UnityAction(this.TagNewUnique));
			this.uniquePanel.UnTagSelected.onClick.AddListener(new UnityAction(this.UntagUnique));
			this.SetTypingHandler(this.tboxMake);
			this.SetTypingHandler(this.tboxLaw);
			this.SetTypingHandler(this.tboxModel);
			this.SetTypingHandler(this.tboxYear);
			this.SetTypingHandler(this.tboxDesignation);
			this.SetTypingHandler(this.tboxName);
			this.SetTypingHandler(this.tboxOrigin);
			this.SetTypingHandler(this.tboxDescription);
			this.SetEditorVisibility(true);
			this.OnShipDetailsButtonDown();
			this.PopulateShipEditTriggerList();
		}

		// Keeps the editor UI in sync with the current ship-edit state, including
		// temporary debug hotkeys and construction-stage save restrictions.
		private void Update()
		{
			if (this.timer > 0f)
			{
				this.timer -= Time.deltaTime;
			}
			else
			{
				this.index = 0;
			}
			if (Input.GetKeyDown(KeyCode.K))
			{
				Vector3 mousePosition = Input.mousePosition;
				Ray ray = UnityEngine.Object.FindObjectOfType<Camera>().ScreenPointToRay(mousePosition);
				RaycastHit[] source = Physics.RaycastAll(ray, 100f);
				List<CondOwner> list = new List<CondOwner>();
				foreach (RaycastHit raycastHit in from go in source
				orderby go.distance
				select go)
				{
					CondOwner component = raycastHit.transform.GetComponent<CondOwner>();
					if (component != null)
					{
						list.Add(component);
					}
				}
				if (list.Count > 0)
				{
					CrewSim.objInstance.SetBracketTarget(list[Mathf.Min(this.index, list.Count - 1)].strID, false, false);
					this.timer = 0.5f;
				}
				this.index++;
			}
			if (CrewSim.jsonShip == null && this.btnConstructionStage.interactable)
			{
				this.btnConstructionStage.interactable = false;
				if (this.cgConstructionStages.alpha > 0f)
				{
					this.OnConstructionStagesButtonDown();
				}
			}
			else if (CrewSim.jsonShip != null && !this.btnConstructionStage.interactable)
			{
				this.btnConstructionStage.interactable = true;
			}
			if (CrewSim.jsonShip != null && CrewSim.jsonShip.nConstructionProgress < 100 && CrewSim.jsonShip.aConstructionTemplates != null)
			{
				this.btnSave.interactable = false;
				this.btnSaveAs.interactable = false;
			}
			else
			{
				this.btnSave.interactable = true;
				this.btnSaveAs.interactable = true;
			}
		}

		private void OnDestroy()
		{
			GUIShipEdit.OnShipEditStageSaveEvent.RemoveAllListeners();
			GUIShipEdit.OnShipEditStageDeleteEvent.RemoveAllListeners();
		}

		// Removes one construction stage from the underlying JsonShip template.
		private void OnDeleteStage(int progress)
		{
			JsonShip jsonShip;
			if (DataHandler.dictShips.TryGetValue(CrewSim.jsonShip.strName, out jsonShip))
			{
				List<JsonShipConstructionTemplate> list = new List<JsonShipConstructionTemplate>();
				if (jsonShip.aConstructionTemplates != null)
				{
					list = jsonShip.aConstructionTemplates.ToList<JsonShipConstructionTemplate>();
				}
				for (int i = list.Count - 1; i >= 0; i--)
				{
					JsonShipConstructionTemplate jsonShipConstructionTemplate = list[i];
					if (jsonShipConstructionTemplate.nProgress == progress)
					{
						list.RemoveAt(i);
						list.TrimExcess();
						break;
					}
				}
				jsonShip.aConstructionTemplates = list.ToArray();
				DataHandler.dictShips[CrewSim.jsonShip.strName] = jsonShip;
				jsonShip.aConstructionTemplates = list.ToArray();
				CrewSim.jsonShip = jsonShip;
				CrewSim.shipCurrentLoaded.json = jsonShip;
				DataHandler.DataToJsonStreaming<JsonShip>(new Dictionary<string, JsonShip>
				{
					{
						jsonShip.strName,
						jsonShip
					}
				}, "/ships/" + jsonShip.strName + ".json", false, string.Empty);
				DataHandler.RemoveFile(string.Concat(new string[]
				{
					DataHandler.strAssetPath,
					"images/ships/",
					jsonShip.strName,
					"/",
					jsonShip.strName,
					"_",
					progress.ToString("N0"),
					".png"
				}));
			}
			else
			{
				Debug.LogWarning("No Ship found in dict. Nothing deleted");
			}
			this.BuildConstructionList(false);
		}

		private void OnSaveStage(int progress)
		{
			if (CrewSim.shipCurrentLoaded == null || CrewSim.jsonShip == null)
			{
				return;
			}
			JsonShip jsonShip = new JsonShip();
			CrewSim.shipCurrentLoaded.SaveCOs(false, jsonShip, null);
			JsonShipConstructionTemplate jsonShipConstructionTemplate = new JsonShipConstructionTemplate(jsonShip, progress);
			JsonShip jsonShip2;
			if (DataHandler.dictShips.TryGetValue(CrewSim.jsonShip.strName, out jsonShip2))
			{
				List<JsonShipConstructionTemplate> list = new List<JsonShipConstructionTemplate>();
				if (jsonShip2.aConstructionTemplates != null)
				{
					list = jsonShip2.aConstructionTemplates.ToList<JsonShipConstructionTemplate>();
				}
				if (list.Any((JsonShipConstructionTemplate x) => x.nProgress == progress))
				{
					this.OnDeleteStage(progress);
					this.OnSaveStage(progress);
					return;
				}
				list.Add(jsonShipConstructionTemplate);
				jsonShip2.aConstructionTemplates = list.ToArray();
				jsonShip2.strTemplateName = jsonShip2.strName;
				jsonShip2.nConstructionProgress = 100;
				DataHandler.DataToJsonStreaming<JsonShip>(new Dictionary<string, JsonShip>
				{
					{
						jsonShip2.strName,
						jsonShip2
					}
				}, "/ships/" + jsonShip2.strName + ".json", false, string.Empty);
				MonoSingleton<ScreenshotUtil>.Instance.CreateAndSaveSingleImage(CrewSim.shipCurrentLoaded, DataHandler.strAssetPath + "images/ships/" + jsonShip2.strName + "/", jsonShip2.strName + "_" + jsonShipConstructionTemplate.nProgress.ToString("N0"));
				CrewSim.shipCurrentLoaded.nConstructionProgress = progress;
				CrewSim.jsonShip.nConstructionProgress = progress;
			}
			else
			{
				Debug.LogWarning("No Ship found in dict. Nothing saved");
			}
			this.BuildConstructionList(true);
		}

		private void OnShipDetailsButtonDown()
		{
			if (this.cgShipDetails.alpha > 0f)
			{
				this.cgShipDetails.alpha = 0f;
				this.cgConstructionStages.blocksRaycasts = false;
				this.cgConstructionStages.alpha = 0f;
				this.cgShipDetails.blocksRaycasts = false;
			}
			else
			{
				this.cgShipDetails.alpha = 1f;
				this.cgShipDetails.interactable = true;
				this.cgShipDetails.blocksRaycasts = true;
				this.cgConstructionStages.interactable = false;
				this.cgConstructionStages.alpha = 0f;
				this.cgConstructionStages.blocksRaycasts = false;
			}
		}

		private void OnConstructionStagesButtonDown()
		{
			if (this.cgConstructionStages.alpha > 0f)
			{
				this.cgConstructionStages.alpha = 0f;
				this.cgConstructionStages.blocksRaycasts = false;
				this.cgShipDetails.alpha = 0f;
				this.cgShipDetails.blocksRaycasts = false;
			}
			else
			{
				this.cgConstructionStages.alpha = 1f;
				this.cgConstructionStages.blocksRaycasts = true;
				this.cgShipDetails.alpha = 0f;
				this.cgShipDetails.interactable = false;
				this.cgShipDetails.blocksRaycasts = false;
				this.cgConstructionStages.interactable = true;
				this.BuildConstructionList(false);
			}
		}

		private void HideUnhideUniques()
		{
			GameObject gameObject = this.uniquePanel.gameObject;
			if (gameObject.activeInHierarchy)
			{
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);
			}
		}

		private void ResetSidePanel()
		{
			if (this.cgConstructionStages.alpha > 0f)
			{
				this.OnConstructionStagesButtonDown();
			}
			else if (this.cgShipDetails.alpha > 0f)
			{
				this.OnShipDetailsButtonDown();
			}
		}

		public void SaveShipEdit(string strNewName)
		{
			if (string.IsNullOrEmpty(strNewName))
			{
				return;
			}
			string strName = CrewSim.shipCurrentLoaded.json.strName;
			CrewSim.jsonShip = CrewSim.shipCurrentLoaded.GetJSON(strNewName, false, null);
			if (!string.IsNullOrEmpty(CrewSim.jsonShip.strTemplateName))
			{
				CrewSim.jsonShip.strTemplateName = strNewName;
			}
			CrewSim.jsonShip = this.ApplyStrings(CrewSim.jsonShip);
			if (this.shipUniques.Count > 0)
			{
				JsonShipUniques[] array = new JsonShipUniques[this.uniquePanel.scrollRectContent.childCount];
				for (int i = 0; i < this.uniquePanel.scrollRectContent.childCount; i++)
				{
					GUIShipUnique component = this.uniquePanel.scrollRectContent.GetChild(i).GetComponent<GUIShipUnique>();
					JsonShipUniques jsonShipUniques = new JsonShipUniques
					{
						strCOID = component.strID
					};
					string text = component.TMP_InputField.text;
					string[] array2 = text.Split(new char[]
					{
						','
					});
					for (int j = 0; j < array2.Length; j++)
					{
						array2[j] = array2[j].Trim(new char[]
						{
							' '
						});
					}
					jsonShipUniques.aConds = array2;
					array[i] = jsonShipUniques;
				}
				CrewSim.jsonShip.aUniques = array;
			}
			else
			{
				CrewSim.jsonShip.aUniques = null;
			}
			DataHandler.dictShips[CrewSim.jsonShip.strName] = CrewSim.jsonShip;
			DataHandler.DataToJsonStreaming<JsonShip>(new Dictionary<string, JsonShip>
			{
				{
					CrewSim.jsonShip.strName,
					CrewSim.jsonShip
				}
			}, "/ships/" + CrewSim.jsonShip.strName + ".json", false, string.Empty);
			string savePath = DataHandler.strAssetPath + "images/ships/" + CrewSim.jsonShip.strName + "/";
			MonoSingleton<ScreenshotUtil>.Instance.CreateAndSave(CrewSim.jsonShip, savePath);
			this.CopyStagePreviewImages(strName, strNewName);
			this.lblShipName.text = CrewSim.jsonShip.strName;
		}

		private void CopyStagePreviewImages(string previousName, string newName)
		{
			if (string.IsNullOrEmpty(previousName) || string.IsNullOrEmpty(newName) || previousName == newName)
			{
				return;
			}
			string path = DataHandler.strAssetPath + "images/ships/" + previousName + "/";
			if (!Directory.Exists(path))
			{
				return;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			string pattern = "_\\d+\\.png$";
			foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.png"))
			{
				if (fileInfo != null)
				{
					if (!(fileInfo.Name == previousName) && Regex.IsMatch(fileInfo.Name, pattern))
					{
						string newFileName = fileInfo.Name.Replace(previousName, newName);
						DataHandler.CopyFile(fileInfo.FullName, DataHandler.strAssetPath + "images/ships/" + newName + "/", newFileName, false);
					}
				}
			}
		}

		private void ChooseShipName(UnityAction<string> act)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load("prefabPnlOptionSelect") as GameObject, base.transform, false);
			List<string> list = new List<string>();
			foreach (string item in DataHandler.dictShips.Keys)
			{
				list.Add(item);
			}
			list.Sort();
			gameObject.GetComponent<GUIOptionSelect>().Init(list, act);
		}

		private void PopulateShipEditTriggerList()
		{
			List<string> list = new List<string>
			{
				"Blank",
				"TIsInstalled",
				"TIsNotInstalled",
				"TIsSystem"
			};
			foreach (string text in DataHandler.dictCTs.Keys)
			{
				if (text.IndexOf("TIs") >= 0 || text.IndexOf("TNot") >= 0)
				{
					list.Add(text);
				}
			}
			this.ddlFilter.ClearOptions();
			this.ddlFilter.AddOptions(list);
			this.ddlFilter.onValueChanged.AddListener(delegate(int A_1)
			{
				CrewSim.objInstance.PopulatePartList(this.ddlFilter.options[this.ddlFilter.value].text);
			});
			this.ddlFilter.value = 0;
			this.ddlFilter.RefreshShownValue();
		}

		private void SetEditorVisibility(bool bEditor)
		{
			if (this.tboxLaw != null && this.txtLaw != null)
			{
				this.tboxLaw.gameObject.SetActive(bEditor);
				this.txtLaw.SetActive(bEditor);
			}
			this.btnConstructionStage.gameObject.SetActive(bEditor);
			this.btnConstructionStage.onClick.RemoveAllListeners();
			if (bEditor)
			{
				this.btnConstructionStage.onClick.AddListener(new UnityAction(this.OnConstructionStagesButtonDown));
			}
		}

		public void BuildConstructionList(bool delayImages = false)
		{
			IEnumerator enumerator = this.tfSRStageContainer.GetEnumerator();
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
			JsonShip jsonShip = CrewSim.jsonShip;
			if (jsonShip != null)
			{
				int num = 100;
				GUIShipEditStageEntry guishipEditStageEntry = UnityEngine.Object.Instantiate<GUIShipEditStageEntry>(this.prefabConstructionStage, this.tfSRStageContainer);
				JsonShip jsonShip2 = DataHandler.GetShip(jsonShip.strName).Clone();
				guishipEditStageEntry.SetData(jsonShip2.strName, new JsonShipConstructionTemplate
				{
					nProgress = num,
					aItems = jsonShip2.aItems,
					aShallowPSpecs = jsonShip2.aShallowPSpecs
				}, false);
				if (jsonShip2.aConstructionTemplates != null)
				{
					foreach (JsonShipConstructionTemplate jsonShipConstructionTemplate in jsonShip2.aConstructionTemplates)
					{
						guishipEditStageEntry = UnityEngine.Object.Instantiate<GUIShipEditStageEntry>(this.prefabConstructionStage, this.tfSRStageContainer);
						guishipEditStageEntry.SetData(jsonShip2.strName, jsonShipConstructionTemplate, delayImages);
						num = jsonShipConstructionTemplate.nProgress;
					}
				}
				guishipEditStageEntry = UnityEngine.Object.Instantiate<GUIShipEditStageEntry>(this.prefabConstructionStage, this.tfSRStageContainer);
				guishipEditStageEntry.SetData(num);
			}
			if (this.ShowHelp)
			{
				UnityEngine.Object.Instantiate<GUIShipStageDescriptionRow>(this.prefabDescriptionRow, this.tfSRStageContainer);
			}
			ScrollRect component = this.tfSRStageContainer.parent.parent.GetComponent<ScrollRect>();
			ScrollRect.ScrollbarVisibility verticalScrollbarVisibility = component.verticalScrollbarVisibility;
			this.tfSRStageContainer.parent.parent.GetComponent<ScrollRect>().verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
			component.verticalScrollbarVisibility = verticalScrollbarVisibility;
		}

		private void SetTypingHandler(TMP_InputField inputField)
		{
			inputField.onSelect.AddListener(delegate(string A_0)
			{
				CrewSim.StartTyping();
			});
			inputField.onDeselect.AddListener(delegate(string A_0)
			{
				CrewSim.EndTyping();
			});
		}

		public void LoadShipEdit(JsonShip jsonShip)
		{
			this.shipUniques.Clear();
			IEnumerator enumerator = this.uniquePanel.scrollRectContent.transform.GetEnumerator();
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
			if (jsonShip == null)
			{
				this.tboxName.text = "$TEMPLATE";
				this.tboxOrigin.text = "$TEMPLATE";
				this.lblShipName.text = string.Empty;
			}
			else
			{
				this.tboxMake.text = jsonShip.make;
				this.tboxModel.text = jsonShip.model;
				this.tboxYear.text = jsonShip.year;
				this.tboxDesignation.text = jsonShip.designation;
				this.tboxName.text = jsonShip.publicName;
				this.tboxOrigin.text = jsonShip.origin;
				this.tboxDescription.text = jsonShip.description;
				this.lblShipName.text = jsonShip.strName;
				if (jsonShip.aUniques != null)
				{
					this.shipUniques.AddRange(jsonShip.aUniques);
				}
				(this.uniquePanel.scrollRectContent.transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)(100 * this.shipUniques.Count));
				using (List<JsonShipUniques>.Enumerator enumerator2 = this.shipUniques.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						JsonShipUniques jsonShipUniques = enumerator2.Current;
						if (!DataHandler.mapCOs.ContainsKey(jsonShipUniques.strCOID))
						{
							Debug.LogError("Missing unique CO key?");
						}
						else
						{
							CondOwner condOwner = DataHandler.mapCOs[jsonShipUniques.strCOID];
							GUIShipUnique guishipUnique = UnityEngine.Object.Instantiate<GUIShipUnique>(this.uniquePanel.prefab, this.uniquePanel.scrollRectContent);
							guishipUnique.IDDisplay.text = condOwner.strNameFriendly + " " + jsonShipUniques.strCOID;
							guishipUnique.strID = condOwner.strID;
							string text = string.Empty;
							if (jsonShipUniques.aConds != null)
							{
								for (int i = 0; i < jsonShipUniques.aConds.Length; i++)
								{
									if (i > 0)
									{
										text += ", ";
									}
									text += jsonShipUniques.aConds[i];
								}
							}
							guishipUnique.TMP_InputField.text = text;
							guishipUnique.Image.texture = DataHandler.mapCOs[jsonShipUniques.strCOID].Item.rend.material.mainTexture;
							Button button = guishipUnique.Image.gameObject.AddComponent<Button>();
							button.onClick.AddListener(delegate()
							{
								CrewSim.objInstance.SetBracketTarget(jsonShipUniques.strCOID, false, false);
							});
						}
					}
				}
				for (int j = 0; j < this.uniquePanel.scrollRectContent.childCount; j++)
				{
					this.uniquePanel.scrollRectContent.transform.GetChild(j).localPosition = new Vector3(this.uniquePanel.scrollRectContent.transform.GetChild(j).localPosition.x, (float)(-(float)(j + 1) * 100 + 50));
				}
			}
		}

		public void UntagUnique()
		{
			if (CrewSim.GetBracketTarget() != null)
			{
				CondOwner bracketTarget = CrewSim.GetBracketTarget();
				int num = -1;
				for (int i = 0; i < this.shipUniques.Count; i++)
				{
					if (this.shipUniques[i].strCOID == bracketTarget.strID)
					{
						num = i;
						break;
					}
				}
				if (num > -1)
				{
					this.shipUniques.RemoveAt(num);
					UnityEngine.Object.Destroy(this.uniquePanel.scrollRectContent.GetChild(num).gameObject);
				}
			}
		}

		public void TagNewUnique()
		{
			if (CrewSim.GetBracketTarget() != null)
			{
				CondOwner selected = CrewSim.GetBracketTarget();
				foreach (JsonShipUniques jsonShipUniques in this.shipUniques)
				{
					if (jsonShipUniques.strCOID == selected.strID)
					{
						Debug.Log("Already tagged");
						return;
					}
				}
				if (this.uniquePanel.scrollRectContent.transform.childCount > 0)
				{
					RectTransform rectTransform = this.uniquePanel.scrollRectContent.transform.GetChild(this.uniquePanel.scrollRectContent.transform.childCount - 1) as RectTransform;
				}
				GUIShipUnique guishipUnique = UnityEngine.Object.Instantiate<GUIShipUnique>(this.uniquePanel.prefab, this.uniquePanel.scrollRectContent);
				guishipUnique.IDDisplay.text = selected.strNameFriendly + " " + selected.strID;
				guishipUnique.strID = selected.strID;
				guishipUnique.Image.texture = selected.Item.rend.material.mainTexture;
				Button button = guishipUnique.Image.gameObject.AddComponent<Button>();
				button.onClick.AddListener(delegate()
				{
					CrewSim.objInstance.SetBracketTarget(selected.strID, false, false);
				});
				JsonShipUniques item = new JsonShipUniques
				{
					strCOID = selected.strID
				};
				this.shipUniques.Add(item);
				(this.uniquePanel.scrollRectContent.transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)(100 * this.shipUniques.Count));
				for (int i = 0; i < this.uniquePanel.scrollRectContent.childCount; i++)
				{
					this.uniquePanel.scrollRectContent.transform.GetChild(i).localPosition = new Vector3(this.uniquePanel.scrollRectContent.transform.GetChild(i).localPosition.x, (float)(-(float)(i + 1) * 100 + 50));
				}
			}
		}

		public string GetLaw()
		{
			return (!(this.tboxLaw != null)) ? string.Empty : this.tboxLaw.text;
		}

		private JsonShip ApplyStrings(JsonShip jShip)
		{
			if (jShip != null)
			{
				jShip.make = this.tboxMake.text;
				jShip.model = this.tboxModel.text;
				jShip.year = this.tboxYear.text;
				jShip.designation = this.tboxDesignation.text;
				jShip.publicName = this.tboxName.text;
				jShip.origin = this.tboxOrigin.text;
				jShip.description = this.tboxDescription.text;
			}
			return jShip;
		}

		public static OnShipEditStageEvent OnShipEditStageSaveEvent;

		public static OnShipEditStageEvent OnShipEditStageDeleteEvent;

		[Header("Main")]
		[SerializeField]
		private Button btnLoad;

		[SerializeField]
		private Button btnSave;

		[SerializeField]
		private Button btnSaveAs;

		[SerializeField]
		private Text lblShipName;

		[SerializeField]
		private TMP_Dropdown ddlFilter;

		[SerializeField]
		private Button btnUniques;

		[Header("SidePanel Ship Info")]
		[SerializeField]
		private TMP_InputField tboxMake;

		[SerializeField]
		private TMP_InputField tboxModel;

		[SerializeField]
		private TMP_InputField tboxDesignation;

		[SerializeField]
		private TMP_InputField tboxOrigin;

		[SerializeField]
		private TMP_InputField tboxYear;

		[SerializeField]
		private TMP_InputField tboxName;

		[SerializeField]
		private TMP_InputField tboxDescription;

		[SerializeField]
		private TMP_InputField tboxLaw;

		[SerializeField]
		private GameObject txtLaw;

		[SerializeField]
		private Button btnShipDetails;

		[SerializeField]
		private CanvasGroup cgShipDetails;

		[SerializeField]
		private RectTransform tfSidePanel;

		[SerializeField]
		private GUIShipUniquePanel uniquePanel;

		[Header("SidePanel Construction Stages")]
		[SerializeField]
		private Button btnConstructionStage;

		[SerializeField]
		private CanvasGroup cgConstructionStages;

		[SerializeField]
		private GUIShipEditStageEntry prefabConstructionStage;

		[SerializeField]
		private GUIShipStageDescriptionRow prefabDescriptionRow;

		[SerializeField]
		private Transform tfSRStageContainer;

		public List<JsonShipUniques> shipUniques = new List<JsonShipUniques>();

		public bool ShowHelp = true;

		public ScrollRect uniquesScrollPanel;

		private float timer = 0.5f;

		private int index;
	}
}
