using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Ships
{
	public class ShipQueue
	{
		public void Clear()
		{
			this._shipQueue.Clear();
			this._priorityQueue.Clear();
		}

		public int Count()
		{
			return this._shipQueue.Count;
		}

		public IEnumerable<Ship> Dequeue()
		{
			if (this._shipQueue == null || this._priorityQueue == null || this._shipQueue.Count == 0)
			{
				return null;
			}
			List<Ship> list = new List<Ship>();
			for (int i = this._priorityQueue.Count - 1; i >= 0; i--)
			{
				list.Add(this._priorityQueue[i]);
				this._priorityQueue.RemoveAt(i);
			}
			list.Add(this._shipQueue.Dequeue());
			return list.Distinct<Ship>();
		}

		public void Enqueue(Ship ship)
		{
			if (!this._shipQueue.Contains(ship))
			{
				this._shipQueue.Enqueue(ship);
			}
		}

		public void Fill(IEnumerable<Ship> ships)
		{
			if (ships == null)
			{
				return;
			}
			this._shipQueue.Clear();
			this._priorityQueue.Clear();
			foreach (Ship item in ships)
			{
				this._shipQueue.Enqueue(item);
			}
		}

		public void PrioritizeShip(Ship priorityShip)
		{
			if (priorityShip == null || this._priorityQueue.Contains(priorityShip))
			{
				return;
			}
			this._priorityQueue.Add(priorityShip);
		}

		public void UnregisterShip(Ship shipToUnregister)
		{
			this._shipQueue = new Queue<Ship>(from x in this._shipQueue
			where x != shipToUnregister
			select x);
			this._priorityQueue = new List<Ship>(from x in this._priorityQueue
			where x != shipToUnregister
			select x);
		}

		private Queue<Ship> _shipQueue = new Queue<Ship>();

		private List<Ship> _priorityQueue = new List<Ship>();
	}
}
