using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeCond : MonoBehaviour, INode
{
	private void Awake()
	{
		this.txtName = base.transform.Find("grpName/tbox").GetComponent<InputField>();
		this.txtDesc = base.transform.Find("grpDesc/tbox").GetComponent<InputField>();
		this.txtColor = base.transform.Find("grpColor/tbox").GetComponent<InputField>();
		this.txtDuration = base.transform.Find("grpDuration/tbox").GetComponent<InputField>();
		this.chkFatal = base.transform.Find("grpChecks/chkFatal").GetComponent<Toggle>();
		this.chkDisplay = base.transform.Find("grpChecks/chkDisplay").GetComponent<Toggle>();
		this.chkResetTimer = base.transform.Find("grpChecks/chkResetTimer").GetComponent<Toggle>();
		this.chkRemoveAll = base.transform.Find("grpChecks/chkRemoveAll").GetComponent<Toggle>();
		this.objCollide = base.gameObject.GetComponent<BoxCollider2D>();
		this.objParent = GameObject.Find("PlayState").GetComponent<DataEdit>();
		this.aListWidgets = new List<GUIListWidget>();
		this.goListWidget = Resources.Load<GameObject>("DataEdit/grpListWidget");
		this.txtName.onValueChanged.AddListener(delegate(string A_1)
		{
			this.Rename();
		});
		Button component = base.transform.Find("grpDuration/btnDelete").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.objParent.RemoveCondFromGame(this);
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
		DataHandler.dictConds.Remove(this.strNameOld);
		this.strNameOld = this.txtName.text;
		this.objParent.SaveNodes();
		this.objParent.UpdateDropdownCond();
	}

	public void DeleteNode()
	{
		this.objParent.RemoveNode(this);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void SetNodeData(string strIn)
	{
		this.txtName.text = strIn;
		this.strNameOld = this.txtName.text;
		if (DataHandler.dictConds.ContainsKey(strIn))
		{
			this.objCond = DataHandler.dictConds[strIn];
			this.txtDuration.text = this.objCond.fDuration.ToString();
			this.txtDesc.text = this.objCond.strDesc;
			this.txtColor.text = this.objCond.strColor;
			this.chkFatal.isOn = this.objCond.bFatal;
			this.chkRemoveAll.isOn = this.objCond.bRemoveAll;
			this.chkResetTimer.isOn = this.objCond.bResetTimer;
			this.objCTNext = this.AddListWidget<CondTrigger>("CondTrigs Next:", DataHandler.dictCTs, this.objCond.aNext);
			return;
		}
		string[] aValues = new string[0];
		this.objCTNext = this.AddListWidget<CondTrigger>("CondTrigs Next:", DataHandler.dictCTs, aValues);
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
		JsonCond jsonCond = new JsonCond();
		jsonCond.strName = this.txtName.text;
		jsonCond.strDesc = this.txtDesc.text;
		jsonCond.strColor = this.txtColor.text;
		float fDuration = 0f;
		float.TryParse(this.txtDuration.text, out fDuration);
		jsonCond.fDuration = fDuration;
		jsonCond.aNext = this.objCTNext.GetData();
		DataHandler.dictConds[jsonCond.strName] = jsonCond;
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

	public int _nLayoutColumn;

	private BoxCollider2D objCollide;

	private DataEdit objParent;

	public InputField txtName;

	private InputField txtDesc;

	private InputField txtColor;

	private InputField txtDuration;

	private Toggle chkFatal;

	private Toggle chkDisplay;

	private Toggle chkResetTimer;

	private Toggle chkRemoveAll;

	private Vector3 offset;

	private JsonCond objCond;

	private GUIListWidget objCTNext;

	private GameObject goListWidget;

	private List<GUIListWidget> aListWidgets;
}
