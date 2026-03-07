using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIPDAFerry : MonoBehaviour
{
	protected void Awake()
	{
		this.dictPrices = new Dictionary<Button, Tuple<string, double>>();
		CanvasManager.HideCanvasGroup(this.cgRequest);
		CanvasManager.HideCanvasGroup(this.cgArrival);
		this.btnCancel.onClick.AddListener(delegate()
		{
			this.CancelFerry();
		});
		AudioManager.AddBtnAudio(this.btnCancel.gameObject, null, "ShipUIBtnPDAClick02");
		this.txtCancelBtnFee.text = "$" + 400.0.ToString("00");
	}

	public void Init()
	{
		if (CrewSim.bShipEdit)
		{
			return;
		}
		if (AIShipManager.FerryComingForCO(CrewSim.GetSelectedCrew().strID))
		{
			this.ShowArrival();
		}
		else
		{
			this.ShowRequest();
		}
	}

	private void Update()
	{
		if (this.cgArrival.alpha > 0f && StarSystem.fEpoch - this.fLastUpdate > 1.0)
		{
			this.txtArrivalETA.text = MathUtils.GetDurationFromS(AIShipManager.FerryETA(CrewSim.GetSelectedCrew().strID) - StarSystem.fEpoch, 4);
			this.fLastUpdate = StarSystem.fEpoch;
		}
	}

	private void ShowArrival()
	{
		CanvasManager.HideCanvasGroup(this.cgRequest);
		CanvasManager.ShowCanvasGroup(this.cgArrival);
		this.txtArrivalDest.text = AIShipManager.FerryDestForCO(CrewSim.GetSelectedCrew().strID);
	}

	private void ShowRequest()
	{
		CanvasManager.ShowCanvasGroup(this.cgRequest);
		CanvasManager.HideCanvasGroup(this.cgArrival);
		IEnumerator enumerator = this.tfListContent.GetEnumerator();
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
		this.tfListContent.DetachChildren();
		this.dictPrices.Clear();
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		Ship ship = selectedCrew.ship;
		Ship shipByRegID = CrewSim.system.GetShipByRegID(CollisionManager.strATCClosest);
		if (shipByRegID == null)
		{
			this.txtRequestATC.text = DataHandler.GetString("GUI_PDA_PASS_REQUEST_ERROR_ATC", false);
			this.txtRequestETA.text = DataHandler.GetString("GUI_PDA_PASS_REQUEST_ERROR_ETA", false);
			return;
		}
		this.txtRequestATC.text = shipByRegID.strRegID;
		this.fETA = AIShipManager.CalcFerryETA(shipByRegID, ship);
		this.txtRequestETA.text = MathUtils.GetDurationFromS(this.fETA, 4);
		Loot loot = DataHandler.GetLoot("FerryCanDock");
		List<Interaction> list = new List<Interaction>();
		foreach (string strName in loot.GetAllLootNames())
		{
			list.Add(DataHandler.GetInteraction(strName, null, false));
		}
		List<string> allStationsInATCRegion = AIShipManager.GetAllStationsInATCRegion(shipByRegID.strRegID);
		foreach (Ship ship2 in CrewSim.system.GetAllLoadedShips())
		{
			if (ship2 != ship && !ship2.IsStationHidden(false))
			{
				if (!ship.GetAllDockedShips().Contains(ship2))
				{
					if (!JsonTransit.IsTransitConnected(ship.strRegID, ship2.strRegID))
					{
						if (ship2.IsStation(false) || !(CrewSim.system.GetShipOwner(ship2.strRegID) != selectedCrew.strID))
						{
							double distance = MathUtils.GetDistance(shipByRegID.objSS, ship2.objSS);
							if (distance <= 3.342293712194078E-05 || allStationsInATCRegion.Contains(ship2.strRegID))
							{
								bool flag = true;
								foreach (Interaction interaction in list)
								{
									if (interaction != null && interaction.ShipTestThem != null && interaction.ShipTestThem.Matches(ship2, null))
									{
										interaction.objUs = selectedCrew;
										interaction.objThem = ship2.ShipCO;
										flag = interaction.Triggered(selectedCrew, false, false);
										break;
									}
								}
								if (flag)
								{
									GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.goRowTemplate, this.tfListContent);
									TMP_Text component = gameObject.transform.Find("txtName").GetComponent<TMP_Text>();
									component.text = ship2.publicName;
									double num = AIShipManager.CalcFerryETA(ship, ship2);
									double num2 = AIShipManager.FerryPriceFromETA(this.fETA + num);
									component = gameObject.transform.Find("txtPrice").GetComponent<TMP_Text>();
									component.text = "$" + num2;
									Button btn = gameObject.transform.Find("btnRequest").GetComponent<Button>();
									btn.onClick.AddListener(delegate()
									{
										this.RequestFerry(btn);
									});
									AudioManager.AddBtnAudio(btn.gameObject, null, "ShipUIBtnPDAClick02");
									this.dictPrices[btn] = new Tuple<string, double>(ship2.strRegID, num2);
								}
							}
						}
					}
				}
			}
		}
	}

	private void RequestFerry(Button btn)
	{
		Tuple<string, double> tuple = null;
		if (!this.dictPrices.TryGetValue(btn, out tuple))
		{
			return;
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		AIShipManager.RequestFerry(selectedCrew.strID, tuple.Item1, this.fETA, tuple.Item2);
		this.ShowArrival();
	}

	private void CancelFerry()
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		AIShipManager.CancelFerry(selectedCrew.strID);
		this.ShowRequest();
	}

	[SerializeField]
	private CanvasGroup cgRequest;

	[SerializeField]
	private CanvasGroup cgArrival;

	[SerializeField]
	private Transform tfListContent;

	[SerializeField]
	private GameObject goRowTemplate;

	[SerializeField]
	private TMP_Text txtArrivalETA;

	[SerializeField]
	private TMP_Text txtArrivalDest;

	[SerializeField]
	private TMP_Text txtRequestETA;

	[SerializeField]
	private TMP_Text txtRequestATC;

	[SerializeField]
	private TMP_Text txtCancelBtnFee;

	[SerializeField]
	private Button btnCancel;

	private double fETA;

	private double fLastUpdate;

	private Dictionary<Button, Tuple<string, double>> dictPrices;
}
