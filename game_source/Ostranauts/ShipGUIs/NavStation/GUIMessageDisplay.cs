using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.Core;
using Ostranauts.Ships.Comms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class GUIMessageDisplay : MonoSingleton<GUIMessageDisplay>
	{
		private string ShipUs
		{
			get
			{
				return CrewSim.GetSelectedCrew().ship.strRegID;
			}
		}

		private new void Awake()
		{
			base.Awake();
		}

		private void Update()
		{
			if (this.cgComms.alpha < 0.95f)
			{
				return;
			}
			if (Input.mouseScrollDelta.y > 0f)
			{
				this.ScrollLog(0.1f);
			}
			else if (Input.mouseScrollDelta.y < 0f)
			{
				this.ScrollLog(-0.1f);
			}
		}

		private void ScrollLog(float fAmount)
		{
			float verticalNormalizedPosition = Mathf.Clamp(this.srLog.verticalNormalizedPosition + fAmount * this.srLog.verticalScrollbar.size, 0f, 1f);
			this.srLog.verticalNormalizedPosition = verticalNormalizedPosition;
			AudioManager.am.PlayAudioEmitter("ShipUINSMapPan02", false, false);
		}

		private void LogConversation(string conversation = null)
		{
			conversation = "\n" + conversation;
			TMP_Text tmp_Text = this.txtComms;
			tmp_Text.text += conversation;
			base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srLog));
		}

		private void PreSetup(string text)
		{
			if (this._statusAnimation == null && !this.txtStatus.text.Contains("OPERATIONAL"))
			{
				this._statusAnimation = base.StartCoroutine(this.PlayStatusMessages());
			}
			this.txtComms.text = text;
			if (string.IsNullOrEmpty(text))
			{
				this.LogConversation("<align=\"center\">----------------------- Connection established -----------------------</align>");
				this.LogConversation(text);
			}
			CanvasManager.ShowCanvasGroup(this.cgComms);
		}

		public void ShowPanel(string text, List<ShipMessage> messages)
		{
			this.PreSetup(text);
			if (messages == null || messages.Count == 0)
			{
				return;
			}
			IEnumerable<ShipMessage> enumerable = (from x in messages
			orderby x.AvailableTime
			select x).Take(100);
			foreach (ShipMessage mfdMessage in enumerable)
			{
				this.AddMessage(mfdMessage);
			}
		}

		public void ShowLog(List<ShipMessage> messages)
		{
			if (messages == null || messages.Count == 0)
			{
				return;
			}
			this.txtComms.text = string.Empty;
			IOrderedEnumerable<ShipMessage> orderedEnumerable = from x in messages
			orderby x.AvailableTime
			select x;
			string text = string.Empty;
			foreach (ShipMessage shipMessage in orderedEnumerable)
			{
				if (text != shipMessage.SenderRegId && text != shipMessage.ReceiverRegId)
				{
					text = ((!(shipMessage.SenderRegId != this.ShipUs)) ? shipMessage.ReceiverRegId : shipMessage.SenderRegId);
					this.LogConversation("<align=\"center\">----------------------- " + text + " -----------------------</align>");
				}
				this.AddMessage(shipMessage);
			}
			CanvasManager.ShowCanvasGroup(this.cgComms);
		}

		public void AddMessage(ShipMessage mfdMessage)
		{
			if (mfdMessage == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<color=#");
			stringBuilder.Append(ColorUtility.ToHtmlStringRGB(DataHandler.GetColor((mfdMessage.Interaction == null) ? "Neutral" : mfdMessage.Interaction.strColor)));
			stringBuilder.Append(">");
			bool flag = mfdMessage.SenderRegId != this.ShipUs;
			string messageText = mfdMessage.MessageText;
			int num = (!(this.ShipUs == mfdMessage.ReceiverRegId)) ? 50 : 99;
			if (flag)
			{
				stringBuilder.Append("<align=\"right\">");
				stringBuilder.AppendLine("<size=12><alpha=#55>[" + MathUtils.GetUTCFromS(mfdMessage.AvailableTime) + "]</size>");
				stringBuilder.Append("<alpha=#" + num + ">");
			}
			else
			{
				stringBuilder.AppendLine("<size=12><alpha=#55>[" + MathUtils.GetUTCFromS(mfdMessage.AvailableTime) + "]</size><alpha=#99>");
			}
			stringBuilder.Append(messageText);
			if (flag)
			{
				stringBuilder.Append("<alpha=#FF></align>");
			}
			stringBuilder.Append("</color>");
			this.LogConversation(stringBuilder.ToString());
		}

		public void HidePanel()
		{
			if (this._statusAnimation != null)
			{
				base.StopCoroutine(this._statusAnimation);
				this._statusAnimation = null;
			}
			CanvasManager.HideCanvasGroup(this.cgComms);
		}

		public void HidePanelDelayed()
		{
			base.StartCoroutine(this.HidePanelDelayed(2f));
		}

		private IEnumerator HidePanelDelayed(float delay)
		{
			yield return new WaitForSeconds(delay);
			this.HidePanel();
			yield break;
		}

		private IEnumerator PlayStatusMessages()
		{
			List<string> messageList = new List<string>();
			this.RedrawStatus(messageList);
			for (int i = 0; i < this._statusMessages.Count; i++)
			{
				messageList.Add(this._statusMessages[i]);
				for (int j = 0; j < 5; j++)
				{
					List<string> list;
					int index;
					(list = messageList)[index = i] = list[index] + ".";
					this.RedrawStatus(messageList);
					yield return new WaitForSeconds(0.1f);
				}
				if (i == this._statusMessages.Count - 1)
				{
					List<string> list;
					int index2;
					(list = messageList)[index2 = i] = list[index2] + "<color=green>OPERATIONAL</color>";
				}
				else
				{
					List<string> list;
					int index3;
					(list = messageList)[index3 = i] = list[index3] + "<color=green>DONE</color>";
				}
				this.RedrawStatus(messageList);
				yield return new WaitForSeconds(0.14f);
			}
			this._statusAnimation = null;
			yield return null;
			yield break;
		}

		private void RedrawStatus(List<string> messages)
		{
			this.txtStatus.text = string.Empty;
			foreach (string str in messages)
			{
				TMP_Text tmp_Text = this.txtStatus;
				tmp_Text.text = tmp_Text.text + str + "\n";
			}
		}

		[SerializeField]
		private CanvasGroup cgComms;

		[SerializeField]
		private TMP_Text txtStatus;

		[SerializeField]
		private TMP_Text txtComms;

		[SerializeField]
		private ScrollRect srLog;

		private Coroutine _statusAnimation;

		private readonly List<string> _statusMessages = new List<string>
		{
			"Open incoming port",
			"Build routing table",
			"Load kernel driver",
			"Interface message Processor"
		};
	}
}
