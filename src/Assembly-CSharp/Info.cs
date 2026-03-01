using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tutorial/help encyclopedia UI.
// Likely built from JSON-backed info nodes and used both in the main menu
// and during CrewSim for codex/tutorial popups.
public class Info : MonoBehaviour
{
	// Treats this UI as focused while it is open, dragged, resized, or scrolling.
	public static bool focused
	{
		get
		{
			return Info.activeThisFrame || Info.instance.draggingWindow || Info.instance.resizing || Info.instance.bodyScrollBar.holdingMouse;
		}
	}

	// Ensures a single persistent instance and defers initialization until DataHandler is ready.
	private void Awake()
	{
		if (Info.instance)
		{
			UnityEngine.Object.DestroyImmediate(base.gameObject);
			return;
		}
		Info.instance = this;
		if (this.staticCG.alpha == 1f)
		{
			this.staticCG.alpha = 0f;
			this.dynamicCG.alpha = 0f;
		}
		this.dragbarStart = this.dragBar.color;
		this.noImageColor = default(Vector4);
		this.arrowStart = this.infoPanelPrefab.arrowImage.color;
		this.arrowPressed = new Color(0.3f, 0.3f, 0.3f, 0.85f);
		this.Logo.enabled = false;
		this.bodyReferenceMaterial = this.mainWindowBodyText.fontSharedMaterial;
		this.titleReferenceMaterial = this.mainWindowTitle.fontSharedMaterial;
		if (!this.listHidden)
		{
			this.btnShowHierarchy.gameObject.SetActive(false);
		}
		this.btnMuteTutorials.gameObject.SetActive(false);
		if (SceneManager.GetActiveScene().name == "MainMenu2")
		{
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			this.triggerDataHandlerInit = false;
			if (!DataHandler.bLoaded)
			{
				DataHandler.LoadComplete = (Action)Delegate.Combine(DataHandler.LoadComplete, new Action(this.Init));
			}
			else
			{
				this.Init();
			}
		}
		else
		{
			this.triggerDataHandlerInit = true;
			this.Init();
		}
	}

	// Builds the UI index and hierarchy after data is available.
	public void Init()
	{
		if (this.triggerDataHandlerInit)
		{
			DataHandler.Init();
		}
		this.CleanupPrefabOnInit();
		this.MakeIndex();
		this.BuildHierarchyFromJSON();
		this.FirstDraw();
		if (this.triggerDataHandlerInit && DataHandler.LoadComplete != null)
		{
			DataHandler.LoadComplete = (Action)Delegate.Remove(DataHandler.LoadComplete, new Action(this.Init));
		}
	}

	// Hides the window and clears the currently expanded panel state.
	public void Close()
	{
		this.staticCG.alpha = 0f;
		this.dynamicCG.alpha = 0f;
		this.dynamicCG.interactable = false;
		this.dynamicCG.blocksRaycasts = false;
		this.displayed = false;
		this.draggingWindow = false;
		this.CurrentChild = null;
		this.CurrentParent = null;
		this.DiscardAll();
		AudioManager.am.PlayAudioEmitterAtVol("UITutorialClose", false, false, 1f);
	}

	// Returns every active panel to the pool.
	public void DiscardAll()
	{
		for (int i = this.active.Count - 1; i >= 0; i--)
		{
			this.DiscardPanel(this.active[i]);
		}
	}

	// Opens directly to a node id from the info hierarchy.
	public void OpenToNode(string s)
	{
		InfoNode node = null;
		if (this.mapNodes.TryGetValue(s, out node))
		{
			this.OpenToNode(node);
		}
	}

	// Opens and redraws the panel stack for a specific node.
	public void OpenToNode(InfoNode node)
	{
		if (!this.displayed)
		{
			this.DefaultOpen();
		}
		this.NextParent = node.parent;
		this.NextChild = node;
		this.DrawNode(this.NextChild);
	}

	// Basic show/open state used by the main button and scripted tutorials.
	public void DefaultOpen()
	{
		this.displayed = true;
		this.staticCG.alpha = 1f;
		this.dynamicCG.alpha = 1f;
		this.dynamicCG.interactable = true;
		this.dynamicCG.blocksRaycasts = true;
		AudioManager.am.PlayAudioEmitterAtVol("UITutorialOpen", false, false, 1f);
		this.headingBG.enabled = false;
		if (this.currentObjectiveNode != null)
		{
			this.btnMuteTutorials.gameObject.SetActive(true);
		}
		else
		{
			this.btnMuteTutorials.gameObject.SetActive(false);
		}
	}

	public void MainButtonOpenClose()
	{
		if (!this.displayed)
		{
			this.DefaultOpen();
			this.CurrentParent = null;
			this.CurrentChild = null;
			this.NextParent = this.Index;
			this.NextChild = this.Index;
			this.DrawNode(this.NextChild);
		}
		else
		{
			this.Close();
		}
	}

	public void DiscardPanel(InfoPanel infoPanel)
	{
		infoPanel.node.expanded = false;
		infoPanel.expanded = false;
		infoPanel.bgImage.color = infoPanel.bgColorStart;
		infoPanel.title.color = infoPanel.textColorStart;
		infoPanel.transform.localPosition = Vector3.right * -200f;
		infoPanel.title.rectTransform.localPosition = Vector3.zero;
		infoPanel.arrowImage.rectTransform.localPosition = this.infoPanelPrefab.arrowImage.rectTransform.localPosition;
		infoPanel.arrowImage.enabled = false;
		infoPanel.arrowImage.color = this.arrowStart;
		infoPanel.isExpandedChild = false;
		this.active.Remove(infoPanel);
		if (!this.panelPool.Contains(infoPanel))
		{
			this.panelPool.Add(infoPanel);
		}
	}

