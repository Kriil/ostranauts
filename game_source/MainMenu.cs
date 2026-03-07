using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ostranauts.Core;
using Ostranauts.UI.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu controller and front-end bootstrap.
// This scene appears to initialize DataHandler, audio, loading widgets, and
// the title-screen UI before handing off to save/load or new-game flows.
public class MainMenu : MonoSingleton<MainMenu>
{
	// Enforces single-instance behavior, then wires volume updates and the bug console.
	private new void Awake()
	{
		base.Awake();
		if (!this.EnsureSingleInstance())
		{
			Debug.LogError("WARNING: Ostranauts is already running in another instance. Shutting this instance down.");
			Application.Quit();
		}
		AudioManager.AudioVolumeUpdated.AddListener(new UnityAction(this.OnVolumeSet));
		ConsoleToGUI component = GameObject.Find("BugConsole").GetComponent<ConsoleToGUI>();
		component.Init();
	}

	// Unity start hook for menu initialization after the singleton is ready.
	private void Start()
	{
		CrewSim.bShipEditTest = false;
		CrewSim.bShipEdit = false;
		this.Init();
	}

	// Front-end update loop.
	// Handles menu ambience, camera drift, loading state, and input once
	// DataHandler has finished its asynchronous startup load.
	private void Update()
	{
		this.UpdateLoadWidget();
		if (!MainMenu.bMixerStarted)
		{
			AudioManager.am.MixerSet(AudioManager.MixerSnap.EVERYTHING, 0.25f);
			MainMenu.bMixerStarted = true;
		}
		if (this.vLastMouse != Input.mousePosition)
		{
			this.fTimeElapsed = 0f;
		}
		this.vLastMouse = Input.mousePosition;
		this.fTimeElapsed += Time.deltaTime;
		if (this.fTimeElapsed > this.fLoopTime)
		{
			this.RestartLoop();
			this.fTimeElapsed = 0f;
			return;
		}
		this.fTimerLogo -= Time.deltaTime;
		if (this.cgBBG.alpha <= 0f)
		{
			this.fTimerLogo = 0f;
		}
		if (this.fTimerLogo <= 0f && !this.bATCPlaying)
		{
			this.StartAudio();
		}
		if (this.cgBlack.alpha <= 0f && this.goBBGSplash.activeInHierarchy)
		{
			this.goBBGSplash.SetActive(false);
		}
		this.ShiftCamera();
		float num;
		for (num = this.tfFlicker.localPosition.y + this.fFlickerRate; num > 128f; num -= 256f)
		{
		}
		this.tfFlicker.localPosition = new Vector3(0f, num, 0f);
		this.fLineTimeLeft -= Time.deltaTime;
		if (this.fLineTimeLeft < 0f)
		{
			this.fLineTimeLeft = this.fLinePeriod;
			this.tfShipEditLineH.localPosition = new Vector3(0f, (float)UnityEngine.Random.Range(-64, 65), 0f);
			this.tfShipEditLineV.localPosition = new Vector3((float)UnityEngine.Random.Range(-64, 65), 0f, 0f);
		}
		if (!DataHandler.bLoaded)
		{
			return;
		}
		this.BlinkHandler();
		this.KeyHandler();
		this.MouseHandler();
	}

	// Cleans up listeners and debug output when leaving the menu.
	private void OnDestroy()
	{
		if (this._outfs != null)
		{
			this._outfs.Close();
		}
		AudioManager.AudioVolumeUpdated.RemoveListener(new UnityAction(this.OnVolumeSet));
		base.StopAllCoroutines();
	}

	// Likely a placeholder for a loading indicator refresh path.
	private void UpdateLoadWidget()
	{
	}

