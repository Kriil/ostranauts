using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PledgeReply : Pledge2
{
	public override bool Do()
	{
		return !(base.Us == null) && this.Finished();
	}

	public override bool IsEmergency()
	{
		return !(base.Us == null) && !base.Us.HasCond("IsAIManual") && base.IsEmergency();
	}

	public override bool Finished()
	{
		if (base.Us.aReplies.Count == 0 || base.Us.HasCond("IsAIManual"))
		{
			return false;
		}
		Interaction interaction = null;
		Dictionary<Interaction, ReplyThread> dictionary = new Dictionary<Interaction, ReplyThread>();
		float num = 0f;
		bool bLogConds = base.Us.bLogConds;
		base.Us.bLogConds = false;
		bool flag = false;
		foreach (ReplyThread replyThread in base.Us.aReplies)
		{
			bool flag2 = false;
			Relationship relationship = null;
			if (base.Us.socUs != null)
			{
				relationship = base.Us.socUs.GetRelationship(replyThread.jis.objUs);
				if (relationship != null)
				{
					if (!relationship.IsContextDefault)
					{
						flag2 = true;
						if (!flag)
						{
							flag = true;
							dictionary.Clear();
							num = 0f;
						}
					}
					relationship.ApplyConds(base.Us, false);
				}
			}
			if (flag2 || !flag)
			{
				this.CheckReplies(replyThread, dictionary, relationship, ref num);
			}
			if (relationship != null)
			{
				relationship.ApplyConds(base.Us, true);
			}
		}
		base.Us.bLogConds = bLogConds;
		if (dictionary.Count > 0)
		{
			int num2 = MathUtils.Rand(0, dictionary.Count, MathUtils.RandType.Flat, null);
			int num3 = 0;
			foreach (KeyValuePair<Interaction, ReplyThread> keyValuePair in dictionary)
			{
				if (num3 == num2)
				{
					interaction = keyValuePair.Key;
				}
				else
				{
					DataHandler.ReleaseTrackedInteraction(keyValuePair.Key);
				}
				num3++;
			}
		}
		if (interaction != null)
		{
			bool flag3 = interaction.objUs.ship != interaction.objThem.ship;
			if (flag3)
			{
				interaction.strTargetPoint = null;
			}
			bool flag4 = interaction.objUs.QueueInteraction(interaction.objThem, interaction, flag3);
			return flag3 || flag4;
		}
		return false;
	}

	private void CheckReplies(ReplyThread rt, Dictionary<Interaction, ReplyThread> dictBestReplies, Relationship rel, ref float fReplyScoreBest)
	{
		if (rt.jis == null)
		{
			return;
		}
		Interaction interaction = DataHandler.GetInteraction(rt.jis.strName, rt.jis, true);
		if (interaction == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(string.Concat(new string[]
		{
			"***** ",
			base.Us.strName,
			" pledge checking replies to ",
			interaction.ToString(),
			" *****"
		}));
		bool flag = interaction.objUs == CrewSim.GetSelectedCrew();
		if (interaction.aInverse.Length == 1 && interaction.aInverse[0] == "SOCBlank" && rel != null)
		{
			List<string> lootNames = DataHandler.GetLoot(rel.strContext).GetLootNames(null, false, null);
			interaction.aInverse = lootNames.ToArray();
		}
		Interaction reply = interaction.GetReply();
		DataHandler.ReleaseTrackedInteraction(interaction);
		interaction = reply;
		if (interaction == null)
		{
			stringBuilder.AppendLine("***** " + base.Us.strName + " pledge checking replies Done: null *****");
			if (stringBuilder.Length > 0 && flag)
			{
				Debug.Log(stringBuilder.ToString());
			}
			return;
		}
		float num = base.Us.GetNetInteractionResult(interaction, false);
		stringBuilder.AppendLine(string.Concat(new object[]
		{
			"Chose ",
			interaction.strName,
			" score: ",
			num
		}));
		if (base.Us.RecentlyTried(interaction, false) >= 0.0)
		{
			if (num < 0f)
			{
				num *= 0.01f;
			}
			else
			{
				num *= 100f;
			}
		}
		if (dictBestReplies.Count == 0 || num == fReplyScoreBest)
		{
			fReplyScoreBest = num;
			dictBestReplies[interaction] = rt;
			if (stringBuilder.Length > 0)
			{
				if (dictBestReplies.Count == 0)
				{
					stringBuilder.Append("BEST 1ST ");
				}
				else
				{
					stringBuilder.Append("BEST ADD ");
				}
				stringBuilder.AppendLine(fReplyScoreBest.ToString());
			}
		}
		else if (num < fReplyScoreBest)
		{
			foreach (Interaction interaction2 in dictBestReplies.Keys)
			{
				if (DataHandler.dictSocialStats.ContainsKey(interaction2.strName))
				{
					DataHandler.dictSocialStats[interaction2.strName].nLowScored++;
				}
			}
			dictBestReplies.Clear();
			fReplyScoreBest = num;
			dictBestReplies[interaction] = rt;
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append("BEST REPLACE ");
				stringBuilder.AppendLine(fReplyScoreBest.ToString());
			}
		}
		else if (DataHandler.dictSocialStats.ContainsKey(interaction.strName))
		{
			DataHandler.dictSocialStats[interaction.strName].nLowScored++;
		}
		stringBuilder.AppendLine(string.Concat(new string[]
		{
			"***** ",
			base.Us.strName,
			" pledge checking replies to ",
			interaction.ToString(),
			": Done *****"
		}));
		if (stringBuilder.Length > 0 && flag)
		{
			Debug.Log(stringBuilder.ToString());
		}
	}
}
