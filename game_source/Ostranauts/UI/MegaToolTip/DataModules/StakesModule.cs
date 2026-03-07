using System;
using System.Collections.Generic;
using System.Text;
using Ostranauts.Social.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class StakesModule : ModuleBase
	{
		private new void Awake()
		{
			this._cg.alpha = 0f;
			this._layoutElement.ignoreLayout = true;
			GUISocialCombat2.OnContextUpdated.AddListener(new UnityAction<Interaction, SocialStakes>(this.OnStakesUpdated));
		}

		private void OnDestroy()
		{
			GUISocialCombat2.OnContextUpdated.RemoveListener(new UnityAction<Interaction, SocialStakes>(this.OnStakesUpdated));
		}

		private void OnStakesUpdated(Interaction ia, SocialStakes stakes)
		{
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if ((ia.objUs == selectedCrew && ia.objThem == this._co) || (ia.objUs == this._co && ia.objThem == selectedCrew))
			{
				if (stakes == null)
				{
					this.ShowModule(false);
				}
				this.UpdateStakesInfo(GUISocialCombat2.GetStakesInfo(selectedCrew, this._co), selectedCrew);
			}
		}

		public override void SetData(CondOwner co)
		{
			if (co == null)
			{
				return;
			}
			this._co = co;
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (this._co != selectedCrew && this._co.socUs != null)
			{
				this.UpdateStakesInfo(GUISocialCombat2.GetStakesInfo(selectedCrew, co), selectedCrew);
			}
		}

		private void UpdateStakesInfo(string stakesInfo, CondOwner coViewer)
		{
			if (string.IsNullOrEmpty(stakesInfo) || coViewer == null)
			{
				this.ShowModule(false);
				return;
			}
			this._txtTitle.text = DataHandler.GetString("SOCIAL_STAKES_TITLE", false);
			Color color = DataHandler.GetColor("SocialStatusRed");
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(stakesInfo);
			List<string> list = new List<string>();
			List<string> aCondsPass = new List<string>();
			GUISocialStatus.GetPassFailConds(aCondsPass, list, coViewer, this._co, null);
			if (list.Count > 0)
			{
				stringBuilder.AppendLine(DataHandler.GetString("SOCIAL_STAKES_FAILS", false));
				bool flag = false;
				foreach (string strCondName in list)
				{
					if (flag)
					{
						stringBuilder.Append(DataHandler.GetString("SOCIAL_STAKES_COMMA", false));
					}
					flag = true;
					stringBuilder.Append("<color=#");
					stringBuilder.Append(ColorUtility.ToHtmlStringRGB(color));
					stringBuilder.Append(">");
					stringBuilder.Append(DataHandler.GetCondFriendlyName(strCondName));
					stringBuilder.Append("</color>");
				}
				stringBuilder.AppendLine();
			}
			this._txtDescription.text = stringBuilder.ToString();
			this._txtDescription.ForceMeshUpdate();
			this.ShowModule(true);
		}

		private void ShowModule(bool show)
		{
			bool flag = this._layoutElement.ignoreLayout == show;
			if (show)
			{
				this._cg.alpha = 1f;
				this._layoutElement.ignoreLayout = false;
			}
			else
			{
				this._cg.alpha = 0f;
				this._layoutElement.ignoreLayout = true;
			}
			if (flag)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
			}
		}

		[SerializeField]
		private LayoutElement _layoutElement;

		[SerializeField]
		private CanvasGroup _cg;

		[SerializeField]
		private TMP_Text _txtTitle;

		[SerializeField]
		private TMP_Text _txtDescription;

		private static string strColorStart = "<color=#999999>";

		private static string strColorEnd = "</color>";

		private CondOwner _co;
	}
}
