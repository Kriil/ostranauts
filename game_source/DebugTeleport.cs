using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DebugTeleport : MonoBehaviour
{
	private void Start()
	{
		this.button = base.GetComponent<Button>();
		this.button.onClick.AddListener(new UnityAction(this.StartMouseTeleport));
		GameObject gameObject = new GameObject("targeter");
		gameObject.transform.SetParent(base.transform.parent);
		this.targeter = gameObject.AddComponent<RectTransform>();
		gameObject.AddComponent<RawImage>();
		gameObject.GetComponent<RawImage>().texture = this.image;
		this.targeter.sizeDelta = new Vector2(1f, 1f);
		gameObject.AddComponent<CanvasGroup>();
		gameObject.GetComponent<CanvasGroup>().ignoreParentGroups = true;
		gameObject.SetActive(false);
		gameObject.layer = LayerMask.NameToLayer("UI");
	}

	public void StartMouseTeleport()
	{
		this.teleport = true;
		this.targeter.gameObject.SetActive(true);
	}

	private void Update()
	{
		if (this.teleport && Input.GetMouseButtonDown(0) && CrewSim.GetSelectedCrew() != null)
		{
			CanvasManager.HideCanvasGroup(CanvasManager.instance.goCanvasDebug);
			Vector3 vector = CrewSim.objInstance.camMain.ScreenToWorldPoint(Input.mousePosition);
			CrewSim.GetSelectedCrew().tf.position = new Vector3(vector.x, vector.y);
			this.teleport = false;
			this.targeter.gameObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		Vector2 v;
		if (this.teleport && RectTransformUtility.ScreenPointToLocalPointInRectangle(this.targeter.parent.transform as RectTransform, Input.mousePosition, CrewSim.objInstance.UICamera, out v))
		{
			this.targeter.localPosition = v;
		}
	}

	private Button button;

	private bool teleport;

	public RectTransform targeter;

	public Texture2D image;
}
