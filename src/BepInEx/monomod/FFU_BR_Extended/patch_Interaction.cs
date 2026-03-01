using System;
using System.Collections.Generic;
using System.Linq;
using FFU_Beyond_Reach;
using MonoMod;
using UnityEngine;
// Extends live interaction execution for FFU_BR-added modifiers and diagnostics.
// README examples mention forced verbose logs, room lookup, and same-ship tests;
// this runtime patch is where those new fields start affecting behavior.
public class patch_Interaction : Interaction
{
	private extern void orig_SetData(JsonInteraction jsonIn, JsonInteractionSave jis = null);
	private void SetData(JsonInteraction jsonIn, JsonInteractionSave jis = null)
	{
		this.orig_SetData(jsonIn, jis);
		bool flag = jsonIn != null;
		if (flag)
		{
			this.bForceVerbose = (jsonIn as patch_JsonInteraction).bForceVerbose;
			this.bRoomLookup = (jsonIn as patch_JsonInteraction).bRoomLookup;
			this.bWriteToLog = (FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Interactions && this.bForceVerbose);
		}
	}
	private extern void orig_AddFailReason(string strKey, string strReason);
	private void AddFailReason(string strKey, string strReason)
	{
		bool flag = this.bWriteToLog && strReason != Interaction.STR_IA_FAIL_DEFAULT;
		if (flag)
		{
			string[] array = new string[12];
			array[0] = "#Interaction# ";
			array[1] = this.strName;
			array[2] = " (US: ";
			int num = 3;
			CondOwner objUs = base.objUs;
			array[num] = (((objUs != null) ? objUs.strName : null) ?? "N/A");
			array[4] = ", THEM: ";
			int num2 = 5;
			CondOwner objThem = base.objThem;
			array[num2] = (((objThem != null) ? objThem.strName : null) ?? "N/A");
			array[6] = ", 3RD: ";
			int num3 = 7;
			CondOwner obj3rd = base.obj3rd;
			array[num3] = (((obj3rd != null) ? obj3rd.strName : null) ?? "N/A");
			array[8] = ") => [Failed] ";
			array[9] = strKey;
			array[10] = ": ";
			array[11] = (string.IsNullOrEmpty(strReason) ? "N/A" : strReason);
			Debug.Log(string.Concat(array));
		}
		this.orig_AddFailReason(strKey, strReason);
	}
	private extern bool orig_TriggeredInternal(CondOwner objUs, CondOwner objThem, bool bStats = false, bool bIgnoreItems = false, bool bCheckPath = false, bool bFetchItems = true, List<string> aForbid3rds = null);
	private bool TriggeredInternal(CondOwner objUs, CondOwner objThem, bool bStats = false, bool bIgnoreItems = false, bool bCheckPath = false, bool bFetchItems = true, List<string> aForbid3rds = null)
	{
		bool flag = this.bForceVerbose;
		if (flag)
		{
			this.bVerboseTrigger = true;
		}
		return this.orig_TriggeredInternal(objUs, objThem, bStats, bIgnoreItems, bCheckPath, bFetchItems, aForbid3rds);
	}
	[MonoModReplace]
	public void ApplyLogging(string strOwner, bool bTraitSuffix)
	{
		string text = null;
		bool flag = this.bWriteToLog;
		if (flag)
		{
			text = GrammarUtils.GenerateDescription(this, true);
			string[] array = new string[10];
			array[0] = "#Interaction# ";
			array[1] = this.strName;
			array[2] = " (US: ";
			int num = 3;
			CondOwner objUs = base.objUs;
			array[num] = (((objUs != null) ? objUs.strName : null) ?? "N/A");
			array[4] = ", THEM: ";
			int num2 = 5;
			CondOwner objThem = base.objThem;
			array[num2] = (((objThem != null) ? objThem.strName : null) ?? "N/A");
			array[6] = ", 3RD: ";
			int num3 = 7;
			CondOwner obj3rd = base.obj3rd;
			array[num3] = (((obj3rd != null) ? obj3rd.strName : null) ?? "N/A");
			array[8] = ") => ";
			array[9] = text;
			Debug.Log(string.Concat(array));
		}
		bool flag2 = this.nLogging == null || this.bLogged;
		if (!flag2)
		{
			if (text == null)
			{
				text = GrammarUtils.GenerateDescription(this, true);
			}
			List<CondOwner> list = new List<CondOwner>();
			switch (this.nLogging)
			{
			case 1:
			{
				list.Add(base.objUs);
				bool flag3 = this.strThemType == Interaction.TARGET_OTHER;
				if (flag3)
				{
					list.Add(base.objThem);
				}
				break;
			}
			case 2:
			{
				bool flag4 = base.objUs.currentRoom != null;
				if (flag4)
				{
					list.AddRange(base.objUs.ship.GetPeopleInRoom(base.objUs.currentRoom, null));
				}
				else
				{
					bool flag5 = this.bRoomLookup;
					if (flag5)
					{
						List<CondOwner> list2 = new List<CondOwner>();
						CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsRoom");
						base.objUs.ship.GetCOsAtWorldCoords1(base.objUs.GetPos(null, false), condTrigger, false, false, list2);
						base.objUs.currentRoom = list2.First<CondOwner>().currentRoom;
						list.AddRange(base.objUs.ship.GetPeopleInRoom(base.objUs.currentRoom, null));
					}
				}
				bool flag6 = !list.Contains(base.objUs);
				if (flag6)
				{
					list.Add(base.objUs);
				}
				bool flag7 = !list.Contains(base.objThem);
				if (flag7)
				{
					list.Add(base.objThem);
				}
				break;
			}
			case 3:
			{
				bool flag8 = base.objUs.ship != null;
				if (flag8)
				{
					list.AddRange(base.objUs.ship.GetPeople(true));
				}
				break;
			}
			}
			foreach (CondOwner condOwner in list)
			{
				condOwner.LogMessage(text, this.strColor, strOwner);
			}
			this.bLogged = true;
		}
	}
	public bool bForceVerbose;
	public bool bRoomLookup;
	public bool bWriteToLog;
}
