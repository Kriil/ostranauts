using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GUIPAX2020 : MonoBehaviour
{
	private void Start()
	{
		this.Init();
		this.aCGs = new List<CanvasGroup>
		{
			this.cgCaptain,
			this.cgSalvage,
			this.cgSocial,
			this.cgNav,
			this.cgReactor
		};
		this.cgDesc.alpha = 0f;
		this.cgDesc.interactable = false;
		this.cgCaptain.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SelectPanel(this.cgCaptain);
		});
		this.cgSalvage.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SelectPanel(this.cgSalvage);
		});
		this.cgSocial.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SelectPanel(this.cgSocial);
		});
		this.cgNav.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SelectPanel(this.cgNav);
		});
		this.cgReactor.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SelectPanel(this.cgReactor);
		});
		this.btnBack.onClick.AddListener(delegate()
		{
			this.Cancel();
		});
		this.btnGo.onClick.AddListener(delegate()
		{
			this.Go();
		});
	}

	private void Update()
	{
	}

	private void ShowPanel(CanvasGroup cg)
	{
		cg.alpha = 1f;
		cg.interactable = true;
	}

	private void HidePanel(CanvasGroup cg)
	{
		cg.alpha = 0f;
		cg.interactable = false;
	}

	private void SelectPanel(CanvasGroup cgSelect)
	{
		foreach (CanvasGroup canvasGroup in this.aCGs)
		{
			if (canvasGroup == cgSelect)
			{
				this.ShowPanel(canvasGroup);
				canvasGroup.GetComponent<RectTransform>().anchorMin = new Vector2(this.dictAnchorsX[this.cgCaptain].x, canvasGroup.GetComponent<RectTransform>().anchorMin.y);
				canvasGroup.GetComponent<RectTransform>().anchorMax = new Vector2(this.dictAnchorsX[this.cgCaptain].y, canvasGroup.GetComponent<RectTransform>().anchorMax.y);
			}
			else
			{
				this.HidePanel(canvasGroup);
			}
		}
		this.ShowPanel(this.cgDesc);
		this.txtDesc.text = this.dictDescs[cgSelect];
	}

	private void Go()
	{
		CanvasGroup cgSelect = null;
		foreach (CanvasGroup canvasGroup in this.aCGs)
		{
			if (canvasGroup.interactable)
			{
				cgSelect = canvasGroup;
				break;
			}
		}
		this.pax2020Music.Stop();
		string text = this.dictJsons[cgSelect];
		if (text != null)
		{
			if (!(text == "PAX2020Captain"))
			{
				if (!(text == "PAX2020Salvage"))
				{
					if (!(text == "PAX2020Social"))
					{
						if (!(text == "PAX2020Nav"))
						{
							if (text == "PAX2020Reactor")
							{
								CrewSim.OnFinishLoading.AddListener(delegate()
								{
									CrewSim.objInstance.LoadGame(this.dictJsons[cgSelect] + ".json", null, null);
									CondOwner objTarget = null;
									foreach (KeyValuePair<string, CondOwner> keyValuePair in DataHandler.mapCOs)
									{
										if (keyValuePair.Value.strName == "ItmReactorIC03Off")
										{
											objTarget = keyValuePair.Value;
										}
									}
									Interaction interaction = DataHandler.GetInteraction("GUIReactor", null, false);
									CrewSim.coPlayer.AddCondAmount("IsPAX2020Short", 1.0, 0.0, 0f);
									CrewSim.coPlayer.QueueInteraction(objTarget, interaction, true);
									CrewSim.objInstance.CamCenter(CrewSim.coPlayer);
									GUIPAX2020Summary.SetDone("Salvage");
								});
								SceneManager.LoadScene("CrewSim");
							}
						}
						else
						{
							CrewSim.OnFinishLoading.AddListener(delegate()
							{
								CrewSim.objInstance.LoadGame(this.dictJsons[cgSelect] + ".json", null, null);
								CondOwner objTarget = null;
								foreach (KeyValuePair<string, CondOwner> keyValuePair in DataHandler.mapCOs)
								{
									if (keyValuePair.Value.strName == "ItmStationNav")
									{
										objTarget = keyValuePair.Value;
									}
								}
								Interaction interaction = DataHandler.GetInteraction("GUINavStation", null, false);
								CrewSim.coPlayer.AddCondAmount("IsPAX2020Short", 1.0, 0.0, 0f);
								CrewSim.coPlayer.QueueInteraction(objTarget, interaction, true);
								CrewSim.objInstance.CamCenter(CrewSim.coPlayer);
								GUIPAX2020Summary.SetDone("Salvage");
								GUIPAX2020Summary.SetDone("Reactor");
								GUIPAX2020Summary.SetDone("Social");
							});
							SceneManager.LoadScene("CrewSim");
						}
					}
					else
					{
						CrewSim.OnFinishLoading.AddListener(delegate()
						{
							CrewSim.objInstance.LoadGame(this.dictJsons[cgSelect] + ".json", null, null);
							CrewSim.coPlayer.AddCondAmount("IsPAX2020Short", 1.0, 0.0, 0f);
							CrewSim.objInstance.CamCenter(CrewSim.coPlayer);
						});
						SceneManager.LoadScene("CrewSim");
						GUIPAX2020Summary.SetDone("Salvage");
						GUIPAX2020Summary.SetDone("Reactor");
					}
				}
				else
				{
					CrewSim.OnFinishLoading.AddListener(delegate()
					{
						CrewSim.objInstance.LoadGame(this.dictJsons[cgSelect] + ".json", null, null);
						CrewSim.coPlayer.AddCondAmount("IsPAX2020Short", 1.0, 0.0, 0f);
						CrewSim.objInstance.CamCenter(CrewSim.coPlayer);
					});
					SceneManager.LoadScene("CrewSim");
				}
			}
			else
			{
				CrewSim.OnFinishLoading.AddListener(delegate()
				{
					CrewSim.objInstance.NewGame(null);
					CrewSim.coPlayer.AddCondAmount("IsPAX2020Short", 1.0, 0.0, 0f);
					CrewSim.objInstance.CamCenter(CrewSim.coPlayer);
				});
				SceneManager.LoadScene("CrewSim");
			}
		}
		this.pax2020Music.Play();
	}

	private void Cancel()
	{
		foreach (CanvasGroup canvasGroup in this.aCGs)
		{
			this.ShowPanel(canvasGroup);
			canvasGroup.GetComponent<RectTransform>().anchorMin = new Vector2(this.dictAnchorsX[canvasGroup].x, canvasGroup.GetComponent<RectTransform>().anchorMin.y);
			canvasGroup.GetComponent<RectTransform>().anchorMax = new Vector2(this.dictAnchorsX[canvasGroup].y, canvasGroup.GetComponent<RectTransform>().anchorMax.y);
		}
		this.HidePanel(this.cgDesc);
		this.btnBack.enabled = false;
		this.btnBack.enabled = true;
	}

	private void Init()
	{
		GUIPAX2020.musicRef = this.pax2020Music;
		this.dictDescs = new Dictionary<CanvasGroup, string>();
		this.dictDescs[this.cgCaptain] = "It's late in the shift cycle, and you are trashed. You're staring at yourself in the bathroom mirror, feeling the bassline of the bar's klax-pop karaoke in your stomach. How did you wind up on K-Leg, of all the nowhere, no-hope stations, and <i>why</i> did you have to wind up broke? There's exactly one game in this town: stripping ships for parts. That's what you're good at - trouble is, so is everyone else trapped here. You rub one bloodshot eye.\n\nYou need a job.\n\nYou stagger out into the corridor, dimly lit by an Ayotimiwa Employment Kiosk (\"One on every street corner!\"). The console blinks cheerily as it scans your face.\n\n \"How far, Zun Gui De Peng You?\" says Uncle Ayo's voice from a speaker. The friendly tone of the stock corporate greeting transitions into clipped, urbane English.\n\n\"So, applicant ... tell us about yourself.\"";
		this.dictDescs[this.cgSalvage] = "Just as you're about to join the line for the labor barge to the scrapyards, fate intervenes. You get <i>the call.</i>\n\nA simple job: a derelict ship, carefully kept off Ayotimiwa's shipbreaking schedule. You, a hardy spacer with a technical aptitude. A faceless client, willing to supply you with materiel for a patch-up-and-deliver job. What could go wrong?\n\nFor starters, your salvage rig malfunctions mid flight, leaving you without enough delta-v to return to port. All you can do is begin the matching burn to the derelict and pray that when you get there, it is salvageable.\n\nFind the reactor, start it up. Don't think about what'll happen if you can't. You repeat it to yourself as the ship draws nearer: 10,000 klicks. 5,000. 2,000. 1,000. 500 metres. It looms large and bright in your rig's spotlights. Find the reactor. Start it up.";
		this.dictDescs[this.cgSocial] = "The reactor comes to life before you, and you let out a whoop of exhilaration. In your helmet, it sounds like a dying bird. But nothing can dampen your excitement. This looks good. This looks salvageable.\n\nYou're able to pilot the dilapidated ship to one of K-Leg's docking arms using the ID supplied by your client. No one in the ATC seems to notice, or care. Just the way you want it for now. But flying this thing anywhere outside local space is a two person job, and for all your talent you are not two people. Now, the hardest part of this salvage mission, the one that made you balk when you read the briefing: you need to find a reliable subcontractor.\n\nThe pressure door slides up to admit the cacophony from Kure, the bazaar. You smell chicken adobo frying, hear the Tharsis gutter Mandarin and pidgin, and at least three different speakers tuned to the same farcast station, but somehow they're all out of sync. You breathe it in.\n\nNow, to find a pilot ...";
		this.dictDescs[this.cgNav] = "EXPERIMENTAL FEATURE\n\nForty-five minutes in the undock queue. Undock clearance. Pushback request. Pushback clearance. Half an hour on pushback, watching the station slide away from the hull cameras. RCS clearance requested. RCS clearance received. An hour on RCS thrusters maneuvering to your blast corridor. Fusion torch clearance request. Clearance received.\n\n\"Happy hunting,\" adds some joker on the comms. The discipline is lacking. Speaking of ...\n\nYou watch your new pilot working the controls. Can you really trust them? You're about to find out.";
		this.dictDescs[this.cgReactor] = "\"<i>Ahh.</i>\" Your exhalation of surprise is tinny and loud, your breath condensing on the EVA helmet's faceplate.\n\nThe reactor is a Sulaiman 500. You've only ever read about them - the big brother to the Moses 360, both of which supposedly sent Nod Helix bankrupt. This should be a museum piece, or dissected by some wunderkind team at the High Energy Physics labs on Deimos.\n\nConveniently, there's a start-up sequence panel in the lower left. You scan its to-do list, and begin to prod at the console ...";
		this.dictJsons = new Dictionary<CanvasGroup, string>();
		this.dictJsons[this.cgCaptain] = "PAX2020Captain";
		this.dictJsons[this.cgSalvage] = "PAX2020Salvage";
		this.dictJsons[this.cgSocial] = "PAX2020Social";
		this.dictJsons[this.cgNav] = "PAX2020Nav";
		this.dictJsons[this.cgReactor] = "PAX2020Reactor";
		this.dictAnchorsX = new Dictionary<CanvasGroup, Vector2>();
		this.dictAnchorsX[this.cgCaptain] = new Vector2(this.cgCaptain.GetComponent<RectTransform>().anchorMin.x, this.cgCaptain.GetComponent<RectTransform>().anchorMax.x);
		this.dictAnchorsX[this.cgSalvage] = new Vector2(this.cgSalvage.GetComponent<RectTransform>().anchorMin.x, this.cgSalvage.GetComponent<RectTransform>().anchorMax.x);
		this.dictAnchorsX[this.cgSocial] = new Vector2(this.cgSocial.GetComponent<RectTransform>().anchorMin.x, this.cgSocial.GetComponent<RectTransform>().anchorMax.x);
		this.dictAnchorsX[this.cgNav] = new Vector2(this.cgNav.GetComponent<RectTransform>().anchorMin.x, this.cgNav.GetComponent<RectTransform>().anchorMax.x);
		this.dictAnchorsX[this.cgReactor] = new Vector2(this.cgReactor.GetComponent<RectTransform>().anchorMin.x, this.cgReactor.GetComponent<RectTransform>().anchorMax.x);
	}

	public void ShowPAX2020Canvas()
	{
		base.GetComponent<CanvasGroup>().alpha = 1f;
		base.GetComponent<CanvasGroup>().interactable = true;
		base.GetComponent<CanvasGroup>().blocksRaycasts = true;
	}

	public CanvasGroup cgDesc;

	public CanvasGroup cgCaptain;

	public CanvasGroup cgSalvage;

	public CanvasGroup cgSocial;

	public CanvasGroup cgNav;

	public CanvasGroup cgReactor;

	public Button btnBack;

	public Button btnGo;

	public TMP_Text txtDesc;

	private List<CanvasGroup> aCGs;

	private Dictionary<CanvasGroup, string> dictDescs;

	private Dictionary<CanvasGroup, string> dictJsons;

	private Dictionary<CanvasGroup, Vector2> dictAnchorsX;

	public AudioSource pax2020Music;

	public static AudioSource musicRef;
}
