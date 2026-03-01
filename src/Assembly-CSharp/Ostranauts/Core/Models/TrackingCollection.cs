using System;
using System.Collections;
using System.Collections.Generic;

namespace Ostranauts.Core.Models
{
	public class TrackingCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable
	{
		public TrackingCollection()
		{
		}

		public TrackingCollection(Action<bool> updateCallback)
		{
			this._updateCallback = updateCallback;
		}

		public int Count
		{
			get
			{
				this._count = this._collection.Count;
				return this._count;
			}
			private set
			{
				this._count = value;
			}
		}

		public bool IsReadOnly { get; private set; }

		public bool Any { get; private set; }

		public TrackingCollection<T>.Enumerator GetEnumerator()
		{
			return new TrackingCollection<T>.Enumerator(this);
		}

		IEnumerator<T> IEnumerable<!0>.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public void Add(T item)
		{
			this._collection.Add(item);
			this.Any = true;
			this.Update();
		}

		public void Clear()
		{
			this._collection.Clear();
		}

		public bool Contains(T item)
		{
			return this._collection.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			this._collection.CopyTo(array, arrayIndex);
		}

		public int IndexOf(T item)
		{
			return this._collection.IndexOf(item);
		}

		public T this[int index]
		{
			get
			{
				return this._collection[index];
			}
		}

		public bool Remove(T item)
		{
			if (this._collection.Remove(item))
			{
				if (this.Count <= 0)
				{
					this.Any = false;
					this.Update();
				}
				return true;
			}
			return false;
		}

		public void Track(Action<bool> updateCallback)
		{
			if (updateCallback == null)
			{
				return;
			}
			this._updateCallback = updateCallback;
		}

		private void Update()
		{
			if (this._updateCallback == null || !this.Any)
			{
				return;
			}
			this._updateCallback(this.Any);
		}

		private int _count;

		private List<T> _collection = new List<T>();

		private Action<bool> _updateCallback;

		public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			internal Enumerator(TrackingCollection<T> collection)
			{
				this._collection = collection;
				this._index = -1;
				this._current = default(T);
			}

			public bool MoveNext()
			{
				if (++this._index < this._collection._collection.Count)
				{
					this._current = this._collection._collection[this._index];
					return true;
				}
				this._current = default(T);
				return false;
			}

			public T Current
			{
				get
				{
					return this._current;
				}
			}

			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}

			public void Dispose()
			{
			}

			public void Reset()
			{
				this._index = -1;
				this._current = default(T);
			}

			private readonly TrackingCollection<T> _collection;

			private int _index;

			private T _current;
		}
	}
}
