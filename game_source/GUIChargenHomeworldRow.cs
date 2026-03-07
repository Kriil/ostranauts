using System;
using UnityEngine;
using UnityEngine.UI;

public class GUIChargenHomeworldRow : MonoBehaviour
{
	private void Awake()
	{
		this.lblColony = base.transform.Find("lblColony").GetComponent<Text>();
		this.chkRegion = base.transform.Find("chkRegion").GetComponent<Toggle>();
		this.LabelEN = this.chkRegion.transform.Find("LabelEN").GetComponent<Text>();
	}

	public void Init(JsonHomeworld jsh, ToggleGroup tg, Action<JsonHomeworld> fn)
	{
		this.jsh = jsh;
		this.lblColony.text = jsh.strColonyName;
		this.LabelEN.text = jsh.strATCCode;
		this.chkRegion.group = tg;
		this.chkRegion.onValueChanged.RemoveAllListeners();
		this.chkRegion.onValueChanged.AddListener(delegate(bool A_1)
		{
			fn(jsh);
		});
	}

	public JsonHomeworld Homeworld
	{
		get
		{
			return this.jsh;
		}
	}

	public Toggle chkRegion;

	private Text lblColony;

	private Text LabelEN;

	private JsonHomeworld jsh;
}