	// Builds the title screen, applies video prefs, and kicks off data loading.
	// DataHandler.Init ultimately feeds the registries used by both the menu and CrewSim.
	private void Init()
	{
		QualitySettings.SetQualityLevel(5);
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
		if (!PlayerPrefs.HasKey("fVolMaster"))
		{
			Resolution resolution = default(Resolution);
			foreach (Resolution resolution2 in Screen.resolutions)
			{
				float num = (float)resolution2.width / (float)resolution2.height;
				if ((double)num >= 1.55 && (double)num <= 1.8)
				{
					if (resolution2.width > resolution.width || resolution2.height > resolution.height)
					{
						resolution = resolution2;
					}
				}
			}
			if (resolution.height == 0 || resolution.width == 0)
			{
				resolution.height = PlayerPrefs.GetInt("ResolutionHeight", 1080);
				resolution.width = PlayerPrefs.GetInt("ResolutionWidth", 1920);
			}
			int @int = PlayerPrefs.GetInt("FullScreen", 1);
			Screen.SetResolution(resolution.width, resolution.height, @int == 1);
		}
		DataHandler.Init();
		Texture2D texture = DataHandler.LoadPNG("GUICursor01.png", false, false);
		Cursor.SetCursor(texture, new Vector2(0f, 0f), CursorMode.Auto);
		TMP_Text component = base.transform.Find("Logo/txtVer").GetComponent<TMP_Text>();
		if (IntPtr.Size == 8)
		{
			component.text = DataHandler.strBuild + " (64)";
		}
		else
		{
			component.text = DataHandler.strBuild + " (32)";
		}
		this.dictTextures = new Dictionary<string, Texture2D[]>();
		this.dictTextures["btnContinue"] = new Texture2D[]
		{
			this.GetTexture("GUIBtnContinue.png"),
			this.GetTexture("GUIBtnContinueIn.png")
		};
		this.dictTextures["btnNew"] = new Texture2D[]
		{
			this.GetTexture("GUIbtnNew.png"),
			this.GetTexture("GUIbtnNewIn.png")
		};
		this.dictTextures["btnOptions"] = new Texture2D[]
		{
			this.GetTexture("GUIbtnOptions.png"),
			this.GetTexture("GUIbtnOptionsIn.png")
		};
		this.dictTextures["btnBBG"] = new Texture2D[]
		{
			this.GetTexture("GUIBtnBBG.png"),
			this.GetTexture("GUIBtnBBGIn.png")
		};
		this.dictTextures["btnSteam"] = new Texture2D[]
		{
			this.GetTexture("GUIBtnSteam.png"),
			this.GetTexture("GUIBtnSteamIn.png")
		};
		this.dictTextures["btnDiscord"] = new Texture2D[]
		{
			this.GetTexture("GUIBtnDiscord.png"),
			this.GetTexture("GUIBtnDiscordIn.png")
		};
		this.dictTextures["btnCredits"] = new Texture2D[]
		{
			this.GetTexture("GUIBtnCredits.png"),
			this.GetTexture("GUIBtnCreditsIn.png")
		};
		this.dictTextures["btnWiki"] = new Texture2D[]
		{
			this.GetTexture("GUIBtnWiki.png"),
			this.GetTexture("GUIBtnWikiIn.png")
		};
		this.dictTextures["btnExit"] = new Texture2D[]
		{
			this.GetTexture("GUIMainMenuExit.png"),
			this.GetTexture("GUIMainMenuExitOn.png")
		};
		this.ptHall = this.tfHall.transform.position;
		this.ptBG = this.tfBG.transform.position;
		this.ptRopes = this.tfRopes.transform.position;
		this.ptControlPanel = this.tfControlPanel.transform.position;
		this.BtnOutAll();
		GameObject original = Resources.Load("Audio/ATCChatter") as GameObject;
		this.atc1 = UnityEngine.Object.Instantiate<GameObject>(original, base.transform).GetComponent<AudioATC>();
		base.StartCoroutine(this.PlayLogoDelayed());
		if (!DataHandler.bLoaded)
		{
			base.StartCoroutine(this.FadeOutLoadWidget());
		}
		else if (this.cgLoading)
		{
			this.cgLoading.alpha = 0f;
		}
		if (!Info.instance && this._infoModalPrefab)
		{
			Info.instance = UnityEngine.Object.Instantiate<Info>(this._infoModalPrefab);
			this.cgInfo = Info.instance.canvas.GetComponentInChildren<CanvasGroup>();
		}
		Resources.UnloadUnusedAssets();
	}

