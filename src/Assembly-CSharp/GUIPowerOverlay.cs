using System;
using UnityEngine;

public class GUIPowerOverlay : MonoBehaviour
{
	private void Awake()
	{
		this.go = (UnityEngine.Object.Instantiate(Resources.Load("prefabQuadPowerUI"), base.transform) as GameObject);
		this.mrBG = this.go.transform.Find("bmpBG").GetComponent<MeshRenderer>();
		this.mrBG.sharedMaterial = DataHandler.GetMaterial(this.mrBG, "GUIBlack16", "blank", "blank", "blank");
		this.tBG = this.go.transform.Find("bmpBG");
		this.tBar = this.go.transform.Find("bmpBG/bmpBar");
		this.mrBar = this.tBar.GetComponent<MeshRenderer>();
		this.tPower = this.go.transform.Find("bmpPower");
		this.mrPower = this.tPower.GetComponent<MeshRenderer>();
		this.mrPower.sharedMaterial = DataHandler.GetMaterial(this.mrPower, "IcoPowerOff", "blank", "blank", "blank");
		this.CO = base.gameObject.GetComponent<CondOwner>();
		this.tBG.gameObject.SetActive(this.bPowered);
		this.Set(0f, null);
	}

	public void Set(float fPower, JsonPowerInfo jsonPI)
	{
		bool flag = this.CO.HasCond("IsPowered") || fPower > 0f;
		bool flag2 = !this.CO.HasCond("IsOff");
		if (flag)
		{
			if (flag2 != this.bOn)
			{
				this.mrBar.sharedMaterial = DataHandler.GetMaterial(this.mrBar, "GUIGreen16", "blank", "blank", "blank");
			}
			this.bOn = flag2;
			this.bPowered = true;
			this.tBar.localScale = new Vector3(this.tBar.localScale.x, fPower, this.tBar.localScale.z);
			this.tBG.gameObject.SetActive(fPower > 0f);
		}
		else if (flag != this.bPowered)
		{
			this.tBG.gameObject.SetActive(false);
			this.mrBar.sharedMaterial = DataHandler.GetMaterial(this.mrBar, "GUIBlack16", "blank", "blank", "blank");
			this.bOn = false;
			this.bPowered = false;
		}
		if (!flag && jsonPI != null && jsonPI.aInputPts != null && jsonPI.aInputPts.Length > 0)
		{
			this.tPower.gameObject.SetActive(true);
		}
		else
		{
			this.tPower.gameObject.SetActive(false);
		}
	}

	public bool Hide
	{
		get
		{
			return this.mrBG.enabled;
		}
		set
		{
			this.mrBG.enabled = !value;
			this.mrBar.enabled = !value;
			this.mrPower.enabled = !value;
		}
	}

	public void AlignInput(Vector3 vPos, Vector3 vScale)
	{
		vPos.z = this.tPower.position.z;
		this.tPower.position = vPos;
		this.tPower.localScale = vScale;
	}

	private MeshRenderer mrBG;

	private MeshRenderer mrBar;

	private MeshRenderer mrPower;

	private Transform tBar;

	private Transform tBG;

	private Transform tPower;

	private GameObject go;

	private bool bPowered;

	private bool bOn;

	private CondOwner CO;
}
