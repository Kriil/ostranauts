using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUISocialItemAnimator : MonoBehaviour
{
	private void Awake()
	{
		this.prefabImage = (Resources.Load("prefabGUIItemPickup") as GameObject);
	}

	public void SpendSocialItemAnimation(string text, string strIMG)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabImage, base.transform);
		gameObject.transform.GetChild(1).GetComponent<RawImage>().texture = DataHandler.LoadPNG(strIMG + ".png", false, false);
		gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = text;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.position = this.portrait.position;
		component.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 72f);
		component.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 60f);
		base.StartCoroutine("AnimateToCentre", component);
	}

	public void SpawnSocialItemAnimation(string text, string strIMG)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabImage, base.transform);
		gameObject.transform.GetChild(1).GetComponent<RawImage>().texture = DataHandler.LoadPNG(strIMG + ".png", false, false);
		gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = text;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.localPosition = CrewSim.objInstance.camMain.WorldToScreenPoint(CrewSim.coPlayer.tf.position) - new Vector3((float)(CrewSim.resolutionX / 2), (float)(CrewSim.resolutionY / 2), 0f);
		component.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 72f);
		component.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 60f);
		base.StartCoroutine("AnimateToPortrait", component);
	}

	public IEnumerator AnimateToCentre(RectTransform rect)
	{
		float distance = 1f;
		CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0f;
		while (canvasGroup.alpha < 1f)
		{
			canvasGroup.alpha += Time.deltaTime / 0.25f;
			yield return null;
		}
		float time = 0f;
		while (time < 0.1f)
		{
			time += Time.deltaTime;
			yield return null;
		}
		while (distance > 0.01f)
		{
			rect.position += (CanvasManager.instance.goCanvasFloaties.transform.position - rect.position) * 0.15f;
			distance = Vector3.Distance(rect.position, CanvasManager.instance.goCanvasFloaties.transform.position);
			yield return null;
		}
		rect.position = CanvasManager.instance.goCanvasFloaties.transform.position;
		time = 0f;
		while (time < 0.1f)
		{
			time += Time.deltaTime;
			yield return null;
		}
		base.StartCoroutine("Fade", canvasGroup);
		yield return null;
		yield break;
	}

	public IEnumerator AnimateToPortrait(RectTransform rect)
	{
		float distance = 1f;
		CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0f;
		while (canvasGroup.alpha < 1f)
		{
			canvasGroup.alpha += Time.deltaTime / 0.25f;
			yield return null;
		}
		float time = 0f;
		while (time < 0.1f)
		{
			time += Time.deltaTime;
			yield return null;
		}
		while (distance > 0.01f)
		{
			rect.position += (this.portrait.position - rect.position) * 0.15f;
			distance = Vector3.Distance(rect.position, this.portrait.position);
			yield return null;
		}
		rect.position = this.portrait.position;
		time = 0f;
		while (time < 0.1f)
		{
			time += Time.deltaTime;
			yield return null;
		}
		base.StartCoroutine("Fade", canvasGroup);
		yield return null;
		yield break;
	}

	public IEnumerator Fade(CanvasGroup cg)
	{
		while (cg.alpha > 0f)
		{
			cg.alpha -= Time.deltaTime;
			yield return null;
		}
		UnityEngine.Object.Destroy(cg.gameObject);
		yield break;
	}

	public GameObject prefabImage;

	public RectTransform portrait;
}
