using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUISocial : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.bPausesGame = true;
		this.tfContent = base.transform.Find("pnlList/scrollMask/pnlContent");
	}

	private void SetPerson(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		IEnumerator enumerator = this.tfContent.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
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
		base.transform.Find("txtTitleValue").GetComponent<TMP_Text>().text = co.strName;
		if (co.socUs == null)
		{
			return;
		}
		GUIChargenStack component = co.GetComponent<GUIChargenStack>();
		List<Relationship> allPeople = co.socUs.GetAllPeople();
		Transform original = Resources.Load<Transform>("GUIShip/GUISocial/pnlRow");
		foreach (Relationship relationship in allPeople)
		{
			Transform transform2 = UnityEngine.Object.Instantiate<Transform>(original, this.tfContent);
			TMP_Text component2 = transform2.Find("btnName/txt").GetComponent<TMP_Text>();
			TMP_Text component3 = transform2.Find("btnRels/txt").GetComponent<TMP_Text>();
			TMP_Text component4 = transform2.Find("btnLoc/txt").GetComponent<TMP_Text>();
			string text = relationship.pspec.FullName;
			if (relationship.pspec.strCO != null)
			{
				CondOwner condOwner = relationship.pspec.MakeCondOwner(PersonSpec.StartShip.OLD, null);
				GUIChargenStack component5 = condOwner.GetComponent<GUIChargenStack>();
				if (component5.GetLatestCareer() != null)
				{
					text = text + ", " + component5.GetLatestCareer().GetJC().strNameFriendly;
				}
			}
			component2.text = text;
			string text2 = string.Empty;
			foreach (string text3 in relationship.aRelationships)
			{
				if (!(text3 == string.Empty))
				{
					if (text2 != string.Empty)
					{
						text2 += ", ";
					}
					text2 += text3.Substring(3);
				}
			}
			foreach (string str in relationship.aEvents)
			{
				text2 = text2 + "\n" + str;
			}
			component3.text = text2;
			text2 = "Unknown";
			if (relationship.pspec.strCO != null)
			{
				text2 = relationship.pspec.MakeCondOwner(PersonSpec.StartShip.OLD, null).ship.strRegID;
			}
			else
			{
				text2 = relationship.pspec.strHomeworldNow;
			}
			component4.text = text2;
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(this.tfContent.GetComponent<RectTransform>());
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.SetPerson(coSelf);
	}

	private Transform tfContent;
}
