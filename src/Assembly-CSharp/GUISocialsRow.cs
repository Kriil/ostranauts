using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUISocialsRow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	public void OnPointerClick(PointerEventData eventData)
	{
		CondOwner condOwner = null;
		if (this.strCOID != null && DataHandler.mapCOs.TryGetValue(this.strCOID, out condOwner) && condOwner.ship != null && condOwner.ship.LoadState >= Ship.Loaded.Edit)
		{
			CrewSim.objInstance.CamCenter(condOwner);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
	}

	public void OnPointerExit(PointerEventData eventData)
	{
	}

	public void SetContact(global::Social socUs, PersonSpec psContact, CondOwner coContact)
	{
		if (socUs == null || psContact == null)
		{
			return;
		}
		string fullName = psContact.FullName;
		this.strCOID = fullName;
		string text = string.Empty;
		Relationship relationship = socUs.GetRelationship(fullName);
		text = DataHandler.GetString("GUI_PDA_SOCIAL_REL", false);
		if (relationship != null)
		{
			bool flag = false;
			foreach (string strCondName in relationship.aRelationships)
			{
				if (flag)
				{
					text += ", ";
				}
				text += DataHandler.GetCondFriendlyName(strCondName);
				flag = true;
			}
		}
		string text2 = DataHandler.GetString("GUI_PDA_SOCIAL_CAR", false);
		string text3 = DataHandler.GetString("GUI_PDA_SOCIAL_LOC", false);
		string text4 = DataHandler.GetString("GUI_PDA_SOCIAL_NOT", false);
		if (coContact != null)
		{
			GUIChargenStack component = coContact.GetComponent<GUIChargenStack>();
			if (component != null)
			{
				text2 += component.GetLatestCareerName();
			}
			text3 += LinkOpener.GetShipLink(coContact.ship);
			this.bmpPortrait.texture = FaceAnim2.GetPNG(coContact);
		}
		else
		{
			text3 += DataHandler.GetString("GUI_PDA_SOCIAL_DECEASED", false);
			text4 = text4 + DataHandler.GetString("GUI_PDA_SOCIAL_DECEASED", false) + "\n";
			this.bmpPortrait.texture = DataHandler.LoadPNG("portraits/pbaseDeceased.png", false, false);
		}
		foreach (string str in relationship.aEvents)
		{
			text4 = text4 + str + "\n";
		}
		this.txtName.text = fullName;
		this.txtBody.text = string.Concat(new string[]
		{
			text,
			"\n",
			text2,
			"\n",
			text3,
			"\n",
			text4
		});
	}

	[SerializeField]
	private TMP_Text txtName;

	[SerializeField]
	private TMP_Text txtBody;

	[SerializeField]
	private RawImage bmpPortrait;

	private string strCOID;
}
