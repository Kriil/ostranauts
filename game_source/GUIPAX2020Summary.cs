using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIPAX2020Summary : MonoBehaviour
{
	private void Start()
	{
		GUIPAX2020Summary.objInstance = this;
		this.aCGs = new List<CanvasGroup>
		{
			this.cgCaptain,
			this.cgSalvage,
			this.cgSocial,
			this.cgNav,
			this.cgReactor
		};
		this.dictCGs = new Dictionary<string, CanvasGroup>();
		this.dictCGs["Captain"] = this.cgCaptain;
		this.dictCGs["Salvage"] = this.cgSalvage;
		this.dictCGs["Social"] = this.cgSocial;
		this.dictCGs["Nav"] = this.cgNav;
		this.dictCGs["Reactor"] = this.cgReactor;
		GUIPAX2020Summary.SetDone(null);
		this.btnQuit.onClick.AddListener(delegate()
		{
			this.Quit();
		});
		this.btnProceed.onClick.AddListener(delegate()
		{
			this.Proceed();
		});
	}

	public static void HideSummary()
	{
		GUIPAX2020Summary.objInstance.HidePanel(GUIPAX2020Summary.objInstance.cg);
	}

	private void Update()
	{
	}

	public static void SetDone(string str)
	{
		if (GUIPAX2020Summary.dictDone == null)
		{
			GUIPAX2020Summary.dictDone = new Dictionary<string, bool>();
			GUIPAX2020Summary.ResetDones();
		}
		if (str != null)
		{
			GUIPAX2020Summary.dictDone[str] = true;
		}
	}

	public static void ResetDones()
	{
		GUIPAX2020Summary.dictDone["Captain"] = false;
		GUIPAX2020Summary.dictDone["Salvage"] = false;
		GUIPAX2020Summary.dictDone["Social"] = false;
		GUIPAX2020Summary.dictDone["Nav"] = false;
		GUIPAX2020Summary.dictDone["Reactor"] = false;
	}

	private static IEnumerator ShowSummary(float time)
	{
		yield return new WaitForSeconds(time);
		Debug.Log(GUIPAX2020Summary.bProceed);
		Debug.Log(GUIPAX2020Summary.strCard);
		Debug.Log(GUIPAX2020Summary.dictDone[GUIPAX2020Summary.strCard]);
		GUIPAX2020Summary.objInstance.ShowPanel(GUIPAX2020Summary.objInstance.cg);
		GUIPAX2020Summary.dictDone[GUIPAX2020Summary.strCard] = true;
		if (GUIPAX2020Summary.strCard != null && GUIPAX2020Summary.objInstance.dictCGs.ContainsKey(GUIPAX2020Summary.strCard))
		{
			GUIPAX2020Summary.objInstance.ShowPanel(GUIPAX2020Summary.objInstance.dictCGs[GUIPAX2020Summary.strCard]);
		}
		GUIPAX2020Summary.objInstance.txtDesc.text = GUIPAX2020Summary.strText;
		GUIPAX2020Summary.objInstance.btnProceed.gameObject.SetActive(GUIPAX2020Summary.bProceed);
		if (GUIPAX2020Summary.bProceed)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
		CrewSim.Paused = true;
		yield break;
	}

	public static void QueueSummary(float time)
	{
		if (GUIPAX2020Summary.strCard == null || (GUIPAX2020Summary.dictDone.ContainsKey(GUIPAX2020Summary.strCard) && GUIPAX2020Summary.dictDone[GUIPAX2020Summary.strCard]))
		{
			return;
		}
		GUIPAX2020Summary.objInstance.StartCoroutine(GUIPAX2020Summary.ShowSummary(time));
	}

	private void ShowPanel(CanvasGroup cg)
	{
		cg.alpha = 1f;
		cg.interactable = true;
		cg.blocksRaycasts = true;
	}

	private void HidePanel(CanvasGroup cg)
	{
		cg.alpha = 0f;
		cg.interactable = false;
		cg.blocksRaycasts = false;
	}

	private void Proceed()
	{
		GUIPAX2020.musicRef.Stop();
		string text = GUIPAX2020Summary.strCard;
		if (text != null)
		{
			if (!(text == "Captain"))
			{
				if (!(text == "Salvage"))
				{
					if (!(text == "Reactor"))
					{
						if (!(text == "Social"))
						{
							if (text == "Nav")
							{
								this.Quit();
							}
						}
						else
						{
							this.HidePanel(this.cg);
							List<Objective> objectivesByName = MonoSingleton<ObjectiveTracker>.Instance.GetObjectivesByName("Use Nav Station");
							foreach (Objective objective in objectivesByName)
							{
								MonoSingleton<ObjectiveTracker>.Instance.RemoveObjective(objective, ObjectiveTracker.REASON_COMPLETED, true);
							}
							CrewSim.Paused = false;
							GUIPAX2020.musicRef.Play();
						}
					}
					else
					{
						this.HidePanel(this.cg);
						Debug.Log("finished reactor");
						Ship ship = CrewSim.system.SpawnShip("OKLG", Ship.Loaded.Full);
						CrewSim.DockShip(CrewSim.coPlayer.ship, "OKLG");
						List<CondOwner> icos = ship.GetICOs1(new CondTrigger
						{
							aReqs = new string[]
							{
								"IsHuman"
							}
						}, false, true, false);
						CondOwner condOwner = null;
						CondOwner condOwner2 = null;
						foreach (CondOwner condOwner3 in icos)
						{
							if (condOwner3.HasCond("IsBeautiful"))
							{
								condOwner = condOwner3;
							}
							else if (condOwner3.HasCond("IsVengeful"))
							{
								condOwner2 = condOwner3;
							}
						}
						Relationship rel = new Relationship(condOwner2.pspec, new List<string>
						{
							"RELLoverEx"
						}, null);
						condOwner.GetComponent<global::Social>().AddPerson(rel);
						rel = new Relationship(condOwner.pspec, new List<string>
						{
							"RELLoverEx"
						}, null);
						condOwner2.GetComponent<global::Social>().AddPerson(rel);
						CrewSim.LowerUI(false);
						CrewSim.Paused = false;
					}
				}
				else
				{
					this.HidePanel(this.cg);
					CrewSim.Paused = false;
					Debug.Log("FInished salvage phase");
					GUIPAX2020.musicRef.Play();
				}
			}
			else
			{
				Debug.Log("Finished captain phase");
			}
		}
	}

	private void Quit()
	{
		GUIPAX2020.musicRef.Stop();
		UnityEngine.Object.Destroy(GUIPAX2020.musicRef.gameObject);
		GUIPAX2020Summary.ResetDones();
		CrewSim.QueueScene("MainMenu2", 0f);
	}

	public static string strCard;

	public static string strText;

	public static bool bProceed;

	private static GUIPAX2020Summary objInstance;

	public CanvasGroup cgDesc;

	public CanvasGroup cgCaptain;

	public CanvasGroup cgSalvage;

	public CanvasGroup cgSocial;

	public CanvasGroup cgNav;

	public CanvasGroup cgReactor;

	public Button btnQuit;

	public Button btnProceed;

	public TMP_Text txtDesc;

	private List<CanvasGroup> aCGs;

	public CanvasGroup cg;

	private Dictionary<string, CanvasGroup> dictCGs;

	public static Dictionary<string, bool> dictDone;
}
