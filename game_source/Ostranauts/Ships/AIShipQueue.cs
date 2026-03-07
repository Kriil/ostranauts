using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots;

namespace Ostranauts.Ships
{
	public class AIShipQueue
	{
		public void Clear()
		{
			this._aIShipQueue.Clear();
			this._priorityQueue.Clear();
		}

		public int Count()
		{
			return this._aIShipQueue.Count;
		}

		public IEnumerable<AIShip> Dequeue()
		{
			if (this._aIShipQueue == null || this._priorityQueue == null)
			{
				return null;
			}
			List<AIShip> list = new List<AIShip>();
			for (int i = this._priorityQueue.Count - 1; i >= 0; i--)
			{
				list.Add(this._priorityQueue[i]);
				if ((AIType.PriorityShips & this._priorityQueue[i].AIType) != this._priorityQueue[i].AIType)
				{
					this._priorityQueue.RemoveAt(i);
				}
			}
			if (this._aIShipQueue.Count != 0)
			{
				list.Add(this._aIShipQueue.Dequeue());
			}
			return list.Distinct<AIShip>();
		}

		public void Enqueue(AIShip ship)
		{
			if (!this._aIShipQueue.Contains(ship))
			{
				this._aIShipQueue.Enqueue(ship);
			}
		}

		public void Fill(IEnumerable<AIShip> ships)
		{
			if (ships == null)
			{
				return;
			}
			this._aIShipQueue.Clear();
			this._priorityQueue.Clear();
			foreach (AIShip aiship in ships)
			{
				if ((AIType.NonPriorityShips & aiship.AIType) == aiship.AIType)
				{
					this._aIShipQueue.Enqueue(aiship);
				}
				else
				{
					this.PrioritizeShip(aiship);
				}
			}
		}

		public void PrioritizeShip(AIShip priorityShip)
		{
			if (priorityShip == null || this._priorityQueue.Contains(priorityShip))
			{
				return;
			}
			this._priorityQueue.Add(priorityShip);
		}

		public void UnregisterShip(AIShip shipToUnregister)
		{
			this._aIShipQueue = new Queue<AIShip>(from x in this._aIShipQueue
			where x != shipToUnregister
			select x);
			this._priorityQueue = new List<AIShip>(from x in this._priorityQueue
			where x != shipToUnregister
			select x);
		}

		private Queue<AIShip> _aIShipQueue = new Queue<AIShip>();

		private List<AIShip> _priorityQueue = new List<AIShip>();
	}
}
