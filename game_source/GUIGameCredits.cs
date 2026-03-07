using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIGameCredits : MonoBehaviour
{
	private void Start()
	{
		if (GUIGameCredits.dictCredits == null)
		{
			GUIGameCredits.Init();
		}
		this.cg = base.GetComponent<CanvasGroup>();
		this.sb.Append("<b>Ostranauts</b>\nA Game by Daniel Fedor\n\n");
		foreach (KeyValuePair<string, string> keyValuePair in GUIGameCredits.dictCredits)
		{
			this.sb.Append("<alpha=#FF>" + keyValuePair.Key + " - <alpha=#AA>" + keyValuePair.Value);
			this.sb.AppendLine();
		}
		this.sb.AppendLine();
		this.sb.AppendLine();
		this.sb.Append("<b>Trailers By:</b>");
		this.sb.AppendLine();
		foreach (string value in GUIGameCredits.aTrailerCredits)
		{
			this.sb.Append(value);
			this.sb.AppendLine();
		}
		this.sb.AppendLine();
		this.sb.AppendLine();
		this.sb.Append("<b>Other Credits:</b>");
		this.sb.AppendLine();
		foreach (string value2 in GUIGameCredits.aOtherCredits)
		{
			this.sb.Append(value2);
			this.sb.AppendLine();
		}
		this.sb.AppendLine();
		this.sb.AppendLine();
		this.sb.Append("<b>Special Thanks:</b>");
		this.sb.AppendLine();
		foreach (string value3 in GUIGameCredits.aSpecialCredits)
		{
			this.sb.Append(value3);
			this.sb.AppendLine();
		}
		this.txtBodyL.text = this.sb.ToString();
	}

	private void Update()
	{
		if (Input.GetMouseButtonUp(0) && this.cg.alpha != 0f)
		{
			this.Close();
		}
		if (this.cg.alpha == 0f)
		{
			this.srCredits.verticalNormalizedPosition = 1.5f;
		}
		else
		{
			this.srCredits.verticalNormalizedPosition -= 0.0003f;
		}
	}

	private void Close()
	{
		base.GetComponent<GUIPanelFade>().StopAllCoroutines();
		this.cg.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, false, true);
		this.cg.interactable = false;
		this.cg.blocksRaycasts = false;
	}

	private static void Init()
	{
		GUIGameCredits.dictCredits = new Dictionary<string, string>();
		GUIGameCredits.dictCredits["Corey Waite Arnold"] = "Writing, Design";
		GUIGameCredits.dictCredits["Chris Blackbourn"] = "Programming, Design";
		GUIGameCredits.dictCredits["Ashley Coad"] = "Art";
		GUIGameCredits.dictCredits["Amandine Coget"] = "Programming, Architecture";
		GUIGameCredits.dictCredits["Cyrus Crashtest"] = "Art";
		GUIGameCredits.dictCredits["Josh Culler (a.k.a.Invisible Acropolis)"] = "Music";
		GUIGameCredits.dictCredits["Rochelle Fedor"] = "VO, Accounting";
		GUIGameCredits.dictCredits["Sarah Ford"] = "Art";
		GUIGameCredits.dictCredits["Freddy Frydenlund"] = "Art";
		GUIGameCredits.dictCredits["Terry Green"] = "Art";
		GUIGameCredits.dictCredits["Jérôme [M3rØj] Grenda"] = "Art";
		GUIGameCredits.dictCredits["Andreas Gschwari"] = "Design";
		GUIGameCredits.dictCredits["Cameron Harris"] = "Design";
		GUIGameCredits.dictCredits["Joe Anthony Howe"] = "Design, Art";
		GUIGameCredits.dictCredits["Bjørn Jacobsen (a.k.a.Cujo Sound)"] = "Audio";
		GUIGameCredits.dictCredits["Kitfox Games"] = "Marketing, Production, Investment";
		GUIGameCredits.dictCredits["Jessie Lam"] = "Art";
		GUIGameCredits.dictCredits["Sabina Lewis"] = "Art";
		GUIGameCredits.dictCredits["Tina Liu"] = "Production";
		GUIGameCredits.dictCredits["Charlie Martin"] = "Art";
		GUIGameCredits.dictCredits["Leonard Menchiari"] = "Art";
		GUIGameCredits.dictCredits["Adam \"Crusoe\" Minnie"] = "Design, Art";
		GUIGameCredits.dictCredits["Modern Wolf Ltd."] = "Marketing, Production, Investment";
		GUIGameCredits.dictCredits["Xalavier Nelson Jr."] = " Writing, Design";
		GUIGameCredits.dictCredits["Alex Nicholson"] = " Design";
		GUIGameCredits.dictCredits["Alexandra Orlando"] = " Community Manager";
		GUIGameCredits.dictCredits["Michael Rabenhaupt"] = "Programming";
		GUIGameCredits.dictCredits["Charis Reid"] = " Community Manager, Design, Art";
		GUIGameCredits.dictCredits["Michael Richardson"] = "Writing, Design, Programming, Art";
		GUIGameCredits.dictCredits["Fernando Rizo"] = "Design";
		GUIGameCredits.dictCredits["Tanya X. Short"] = "Design";
		GUIGameCredits.dictCredits["Joshua Simons"] = " Community Manager, Programming, Design, Art";
		GUIGameCredits.dictCredits["Emily Siu"] = "Art";
		GUIGameCredits.dictCredits["Eduardo \"Chiko\" Valenzuela"] = "Art";
		GUIGameCredits.dictCredits["Wintermute Company London Limited"] = "Marketing";
		GUIGameCredits.aTrailerCredits = new List<string>
		{
			"M. Joshua Cauller - Videographer",
			"Oliver Cross - Videographer",
			"Xin Ran Liu - Videographer",
			"Fernando Rizo - Writing",
			"Dominique Tipper - VO",
			"Amelia Tyler - VO"
		};
		GUIGameCredits.aOtherCredits = new List<string>
		{
			"Staffan Widegarn Åhlvik - unity-vhsglitch Effect",
			"Caves of Qud Team - Discord Bug Bot Development",
			"Christopher Huppertz - VHS Glitch Effect Footage",
			"Gökhan Gökçe - UnityStandaloneFileBrowser - MIT License",
			"Mona Shakibapour - Discord Bug Bot Development"
		};
		GUIGameCredits.aSpecialCredits = new List<string>
		{
			"David Claesson",
			"Flore de Fontaine Vive Curtaz",
			"Pilar Malo",
			"Tiffany Ren-Edson",
			"CaptainX56",
			"Lauran Carter",
			"Dean Cutsforth",
			"Kemal",
			"Cassandra Khaw",
			"David Park",
			"Betsy Rosenzweig",
			"arclight",
			"Stella Pycroft",
			"Lawrence Cecil",
			"Fuzz",
			"Michael Magee",
			"Dirty Rider",
			"Kaelum Duong",
			"Boomlover",
			"an_robit",
			"Antice",
			"Blake",
			"brother tony",
			"Commander Phoenix",
			"Eddie",
			"Flambo",
			"FoxenCrew",
			"Frission",
			"JungleForce",
			"nox",
			"R3D",
			"Stip",
			"TinTuna",
			"Totally Sus",
			"Alex Tritton",
			"happycat",
			"Jacob \"macinsight\" Hergemöller",
			"Brian Bucklew",
			"Jason Grinblat",
			"Tarn Adams"
		};
	}

	public static string GetRandomCredit()
	{
		if (GUIGameCredits.dictCredits == null)
		{
			GUIGameCredits.Init();
		}
		float num = 1f / (float)GUIGameCredits.dictCredits.Keys.Count;
		float num2 = num;
		foreach (KeyValuePair<string, string> keyValuePair in GUIGameCredits.dictCredits)
		{
			if (UnityEngine.Random.Range(0f, 1f) <= num2)
			{
				return keyValuePair.Key + " - " + keyValuePair.Value;
			}
			num2 += num;
		}
		return "Merga - Watching Dan";
	}

	private void KeyHandler()
	{
		if (GUIActionKeySelector.commandEscape.Down && this.cg.alpha == 1f)
		{
			this.Close();
		}
	}

	public static Dictionary<string, string> dictCredits;

	public static List<string> aTrailerCredits;

	public static List<string> aOtherCredits;

	public static List<string> aSpecialCredits;

	public TMP_Text txtBodyL;

	public ScrollRect srCredits;

	private CanvasGroup cg;

	private StringBuilder sb = new StringBuilder();
}
