using System;
using System.Collections;
using System.Collections.Generic;

namespace Priority_Queue
{
	public sealed class GenericPriorityQueue<TItem, TPriority> : IFixedSizePriorityQueue<TItem, TPriority>, IPriorityQueue<TItem, TPriority>, IEnumerable<TItem>, IEnumerable where TItem : GenericPriorityQueueNode<TPriority> where TPriority : IComparable<TPriority>
	{
		public GenericPriorityQueue(int maxNodes) : this(maxNodes, Comparer<TPriority>.Default)
		{
		}

		public GenericPriorityQueue(int maxNodes, IComparer<TPriority> comparer) : this(maxNodes, new Comparison<TPriority>(comparer.Compare))
		{
		}

		public GenericPriorityQueue(int maxNodes, Comparison<TPriority> comparer)
		{
			this._numNodes = 0;
			this._nodes = new TItem[maxNodes + 1];
			this._numNodesEverEnqueued = 0L;
			this._comparer = comparer;
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

		public bool Contains(TItem node)
		{
			return this._nodes[node.QueueIndex] == node;
		}

		public void Enqueue(TItem node, TPriority priority)
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

		private void CascadeUp(TItem node)
		{
			if (node.QueueIndex <= 1)
			{
				return;
			}
			int i = node.QueueIndex >> 1;
			TItem titem = this._nodes[i];
			if (this.HasHigherPriority(titem, node))
			{
				return;
			}
			this._nodes[node.QueueIndex] = titem;
			titem.QueueIndex = node.QueueIndex;
			node.QueueIndex = i;
			while (i > 1)
			{
				i >>= 1;
				TItem titem2 = this._nodes[i];
				if (this.HasHigherPriority(titem2, node))
				{
					break;
				}
				this._nodes[node.QueueIndex] = titem2;
				titem2.QueueIndex = node.QueueIndex;
				node.QueueIndex = i;
			}
			this._nodes[node.QueueIndex] = node;
		}

		private void CascadeDown(TItem node)
		{
			int num = node.QueueIndex;
			int num2 = 2 * num;
			if (num2 > this._numNodes)
			{
				return;
			}
			int num3 = num2 + 1;
			TItem titem = this._nodes[num2];
			if (this.HasHigherPriority(titem, node))
			{
				if (num3 > this._numNodes)
				{
					node.QueueIndex = num2;
					titem.QueueIndex = num;
					this._nodes[num] = titem;
					this._nodes[num2] = node;
					return;
				}
				TItem titem2 = this._nodes[num3];
				if (this.HasHigherPriority(titem, titem2))
				{
					titem.QueueIndex = num;
					this._nodes[num] = titem;
					num = num2;
				}
				else
				{
					titem2.QueueIndex = num;
					this._nodes[num] = titem2;
					num = num3;
				}
			}
			else
			{
				if (num3 > this._numNodes)
				{
					return;
				}
				TItem titem3 = this._nodes[num3];
				if (!this.HasHigherPriority(titem3, node))
				{
					return;
				}
				titem3.QueueIndex = num;
				this._nodes[num] = titem3;
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
				titem = this._nodes[num2];
				if (this.HasHigherPriority(titem, node))
				{
					if (num3 > this._numNodes)
					{
						goto Block_9;
					}
					TItem titem4 = this._nodes[num3];
					if (this.HasHigherPriority(titem, titem4))
					{
						titem.QueueIndex = num;
						this._nodes[num] = titem;
						num = num2;
					}
					else
					{
						titem4.QueueIndex = num;
						this._nodes[num] = titem4;
						num = num3;
					}
				}
				else
				{
					if (num3 > this._numNodes)
					{
						goto Block_11;
					}
					TItem titem5 = this._nodes[num3];
					if (!this.HasHigherPriority(titem5, node))
					{
						goto IL_28F;
					}
					titem5.QueueIndex = num;
					this._nodes[num] = titem5;
					num = num3;
				}
			}
			node.QueueIndex = num;
			this._nodes[num] = node;
			return;
			Block_9:
			node.QueueIndex = num2;
			titem.QueueIndex = num;
			this._nodes[num] = titem;
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

		private bool HasHigherPriority(TItem higher, TItem lower)
		{
			int num = this._comparer(higher.Priority, lower.Priority);
			return num < 0 || (num == 0 && higher.InsertionIndex < lower.InsertionIndex);
		}

		public TItem Dequeue()
		{
			TItem result = this._nodes[1];
			if (this._numNodes == 1)
			{
				this._nodes[1] = (TItem)((object)null);
				this._numNodes = 0;
				return result;
			}
			TItem titem = this._nodes[this._numNodes];
			this._nodes[1] = titem;
			titem.QueueIndex = 1;
			this._nodes[this._numNodes] = (TItem)((object)null);
			this._numNodes--;
			this.CascadeDown(titem);
			return result;
		}

		public void Resize(int maxNodes)
		{
			TItem[] array = new TItem[maxNodes + 1];
			int num = Math.Min(maxNodes, this._numNodes);
			Array.Copy(this._nodes, array, num + 1);
			this._nodes = array;
		}

		public TItem First
		{
			get
			{
				return this._nodes[1];
			}
		}

		public void UpdatePriority(TItem node, TPriority priority)
		{
			node.Priority = priority;
			this.OnNodeUpdated(node);
		}

		private void OnNodeUpdated(TItem node)
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

		public void Remove(TItem node)
		{
			if (node.QueueIndex == this._numNodes)
			{
				this._nodes[this._numNodes] = (TItem)((object)null);
				this._numNodes--;
				return;
			}
			TItem titem = this._nodes[this._numNodes];
			this._nodes[node.QueueIndex] = titem;
			titem.QueueIndex = node.QueueIndex;
			this._nodes[this._numNodes] = (TItem)((object)null);
			this._numNodes--;
			this.OnNodeUpdated(titem);
		}

		public void ResetNode(TItem node)
		{
			node.QueueIndex = 0;
		}

		public IEnumerator<TItem> GetEnumerator()
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

		private TItem[] _nodes;

		private long _numNodesEverEnqueued;

		private readonly Comparison<TPriority> _comparer;
	}
}
