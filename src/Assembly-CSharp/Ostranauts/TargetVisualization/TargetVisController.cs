using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using UnityEngine;

namespace Ostranauts.TargetVisualization
{
	public class TargetVisController : MonoSingleton<TargetVisController>
	{
		public void UpdateTargetVis(CondOwner co, List<Interaction> aQueue)
		{
			if (co == null || aQueue == null)
			{
				return;
			}
			Interaction interaction = null;
			int num = 0;
			if (num < aQueue.Count)
			{
				if (aQueue[num] == null || aQueue[num].attackMode == null)
				{
					return;
				}
				interaction = aQueue[num];
			}
			GameObject gameObject;
			if (this._lineDict.TryGetValue(co, out gameObject))
			{
				if (interaction != null && !(gameObject == null))
				{
					gameObject.GetComponent<TargetLine>().SetData(interaction);
					return;
				}
				if (gameObject != null)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
				this._lineDict.Remove(co);
			}
			else if (interaction != null)
			{
				TargetLine component = UnityEngine.Object.Instantiate<GameObject>(this._targetLinePrefab, base.transform).GetComponent<TargetLine>();
				this._lineDict.Add(co, component.gameObject);
				component.SetData(interaction);
			}
		}

		public void ClearTargetVis()
		{
			foreach (CondOwner key in this._lineDict.Keys.ToArray<CondOwner>())
			{
				GameObject gameObject;
				if (this._lineDict.TryGetValue(key, out gameObject))
				{
					if (gameObject != null)
					{
						UnityEngine.Object.Destroy(gameObject);
					}
					this._lineDict.Remove(key);
				}
			}
			this._lineDict.Clear();
		}

		public GameObject _targetLinePrefab;

		private readonly Dictionary<CondOwner, GameObject> _lineDict = new Dictionary<CondOwner, GameObject>();
	}
}
