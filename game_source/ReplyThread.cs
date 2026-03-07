using System;

public class ReplyThread
{
	public string strID { get; set; }

	public JsonInteractionSave jis { get; set; }

	public double fEpoch { get; set; }

	public bool bDone { get; set; }

	public bool Matches(string strIDUs, string strIDThem, string strIANew)
	{
		return strIDUs != null && strIDThem != null && strIANew != null && (this.strID == strIDUs && strIDThem == this.jis.objThem) && strIANew == this.jis.strName;
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.jis.objUs,
			"->",
			this.jis.strName,
			": ",
			this.bDone
		});
	}

	public bool Fulfills(string strIAName, string strThem)
	{
		if (strThem == null || strIAName == null || strThem != this.strID)
		{
			return false;
		}
		if (strIAName == "SocialCombatExit")
		{
			return true;
		}
		if (strIAName == "QuickWait")
		{
			return false;
		}
		Interaction interaction = DataHandler.GetInteraction(this.jis.strName, this.jis, true);
		if (interaction == null)
		{
			return false;
		}
		bool result = false;
		foreach (string text in interaction.aInverse)
		{
			string[] array = text.Split(new char[]
			{
				','
			});
			if (array[0] == strIAName || array[0] == "SOCBlank")
			{
				result = true;
				break;
			}
		}
		DataHandler.ReleaseTrackedInteraction(interaction);
		return result;
	}

	public ReplyThread Clone()
	{
		ReplyThread replyThread = base.MemberwiseClone() as ReplyThread;
		if (this.jis != null)
		{
			replyThread.jis = this.jis.Clone();
		}
		return replyThread;
	}
}
