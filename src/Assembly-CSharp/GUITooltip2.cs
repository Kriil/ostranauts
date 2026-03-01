using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GUITooltip2 : MonoBehaviour
{
	private void Awake()
	{
		if (GUITooltip2.m_instance == null)
		{
			GUITooltip2.m_instance = this;
		}
		else if (GUITooltip2.m_instance != this)
		{
			UnityEngine.Object.Destroy(base.transform.gameObject);
		}
		this.m_rt = base.GetComponent<RectTransform>();
		this.m_title = base.transform.Find("txtTitle").GetComponent<TMP_Text>();
		this.m_body = base.transform.Find("txtBody").GetComponent<TMP_Text>();
		this.m_subtitle = base.transform.Find("txtSubtitle").GetComponent<TMP_Text>();
		GUITooltip2.SetToolTip("Uh oh!", "You shouldn't be able to see this...", false);
	}

	private void Start()
	{
		CrewSim.RefreshTooltipEvent.AddListener(new UnityAction(this.CloseTooltip));
	}

	private void OnDestroy()
	{
		CrewSim.RefreshTooltipEvent.RemoveListener(new UnityAction(this.CloseTooltip));
	}

	private void Update()
	{
		float num = 720f / (float)Screen.height;
		this.m_rt.anchoredPosition = new Vector2(Input.mousePosition.x * num, Input.mousePosition.y * num);
		if (Input.mousePosition.x > (float)Screen.width / 2f)
		{
			this.pivots.x = 1f;
		}
		else
		{
			this.pivots.x = 0f;
		}
		if (Input.mousePosition.y > (float)Screen.height / 2f)
		{
			this.pivots.y = 1f;
		}
		else
		{
			this.pivots.y = 0f;
		}
		this.m_rt.pivot = this.pivots;
	}

	private void CloseTooltip()
	{
		GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
	}

	private static string AddLineBreaks(string strBody)
	{
		if (string.IsNullOrEmpty(strBody) || strBody.Length <= 50)
		{
			return strBody;
		}
		string[] array = strBody.Split(new char[]
		{
			' '
		});
		StringBuilder stringBuilder = new StringBuilder();
		int num = 1;
		foreach (string text in array)
		{
			if (text.Contains("\n"))
			{
				num++;
			}
			stringBuilder.Append(text + " ");
			if (stringBuilder.Length >= 45 * num)
			{
				stringBuilder.AppendLine();
				num++;
			}
		}
		return stringBuilder.ToString();
	}

	public static void SetToolTip(string strTitle, string strBody, bool show = true)
	{
		if (GUITooltip2.m_instance == null)
		{
			return;
		}
		GUITooltip2.m_instance.m_subtitle.enabled = false;
		if (show)
		{
			GUITooltip2.m_instance.m_subtitle.text = string.Empty;
			GUITooltip2.m_instance.m_title.text = strTitle;
			GUITooltip2.m_instance.m_body.text = GUITooltip2.AddLineBreaks(strBody);
			GUITooltip2.m_instance.transform.parent.GetComponent<CanvasGroup>().alpha = 1f;
		}
		else if (GUITooltip2.m_instance.m_title.text == strTitle || (strTitle == string.Empty && strBody == string.Empty))
		{
			GUITooltip2.m_instance.m_subtitle.text = string.Empty;
			GUITooltip2.m_instance.m_title.text = string.Empty;
			GUITooltip2.m_instance.m_body.text = string.Empty;
			GUITooltip2.m_instance.transform.parent.GetComponent<CanvasGroup>().alpha = 0f;
		}
	}

	public static void SetToolTip_1(string strSubtitle, string strTitle, string strBody, bool show = true)
	{
		if (GUITooltip2.m_instance == null)
		{
			return;
		}
		GUITooltip2.m_instance.m_subtitle.enabled = true;
		if (show)
		{
			GUITooltip2.m_instance.m_subtitle.text = strSubtitle;
			GUITooltip2.m_instance.m_title.text = strTitle;
			GUITooltip2.m_instance.m_body.text = strBody;
			GUITooltip2.m_instance.transform.parent.GetComponent<CanvasGroup>().alpha = 1f;
		}
		else if (GUITooltip2.m_instance.m_title.text == strTitle || (strTitle == string.Empty && strBody == string.Empty))
		{
			GUITooltip2.m_instance.m_subtitle.text = string.Empty;
			GUITooltip2.m_instance.m_title.text = string.Empty;
			GUITooltip2.m_instance.m_body.text = string.Empty;
			GUITooltip2.m_instance.transform.parent.GetComponent<CanvasGroup>().alpha = 0f;
		}
	}

	public static GUITooltip2 m_instance;

	public RectTransform m_rt;

	public TMP_Text m_title;

	public TMP_Text m_body;

	public TMP_Text m_subtitle;

	private Vector2 pivots = new Vector2(0f, 1f);
}
