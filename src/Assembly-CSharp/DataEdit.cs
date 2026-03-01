using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DataEdit : MonoBehaviour
{
	private void Awake()
	{
		if (!DataHandler.bLoaded)
		{
			DataHandler.Init();
		}
		this.aNodeList = new List<INode>();
		this.objInteractionTemplate = Resources.Load<GameObject>("DataEdit/pnlNodeInteraction");
		this.objCondTemplate = Resources.Load<GameObject>("DataEdit/pnlNodeCond");
		this.objCTTemplate = Resources.Load<GameObject>("DataEdit/pnlNodeCT");
		this.objArrowTemplate = Resources.Load<GameObject>("DataEdit/imgArrow");
		DataEdit.goCurrentSel = null;
		this.goCanvas = GameObject.Find("Canvas World");
		this.goCanvasArrows = GameObject.Find("Canvas Arrows");
		GameObject.Find("btnShipEdit").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SceneShipEdit();
		});
		this.btnNewInteraction = GameObject.Find("btnNewInteraction").GetComponent<Button>();
		this.btnNewInteraction.onClick.AddListener(delegate()
		{
			this.CreateNodeInteraction();
		});
		this.btnNewCond = GameObject.Find("btnNewCond").GetComponent<Button>();
		this.btnNewCond.onClick.AddListener(delegate()
		{
			this.CreateNodeCond();
		});
		this.btnNewCT = GameObject.Find("btnNewCT").GetComponent<Button>();
		this.btnNewCT.onClick.AddListener(delegate()
		{
			this.CreateNodeCT();
		});
		this.btnExportData = GameObject.Find("btnExportData").GetComponent<Button>();
		this.btnExportData.onClick.AddListener(delegate()
		{
			this.ExportData();
		});
		this.dropLoadInteraction = GameObject.Find("dropLoadInteraction").GetComponent<Dropdown>();
		this.UpdateDropdownInteraction();
		this.dropLoadInteraction.onValueChanged.AddListener(delegate(int A_1)
		{
			this.LoadNode(this.dropLoadInteraction);
		});
		this.dropLoadCond = GameObject.Find("dropLoadCond").GetComponent<Dropdown>();
		this.UpdateDropdownCond();
		this.dropLoadCond.onValueChanged.AddListener(delegate(int A_1)
		{
			this.LoadNode(this.dropLoadCond);
		});
		this.dropLoadCT = GameObject.Find("dropLoadCT").GetComponent<Dropdown>();
		this.UpdateDropdownCT();
		this.dropLoadCT.onValueChanged.AddListener(delegate(int A_1)
		{
			this.LoadNode(this.dropLoadCT);
		});
		this.CamZoom(1f);
	}

	private void Update()
	{
		if (!this.bLaidOut && this.aNodeList.Count > 0 && ((RectTransform)this.aNodeList[this.aNodeList.Count - 1].transform).rect.width > 0f)
		{
			this.aNodeList.Sort(delegate(INode objA, INode objB)
			{
				if (objA.nLayoutColumn < objB.nLayoutColumn)
				{
					return -1;
				}
				return 1;
			});
			Vector3 vector = new Vector3(this.aNodeList[0].transform.position.x, this.aNodeList[0].transform.position.y);
			float width = ((RectTransform)this.aNodeList[0].transform).rect.width;
			float num = -200f;
			for (int i = 1; i < this.aNodeList.Count; i++)
			{
				INode node = this.aNodeList[i];
				if (node.nLayoutColumn == this.aNodeList[i - 1].nLayoutColumn)
				{
					num -= ((RectTransform)this.aNodeList[i - 1].transform).rect.height + 100f;
				}
				else
				{
					num = Mathf.Pow(-1f, (float)node.nLayoutColumn) * 200f;
					vector.x += width * 2f;
				}
				node.transform.position = new Vector3(vector.x, vector.y + num);
			}
			for (int j = 0; j < this.aNodeList.Count; j++)
			{
				this.aNodeList[j].Redraw();
			}
			this.bLaidOut = true;
		}
		if (DataEdit.goCurrentSel == null)
		{
			this.KeyHandler();
		}
		this.MouseHandler();
	}

	private void SceneShipEdit()
	{
		SceneManager.LoadScene("ShipEdit");
	}

	private void LoadNode(Dropdown drop)
	{
		string text = drop.options[drop.value].text;
		List<INode> list = new List<INode>(this.aNodeList);
		foreach (INode node in list)
		{
			node.SaveData();
			node.DeleteNode();
		}
		if (drop == this.dropLoadInteraction)
		{
			this.AddInteraction(text, 0, null);
		}
		else if (drop == this.dropLoadCond)
		{
			this.AddCond(text, 0);
		}
		else if (drop == this.dropLoadCT)
		{
			this.AddCT(text, 0);
		}
	}

	private void CreateNodeInteraction()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objInteractionTemplate);
		gameObject.transform.SetParent(this.goCanvas.transform);
		NodeInteraction nodeInteraction = gameObject.AddComponent<NodeInteraction>();
		nodeInteraction.nLayoutColumn = -1;
		string text = "NewInteraction";
		int num = 0;
		while (DataHandler.dictInteractions.ContainsKey(text + num.ToString().PadLeft(3, '0')))
		{
			num++;
		}
		text += num.ToString().PadLeft(3, '0');
		nodeInteraction.SetNodeData(text);
		this.aNodeList.Add(nodeInteraction);
		this.SaveNodes();
		this.UpdateDropdownInteraction();
		this.bLaidOut = false;
	}

	private void CreateNodeCT()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objCTTemplate);
		gameObject.transform.SetParent(this.goCanvas.transform);
		NodeCT nodeCT = gameObject.AddComponent<NodeCT>();
		nodeCT.nLayoutColumn = -1;
		string text = "NewCT";
		int num = 0;
		while (DataHandler.dictCTs.ContainsKey(text + num.ToString().PadLeft(3, '0')))
		{
			num++;
		}
		text += num.ToString().PadLeft(3, '0');
		nodeCT.SetNodeData(text);
		this.aNodeList.Add(nodeCT);
		this.SaveNodes();
		this.UpdateDropdownCT();
		this.bLaidOut = false;
	}

	private void CreateNodeCond()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objCondTemplate);
		gameObject.transform.SetParent(this.goCanvas.transform);
		NodeCond nodeCond = gameObject.AddComponent<NodeCond>();
		nodeCond.nLayoutColumn = -1;
		string text = "NewCond";
		int num = 0;
		while (DataHandler.dictConds.ContainsKey(text + num.ToString().PadLeft(3, '0')))
		{
			num++;
		}
		text += num.ToString().PadLeft(3, '0');
		nodeCond.SetNodeData(text);
		this.aNodeList.Add(nodeCond);
		this.SaveNodes();
		this.UpdateDropdownCond();
		this.bLaidOut = false;
	}

	public NodeInteraction AddInteraction(string strIn, int nCol, NodeInteraction objOrigin = null)
	{
		NodeInteraction[] componentsInChildren = this.goCanvas.GetComponentsInChildren<NodeInteraction>();
		foreach (NodeInteraction nodeInteraction in componentsInChildren)
		{
			if (nodeInteraction.txtName.text == strIn)
			{
				if (objOrigin != null)
				{
					this.AddArrow(objOrigin, nodeInteraction);
				}
				return null;
			}
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objInteractionTemplate);
		gameObject.transform.SetParent(this.goCanvas.transform);
		NodeInteraction nodeInteraction2 = gameObject.AddComponent<NodeInteraction>();
		nodeInteraction2.nLayoutColumn = nCol;
		nodeInteraction2.SetNodeData(strIn);
		this.aNodeList.Add(nodeInteraction2);
		if (objOrigin != null)
		{
			this.AddArrow(objOrigin, nodeInteraction2);
		}
		this.bLaidOut = false;
		return nodeInteraction2;
	}

	public NodeCT AddCT(string strIn, int nCol)
	{
		NodeCT[] componentsInChildren = this.goCanvas.GetComponentsInChildren<NodeCT>();
		foreach (NodeCT nodeCT in componentsInChildren)
		{
			if (nodeCT.txtName.text == strIn)
			{
				return null;
			}
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objCTTemplate);
		gameObject.transform.SetParent(this.goCanvas.transform);
		NodeCT nodeCT2 = gameObject.AddComponent<NodeCT>();
		nodeCT2.nLayoutColumn = nCol;
		nodeCT2.SetNodeData(strIn);
		this.aNodeList.Add(nodeCT2);
		this.bLaidOut = false;
		return nodeCT2;
	}

	public NodeCond AddCond(string strIn, int nCol)
	{
		NodeCond[] componentsInChildren = this.goCanvas.GetComponentsInChildren<NodeCond>();
		foreach (NodeCond nodeCond in componentsInChildren)
		{
			if (nodeCond.txtName.text == strIn)
			{
				return null;
			}
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objCondTemplate);
		gameObject.transform.SetParent(this.goCanvas.transform);
		NodeCond nodeCond2 = gameObject.AddComponent<NodeCond>();
		nodeCond2.nLayoutColumn = nCol;
		nodeCond2.SetNodeData(strIn);
		this.aNodeList.Add(nodeCond2);
		this.bLaidOut = false;
		return nodeCond2;
	}

	public void RemoveNode(INode objNode)
	{
		this.aNodeList.Remove(objNode);
	}

	public void RemoveInteractionFromGame(NodeInteraction objNode)
	{
		objNode.UnlinkNode();
		DataHandler.dictInteractions.Remove(objNode.txtName.text);
		objNode.DeleteNode();
		this.UpdateDropdownInteraction();
	}

	public void RemoveCTFromGame(NodeCT objNode)
	{
		DataHandler.dictCTs.Remove(objNode.txtName.text);
		objNode.DeleteNode();
		this.UpdateDropdownCT();
	}

	public void RemoveCondFromGame(NodeCond objNode)
	{
		DataHandler.dictConds.Remove(objNode.txtName.text);
		objNode.DeleteNode();
		this.UpdateDropdownCond();
	}

	private void AddArrow(NodeInteraction objOrigin, NodeInteraction objDest)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.objArrowTemplate);
		gameObject.transform.SetParent(this.goCanvasArrows.transform);
		GUIArrow guiarrow = gameObject.AddComponent<GUIArrow>();
		guiarrow.SetNodes(objOrigin, objDest);
		objOrigin.AddArrow(guiarrow, false);
		objDest.AddArrow(guiarrow, true);
	}

	public void UpdateDropdownInteraction()
	{
		List<string> list = new List<string>();
		foreach (string item in DataHandler.dictInteractions.Keys)
		{
			list.Add(item);
		}
		this.dropLoadInteraction.ClearOptions();
		this.dropLoadInteraction.AddOptions(list);
		foreach (INode node in this.aNodeList)
		{
			NodeInteraction nodeInteraction = (NodeInteraction)node;
			nodeInteraction.UpdateLinkDropdown(list);
		}
	}

	public void UpdateDropdownCond()
	{
		List<string> list = new List<string>();
		foreach (string item in DataHandler.dictConds.Keys)
		{
			list.Add(item);
		}
		this.dropLoadCond.ClearOptions();
		this.dropLoadCond.AddOptions(list);
	}

	public void UpdateDropdownCT()
	{
		List<string> list = new List<string>();
		foreach (string item in DataHandler.dictCTs.Keys)
		{
			list.Add(item);
		}
		this.dropLoadCT.ClearOptions();
		this.dropLoadCT.AddOptions(list);
	}

	public void SaveNodes()
	{
		foreach (INode node in this.aNodeList)
		{
			node.SaveData();
		}
	}

	private void ExportData()
	{
		this.SaveNodes();
		DataHandler.DataToJsonStreaming<CondTrigger>(DataHandler.dictCTs, "condtrigs.json", false, string.Empty);
		DataHandler.DataToJsonStreaming<JsonCond>(DataHandler.dictConds, "conditions.json", false, string.Empty);
		DataHandler.DataToJsonStreaming<JsonInteraction>(DataHandler.dictInteractions, "interactions.json", false, string.Empty);
	}

	private void CamZoom(float fAmount)
	{
		Camera.main.orthographicSize *= fAmount;
	}

	private void KeyHandler()
	{
		float num = this.fCamSpeed;
		if (GUIActionKeySelector.commandPanFaster.Held)
		{
			num *= 10f;
		}
		if (GUIActionKeySelector.commandPanCameraUp.Held)
		{
			Camera.main.transform.Translate(0f, num * Camera.main.orthographicSize, 0f);
		}
		if (GUIActionKeySelector.commandPanCameraDown.Held)
		{
			Camera.main.transform.Translate(0f, -num * Camera.main.orthographicSize, 0f);
		}
		if (GUIActionKeySelector.commandPanCameraLeft.Held)
		{
			Camera.main.transform.Translate(-num * Camera.main.orthographicSize, 0f, 0f);
		}
		if (GUIActionKeySelector.commandPanCameraRight.Held)
		{
			Camera.main.transform.Translate(num * Camera.main.orthographicSize, 0f, 0f);
		}
		if (GUIActionKeySelector.commandZoomIn.Down)
		{
			this.CamZoom(0.5f);
		}
		if (GUIActionKeySelector.commandZoomOut.Down)
		{
			this.CamZoom(2f);
		}
	}

	private void MouseHandler()
	{
		if (DataEdit.goCurrentSel != null && (DataEdit.goCurrentSel.GetComponent<Button>() != null || DataEdit.goCurrentSel.GetComponent<Dropdown>() != null || DataEdit.goCurrentSel.GetComponent<Toggle>() != null))
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
		if (Input.mouseScrollDelta.y > 0f)
		{
			this.CamZoom(0.5f);
		}
		if (Input.mouseScrollDelta.y < 0f)
		{
			this.CamZoom(2f);
		}
		if (Input.GetMouseButtonDown(1))
		{
		}
		if (Input.GetMouseButton(0))
		{
			DataEdit.goCurrentSel = EventSystem.current.currentSelectedGameObject;
		}
	}

	private List<INode> aNodeList;

	private GameObject goCanvas;

	private GameObject goCanvasArrows;

	private GameObject objInteractionTemplate;

	private GameObject objCondTemplate;

	private GameObject objCTTemplate;

	private GameObject objArrowTemplate;

	private float fCamSpeed = 0.01f;

	public static GameObject goCurrentSel;

	private bool bLaidOut;

	private Dropdown dropLoadInteraction;

	private Dropdown dropLoadCond;

	private Dropdown dropLoadCT;

	private Button btnNewInteraction;

	private Button btnNewCond;

	private Button btnNewCT;

	private Button btnExportData;
}
