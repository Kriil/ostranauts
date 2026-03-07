using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIStationRow : MonoBehaviour
{
	private void Awake()
	{
		this.txtService = base.transform.Find("pnlService/txtService").GetComponent<TMP_Text>();
		this.txtPrice = base.transform.Find("txtPrice").GetComponent<TMP_Text>();
		this.txtInv = base.transform.Find("txtInv").GetComponent<TMP_Text>();
		this.txtBlank = base.transform.Find("txtBlank").GetComponent<TMP_Text>();
		this.txtPurchased = base.transform.Find("txtPurchased").GetComponent<TMP_Text>();
		this.txtOnBoard = base.transform.Find("txtOnBoard").GetComponent<TMP_Text>();
		this.txtMax = base.transform.Find("txtMax").GetComponent<TMP_Text>();
		this.txtTotal = base.transform.Find("pnlTotal/txtTotal").GetComponent<TMP_Text>();
		this.slider = base.transform.Find("Slider").GetComponent<Slider>();
		this.slider.onValueChanged.AddListener(delegate(float A_1)
		{
			this.OnSliderChanged();
		});
	}

	private void Update()
	{
	}

	public void Init(int nType, string strService, float fTotal, float fPrice = 0f)
	{
		this.nType = nType;
		this.strService = strService;
		this.fPrice = fPrice;
		this.fInv = 0f;
		this.fPurchased = 0f;
		this.fOnBoard = 0f;
		this.fMax = 0f;
		this.fTotal = fTotal;
		this.bSlider = false;
		this.txtService.text = strService;
		if (fPrice != 0f)
		{
			this.txtPrice.text = fPrice.ToString("#.00");
		}
		else
		{
			this.txtPrice.text = string.Empty;
		}
		this.txtInv.text = string.Empty;
		this.txtPurchased.text = string.Empty;
		this.txtOnBoard.text = string.Empty;
		this.txtMax.text = string.Empty;
		this.txtTotal.text = fTotal.ToString("#.00");
		this.txtBlank.gameObject.SetActive(true);
		this.slider.gameObject.SetActive(false);
	}

	public void Init(string strService, float fPrice, float fInv, float fPurchased, float fOnBoard, float fMax, float fTotal, Action fn = null)
	{
		this.nType = 0;
		this.strService = strService;
		this.fPrice = fPrice;
		this.fInv = fInv;
		this.fPurchased = fPurchased;
		this.fOnBoard = fOnBoard;
		this.fMax = fMax;
		this.fTotal = fTotal;
		this.bSlider = true;
		this.txtService.text = strService;
		this.txtPrice.text = fPrice.ToString("#.00");
		this.txtInv.text = fInv.ToString("#.00");
		this.txtPurchased.text = fPurchased.ToString("#.00");
		this.txtOnBoard.text = fOnBoard.ToString("#.00");
		this.txtMax.text = fMax.ToString("#.00");
		this.txtTotal.text = fTotal.ToString("#.00");
		if (fOnBoard > fMax)
		{
			this.bSlider = false;
		}
		if (this.bSlider)
		{
			this.txtBlank.gameObject.SetActive(false);
			this.slider.gameObject.SetActive(true);
			float num = fOnBoard / fMax;
			if (float.IsNaN(num))
			{
				this.slider.value = 0f;
				this.slider.maxValue = 0f;
			}
			else
			{
				this.slider.value = num;
				this.slider.maxValue = 1f;
			}
			if (fn != null)
			{
				this.slider.onValueChanged.AddListener(delegate(float A_1)
				{
					fn();
				});
			}
		}
		else
		{
			this.txtBlank.gameObject.SetActive(true);
			this.slider.gameObject.SetActive(false);
		}
	}

	public void SetColor(Color color)
	{
		this.txtService.color = color;
		this.txtPrice.color = color;
		this.txtInv.color = color;
		this.txtPurchased.color = color;
		this.txtOnBoard.color = color;
		this.txtMax.color = color;
		this.txtTotal.color = color;
	}

	private void OnSliderChanged()
	{
		if (this.slider.value < this.fOnBoard / this.fMax)
		{
			this.slider.value = this.fOnBoard / this.fMax;
		}
		switch (this.nType)
		{
		case 0:
			this.fPurchased = this.fMax * this.slider.value - this.fOnBoard;
			this.fTotal = this.fPurchased * this.fPrice;
			break;
		case 1:
			this.fTotal = this.fPrice;
			break;
		}
		this.txtPurchased.text = this.fPurchased.ToString("#.00");
		this.txtTotal.text = this.fTotal.ToString("#.00");
		if (this.fTotal > this.fTotalLast)
		{
			GUIStationRefuel.bPlayAEUp = true;
		}
		else if (this.fTotal < this.fTotalLast)
		{
			GUIStationRefuel.bPlayAEDown = true;
		}
		this.fTotalLast = this.fTotal;
	}

	public const int TOTAL_UNITS = 0;

	public const int TOTAL_ONE = 1;

	public const int TOTAL_OTHER = 2;

	private TMP_Text txtService;

	private TMP_Text txtPrice;

	private TMP_Text txtInv;

	private TMP_Text txtBlank;

	private TMP_Text txtPurchased;

	private TMP_Text txtOnBoard;

	private TMP_Text txtMax;

	private TMP_Text txtTotal;

	private Slider slider;

	public int nType;

	public string strService;

	public float fPrice;

	public float fInv;

	public float fPurchased;

	public float fOnBoard;

	public float fMax;

	public float fTotal;

	public float fTotalLast;

	public bool bSlider;
}
