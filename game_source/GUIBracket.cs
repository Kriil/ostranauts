using System;
using UnityEngine;

public class GUIBracket : MonoBehaviour
{
	private void Awake()
	{
		MeshRenderer component = base.gameObject.GetComponent<MeshRenderer>();
		component.material.SetTexture("_MainTex", DataHandler.LoadPNG("GUIBracket01.png", false, false));
	}

	private void Update()
	{
		if (this.coTarget != null)
		{
			base.gameObject.transform.position = new Vector3(this.coTarget.transform.position.x, this.coTarget.transform.position.y, base.gameObject.transform.position.z);
		}
	}

	public bool SetTarget(string strID)
	{
		if (strID == null)
		{
			this.coTarget = null;
		}
		else
		{
			if (!DataHandler.mapCOs.ContainsKey(strID))
			{
				Debug.Log("Cannot select object ID: " + strID);
				return false;
			}
			CondOwner condOwner = DataHandler.mapCOs[strID];
			this.coTarget = condOwner;
		}
		return true;
	}

	public CondOwner coTarget;
}
