using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Components
{
	public class DontDestroyOnLoad : MonoBehaviour
	{
		private void Awake()
		{
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			if (CrewSim.OnGameEnd == null)
			{
				CrewSim.OnGameEnd = new UnityEvent();
			}
			CrewSim.OnGameEnd.AddListener(new UnityAction(this.RemoveSelf));
		}

		private void OnDestroy()
		{
			if (CrewSim.OnGameEnd != null)
			{
				CrewSim.OnGameEnd.RemoveListener(new UnityAction(this.RemoveSelf));
			}
		}

		private void RemoveSelf()
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}
