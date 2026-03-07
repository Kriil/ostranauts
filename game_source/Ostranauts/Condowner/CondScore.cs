using System;
using Ostranauts.Core.Models;

namespace Ostranauts.Condowner
{
	public class CondScore
	{
		public CondScore()
		{
		}

		public CondScore(string strN)
		{
			this.strName = strN;
			this.nInteractions = 0;
			this.fTotalValue = 0f;
		}

		public CondScore(InteractionHistoryDTO dto)
		{
			if (dto == null)
			{
				return;
			}
			this.strName = dto.Name;
			this.nInteractions = dto.IterationCounter;
			this.fTotalValue = dto.TotalValue;
		}

		public string strName { get; set; }

		public int nInteractions { get; set; }

		public float fTotalValue { get; set; }

		public float fAverage
		{
			get
			{
				return (this.nInteractions != 0) ? (this.fTotalValue / (float)this.nInteractions) : 0f;
			}
		}

		public CondScore Clone()
		{
			return new CondScore(this.strName)
			{
				nInteractions = this.nInteractions,
				fTotalValue = this.fTotalValue
			};
		}

		public void AddCondScore(float fScore, bool bNew)
		{
			if (bNew)
			{
				this.nInteractions++;
			}
			this.fTotalValue += fScore;
		}

		public override string ToString()
		{
			return this.strName;
		}

		public string Print()
		{
			string empty = string.Empty;
			string text = empty;
			return string.Concat(new object[]
			{
				text,
				this.strName,
				",",
				this.nInteractions,
				",",
				this.fTotalValue,
				",",
				this.fAverage
			});
		}
	}
}
