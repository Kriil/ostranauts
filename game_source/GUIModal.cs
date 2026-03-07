using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIModal : MonoBehaviour
{
	private void Awake()
	{
		GUIModal.objInstance = this;
		GUIModal.tfBody = base.transform.Find("pnlBody");
		GUIModal.tfTooltip = base.transform.Find("pnlTooltip");
		this.goTooltipClick = GUIModal.tfTooltip.Find("txtClick").gameObject;
		this.camMain = GameObject.Find("Main Camera").GetComponent<Camera>();
		this.btnExit = base.transform.Find("pnlBody/btnExit").GetComponent<Button>();
		this.btnExit.onClick.AddListener(delegate()
		{
			this.HideAll();
		});
		this.txtTitle = base.transform.Find("pnlBody/txtTitle").GetComponent<TMP_Text>();
		this.txtText = base.transform.Find("pnlBody/pnlText/txtMain").GetComponent<TMP_Text>();
		this.txtTooltip = base.transform.Find("pnlTooltip/txtTooltip").GetComponent<TMP_Text>();
		this.HideAll();
	}

	private void Update()
	{
	}

	public void ShowTooltip(bool bRightClick)
	{
		if (GUIModal.tfBody.gameObject.activeInHierarchy)
		{
			return;
		}
		GUIModal.tfTooltip.gameObject.SetActive(true);
		Vector3 mousePosition = Input.mousePosition;
		mousePosition.x += 20f;
		mousePosition.y -= 10f;
		GUIModal.tfTooltip.position = mousePosition;
		this.goTooltipClick.SetActive(bRightClick);
	}

	public void ShowModal()
	{
		GUIModal.tfTooltip.gameObject.SetActive(false);
		GUIModal.tfBody.gameObject.SetActive(true);
	}

	public void Hide()
	{
		if (GUIModal.tfBody.gameObject.activeInHierarchy)
		{
			return;
		}
		GUIModal.tfTooltip.gameObject.SetActive(false);
	}

	private void HideAll()
	{
		GUIModal.tfTooltip.gameObject.SetActive(false);
		GUIModal.tfBody.gameObject.SetActive(false);
	}

	public void SetText(string strTitle = "", string strText = "")
	{
		if (strTitle == null)
		{
			strTitle = string.Empty;
		}
		if (strText == null)
		{
			strText = string.Empty;
		}
		this.txtTitle.text = strTitle;
		this.txtText.text = strText;
	}

	public void SetTooltip(string strText = "")
	{
		if (strText == null)
		{
			strText = string.Empty;
		}
		this.txtTooltip.text = strText;
	}

	public string GetText()
	{
		return this.txtText.text;
	}

	public string GetTooltip()
	{
		return this.txtTooltip.text;
	}

	public static GUIModal Instance
	{
		get
		{
			return GUIModal.objInstance;
		}
	}

	private Camera camMain;

	private Button btnExit;

	private TMP_Text txtTitle;

	private TMP_Text txtText;

	private TMP_Text txtTooltip;

	private GameObject goTooltipClick;

	private string strTitle = "Title";

	private string strText = "Body text";

	private static GUIModal objInstance;

	private static Transform tfBody;

	private static Transform tfTooltip;
}
