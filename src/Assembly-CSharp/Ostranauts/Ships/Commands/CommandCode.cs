using System;

namespace Ostranauts.Ships.Commands
{
	[Flags]
	public enum CommandCode
	{
		None = 0,
		Finished = 1,
		Skipped = 2,
		Ongoing = 4,
		Cancelled = 8,
		ResultDone = 11,
		ResultNegative = 10
	}
}
