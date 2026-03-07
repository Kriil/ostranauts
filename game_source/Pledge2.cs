using System;
using UnityEngine;

public class Pledge2
{
	public virtual bool Init(CondOwner coUs, JsonPledge jpIn, CondOwner coThem = null)
	{
		if (coUs == null || jpIn == null || !jpIn.Valid())
		{
			return false;
		}
		this.Us = coUs;
		this.jp = jpIn;
		if (coThem != null)
		{
			this.Them = coThem;
		}
		else
		{
			this.strThemID = this.jp.strThemID;
			if (this.strThemID == "[them]")
			{
				Interaction interactionCurrent = this.Us.GetInteractionCurrent();
				if (interactionCurrent != null)
				{
					this.strThemID = interactionCurrent.objThem.strID;
				}
			}
		}
		return true;
	}

	public virtual bool Init(string strUs, JsonPledge jpIn, string strThem = null)
	{
		if (strUs == null || jpIn == null)
		{
			return false;
		}
		this.strUsID = strUs;
		this.strThemID = strThem;
		this.jp = jpIn;
		return true;
	}

	public virtual bool Finished()
	{
		if (this.jp == null)
		{
			return true;
		}
		if (this.jp.aIAEnd == null)
		{
			return false;
		}
		foreach (string strName in this.jp.aIAEnd)
		{
			Interaction interaction = DataHandler.GetInteraction(strName, null, false);
			if (interaction != null && interaction.Triggered(this.Us, this.Them, false, false, false, true, null))
			{
				if (interaction.bApplyChain)
				{
					interaction.objUs = this.Us;
					interaction.objThem = this.Them;
					interaction.ApplyChain(null);
				}
				else
				{
					this.Us.QueueInteraction(this.Them, interaction, false);
				}
				this.Us.RemovePledge(this);
				return true;
			}
		}
		return false;
	}

	protected bool Triggered()
	{
		if (this.jp != null)
		{
			Interaction interaction = DataHandler.GetInteraction(this.jp.strIATrigger, null, false);
			if (interaction != null)
			{
				return interaction.Triggered(this.Us, this.Them, false, false, true, true, null);
			}
		}
		return false;
	}

	public virtual bool Do()
	{
		return false;
	}

	public virtual bool IsEmergency()
	{
		if (this.jp == null)
		{
			return false;
		}
		Interaction interaction = DataHandler.GetInteraction(this.jp.strIAEmergency, null, false);
		if (interaction != null && interaction.Triggered(this.Us, this.Them, false, false, false, true, null))
		{
			this.Us.QueueInteraction(this.Them, interaction, false);
			return true;
		}
		return false;
	}

	public JsonPledgeSave GetJSON()
	{
		return new JsonPledgeSave
		{
			strName = this.jp.strName,
			strUsID = this.strUsID,
			strThemID = this.strThemID
		};
	}

	public void ForgetThem()
	{
		this.strThemID = null;
		this.objThemTemp = null;
	}

	public CondOwner Us
	{
		get
		{
			if ((this.objUsTemp == null || this.objUsTemp.tf == null) && this.strUsID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strUsID, out this.objUsTemp);
			}
			return this.objUsTemp;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.strUsID = value.strID;
			this.objUsTemp = value;
		}
	}

	public virtual CondOwner Them
	{
		get
		{
			if ((this.objThemTemp == null || this.objThemTemp.tf == null) && this.strThemID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strThemID, out this.objThemTemp);
			}
			return this.objThemTemp;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.strThemID = value.strID;
			this.objThemTemp = value;
		}
	}

	public int Priority
	{
		get
		{
			return Mathf.Clamp(this.jp.nPriority, 1, 10);
		}
	}

	public override string ToString()
	{
		if (this.jp == null)
		{
			base.ToString();
		}
		string text = string.Empty;
		if (this.jp.aIAEnd != null)
		{
			foreach (string text2 in this.jp.aIAEnd)
			{
				text = text + text2.ToString() + " ";
			}
		}
		return string.Concat(new object[]
		{
			this.jp.strType,
			"; End: ",
			text,
			"; Trig: ",
			this.jp.strIATrigger,
			"; Priority: ",
			this.jp.nPriority
		});
	}

	public string NameFriendly
	{
		get
		{
			if (this.jp == null)
			{
				return base.ToString();
			}
			return (!string.IsNullOrEmpty(this.jp.strNameFriendly)) ? this.jp.strNameFriendly : this.jp.strName;
		}
	}

	public static bool Same(Pledge2 pl1, Pledge2 pl2)
	{
		return pl1 == pl2 || (pl1 != null && pl2 != null && !(pl1.Us != pl2.Us) && !(pl1.Them != pl2.Them) && JsonPledge.Same(pl1.jp, pl2.jp));
	}

	public static bool Same(Pledge2 pl1, JsonPledge jp)
	{
		return (pl1 == null && jp == null) || (pl1 != null && jp != null && JsonPledge.Same(pl1.jp, jp));
	}

	private CondOwner objUsTemp;

	protected CondOwner objThemTemp;

	protected JsonPledge jp;

	private string strUsID;

	protected string strThemID;
}
