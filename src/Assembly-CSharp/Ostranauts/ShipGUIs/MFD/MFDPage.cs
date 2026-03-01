using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ostranauts.Core.Models;
using Ostranauts.Events.DTOs;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.Ships.Comms;
using UnityEngine;

namespace Ostranauts.ShipGUIs.MFD
{
	public abstract class MFDPage
	{
		protected virtual string Title { get; set; }

		protected virtual List<string> Left { get; set; }

		protected virtual List<string> Right { get; set; }

		protected CondOwner CoUs
		{
			get
			{
				if (this._coUs == null)
				{
					this._coUs = CrewSim.GetSelectedCrew();
				}
				return this._coUs;
			}
		}

		protected Ship ShipUs
		{
			get
			{
				if (this._shipUs != null || this.CoUs == null)
				{
					return this._shipUs;
				}
				this._shipUs = this.CoUs.ship;
				return this._shipUs;
			}
		}

		public virtual MFDPage OnButtonDown(int btnIndex)
		{
			if (btnIndex == this._mainMenuButton)
			{
				return new MFDMainMenu();
			}
			return this;
		}

		protected void UpdateDisplay()
		{
			GUIMFDDisplay.OnUpdateMFD.Invoke(new MFDDTO
			{
				Left = this.Left,
				Right = this.Right,
				Title = this.Title
			});
		}

		public virtual void OnUIRefresh(ShipMessage shipMessage)
		{
		}

		protected void PopulateMFD(List<ShipDist> aContacts, int subPage)
		{
			Tuple<List<string>, List<string>> tuple = this.StringListToPageLayout(this.GetKnownIDs(aContacts), subPage, false);
			this.Title = ((aContacts.Count != 0) ? "SELECT TARGET" : "NO TARGETS IN RANGE");
			this.Left = tuple.Item1;
			this.Right = tuple.Item2;
			this.UpdateDisplay();
		}

		protected void PopulateMFD(List<string> content, int subPage, string title = null)
		{
			Tuple<List<string>, List<string>> tuple = this.StringListToPageLayout(content, subPage, false);
			this.Title = ((title != null) ? title : this.Title);
			this.Left = tuple.Item1;
			this.Right = tuple.Item2;
			this.UpdateDisplay();
		}

		protected List<string> GetKnownIDs(List<ShipDist> aContacts)
		{
			List<string> list = new List<string>();
			foreach (ShipDist shipDist in aContacts)
			{
				list.Add(shipDist.si.strRegID);
			}
			return list;
		}

		protected Tuple<List<string>, List<string>> StringListToPageLayout(List<Interaction> content, int currentPage = 0)
		{
			return this.StringListToPageLayout((from ia in content
			select ia.strTitle).ToList<string>(), currentPage, false);
		}

		protected Tuple<List<string>, List<string>> StringListToPageLayout(List<string> content, int currentPage = 0, bool useAllRows = false)
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			if (content == null)
			{
				return new Tuple<List<string>, List<string>>(list, list2);
			}
			int num = (!useAllRows) ? 4 : 8;
			int num2 = Mathf.Clamp(currentPage, 0, content.Count / (num * 2));
			int num3 = num * 2 * num2;
			for (int i = num3; i < num3 + num; i++)
			{
				if (!useAllRows)
				{
					list.Add(string.Empty);
				}
				if (content.Count > i)
				{
					list.Add("< " + this.ClampText(content, i, i + num));
				}
				else
				{
					list.Add(string.Empty);
				}
			}
			list.Add("-----------------------------------------------------------");
			list.Add((num2 <= 0) ? string.Empty : "<PREVIOUS PAGE");
			for (int j = num3 + num; j < num3 + num * 2; j++)
			{
				if (!useAllRows)
				{
					list2.Add(string.Empty);
				}
				if (content.Count > j)
				{
					list2.Add(this.ClampText(content, j, j - num) + " >");
				}
				else
				{
					list2.Add(string.Empty);
				}
			}
			list2.Add(string.Empty);
			list2.Add((num2 >= content.Count / (2 * num + 1)) ? string.Empty : "NEXT PAGE>");
			list2.Add("RETURN TO");
			list2.Add("MAIN MENU>");
			return new Tuple<List<string>, List<string>>(list, list2);
		}

		private string ClampText(List<string> content, int currentIndex, int indexOfOtherSide)
		{
			string text = content[currentIndex];
			string text2 = Regex.Replace(text, "<.*?>", string.Empty);
			if (text2.Length < 20 || content.Count <= indexOfOtherSide)
			{
				return text;
			}
			string input = content[indexOfOtherSide];
			string text3 = Regex.Replace(input, "<.*?>", string.Empty);
			if (text2.Length + text3.Length < 44)
			{
				return text;
			}
			return text.Replace(text2, text2.Substring(0, 17) + "..");
		}

		protected int PageSelectionToIndex(int btnIndex, int currentSubPage)
		{
			int num = currentSubPage * 8;
			int num2 = (btnIndex <= 5) ? btnIndex : (btnIndex - 2);
			return num + num2;
		}

		protected readonly int _mainMenuButton = 11;

		private CondOwner _coUs;

		private Ship _shipUs;
	}
}
