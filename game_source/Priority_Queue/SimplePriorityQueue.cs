using System;
using System.Collections;
using System.Collections.Generic;

namespace Priority_Queue
{
	public class SimplePriorityQueue<TItem, TPriority> : IPriorityQueue<TItem, TPriority>, IEnumerable<TItem>, IEnumerable where TPriority : IComparable<TPriority>
	{
		public SimplePriorityQueue() : this(Comparer<TPriority>.Default, EqualityComparer<TItem>.Default)
		{
		}

		public SimplePriorityQueue(IComparer<TPriority> priorityComparer) : this(new Comparison<TPriority>(priorityComparer.Compare), EqualityComparer<TItem>.Default)
		{
		}

		public SimplePriorityQueue(Comparison<TPriority> priorityComparer) : this(priorityComparer, EqualityComparer<TItem>.Default)
		{
		}

		public SimplePriorityQueue(IEqualityComparer<TItem> itemEquality) : this(Comparer<TPriority>.Default, itemEquality)
		{
		}

		public SimplePriorityQueue(IComparer<TPriority> priorityComparer, IEqualityComparer<TItem> itemEquality) : this(new Comparison<TPriority>(priorityComparer.Compare), itemEquality)
		{
		}

		public SimplePriorityQueue(Comparison<TPriority> priorityComparer, IEqualityComparer<TItem> itemEquality)
		{
			this._queue = new GenericPriorityQueue<SimplePriorityQueue<TItem, TPriority>.SimpleNode, TPriority>(10, priorityComparer);
			this._itemToNodesCache = new Dictionary<TItem, IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode>>(itemEquality);
			this._nullNodesCache = new List<SimplePriorityQueue<TItem, TPriority>.SimpleNode>();
		}

		private SimplePriorityQueue<TItem, TPriority>.SimpleNode GetExistingNode(TItem item)
		{
			if (item == null)
			{
				return (this._nullNodesCache.Count <= 0) ? null : this._nullNodesCache[0];
			}
			IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> list;
			if (!this._itemToNodesCache.TryGetValue(item, out list))
			{
				return null;
			}
			return list[0];
		}

		private void AddToNodeCache(SimplePriorityQueue<TItem, TPriority>.SimpleNode node)
		{
			if (node.Data == null)
			{
				this._nullNodesCache.Add(node);
				return;
			}
			IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> list;
			if (!this._itemToNodesCache.TryGetValue(node.Data, out list))
			{
				list = new List<SimplePriorityQueue<TItem, TPriority>.SimpleNode>();
				this._itemToNodesCache[node.Data] = list;
			}
			list.Add(node);
		}

		private void RemoveFromNodeCache(SimplePriorityQueue<TItem, TPriority>.SimpleNode node)
		{
			if (node.Data == null)
			{
				this._nullNodesCache.Remove(node);
				return;
			}
			IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> list;
			if (!this._itemToNodesCache.TryGetValue(node.Data, out list))
			{
				return;
			}
			list.Remove(node);
			if (list.Count == 0)
			{
				this._itemToNodesCache.Remove(node.Data);
			}
		}

		public int Count
		{
			get
			{
				object queue = this._queue;
				int count;
				lock (queue)
				{
					count = this._queue.Count;
				}
				return count;
			}
		}

		public TItem First
		{
			get
			{
				object queue = this._queue;
				TItem data;
				lock (queue)
				{
					if (this._queue.Count <= 0)
					{
						throw new InvalidOperationException("Cannot call .First on an empty queue");
					}
					data = this._queue.First.Data;
				}
				return data;
			}
		}

		public void Clear()
		{
			object queue = this._queue;
			lock (queue)
			{
				this._queue.Clear();
				this._itemToNodesCache.Clear();
				this._nullNodesCache.Clear();
			}
		}

		public bool Contains(TItem item)
		{
			object queue = this._queue;
			bool result;
			lock (queue)
			{
				result = ((item != null) ? this._itemToNodesCache.ContainsKey(item) : (this._nullNodesCache.Count > 0));
			}
			return result;
		}

