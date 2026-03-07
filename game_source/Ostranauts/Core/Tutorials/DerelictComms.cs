using System;

namespace Ostranauts.Core.Tutorials
{
	public class DerelictComms : SwitchToComms
	{
		public override string NextDefault
		{
			get
			{
				return "GainClearance";
			}
		}
	}
}
