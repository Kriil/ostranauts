using System;

namespace Ostranauts.UI.Quickbar.Models
{
	public class AvailableActionDTO
	{
		public AvailableActionDTO(Interaction ia, bool isReply)
		{
			this.Ia = ia;
			this.IsReply = isReply;
		}

		public AvailableActionDTO(Interaction ia)
		{
			this.Ia = ia;
		}

		public Interaction Ia { get; private set; }

		public bool IsFight
		{
			get
			{
				return this.Ia != null && this.Ia.strActionGroup == "Fight";
			}
		}

		public bool IsGambit
		{
			get
			{
				return this.Ia != null && this.Ia.bGamit;
			}
		}

		public int IAOrderPriority
		{
			get
			{
				return (this.Ia == null) ? 0 : this.Ia.nQabOrderPriority;
			}
		}

		public bool Matches(string iaName)
		{
			return this.Ia != null && this.Ia.strName == iaName;
		}

		public bool IsReply;

		public bool IsClickable = true;

		public bool IsGig;

		public int IndexPosition = -1;
	}
}