		public TItem Dequeue()
		{
			object queue = this._queue;
			TItem data;
			lock (queue)
			{
				if (this._queue.Count <= 0)
				{
					throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
				}
				SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode = this._queue.Dequeue();
				this.RemoveFromNodeCache(simpleNode);
				data = simpleNode.Data;
			}
			return data;
		}

		private SimplePriorityQueue<TItem, TPriority>.SimpleNode EnqueueNoLockOrCache(TItem item, TPriority priority)
		{
			SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode = new SimplePriorityQueue<TItem, TPriority>.SimpleNode(item);
			if (this._queue.Count == this._queue.MaxSize)
			{
				this._queue.Resize(this._queue.MaxSize * 2 + 1);
			}
			this._queue.Enqueue(simpleNode, priority);
			return simpleNode;
		}

		public void Enqueue(TItem item, TPriority priority)
		{
			object queue = this._queue;
			lock (queue)
			{
				IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> list;
				if (item == null)
				{
					list = this._nullNodesCache;
				}
				else if (!this._itemToNodesCache.TryGetValue(item, out list))
				{
					list = new List<SimplePriorityQueue<TItem, TPriority>.SimpleNode>();
					this._itemToNodesCache[item] = list;
				}
				SimplePriorityQueue<TItem, TPriority>.SimpleNode item2 = this.EnqueueNoLockOrCache(item, priority);
				list.Add(item2);
			}
		}

		public bool EnqueueWithoutDuplicates(TItem item, TPriority priority)
		{
			object queue = this._queue;
			bool result;
			lock (queue)
			{
				IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> list;
				if (item == null)
				{
					if (this._nullNodesCache.Count > 0)
					{
						return false;
					}
					list = this._nullNodesCache;
				}
				else
				{
					if (this._itemToNodesCache.ContainsKey(item))
					{
						return false;
					}
					list = new List<SimplePriorityQueue<TItem, TPriority>.SimpleNode>();
					this._itemToNodesCache[item] = list;
				}
				SimplePriorityQueue<TItem, TPriority>.SimpleNode item2 = this.EnqueueNoLockOrCache(item, priority);
				list.Add(item2);
				result = true;
			}
			return result;
		}

