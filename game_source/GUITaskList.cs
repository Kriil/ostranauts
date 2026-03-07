using System;
using System.Collections.Generic;
using UnityEngine;

public class GUITaskList : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.aRows = new List<GUITaskRow>();
	}

	private void Update()
	{
	}

	private void SetUI()
	{
		foreach (GUITaskRow obj in this.aRows)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.aRows.Clear();
		Transform parent = base.transform.Find("pnlList/Viewport/pnlListContent");
		GUITaskRow component = Resources.Load<GameObject>("GUIShip/GUITaskList/pnlTaskRow").GetComponent<GUITaskRow>();
		List<Task2> allTasks = CrewSim.objInstance.workManager.GetAllTasks();
		GUITaskRow guitaskRow = null;
		foreach (Task2 task in allTasks)
		{
			if (guitaskRow == null)
			{
				guitaskRow = UnityEngine.Object.Instantiate<GUITaskRow>(component, parent);
			}
			if (guitaskRow.SetTask(task))
			{
				this.aRows.Add(guitaskRow);
				guitaskRow = null;
			}
		}
		if (guitaskRow != null)
		{
			UnityEngine.Object.Destroy(guitaskRow.gameObject);
		}
	}

	private void OnCancel()
	{
		CrewSim.LowerUI(false);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.SetUI();
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
	}

	private List<GUITaskRow> aRows;
}
