using System;
using System.Text;
using Ostranauts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class PersonModule : ItemModule
	{
		public override void SetData(CondOwner co)
		{
			base.SetData(co);
			this._coSelected = co;
			this._txtDescription.text = this.CollectAboutInformation(co);
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
		}

		protected override void OnUpdateUI()
		{
			MonoSingleton<GUIRenderTargets>.Instance.SetFace(this._coSelected, false);
			this._txtDescription.text = this.CollectAboutInformation(this._coSelected);
		}

		private string CollectAboutInformation(CondOwner co)
		{
			StringBuilder stringBuilder = new StringBuilder();
			GUIChargenStack component = co.GetComponent<GUIChargenStack>();
			if (component != null)
			{
				stringBuilder.Append("Age: ");
				stringBuilder.Append(PersonModule.strColorStart);
				stringBuilder.Append(co.GetCondAmount("StatAge").ToString("0"));
				stringBuilder.Append(PersonModule.strColorEnd);
				stringBuilder.Append("  Gender: ");
				string value = "n/a";
				if (co.pspec != null)
				{
					Condition cond = DataHandler.GetCond(co.pspec.strGender);
					if (cond != null)
					{
						value = cond.strNameFriendly;
					}
				}
				stringBuilder.Append(PersonModule.strColorStart);
				stringBuilder.Append(value);
				stringBuilder.AppendLine(PersonModule.strColorEnd);
				stringBuilder.Append("Career: ");
				stringBuilder.Append(PersonModule.strColorStart);
				if (component.GetLatestCareer() != null && component.GetLatestCareer().GetJC() != null)
				{
					stringBuilder.Append(component.GetLatestCareer().GetJC().strNameFriendly);
				}
				else
				{
					stringBuilder.Append("?");
				}
				stringBuilder.AppendLine(PersonModule.strColorEnd);
				stringBuilder.Append("Homeworld: ");
				if (component.GetHomeworld() != null)
				{
					stringBuilder.Append(PersonModule.strColorStart);
					stringBuilder.Append(component.GetHomeworld().strColonyName);
					stringBuilder.AppendLine(PersonModule.strColorEnd);
					stringBuilder.Append("Strata: ");
					string text = string.Empty;
					foreach (Condition condition in co.mapConds.Values)
					{
						if (condition.strName.IndexOf("IsStrata", StringComparison.Ordinal) == 0)
						{
							if (text.Length > 0)
							{
								text += ", ";
							}
							text += condition.strNameFriendly;
						}
					}
					stringBuilder.Append(PersonModule.strColorStart);
					stringBuilder.Append((!(text != string.Empty)) ? "?" : text);
					stringBuilder.AppendLine(PersonModule.strColorEnd);
				}
				else
				{
					stringBuilder.Append(PersonModule.strColorStart);
					stringBuilder.Append("?");
					stringBuilder.AppendLine(PersonModule.strColorEnd);
				}
				stringBuilder.Append("Factions: ");
				bool flag = false;
				stringBuilder.Append(PersonModule.strColorStart);
				foreach (string strName in co.GetAllFactions())
				{
					JsonFaction faction = CrewSim.system.GetFaction(strName);
					if (faction != null && !(faction.strName == co.strID))
					{
						if (flag)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(faction.strNameFriendly);
						flag = true;
					}
				}
				if (!flag)
				{
					stringBuilder.Append("n/a");
				}
				stringBuilder.AppendLine(PersonModule.strColorEnd);
				stringBuilder.Append("My Standings: ");
				stringBuilder.Append(PersonModule.strColorStart);
				stringBuilder.Append(co.GetFactionScore(CrewSim.GetSelectedCrew().GetAllFactions()).ToString("0.00"));
				if (CrewSim.bEnableDebugCommands)
				{
					stringBuilder.Append("; Fear: " + co.GetCondAmount("StatFightFear").ToString("0.00"));
				}
				stringBuilder.AppendLine(PersonModule.strColorEnd);
			}
			stringBuilder.AppendLine();
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (co != selectedCrew && co.socUs != null)
			{
				Relationship relationship = co.socUs.GetRelationship(selectedCrew.strName);
				if (relationship == null)
				{
					relationship = co.socUs.AddStranger(selectedCrew.pspec);
				}
				Relationship relationship2 = selectedCrew.socUs.GetRelationship(co.strName);
				if (relationship2 == null)
				{
					relationship2 = selectedCrew.socUs.AddStranger(co.pspec);
				}
				stringBuilder.Append("They See Us As: ");
				stringBuilder.Append(PersonModule.strColorStart);
				if (relationship.aRelationships.Count == 0)
				{
					stringBuilder.Append("None");
				}
				else
				{
					for (int i = 0; i < relationship.aRelationships.Count; i++)
					{
						if (i > 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(DataHandler.GetCond(relationship.aRelationships[i]).strNameFriendly);
					}
				}
				stringBuilder.AppendLine(PersonModule.strColorEnd);
				stringBuilder.Append("We See Them As: ");
				stringBuilder.Append(PersonModule.strColorStart);
				if (relationship2.aRelationships.Count == 0)
				{
					stringBuilder.Append("None");
				}
				else
				{
					for (int j = 0; j < relationship2.aRelationships.Count; j++)
					{
						if (j > 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(DataHandler.GetCond(relationship2.aRelationships[j]).strNameFriendly);
					}
				}
				stringBuilder.AppendLine(PersonModule.strColorEnd);
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}

		protected override Texture GetTexture(CondOwner co)
		{
			return MonoSingleton<GUIRenderTargets>.Instance.CreatePortrait(co);
		}

		private static string strColorStart = "<color=#999999>";

		private static string strColorEnd = "</color>";

		private CondOwner _coSelected;
	}
}
