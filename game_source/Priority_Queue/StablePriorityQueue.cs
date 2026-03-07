using System;
using System.Collections;
using System.Collections.Generic;

namespace Priority_Queue
{
	public sealed class StablePriorityQueue<T> : IFixedSizePriorityQueue<T, float>, IPriorityQueue<T, float>, IEnumerable<T>, IEnumerable where T : StablePriorityQueueNode
	{
		public StablePriorityQueue(int maxNodes)
		{
			this._numNodes = 0;
			this._nodes = new T[maxNodes + 1];
			this._numNodesEverEnqueued = 0L;
		}

		public int Count
		{
			get
			{
				return this._numNodes;
			}
		}

		public int MaxSize
		{
			get
			{
				return this._nodes.Length - 1;
			}
		}

		public void Clear()
		{
			Array.Clear(this._nodes, 1, this._numNodes);
			this._numNodes = 0;
		}

		public bool Contains(T node)
		{
			return this._nodes[node.QueueIndex] == node;
		}

		public void Enqueue(T node, float priority)
		{
			node.Priority = priority;
			this._numNodes++;
			this._nodes[this._numNodes] = node;
			node.QueueIndex = this._numNodes;
			long numNodesEverEnqueued;
			this._numNodesEverEnqueued = (numNodesEverEnqueued = this._numNodesEverEnqueued) + 1L;
			node.InsertionIndex = numNodesEverEnqueued;
			this.CascadeUp(node);
		}

		private void CascadeUp(T node)
		{
			if (node.QueueIndex <= 1)
			{
				return;
			}
			int i = node.QueueIndex >> 1;
			T t = this._nodes[i];
			if (this.HasHigherPriority(t, node))
			{
				return;
			}
			this._nodes[node.QueueIndex] = t;
			t.QueueIndex = node.QueueIndex;
			node.QueueIndex = i;
			while (i > 1)
			{
				i >>= 1;
				T t2 = this._nodes[i];
				if (this.HasHigherPriority(t2, node))
				{
					break;
				}
				this._nodes[node.QueueIndex] = t2;
				t2.QueueIndex = node.QueueIndex;
				node.QueueIndex = i;
			}
			this._nodes[node.QueueIndex] = node;
		}

		private void CascadeDown(T node)
		{
			int num = node.QueueIndex;
			int num2 = 2 * num;
			if (num2 > this._numNodes)
			{
				return;
			}
			int num3 = num2 + 1;
			T t = this._nodes[num2];
			if (this.HasHigherPriority(t, node))
			{
				if (num3 > this._numNodes)
				{
					node.QueueIndex = num2;
					t.QueueIndex = num;
					this._nodes[num] = t;
					this._nodes[num2] = node;
					return;
				}
				T t2 = this._nodes[num3];
				if (this.HasHigherPriority(t, t2))
				{
					t.QueueIndex = num;
					this._nodes[num] = t;
					num = num2;
				}
				else
				{
					t2.QueueIndex = num;
					this._nodes[num] = t2;
					num = num3;
				}
			}
			else
			{
				if (num3 > this._numNodes)
				{
					return;
				}
				T t3 = this._nodes[num3];
				if (!this.HasHigherPriority(t3, node))
				{
					return;
				}
				t3.QueueIndex = num;
				this._nodes[num] = t3;
				num = num3;
			}
			for (;;)
			{
				num2 = 2 * num;
				if (num2 > this._numNodes)
				{
					break;
				}
				num3 = num2 + 1;
				t = this._nodes[num2];
				if (this.HasHigherPriority(t, node))
				{
					if (num3 > this._numNodes)
					{
						goto Block_9;
					}
					T t4 = this._nodes[num3];
					if (this.HasHigherPriority(t, t4))
					{
						t.QueueIndex = num;
						this._nodes[num] = t;
						num = num2;
					}
					else
					{
						t4.QueueIndex = num;
						this._nodes[num] = t4;
						num = num3;
					}
				}
				else
				{
					if (num3 > this._numNodes)
					{
						goto Block_11;
					}
					T t5 = this._nodes[num3];
					if (!this.HasHigherPriority(t5, node))
					{
						goto IL_28F;
					}
					t5.QueueIndex = num;
					this._nodes[num] = t5;
					num = num3;
				}
			}
			node.QueueIndex = num;
			this._nodes[num] = node;
			return;
			Block_9:
			node.QueueIndex = num2;
			t.QueueIndex = num;
			this._nodes[num] = t;
			this._nodes[num2] = node;
			return;
			Block_11:
			node.QueueIndex = num;
			this._nodes[num] = node;
			return;
			IL_28F:
			node.QueueIndex = num;
			this._nodes[num] = node;
		}

		private bool HasHigherPriority(T higher, T lower)
		{
			return higher.Priority < lower.Priority || (higher.Priority == lower.Priority && higher.InsertionIndex < lower.InsertionIndex);
		}

		public T Dequeue()
		{
			T result = this._nodes[1];
			if (this._numNodes == 1)
			{
				this._nodes[1] = (T)((object)null);
				this._numNodes = 0;
				return result;
			}
			T t = this._nodes[this._numNodes];
			this._nodes[1] = t;
			t.QueueIndex = 1;
			this._nodes[this._numNodes] = (T)((object)null);
			this._numNodes--;
			this.CascadeDown(t);
			return result;
		}

		public void Resize(int maxNodes)
		{
			T[] array = new T[maxNodes + 1];
			int num = Math.Min(maxNodes, this._numNodes);
			Array.Copy(this._nodes, array, num + 1);
			this._nodes = array;
		}

		public T First
		{
			get
			{
				return this._nodes[1];
			}
		}

		public void UpdatePriority(T node, float priority)
		{
			node.Priority = priority;
			this.OnNodeUpdated(node);
		}

		private void OnNodeUpdated(T node)
		{
			int num = node.QueueIndex >> 1;
			if (num > 0 && this.HasHigherPriority(node, this._nodes[num]))
			{
				this.CascadeUp(node);
			}
			else
			{
				this.CascadeDown(node);
			}
		}

		public void Remove(T node)
		{
			if (node.QueueIndex == this._numNodes)
			{
				this._nodes[this._numNodes] = (T)((object)null);
				this._numNodes--;
				return;
			}
			T t = this._nodes[this._numNodes];
			this._nodes[node.QueueIndex] = t;
			t.QueueIndex = node.QueueIndex;
			this._nodes[this._numNodes] = (T)((object)null);
			this._numNodes--;
			this.OnNodeUpdated(t);
		}

		public void ResetNode(T node)
		{
			node.QueueIndex = 0;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 1; i <= this._numNodes; i++)
			{
				yield return this._nodes[i];
			}
			yield break;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public bool IsValidQueue()
		{
			for (int i = 1; i < this._nodes.Length; i++)
			{
				if (this._nodes[i] != null)
				{
					int num = 2 * i;
					if (num < this._nodes.Length && this._nodes[num] != null && this.HasHigherPriority(this._nodes[num], this._nodes[i]))
					{
						return false;
					}
					int num2 = num + 1;
					if (num2 < this._nodes.Length && this._nodes[num2] != null && this.HasHigherPriority(this._nodes[num2], this._nodes[i]))
					{
						return false;
					}
				}
			}
			return true;
		}

		private int _numNodes;

		private T[] _nodes;

		private long _numNodesEverEnqueued;
	}
}
