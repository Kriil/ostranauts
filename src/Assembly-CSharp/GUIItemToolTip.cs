using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIItemToolTip : MonoBehaviour
{
	private void Start()
	{
		this.m_background.color = DataHandler.GetColor("ToolTipBg");
		this.m_icon.material = UnityEngine.Object.Instantiate<Material>(this.m_icon.material);
		this.m_condHealthBar.fillColor = "DamageBarFill";
		this.m_condHealthBar.bgColor = "DamageBarBg";
		this.m_condHealthBar.numerator = "StatDamage";
		this.m_condHealthBar.denominator = "StatDamageMax";
		this.m_condHealthBar2.fillColor = "PowerBarFill";
		this.m_condHealthBar2.bgColor = "PowerBarBg";
		this.m_condHealthBar2.numerator = "StatPower";
		this.m_condHealthBar2.denominator = "StatPowerMax";
		this.m_condHealthBar2.m_flip = false;
	}

	public void SetCondOwner(CondOwner co)
	{
		if (co != null)
		{
			this.m_txtName.text = co.FriendlyName;
		}
		else
		{
			this.m_txtName.text = string.Empty;
		}
		this.m_condHealthBar.condOwner = co;
		this.m_condHealthBar2.condOwner = co;
		Powered component = co.GetComponent<Powered>();
		if (co.HasCond("IsPowerObservable"))
		{
			double num = 0.0;
			double num2 = 0.0;
			num2 += co.GetCondAmount("StatPowerMax") * co.GetDamageState();
			num += co.GetCondAmount("StatPower");
			if (co.GetComponent<Container>() != null)
			{
				List<CondOwner> cos = co.GetComponent<Container>().GetCOs(true, null);
				if (cos != null && cos.Count > 0)
				{
					foreach (CondOwner condOwner in cos)
					{
						if (condOwner != null)
						{
							num2 += condOwner.GetCondAmount("StatPowerMax") * condOwner.GetDamageState();
							num += condOwner.GetCondAmount("StatPower");
						}
					}
				}
			}
			this.m_condHealthBar2.numAmount = Convert.ToSingle(num);
			this.m_condHealthBar2.denAmount = Convert.ToSingle(num2);
			this.m_condHealthBar2.gameObject.SetActive(true);
		}
		else if (component != null)
		{
			this.m_condHealthBar2.numAmount = Convert.ToSingle(co.GetCondAmount("StatPower"));
			this.m_condHealthBar2.denAmount = Convert.ToSingle(component.PowerStoredMax);
			this.m_condHealthBar2.gameObject.SetActive(true);
		}
		else
		{
			this.m_condHealthBar2.gameObject.SetActive(false);
		}
		Texture2D texture2D = DataHandler.LoadPNG(co.strPortraitImg + ".png", false, false);
		this.m_icon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 1f));
	}

	public CondHealthBar m_condHealthBar;

	public CondHealthBar m_condHealthBar2;

	public Image m_icon;

	public Image m_background;

	public TextMeshProUGUI m_txtName;
}
