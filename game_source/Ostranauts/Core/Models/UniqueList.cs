using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Core.Models
{
	public class UniqueList<T> : IEnumerable<T>, IEnumerable
	{
		public UniqueList()
		{
			this._hashSet = new HashSet<T>();
			this._list = new List<T>();
		}

		public int Count
		{
			get
			{
				return this._hashSet.Count;
			}
		}

		public void Clear()
		{
			this._hashSet.Clear();
			this._list.Clear();
		}

		public bool Contains(T item)
		{
			return this._hashSet.Contains(item);
		}

		public void Add(T item)
		{
			if (this._hashSet.Add(item))
			{
				this._list.Add(item);
			}
		}

		public void AddRange(IEnumerable<T> aItms)
		{
			if (aItms == null)
			{
				return;
			}
			foreach (T item in aItms)
			{
				if (this._hashSet.Add(item))
				{
					this._list.Add(item);
				}
			}
		}

		public bool Remove(T item)
		{
			if (this._hashSet.Remove(item))
			{
				this._list.Remove(item);
				return true;
			}
			return false;
		}

		public T FirstOrDefault()
		{
			return this._list.FirstOrDefault<T>();
		}

		public override string ToString()
		{
			return "Count=" + this._list.Count;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this._list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this._list.GetEnumerator();
		}

		private HashSet<T> _hashSet;

		private List<T> _list;
	}
}
