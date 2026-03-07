using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeCT : MonoBehaviour, INode
{
	private void Awake()
	{
		this.txtName = base.transform.Find("grpName/tboxName").GetComponent<InputField>();
		this.txtCount = base.transform.Find("grpCount/tboxCount").GetComponent<InputField>();
		this.txtChance = base.transform.Find("grpChance/tboxChance").GetComponent<InputField>();
		this.txtThreshMin = base.transform.Find("grpThreshMin/tboxThreshMin").GetComponent<InputField>();
		this.txtThreshMax = base.transform.Find("grpThreshMax/tboxThreshMax").GetComponent<InputField>();
		this.aListWidgets = new List<GUIListWidget>();
		this.goListWidget = Resources.Load<GameObject>("DataEdit/grpListWidget");
		this.dropCond = base.transform.Find("grpCond/dropCond").GetComponent<Dropdown>();
		this.objParent = GameObject.Find("PlayState").GetComponent<DataEdit>();
		this.objCollide = base.gameObject.GetComponent<BoxCollider2D>();
		this.UpdateCondDropdown(null);
		this.txtName.onValueChanged.AddListener(delegate(string A_1)
		{
			this.Rename();
		});
		Button component = base.transform.Find("grpChance/btnDelete").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.objParent.RemoveCTFromGame(this);
		});
	}

	private void Update()
	{
		this.Redraw();
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

	public void Redraw()
	{
		this.objCollide.size = new Vector2(this.objCollide.size.x, ((RectTransform)base.transform).rect.height);
		this.objCollide.offset = new Vector2(this.objCollide.offset.x, -this.objCollide.size.y / 2f);
	}

	private void Rename()
	{
		if (this.strNameOld == null)
		{
			return;
		}
		DataHandler.dictCTs.Remove(this.strNameOld);
		this.strNameOld = this.txtName.text;
		this.objParent.SaveNodes();
		this.objParent.UpdateDropdownCT();
	}

	public void DeleteNode()
	{
		this.objParent.RemoveNode(this);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void UpdateCondDropdown(List<string> aItems = null)
	{
		if (aItems == null)
		{
			aItems = new List<string>();
			foreach (string item in DataHandler.dictConds.Keys)
			{
				aItems.Add(item);
			}
		}
		this.dropCond.ClearOptions();
		this.dropCond.AddOptions(aItems);
	}

	public void SetNodeData(string strIn)
	{
		this.txtName.text = strIn;
		this.strNameOld = this.txtName.text;
		this.objCT = DataHandler.GetCondTrigger(strIn);
		if (this.objCT == null)
		{
			string[] aValues = new string[0];
			this.objForbids = this.AddListWidget<JsonCond>("Forbidden Conditions On Us:", DataHandler.dictConds, aValues);
			this.objReqs = this.AddListWidget<JsonCond>("Required Conditions On Us:", DataHandler.dictConds, aValues);
			return;
		}
		this.txtChance.text = this.objCT.fChance.ToString();
		this.txtCount.text = this.objCT.fCount.ToString();
		for (int i = 0; i < this.dropCond.options.Count; i++)
		{
			Dropdown.OptionData optionData = this.dropCond.options[i];
			if (optionData.text == this.objCT.strCondName)
			{
				this.dropCond.value = i;
				break;
			}
		}
		this.objForbids = this.AddListWidget<JsonCond>("Forbidden Conditions On Us:", DataHandler.dictConds, this.objCT.aForbids);
		this.objReqs = this.AddListWidget<JsonCond>("Required Conditions On Us:", DataHandler.dictConds, this.objCT.aReqs);
	}

	private GUIListWidget AddListWidget<T>(string strLabel, Dictionary<string, T> dictItems, string[] aValues)
	{
		List<string> list = new List<string>();
		List<string> aSelections = new List<string>(aValues);
		foreach (string item in dictItems.Keys)
		{
			list.Add(item);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.goListWidget);
		gameObject.transform.SetParent(base.transform);
		GUIListWidget guilistWidget = gameObject.AddComponent<GUIListWidget>();
		guilistWidget.SetData(strLabel, list, aSelections);
		this.aListWidgets.Add(guilistWidget);
		return guilistWidget;
	}

	public void SaveData()
	{
		if (this.objCT == null)
		{
			this.objCT = new CondTrigger();
		}
		this.objCT.strName = this.txtName.text;
		float fChance = 1f;
		float.TryParse(this.txtChance.text, out fChance);
		this.objCT.fChance = fChance;
		float fCount = 1f;
		float.TryParse(this.txtCount.text, out fCount);
		this.objCT.fCount = fCount;
		this.objCT.strCondName = this.dropCond.options[this.dropCond.value].text;
		this.objCT.aForbids = this.objForbids.GetData();
		this.objCT.aReqs = this.objReqs.GetData();
		DataHandler.dictCTs[this.objCT.strName] = this.objCT;
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

	public InputField txtName;

	public InputField txtCount;

	public InputField txtChance;

	public InputField txtThreshMin;

	public InputField txtThreshMax;

	private Dropdown dropCond;

	public int _nLayoutColumn;

	private CondTrigger objCT;

	private GUIListWidget objReqs;

	private GUIListWidget objForbids;

	private GameObject goListWidget;

	private List<GUIListWidget> aListWidgets;

	private BoxCollider2D objCollide;

	private DataEdit objParent;

	private Vector3 offset;
}
