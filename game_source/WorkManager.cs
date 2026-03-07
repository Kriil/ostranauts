using System;
using System.Collections.Generic;
using Ostranauts;
using UnityEngine;

// Global task board for crew work orders. This appears to track pending versus
// active Task2 entries, assign visual markers, and mirror the task list into
// the PDA/task UI.
public class WorkManager : MonoBehaviour
{
	// Unity setup: initializes the duty buckets, task pools, and construction
	// sign pool, then captures the initial idle-crew count.
	private void Awake()
	{
		this.dictTasks2 = new Dictionary<string, List<Task2>>();
		this.dictTasks2ByCOID = new Dictionary<string, List<Task2>>();
		foreach (string key in JsonCompanyRules.aDutiesNew)
		{
			this.dictTasks2[key] = new List<Task2>();
		}
		this.aTasksActive = new List<Task2>();
		this.constructionSigns = new List<GameObject>();
		this.aNonIdleCrewIDs = new List<CondOwner>();
		this.CountIdle();
	}

	// Updates the visual tint fade for all tracked tasks each frame.
	private void Update()
	{
		if (this.nTotalTasks < 1)
		{
			this.nTotalTasks = 1;
		}
		float num = (float)this.nTotalTasks;
		float num2 = 1f / num;
		float blendLoss = Time.deltaTime * num2;
		int num3 = 0;
		foreach (List<Task2> list in this.dictTasks2ByCOID.Values)
		{
			foreach (Task2 task in list)
			{
				task.UpdateTint(blendLoss);
				num3++;
			}
		}
		this.nTotalTasks = num3;
	}

	// Adds a task if the target exists and the per-duty duplicate cap is not
	// exceeded, then spawns its construction sign and PDA entry.
	public bool AddTask(Task2 task, int nMax = 1)
	{
		if (task == null || task.strTargetCOID == null || Array.IndexOf<string>(JsonCompanyRules.aDutiesNew, task.strDuty) < 0)
		{
			return false;
		}
		if (!DataHandler.mapCOs.ContainsKey(task.strTargetCOID) && !DataHandler.dictCOSaves.ContainsKey(task.strTargetCOID))
		{
			return false;
		}
		int num = 0;
		foreach (Task2 task2 in this.dictTasks2[task.strDuty])
		{
			if (task2.strTargetCOID == task.strTargetCOID && task2.strInteraction == task.strInteraction)
			{
				num++;
			}
			if (num >= nMax)
			{
				this.nSpawnedPriorities++;
				return false;
			}
		}
		foreach (Task2 task3 in this.aTasksActive)
		{
			if (task3.strTargetCOID == task.strTargetCOID && task3.strInteraction == task.strInteraction)
			{
				num++;
			}
			if (num >= nMax)
			{
				this.nSpawnedPriorities++;
				return false;
			}
		}
		this.dictTasks2[task.strDuty].Add(task);
		if (!this.dictTasks2ByCOID.ContainsKey(task.strTargetCOID))
		{
			this.dictTasks2ByCOID[task.strTargetCOID] = new List<Task2>();
		}
		this.dictTasks2ByCOID[task.strTargetCOID].Add(task);
		CondOwner condOwner = null;
		if (DataHandler.mapCOs.TryGetValue(task.strTargetCOID, out condOwner))
		{
			string iconName = task.GetIconName();
			task.SetConstructionSign(this.ActivateSignFromPool(condOwner.gameObject, iconName));
		}
		task.UpdateTint(1f);
		this.nSpawnedPriorities++;
		CrewSim.guiPDA.AddTask(task);
		return true;
	}

	// Removes a task from all tracking lists, hides its world sign, and removes
	// it from the PDA task list.
	public void RemoveTask(Task2 task)
	{
		if (task == null || Array.IndexOf<string>(JsonCompanyRules.aDutiesNew, task.strDuty) < 0)
		{
			return;
		}
		this.dictTasks2[task.strDuty].Remove(task);
		if (this.dictTasks2ByCOID.ContainsKey(task.strTargetCOID))
		{
			this.dictTasks2ByCOID[task.strTargetCOID].Remove(task);
			if (this.dictTasks2ByCOID[task.strTargetCOID].Count == 0)
			{
				this.dictTasks2ByCOID.Remove(task.strTargetCOID);
			}
		}
		this.aTasksActive.Remove(task);
		GameObject constructionSign = task.GetConstructionSign();
		if (constructionSign != null)
		{
			constructionSign.transform.SetParent(base.transform);
			constructionSign.GetComponent<MeshRenderer>().enabled = false;
			constructionSign.GetComponent<Collider>().enabled = false;
		}
		task.UpdateTint(1f);
		CrewSim.guiPDA.RemoveTask(task);
	}

