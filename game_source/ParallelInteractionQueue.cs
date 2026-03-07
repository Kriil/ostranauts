using System;
using System.Collections.Generic;
using UnityEngine;

public class ParallelInteractionQueue : MonoBehaviour
{
	public void Init(GameObject crewGO)
	{
		this.co = crewGO.GetComponent<CondOwner>();
		this.pf = crewGO.GetComponent<Pathfinder>();
		GameObject gameObject = new GameObject();
		gameObject.transform.SetParent(base.transform);
		gameObject.AddComponent<SphereCollider>();
		gameObject.GetComponent<SphereCollider>().radius = 5f;
		gameObject.transform.localPosition = Vector3.zero;
		this.SCollider = gameObject.GetComponent<SphereCollider>();
	}

	private void Update()
	{
	}

	public CondOwner co;

	public Pathfinder pf;

	public List<CondOwner> recentlyGreeted;

	public SphereCollider SCollider;
}
