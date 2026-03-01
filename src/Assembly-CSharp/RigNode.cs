using System;
using UnityEngine;

public class RigNode : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		if (base.gameObject.name.LastIndexOf("L") == base.gameObject.name.Length - 1)
		{
			Gizmos.color = Color.green;
		}
		else if (base.gameObject.name.LastIndexOf("R") == base.gameObject.name.Length - 1)
		{
			Gizmos.color = Color.red;
		}
		Vector3 position = base.transform.parent.position;
		Vector3 b = new Vector3(0.05f, 0f, 0f);
		Vector3 b2 = new Vector3(0f, 0.05f, 0f);
		Vector3 b3 = new Vector3(0f, 0f, 0.05f);
		if (base.transform.parent != null)
		{
			Gizmos.DrawLine(base.transform.position + b, position);
			Gizmos.DrawLine(base.transform.position + b2, position);
			Gizmos.DrawLine(base.transform.position + b3, position);
		}
	}

	public Color lineColor = Color.red;
}
