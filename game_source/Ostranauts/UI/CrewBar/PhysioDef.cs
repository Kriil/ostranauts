using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.UI.CrewBar
{
	public class PhysioDef
	{
		public void Init()
		{
			if (this._loaded)
			{
				return;
			}
			this.iconPos = DataHandler.LoadPNG(this.Icon + ".png", false, false);
			this.iconNeg = DataHandler.LoadPNG(this.IconNeg + ".png", false, false);
			this.aConds = new List<Condition>();
			foreach (string strName in this.CondTrack)
			{
				Condition cond = DataHandler.GetCond(strName);
				if (cond != null)
				{
					this.aConds.Add(cond);
				}
			}
			this._loaded = true;
		}

		public double CalculateFillAmount(CondOwner co)
		{
			if (co == null)
			{
				return 0.0;
			}
			if (this.NeedsRoom && !co.HasCond("IsAirtight"))
			{
				if (co.currentRoom == null || co.currentRoom.CO == null)
				{
					return 0.0;
				}
				co = co.currentRoom.CO;
			}
			double num = co.GetCondAmount(this.StatTracked);
			num -= this.Minimum;
			return (num >= 0.0) ? (num / this.Maximum) : (num / this.InverseMaximum);
		}

		public string Title = "GUI_STAT_NONE";

		public string Icon = "ComputerIconX";

		public string IconNeg = "ComputerIconX";

		public string StatTracked = string.Empty;

		public double Minimum;

		public double Maximum = 100.0;

		public double InverseMaximum = -100.0;

		public List<string> CondTrack;

		public bool NeedsRoom;

		public Texture iconPos;

		public Texture iconNeg;

		public List<Condition> aConds;

		private bool _loaded;
	}
}
