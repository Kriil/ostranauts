using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeInteraction : MonoBehaviour, INode
{
	private void Awake()
	{
		this.aArrowsIn = new List<GUIArrow>();
		this.aArrowsOut = new List<GUIArrow>();
		this.goLabelDropWidget = Resources.Load<GameObject>("DataEdit/grpLabelDrop");
		this.txtName = base.transform.Find("grpName/tboxName").GetComponent<InputField>();
		this.txtDesc = base.transform.Find("grpDesc/tboxDesc").GetComponent<InputField>();
		this.txtTargetPoint = base.transform.Find("grpTargetPoint/tboxTargetPoint").GetComponent<InputField>();
		this.txtAnim = base.transform.Find("grpAnim/tboxAnim").GetComponent<InputField>();
		this.txtBubble = base.transform.Find("grpBubble/tboxBubble").GetComponent<InputField>();
		this.txtDuration = base.transform.Find("grpDuration/tboxDuration").GetComponent<InputField>();
		this.chkDialogue = base.transform.Find("grpChecks/chkDialogue").GetComponent<Toggle>();
		this.chkPortraitThem = base.transform.Find("grpChecks/chkPortraitThem").GetComponent<Toggle>();
		this.dropLink = base.transform.Find("grpLink/dropLink").GetComponent<Dropdown>();
		this.objParent = GameObject.Find("PlayState").GetComponent<DataEdit>();
		this.objCollide = base.gameObject.GetComponent<BoxCollider2D>();
		this.txtName.onValueChanged.AddListener(delegate(string A_1)
		{
			this.Rename();
		});
		this.UpdateLinkDropdown(null);
		this.dropLink.onValueChanged.AddListener(delegate(int A_1)
		{
			this.LinkNode(this.dropLink);
		});
		Button component = base.transform.Find("grpLink/btnDelete").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.objParent.RemoveInteractionFromGame(this);
		});
		Button component2 = base.transform.Find("grpLink/btnUnlink").GetComponent<Button>();
		component2.onClick.AddListener(delegate()
		{
			this.UnlinkNode();
		});
	}

	private void Update()
	{
		this.Redraw();
	}

	private void Rename()
	{
		if (this.strNameOld == null)
		{
			return;
		}
		DataHandler.dictInteractions.Remove(this.strNameOld);
		this.strNameOld = this.txtName.text;
		this.objParent.SaveNodes();
		this.objParent.UpdateDropdownInteraction();
	}

	private void LinkNode(Dropdown drop)
	{
		this.objParent.AddInteraction(drop.options[drop.value].text, this.nLayoutColumn + 1, this);
		this.SaveData();
	}

	public void UpdateLinkDropdown(List<string> aItems = null)
	{
		if (aItems == null)
		{
			aItems = new List<string>();
			foreach (string item in DataHandler.dictInteractions.Keys)
			{
				aItems.Add(item);
			}
		}
		this.dropLink.ClearOptions();
		this.dropLink.AddOptions(aItems);
	}

	public void UnlinkNode()
	{
		List<GUIArrow> list = new List<GUIArrow>(this.aArrowsOut);
		foreach (GUIArrow guiarrow in list)
		{
			guiarrow.Unlink();
		}
		list = new List<GUIArrow>(this.aArrowsIn);
		foreach (GUIArrow guiarrow2 in list)
		{
			guiarrow2.Unlink();
		}
	}

	public void DeleteNode()
	{
		List<GUIArrow> list = new List<GUIArrow>(this.aArrowsOut);
		foreach (GUIArrow guiarrow in list)
		{
			guiarrow.Delete();
		}
		this.objParent.RemoveNode(this);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private Dropdown AddLabelDropWidget<T>(string strLabel, Dictionary<string, T> dictItems, string strValue)
	{
		List<string> list = new List<string>();
		foreach (string item in dictItems.Keys)
		{
			list.Add(item);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.goLabelDropWidget);
		gameObject.transform.SetParent(base.transform);
		Text componentInChildren = gameObject.GetComponentInChildren<Text>();
		Dropdown componentInChildren2 = gameObject.GetComponentInChildren<Dropdown>();
		componentInChildren.text = strLabel;
		componentInChildren2.ClearOptions();
		componentInChildren2.AddOptions(list);
		int num = list.IndexOf(strValue);
		if (num >= 0)
		{
			componentInChildren2.value = num;
		}
		else
		{
			componentInChildren2.value = 0;
		}
		return componentInChildren2;
	}

	private void OnMouseDown()
	{
		this.offset = base.gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));
	}

	private void OnMouseDrag()
	{
		if (DataEdit.goCurrentSel != null)
		{
			return;
		}
		Vector3 position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, base.transform.position.z);
		Vector3 position2 = Camera.main.ScreenToWorldPoint(position) + this.offset;
		base.transform.position = position2;
	}

	public void SetNodeData(string strIn)
	{
		this.txtName.text = strIn;
		this.strNameOld = this.txtName.text;
		this.objInteraction = DataHandler.GetInteraction(strIn, null, false);
		if (this.objInteraction == null)
		{
			string empty = string.Empty;
			this.dropLootCTThem = this.AddLabelDropWidget<CondTrigger>("Condition Triggers On Them Now:", DataHandler.dictCTs, empty);
			this.dropLootCTUs = this.AddLabelDropWidget<CondTrigger>("Condition Triggers On Us Now:", DataHandler.dictCTs, empty);
			this.dropCTThem = this.AddLabelDropWidget<JsonCond>("Reqs/Forbids On Them:", DataHandler.dictConds, empty);
			this.dropCTUs = this.AddLabelDropWidget<JsonCond>("Reqs/Forbids On Us:", DataHandler.dictConds, empty);
			this.dropLootItmAddThem = this.AddLabelDropWidget<Loot>("Items Added to Them:", DataHandler.dictLoot, empty);
			this.dropLootItmAddUs = this.AddLabelDropWidget<Loot>("Items Added to Us:", DataHandler.dictLoot, empty);
			this.dropLootItmGive = this.AddLabelDropWidget<Loot>("Items We Give to Them:", DataHandler.dictLoot, empty);
			this.dropLootItmRemoveThem = this.AddLabelDropWidget<Loot>("Items Removed From Them:", DataHandler.dictLoot, empty);
			this.dropLootItmRemoveUs = this.AddLabelDropWidget<Loot>("Items Removed From Us:", DataHandler.dictLoot, empty);
			this.dropLootItmTake = this.AddLabelDropWidget<Loot>("Items We Take From Them:", DataHandler.dictLoot, empty);
			return;
		}
		this.txtDesc.text = this.objInteraction.strDesc;
		if (this.objInteraction.strTargetPoint != null)
		{
			this.txtTargetPoint.text = this.objInteraction.strTargetPoint;
		}
		if (this.objInteraction.strAnim != null)
		{
			this.txtAnim.text = this.objInteraction.strAnim;
		}
		if (this.objInteraction.strBubble != null)
		{
			this.txtBubble.text = this.objInteraction.strBubble;
		}
		this.txtDuration.text = this.objInteraction.fDuration.ToString();
		this.chkDialogue.isOn = (this.objInteraction.nLogging == Interaction.Logging.GROUP);
		this.dropLootCTThem = this.AddLabelDropWidget<CondTrigger>("Condition Triggers On Them Now:", DataHandler.dictCTs, this.objInteraction.LootCTsThem.strName);
		this.dropLootCTUs = this.AddLabelDropWidget<CondTrigger>("Condition Triggers On Us Now:", DataHandler.dictCTs, this.objInteraction.LootCTsUs.strName);
		this.dropCTThem = this.AddLabelDropWidget<JsonCond>("Reqs/Forbids On Them:", DataHandler.dictConds, this.objInteraction.CTTestThem.strName);
		this.dropCTUs = this.AddLabelDropWidget<JsonCond>("Reqs/Forbids On Us:", DataHandler.dictConds, this.objInteraction.CTTestUs.strName);
		this.dropLootItmAddThem = this.AddLabelDropWidget<Loot>("Items Added to Them:", DataHandler.dictLoot, this.objInteraction.strLootItmAddThem);
		this.dropLootItmAddUs = this.AddLabelDropWidget<Loot>("Items Added to Us:", DataHandler.dictLoot, this.objInteraction.strLootItmAddUs);
		this.dropLootItmGive = this.AddLabelDropWidget<Loot>("Items We Give to Them:", DataHandler.dictLoot, this.objInteraction.strLootCTsGive);
		this.dropLootItmRemoveThem = this.AddLabelDropWidget<Loot>("Items Removed From Them:", DataHandler.dictLoot, this.objInteraction.strLootItmRemoveThem);
		this.dropLootItmRemoveUs = this.AddLabelDropWidget<Loot>("Items Removed From Us:", DataHandler.dictLoot, this.objInteraction.strLootCTsRemoveUs);
		this.dropLootItmTake = this.AddLabelDropWidget<Loot>("Items We Take From Them:", DataHandler.dictLoot, this.objInteraction.strLootCTsTake);
		foreach (string strIn2 in this.objInteraction.aInverse)
		{
			if (this.objParent == null)
			{
				break;
			}
			this.objParent.AddInteraction(strIn2, this.nLayoutColumn + 1, this);
		}
		JsonInteraction[] array = new JsonInteraction[DataHandler.dictInteractions.Values.Count];
		DataHandler.dictInteractions.Values.CopyTo(array, 0);
		foreach (JsonInteraction jsonInteraction in array)
		{
			int num = Array.FindIndex<string>(jsonInteraction.aInverse, (string name) => name.Contains(this.strNameOld));
			if (num >= 0)
			{
				this.objParent.AddInteraction(jsonInteraction.strName, this.nLayoutColumn - 1, null);
			}
		}
	}

	public void Redraw()
	{
		this.objCollide.size = new Vector2(this.objCollide.size.x, ((RectTransform)base.transform).rect.height);
		this.objCollide.offset = new Vector2(this.objCollide.offset.x, -this.objCollide.size.y / 2f);
	}

	public void AddArrow(GUIArrow objArrow, bool bIn)
	{
		if (bIn)
		{
			this.aArrowsIn.Add(objArrow);
		}
		else
		{
			this.aArrowsOut.Add(objArrow);
		}
		this.SaveData();
	}

	public void RemoveArrow(GUIArrow objArrow, bool bIn)
	{
		if (bIn)
		{
			this.aArrowsIn.Remove(objArrow);
		}
		else
		{
			this.aArrowsOut.Remove(objArrow);
		}
		this.SaveData();
	}

	public void SaveData()
	{
		JsonInteraction jsonInteraction = new JsonInteraction();
		jsonInteraction.strName = this.txtName.text;
		jsonInteraction.strDesc = this.txtDesc.text;
		if (this.txtTargetPoint.text != string.Empty)
		{
			jsonInteraction.strTargetPoint = this.txtTargetPoint.text;
		}
		else
		{
			jsonInteraction.strTargetPoint = null;
		}
		if (this.txtAnim.text != string.Empty)
		{
			jsonInteraction.strAnim = this.txtAnim.text;
		}
		else
		{
			jsonInteraction.strAnim = null;
		}
		if (this.txtBubble.text != string.Empty)
		{
			jsonInteraction.strBubble = this.txtBubble.text;
		}
		float num = 0f;
		float.TryParse(this.txtDuration.text, out num);
		jsonInteraction.fDuration = (double)num;
		jsonInteraction.aInverse = new string[this.aArrowsOut.Count];
		for (int i = 0; i < this.aArrowsOut.Count; i++)
		{
			jsonInteraction.aInverse[i] = this.aArrowsOut[i].DestName;
		}
		DataHandler.dictInteractions[jsonInteraction.strName] = jsonInteraction;
	}

	public int nLayoutColumn
	{
		get
		{
			return this._nLayoutColumn;
		}
		set
		{
			this._nLayoutColumn = value;
		}
	}

	Transform INode.get_transform()
	{
		return base.transform;
	}

	private string strNameOld;

	private Vector3 offset;

	public InputField txtName;

	private InputField txtDesc;

	private InputField txtTargetPoint;

	private InputField txtAnim;

	private InputField txtBubble;

	private InputField txtDuration;

	private Toggle chkDialogue;

	private Toggle chkPortraitThem;

	private Dropdown dropLink;

	private Interaction objInteraction;

	private List<GUIArrow> aArrowsIn;

	private List<GUIArrow> aArrowsOut;

	private GameObject goLabelDropWidget;

	private DataEdit objParent;

	public int _nLayoutColumn;

	private BoxCollider2D objCollide;

	private Dropdown dropLootCTUs;

	private Dropdown dropLootCTThem;

	private Dropdown dropCTUs;

	private Dropdown dropCTThem;

	private Dropdown dropLootItmAddUs;

	private Dropdown dropLootItmAddThem;

	private Dropdown dropLootItmRemoveUs;

	private Dropdown dropLootItmRemoveThem;

	private Dropdown dropLootItmGive;

	private Dropdown dropLootItmTake;
}
