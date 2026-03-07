using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class PhotoMode : MonoBehaviour
{
	private void Start()
	{
		PhotoMode.instance = this;
	}

	private void Update()
	{
		if (GUIActionKeySelector.commandToggleFog.Down)
		{
			if (this.m_PDA == null)
			{
				this.LinkUp();
			}
			this.TogglePhotoMode(!this.m_status);
		}
		if (GUIActionKeySelector.commandScreenshot.Down)
		{
			this.Screenshot();
		}
	}

	public void TogglePhotoMode(bool toggle)
	{
		this.m_status = toggle;
		this.m_MainCam.HideLoS = toggle;
		if (this.m_HideUIOnPhotoMode)
		{
			this.m_CanvasStack.SetActive(!toggle);
			this.m_PDA.GetComponent<GUIPDA>().State = GUIPDA.UIState.Tasks;
			this.m_PDA.GetComponent<GUIPDA>().State = GUIPDA.UIState.Closed;
		}
	}

	public static void ToggleAO()
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_AOStatus = !PhotoMode.instance.m_AOStatus;
		PhotoMode.instance.m_MainCam.ShowAO = PhotoMode.instance.m_AOStatus;
	}

	public static void SetAO(bool value)
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_AOStatus = value;
		PhotoMode.instance.m_MainCam.ShowAO = value;
	}

	public static void ToggleAOZoom()
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_AOZoom = !PhotoMode.instance.m_AOZoom;
		PhotoMode.instance.m_MainCam.ZoomAO = PhotoMode.instance.m_AOZoom;
	}

	public static void AOSpread(float spread)
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_MainCam.AOZoomBase = spread;
	}

	public static void AOIntensity(float intensity)
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_MainCam.AOIntensity = intensity;
	}

	public static void ToggleFog()
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_fogStatus = !PhotoMode.instance.m_fogStatus;
		PhotoMode.instance.m_MainCam.HideLoS = PhotoMode.instance.m_fogStatus;
	}

	public static void SetFog(bool value)
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_fogStatus = !value;
		PhotoMode.instance.m_MainCam.HideLoS = !value;
	}

	public static void SetLights(bool value)
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_lightsStatus = value;
		PhotoMode.instance.m_MainCam.UseLighting = value;
	}

	public static bool GetFog()
	{
		return !(PhotoMode.instance == null) && !PhotoMode.instance.m_fogStatus;
	}

	public static void ToggleUI()
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		PhotoMode.instance.m_UIstatus = !PhotoMode.instance.m_UIstatus;
		if (PhotoMode.instance.m_PDA == null)
		{
			PhotoMode.instance.LinkUp();
		}
		if (PhotoMode.instance.m_UIstatus)
		{
			CanvasManager.instance.CrewSimNormal();
		}
		else
		{
			CanvasManager.instance.HideUI();
		}
		PhotoMode.instance.m_PDA.GetComponent<GUIPDA>().State = GUIPDA.UIState.Tasks;
		PhotoMode.instance.m_PDA.GetComponent<GUIPDA>().State = GUIPDA.UIState.Closed;
	}

	public static void SetParallax(bool value)
	{
		if (PhotoMode.instance == null)
		{
			return;
		}
		if (PhotoMode.instance.m_MainCam == null)
		{
			PhotoMode.instance.LinkUp();
		}
		PhotoMode.instance.m_MainCam.ShowParallax = value;
	}

	public void Screenshot()
	{
		Directory.CreateDirectory(Application.persistentDataPath + "/Screenshots");
		string text = "Screenshot" + DateTime.Now.ToString(" yyyy-MM-dd HHmmss") + ".png";
		string text2 = Application.persistentDataPath + "/Screenshots/" + text;
		Application.CaptureScreenshot(text2);
		base.StartCoroutine(this.ValidateSave(text2, text, 100));
	}

	private IEnumerator ValidateSave(string fileName, string cleanName, int attempts)
	{
		bool isSaved = false;
		for (int i = 0; i < attempts; i++)
		{
			if (File.Exists(fileName) && !isSaved)
			{
				isSaved = true;
				if (CrewSim.objInstance != null && CrewSim.coPlayer != null)
				{
					CrewSim.coPlayer.LogMessage("Screenshot taken: " + cleanName, "Neutral", "Game");
				}
				i = attempts;
			}
			yield return new WaitForSeconds(0.1f);
		}
		yield return null;
		yield break;
	}

	private void LinkUp()
	{
		this.m_PDA = GameObject.Find("GUIPDA2");
		this.m_CanvasStack = GameObject.Find("Canvas Stack");
		this.m_MainCam = GameObject.Find("Main Camera").GetComponent<GameRenderer>();
	}

	public static PhotoMode instance;

	private bool m_status;

	public bool m_HideUIOnPhotoMode = true;

	public GameObject m_PDA;

	public GameObject m_CanvasStack;

	public GameRenderer m_MainCam;

	private bool m_UIstatus = true;

	private bool m_fogStatus;

	private bool m_lightsStatus = true;

	private bool m_AOStatus = true;

	private bool m_AOZoom = true;
}