	public InfoPanel GetPanel(InfoNode n)
	{
		InfoPanel infoPanel;
		if (this.panelPool.Count > 0)
		{
			infoPanel = this.panelPool[this.panelPool.Count - 1];
			this.panelPool.RemoveAt(this.panelPool.Count - 1);
		}
		else
		{
			infoPanel = UnityEngine.Object.Instantiate<InfoPanel>(this.infoPanelPrefab, this.scrollListContent);
			infoPanel.arrowImage.enabled = false;
		}
		float x = this.infoPanelPrefab.rect.sizeDelta.x;
		float num = this.infoPanelPrefab.title.rectTransform.sizeDelta.x;
		if (n.children.Count > 0)
		{
			infoPanel.arrowImage.enabled = true;
			infoPanel.arrowImage.rectTransform.rotation = Quaternion.Euler(0f, 0f, -90f);
			num -= 15f;
		}
		else
		{
			infoPanel.arrowImage.enabled = false;
		}
		n.InfoPanel = infoPanel;
		infoPanel.isExpandedChild = false;
		infoPanel.bgImage.raycastTarget = true;
		infoPanel.info = this;
		infoPanel.node = n;
		infoPanel.bgImage.color = infoPanel.bgColorStart;
		infoPanel.title.color = infoPanel.textColorStart;
		infoPanel.title.rectTransform.sizeDelta = new Vector2(num, this.infoPanelPrefab.title.rectTransform.sizeDelta.y);
		infoPanel.title.SetText(n.label);
		infoPanel.title.ForceMeshUpdate();
		infoPanel.rect.sizeDelta = new Vector2(x, this.infoPanelPrefab.rect.sizeDelta.y + (float)(infoPanel.title.textInfo.lineCount - 1) * infoPanel.title.fontSize);
		if (infoPanel.arrowImage.enabled)
		{
			infoPanel.arrowImage.rectTransform.localPosition = new Vector3(x / 2f - 7.5f, (float)(infoPanel.title.textInfo.lineCount - 1) * (infoPanel.title.fontSize / 2f - 0.5f));
			infoPanel.title.rectTransform.localPosition = new Vector3(-7.5f, 0f);
		}
		else
		{
			infoPanel.title.rectTransform.localPosition = new Vector3(0f, 0f);
		}
		if (!this.active.Contains(infoPanel))
		{
			this.active.Add(infoPanel);
		}
		else
		{
			Debug.LogError("Something's gone wrong with pooling, adding already active panel to active");
		}
		return infoPanel;
	}

	public void ContractNode(InfoNode nodeToContract)
	{
		if (!nodeToContract.expanded)
		{
			return;
		}
		int num = nodeToContract.siblings.IndexOf(nodeToContract);
		InfoPanel infoPanel = nodeToContract.children[nodeToContract.children.Count - 1].InfoPanel;
		float d = nodeToContract.InfoPanel.rect.localPosition.y - infoPanel.rect.localPosition.y - (-infoPanel.rect.sizeDelta.y + nodeToContract.InfoPanel.rect.sizeDelta.y) / 2f;
		nodeToContract.InfoPanel.arrowImage.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
		for (int i = 0; i < nodeToContract.children.Count; i++)
		{
			InfoPanel infoPanel2 = nodeToContract.children[i].InfoPanel;
			this.DiscardPanel(infoPanel2);
		}
		for (int j = num + 1; j < nodeToContract.siblings.Count; j++)
		{
			nodeToContract.siblings[j].InfoPanel.rect.localPosition += Vector3.up * d;
		}
		nodeToContract.expanded = false;
	}

	public void ExpandNode(InfoNode nodeToExpand)
	{
		if (nodeToExpand.expanded)
		{
			return;
		}
		int num = nodeToExpand.parent.children.IndexOf(nodeToExpand);
		float num2 = 0f;
		InfoPanel infoPanel = nodeToExpand.InfoPanel;
		infoPanel.arrowImage.transform.localRotation = Quaternion.Euler(0f, 0f, -180f);
		infoPanel.arrowImage.color = this.arrowPressed;
		infoPanel.expanded = true;
		infoPanel.arrowImage.rectTransform.localPosition = new Vector3(infoPanel.rect.sizeDelta.x / 2f - 10f, (float)(infoPanel.title.textInfo.lineCount - 1) * infoPanel.title.fontSize / 2f - 2f);
		for (int i = 0; i < nodeToExpand.children.Count; i++)
		{
			InfoNode n = nodeToExpand.children[i];
			InfoPanel panel = this.GetPanel(n);
			panel.isExpandedChild = true;
			float num3 = this.infoPanelPrefab.rect.sizeDelta.x * 0.9f;
			float num4 = num3 * 0.9f;
			if (panel.arrowImage.enabled)
			{
				num4 -= 15f;
			}
			panel.rect.sizeDelta = new Vector2(this.infoPanelPrefab.rect.sizeDelta.x * 0.9f, this.infoPanelPrefab.rect.sizeDelta.y);
			panel.title.rectTransform.sizeDelta = new Vector2(num4, this.infoPanelPrefab.title.rectTransform.sizeDelta.y);
			panel.title.ForceMeshUpdate();
			panel.rect.sizeDelta = new Vector2(panel.rect.sizeDelta.x, this.infoPanelPrefab.rect.sizeDelta.y + (float)(panel.title.textInfo.lineCount - 1) * panel.title.fontSize);
			if (panel.arrowImage.enabled)
			{
				panel.arrowImage.rectTransform.localPosition = new Vector3(panel.rect.sizeDelta.x / 2f - 10f, (float)(panel.title.textInfo.lineCount - 1) * panel.title.fontSize / 2f);
				panel.title.rectTransform.localPosition = -Vector3.right * 7.5f;
			}
			num2 += infoPanel.rect.sizeDelta.y / 2f + panel.rect.sizeDelta.y / 2f;
			panel.transform.localPosition = nodeToExpand.InfoPanel.transform.localPosition - new Vector3(-0.05f * this.infoPanelPrefab.rect.sizeDelta.x, num2);
			panel.title.color = Color.Lerp(panel.textColorStart, Color.black, 0.2f);
			infoPanel = panel;
		}
		InfoPanel infoPanel2 = nodeToExpand.children[nodeToExpand.children.Count - 1].InfoPanel;
		num2 = nodeToExpand.InfoPanel.rect.localPosition.y - infoPanel2.rect.localPosition.y - (nodeToExpand.InfoPanel.rect.sizeDelta.y - infoPanel2.rect.sizeDelta.y) / 2f;
		for (int j = num + 1; j < nodeToExpand.parent.children.Count; j++)
		{
			nodeToExpand.parent.children[j].InfoPanel.transform.localPosition += Vector3.up * -num2;
		}
		nodeToExpand.expanded = true;
	}