	public IEnumerator FadeOutLoadWidget()
	{
		if (this.cgLoading.gameObject)
		{
			UnityEngine.Object.Destroy(this.cgLoading.gameObject);
		}
		yield return null;
		yield break;
	}

	private void OnVolumeSet()
	{
		this._volumesSet = true;
	}

	private IEnumerator PlayLogoDelayed()
	{
		yield return new WaitUntil(() => this._volumesSet);
		this.srcLogo.PlayDelayed(1f);
		yield break;
	}

	private bool EnsureSingleInstance()
	{
		string path = Application.persistentDataPath + "/OstraLock.txt";
		try
		{
			this._outfs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			this._outfs.Seek(0L, SeekOrigin.Begin);
			byte[] bytes = Encoding.UTF8.GetBytes("Locking file");
			byte[] bytes2 = BitConverter.GetBytes(bytes.Length);
			if (this._outfs.Length + (long)bytes.Length < 1048576L)
			{
				List<byte> list = new List<byte>();
				list.AddRange(bytes2);
				list.AddRange(bytes);
				byte[] array = list.ToArray();
				this._outfs.Write(array, 0, array.Length);
				return true;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("ERROR: " + ex.ToString());
			return false;
		}
		return false;
	}

	private void ShiftCamera()
	{
		Vector3 a = new Vector3(Input.mousePosition.x / (float)Screen.width, Input.mousePosition.y / (float)Screen.height, 0f);
		a.x -= 0.5f;
		a.y -= 0.5f;
		float num = 20f;
		a *= -num;
		a.x = Mathf.Clamp(a.x, -num / 2f, num / 2f);
		a.y = Mathf.Clamp(a.y, -num / 2f, num / 2f);
		this.tfHall.transform.position = new Vector3(this.ptHall.x + a.x * 0.25f, this.ptHall.y + a.y * 0.25f, this.ptHall.z);
		this.tfBG.transform.position = new Vector3(this.ptBG.x + a.x * 0.5f, this.ptBG.y + a.y * 0.5f, this.ptBG.z);
		this.tfRopes.transform.position = new Vector3(this.ptRopes.x + a.x * 0.75f, this.ptRopes.y + a.y * 0.75f, this.ptRopes.z);
		this.tfControlPanel.transform.position = new Vector3(this.ptControlPanel.x + a.x * 1f, this.ptControlPanel.y + a.y * 1f, this.ptControlPanel.z);
	}

	private void StartAudio()
	{
		this.atc1.Init(0.0, true);
		this.srcAmbientHum.Play();
		AudioManager.am.CueMusic("Josh Culler - Power On Sequence.ogg", 1f, 0f);
		this.bATCPlaying = true;
	}

	private void StopATCAudio()
	{
		this.atc1.Stop();
		this.srcAmbientHum.Stop();
		AudioManager.am.FadeOutMusic(0.6f);
		this.bATCPlaying = false;
	}

	private void OnButtonContinue()
	{
		this._loadingScreen = UnityEngine.Object.Instantiate<GameObject>(this._guiLoadingPrefab, this._canvasScreen);
	}

	private void NewGame()
	{
		CrewSim.bIsQuickstartSession = false;
		if (this._loadingstarted)
		{
			return;
		}
		this._loadingstarted = true;
		this.StartNewGame();
		this._loadingstarted = false;
	}

	private void StartNewGame()
	{
		CrewSim.OnFinishLoading.AddListener(delegate()
		{
			CrewSim.jsonShip = null;
			CrewSim.objInstance.NewGame(null);
		});
		SceneManager.LoadScene("Loading");
	}

	private static void OnButtonShipEdit()
	{
		CrewSim.OnFinishLoading.AddListener(delegate()
		{
			CrewSim.objInstance.StartShipEdit();
		});
		CrewSim.bShipEdit = true;
		SceneManager.LoadScene("Loading");
	}

	private void RestartLoop()
	{
		this.goBBGSplash.SetActive(true);
		this.srcLogo.PlayDelayed(1f);
		this.cgBlack.alpha = 1f;
		this.cgBlack.GetComponent<GUIPanelFade>().Reset(3f, 10f, false, true);
		this.cgBBG.alpha = 1f;
		this.cgBBG.GetComponent<GUIPanelFade>().Reset(3f, 8f, false, true);
		this.cgWarning.alpha = 1f;
		this.cgWarning.GetComponent<GUIPanelFade>().Reset(0.5f, 5f, false, true);
		this.StopATCAudio();
		this.fTimerLogo = 8f;
	}

	private Texture2D GetTexture(string strName)
	{
		Texture2D texture2D = DataHandler.LoadPNG(strName, false, false);
		texture2D.filterMode = FilterMode.Bilinear;
		return texture2D;
	}

	private void BtnOutAll()
	{
		this.SetButton(this.tfContinue, "btnContinue", 0);
		this.SetButton(this.tfNew, "btnNew", 0);
		this.SetButton(this.tfOptions, "btnOptions", 0);
		this.SetButton(this.tfBBG, "btnBBG", 0);
		this.SetButton(this.tfSteam, "btnSteam", 0);
		this.SetButton(this.tfDiscord, "btnDiscord", 0);
		this.SetButton(this.tfCredits, "btnCredits", 0);
		this.SetButton(this.tfWiki, "btnWiki", 0);
		this.SetButton(this.tfExit, "btnExit", 0);
		this.txtShipEditor.color = Color.white;
	}

	private void SetButton(Transform tf, string strBtn, int nIndex)
	{
		MeshRenderer component = tf.GetComponent<MeshRenderer>();
		Material material = component.material;
		material.SetTexture("_MainTex", this.dictTextures[strBtn][nIndex]);
	}

	private void MouseHandler()
	{
		if (this.cgWarning.alpha > 0f || this.cgBBG.alpha > 0f || this.cgBlack.alpha > 0f)
		{
			return;
		}
		if (this.cgManual.alpha > 0f || this.cgOptions.alpha > 0f || this.cgEAMessage.alpha > 0f || this._loadingScreen != null || !this.bClickable)
		{
			return;
		}
		if (this.nNoClickTimeLeft > 0 && Input.GetMouseButtonUp(0))
		{
			this.nNoClickTimeLeft--;
			return;
		}
		Ray ray = this.camMain.ScreenPointToRay(Input.mousePosition);
		RaycastHit raycastHit;
		bool flag = Physics.Raycast(ray, out raycastHit);
		Transform y = this.tfCurrentHit;
		this.tfCurrentHit = raycastHit.transform;
		if (raycastHit.transform == this.tfExit)
		{
			this.SetButton(this.tfExit, "btnExit", 1);
		}
		else if (!this.bBlink)
		{
			this.SetButton(this.tfExit, "btnExit", 0);
		}
		if (Input.GetMouseButtonDown(0) && flag && this.tfCurrentHit == y)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnNewGameIn", false, false);
		}
		if (Input.GetMouseButton(0))
		{
			this.BtnOutAll();
			if (flag)
			{
				if (this.tfCurrentHit != y)
				{
					AudioManager.am.PlayAudioEmitter("ShipUIBtnNewGameIn", false, false);
				}
				if (this.tfCurrentHit == this.tfContinue)
				{
					this.SetButton(this.tfContinue, "btnContinue", 1);
				}
				else if (this.tfCurrentHit == this.tfNew)
				{
					this.SetButton(this.tfNew, "btnNew", 1);
				}
				else if (this.tfCurrentHit == this.tfOptions)
				{
					this.SetButton(this.tfOptions, "btnOptions", 1);
				}
				else if (this.tfCurrentHit == this.tfBBG)
				{
					this.SetButton(this.tfBBG, "btnBBG", 1);
				}
				else if (this.tfCurrentHit == this.tfSteam)
				{
					this.SetButton(this.tfSteam, "btnSteam", 1);
				}
				else if (this.tfCurrentHit == this.tfDiscord)
				{
					this.SetButton(this.tfDiscord, "btnDiscord", 1);
				}
				else if (this.tfCurrentHit == this.tfCredits)
				{
					this.SetButton(this.tfCredits, "btnCredits", 1);
				}
				else if (this.tfCurrentHit == this.tfWiki)
				{
					this.SetButton(this.tfWiki, "btnWiki", 1);
				}
				else if (this.tfCurrentHit == this.tfShipEdit)
				{
					this.txtShipEditor.color = new Color(1f, 0.23529412f, 0f);
				}
			}
			else if (this.tfCurrentHit != y)
			{
				AudioManager.am.PlayAudioEmitter("ShipUIBtnLaunchOut", false, false);
			}
		}
		if (Input.GetMouseButtonUp(0))
		{
			this.BtnOutAll();
			if (!flag)
			{
				return;
			}
			AudioManager.am.PlayAudioEmitter("ShipUIBtnLaunchOut", false, false);
			if (this.tfCurrentHit == this.tfContinue)
			{
				this.cgEAMessage.GetComponent<GUIPAXIntro>().Show(new Action(this.OnButtonContinue));
			}
			else if (this.tfCurrentHit == this.tfNew)
			{
				this.cgEAMessage.GetComponent<GUIPAXIntro>().Show(new Action(this.NewGame));
			}
			else if (this.tfCurrentHit == this.tfOptions)
			{
				this.cgOptions.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, true, false);
				this.cgOptions.interactable = true;
				this.cgOptions.blocksRaycasts = true;
			}
			else if (this.tfCurrentHit == this.tfShipEdit)
			{
				MainMenu.OnButtonShipEdit();
			}
			else if (this.tfCurrentHit == this.tfBBG)
			{
				Application.OpenURL("https://bluebottlegames.com/forum");
			}
			else if (this.tfCurrentHit == this.tfSteam)
			{
				Application.OpenURL("https://steamcommunity.com/app/1022980/discussions/");
			}
			else if (this.tfCurrentHit == this.tfDiscord)
			{
				Application.OpenURL("https://discord.gg/PGfs6uJbMg");
			}
			else if (this.tfCurrentHit == this.tfCredits)
			{
				if (this.cgCredits.alpha == 0f)
				{
					this.cgCredits.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, true, false);
					this.cgCredits.interactable = true;
					this.cgCredits.blocksRaycasts = true;
				}
			}
			else if (this.tfCurrentHit == this.tfWiki)
			{
				Application.OpenURL("https://ostranauts.wiki.gg/wiki/Ostranauts_Wiki");
			}
			else if (this.tfCurrentHit == this.tfExit)
			{
				this.PopupQuitToDesktop();
			}
			else if (this.tfCurrentHit == this.tfManual && this.cgManual.alpha == 0f)
			{
				this.cgManual.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, true, false);
				this.cgManual.interactable = true;
				this.cgManual.blocksRaycasts = true;
			}
		}
	}

	private void PopupQuitToDesktop()
	{
		this.bClickable = false;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this._confirmationDialoguePrefab, this.tfQuit);
		Color clrBg = new Color(0.10980392f, 0.10980392f, 0.10980392f);
		Color clrFg = new Color(0.05882353f, 0.05882353f, 0.05882353f);
		Color clrFont = new Color(0.78431374f, 0.78431374f, 0.78431374f);
		gameObject.GetComponent<GUIConfirmationDialogue>().Setup("Are you sure you want to quit to desktop?", delegate()
		{
			Application.Quit();
		}, delegate()
		{
			this.ResetClickables();
		}, clrBg, clrFg, clrFont);
	}

	public void ResetClickables()
	{
		this.nNoClickTimeLeft = 1;
		this.bClickable = true;
	}

	private void KeyHandler()
	{
		if (CommandEscape.bApplicationQuitInProgress)
		{
			return;
		}
		if (Input.anyKeyDown)
		{
			if (this.cgBBG.alpha > 0f)
			{
				this.cgBBG.alpha = 0f;
				this.srcLogo.Stop();
				return;
			}
			if (this.cgWarning.alpha > 0f)
			{
				this.cgWarning.alpha = 0f;
				this.cgBlack.alpha = 0f;
				if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
				{
					this.nNoClickTimeLeft = 1;
				}
				return;
			}
		}
	}

	private void BlinkHandler()
	{
		this.fBlinkActual -= Time.deltaTime;
		if (this.fBlinkActual < 0f)
		{
			this.bBlink = !this.bBlink;
			if (this.bBlink)
			{
				this.fBlinkActual = MathUtils.Rand(0f, this.fBlinkCap, MathUtils.RandType.Flat, null);
				this.SetButton(this.tfExit, "btnExit", 1);
			}
			else
			{
				this.fBlinkActual = MathUtils.Rand(0f, this.fBlinkCap, MathUtils.RandType.Low, null);
				this.SetButton(this.tfExit, "btnExit", 0);
			}
		}
	}

	private const float LOGO_DURATION = 8f;

	[SerializeField]
	private GameObject _guiLoadingPrefab;

	[SerializeField]
	private GameObject _confirmationDialoguePrefab;

	[SerializeField]
	private Transform tfQuit;

	[SerializeField]
	private Transform _canvasScreen;

	[SerializeField]
	private Info _infoModalPrefab;

	public float fFlickerRate = 176f;

	public float fLinePeriod = 1f;

	public bool bClickable = true;

	[SerializeField]
	private Camera camMain;

	[SerializeField]
	private Transform tfHall;

	[SerializeField]
	private Transform tfRopes;

	[SerializeField]
	private Transform tfBG;

	[SerializeField]
	private Transform tfControlPanel;

	[SerializeField]
	private Transform tfNew;

	[SerializeField]
	private Transform tfContinue;

	[SerializeField]
	private Transform tfShipEdit;

	[SerializeField]
	private Transform tfOptions;

	[SerializeField]
	private Transform tfBBG;

	[SerializeField]
	private Transform tfSteam;

	[SerializeField]
	private Transform tfDiscord;

	[SerializeField]
	private Transform tfCredits;

	[SerializeField]
	private Transform tfExit;

	[SerializeField]
	private Transform tfWiki;

	[SerializeField]
	private Transform tfManual;

	[SerializeField]
	private Transform tfFlicker;

	[SerializeField]
	private Transform tfShipEditLineH;

	[SerializeField]
	private Transform tfShipEditLineV;

	[SerializeField]
	private CanvasGroup cgBlack;

	[SerializeField]
	private CanvasGroup cgBBG;

	[SerializeField]
	private CanvasGroup cgWarning;

	[SerializeField]
	private CanvasGroup cgEAMessage;

	[SerializeField]
	private CanvasGroup cgManual;

	[SerializeField]
	private CanvasGroup cgCredits;

	[SerializeField]
	private CanvasGroup cgOptions;

	[SerializeField]
	private CanvasGroup cgInfo;

	[SerializeField]
	private CanvasGroup cgLoading;

	[SerializeField]
	private Text txtShipEditor;

	[SerializeField]
	private GameObject goBBGSplash;

	private Transform tfCurrentHit;

	private Dictionary<string, Texture2D[]> dictTextures;

	private GameObject _loadingScreen;

	private AudioATC atc1;

	[SerializeField]
	private AudioSource srcLogo;

	[SerializeField]
	private AudioSource srcAmbientHum;

	private Vector3 ptHall;

	private Vector3 ptRopes;

	private Vector3 ptBG;

	private Vector3 ptControlPanel;

	private Vector3 vLastMouse;

	private float fTimeElapsed;

	private float fLoopTime = 120f;

	private float fLineTimeLeft;

	private int nNoClickTimeLeft;

	private FileStream _outfs;

	private static bool bDDNAStarted;

	private static bool bMixerStarted;

	public static bool bCueMenuMusic = true;

	private bool _loadingstarted;

	private bool bBlink;

	private float fBlinkCap = 2f;

	private float fBlinkActual = 1f;

	private float fTimerLogo = 8f;

	private bool bATCPlaying;

	private bool _volumesSet;

	public TextMeshProUGUI output;

	public TextMeshProUGUI loadOutputShort;

	private float modLoaderPrintTime;

	private bool running;
}
