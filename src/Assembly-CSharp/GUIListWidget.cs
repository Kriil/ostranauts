using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIListWidget : MonoBehaviour
{
	private void Awake()
	{
		this.lblList = base.GetComponentInChildren<Text>();
		this.btnAdd = base.GetComponentInChildren<Button>();
		this.btnAdd.onClick.AddListener(delegate()
		{
			this.AddBlank();
		});
		this.goListItem = Resources.Load<GameObject>("DataEdit/grpListItem");
	}

	private void Update()
	{
	}

	public void SetData(string strLabel, List<string> aItems, List<string> aSelections)
	{
		if (aItems == null || aItems.Count <= 0)
		{
			return;
		}
		this.lblList.text = strLabel;
		this.aAvailItems = aItems;
		foreach (string strValue in aSelections)
		{
			this.AddItem(strValue);
		}
	}

	private void AddItem(string strValue)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.goListItem);
		gameObject.transform.SetParent(base.transform);
		GUIListItem guilistItem = gameObject.AddComponent<GUIListItem>();
		guilistItem.SetData(this.aAvailItems, strValue);
	}

	private void AddBlank()
	{
		if (this.aAvailItems == null)
		{
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.goListItem);
		gameObject.transform.SetParent(base.transform);
		GUIListItem guilistItem = gameObject.AddComponent<GUIListItem>();
		guilistItem.SetData(this.aAvailItems, this.aAvailItems[0]);
	}

	public string[] GetData()
	{
		GUIListItem[] componentsInChildren = base.GetComponentsInChildren<GUIListItem>();
		string[] array = new string[componentsInChildren.Length];
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Dropdown componentInChildren = componentsInChildren[i].GetComponentInChildren<Dropdown>();
			array[i] = componentInChildren.options[componentInChildren.value].text;
		}
		return array;
	}

	private Text lblList;

	private Button btnAdd;

	private List<string> aAvailItems;

	private GameObject goListItem;
}
