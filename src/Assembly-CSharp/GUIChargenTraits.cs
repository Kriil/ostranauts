using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIChargenTraits : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.tfPointsGreen = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent/lblPointsGreen");
		this.tfPointsRed = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent/lblPointsRed");
		this.txtSelectName = base.transform.Find("lblSelectName").GetComponent<Text>();
		this.txtSelectDesc = base.transform.Find("lblSelectDesc").GetComponent<Text>();
		this.lblWarning = base.transform.Find("lblWarning").GetComponent<TMP_Text>();
		Button component = base.transform.Find("btnAccept").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.Exit();
		});
		AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnSelfCareAccept", "ShipUIBtnSelfCareAcceptOut");
		this.txtSelectName.text = string.Empty;
		this.txtSelectDesc.text = string.Empty;
		this.lblWarning.alpha = 0f;
	}

	private void Update()
	{
		if (this.lblWarning.alpha > 0f)
		{
			this.timePassed += Time.deltaTime;
			float num = this.timePassed / this.duration;
			this.lblWarning.alpha = Mathf.Clamp01(1f - num);
		}
	}

	private void SetUI()
	{
		this.coUser = this.COSelf.GetInteractionCurrent().objThem;
		this.cgs = this.coUser.GetComponent<GUIChargenStack>();
		this.DrawShelves();
		this.DrawSidebar();
	}

	public static int Calc(CondOwner co)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, int[]> keyValuePair in DataHandler.dictTraitScores)
		{
			if (co.HasCond(keyValuePair.Key))
			{
				if (keyValuePair.Value[1] != 0)
				{
					if (keyValuePair.Value[0] > 0)
					{
						list.Add(keyValuePair.Key);
					}
					else
					{
						list2.Add(keyValuePair.Key);
					}
				}
			}
		}
		int count = list.Count;
		if (list2.Count > count)
		{
			count = list2.Count;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			if (list.Count > i)
			{
				num += DataHandler.dictTraitScores[list[i]][0];
			}
			if (list2.Count > i)
			{
				num2 += DataHandler.dictTraitScores[list2[i]][0];
			}
		}
		return num + num2;
	}

	private void Exit()
	{
		int num = GUIChargenTraits.Calc(this.coUser);
		if (num < 0)
		{
			this.Warning();
		}
		else
		{
			CrewSim.LowerUI(false);
		}
	}

	private void Warning()
	{
		this.lblWarning.alpha = 1f;
		this.timePassed = 0f;
		AudioManager.am.PlayAudioEmitter("ShipUIBtnSelfCareFaultyAccept", false, false);
	}

	public IEnumerator FadeDownWarning(float duration)
	{
		float timePassed = 0f;
		this.lblWarning.alpha = 1f;
		while (timePassed < duration)
		{
			timePassed += Time.deltaTime;
			float blend = Mathf.Clamp01(timePassed / duration);
			this.lblWarning.alpha = Mathf.Lerp(this.lblWarning.alpha, 0f, blend);
			yield return null;
		}
		yield break;
	}

	private void DrawShelves()
	{
		Transform transform = base.transform.Find("pnlMain/Viewport/pnlMainContent");
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				UnityEngine.Object.Destroy(transform2.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		transform.DetachChildren();
		GameObject original = Resources.Load("GUIShip/GUIChargenTraits/bmpShelf") as GameObject;
		GameObject original2 = Resources.Load("GUIShip/GUIChargenTraits/chkTraitGreen") as GameObject;
		GameObject original3 = Resources.Load("GUIShip/GUIChargenTraits/chkTraitRed") as GameObject;
		Transform parent = null;
		int num = 3;
		using (Dictionary<string, int[]>.Enumerator enumerator2 = DataHandler.dictTraitScores.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				GUIChargenTraits.<DrawShelves>c__AnonStorey2 <DrawShelves>c__AnonStorey = new GUIChargenTraits.<DrawShelves>c__AnonStorey2();
				<DrawShelves>c__AnonStorey.objPair = enumerator2.Current;
				<DrawShelves>c__AnonStorey.$this = this;
				if (<DrawShelves>c__AnonStorey.objPair.Value[1] != 0)
				{
					if (num >= 3)
					{
						GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform);
						parent = gameObject.transform.Find("pnlShelf").transform;
						num = 0;
					}
					if (DataHandler.dictConds.ContainsKey(<DrawShelves>c__AnonStorey.objPair.Key))
					{
						Condition cond = DataHandler.GetCond(<DrawShelves>c__AnonStorey.objPair.Key);
						GameObject gameObject2;
						if (<DrawShelves>c__AnonStorey.objPair.Value[0] < 0)
						{
							gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original2, parent);
						}
						else
						{
							gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original3, parent);
						}
						AudioManager.AddBtnAudio(gameObject2.gameObject, "ShipUIBtnSelfCareClickIn", "ShipUIBtnSelfCareClickOut");
						gameObject2.transform.Find("Label").GetComponent<Text>().text = cond.strNameFriendly;
						Toggle chkToggle = gameObject2.GetComponent<Toggle>();
						chkToggle.isOn = this.coUser.HasCond(<DrawShelves>c__AnonStorey.objPair.Key);
						chkToggle.onValueChanged.AddListener(delegate(bool A_1)
						{
							bool isOn = chkToggle.isOn;
							<DrawShelves>c__AnonStorey.$this.SetCond(<DrawShelves>c__AnonStorey.objPair.Key, isOn);
							if (!<DrawShelves>c__AnonStorey.$this.coUser.HasCond(<DrawShelves>c__AnonStorey.objPair.Key))
							{
								<DrawShelves>c__AnonStorey.$this.SetCond(<DrawShelves>c__AnonStorey.objPair.Key, isOn);
							}
							<DrawShelves>c__AnonStorey.$this.DrawShelves();
							<DrawShelves>c__AnonStorey.$this.DrawSidebar();
						});
						chkToggle.GetComponent<GUIEnterExitHandler>().fnOnEnter = delegate()
						{
							<DrawShelves>c__AnonStorey.$this.DrawSelection(<DrawShelves>c__AnonStorey.objPair.Key);
						};
						chkToggle.GetComponent<GUIEnterExitHandler>().fnOnExit = delegate()
						{
							<DrawShelves>c__AnonStorey.$this.DrawSelection(string.Empty);
						};
						int num2 = Mathf.Abs(<DrawShelves>c__AnonStorey.objPair.Value[0]);
						for (int i = 0; i < 4; i++)
						{
							if (i + 1 > num2)
							{
								gameObject2.transform.Find("Background/bmpDot0" + (i + 1)).gameObject.SetActive(false);
							}
						}
						num++;
					}
				}
			}
		}
	}

	private void DrawSidebar()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, int[]> keyValuePair in DataHandler.dictTraitScores)
		{
			if (this.coUser.HasCond(keyValuePair.Key))
			{
				if (keyValuePair.Value[1] != 0)
				{
					if (keyValuePair.Value[0] > 0)
					{
						list.Add(keyValuePair.Key);
					}
					else
					{
						list2.Add(keyValuePair.Key);
					}
				}
			}
		}
		int count = list.Count;
		if (list2.Count > count)
		{
			count = list2.Count;
		}
		Transform transform = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent/pnlColumnLists/pnlPlusNames");
		Transform transform2 = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent/pnlColumnLists/pnlPlusValues");
		Transform transform3 = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent/pnlColumnLists/pnlNegNames");
		Transform transform4 = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent/pnlColumnLists/pnlNegValues");
		int childCount = transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			UnityEngine.Object.DestroyImmediate(transform.GetChild(0).gameObject);
			UnityEngine.Object.DestroyImmediate(transform2.GetChild(0).gameObject);
		}
		childCount = transform3.childCount;
		for (int j = 0; j < childCount; j++)
		{
			UnityEngine.Object.DestroyImmediate(transform3.GetChild(0).gameObject);
			UnityEngine.Object.DestroyImmediate(transform4.GetChild(0).gameObject);
		}
		GameObject original = Resources.Load("GUIShip/GUIChargenTraits/lblLeft") as GameObject;
		int num = 0;
		int num2 = 0;
		GameObject gameObject;
		GameObject gameObject2;
		for (int k = 0; k < count; k++)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform);
			gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, transform2);
			if (list.Count > k)
			{
				Condition cond = DataHandler.GetCond(list[k]);
				gameObject.GetComponent<Text>().text = cond.strNameFriendly;
				gameObject2.GetComponent<Text>().text = DataHandler.dictTraitScores[list[k]][0].ToString();
				num += DataHandler.dictTraitScores[list[k]][0];
			}
			else
			{
				gameObject.GetComponent<Text>().text = string.Empty;
				gameObject2.GetComponent<Text>().text = string.Empty;
			}
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform3);
			gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, transform4);
			if (list2.Count > k)
			{
				Condition cond2 = DataHandler.GetCond(list2[k]);
				gameObject.GetComponent<Text>().text = cond2.strNameFriendly;
				gameObject2.GetComponent<Text>().text = DataHandler.dictTraitScores[list2[k]][0].ToString();
				num2 += DataHandler.dictTraitScores[list2[k]][0];
			}
			else
			{
				gameObject.GetComponent<Text>().text = string.Empty;
				gameObject2.GetComponent<Text>().text = string.Empty;
			}
		}
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform);
		gameObject.GetComponent<Text>().color = Color.white;
		gameObject.GetComponent<Text>().text = "Subtotal";
		gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, transform2);
		gameObject2.GetComponent<Text>().color = Color.white;
		gameObject2.GetComponent<Text>().text = num.ToString();
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform3);
		gameObject.GetComponent<Text>().color = Color.white;
		gameObject.GetComponent<Text>().text = "Subtotal";
		gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, transform4);
		gameObject2.GetComponent<Text>().color = Color.white;
		gameObject2.GetComponent<Text>().text = num2.ToString();
		int num3 = num + num2;
		bool flag = num + num2 < 0;
		this.tfPointsGreen.gameObject.SetActive(!flag);
		this.tfPointsRed.gameObject.SetActive(flag);
		this.tfPointsGreen.GetComponent<Text>().text = "POINTS LEFT " + num3;
		this.tfPointsRed.GetComponent<Text>().text = "POINTS LEFT " + num3;
		LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
	}

	private void DrawSelection(string strTrait)
	{
		if (strTrait == string.Empty || strTrait == null)
		{
			this.txtSelectName.text = string.Empty;
			this.txtSelectDesc.text = string.Empty;
			return;
		}
		Condition cond = DataHandler.GetCond(strTrait);
		this.txtSelectName.text = cond.strNameFriendly;
		this.txtSelectDesc.text = DataHandler.dictConds[strTrait].strDesc.Replace("[us]", this.coUser.FriendlyName);
	}

	private void SetCond(string strCond, bool bAdd)
	{
		if (bAdd && !this.coUser.HasCond(strCond))
		{
			this.coUser.AddCondAmount(strCond, 1.0, 0.0, 0f);
		}
		else if (!bAdd && this.coUser.HasCond(strCond))
		{
			this.coUser.AddCondAmount(strCond, -1.0, 0.0, 0f);
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.SetUI();
	}

	private CondOwner coUser;

	private GUIChargenStack cgs;

	private Transform tfPointsGreen;

	private Transform tfPointsRed;

	private Text txtSelectName;

	private Text txtSelectDesc;

	private TMP_Text lblWarning;

	private float timePassed;

	private float duration = 3f;
}
