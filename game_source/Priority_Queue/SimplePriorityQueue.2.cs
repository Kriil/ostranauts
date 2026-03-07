using System;
using System.Collections.Generic;

namespace Priority_Queue
{
	public class SimplePriorityQueue<TItem> : SimplePriorityQueue<TItem, float>
	{
		public SimplePriorityQueue()
		{
		}

		public SimplePriorityQueue(IComparer<float> comparer) : base(comparer)
		{
		}

		public SimplePriorityQueue(Comparison<float> comparer) : base(comparer)
		{
		}
	}
}
