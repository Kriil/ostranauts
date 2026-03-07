using System;
using System.Collections.Generic;

namespace Ostranauts.ShipGUIs.Chargen
{
	public class SkillSelectionDTO
	{
		public SkillSelectionDTO(Condition cond, int change)
		{
			this.Condition = cond;
			this.Change = change;
		}

		public string CondName
		{
			get
			{
				return this.Condition.strName;
			}
		}

		public Condition Condition;

		public int Change;

		public Dictionary<string, double> AgeConds = new Dictionary<string, double>();
	}
}
