using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIOptionSelect : MonoBehaviour
{
	private void Awake()
	{
		this.drop = base.transform.Find("drop").GetComponent<TMP_Dropdown>();
		this.btnOK = base.transform.Find("btnOK").GetComponent<Button>();
		this.btnOK.onClick.AddListener(delegate()
		{
			this.ClickOK();
		});
		this.btnCancel = base.transform.Find("btnCancel").GetComponent<Button>();
		this.btnCancel.onClick.AddListener(delegate()
		{
			this.Close();
		});
		this.tbox = base.transform.Find("tbox").GetComponent<TMP_InputField>();
		this.tbox.onSelect.AddListener(delegate(string A_0)
		{
			CrewSim.Typing = true;
		});
		this.tbox.onDeselect.AddListener(delegate(string A_0)
		{
			CrewSim.Typing = false;
		});
	}

	private void Update()
	{
	}

	private void ClickOK()
	{
		if (this.act != null)
		{
			string text = this.tbox.text;
			if ((text == string.Empty || text == null) && this.drop.options.Count > 0)
			{
				text = this.drop.options[this.drop.value].text;
			}
			if (text == string.Empty || text == null)
			{
				text = string.Empty;
			}
			this.act(text);
		}
		this.Close();
	}

	private void Close()
	{
		GUIOptionSelect.bRaised = false;
		this.act = null;
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void Init(List<string> aOptions, UnityAction<string> actSet)
	{
		GUIOptionSelect.bRaised = true;
		this.drop.ClearOptions();
		if (aOptions != null)
		{
			this.drop.AddOptions(aOptions);
		}
		this.act = actSet;
	}

	private TMP_Dropdown drop;

	private Button btnOK;

	private Button btnCancel;

	private TMP_InputField tbox;

	private UnityAction<string> act;

	public static bool bRaised;
}
