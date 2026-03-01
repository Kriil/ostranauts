using System;
using System.Collections;
using System.Collections.Generic;
using SFB;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CanvasSampleOpenFileTextMultiple : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	public void OnPointerDown(PointerEventData eventData)
	{
	}

	private void Start()
	{
		Button component = base.GetComponent<Button>();
		component.onClick.AddListener(new UnityAction(this.OnClick));
	}

	private void OnClick()
	{
		string[] array = StandaloneFileBrowser.OpenFilePanel("Open File", string.Empty, string.Empty, true);
		if (array.Length > 0)
		{
			List<string> list = new List<string>(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				list.Add(new Uri(array[i]).AbsoluteUri);
			}
			base.StartCoroutine(this.OutputRoutine(list.ToArray()));
		}
	}

	private IEnumerator OutputRoutine(string[] urlArr)
	{
		string outputText = string.Empty;
		for (int i = 0; i < urlArr.Length; i++)
		{
			WWW loader = new WWW(urlArr[i]);
			yield return loader;
			outputText += loader.text;
		}
		this.output.text = outputText;
		yield break;
	}

	public Text output;
}
