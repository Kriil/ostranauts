using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIEncounter : GUIData, IPointerClickHandler, IEventSystemHandler
{
	protected override void Awake()
	{
		base.Awake();
		this.bmpPortrait = base.transform.Find("bmpPortrait");
		this.bmpImage = base.transform.Find("bmpImage").GetComponent<Image>();
		this.txtMain = base.transform.Find("pnlEncMain/txtMain").GetComponent<TMP_Text>();
		this.tfChoices = base.transform.Find("pnlEncChoices/pnlContent");
		this.dictResponses = new Dictionary<string, Interaction>();
	}

	private void LateUpdate()
	{
		int num = TMP_TextUtilities.FindIntersectingLink(this.txtMain, Input.mousePosition, null);
		if ((num == -1 && this.m_selectedLink != -1) || num != this.m_selectedLink)
		{
			GUIModal.Instance.Hide();
			this.m_selectedLink = -1;
		}
		if (num != -1 && num != this.m_selectedLink)
		{
			this.m_selectedLink = num;
			TMP_LinkInfo tmp_LinkInfo = this.txtMain.textInfo.linkInfo[num];
			Vector3 zero = Vector3.zero;
			RectTransformUtility.ScreenPointToWorldPointInRectangle(this.txtMain.rectTransform, Input.mousePosition, null, out zero);
			string linkID = tmp_LinkInfo.GetLinkID();
			if (linkID != null)
			{
				if (!(linkID == "id_1"))
				{
					if (linkID == "id_2")
					{
						GUIModal.Instance.SetText("Link 2", "This is Link 2 text.");
						GUIModal.Instance.ShowTooltip(true);
					}
				}
				else
				{
					GUIModal.Instance.SetText("Link 1", "This is Link 1 text.");
					GUIModal.Instance.ShowTooltip(true);
				}
			}
		}
	}

	private void SetEncounter(Interaction iaCurrent)
	{
		this.bmpPortrait.gameObject.SetActive(false);
		if (iaCurrent == null)
		{
			CrewSim.LowerUI(false);
		}
		this.dictResponses.Remove(iaCurrent.strName);
		foreach (string key in this.dictResponses.Keys)
		{
			this.dictResponses[key].Destroy();
		}
		this.dictResponses.Clear();
		IEnumerator enumerator2 = this.tfChoices.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				object obj = enumerator2.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator2 as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		string text = GrammarUtils.GenerateDescription(iaCurrent);
		this.txtMain.text = text;
		GameObject original = Resources.Load("GUIShip/GUIEncounter/btnChoice") as GameObject;
		string[] aInverse = iaCurrent.aInverse;
		for (int i = 0; i < aInverse.Length; i++)
		{
			string text2 = aInverse[i];
			string[] array = text2.Split(new char[]
			{
				','
			});
			Interaction iaNext = DataHandler.GetInteraction(array[0], null, false);
			if (iaNext != null)
			{
				CondOwner objUs = iaCurrent.objThem;
				CondOwner objThem = iaCurrent.objUs;
				if (array.Length == 3)
				{
					if (array[1] == "[us]")
					{
						objUs = iaCurrent.objUs;
					}
					if (array[2] == "[them]")
					{
						objThem = iaCurrent.objThem;
					}
				}
				if (iaNext.Triggered(objUs, objThem, false, false, false, true, null))
				{
					Transform transform2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfChoices).transform;
					iaNext.objUs = objUs;
					iaNext.objThem = objThem;
					text = GrammarUtils.GenerateDescription(iaNext);
					this.dictResponses[iaNext.strName] = iaNext;
					transform2.GetComponent<Button>().onClick.AddListener(delegate()
					{
						this.dictResponses[iaNext.strName].ApplyEffects(null, false);
						this.SetEncounter(this.dictResponses[iaNext.strName]);
					});
					transform2.GetComponentInChildren<TMP_Text>().text = text;
				}
			}
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(this.tfChoices.GetComponent<RectTransform>());
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Right)
		{
			return;
		}
		int num = TMP_TextUtilities.FindIntersectingLink(this.txtMain, Input.mousePosition, null);
		if (num != -1)
		{
			TMP_LinkInfo tmp_LinkInfo = this.txtMain.textInfo.linkInfo[num];
			string linkID = tmp_LinkInfo.GetLinkID();
			if (linkID != null)
			{
				if (!(linkID == "id_1"))
				{
					if (linkID == "id_2")
					{
						GUIModal.Instance.SetText("Link 2", "This is Link 2 text.");
						GUIModal.Instance.ShowModal();
					}
				}
				else
				{
					GUIModal.Instance.SetText("Link 1", "This is Link 1 text.");
					GUIModal.Instance.ShowModal();
				}
			}
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.SetEncounter(coSelf.GetInteractionCurrent());
	}

	private Transform bmpPortrait;

	private Image bmpImage;

	private TMP_Text txtMain;

	private Transform tfChoices;

	private int m_selectedLink = -1;

	private Dictionary<string, Interaction> dictResponses;
}
