using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIJobsRow : MonoBehaviour
{
	private void Awake()
	{
		this.bmpCenter = base.transform.Find("pnlCenter").GetComponent<Image>();
		this.txtTitle = base.transform.Find("txtTitle").GetComponent<TMP_Text>();
		this.txtPrice = base.transform.Find("txtPrice").GetComponent<TMP_Text>();
	}

	public bool Selected
	{
		get
		{
			return this.bmpCenter.color == this.clrDark;
		}
		set
		{
			if (value)
			{
				this.bmpCenter.color = this.clrDark;
				this.txtPrice.color = this.clrLight;
				this.txtTitle.color = this.clrLight;
			}
			else
			{
				if (this.bAltRow)
				{
					this.bmpCenter.color = this.clrMid;
				}
				else
				{
					this.bmpCenter.color = this.clrLight;
				}
				this.txtPrice.color = this.clrDark;
				this.txtTitle.color = this.clrDark;
			}
		}
	}

	public bool AltRow
	{
		get
		{
			return this.bAltRow;
		}
		set
		{
			this.bAltRow = value;
			if (this.Selected)
			{
				return;
			}
			if (this.bAltRow)
			{
				this.bmpCenter.color = this.clrMid;
			}
			else
			{
				this.bmpCenter.color = this.clrLight;
			}
		}
	}

	public JsonJobSave Job
	{
		get
		{
			return this.jsJobSave;
		}
		set
		{
			this.jsJob = null;
			this.jsJobSave = null;
			this.jsJobSave = value;
			string text = string.Empty;
			string text2 = string.Empty;
			if (this.jsJobSave != null)
			{
				this.jsJob = DataHandler.GetJob(this.jsJobSave.strJobName);
				if (this.jsJob != null)
				{
					Interaction interaction = DataHandler.GetInteraction(this.jsJob.strIASetupClient, null, false);
					if (interaction != null)
					{
						text2 = interaction.strTitle;
						text2 += "\n";
						text2 += this.jsJobSave.COClient().FriendlyName;
					}
				}
				text = DataHandler.GetString("GUI_JOBS_ROW_PRICE1", false);
				text += (this.jsJobSave.fCostContract + (double)((int)this.jsJobSave.fItemValue)).ToString("n");
				text += "\n";
				text += DataHandler.GetString("GUI_JOBS_ROW_PRICE2", false);
				text += (this.jsJobSave.fPayout * this.jsJobSave.fPayoutMult + (double)((int)this.jsJobSave.fItemValue)).ToString("n");
				text += " - ";
				text += (this.jsJobSave.fPayout * this.jsJobSave.fPayoutMult * 8.0 + (double)((int)this.jsJobSave.fItemValue)).ToString("n");
			}
			this.txtTitle.text = text2;
			this.txtPrice.text = text;
		}
	}

	public Color clrLight;

	public Color clrDark;

	public Color clrMid;

	private JsonJob jsJob;

	private JsonJobSave jsJobSave;

	private Image bmpCenter;

	private TMP_Text txtTitle;

	private TMP_Text txtPrice;

	private bool bAltRow;
}
