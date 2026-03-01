using System;
using UnityEngine;

public class SpacedObject : MonoBehaviour
{
	private void Start()
	{
	}

	public void Init(CondOwner objSpaced, CondOwner airlock)
	{
		CondTrigger condTrigger = new CondTrigger();
		condTrigger.aReqs = new string[]
		{
			"IsDockSys"
		};
		this.airlock = CrewSim.shipCurrentLoaded.GetICOs1(condTrigger, false, false, true)[0];
		base.transform.position = this.airlock.tf.position;
		this.objSpotlight = CrewSim.objInstance.MakeGenericVisibility(DataHandler.GetLight("Wall1x0Blue"), false);
		Debug.Log(this.objSpotlight);
		this.directionOfSunNorm = new Vector3(0.5f, 0.5f);
		this.objSpotlight.transform.position = base.transform.position + this.directionOfSunNorm * 3f;
		this.emissionStrength = 0.08f;
		this.directionOfEmissionNorm = new Vector3(0f, 1f);
		objSpaced.AICancelAll(null);
		objSpaced.QueueInteraction(objSpaced, DataHandler.GetInteraction("ACTSpaceSelf", null, false), false);
		this.coSpaced = objSpaced;
		this.coSpaced.AddCondAmount("IsSpaced", 1.0, 0.0, 0f);
		this.rotationAmount = new Vector3(0f, 0f, UnityEngine.Random.Range(0.5f, 2f));
	}

	private void Update()
	{
		this.objSpotlight.GetChild(0).GetComponent<Visibility>().bRedraw = true;
		base.transform.Rotate(this.rotationAmount);
		base.transform.position += this.directionOfEmissionNorm * this.emissionStrength;
		while ((double)this.emissionStrength > 0.05)
		{
			this.emissionStrength -= Time.deltaTime * 0.01f;
		}
		this.objSpotlight.position = base.transform.position + this.directionOfSunNorm * 8f;
	}

	public Transform objSpotlight;

	public CondOwner coSpaced;

	public ParticleSystem gasEmission;

	public Vector3 directionOfEmissionNorm;

	public Vector3 directionOfSunNorm;

	public CondOwner airlock;

	public Vector3 rotationAmount;

	public float emissionStrength;
}
