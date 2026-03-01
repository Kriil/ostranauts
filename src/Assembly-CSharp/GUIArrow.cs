using System;
using UnityEngine;
using UnityEngine.UI;

public class GUIArrow : MonoBehaviour
{
	private void Awake()
	{
		Image component = base.gameObject.GetComponent<Image>();
		component.color = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.value * 0.5f + 0.5f, UnityEngine.Random.value * 0.5f + 0.5f);
		GameObject original = Resources.Load<GameObject>("DataEdit/btnYellow");
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original);
		GameObject gameObject2 = GameObject.Find("Canvas Arrows");
		this.btnUnlink = gameObject.GetComponent<Button>();
		this.btnUnlink.transform.Find("lblText").GetComponent<Text>().text = "Unlink";
		this.btnUnlink.transform.SetParent(gameObject2.transform);
		this.btnUnlink.onClick.AddListener(delegate()
		{
			this.Unlink();
		});
	}

	private void Update()
	{
		this.Redraw();
	}

	public void Unlink()
	{
		this.objNodeOrigin.RemoveArrow(this, false);
		this.objNodeDest.RemoveArrow(this, true);
		this.Delete();
	}

	public void Delete()
	{
		UnityEngine.Object.Destroy(this.btnUnlink.gameObject);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void SetNodes(NodeInteraction objOrigin, NodeInteraction objDest)
	{
		this.objNodeOrigin = objOrigin;
		this.objNodeDest = objDest;
		this.Redraw();
	}

	public void Redraw()
	{
		Vector3 vector = this.objNodeDest.transform.position - this.objNodeOrigin.transform.position;
		float num = Vector3.Angle(Vector3.up, vector);
		if (vector.x < 0f)
		{
			num = -num;
		}
		base.transform.localScale = new Vector3(2f, vector.magnitude / ((RectTransform)base.transform).rect.height, 1f);
		base.transform.rotation = Quaternion.AngleAxis(num, Vector3.back);
		base.transform.position = new Vector3(this.objNodeOrigin.transform.position.x, this.objNodeOrigin.transform.position.y - 300f, this.objNodeOrigin.transform.position.z + 10f);
		this.btnUnlink.transform.position = base.transform.position + vector / 2f;
	}

	public string DestName
	{
		get
		{
			return this.objNodeDest.txtName.text;
		}
	}

	private NodeInteraction objNodeOrigin;

	private NodeInteraction objNodeDest;

	private Button btnUnlink;
}