	// Removes tasks by the duty/interaction/target key triple.
	public void RemoveTask(string strDuty, string strInteraction, string strCOID)
	{
		if (strDuty == null || strInteraction == null || strCOID == null)
		{
			return;
		}
		List<Task2> list = new List<Task2>();
		foreach (Task2 task in this.dictTasks2[strDuty])
		{
			if (task.strInteraction == strInteraction && task.strTargetCOID == strCOID)
			{
				list.Add(task);
			}
		}
		foreach (Task2 task2 in list)
		{
			this.RemoveTask(task2);
		}
	}

	// Removes every task currently targeting the given CondOwner id.
	public void RemoveTask(string strCOID)
	{
		if (strCOID == null)
		{
			return;
		}
		List<Task2> list = new List<Task2>();
		if (this.dictTasks2ByCOID.ContainsKey(strCOID))
		{
			foreach (Task2 task in this.dictTasks2ByCOID[strCOID])
			{
				if (task.strTargetCOID == strCOID)
				{
					list.Add(task);
				}
			}
		}
		foreach (Task2 task2 in list)
		{
			this.RemoveTask(task2);
		}
	}

	// Marks the matching task complete, applies any task-complete condtrig loot
	// to the worker, and removes the task.
	public void CompleteTask(string strIA, string strUs, string strTarget)
	{
		if (strIA == null || strTarget == null || strUs == null)
		{
			return;
		}
		if (!this.dictTasks2ByCOID.ContainsKey(strTarget))
		{
			return;
		}
		List<CondTrigger> list = null;
		foreach (Task2 task in this.dictTasks2ByCOID[strTarget])
		{
			if (task.Matches(strIA, strTarget))
			{
				if (list == null)
				{
					Loot loot = DataHandler.GetLoot("CTACTTaskComplete");
					list = loot.GetCTLoot(null, null);
				}
				CondOwner objOwner = null;
				if (DataHandler.mapCOs.TryGetValue(strUs, out objOwner))
				{
					foreach (CondTrigger condTrigger in list)
					{
						if (condTrigger != null && condTrigger.Triggered(objOwner, null, true))
						{
							condTrigger.ApplyChanceID(true, objOwner, 1f, 0f);
						}
					}
				}
				this.RemoveTask(task);
				break;
			}
		}
	}

	// Moves an active claimed task back into the unclaimed queue.
	public void UnclaimTask(Task2 task)
	{
		if (task == null || Array.IndexOf<string>(JsonCompanyRules.aDutiesNew, task.strDuty) < 0 || this.aTasksActive.IndexOf(task) < 0)
		{
			return;
		}
		if (this.dictTasks2[task.strDuty].IndexOf(task) < 0)
		{
			this.dictTasks2[task.strDuty].Add(task);
		}
		if (this.aTasksActive.IndexOf(task) >= 0)
		{
			this.aTasksActive.Remove(task);
		}
		task.SetIA(null);
		CrewSim.guiPDA.AddTask(task);
	}

	public void UnclaimTask(Interaction ia)
	{
		if (ia == null)
		{
			return;
		}
		foreach (Task2 task in this.aTasksActive)
		{
			if (task.GetIA() == ia)
			{
				this.UnclaimTask(task);
				break;
			}
		}
	}