	public void DrawMainWindow(InfoNode node)
	{
		if (node.mainWindowData == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(node.mainWindowData.title))
		{
			if (!this.mainWindowTitle.transform.parent.gameObject.activeSelf)
			{
				this.mainWindowTitle.transform.parent.gameObject.SetActive(true);
			}
			this.mainWindowTitle.enableAutoSizing = false;
			this.mainWindowTitle.fontSize = 32f;
			if (!this.mainWindowTitle.enabled)
			{
				this.mainWindowTitle.enabled = true;
			}
			if (!this.headingBG.enabled)
			{
				this.headingBG.enabled = true;
			}
			this.mainWindowTitle.SetText(node.mainWindowData.title);
			this.RepositionTitle();
			this.RepositionTitleBG();
		}
		else
		{
			this.mainWindowTitle.SetText(string.Empty);
			this.headingBG.enabled = false;
			this.mainWindowTitle.transform.parent.gameObject.SetActive(false);
		}
		bool flag = false;
		if (!string.IsNullOrEmpty(node.strLookup))
		{
			flag = true;
			if (!this.articleSeriesParent.gameObject.activeInHierarchy)
			{
				this.articleSeriesParent.gameObject.SetActive(true);
			}
			this.articleLookup.text = node.strLookup;
			this.articleLookupXOX.text = InfoLookup.GetSeries(node.strLookup).infoNodes.IndexOf(node) + 1 + " of " + InfoLookup.GetSeries(node.strLookup).Count;
		}
		else
		{
			if (this.articleSeriesParent.gameObject.activeInHierarchy)
			{
				this.articleSeriesParent.gameObject.SetActive(false);
			}
			this.articleLookup.text = string.Empty;
			this.articleLookupXOX.text = string.Empty;
		}
		if (flag)
		{
			this.bodyTextMask.offsetMin = new Vector2(this.bodyTextMask.offsetMin.x, 32.5f);
		}
		else
		{
			this.bodyTextMask.offsetMin = new Vector2(this.bodyTextMask.offsetMin.x, 11.5f);
		}
		if (!string.IsNullOrEmpty(node.mainWindowData.img))
		{
			Texture2D texture = DataHandler.LoadPNG(node.mainWindowData.img + ".png", false, false);
			this.mainImage.texture = texture;
			this.mainImage.color = Color.white;
			this.mainImage.SetNativeSize();
			this.mainImage.enabled = true;
			this.Logo.enabled = false;
		}
		else
		{
			this.Logo.enabled = true;
			this.mainImage.enabled = false;
			this.mainImage.texture = null;
			this.mainImage.color = this.noImageColor;
		}
		if (!string.IsNullOrEmpty(node.mainWindowData.body))
		{
			string text = node.mainWindowData.body;
			if (!string.IsNullOrEmpty(node.escapeChar))
			{
				char c = node.escapeChar[0];
				List<int> list = new List<int>();
				for (int i = 0; i < node.mainWindowData.body.Length; i++)
				{
					if (node.mainWindowData.body[i] == c)
					{
						list.Add(i);
					}
				}
				for (int j = 0; j < list.Count; j += 2)
				{
					int num = list[j];
					int num2 = list[j + 1];
					int num3 = int.Parse(node.mainWindowData.body.Substring(num + 1, num2 - num - 1));
					GameAction key = (GameAction)num3;
					GUIActionKey guiactionKey = GUIActionKeySelector.dictActionKeysStaticRef[key];
					text = text.Replace(node.mainWindowData.body.Substring(num, num2 - num + 1), guiactionKey.command.KeyName);
				}
			}
			this.mainWindowBodyText.SetText(text);
			this.mainWindowBodyText.ForceMeshUpdate();
			this.mainWindowBodyText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, this.mainWindowBodyText.preferredHeight);
			this.lastBodyTextPreferredHeight = this.mainWindowBodyText.preferredHeight;
			this.bodyScrollBar.AfterNewTextDraw();
		}
		else
		{
			this.mainWindowBodyText.SetText(string.Empty);
			this.bodyScrollBar.HideScrollBar();
		}
		if (this.mainWindowBodyText.fontSharedMaterial != this.bodyReferenceMaterial)
		{
			this.mainWindowBodyText.fontSharedMaterial = this.bodyReferenceMaterial;
		}
		if (this.mainWindowTitle.fontSharedMaterial != this.titleReferenceMaterial)
		{
			this.mainWindowTitle.fontSharedMaterial = this.titleReferenceMaterial;
		}
	}

	public void DrawScrollBar()
	{
		float num = 0f;
		if (this.NextChild != this.Index)
		{
			if (this.NextChild != null)
			{
				this.NextParent = this.NextChild.parent;
				if (this.NextParent == this.CurrentParent)
				{
					for (int i = this.active.Count - 1; i >= 0; i--)
					{
						if (this.active[i].node == this.CurrentChild)
						{
							this.active[i].bgImage.color = this.active[i].bgColorStart;
							this.active[i].title.color = this.active[i].textColorStart;
							this.active[i].arrowImage.color = this.arrowPressed;
							break;
						}
					}
					this.CurrentChild = this.NextChild;
					this.CurrentParent = this.NextParent;
					return;
				}
				this.CurrentChild = this.NextChild;
				this.CurrentParent = this.NextParent;
				for (int j = this.active.Count - 1; j >= 0; j--)
				{
					this.DiscardPanel(this.active[j]);
				}
			}
		}
		else
		{
			for (int k = this.active.Count - 1; k >= 0; k--)
			{
				this.DiscardPanel(this.active[k]);
			}
			this.CurrentChild = this.Index;
			this.CurrentParent = this.Index;
		}
		for (int l = 0; l < this.CurrentParent.children.Count; l++)
		{
			InfoNode n = this.CurrentParent.children[l];
			InfoPanel panel = this.GetPanel(n);
			panel.transform.localPosition = new Vector3(0f, num - panel.rect.sizeDelta.y / 2f);
			num -= panel.rect.sizeDelta.y;
		}
		InfoPanel infoPanel = null;
		if (this.CurrentParent != this.Index)
		{
			infoPanel = this.GetPanel(this.CurrentParent);
			infoPanel.transform.localPosition = new Vector3(0f, this.CurrentParent.children[0].InfoPanel.rect.sizeDelta.y);
			infoPanel.arrowImage.rectTransform.rotation = Quaternion.Euler(0f, 0f, -180f);
			infoPanel.title.text = "..";
			infoPanel.rect.sizeDelta = this.infoPanelPrefab.rect.sizeDelta;
			this.directoryLabel.enabled = true;
			this.directoryLabel.SetText(this.CurrentParent.label);
			this.directoryLabel.ForceMeshUpdate();
			RectTransform rectTransform = this.directoryLabel.rectTransform.parent as RectTransform;
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, this.infoPanelPrefab.rect.sizeDelta.y + (float)(this.directoryLabel.textInfo.lineCount - 1) * this.directoryLabel.fontSize);
			this.directoryLabel.rectTransform.localPosition = infoPanel.transform.localPosition + this.directoryLabel.rectTransform.sizeDelta.y * Vector3.up * 1.5f;
		}
		if (infoPanel == null)
		{
			this.directoryLabel.enabled = false;
		}
	}

	public void DrawNode(InfoNode node)
	{
		this.NextChild = node;
		this.NextParent = node.parent;
		if (this.CurrentChild != null && this.CurrentChild != node && this.CurrentChild.expanded)
		{
			this.ContractNode(this.CurrentChild);
		}
		this.DrawScrollBar();
		this.DrawMainWindow(this.NextChild);
		if (this.NextChild.InfoPanel)
		{
			this.NextChild.InfoPanel.title.color = this.NextChild.InfoPanel.textColorCurrent;
			this.NextChild.InfoPanel.bgImage.color = this.NextChild.InfoPanel.bgColorCurrent;
			if (this.NextChild.children.Count > 0 && !this.NextChild.expanded)
			{
				this.ExpandNode(this.NextChild);
			}
			else if (this.NextChild.children.Count > 0 && this.NextChild.expanded)
			{
				this.ContractNode(this.NextChild);
			}
		}
		if (this.CurrentChild.InfoPanel)
		{
			this.scrollListContent.localPosition = Vector3.up * -this.CurrentChild.InfoPanel.rect.localPosition.y;
		}
	}

	public InfoNode Previous(InfoNode start, InfoNode current, int recursions)
	{
		recursions++;
		if (recursions == 100)
		{
			Debug.Log("You dope you did the recursion overflow");
			return null;
		}
		if (current == this.Index)
		{
			return this.Index;
		}
		int num = current.siblings.IndexOf(current);
		if (num == 0 && current.parent == this.Index)
		{
			return this.Index;
		}
		if (current.children.Contains(start))
		{
			return current;
		}
		if (num == 0)
		{
			return this.Previous(start, current.parent, recursions);
		}
		InfoNode parent = current.siblings[num - 1];
		return this.DeepestChildTail(parent, 0);
	}

	public InfoNode DeepestChildTail(InfoNode parent, int depth)
	{
		if (depth > 100)
		{
			Debug.Log("recursion overflow");
			return null;
		}
		if (parent.children.Count > 0)
		{
			return this.DeepestChildTail(parent.children[parent.children.Count - 1], depth + 1);
		}
		return parent;
	}

	public void In()
	{
		if (this.CurrentChild != null && this.CurrentChild.children.Count > 0)
		{
			this.NextChild = this.CurrentChild.children[0];
			this.DrawNode(this.NextChild);
		}
	}

	public void Out()
	{
		if (this.CurrentChild != null && this.CurrentChild.parent != null && this.CurrentChild.parent != this.Index)
		{
			this.NextChild = this.CurrentChild.parent;
			this.DrawNode(this.NextChild);
		}
	}

	public void Previous()
	{
		if (this.CurrentChild == null || this.CurrentChild == this.Index)
		{
			this.CurrentChild = this.Index;
			this.CurrentParent = this.Index;
			this.DrawNode(this.CurrentChild);
			return;
		}
		if (this.CurrentChild.siblings.IndexOf(this.CurrentChild) == 0)
		{
			return;
		}
		this.NextChild = this.CurrentChild.siblings[this.CurrentChild.siblings.IndexOf(this.CurrentChild) - 1];
		this.DrawNode(this.NextChild);
	}

	public void NextInSeries()
	{
		InfoLookup series = InfoLookup.GetSeries(this.CurrentChild.strLookup);
		if (series == null)
		{
			return;
		}
		int num = series.infoNodes.IndexOf(this.CurrentChild);
		if (num + 1 == series.Count)
		{
			return;
		}
		this.DrawNode(series.infoNodes[num + 1]);
	}

	public void BeginResize()
	{
		this.resizing = true;
		this.deadzoneEscaped = false;
		this.resizeStart = this.resizeTargets[0].rect.size;
		this.mouseClickResizeStart = Input.mousePosition;
	}

	public void MuteTutorials()
	{
		this.Close();
		GUIPDA.OpenApp("goals:settings");
	}

	public void CurrentObjective()
	{
		if (this.currentObjectiveNode != null)
		{
			this.DrawNode(this.currentObjectiveNode);
		}
	}

	public void PrevInSeries()
	{
		InfoLookup series = InfoLookup.GetSeries(this.CurrentChild.strLookup);
		if (series == null)
		{
			return;
		}
		int num = series.infoNodes.IndexOf(this.CurrentChild);
		if (num == 0)
		{
			return;
		}
		this.DrawNode(series.infoNodes[num - 1]);
	}

	public void AddNodeToTrackedNodes(string key, InfoNode node)
	{
		if (!this.mapNodes.ContainsKey(key))
		{
			this.mapNodes.Add(key, node);
			if (!string.IsNullOrEmpty(node.strLookup))
			{
				InfoLookup.Add(node);
			}
		}
	}

	public void BuildHierarchyFromJSON()
	{
		this.jsonNodes.AddRange(DataHandler.dictInfoNodes.Values);
		this.jsonNodes.Remove(DataHandler.dictInfoNodes["Index"]);
		for (int i = 0; i < this.jsonNodes.Count; i++)
		{
			InfoNode node = new InfoNode
			{
				label = this.jsonNodes[i].strNodeLabel,
				strLookup = this.jsonNodes[i].strLookup,
				lookupIndex = this.jsonNodes[i].nLookup,
				escapeChar = this.jsonNodes[i].escapeChar
			};
			this.AddNodeToTrackedNodes(this.jsonNodes[i].strName, node);
		}
		for (int j = 0; j < this.jsonNodes.Count; j++)
		{
			JsonInfoNode jsonInfoNode = this.jsonNodes[j];
			InfoNode infoNode = this.mapNodes[jsonInfoNode.strName];
			if (!string.IsNullOrEmpty(this.jsonNodes[j].strTutorialKey))
			{
				Info.tutorialKeys.Add(this.jsonNodes[j].strTutorialKey, infoNode);
			}
			if (string.IsNullOrEmpty(this.jsonNodes[j].strNodeParent))
			{
				infoNode.parent = this.Index;
				this.Index.children.Add(infoNode);
				infoNode.siblings = this.Index.children;
			}
			else
			{
				infoNode.parent = this.mapNodes[this.jsonNodes[j].strNodeParent];
				infoNode.parent.children.Add(infoNode);
				infoNode.siblings = infoNode.parent.children;
			}
			infoNode.depth = infoNode.parent.depth + 1;
			infoNode.mainWindowData = new MainWindowData
			{
				body = jsonInfoNode.strArticleBody,
				img = jsonInfoNode.strImage,
				title = jsonInfoNode.strArticleTitle
			};
		}
	}

	public void MakeIndex()
	{
		if (this.Index != null)
		{
			return;
		}
		this.Index = new InfoNode
		{
			depth = 0
		};
		JsonInfoNode jsonInfoNode = DataHandler.dictInfoNodes["Index"];
		this.Index.parent = this.Index;
		this.Index.siblings = new List<InfoNode>();
		this.Index.mainWindowData = new MainWindowData
		{
			body = jsonInfoNode.strArticleBody,
			img = jsonInfoNode.strImage
		};
		this.AddNodeToTrackedNodes("Index", this.Index);
	}

	public void CleanupPrefabOnInit()
	{
		this.headingBG.enabled = false;
		this.mainWindowTitle.SetText(string.Empty);
		this.mainWindowBodyText.SetText(string.Empty);
	}

	public void FirstDraw()
	{
		this.CurrentParent = this.Index;
		this.CurrentChild = this.Index;
		this.DrawScrollBar();
		this.DrawNode(this.CurrentChild);
	}

	public void Next()
	{
		if (this.CurrentChild == null || this.CurrentChild == this.Index)
		{
			this.CurrentChild = this.Index;
			this.CurrentParent = this.Index;
			this.DrawNode(this.CurrentChild);
			return;
		}
		if (this.CurrentChild.siblings.IndexOf(this.CurrentChild) == this.CurrentChild.siblings.Count - 1)
		{
			return;
		}
		this.NextChild = this.CurrentChild.siblings[this.CurrentChild.siblings.IndexOf(this.CurrentChild) + 1];
		if (this.NextChild == this.Index)
		{
			this.NextChild = this.Index.children[0];
			this.NextParent = this.Index;
		}
		this.DrawNode(this.NextChild);
	}

	public bool NodeIsAtHead(InfoNode node)
	{
		return this.CurrentChild.siblings.IndexOf(this.CurrentChild) == 0;
	}

	public bool NodeIsAtTail(InfoNode node)
	{
		return this.CurrentChild.siblings.IndexOf(this.CurrentChild) == this.CurrentChild.siblings.Count - 1;
	}

	public void ResizeWindows()
	{
		this.bodyScrollBar.RememberCurrentScrollVal();
		Vector3 a = new Vector3(Mathf.Clamp(Input.mousePosition.x, (float)Screen.width * 0.015625f, (float)Screen.width * 0.984375f), Mathf.Clamp(Input.mousePosition.y, (float)Screen.height * 0.015625f, (float)Screen.height * 0.984375f));
		Vector3 a2 = a - this.mouseClickResizeStart;
		a2 *= Mathf.Lerp(this.canvasScaler.referenceResolution.x / (float)Screen.width, this.canvasScaler.referenceResolution.y / (float)Screen.height, this.canvasScaler.matchWidthOrHeight);
		Vector3 b = new Vector3(a2.x, -a2.y, a2.z);
		if (a2.magnitude < 10f && !this.deadzoneEscaped)
		{
			return;
		}
		this.deadzoneEscaped = true;
		Vector3 vector = this.resizeTargets[0].sizeDelta;
		Vector3 v = this.resizeStart + b;
		v.x = Mathf.Clamp(v.x, this.resizeMin.x, this.resizeMax.x);
		v.y = Mathf.Clamp(v.y, this.resizeMin.y, this.resizeMax.y);
		foreach (RectTransform rectTransform in this.resizeTargets)
		{
			rectTransform.sizeDelta = v;
		}
		this.offsetParent.localPosition += new Vector3(v.x - vector.x, -(v.y - vector.y)) / 2f;
	}

	private void RepositionBody()
	{
		this.mainWindowBodyText.ForceMeshUpdate();
		this.mainWindowBodyText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, this.mainWindowBodyText.preferredHeight);
		this.lastBodyTextPreferredHeight = this.mainWindowBodyText.preferredHeight;
	}

	private void RepositionTitle()
	{
		this.mainWindowTitle.ForceMeshUpdate();
		int lineCount = this.mainWindowTitle.textInfo.lineCount;
		this.mainWindowTitle.rectTransform.offsetMax = new Vector2(-12f, this.mainWindowTitle.fontSize * (float)lineCount + 8f);
		this.mainWindowTitle.rectTransform.offsetMin = new Vector2(12f, 8f);
		if (this.mainWindowTitle.textInfo.lineCount > 2)
		{
			this.mainWindowTitle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64f);
			this.mainWindowTitle.enableAutoSizing = true;
			this.mainWindowTitle.ForceMeshUpdate();
			this.mainWindowTitle.enableAutoSizing = false;
		}
		this.mainWindowTitle.ForceMeshUpdate();
	}

	private void RepositionTitleBG()
	{
		int siblingIndex = this.mainWindowTitle.transform.GetSiblingIndex();
		Vector2 vector = new Vector2(this.mainWindowTitle.renderedWidth, this.mainWindowTitle.renderedHeight);
		Vector2 vector2 = new Vector2(this.mainWindowTitle.renderedWidth + 10f, this.mainWindowTitle.renderedHeight + 4f);
		this.headingBG.rectTransform.offsetMin = new Vector2(-1f, -1f);
		this.headingBG.rectTransform.offsetMax = new Vector2(1f, (float)(32 * this.mainWindowTitle.textInfo.lineCount + 16));
		RectTransform rectTransform = this.mainWindowTitle.transform.parent as RectTransform;
		RectTransform rectTransform2 = rectTransform.GetChild(siblingIndex - 1).transform as RectTransform;
		RectTransform rectTransform3 = rectTransform.GetChild(siblingIndex + 1).transform as RectTransform;
		float num = this.mainWindowTitle.rectTransform.rect.size.x - this.mainWindowTitle.renderedWidth;
		RectTransform rectTransform4 = rectTransform2;
		Vector2 vector3 = this.mainWindowTitle.rectTransform.offsetMax + Vector2.right * (-num + 4f) + Vector2.up * 3f;
		rectTransform3.offsetMax = vector3;
		rectTransform4.offsetMax = vector3;
		RectTransform rectTransform5 = rectTransform2;
		vector3 = this.mainWindowTitle.rectTransform.offsetMin + Vector2.right * -4f + Vector2.up * -2f;
		rectTransform3.offsetMin = vector3;
		rectTransform5.offsetMin = vector3;
	}

	private void Update()
	{
		Info.activeThisFrame = false;
		if (!this.displayed)
		{
			return;
		}
		if (this.resizing && !Input.GetMouseButtonUp(0))
		{
			this.ResizeWindows();
			this.RepositionTitle();
			this.RepositionTitleBG();
			this.RepositionBody();
			this.bodyScrollBar.AfterResize();
		}
		else if (this.resizing && Input.GetMouseButtonUp(0))
		{
			this.resizing = false;
			this.deadzoneEscaped = false;
		}
		if (this.NodeIsAtTail(this.CurrentChild))
		{
			if (this.btnNext.cg.alpha != 0.5f)
			{
				this.btnNext.cg.alpha = 0.5f;
			}
		}
		else if (this.btnNext.cg.alpha != 1f)
		{
			this.btnNext.cg.alpha = 1f;
		}
		if (this.NodeIsAtHead(this.CurrentChild) || this.CurrentChild == this.Index)
		{
			if (this.btnPrevious.cg.alpha != 0.5f)
			{
				this.btnPrevious.cg.alpha = 0.5f;
			}
		}
		else if (this.btnPrevious.cg.alpha != 1f)
		{
			this.btnPrevious.cg.alpha = 1f;
		}
		if (this.CurrentChild.parent == this.Index)
		{
			if (this.btnOut.cg.alpha != 0.5f)
			{
				this.btnOut.cg.alpha = 0.5f;
			}
		}
		else if (this.btnOut.cg.alpha != 1f)
		{
			this.btnOut.cg.alpha = 1f;
		}
		if (this.CurrentChild.children.Count == 0)
		{
			if (this.btnIn.cg.alpha != 0.5f)
			{
				this.btnIn.cg.alpha = 0.5f;
			}
		}
		else if (this.btnIn.cg.alpha != 1f)
		{
			this.btnIn.cg.alpha = 1f;
		}
		if (this.currentObjectiveNode != null)
		{
			this.btnCurrentObjective.cg.alpha = 1f;
		}
		else
		{
			this.btnCurrentObjective.cg.alpha = 0.5f;
		}
		if (RectTransformUtility.RectangleContainsScreenPoint(this.activeWindow, Input.mousePosition, this.canvas.worldCamera))
		{
			Info.activeThisFrame = true;
		}
		this.scrollingList = false;
		this.scrollingBody = false;
		if (RectTransformUtility.RectangleContainsScreenPoint(this.scrollRectWindow, Input.mousePosition, this.canvas.worldCamera))
		{
			if (Input.GetAxis("Mouse ScrollWheel") > 0f)
			{
				this.scrollingList = true;
				this.scrollListAccel += Time.deltaTime * 6f;
				if (this.scrollListAccel > 1f)
				{
					this.scrollListAccel = 1f;
				}
				this.scrollListVel += 550f * Time.smoothDeltaTime * -Vector3.up + 350f * Time.smoothDeltaTime * -Vector3.up * this.scrollListAccel;
			}
			else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
			{
				this.scrollingList = true;
				this.scrollListAccel += Time.deltaTime * 6f;
				if (this.scrollListAccel > 1f)
				{
					this.scrollListAccel = 1f;
				}
				this.scrollListVel += 550f * Time.smoothDeltaTime * Vector3.up + 350f * Time.smoothDeltaTime * Vector3.up * this.scrollListAccel;
			}
		}
		else if (RectTransformUtility.RectangleContainsScreenPoint(this.repositionWindowBar, Input.mousePosition, this.canvas.worldCamera))
		{
			if (this.dragBarHighlightState == 0)
			{
				this.dragBarHighlightState = 1;
			}
			if (Input.GetMouseButtonDown(0) && !this.draggingWindow)
			{
				this.StartDraggingWindow();
			}
		}
		else if (!this.bodyScrollBar.ContentSmallerThanViewport() && RectTransformUtility.RectangleContainsScreenPoint(this.bodyTextMask, Input.mousePosition, this.canvas.worldCamera))
		{
			if (Input.GetAxis("Mouse ScrollWheel") > 0f && this.scrollBodyContent.localPosition.y > 90f - this.scrollBodyContent.rect.size.y / 2f)
			{
				this.scrollingBody = true;
				this.scrollBodyAccel += Time.deltaTime * 6f;
				if (this.scrollBodyAccel > 1f)
				{
					this.scrollBodyAccel = 1f;
				}
				this.scrollBodyVel += 550f * Time.smoothDeltaTime * -Vector3.up + 350f * Time.smoothDeltaTime * -Vector3.up * this.scrollBodyAccel;
			}
			else if (Input.GetAxis("Mouse ScrollWheel") < 0f && this.scrollBodyContent.localPosition.y < this.scrollBodyContent.rect.size.y / 2f - 90f)
			{
				this.scrollingBody = true;
				this.scrollBodyAccel += Time.deltaTime * 6f;
				if (this.scrollBodyAccel > 1f)
				{
					this.scrollBodyAccel = 1f;
				}
				this.scrollBodyVel += 550f * Time.smoothDeltaTime * Vector3.up + 350f * Time.smoothDeltaTime * Vector3.up * this.scrollBodyAccel;
			}
		}
		else if (!this.draggingWindow)
		{
			this.dragBarHighlightState = 0;
		}
		if (this.draggingWindow)
		{
			Info.activeThisFrame = true;
			if (Input.GetMouseButtonUp(0))
			{
				this.draggingWindow = false;
				Info.activeThisFrame = false;
				this.dragBarHighlightState = 0;
			}
			else
			{
				float num = this.canvasScaler.referenceResolution.x / (float)Screen.width;
				float num2 = this.canvasScaler.referenceResolution.y / (float)Screen.height;
				Vector3 vector = new Vector3(Mathf.Clamp(Input.mousePosition.x, (float)Screen.width * 0.03f, (float)Screen.width * 0.97f), Mathf.Clamp(Input.mousePosition.y, (float)Screen.height * 0.03f, (float)Screen.height * 0.97f));
				float min = -this.canvasScaler.referenceResolution.x / 2f + this.resizeTargets[0].sizeDelta.x / 2f;
				float max = this.canvasScaler.referenceResolution.x / 2f - this.resizeTargets[0].sizeDelta.x / 2f;
				float min2 = -this.canvasScaler.referenceResolution.y / 2f + this.resizeTargets[0].sizeDelta.y / 2f;
				float max2 = this.canvasScaler.referenceResolution.y / 2f - this.resizeTargets[0].sizeDelta.y / 2f;
				Vector3 localPosition = new Vector3(Mathf.Clamp(Input.mousePosition.x * num - this.dragOffsetAtClick.x, min, max), Mathf.Clamp(Input.mousePosition.y * num2 - this.dragOffsetAtClick.y, min2, max2));
				this.offsetParent.localPosition = localPosition;
			}
		}
		if (!this.scrollingList)
		{
			this.scrollListAccel *= 0.95f;
			this.scrollListVel *= 0.9f;
		}
		if (!this.scrollingBody)
		{
			this.scrollBodyAccel *= 0.95f;
			this.scrollBodyVel *= 0.9f;
		}
		if (this.active.Count <= 1)
		{
			return;
		}
		InfoPanel infoPanel = this.active[0];
		InfoPanel infoPanel2 = this.active[0];
		float num3 = float.MinValue;
		float num4 = float.MaxValue;
		for (int i = 0; i < this.active.Count; i++)
		{
			if (this.active[i].transform.localPosition.y > num3)
			{
				num3 = this.active[i].transform.localPosition.y;
				infoPanel = this.active[i];
			}
			if (this.active[i].transform.localPosition.y < num4)
			{
				num4 = this.active[i].transform.localPosition.y;
				infoPanel2 = this.active[i];
			}
		}
		float num5 = this.scrollRectWindow.rect.size.y / 2f;
		float num6 = -this.scrollRectWindow.rect.size.y / 2f;
		bool flag = this.scrollListContent.localPosition.y + infoPanel.rect.localPosition.y + infoPanel.rect.rect.size.y / 2f > num5;
		bool flag2 = this.scrollListContent.localPosition.y + infoPanel2.rect.localPosition.y - infoPanel2.rect.rect.size.y / 2f < num6;
		if (flag && flag2)
		{
			this.scrollListContent.transform.localPosition += this.scrollListVel;
		}
		else if (flag && !flag2)
		{
			if (this.scrollListVel.y < 0f)
			{
				this.scrollListContent.transform.localPosition += this.scrollListVel;
			}
		}
		else if (!flag && flag2)
		{
			if (this.scrollListVel.y > 0f)
			{
				this.scrollListContent.transform.localPosition += this.scrollListVel;
			}
		}
		else
		{
			this.scrollListContent.transform.localPosition += this.scrollListVel;
		}
		float num7 = -this.lastBodyTextPreferredHeight / 2f + this.bodyTextMask.rect.size.y / 2f;
		float num8 = this.lastBodyTextPreferredHeight / 2f - this.bodyTextMask.rect.size.y / 2f;
		if (this.scrollBodyContent.localPosition.y + this.scrollBodyVel.y <= num7 && this.scrollBodyVel.y < 0f)
		{
			this.bodyScrollBar.PositionBar(0f);
		}
		else if (this.scrollBodyContent.localPosition.y + this.scrollBodyVel.y >= num8 && this.scrollBodyVel.y > 0f)
		{
			this.bodyScrollBar.PositionBar(1f);
		}
		else
		{
			this.scrollBodyContent.transform.localPosition += this.scrollBodyVel;
		}
		if (this.bodyScrollBar.ScrollBarImage.enabled && !this.scrollingBody)
		{
			this.bodyScrollBar.PositionBar(Mathf.InverseLerp(num7, num8, this.scrollBodyContent.localPosition.y));
		}
		int num9 = this.dragBarHighlightState;
		if (num9 != 0)
		{
			if (num9 != 1)
			{
				if (num9 == 2)
				{
					float num10 = Mathf.SmoothDamp(this.dragBar.color.r, 0.8f, ref this.dragBarColorVel, 0.001f);
					this.dragBar.color = new Color(num10, num10, num10, 1f);
				}
			}
			else
			{
				float num11 = Mathf.SmoothDamp(this.dragBar.color.r, 0.7f, ref this.dragBarColorVel, 0.1f);
				this.dragBar.color = new Color(num11, num11, num11, 1f);
			}
		}
		else
		{
			float num12 = Mathf.SmoothDamp(this.dragBar.color.r, this.dragbarStart.r, ref this.dragBarColorVel, 0.1f);
			this.dragBar.color = new Color(num12, num12, num12, 1f);
		}
	}

	public void ToggleList()
	{
		if (this.listHidden)
		{
			this.listHidden = false;
			foreach (Image image in this.listHideTargets)
			{
				image.enabled = true;
			}
			this.leftListRoot.gameObject.SetActive(true);
			this.controlsRoot.gameObject.SetActive(true);
			this.mainDisplay.offsetMin = new Vector2(200f, 2f);
			this.mainWindowParentDynamic.offsetMin = new Vector2(200f, 2f);
			this.DrawMainWindow(this.CurrentChild);
			this.btnShowHierarchy.gameObject.SetActive(false);
		}
		else
		{
			this.listHidden = true;
			foreach (Image image2 in this.listHideTargets)
			{
				image2.enabled = false;
			}
			this.leftListRoot.gameObject.SetActive(false);
			this.controlsRoot.gameObject.SetActive(false);
			this.mainDisplay.offsetMin = new Vector2(2f, 2f);
			this.mainWindowParentDynamic.offsetMin = new Vector2(2f, 2f);
			this.DrawMainWindow(this.CurrentChild);
			this.btnShowHierarchy.gameObject.SetActive(true);
			(this.btnShowHierarchy.transform as RectTransform).anchoredPosition = new Vector3(19f, -58f);
		}
	}

	public void StartDraggingWindow()
	{
		this.dragBarHighlightState = 2;
		this.draggingWindow = true;
		this.dragOffsetAtClick = new Vector3(Input.mousePosition.x * (this.canvasScaler.referenceResolution.x / (float)Screen.width), Input.mousePosition.y * (this.canvasScaler.referenceResolution.y / (float)Screen.height)) - this.offsetParent.localPosition;
	}

	public static Info instance;

	public static bool activeThisFrame;

	public Canvas canvas;

	public CanvasScaler canvasScaler;

	public InfoPanel infoPanelPrefab;

	public static Dictionary<string, InfoLookup> articleLookups = new Dictionary<string, InfoLookup>();

	public static Dictionary<string, InfoNode> tutorialKeys = new Dictionary<string, InfoNode>();

	public InfoScrollbar bodyScrollBar;

	[Header("Text")]
	public TextMeshProUGUI mainWindowTitle;

	public TextMeshProUGUI mainWindowBodyText;

	public TextMeshProUGUI articleLookup;

	public TextMeshProUGUI articleLookupXOX;

	public TextMeshProUGUI directoryLabel;

	[Header("Canvas Groups")]
	public CanvasGroup staticCG;

	public CanvasGroup dynamicCG;

	[Header("Rects")]
	public RectTransform mainDisplay;

	public RectTransform mainWindowParentDynamic;

	public RectTransform offsetParent;

	public RectTransform listHeading;

	public RectTransform scrollListContent;

	public RectTransform scrollBodyContent;

	public RectTransform scrollRectWindow;

	public RectTransform repositionWindowBar;

	public RectTransform activeWindow;

	public RectTransform imageBorder;

	public RectTransform headerBorderTop;

	public RectTransform bodyTextMask;

	public RectTransform articleSeriesParent;

	public RectTransform leftListRoot;

	public RectTransform controlsRoot;

	public List<RectTransform> resizeTargets;

	[Header("HiddenWithList")]
	public List<Image> listHideTargets;

	[Header("Images")]
	public Image headingBG;

	public RawImage mainImage;

	public Image Logo;

	public Image dragBar;

	public Sprite logo;

	[Header("Controls")]
	public InfoButtons btnNext;

	public InfoButtons btnPrevious;

	public InfoButtons btnExit;

	public InfoButtons btnHome;

	public InfoButtons btnOut;

	public InfoButtons btnIn;

	public InfoButtons btnNextSeries;

	public InfoButtons btnPrevSeries;

	public InfoButtons btnMuteTutorials;

	public InfoButtons btnCurrentObjective;

	public InfoButtons btnResize;

	public InfoButtons btnHideHierarchy;

	public InfoButtons btnShowHierarchy;

	public InfoNode Index;

	public InfoNode CurrentParent;

	public InfoNode CurrentChild;

	public InfoNode NextParent;

	public InfoNode NextChild;

	public List<JsonInfoNode> jsonNodes = new List<JsonInfoNode>();

	public InfoNode currentObjectiveNode;

	public Dictionary<string, InfoNode> mapNodes = new Dictionary<string, InfoNode>();

	[HideInInspector]
	public List<InfoPanel> panelPool = new List<InfoPanel>();

	[HideInInspector]
	public List<InfoPanel> active = new List<InfoPanel>();

	[HideInInspector]
	public Color dragbarStart;

	[HideInInspector]
	public Color arrowStart;

	[HideInInspector]
	public Color arrowPressed;

	[HideInInspector]
	public Color noImageColor;

	[HideInInspector]
	public Material titleReferenceMaterial;

	[HideInInspector]
	public Material bodyReferenceMaterial;

	[HideInInspector]
	public int dragBarHighlightState;

	[HideInInspector]
	public Vector2 bodyTextOffsetMax;

	[HideInInspector]
	public Vector2 bodyTextOffsetMin = Vector2.zero;

	[HideInInspector]
	public bool displayed;

	[HideInInspector]
	public bool resizing;

	[HideInInspector]
	public bool deadzoneEscaped;

	[HideInInspector]
	public bool scrollingList;

	[HideInInspector]
	public bool scrollingBody;

	[HideInInspector]
	public bool triggerDataHandlerInit;

	[HideInInspector]
	public bool draggingWindow;

	[HideInInspector]
	public Vector3 resizeStart;

	[HideInInspector]
	public Vector3 resizeMin = new Vector2(700f, 500f);

	[HideInInspector]
	public Vector3 resizeMax = new Vector2(1400f, 1000f);

	[HideInInspector]
	public Vector3 mouseClickResizeStart;

	[HideInInspector]
	public Vector3 scrollListVel;

	[HideInInspector]
	public Vector3 scrollBodyVel;

	[HideInInspector]
	public Vector2 lastBodyTextViewportSize;

	[HideInInspector]
	public float lastBodyTextPreferredHeight;

	[HideInInspector]
	public float scrollListAccel;

	[HideInInspector]
	public float scrollBodyAccel;

	private bool listHidden;

	private float dragBarColorVel;

	private Vector3 dragOffsetAtClick;
}
