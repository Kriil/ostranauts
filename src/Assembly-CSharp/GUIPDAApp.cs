using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIPDAApp : MonoBehaviour
{
	public void UpdateInfo(JsonPDAAppIcon info)
	{
		if (this._tooltippable2 == null)
		{
			this._tooltippable2 = base.gameObject.GetComponentInChildren<Tooltippable2>();
		}
		this.strName = info.strName;
		this.m_txtName.text = info.strFriendlyName;
		this.m_icon = DataHandler.LoadPNG(info.strIcon + ".png", false, false);
		this.m_icon.filterMode = FilterMode.Bilinear;
		this.m_appIcon.sprite = Sprite.Create(this.m_icon, new Rect(0f, 0f, (float)this.m_icon.width, (float)this.m_icon.height), new Vector2(0.5f, 0.5f));
		this.m_appIcon.color = DataHandler.GetColor("AppIcon");
		this.m_notifIcon.texture = DataHandler.LoadPNG("IcoNotificationBlank.png", false, false);
		this.m_notifIcon.color = DataHandler.GetColor("AppNotif");
		this.m_notifIcon.texture.filterMode = FilterMode.Bilinear;
		this.SetToolTip(this.strName);
		AudioManager.AddBtnAudio(base.gameObject, null, "ShipUIBtnPDAClick02");
	}

	private void SetToolTip(string appName)
	{
		if (this._tooltippable2 == null || string.IsNullOrEmpty(appName))
		{
			return;
		}
		this._tooltippable2.SetData("GUI_PDA_BUTTON_" + appName.ToUpper() + "_TITLE", "GUI_PDA_BUTTON_" + appName.ToUpper(), true);
	}

	public void UpdateNotifs(int iAmount)
	{
		if (iAmount <= 0)
		{
			this.m_notifIcon.gameObject.SetActive(false);
		}
		else if (iAmount >= 100)
		{
			this.m_notifIcon.gameObject.SetActive(true);
			this.m_txtNotif.text = "99+";
		}
		else
		{
			this.m_notifIcon.gameObject.SetActive(true);
			this.m_txtNotif.text = iAmount.ToString();
		}
	}

	public void Toggle()
	{
		Button component = this.m_appIcon.GetComponent<Button>();
		component.OnPointerExit(null);
		GUIPDA.OpenApp(this.strName);
	}

	public string strName;

	public Image m_appIcon;

	public TextMeshProUGUI m_txtName;

	public Texture2D m_icon;

	public RawImage m_notifIcon;

	public TextMeshProUGUI m_txtNotif;

	[SerializeField]
	private Tooltippable2 _tooltippable2;
}