		public void Remove(TItem item)
		{
			object queue = this._queue;
			lock (queue)
			{
				SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode;
				IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> nullNodesCache;
				if (item == null)
				{
					if (this._nullNodesCache.Count == 0)
					{
						throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + item);
					}
					simpleNode = this._nullNodesCache[0];
					nullNodesCache = this._nullNodesCache;
				}
				else
				{
					if (!this._itemToNodesCache.TryGetValue(item, out nullNodesCache))
					{
						throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + item);
					}
					simpleNode = nullNodesCache[0];
					if (nullNodesCache.Count == 1)
					{
						this._itemToNodesCache.Remove(item);
					}
				}
				this._queue.Remove(simpleNode);
				nullNodesCache.Remove(simpleNode);
			}
		}

		public void UpdatePriority(TItem item, TPriority priority)
		{
			object queue = this._queue;
			lock (queue)
			{
				SimplePriorityQueue<TItem, TPriority>.SimpleNode existingNode = this.GetExistingNode(item);
				if (existingNode == null)
				{
					throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + item);
				}
				this._queue.UpdatePriority(existingNode, priority);
			}
		}

		public TPriority GetPriority(TItem item)
		{
			object queue = this._queue;
			TPriority priority;
			lock (queue)
			{
				SimplePriorityQueue<TItem, TPriority>.SimpleNode existingNode = this.GetExistingNode(item);
				if (existingNode == null)
				{
					throw new InvalidOperationException("Cannot call GetPriority() on a node which is not enqueued: " + item);
				}
				priority = existingNode.Priority;
			}
			return priority;
		}

		public bool TryFirst(out TItem first)
		{
			if (this._queue.Count > 0)
			{
				object queue = this._queue;
				lock (queue)
				{
					if (this._queue.Count > 0)
					{
						first = this._queue.First.Data;
						return true;
					}
				}
			}
			first = default(TItem);
			return false;
		}

		public bool TryDequeue(out TItem first)
		{
			if (this._queue.Count > 0)
			{
				object queue = this._queue;
				lock (queue)
				{
					if (this._queue.Count > 0)
					{
						SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode = this._queue.Dequeue();
						first = simpleNode.Data;
						this.RemoveFromNodeCache(simpleNode);
						return true;
					}
				}
			}
			first = default(TItem);
			return false;
		}

		public bool TryRemove(TItem item)
		{
			object queue = this._queue;
			bool result;
			lock (queue)
			{
				SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode;
				IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> nullNodesCache;
				if (item == null)
				{
					if (this._nullNodesCache.Count == 0)
					{
						return false;
					}
					simpleNode = this._nullNodesCache[0];
					nullNodesCache = this._nullNodesCache;
				}
				else
				{
					if (!this._itemToNodesCache.TryGetValue(item, out nullNodesCache))
					{
						return false;
					}
					simpleNode = nullNodesCache[0];
					if (nullNodesCache.Count == 1)
					{
						this._itemToNodesCache.Remove(item);
					}
				}
				this._queue.Remove(simpleNode);
				nullNodesCache.Remove(simpleNode);
				result = true;
			}
			return result;
		}

		public bool TryUpdatePriority(TItem item, TPriority priority)
		{
			object queue = this._queue;
			bool result;
			lock (queue)
			{
				SimplePriorityQueue<TItem, TPriority>.SimpleNode existingNode = this.GetExistingNode(item);
				if (existingNode == null)
				{
					result = false;
				}
				else
				{
					this._queue.UpdatePriority(existingNode, priority);
					result = true;
				}
			}
			return result;
		}

		public bool TryGetPriority(TItem item, out TPriority priority)
		{
			object queue = this._queue;
			bool result;
			lock (queue)
			{
				SimplePriorityQueue<TItem, TPriority>.SimpleNode existingNode = this.GetExistingNode(item);
				if (existingNode == null)
				{
					priority = default(TPriority);
					result = false;
				}
				else
				{
					priority = existingNode.Priority;
					result = true;
				}
			}
			return result;
		}

		public IEnumerator<TItem> GetEnumerator()
		{
			List<TItem> list = new List<TItem>();
			object queue = this._queue;
			lock (queue)
			{
				foreach (SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode in this._queue)
				{
					list.Add(simpleNode.Data);
				}
			}
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public bool IsValidQueue()
		{
			object queue = this._queue;
			bool result;
			lock (queue)
			{
				foreach (IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> list in this._itemToNodesCache.Values)
				{
					foreach (SimplePriorityQueue<TItem, TPriority>.SimpleNode node in list)
					{
						if (!this._queue.Contains(node))
						{
							return false;
						}
					}
				}
				foreach (SimplePriorityQueue<TItem, TPriority>.SimpleNode simpleNode in this._queue)
				{
					if (this.GetExistingNode(simpleNode.Data) == null)
					{
						return false;
					}
				}
				result = this._queue.IsValidQueue();
			}
			return result;
		}

		private const int INITIAL_QUEUE_SIZE = 10;

		private readonly GenericPriorityQueue<SimplePriorityQueue<TItem, TPriority>.SimpleNode, TPriority> _queue;

		private readonly Dictionary<TItem, IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode>> _itemToNodesCache;

		private readonly IList<SimplePriorityQueue<TItem, TPriority>.SimpleNode> _nullNodesCache;

		private class SimpleNode : GenericPriorityQueueNode<TPriority>
		{
			public SimpleNode(TItem data)
			{
				this.Data = data;
			}

			public TItem Data { get; private set; }
		}
	}
}
