using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs.GUIFFWD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIFFWD : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.aLoots = new List<Loot>();
		this.aLoots.Add(DataHandler.GetLoot("CONDTick1HourFree"));
		this.aLoots.Add(DataHandler.GetLoot("CONDTick1HourSleep"));
		this.aLoots.Add(DataHandler.GetLoot("CONDTick1HourWork"));
		this.txtBtn = this.btnSubmit.transform.Find("txt").GetComponent<TMP_Text>();
		this.sldHours.onValueChanged.AddListener(delegate(float A_1)
		{
			this.SetUI();
			this.SetSlider();
		});
		this.txtMessage.gameObject.SetActive(false);
		this.cgReportHeader.alpha = 0f;
		this.cgSlider = this.sldHours.GetComponent<CanvasGroup>();
		this.cgSlider.alpha = 1f;
		this.btnSubmit.onClick.AddListener(delegate()
		{
			this.OnFFWDClick();
		});
		this.btnRoster.onClick.AddListener(delegate()
		{
			CrewSim.RaiseUI("Roster", CrewSim.GetSelectedCrew());
		});
		this._ffwdAnimation = UnityEngine.Object.Instantiate<FFWDAnimation>(this.prefabFFWDAnimation, base.transform);
	}

	private void SetUI()
	{
		if (this.COSelf == null || this.COSelf.Company == null || this.COSelf.ship == null)
		{
			return;
		}
		this.bCanSubmit = true;
		this.cgReportHeader.alpha = 0f;
		this.cgSlider.alpha = 1f;
		this.txtBtn.text = "GO";
		string collisionInfo = this.GetCollisionInfo();
		string pathPrediction = this.GetPathPrediction();
		this.txtShipPath.text = collisionInfo + pathPrediction;
	}

	private string GetCollisionInfo()
	{
		Ship ship = this.COSelf.ship;
		bool flag = ship.objSS.bBOLocked || ship.objSS.bIsBO;
		int num = 1000;
		float num2 = GUIFFWD.FFWDTIMEUNIT * 6f / (float)num;
		this.boCollision = null;
		BodyOrbit bodyOrbit = null;
		double num3 = double.PositiveInfinity;
		this.fCollisionTime = 0.0;
		this.ssNew = new ShipSitu();
		this.ssNew.CopyFrom(ship.objSS, false);
		BodyOrbit bo = CrewSim.system.GetBO(ship.strRegID);
		bool flag2 = ship.objSS.bOrbitLocked && bo != null;
		int num4 = (int)this.sldHours.value;
		bool flag3 = false;
		float num5 = 0f;
		while ((double)num5 <= 3600.0 * (double)num4)
		{
			if (flag2)
			{
				bo.UpdateTime(StarSystem.fEpoch + (double)num5, true, true);
			}
			Vector2 vector = default(Vector2);
			foreach (KeyValuePair<string, BodyOrbit> keyValuePair in CrewSim.system.aBOs)
			{
				if (flag)
				{
					bodyOrbit = keyValuePair.Value;
					num3 = 0.0;
					break;
				}
				if (!keyValuePair.Value.IsPlaceholder())
				{
					keyValuePair.Value.UpdateTime(StarSystem.fEpoch + (double)num5, true, true);
					double num6;
					if (flag2)
					{
						num6 = MathUtils.GetMagnitude(bo.vPos, keyValuePair.Value.vPos);
						if (num6 < keyValuePair.Value.RadiusAtmo)
						{
							keyValuePair.Value.UpdateTime(StarSystem.fEpoch, true, true);
							this.boCollision = keyValuePair.Value;
							this.fCollisionTime = (double)num5;
							flag3 = true;
							break;
						}
					}
					else
					{
						vector += CrewSim.system.GetGravAccelPoint(keyValuePair.Value, this.ssNew.vPosx, this.ssNew.vPosy);
						num6 = this.ssNew.GetDistance(keyValuePair.Value.dXReal, keyValuePair.Value.dYReal);
					}
					keyValuePair.Value.UpdateTime(StarSystem.fEpoch, true, true);
					if (num6 < num3)
					{
						bodyOrbit = keyValuePair.Value;
						num3 = num6;
					}
					if (num6 != 0.0)
					{
						double collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(ship, keyValuePair.Value);
						if (num6 < collisionDistanceAU)
						{
							this.boCollision = keyValuePair.Value;
							this.fCollisionTime = (double)num5;
							break;
						}
					}
				}
			}
			if (this.boCollision != null)
			{
				break;
			}
			ShipSitu shipSitu = null;
			if (ship.objSS.HasNavData())
			{
				shipSitu = ship.objSS.NavData.GetShipSituAtTime(StarSystem.fEpoch + (double)num5 + (double)num2, false);
			}
			if (shipSitu == null)
			{
				this.ssNew.vAccEx += vector;
				this.ssNew.TimeAdvance((double)num2, false);
				this.ssNew.vAccEx -= vector;
			}
			else
			{
				this.ssNew = shipSitu;
			}
			num5 += num2;
		}
		if (flag2)
		{
			bo.UpdateTime(StarSystem.fEpoch, true, true);
		}
		string text = "Checking flight path...";
		if (this.boCollision != null)
		{
			if (flag3)
			{
				string text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					" DANGER: Atmospheric Entry with ",
					this.boCollision.strName,
					" in ",
					MathUtils.GetTimeNAV(this.fCollisionTime)
				});
			}
			else
			{
				string text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					" DANGER: Collision predicted with ",
					this.boCollision.strName,
					" in ",
					MathUtils.GetTimeNAV(this.fCollisionTime)
				});
			}
		}
		else
		{
			string text2 = text;
			text = string.Concat(new string[]
			{
				text2,
				"ALL CLEAR: Closest approach to ",
				bodyOrbit.strName,
				" at ",
				MathUtils.GetDistUnits(num3)
			});
		}
		return text + "\n";
	}

	private string GetPathPrediction()
	{
		string result = "\n";
		double fEpoch = StarSystem.fEpoch + (double)(this.sldHours.value * 3600f);
		double distanceToDestination = this.COSelf.ship.objSS.GetDistanceToDestination(fEpoch);
		if (distanceToDestination > 0.0)
		{
			result = "Distance to plotted destination: " + MathUtils.GetDistUnits(distanceToDestination) + "\n";
		}
		else if (distanceToDestination < 0.0)
		{
			result = "Distance to plotted destination: Arrival Time Surpassed\n";
		}
		return result;
	}

	private bool IsTargetOfAIShip(Ship playerShip)
	{
		if (playerShip == null || playerShip.objSS.bIsBO)
		{
			return false;
		}
		foreach (Ship ship in CrewSim.system.dictShips.Values)
		{
			if (ship != null && !ship.bDestroyed && ship.IsAIShip && ship.shipScanTarget != null)
			{
				if (!(ship.shipScanTarget.strRegID != playerShip.strRegID))
				{
					if (ship.GetRCSRemain() > 0.0)
					{
						if (AIShipManager.GetShipType(ship) != AIType.NA)
						{
							if (ship.NavAIManned)
							{
								if (ship.objSS == null || ship.objSS.GetDistance(playerShip.objSS) <= 0.000668458706417963)
								{
									return true;
								}
							}
						}
					}
				}
			}
		}
		return false;
	}

	private void OnFFWDClick()
	{
		if (this.cgReportHeader.alpha > 0f)
		{
			this.sldHours.value = 1f;
			CrewSim.ToggleSFF();
			return;
		}
		if (!this.bCanSubmit)
		{
			Debug.Log("Cannot FFWD");
			return;
		}
		this.btnSubmit.interactable = false;
		CrewSim.objInstance.vhs.SetTime(8.0);
		CrewSim.objInstance.vhs.enabled = true;
		base.StartCoroutine(this.PreAnim());
	}

	private IEnumerator PreAnim()
	{
		AudioManager.am.PlayAudioEmitter("FFWD", false, true);
		yield return new WaitForSeconds(0.5f);
		this._ffwdAnimation.Run(new Action(this.FFWD), new Action(this.UnlockButton));
		yield break;
	}

	private void UnlockButton()
	{
		this.btnSubmit.interactable = true;
	}

	private IEnumerator Unpause()
	{
		CrewSim.Paused = false;
		yield return null;
		CrewSim.Paused = true;
		CrewSim.objInstance.vhs.enabled = false;
		yield break;
	}

	private void FFWD()
	{
		GUIFFWD.bFFWDActive = true;
		CrewSim.system.Update((double)this.fTimeDelta);
		int num = 0;
		this.cgSlider.alpha = 0f;
		this.cgReportHeader.alpha = 1f;
		List<Tuple<CondOwner, string>> list = new List<Tuple<CondOwner, string>>();
		List<CondOwner> list2 = new List<CondOwner>();
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsNotSleeping");
		IEnumerator enumerator = this.tfCrew.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				GUIFFWDRow component = transform.gameObject.GetComponent<GUIFFWDRow>();
				Tuple<CondOwner, string> tuple = new Tuple<CondOwner, string>(component.CO, "Effects: ");
				Tuple<CondOwner, string> tuple2 = tuple;
				tuple2.Item2 += component.ApplyEffects();
				if (condTrigger.Triggered(component.CO, null, true) && component.CO.OwnsShip(component.CO.ship.strRegID))
				{
					num += component.Ship;
				}
				UnityEngine.Object.Destroy(transform.gameObject);
				list.Add(tuple);
				list2.Add(component.CO);
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
		this.tfCrew.DetachChildren();
		if (this.COSelf.ship.Reactor != null)
		{
			this.COSelf.ship.Reactor.GetComponent<FusionIC>().CatchUp();
		}
		float num2 = (float)num * this.fTimeDelta / 3600f * 2.25f;
		string item = this.UndamageParts((double)num2);
		Tuple<CondOwner, string> item2 = new Tuple<CondOwner, string>(this.COSelf.ship.ShipCO, item);
		list.Add(item2);
		if (this.COSelf.ship.Reactor != null)
		{
			this.COSelf.ship.Reactor.GetComponent<FusionIC>().TimeNextRun = StarSystem.fEpoch;
			this.COSelf.ship.Reactor.GetComponent<FusionIC>().CatchUp();
		}
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("Blank");
		List<string> allLootNames = DataHandler.GetLoot("ACTFFWDTickersToAllow").GetAllLootNames();
		foreach (CondOwner condOwner in list2)
		{
			foreach (string strTicker in allLootNames)
			{
				JsonTicker ticker = condOwner.GetTicker(strTicker);
				if (ticker != null)
				{
					ticker.bTickWhileAway = true;
				}
			}
		}
		int hourFromS = MathUtils.GetHourFromS(StarSystem.fEpoch);
		foreach (CondOwner condOwner2 in this.COSelf.ship.GetCOs(condTrigger2, true, true, true))
		{
			condOwner2.CatchUp();
			if (condOwner2.Company != null)
			{
				condOwner2.ShiftChange(condOwner2.Company.GetShift(hourFromS, condOwner2), false);
			}
		}
		foreach (CondOwner condOwner3 in list2)
		{
			condOwner3.UpdateManual((int)this.fTimeDelta);
			foreach (string strTicker2 in allLootNames)
			{
				JsonTicker ticker2 = condOwner3.GetTicker(strTicker2);
				if (ticker2 != null)
				{
					ticker2.bTickWhileAway = false;
				}
			}
		}
		foreach (Tuple<CondOwner, string> tuple3 in list)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabCrewReportRow, this.tfCrew);
			TMP_Text component2 = gameObject.transform.Find("txtCrew").GetComponent<TMP_Text>();
			if (tuple3.Item1.HasCond("IsHuman"))
			{
				component2.text = tuple3.Item1.FriendlyName;
			}
			else
			{
				component2.text = tuple3.Item1.ship.strRegID + ": " + tuple3.Item1.ship.publicName;
			}
			component2 = gameObject.transform.Find("txtReport").GetComponent<TMP_Text>();
			component2.text = tuple3.Item2;
		}
		this.txtBtn.text = "DISMISS";
		this.txtShipPath.text = string.Empty;
		GUIFFWD.bFFWDActive = false;
		base.StartCoroutine(this.Unpause());
	}

	private string UndamageParts(double fAmount)
	{
		if (fAmount == 0.0)
		{
			return "No repairs done.";
		}
		double num = fAmount;
		CondTrigger condTrigger = new CondTrigger();
		condTrigger.aReqs = new string[]
		{
			"StatDamage"
		};
		List<CondOwner> cos = this.COSelf.ship.GetCOs(condTrigger, true, false, false);
		List<CondOwner> list = new List<CondOwner>();
		List<double> list2 = new List<double>();
		foreach (CondOwner condOwner in cos)
		{
			if (!condOwner.GetIsLikeNew())
			{
				double damageState = condOwner.GetDamageState();
				int num2 = 0;
				foreach (double num3 in list2)
				{
					double num4 = num3;
					if (num4 >= damageState)
					{
						break;
					}
					num2++;
				}
				list2.Insert(num2, damageState);
				list.Insert(num2, condOwner);
			}
		}
		int num5 = 0;
		if (list.Count > 0)
		{
			foreach (CondOwner condOwner2 in cos)
			{
				if (fAmount <= 0.0)
				{
					break;
				}
				double condAmount = condOwner2.GetCondAmount("StatDamage");
				if (fAmount >= condAmount)
				{
					condOwner2.ZeroCondAmount("StatDamage");
					fAmount -= condAmount;
				}
				else
				{
					condOwner2.SetCondAmount("StatDamage", condAmount - fAmount, 0.0);
					fAmount = 0.0;
				}
				condOwner2.Item.VisualizeOverlays(false);
				num5++;
			}
		}
		return string.Concat(new object[]
		{
			"Repaired ",
			num - fAmount,
			" damage on ",
			num5,
			" parts."
		});
	}

	private void SetSlider()
	{
		IEnumerator enumerator = this.tfCrew.GetEnumerator();
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
		this.tfCrew.DetachChildren();
		foreach (CondOwner condOwner in this.COSelf.Company.GetCrewMembers(null))
		{
			if (condOwner.ship.LoadState >= Ship.Loaded.Edit)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabCrewRow, this.tfCrew);
				GUIFFWDRow component = gameObject.GetComponent<GUIFFWDRow>();
				component.SetCrew(condOwner, Mathf.RoundToInt(this.sldHours.value));
			}
		}
		this.fTimeDelta = GUIFFWD.FFWDTIMEUNIT * this.sldHours.value;
		if (this.boCollision != null && (double)this.fTimeDelta >= this.fCollisionTime)
		{
			this.bCanSubmit = false;
			this.txtMessage.gameObject.SetActive(true);
			this.txtMessage.text = DataHandler.GetString("GUI_SFFWD_COLLISION", false);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnRefuelAcceptNeg", false, false);
		}
		else if (this.IsTargetOfAIShip(this.COSelf.ship))
		{
			this.bCanSubmit = false;
			this.txtMessage.gameObject.SetActive(true);
			this.txtMessage.text = DataHandler.GetString("GUI_SFFWD_TARGETOFSHIP", false);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnRefuelAcceptNeg", false, false);
		}
		else
		{
			this.bCanSubmit = true;
			this.txtMessage.gameObject.SetActive(false);
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		CrewSim.Paused = true;
		CrewSim.bPauseLock = true;
		this.SetUI();
		this.SetSlider();
	}

	public override void SaveAndClose()
	{
		CrewSim.bPauseLock = false;
		if (CrewSim.tplLastUI != null)
		{
			Interaction interactionCurrent = CrewSim.tplLastUI.Item2.GetInteractionCurrent();
			if (interactionCurrent != null && interactionCurrent.bRaisedUI)
			{
				interactionCurrent.fDuration = interactionCurrent.fDurationOrig;
				CrewSim.tplLastUI.Item2.SetTicker(interactionCurrent.strName, (float)interactionCurrent.fDurationOrig);
			}
		}
		CrewSim.objInstance.vhs.enabled = false;
		base.SaveAndClose();
	}

	public static bool Active
	{
		get
		{
			return GUIFFWD.bFFWDActive;
		}
	}

	public static readonly float FFWDTIMEUNIT = 3600f;

	[SerializeField]
	private TMP_Text txtShipPath;

	[SerializeField]
	private TMP_Text txtMessage;

	[SerializeField]
	private Button btnSubmit;

	[SerializeField]
	private Button btnRoster;

	[SerializeField]
	private Transform tfCrew;

	[SerializeField]
	private Slider sldHours;

	[SerializeField]
	private GameObject prefabCrewRow;

	[SerializeField]
	private GameObject prefabCrewReportRow;

	[SerializeField]
	private CanvasGroup cgReportHeader;

	private CanvasGroup cgSlider;

	[SerializeField]
	private FFWDAnimation prefabFFWDAnimation;

	private ShipSitu ssNew;

	private bool bCanSubmit;

	private float fTimeDelta;

	private double fCollisionTime;

	private BodyOrbit boCollision;

	private List<Loot> aLoots;

	private TMP_Text txtBtn;

	private FFWDAnimation _ffwdAnimation;

	private static bool bFFWDActive;
}
