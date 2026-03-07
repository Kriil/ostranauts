using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class LinkOpener : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.OnLinkClickedEvent = new OnLinkClicked();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			return;
		}
		TMP_Text component = base.GetComponent<TMP_Text>();
		int num = TMP_TextUtilities.FindIntersectingLink(component, eventData.position, base.GetComponentInParent<Canvas>().worldCamera);
		if (num != -1)
		{
			TMP_LinkInfo tmp_LinkInfo = component.textInfo.linkInfo[num];
			string linkID = tmp_LinkInfo.GetLinkID();
			this.OpenURL(linkID);
			Debug.Log("Clicked Link: " + linkID);
		}
		else if (!string.IsNullOrEmpty(this.strDefaultURL))
		{
			this.OpenURL(this.strDefaultURL);
			Debug.Log("Clicked Link: " + this.strDefaultURL);
		}
		this.OnLinkClickedEvent.Invoke();
		AudioManager.am.PlayAudioEmitter("UIObjectiveLinkClick", false, false);
	}

	private void OpenURL(string strURL)
	{
		if (string.IsNullOrEmpty(strURL))
		{
			return;
		}
		if (strURL.IndexOf("coid:") == 0)
		{
			CondOwner co = null;
			DataHandler.mapCOs.TryGetValue(strURL.Replace("coid:", string.Empty), out co);
			if (co != null)
			{
				if (co.ship.LoadState >= Ship.Loaded.Edit)
				{
					CrewSim.objInstance.CamCenter(co);
				}
				else
				{
					CondOwner selectedCrew = CrewSim.GetSelectedCrew();
					List<Relationship> knownSocialContacts = GUIPDA.GetKnownSocialContacts(selectedCrew);
					Relationship relationship = knownSocialContacts.FirstOrDefault((Relationship x) => x.pspec.FullName == co.strID);
					if (relationship != null)
					{
						CrewSim.guiPDA.State = GUIPDA.UIState.Socials;
						CrewSim.guiPDA.ScrollSocials(co.strID);
					}
					else
					{
						string @string = DataHandler.GetString("GUI_PDA_OBJECTIVE_CONTACT_NOT_FOUND", false);
						selectedCrew.LogMessage(@string, "Bad", selectedCrew.strID);
					}
				}
				Debug.Log("CO found!");
			}
			else
			{
				Debug.Log("CO not found.");
			}
		}
		else if (strURL.IndexOf("http://") == 0)
		{
			Application.OpenURL(strURL);
		}
		else if (strURL.IndexOf("regid:") == 0)
		{
			CrewSim.guiPDA.ToggleNAV(strURL.Replace("regid:", string.Empty));
		}
	}

	public static string GetShipLink(Ship ship)
	{
		if (ship == null || ship.bDestroyed)
		{
			return "ERROR: MISSING SHIP";
		}
		return string.Concat(new string[]
		{
			"<link=\"regid:",
			ship.strRegID,
			"\"><u>",
			ship.publicName,
			"</u></link>"
		});
	}

	public static string GetCOLink(CondOwner co)
	{
		if (co == null || co.bDestroyed)
		{
			return "ERROR: MISSING SHIP";
		}
		return string.Concat(new string[]
		{
			"<link=\"coid:",
			co.strID,
			"\"><u>",
			co.strNameFriendly,
			"</u></link>"
		});
	}

	public OnLinkClicked OnLinkClickedEvent;

	public const string URL_CO = "coid:";

	private const string URL_SHIP = "regid:";

	private const string URL_WEB = "http://";

	private const string LINK1 = "<link=\"";

	private const string LINK2 = "\"><u>";

	private const string LINK3 = "</u></link>";

	public string strDefaultURL;
}
