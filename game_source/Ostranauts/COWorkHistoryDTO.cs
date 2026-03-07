using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;

namespace Ostranauts
{
	public class COWorkHistoryDTO
	{
		public Tuple<double, string> LastPledge { get; private set; }

		public List<Tuple<double, string>> GetLastFailedWorkAttempts()
		{
			return (from x in this.LastFailedWorkAttempts
			where StarSystem.fEpoch - x.Item1 < 30.0
			select x).ToList<Tuple<double, string>>();
		}

		public void RecordFailedWorkAttempt(string reason)
		{
			this.LastFailedWorkAttempts.Add(new Tuple<double, string>(StarSystem.fEpoch, reason));
			if (this.LastFailedWorkAttempts.Count > 2)
			{
				this.LastFailedWorkAttempts = this.LastFailedWorkAttempts.GetRange(this.LastFailedWorkAttempts.Count - 2, 2);
			}
		}

		public void RecordPledge(Pledge2 pledge)
		{
			this.LastPledge = new Tuple<double, string>(StarSystem.fEpoch, pledge.NameFriendly);
		}

		private List<Tuple<double, string>> LastFailedWorkAttempts = new List<Tuple<double, string>>();
	}
}
