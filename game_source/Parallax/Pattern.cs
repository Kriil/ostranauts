using System;

namespace Parallax
{
	// Selection pattern for cycling parallax/background assets.
	// Likely used by the parallax system to choose which sprite/panel to show next.
	public enum Pattern
	{
		// Cycle through all entries in order.
		RoundRobin,
		// Keep reusing the most recently selected entry.
		RepeatLast,
		// Always restart from the first entry.
		RepeatFirst,
		// Use only one entry with no cycling.
		Single
	}
}
