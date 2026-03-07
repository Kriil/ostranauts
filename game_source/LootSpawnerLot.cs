using System;
using UnityEngine;

public class LootSpawnerLot : MonoBehaviour
{
	private void Awake()
	{
		this.ls = base.GetComponent<LootSpawner>();
	}

	public void Update()
	{
		if (this.ls == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		CondOwner component = base.gameObject.GetComponent<CondOwner>();
		if (component.ship != null)
		{
			this.ls.DoLoot(component.ship);
			component.RemoveFromCurrentHome(true);
			component.Destroy();
		}
	}

	private LootSpawner ls;
}
