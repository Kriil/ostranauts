using System;
using System.Collections.Generic;

namespace Ostranauts.Pathing
{
	public class JPSPriorityQueue<T> where T : Node
	{
		public int Count
		{
			get
			{
				return this.heap.Count;
			}
		}

		public void Enqueue(T item)
		{
			this.heap.Add(item);
			int num = this.heap.Count - 1;
			this.indices[item] = num;
			this.SiftUp(num);
		}

		public T Dequeue()
		{
			if (this.heap.Count == 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}
			T t = this.heap[0];
			this.indices.Remove(t);
			if (this.heap.Count > 1)
			{
				this.heap[0] = this.heap[this.heap.Count - 1];
				this.indices[this.heap[0]] = 0;
				this.heap.RemoveAt(this.heap.Count - 1);
				this.SiftDown(0);
			}
			else
			{
				this.heap.Clear();
			}
			return t;
		}

		public bool Contains(T item)
		{
			return this.indices.ContainsKey(item);
		}

		public void UpdatePriority(T item)
		{
			int index = 0;
			if (!this.indices.TryGetValue(item, out index))
			{
				return;
			}
			this.SiftUp(index);
			this.SiftDown(index);
		}

		private void SiftUp(int index)
		{
			T t = this.heap[index];
			int num = (index - 1) / 2;
			while (index > 0)
			{
				T t2 = this.heap[num];
				if (t2.TotalCost <= t.TotalCost)
				{
					break;
				}
				this.heap[index] = this.heap[num];
				this.indices[this.heap[index]] = index;
				index = num;
				num = (index - 1) / 2;
			}
			this.heap[index] = t;
			this.indices[t] = index;
		}

		private void SiftDown(int index)
		{
			int count = this.heap.Count;
			T t = this.heap[index];
			for (;;)
			{
				int num = index * 2 + 1;
				if (num >= count)
				{
					break;
				}
				int num2 = num + 1;
				if (num2 >= count)
				{
					goto IL_7C;
				}
				T t2 = this.heap[num2];
				float totalCost = t2.TotalCost;
				T t3 = this.heap[num];
				if (totalCost >= t3.TotalCost)
				{
					goto IL_7C;
				}
				int num3 = num2;
				IL_7E:
				float totalCost2 = t.TotalCost;
				T t4 = this.heap[num3];
				if (totalCost2 <= t4.TotalCost)
				{
					break;
				}
				this.heap[index] = this.heap[num3];
				this.indices[this.heap[index]] = index;
				index = num3;
				continue;
				IL_7C:
				num3 = num;
				goto IL_7E;
			}
			this.heap[index] = t;
			this.indices[t] = index;
		}

		private List<T> heap = new List<T>();

		private Dictionary<T, int> indices = new Dictionary<T, int>();
	}
}
