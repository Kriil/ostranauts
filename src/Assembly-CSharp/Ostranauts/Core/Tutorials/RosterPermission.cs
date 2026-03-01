using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class RosterPermission : TutorialBeat
	{
		public RosterPermission()
		{
			GUIRosterRow.Opened.AddListener(new UnityAction(this.RespondToDelegate));
		}

		public override string ObjectiveName
		{
			get
			{
				return "Change Roster Permissions";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Open the Roster UI and change the airlock permissions";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Permissions changed";
			}
		}

		public override string NextDefault
		{
			get
			{
				return string.Empty;
			}
		}

		public void RespondToDelegate()
		{
			base.Finished = true;
		}

		public override void Process()
		{
			base.Process();
		}
	}
}
