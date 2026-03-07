using System;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.Events.DTOs;
using Ostranauts.UI.MegaToolTip.DataModules.SubElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class StatusModule : ModuleBase
	{
		private new void Awake()
		{
			base.Awake();
			Wound.OnWoundUpdated.AddListener(new UnityAction<PaperdollWoundDTO>(this.OnWoundUpdated));
			if (StatusModule._ctWoundMap == null)
			{
				StatusModule._ctWoundMap = new List<CondTrigger>
				{
					DataHandler.GetCondTrigger("TIsBleeding"),
					DataHandler.GetCondTrigger("TIsFracturedBone")
				};
			}
		}

		private void OnDestroy()
		{
			Wound.OnWoundUpdated.RemoveListener(new UnityAction<PaperdollWoundDTO>(this.OnWoundUpdated));
		}

		private void OnWoundUpdated(PaperdollWoundDTO pdWoundDto)
		{
			if (pdWoundDto == null || pdWoundDto.CoDoll != this._coDisplay)
			{
				return;
			}
			UniqueList<string> uniqueList;
			if (!this._dictWoundConds.TryGetValue(pdWoundDto.CoSlot.strName, out uniqueList) || uniqueList == null)
			{
				uniqueList = new UniqueList<string>();
				this._dictWoundConds.Add(pdWoundDto.CoSlot.strName, uniqueList);
			}
			foreach (CondTrigger condTrigger in StatusModule._ctWoundMap)
			{
				if (condTrigger.Triggered(pdWoundDto.CoSlot, null, true))
				{
					uniqueList.Add(condTrigger.strCondName);
				}
				else
				{
					uniqueList.Remove(condTrigger.strCondName);
				}
			}
		}

		public override void SetData(CondOwner co)
		{
			if (co == null)
			{
				return;
			}
			this._coDisplay = co;
			if (StatusModule.aDCList == null)
			{
				Loot loot = DataHandler.GetLoot("CONDSocialGUIFilterDCsMTT");
				StatusModule.aDCList = loot.GetLootNames(null, false, null);
			}
			List<Wound> allWounds = co.GetAllWounds();
			if (allWounds != null)
			{
				foreach (Wound wound in allWounds)
				{
					this.OnWoundUpdated(new PaperdollWoundDTO
					{
						CoDoll = co,
						CoSlot = wound.coUs
					});
				}
			}
			this.CreateStatusElements(co, this.GetConditions(co));
		}

		private List<string> GetConditions(CondOwner co)
		{
			Relationship relationship = null;
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			if (co != CrewSim.GetSelectedCrew())
			{
				this._coThem = CrewSim.GetSelectedCrew();
			}
			if (this._coThem != co && this._coThem != null && this._coThem.socUs != null)
			{
				Relationship relationship2 = this._coThem.socUs.GetRelationship(co.strName) ?? this._coThem.socUs.AddStranger(co.pspec);
				relationship = relationship2;
			}
			if (co == CrewSim.coPlayer)
			{
				foreach (Condition condition in co.mapConds.Values)
				{
					if (condition.nDisplaySelf == 2 && list.IndexOf(condition.strName) < 0)
					{
						list.Add(condition.strName);
					}
					else if (condition.nDisplaySelf == 1 && list2.IndexOf(condition.strName) < 0)
					{
						list3.Add(condition.strName);
					}
				}
			}
			else
			{
				List<string> list4 = new List<string>();
				if (relationship != null)
				{
					list4 = relationship.aReveals;
				}
				foreach (Condition condition2 in co.mapConds.Values)
				{
					if (condition2.nDisplayOther == 2 && list.IndexOf(condition2.strName) < 0)
					{
						list.Add(condition2.strName);
					}
					else if (condition2.nDisplayOther == 1 && list2.IndexOf(condition2.strName) < 0)
					{
						list2.Add(condition2.strName);
						if (list4.IndexOf(condition2.strName) < 0)
						{
							if (!GUIStatus.StatusIsOld(co.mapConds[condition2.strName]))
							{
								list4.Add(condition2.strName);
							}
							else
							{
								list2.Remove(condition2.strName);
								list3.Add(condition2.strName);
							}
						}
					}
				}
				List<string> list5 = new List<string>();
				foreach (string text in list4)
				{
					if (!co.mapConds.ContainsKey(text))
					{
						list5.Add(text);
					}
				}
				foreach (string item in list5)
				{
					list4.Remove(item);
				}
			}
			List<string> list6 = new List<string>();
			List<string> list7 = new List<string>();
			List<string> list8 = new List<string>();
			List<string> list9 = new List<string>();
			List<string> list10 = new List<string>();
			List<string> aDiscard = new List<string>();
			foreach (string strCondName in list)
			{
				GUISocialCombat2.MMTCategory(strCondName, list7, list8, list9, list10, aDiscard, null);
			}
			foreach (string strCondName2 in list2)
			{
				GUISocialCombat2.MMTCategory(strCondName2, list7, list8, list9, list10, aDiscard, null);
			}
			foreach (string strCondName3 in list3)
			{
				GUISocialCombat2.MMTCategory(strCondName3, list7, list8, list9, list10, aDiscard, "???");
			}
			list6.AddRange(this.CollapseDuplicates(list7, "???"));
			if (list6.Count > 0)
			{
				list6.Add("_NewLine");
			}
			list6.AddRange(this.CollapseDuplicates(list8, "???"));
			if (list6.Count > 0)
			{
				list6.Add("_NewLine");
			}
			list6.AddRange(this.CollapseDuplicates(list9, "???"));
			if (list6.Count > 0)
			{
				list6.Add("_NewLine");
			}
			list6.AddRange(this.CollapseDuplicates(list10, "???"));
			this.AddWoundConditions(list6);
			return list6;
		}

		private List<string> CollapseDuplicates(List<string> aNames, string strMatch)
		{
			if (aNames == null)
			{
				return new List<string>();
			}
			if (aNames.Count == 0 || string.IsNullOrEmpty(strMatch))
			{
				return aNames;
			}
			int num = 0;
			int num2 = -1;
			for (int i = 0; i < aNames.Count; i++)
			{
				if (!(aNames[i] != strMatch))
				{
					num++;
					if (num2 < 0)
					{
						num2 = i;
					}
					else
					{
						aNames.RemoveAt(i);
						i--;
					}
				}
			}
			if (num2 >= 0 && num > 1)
			{
				aNames[num2] = num + "x " + strMatch;
			}
			return aNames;
		}

		private void AddWoundConditions(List<string> aConditions)
		{
			foreach (UniqueList<string> uniqueList in this._dictWoundConds.Values)
			{
				if (uniqueList != null)
				{
					foreach (string item in uniqueList)
					{
						if (!aConditions.Contains(item))
						{
							aConditions.Insert(0, item);
						}
					}
				}
			}
		}

		private void CreateStatusElements(CondOwner co, List<string> aConditions)
		{
			this._condRows.Clear();
			this._conds = aConditions;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.CondRow, this._tfCondsContainer);
			this._condRows.Add(gameObject.transform);
			float num = 0f;
			int i = 0;
			while (i < aConditions.Count)
			{
				string text = aConditions[i];
				Condition condition = null;
				if (text.IndexOf("???") >= 0)
				{
					condition = DataHandler.GetCond("???");
					if (condition != null)
					{
						condition.strNameFriendly = text;
						goto IL_9E;
					}
				}
				else
				{
					if (!co.mapConds.TryGetValue(text, out condition))
					{
						condition = DataHandler.GetCond(text);
						goto IL_9E;
					}
					goto IL_9E;
				}
				IL_188:
				i++;
				continue;
				IL_9E:
				if (condition == null || string.IsNullOrEmpty(condition.strNameFriendly))
				{
					goto IL_188;
				}
				if (condition.strName == "_NewLine")
				{
					gameObject = UnityEngine.Object.Instantiate<GameObject>(this.CondRow, this._tfCondsContainer);
					this._condRows.Add(gameObject.transform);
					num = 0f;
					goto IL_188;
				}
				CondElement component = UnityEngine.Object.Instantiate<GameObject>(this.CondElement, gameObject.transform).GetComponent<CondElement>();
				component.SetData(co, condition);
				component.ForceMeshUpdate();
				num += component.Width + 12f;
				if (num >= this.MaxWidth)
				{
					gameObject = UnityEngine.Object.Instantiate<GameObject>(this.CondRow, this._tfCondsContainer);
					this._condRows.Add(gameObject.transform);
					component.transform.SetParent(gameObject.transform, false);
					num = component.Width + 10f;
					goto IL_188;
				}
				goto IL_188;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
		}

		protected override void OnUpdateUI()
		{
			bool flag = false;
			List<string> conditions = this.GetConditions(this._coDisplay);
			foreach (string item in conditions)
			{
				if (!this._conds.Contains(item))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (string item2 in this._conds)
				{
					if (!conditions.Contains(item2))
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			for (int i = this._condRows.Count - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(this._condRows[i].gameObject);
				this._condRows.RemoveAt(i);
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			if (this._coDisplay != null)
			{
				this.CreateStatusElements(this._coDisplay, this.GetConditions(this._coDisplay));
			}
		}

		[SerializeField]
		private Transform _tfCondsContainer;

		[SerializeField]
		private GameObject CondElement;

		[SerializeField]
		private GameObject CondRow;

		[SerializeField]
		private float MaxWidth = 250f;

		private List<Transform> _condRows = new List<Transform>();

		private CondOwner _coDisplay;

		private CondOwner _coThem;

		private static List<string> aDCList;

		private List<string> _conds = new List<string>();

		private Dictionary<string, UniqueList<string>> _dictWoundConds = new Dictionary<string, UniqueList<string>>();

		private static List<CondTrigger> _ctWoundMap;

		private const string UNKNOWN = "???";
	}
}
