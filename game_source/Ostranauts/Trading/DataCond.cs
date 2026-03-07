using System;

namespace Ostranauts.Trading
{
	public class DataCond
	{
		public DataCond(string value)
		{
			string text = value;
			if (value.StartsWith("-"))
			{
				text = value.Substring(1);
				this.NegativeValue = true;
			}
			string[] array = text.Split(new char[]
			{
				'='
			});
			this.CondName = array[0];
			if (array.Length > 1)
			{
				string[] array2 = array[1].Split(new char[]
				{
					'x'
				});
				float.TryParse(array2[0], out this.Chance);
				float.TryParse(array2[1], out this.Amount);
			}
		}

		public void JoinConds(DataCond dCon)
		{
			if (dCon.NegativeValue)
			{
				this.Amount -= dCon.Amount;
			}
			else
			{
				this.Amount += dCon.Amount;
			}
		}

		public string CondName;

		public float Chance;

		public float Amount;

		public bool NegativeValue;
	}
}