	public Task2 ClaimNextTask(CondOwner co)
	{
		int num = 0;
		this.nSpawnedPriorities = 0;
		if (co == null || co.Company == null || co.Company != CrewSim.coPlayer.Company)
		{
			return null;
		}
		JsonCompanyRules jsonCompanyRules = null;
		if (!co.Company.mapRoster.TryGetValue(co.strID, out jsonCompanyRules))
		{
			return null;
		}
		bool bAIManual = co.HasCond("IsAIManual");
		bool flag = co.HasCond("IsAirtight");
		List<string> list = null;
		bool flag2 = false;
		int num2 = 0;
		do
		{
			num2++;
			flag2 = false;
			num = 0;
			int num3 = JsonCompanyRules.nPriorityMin;
			while (num3 <= JsonCompanyRules.nPriorityMax && !flag2)
			{
				for (int i = 0; i < jsonCompanyRules.aDutyLvls.Length; i++)
				{
					if (jsonCompanyRules.aDutyLvls[i] == num3)
					{
						string text = JsonCompanyRules.aDutiesNew[i];
						if (this.dictTasks2.ContainsKey(text))
						{
							List<Task2> list2 = this.CollectTasks(text, co, bAIManual);
							if (list2.Count > 0 && list == null)
							{
								list = this.GetDockedships(co);
							}
							foreach (Task2 task in list2)
							{
								if (num > 5)
								{
									return null;
								}
								this.dictTasks2[text].Remove(task);
								this.dictTasks2[text].Add(task);
								CondOwner condOwner = null;
								DataHandler.mapCOs.TryGetValue(task.strTargetCOID, out condOwner);
								if (condOwner != null)
								{
									if (condOwner.ship == null)
									{
										continue;
									}
									if (list.IndexOf(condOwner.ship.strRegID) < 0)
									{
										continue;
									}
								}
								if (task.strTileShip == null || list.IndexOf(task.strTileShip) >= 0)
								{
									Interaction interaction = (!(task.strInteraction == "ACTHaulItem")) ? DataHandler.GetInteraction(task.strInteraction, null, false) : this.HaulZone(co, task, condOwner);
									if (interaction != null)
									{
										interaction.bVerboseTrigger = true;
										interaction.bHumanOnly = false;
										interaction.bManual = task.bManual;
										num++;
										this.taskUpstream = task;
										bool flag3 = condOwner != null && condOwner.GetComponent<Placeholder>() != null;
										if (flag3)
										{
											interaction.CTTestThem = null;
										}
										co.AddCondAmount("IsAirtightFake", 1.0, 0.0, 0f);
										if (co.Pathfinder != null)
										{
											co.Pathfinder.ResetMemory();
										}
										bool flag4 = interaction.Triggered(co, condOwner, false, false, true, false, null);
										co.ZeroCondAmount("IsAirtightFake");
										if (co.Pathfinder != null)
										{
											co.Pathfinder.ResetMemory();
										}
										if (!flag4)
										{
											this.FailTask(task, interaction, co, condOwner);
										}
										else
										{
											if (interaction.Triggered(co, condOwner, false, false, true, true, null))
											{
												if (flag3)
												{
													this.taskUpstream = null;
												}
												return this.FinalizeTask(co, task, interaction, condOwner);
											}
											num++;
											if (this.nSpawnedPriorities > 0)
											{
												this.FailTask(task, interaction, co, condOwner);
												flag2 = true;
												break;
											}
											if (flag)
											{
												this.FailTask(task, interaction, co, condOwner);
											}
											else
											{
												Task2 task2 = this.HandleSuitRequirement(co, task, interaction, condOwner);
												if (task2 != null)
												{
													return task2;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				num3++;
			}
		}
		while (flag2 && num2 <= 5);
		return null;
	}

	private Task2 FinalizeTask(CondOwner co, Task2 task, Interaction iact, CondOwner coThem)
	{
		iact.objUs = co;
		iact.objThem = coThem;
		task.SetIA(iact);
		this.TintTask(task, Color.yellow);
		if (this.dictTasks2[task.strDuty].IndexOf(task) >= 0)
		{
			this.dictTasks2[task.strDuty].Remove(task);
		}
		if (this.aTasksActive.IndexOf(task) < 0)
		{
			this.aTasksActive.Add(task);
		}
		task.strStatus = "Claimed by " + co.strNameFriendly;
		CrewSim.guiPDA.AddTask(task);
		return task;
	}

	private Task2 HandleSuitRequirement(CondOwner co, Task2 task, Interaction iact, CondOwner coThem)
	{
		if (PledgeWearSuit.FindItem(co, PledgeWearSuit.ctHelmet) == null)
		{
			string failureReason = co.strName + DataHandler.GetString("AI_PATHFIND_NO_HELMET", false);
			this.FailTask(task, co, coThem, failureReason, iact.strTitle);
			return null;
		}
		if (!PledgeWearSuit.ctWearingSuit.Triggered(co, null, true) && PledgeWearSuit.FindItem(co, PledgeWearSuit.ctSuit) == null)
		{
			string failureReason2 = co.strName + DataHandler.GetString("AI_PATHFIND_NO_SUIT", false);
			this.FailTask(task, co, coThem, failureReason2, iact.strTitle);
			return null;
		}
		JsonPledge pledge = DataHandler.GetPledge("AIEquipSpaceSuitAndHelmet");
		Pledge2 pledge2 = PledgeFactory.Factory(co, pledge, null);
		iact.objUs = co;
		iact.objThem = coThem;
		task.SetIA(iact);
		((PledgeWearSuit)pledge2).SetQueueTask(task);
		co.AddPledge(pledge2);
		co.LogMessage(co.strName + DataHandler.GetString("AI_PATHFIND_GET_SUIT", false), "Neutral", co.strName);
		return new Task2
		{
			strName = "QuickWaitForHelmet",
			strInteraction = "QuickWait"
		};
	}

	private List<string> GetDockedships(CondOwner co)
	{
		List<string> list = new List<string>
		{
			co.ship.strRegID
		};
		foreach (Ship ship in co.ship.GetAllDockedShips())
		{
			list.Add(ship.strRegID);
		}
		return list;
	}

	private List<Task2> CollectTasks(string strDuty, CondOwner co, bool bAIManual)
	{
		List<Task2> list = new List<Task2>();
		foreach (Task2 task in this.dictTasks2[strDuty])
		{
			if (StarSystem.fEpoch - task.fLastCheck > 5.0)
			{
				Task2.Allowed ownership = task.GetOwnership(co.strID);
				if (ownership != Task2.Allowed.Owned)
				{
					if (ownership != Task2.Allowed.Allowed)
					{
						if (ownership != Task2.Allowed.Forbidden)
						{
						}
					}
					else if (!bAIManual)
					{
						list.Add(task);
					}
				}
				else
				{
					list.Insert(0, task);
				}
			}
		}
		return list;
	}

	private void FailTask(Task2 task, Interaction iact, CondOwner co, CondOwner coThem)
	{
		this.TintTask(task, Color.red);
		this.taskUpstream = null;
		task.strStatus = "Last attempt by " + co.strNameFriendly;
		string text = iact.FailReasons(true, true, false);
		task.strStatus = task.strStatus + "\n" + text;
		task.fLastCheck = StarSystem.fEpoch;
		this.RecordFailReason(co, coThem, iact.strTitle, text);
		CrewSim.guiPDA.AddTask(task);
	}

	private void FailTask(Task2 task, CondOwner co, CondOwner coThem, string failureReason, string strTitle)
	{
		this.TintTask(task, Color.red);
		this.taskUpstream = null;
		task.strStatus = "Last attempt by " + co.strNameFriendly;
		task.strStatus = task.strStatus + "\n" + failureReason;
		task.fLastCheck = StarSystem.fEpoch;
		this.RecordFailReason(co, coThem, strTitle, failureReason);
		CrewSim.guiPDA.AddTask(task);
	}

	public void ResumeTask(CondOwner co, string strCOID)
	{
		if (string.IsNullOrEmpty(strCOID) || co == null)
		{
			return;
		}
		bool flag = co.HasCond("IsAirtight");
		CondOwner condOwner = null;
		DataHandler.mapCOs.TryGetValue(strCOID, out condOwner);
		List<Task2> allTasksForCOID = CrewSim.objInstance.workManager.GetAllTasksForCOID(strCOID);
		List<string> list = null;
		if (allTasksForCOID.Count > 0 && list == null)
		{
			list = this.GetDockedships(co);
		}
		foreach (Task2 task in allTasksForCOID)
		{
			if (condOwner != null)
			{
				if (condOwner.ship == null)
				{
					continue;
				}
				if (list.IndexOf(condOwner.ship.strRegID) < 0)
				{
					continue;
				}
			}
			if (task.strTileShip == null || list.IndexOf(task.strTileShip) >= 0)
			{
				Interaction interaction = (!(task.strInteraction == "ACTHaulItem")) ? DataHandler.GetInteraction(task.strInteraction, null, false) : this.HaulZone(co, task, condOwner);
				if (interaction != null)
				{
					interaction.bVerboseTrigger = true;
					interaction.bHumanOnly = false;
					interaction.bManual = task.bManual;
					this.taskUpstream = task;
					bool flag2 = condOwner != null && condOwner.GetComponent<Placeholder>() != null;
					if (flag2)
					{
						interaction.CTTestThem = null;
					}
					co.AddCondAmount("IsAirtightFake", 1.0, 0.0, 0f);
					if (co.Pathfinder != null)
					{
						co.Pathfinder.ResetMemory();
					}
					bool flag3 = interaction.Triggered(co, condOwner, false, false, true, false, null);
					co.ZeroCondAmount("IsAirtightFake");
					if (co.Pathfinder != null)
					{
						co.Pathfinder.ResetMemory();
					}
					if (!flag3)
					{
						this.FailTask(task, interaction, co, condOwner);
					}
					else
					{
						if (interaction.Triggered(co, condOwner, false, false, true, true, null))
						{
							if (flag2)
							{
								this.taskUpstream = null;
							}
							this.FinalizeTask(co, task, interaction, condOwner);
							interaction.objUs.AIIssueOrder(interaction.objThem, interaction, true, null, 0f, 0f);
							this.IdleRemove(interaction.objUs);
							break;
						}
						if (this.nSpawnedPriorities > 0)
						{
							this.FailTask(task, interaction, co, condOwner);
							break;
						}
						if (flag)
						{
							this.FailTask(task, interaction, co, condOwner);
						}
						else
						{
							Task2 task2 = this.HandleSuitRequirement(co, task, interaction, condOwner);
							if (task2 != null)
							{
								break;
							}
						}
					}
				}
			}
		}
	}

	public void ClaimTaskDirect(Interaction iact)
	{
		if (iact == null || iact.objUs == null || iact.objThem == null)
		{
			return;
		}
		Task2 task = new Task2();
		task.strDuty = iact.strDuty;
		task.strInteraction = iact.strName;
		task.strName = iact.strTitle;
		task.strTargetCOID = iact.objThem.strID;
		task.bManual = true;
		task.SetIA(iact);
		task.AddOwner(iact.objUs.strID);
		if (iact.objThem != null && iact.objThem.GetComponent<Placeholder>() != null)
		{
			iact.CTTestThem = null;
		}
		this.taskUpstream = task;
		if (this.AddTask(task, 1) && iact.Triggered(iact.objUs, iact.objThem, false, false, false, true, null))
		{
			this.taskUpstream = null;
			if (this.dictTasks2[task.strDuty].IndexOf(task) >= 0)
			{
				this.dictTasks2[task.strDuty].Remove(task);
			}
			if (this.aTasksActive.IndexOf(task) < 0)
			{
				this.aTasksActive.Add(task);
			}
			iact.objUs.AIIssueOrder(iact.objThem, iact, true, null, 0f, 0f);
			task.strStatus = "Claimed by " + iact.objUs.strNameFriendly;
			CrewSim.guiPDA.AddTask(task);
			this.IdleRemove(iact.objUs);
			return;
		}
		this.taskUpstream = null;
		string text = iact.FailReasons(true, true, false);
		iact.objUs.LogMessage(text, "Bad", iact.objUs.strName);
		task.strStatus = "Last attempt by " + iact.objUs.strNameFriendly + "\n";
		Task2 task2 = task;
		task2.strStatus += text;
		this.RecordFailReason(iact.objUs, iact.objThem, iact.strTitle, text);
		CrewSim.guiPDA.AddTask(task);
		JsonTicker jsonTicker = new JsonTicker();
		jsonTicker.strName = "AINudge";
		jsonTicker.bQueue = true;
		jsonTicker.fPeriod = 0.0;
		jsonTicker.SetTimeLeft(jsonTicker.fPeriod);
		iact.objUs.AddTicker(jsonTicker);
	}

	private void RecordFailReason(CondOwner coUs, CondOwner coThem, string iaTitle, string failReason)
	{
		if (coUs == null)
		{
			return;
		}
		string text = (!(coThem != null)) ? string.Empty : coThem.strNameFriendly;
		if (coUs.RecentWorkHistory == null)
		{
			coUs.RecentWorkHistory = new COWorkHistoryDTO();
		}
		coUs.RecentWorkHistory.RecordFailedWorkAttempt(string.Concat(new string[]
		{
			iaTitle,
			" ",
			text,
			": ",
			failReason
		}));
	}

	public void IdleAdd(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		bool flag = false;
		if (co.Company == null || co.Company != CrewSim.coPlayer.Company)
		{
			flag = this.aNonIdleCrewIDs.Remove(co);
		}
		else
		{
			JsonShift shift = co.Company.GetShift(StarSystem.nUTCHour, co);
			if (shift != null && shift.nID == 2)
			{
				flag = this.aNonIdleCrewIDs.Remove(co);
			}
		}
		if (flag)
		{
			this.CountIdle();
		}
	}

	public void IdleRemove(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		bool flag = false;
		if (co.Company == null || co.Company != CrewSim.coPlayer.Company)
		{
			flag = this.aNonIdleCrewIDs.Remove(co);
		}
		else
		{
			JsonShift shift = co.Company.GetShift(StarSystem.nUTCHour, co);
			if (shift != null && shift.nID == 2 && this.aNonIdleCrewIDs.IndexOf(co) < 0)
			{
				this.aNonIdleCrewIDs.Add(co);
				flag = true;
			}
		}
		if (flag)
		{
			this.CountIdle();
		}
	}

	public void CountIdle()
	{
		int num = 0;
		if (CrewSim.coPlayer != null && CrewSim.coPlayer.Company != null)
		{
			foreach (string text in CrewSim.coPlayer.Company.mapRoster.Keys)
			{
				if (text != null)
				{
					CondOwner condOwner = null;
					if (DataHandler.mapCOs.TryGetValue(text, out condOwner) && !(condOwner == null))
					{
						JsonShift shift = CrewSim.coPlayer.Company.GetShift(StarSystem.nUTCHour, condOwner);
						if (shift != null && shift.nID == 2)
						{
							num++;
						}
					}
				}
			}
			num -= this.aNonIdleCrewIDs.Count;
		}
		CrewSim.guiPDA.IdleCrew = num;
	}

	public void AddClaimedTask(Interaction iact)
	{
		if (iact == null || iact.objUs == null || iact.objThem == null)
		{
			return;
		}
		Task2 task = new Task2();
		task.strDuty = iact.strDuty;
		task.strInteraction = iact.strName;
		task.strName = iact.strTitle;
		task.strTargetCOID = iact.objThem.strID;
		task.SetIA(iact);
		this.AddTask(task, 1);
		if (this.dictTasks2[task.strDuty].IndexOf(task) >= 0)
		{
			this.dictTasks2[task.strDuty].Remove(task);
		}
		if (this.aTasksActive.IndexOf(task) < 0)
		{
			this.aTasksActive.Add(task);
		}
		CrewSim.guiPDA.AddTask(task);
	}

	public List<Task2> GetAllTasks()
	{
		List<Task2> list = new List<Task2>();
		foreach (List<Task2> list2 in this.dictTasks2ByCOID.Values)
		{
			foreach (Task2 item in list2)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public bool COIDHasTasks(string strCOID)
	{
		return this.dictTasks2ByCOID != null && strCOID != null && this.dictTasks2ByCOID.ContainsKey(strCOID) && this.dictTasks2ByCOID[strCOID].Count > 0;
	}

	public List<Task2> GetAllTasksForCOID(string strCOID)
	{
		List<Task2> list = new List<Task2>();
		if (this.dictTasks2ByCOID == null || strCOID == null || !this.dictTasks2ByCOID.ContainsKey(strCOID))
		{
			return list;
		}
		foreach (Task2 item in this.dictTasks2ByCOID[strCOID])
		{
			list.Add(item);
		}
		return list;
	}

	public Task2 GetTask(string strCOID, string strInteraction)
	{
		if (this.dictTasks2ByCOID == null || strCOID == null || !this.dictTasks2ByCOID.ContainsKey(strCOID))
		{
			return null;
		}
		foreach (Task2 task in this.dictTasks2ByCOID[strCOID])
		{
			if (task.Matches(strInteraction, strCOID))
			{
				return task;
			}
		}
		return null;
	}

	public Interaction HaulZone(CondOwner coHauler, Task2 task, CondOwner coTarget)
	{
		if (coHauler == null || task == null || coTarget == null)
		{
			return null;
		}
		if (!WorkManager.CTHaul.Triggered(coTarget, null, true))
		{
			return null;
		}
		List<JsonZone> zones = coHauler.ship.GetZones("IsZoneStockpile", coHauler, true, false);
		if (zones == null || zones.Count == 0)
		{
			return null;
		}
		zones.Sort();
		foreach (JsonZone jsonZone in zones)
		{
			Ship ship = null;
			if (jsonZone.strRegID != null && CrewSim.system.dictShips.TryGetValue(jsonZone.strRegID, out ship))
			{
				if (jsonZone.categoryConds != null && jsonZone.categoryConds.Length > 0)
				{
					bool flag = false;
					int num = 0;
					while (num < jsonZone.categoryConds.Length && !flag)
					{
						flag = coTarget.HasCond(jsonZone.categoryConds[num]);
						num++;
					}
					if (!flag)
					{
						continue;
					}
				}
				List<CondOwner> cosInZone = ship.GetCOsInZone(jsonZone, DataHandler.GetCondTrigger("TIsValidHaulDest"), false, true);
				foreach (CondOwner condOwner in cosInZone)
				{
					if (condOwner.CanStackOnItem(coTarget) != 0)
					{
						task.nTile = -1;
						Tile tileAtWorldCoords = ship.GetTileAtWorldCoords1(condOwner.tf.position.x, condOwner.tf.position.y, true, true);
						task.nTile = tileAtWorldCoords.Index;
						task.strTileShip = tileAtWorldCoords.coProps.ship.strRegID;
						if (task.nTile >= 0)
						{
							return DataHandler.GetInteraction(task.strInteraction, null, false);
						}
					}
				}
				Vector3 vector = default(Vector3);
				if (TileUtils.TryFitItem(coTarget.GetComponent<Item>(), ship, jsonZone, out vector))
				{
					task.nTile = -1;
					Tile tileAtWorldCoords2 = ship.GetTileAtWorldCoords1(vector.x, vector.y, true, true);
					task.nTile = tileAtWorldCoords2.Index;
					task.strTileShip = tileAtWorldCoords2.coProps.ship.strRegID;
					if (task.nTile >= 0)
					{
						return DataHandler.GetInteraction(task.strInteraction, null, false);
					}
				}
			}
		}
		coHauler.LogMessage("Could not find a zone with matching category for " + coTarget.strNameFriendly, "Bad", coHauler.strName);
		return null;
	}

	public void ChangeCOID(string strIDOld, string strIDNew)
	{
		if (strIDOld == null || strIDNew == null || strIDNew == strIDOld)
		{
			return;
		}
		if (this.dictTasks2ByCOID.ContainsKey(strIDOld))
		{
			foreach (Task2 task in this.dictTasks2ByCOID[strIDOld])
			{
				task.strTargetCOID = strIDNew;
				task.GetConstructionSign().transform.SetParent(DataHandler.mapCOs[strIDNew].tf);
			}
			this.dictTasks2ByCOID[strIDNew] = this.dictTasks2ByCOID[strIDOld];
			this.dictTasks2ByCOID.Remove(strIDOld);
		}
		foreach (List<Task2> list in this.dictTasks2ByCOID.Values)
		{
			foreach (Task2 task2 in list)
			{
				if (task2.aOwnerIDs != null && task2.aOwnerIDs.Length != 0)
				{
					for (int i = 0; i < task2.aOwnerIDs.Length; i++)
					{
						if (task2.aOwnerIDs[i] == strIDOld)
						{
							task2.aOwnerIDs[i] = strIDNew;
						}
					}
				}
			}
		}
	}

	public GameObject ActivateSignFromPool(GameObject ConstructionSignHolder, string strMapIcon)
	{
		GameObject gameObject = null;
		List<int> list = new List<int>();
		for (int i = 0; i < this.constructionSigns.Count; i++)
		{
			if (this.constructionSigns[i] == null)
			{
				list.Add(i);
			}
			else if (!this.constructionSigns[i].GetComponent<MeshRenderer>().enabled)
			{
				gameObject = this.constructionSigns[i];
			}
		}
		list.Sort();
		for (int j = list.Count - 1; j >= 0; j--)
		{
			this.constructionSigns.RemoveAt(j);
		}
		if (gameObject == null)
		{
			gameObject = DataHandler.GetMesh("prefabQuadGUI", null);
			gameObject.name = "Debug Construction Sign";
			this.constructionSigns.Add(gameObject);
		}
		return this.AttachSignToObject(gameObject, ConstructionSignHolder, strMapIcon);
	}

	public GameObject AttachSignToObject(GameObject goSign, GameObject goHolder, string strMapIcon)
	{
		Transform transform = goSign.transform;
		transform.SetParent(goHolder.transform, false);
		transform.localPosition = default(Vector3);
		Transform parent = transform.parent;
		if (strMapIcon == null)
		{
			strMapIcon = "IcoConstructionSign";
		}
		goSign.GetComponent<MeshRenderer>().sharedMaterial = DataHandler.GetMaterial(goSign.GetComponent<MeshRenderer>(), strMapIcon, "blank", "blank", "blank");
		float x = 1f / parent.localScale.x;
		float y = 1f / parent.localScale.y;
		if (MathUtils.IsRotationVertical(parent.rotation.eulerAngles.z))
		{
			MathUtils.Swap(ref x, ref y);
		}
		Vector3 localScale = new Vector3(x, y, 1f / parent.localScale.z);
		transform.localScale = localScale;
		transform.rotation = Quaternion.identity;
		bool enabled = true;
		if (goHolder.GetComponent<MeshRenderer>() != null)
		{
			enabled = goHolder.GetComponent<MeshRenderer>().enabled;
		}
		goSign.GetComponent<MeshRenderer>().enabled = enabled;
		goSign.layer = LayerMask.NameToLayer("Task");
		return goSign;
	}

	public void RefreshTileIDs(string strRegID, List<Tile> aTilesOld)
	{
		if (aTilesOld == null)
		{
			return;
		}
		foreach (string key in this.dictTasks2.Keys)
		{
			foreach (Task2 task in this.dictTasks2[key])
			{
				if (!(task.strTileShip != strRegID))
				{
					if (task.nTile >= 0 && aTilesOld.Count > task.nTile)
					{
						task.nTile = aTilesOld[task.nTile].Index;
					}
					else
					{
						task.nTile = -1;
						task.strTileShip = null;
					}
				}
			}
		}
	}

	public void RefreshTileIDsEVA(string strRegID, List<Tile> aTiles, int nOldCols, int nNewCols)
	{
		if (nOldCols == 0)
		{
			nOldCols = 1;
		}
		foreach (string key in this.dictTasks2.Keys)
		{
			foreach (Task2 task in this.dictTasks2[key])
			{
				if (!(task.strTileShip != strRegID))
				{
					if (task.nTile >= 0 && aTiles.Count > task.nTile)
					{
						int num = task.nTile;
						int num2 = num / nOldCols + 1;
						int num3 = num % nOldCols + 1;
						num = nNewCols * num2 + num3;
						task.nTile = num;
					}
					else
					{
						task.nTile = -1;
						task.strTileShip = null;
					}
				}
			}
		}
	}

	public Task2[] GetUnclaimedTasksSaveData()
	{
		List<Task2> list = new List<Task2>();
		foreach (List<Task2> list2 in this.dictTasks2.Values)
		{
			foreach (Task2 item in list2)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public void LoadTasksFromSave(JsonGameSave jgs)
	{
		if (jgs == null)
		{
			return;
		}
		if (jgs.aTasksUnclaimed != null)
		{
			foreach (Task2 task in jgs.aTasksUnclaimed)
			{
				if (task != null && DataHandler.GetInteraction(task.strInteraction, null, false) != null)
				{
					this.AddTask(task, 1);
				}
			}
		}
	}

	public void ShowShipTasks(string strRegID)
	{
		if (strRegID == null)
		{
			return;
		}
		List<Task2> list = new List<Task2>();
		foreach (List<Task2> list2 in this.dictTasks2.Values)
		{
			foreach (Task2 task in list2)
			{
				if (task.strTargetCOID == null)
				{
					list.Add(task);
				}
				else
				{
					GameObject gameObject = task.GetConstructionSign();
					CondOwner condOwner = null;
					if (DataHandler.mapCOs.TryGetValue(task.strTargetCOID, out condOwner) && condOwner != null && DataHandler.GetInteraction(task.strInteraction, null, false) != null)
					{
						string iconName = task.GetIconName();
						if (gameObject == null)
						{
							gameObject = this.ActivateSignFromPool(condOwner.gameObject, iconName);
						}
						task.SetConstructionSign(gameObject);
						this.AttachSignToObject(gameObject, condOwner.gameObject, iconName);
						task.UpdateTint(1f);
					}
					else
					{
						list.Add(task);
					}
				}
			}
		}
		foreach (Task2 task2 in list)
		{
			this.RemoveTask(task2);
		}
	}

	public void TintTask(Task2 task, string strColor)
	{
		Color color = Color.white;
		if (strColor != "none" || strColor != null)
		{
			color = DataHandler.GetColor(strColor);
		}
		this.TintTask(task, color);
	}

	public void TintTask(Task2 task, Color color)
	{
		task.clrTint = color;
		task.fTintBlend = 1f;
		GameObject constructionSign = task.GetConstructionSign();
		if (constructionSign == null)
		{
			return;
		}
		MeshRenderer component = constructionSign.GetComponent<MeshRenderer>();
		if (component == null)
		{
			return;
		}
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		component.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor("_Color", color);
		component.SetPropertyBlock(materialPropertyBlock);
	}

	public static CondTrigger CTHaul
	{
		get
		{
			if (WorkManager._ctHaul == null)
			{
				WorkManager._ctHaul = DataHandler.GetCondTrigger("TIsValidHaulSource");
			}
			return WorkManager._ctHaul;
		}
	}

	private Dictionary<string, List<Task2>> dictTasks2;

	private Dictionary<string, List<Task2>> dictTasks2ByCOID;

	private List<Task2> aTasksActive;

	public List<GameObject> constructionSigns;

	public List<CondOwner> aNonIdleCrewIDs;

	public Task2 taskUpstream;

	private int nSpawnedPriorities;

	public const double fCheckPeriod = 5.0;

	private const int nMaxPerCheck = 5;

	public int nTotalTasks;

	private static CondTrigger _ctHaul;
}
