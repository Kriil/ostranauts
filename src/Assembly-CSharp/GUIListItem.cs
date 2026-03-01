using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIListItem : MonoBehaviour
{
	private void Awake()
	{
		this.dropItem = base.GetComponentInChildren<Dropdown>();
		this.btnRemove = base.GetComponentInChildren<Button>();
		this.btnRemove.onClick.AddListener(delegate()
		{
			this.Remove();
		});
	}

	public void SetData(List<string> aStrings, string strSelect)
	{
		this.dropItem.ClearOptions();
		this.dropItem.AddOptions(aStrings);
		this.dropItem.value = aStrings.IndexOf(strSelect);
	}

	private void Update()
	{
	}

	private void Remove()
	{
		GUIListWidget componentInParent = base.GetComponentInParent<GUIListWidget>();
		if (componentInParent == null)
		{
			Debug.Log("Error: GUIListItem " + this.dropItem.options[this.dropItem.value].text + " has no parent.");
			return;
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private Dropdown dropItem;

	private Button btnRemove;
}
