using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIXPDR : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.chkXPDR = base.transform.Find("chkXPDR").GetComponent<Toggle>();
		this.chkXPDR.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleXPDR(this.chkXPDR.isOn);
		});
		this.cgXPDR = base.transform.Find("pnlXPDRTxt").GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(this.cgXPDR);
		this.cgXPDRLicensed = base.transform.Find("pnlXPDRTxt/txtLicensed").GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(this.cgXPDRLicensed);
		this.cgXPDRWanted = base.transform.Find("pnlXPDRTxt/txtWanted").GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(this.cgXPDRWanted);
		this.txtRegID = base.transform.Find("pnlXPDRTxt/txtRegID").GetComponent<TMP_Text>();
		this.txtShipName = base.transform.Find("pnlXPDRTxt/txtShipName").GetComponent<TMP_Text>();
		this.txtMake = base.transform.Find("pnlXPDRTxt/txtMake").GetComponent<TMP_Text>();
		this.txtModel = base.transform.Find("pnlXPDRTxt/txtModel").GetComponent<TMP_Text>();
		this.txtYear = base.transform.Find("pnlXPDRTxt/txtYear").GetComponent<TMP_Text>();
		this.txtDesignation = base.transform.Find("pnlXPDRTxt/txtDesignation").GetComponent<TMP_Text>();
	}

	private void Update()
	{
		if (StarSystem.fEpoch - this.fTimeLastUpdate > 1.0)
		{
			this.fTimeLastUpdate = StarSystem.fEpoch;
			this.bXPDRRetry = true;
		}
		if (this.bXPDRRetry && this.fEpochXPDRRetry <= StarSystem.fEpoch)
		{
			if (!this.chkXPDR.isOn)
			{
				this.bXPDRRetry = false;
			}
			else
			{
				this.SetTextXPDR(true, this.strXPDRRetryCOID);
			}
		}
	}

	public static bool GetXPDRStatusFromCO(string strXPDRCOID, out string strXPDRID)
	{
		strXPDRID = null;
		bool result = false;
		CondOwner condOwner = null;
		if (!string.IsNullOrEmpty(strXPDRCOID) && DataHandler.mapCOs.TryGetValue(strXPDRCOID, out condOwner))
		{
			result = condOwner.HasCond("IsOff");
			strXPDRID = condOwner.GetGPMInfo("Data", "strRegID");
		}
		if (strXPDRID == null)
		{
			strXPDRID = "?";
		}
		return result;
	}

	private void SetTextXPDR(bool bOn, string strXPDRCOID)
	{
		if (bOn)
		{
			string text = null;
			this.bXPDRRetry = GUIXPDR.GetXPDRStatusFromCO(strXPDRCOID, out text);
			this.strXPDRRetryCOID = strXPDRCOID;
			if (this.fEpochXPDRRetry < StarSystem.fEpoch)
			{
				this.fEpochXPDRRetry = StarSystem.fEpoch + 1.0;
			}
			if (this.bXPDRRetry)
			{
				return;
			}
			CanvasManager.ShowCanvasGroup(this.cgXPDR);
			Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
			if (shipByRegID == null || text == "?" || this.bXPDRRetry)
			{
				this.txtRegID.text = "------";
				this.txtShipName.text = "------";
				this.txtMake.text = "------";
				this.txtModel.text = "------";
				this.txtYear.text = "------";
				this.txtDesignation.text = "------";
				CanvasManager.HideCanvasGroup(this.cgXPDRLicensed);
				CanvasManager.HideCanvasGroup(this.cgXPDRWanted);
			}
			else
			{
				if (shipByRegID.strRegID == text)
				{
					this.txtRegID.text = shipByRegID.strRegID;
				}
				else
				{
					this.txtRegID.text = shipByRegID.strRegID + "*";
				}
				this.txtShipName.text = shipByRegID.publicName;
				this.txtMake.text = shipByRegID.make;
				this.txtModel.text = shipByRegID.model;
				this.txtYear.text = shipByRegID.year;
				this.txtDesignation.text = shipByRegID.designation;
				if (shipByRegID.HasOKLGSalvageLicense())
				{
					CanvasManager.ShowCanvasGroup(this.cgXPDRLicensed);
				}
				else
				{
					CanvasManager.HideCanvasGroup(this.cgXPDRLicensed);
				}
				if (CrewSim.system.IsOwnerWanted(text, shipByRegID.objSS.vPos))
				{
					CanvasManager.ShowCanvasGroup(this.cgXPDRWanted);
				}
				else
				{
					CanvasManager.HideCanvasGroup(this.cgXPDRWanted);
				}
			}
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgXPDR);
		}
	}

	private void ToggleXPDR(bool bValue)
	{
		string text = null;
		if (this.COSelf != null)
		{
			this.COSelf.ZeroCondAmount("IsOverrideOff");
			this.COSelf.ZeroCondAmount("IsOverrideOn");
			if (bValue)
			{
				this.COSelf.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
			}
			else
			{
				this.COSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
			}
			if (text == null)
			{
				text = this.COSelf.strID;
			}
		}
		this.SetTextXPDR(bValue, text);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		bool flag = this.COSelf.HasCond("IsOverrideOn");
		if (!flag && !this.COSelf.HasCond("IsOverrideOff"))
		{
			flag = !string.IsNullOrEmpty(this.COSelf.ship.strXPDR);
		}
		if (this.chkXPDR.isOn != flag)
		{
			this.chkXPDR.isOn = flag;
		}
		else
		{
			this.ToggleXPDR(this.chkXPDR.isOn);
		}
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
	}

	private Toggle chkXPDR;

	private TMP_Text txtRegID;

	private TMP_Text txtShipName;

	private TMP_Text txtMake;

	private TMP_Text txtModel;

	private TMP_Text txtYear;

	private TMP_Text txtDesignation;

	private CanvasGroup cgXPDR;

	private CanvasGroup cgXPDRLicensed;

	private CanvasGroup cgXPDRWanted;

	private bool bXPDRRetry;

	private double fEpochXPDRRetry;

	private string strXPDRRetryCOID;

	private double fTimeLastUpdate;
}
