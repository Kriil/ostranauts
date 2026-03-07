using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ship computer/storage terminal UI.
// This panel appears to bridge the cockpit terminal and the PDA, letting the
// player browse storage devices, rename a nav target, and move/run files.
public class GUIComputer2 : GUIData
{
	// Caches buttons, screen panels, and file-management controls after instantiation.
	protected override void Awake()
	{
		base.Awake();
		this.txtTime = base.transform.Find("MiddleGround/txtTime").GetComponent<TMP_Text>();
		Button component = base.transform.Find("MiddleGround/btnHome").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			CrewSim.LowerUI(false);
			GUIPDA.OpenApp("home");
		});
		AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		Button component2 = base.transform.Find("MiddleGround/btnQuit").GetComponent<Button>();
		component2.onClick.AddListener(delegate()
		{
			CrewSim.LowerUI(false);
		});
		AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		Button component3 = base.transform.Find("MiddleGround/Home/btnSearch").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			this.State = GUIComputer2.ScreenState.Search;
		});
		AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		Button component4 = base.transform.Find("MiddleGround/Home/btnStorage").GetComponent<Button>();
		component4.onClick.AddListener(delegate()
		{
			this.State = GUIComputer2.ScreenState.Stored;
		});
		AudioManager.AddBtnAudio(component4.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		Button component5 = base.transform.Find("MiddleGround/Search/pnlNavStation/btnEdit/editIcon").GetComponent<Button>();
		component5.onClick.AddListener(delegate()
		{
			this.StartNameEdit();
		});
		AudioManager.AddBtnAudio(component5.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.btnDetailsExit.onClick.AddListener(delegate()
		{
			CanvasManager.HideCanvasGroup(this.cgDetails);
		});
		AudioManager.AddBtnAudio(this.btnDetailsExit.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.btnLeftUp.onClick.AddListener(delegate()
		{
			base.StartCoroutine(this.ShowStorageDevices(true, false));
		});
		AudioManager.AddBtnAudio(this.btnLeftUp.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.btnRightUp.onClick.AddListener(delegate()
		{
			base.StartCoroutine(this.ShowStorageDevices(false, true));
		});
		AudioManager.AddBtnAudio(this.btnRightUp.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.cgDel.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.DeleteFilesAll();
		});
		AudioManager.AddBtnAudio(this.cgDel.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.cgMoveL.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.MoveFiles(this.strStorageRight, this.strStorageLeft);
		});
		AudioManager.AddBtnAudio(this.cgMoveL.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.cgMoveR.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.MoveFiles(this.strStorageLeft, this.strStorageRight);
		});
		AudioManager.AddBtnAudio(this.cgMoveR.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.cgRun.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.RunFile();
		});
		AudioManager.AddBtnAudio(this.cgRun.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		this.txtShipNameInput = base.transform.Find("MiddleGround/Search/pnlNavStation/txtInputVesselNameEdit").GetComponent<TMP_InputField>();
		this.cgShipNameInput = base.transform.Find("MiddleGround/Search/pnlNavStation/txtInputVesselNameEdit").GetComponent<CanvasGroup>();
		this.txtShipNameInput.onSubmit.AddListener(delegate(string A_1)
		{
			this.ChangeShipName();
		});
		this.txtShipNameInput.resetOnDeActivation = true;
		this.txtShipNameInput.onSelect.AddListener(delegate(string A_0)
		{
			CrewSim.Typing = true;
		});
		this.txtShipNameInput.onDeselect.AddListener(delegate(string A_1)
		{
			CrewSim.Typing = false;
			CanvasManager.HideCanvasGroup(this.cgShipNameInput);
		});
		this.txtShipNameInput.onValueChanged.AddListener(delegate(string A_0)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIComputerList", false, false);
		});
		CanvasManager.HideCanvasGroup(this.cgShipNameInput);
		this.cgBtnHome = component.GetComponent<CanvasGroup>();
		this.cgBtnQuit = component2.GetComponent<CanvasGroup>();
		this.cgHome = base.transform.Find("MiddleGround/Home").GetComponent<CanvasGroup>();
		this.cgLogin = base.transform.Find("MiddleGround/Login").GetComponent<CanvasGroup>();
		this.cgSearch = base.transform.Find("MiddleGround/Search").GetComponent<CanvasGroup>();
		this.cgStored = base.transform.Find("MiddleGround/Stored").GetComponent<CanvasGroup>();
		this.cgDetails = base.transform.Find("MiddleGround/Details").GetComponent<CanvasGroup>();
		this.aScreens = new List<CanvasGroup>
		{
			this.cgHome,
			this.cgLogin,
			this.cgSearch,
			this.cgStored,
			this.cgDetails
		};
		this.fBlink = this.fBlinkPeriod;
		this.rowTempDevice = (Resources.Load("GUIShip/GUIComputer/pnlDeviceRow") as GameObject);
	}

	// Initializes screen state from the owning CondOwner's condition/prop data.
	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.State = this.GetCondState();
	}

	// Opens the inline ship-name edit field for the current nav target.
	private void StartNameEdit()
	{
		CanvasManager.ShowCanvasGroup(this.cgShipNameInput);
		this.txtShipNameInput.text = this.coNAV.ship.publicName;
		this.txtShipNameInput.ActivateInputField();
	}

	// Commits the edited ship name to both the live ship and its JSON payload when present.
	private void ChangeShipName()
	{
		this.coNAV.ship.publicName = this.txtShipNameInput.text;
		if (this.coNAV.ship.json != null)
		{
			this.coNAV.ship.json.publicName = this.coNAV.ship.publicName;
		}
		this.ShowNav(this.tfListNAVSearch, this.coNAV);
		this.txtShipNameInput.DeactivateInputField();
		CanvasManager.HideCanvasGroup(this.cgShipNameInput);
	}

	// Updates the terminal clock and simple blinking cursor/indicator state.
	private void Update()
	{
		this.txtTime.text = MathUtils.GetUTCFromS(StarSystem.fEpoch);
		this.fBlink -= CrewSim.TimeElapsedScaled();
		if (this.fBlink < 0f)
		{
			this.bBlink = !this.bBlink;
			this.fBlink = this.fBlinkPeriod;
		}
	}

	// Boot logo animation shown when the computer UI starts.
	public IEnumerator LogoAnimation()
	{
		float duration = 1.4f;
		float timePassed = 0f;
		CanvasGroup cgBG = base.transform.Find("Background/bmpBG").GetComponent<CanvasGroup>();
		CanvasGroup cgBlank = base.transform.Find("Background/bmpBlank").GetComponent<CanvasGroup>();
		cgBlank.alpha = 0f;
		cgBG.alpha = 1f;
		while (duration > 0f)
		{
			duration -= Time.deltaTime;
			yield return null;
		}
		duration = 0.4f;
		while (cgBlank.alpha < 1f)
		{
			timePassed += Time.deltaTime;
			float blend = Mathf.Clamp01(timePassed / duration);
			cgBlank.alpha = Mathf.Lerp(cgBlank.alpha, 1f, blend);
			yield return null;
		}
		this.State = GUIComputer2.ScreenState.Login;
		yield return null;
		yield break;
	}

	public IEnumerator LoginAnimation()
	{
		float duration = 0f;
		float threshold = UnityEngine.Random.Range(0.02f, 0.04f);
		TMP_Text PasswordText = base.transform.Find("MiddleGround/Login/LoginBar/txtPwd").GetComponent<TMP_Text>();
		CanvasGroup cgTick = base.transform.Find("MiddleGround/Login/bmpUnlock").GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(cgTick);
		for (;;)
		{
			if (duration > threshold)
			{
				PasswordText.text += "*";
				duration = 0f;
				threshold = UnityEngine.Random.Range(0.02f, 0.04f);
			}
			duration += Time.deltaTime;
			if (PasswordText.text.Length > 35)
			{
				break;
			}
			yield return null;
		}
		CanvasManager.ShowCanvasGroup(cgTick);
		duration = 0.4f;
		while (duration > 0f)
		{
			duration -= Time.deltaTime;
			yield return null;
		}
		this.COSelf.AddCondAmount("IsPDALoggedIn", 1.0, 0.0, 0f);
		this.State = this.GetCondState();
		yield return null;
		yield break;
	}

	public IEnumerator ShowNAVDevices(CondOwner coAutoSelect)
	{
		AudioManager.am.PlayAudioEmitter("ShipUIComputerProcessing", false, false);
		List<CondOwner> aCOs = null;
		if (coAutoSelect == null && this.CTNav.Triggered(this.COSelf, null, true))
		{
			coAutoSelect = this.COSelf;
		}
		if (coAutoSelect != null)
		{
			aCOs = new List<CondOwner>
			{
				coAutoSelect
			};
		}
		else
		{
			aCOs = this.COUser.ship.GetICOs1(this.CTNav, true, false, true);
		}
		TMP_Text txtSearching = base.transform.Find("MiddleGround/Search/txtSearching").GetComponent<TMP_Text>();
		CanvasGroup cgNav = base.transform.Find("MiddleGround/Search/pnlNavStation").GetComponent<CanvasGroup>();
		IEnumerator enumerator = this.tfListNAVSearch.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		CanvasManager.HideCanvasGroup(cgNav);
		CanvasManager.HideCanvasGroup(this.cgSearchNoneFound);
		float duration = 1.4f;
		float threshold = UnityEngine.Random.Range(0.2f, 0.4f);
		while (duration > 0f)
		{
			txtSearching.alpha = 0f;
			if (this.bBlink)
			{
				txtSearching.alpha = 1f;
			}
			duration -= Time.deltaTime;
			yield return null;
		}
		duration = 0f;
		bool bNoneFound = true;
		while (this.State == GUIComputer2.ScreenState.Search)
		{
			txtSearching.alpha = 0f;
			if (this.bBlink)
			{
				txtSearching.alpha = 1f;
			}
			if (aCOs.Count > 0 && duration > threshold)
			{
				CondOwner condOwner = aCOs[0];
				aCOs.RemoveAt(0);
				if (condOwner == this.COSelf && this.CTPDA.Triggered(condOwner, null, true))
				{
					continue;
				}
				this.CreateDeviceRow(condOwner, this.tfListNAVSearch, new Action<Transform, CondOwner>(this.ShowNav));
				AudioManager.am.PlayAudioEmitter("ShipUIComputerList", false, false);
				duration = 0f;
				threshold = UnityEngine.Random.Range(0.02f, 0.04f);
				bNoneFound = false;
			}
			duration += Time.deltaTime;
			if (aCOs.Count == 0)
			{
				txtSearching.alpha = 0f;
				if (bNoneFound)
				{
					CanvasManager.ShowCanvasGroup(this.cgSearchNoneFound);
				}
				if (this.State != GUIComputer2.ScreenState.Search)
				{
					yield break;
				}
				if (coAutoSelect != null)
				{
					this.ShowNav(this.tfListNAVSearch, coAutoSelect);
				}
				yield return null;
				yield break;
			}
			else
			{
				yield return null;
			}
		}
		yield break;
	}

	private IEnumerator ShowStorageDevices(bool bLeft, bool bRight)
	{
		AudioManager.am.PlayAudioEmitter("ShipUIComputerProcessing", false, false);
		if (bLeft)
		{
			this.strStorageLeft = null;
			this.bSearchingLeft = true;
			CanvasManager.HideCanvasGroup(this.txtStorageLeft.GetComponent<CanvasGroup>());
			this.btnLeftUp.interactable = false;
		}
		if (bRight)
		{
			this.strStorageRight = null;
			this.bSearchingRight = true;
			CanvasManager.HideCanvasGroup(this.txtStorageRight.GetComponent<CanvasGroup>());
			this.btnRightUp.interactable = false;
		}
		if (this.bSearchingLeft || this.bSearchingRight)
		{
			CanvasManager.ShowCanvasGroup(this.cgStorageSearching);
		}
		CanvasManager.HideCanvasGroup(this.cgRun);
		CanvasManager.HideCanvasGroup(this.cgDel);
		CanvasManager.HideCanvasGroup(this.cgMoveL);
		CanvasManager.HideCanvasGroup(this.cgMoveR);
		List<CondOwner> aCOs = this.COUser.ship.GetICOs1(this.CTComputerOn, true, false, false);
		CondOwner.NullSafeAddRange(ref aCOs, this.COUser.GetCOs(false, this.CTDataCard));
		aCOs.Sort((CondOwner x, CondOwner y) => MathUtils.GetDistanceSquared(x, this.COSelf).CompareTo(MathUtils.GetDistanceSquared(y, this.COSelf)));
		TMP_Text txtSearching = base.transform.Find("MiddleGround/Stored/pnlSearching/txtSearching").GetComponent<TMP_Text>();
		IEnumerator enumerator = this.tfListFileSearchLeft.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				if (!bLeft)
				{
					break;
				}
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		IEnumerator enumerator2 = this.tfListFileSearchRight.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				object obj2 = enumerator2.Current;
				Transform transform2 = (Transform)obj2;
				if (!bRight)
				{
					break;
				}
				UnityEngine.Object.Destroy(transform2.gameObject);
			}
		}
		finally
		{
			IDisposable disposable2;
			if ((disposable2 = (enumerator2 as IDisposable)) != null)
			{
				disposable2.Dispose();
			}
		}
		float duration = 1.4f;
		float threshold = UnityEngine.Random.Range(0.2f, 0.4f);
		while (duration > 0f)
		{
			txtSearching.alpha = 0f;
			if (this.bBlink)
			{
				txtSearching.alpha = 1f;
			}
			duration -= Time.deltaTime;
			yield return null;
		}
		duration = 0f;
		while (this.State == GUIComputer2.ScreenState.Stored)
		{
			txtSearching.alpha = 0f;
			if (this.bBlink)
			{
				txtSearching.alpha = 1f;
			}
			if (aCOs.Count > 0 && duration > threshold)
			{
				CondOwner coDevice = aCOs[0];
				aCOs.RemoveAt(0);
				if (bLeft)
				{
					this.CreateDeviceRow(coDevice, this.tfListFileSearchLeft, new Action<Transform, CondOwner>(this.ShowDeviceFiles));
				}
				if (bRight)
				{
					this.CreateDeviceRow(coDevice, this.tfListFileSearchRight, new Action<Transform, CondOwner>(this.ShowDeviceFiles));
				}
				duration = 0f;
				threshold = UnityEngine.Random.Range(0.02f, 0.04f);
				AudioManager.am.PlayAudioEmitter("ShipUIComputerList", false, false);
			}
			duration += Time.deltaTime;
			if (aCOs.Count == 0)
			{
				if (bLeft)
				{
					this.bSearchingLeft = false;
				}
				if (bRight)
				{
					this.bSearchingRight = false;
				}
				if (!this.bSearchingLeft && !this.bSearchingRight)
				{
					CanvasManager.HideCanvasGroup(this.cgStorageSearching);
				}
				yield return null;
				yield break;
			}
			yield return null;
		}
		yield break;
	}

	private void CreateDeviceRow(CondOwner coDevice, Transform tfList, Action<Transform, CondOwner> act = null)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.rowTempDevice, tfList);
		string deviceName = this.GetDeviceName(coDevice);
		DeviceRow component = gameObject.GetComponent<DeviceRow>();
		component.Init(coDevice, deviceName, act);
		if (coDevice == this.COSelf)
		{
			component.Tint(Color.white);
		}
	}

	private string GetDeviceName(CondOwner coDevice)
	{
		double num = (double)MathUtils.GetDistance(coDevice, this.COSelf);
		string text = coDevice.ShortName;
		string text2 = " DIN: " + coDevice.strID.Substring(coDevice.strID.Length - 4).ToUpper();
		if (coDevice.objCOParent != null)
		{
			text = coDevice.RootParent(null).ShortName + " " + text;
		}
		string text3 = text;
		text = string.Concat(new string[]
		{
			text3,
			" (",
			MathUtils.GetDistUnits(num * 6.6845869117759804E-12),
			text2,
			")"
		});
		if (coDevice == this.COSelf)
		{
			text += "*";
		}
		return text;
	}

	private void ShowDeviceFiles(Transform tfList, CondOwner coDevice)
	{
		base.StartCoroutine(this._ShowDeviceFiles(tfList, coDevice));
	}

	private IEnumerator _ShowDeviceFiles(Transform tfList, CondOwner coDevice)
	{
		if (coDevice == null)
		{
			base.StartCoroutine(this.ShowStorageDevices(tfList == this.tfListFileSearchLeft, tfList == this.tfListFileSearchRight));
			yield break;
		}
		AudioManager.am.PlayAudioEmitter("ShipUIComputerProcessing", false, false);
		GameObject rowTempFile = Resources.Load("GUIShip/GUIComputer/chkDataFileRow") as GameObject;
		if (rowTempFile == null)
		{
			Debug.LogWarning("Error: rowTempFile is null");
			yield break;
		}
		IEnumerator enumerator = tfList.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		yield return null;
		CondOwner coNavData = null;
		bool bNAV = false;
		if (this.CTNav.Triggered(coDevice, null, true))
		{
			bNAV = true;
			coNavData = GUIOrbitDraw.GenerateNavDataCO(coDevice);
			coDevice.AddCO(coNavData, false, true, true);
		}
		List<CondOwner> aCOs = coDevice.GetCOs(true, this.CTFile);
		if (aCOs != null)
		{
			if (aCOs.Count > 0)
			{
				aCOs.RemoveAll((CondOwner co) => co == null);
				aCOs.Sort((CondOwner x, CondOwner y) => string.Compare(x.FriendlyName, y.FriendlyName, StringComparison.Ordinal));
			}
			float threshold = UnityEngine.Random.Range(0.2f, 0.4f);
			float duration = 0f;
			while (this.State == GUIComputer2.ScreenState.Stored)
			{
				if (aCOs.Count > 0 && duration > threshold)
				{
					CondOwner condOwner = aCOs[0];
					aCOs.RemoveAt(0);
					if (condOwner.objCOParent == null || condOwner.objCOParent.objCOParent != coDevice)
					{
						continue;
					}
					if (bNAV && condOwner.HasCond("IsDataBINNAV") && condOwner != coNavData)
					{
						coDevice.RemoveCO(condOwner, true);
						condOwner.Destroy();
						duration = 0f;
						continue;
					}
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(rowTempFile, tfList);
					GUIBtnLitRim component = gameObject.GetComponent<GUIBtnLitRim>();
					component.SetText(condOwner.FriendlyName);
					DatafileRow dfr = gameObject.GetComponent<DatafileRow>();
					dfr.strCOID = condOwner.strID;
					dfr.strName = condOwner.strName;
					dfr.chk.onValueChanged.AddListener(delegate(bool A_1)
					{
						this.ToggleFile(dfr);
					});
					string text = null;
					if (this.dictPropMap != null && this.dictPropMap.TryGetValue("Datafile_" + condOwner.strName, out text))
					{
						component.Tint(Color.gray, false);
						dfr.Tint(Color.gray);
					}
					if (condOwner.HasCond("IsDataIMG"))
					{
						dfr.SetFileIcon(this.bmpIMG);
					}
					else if (condOwner.HasCond("IsDataSND"))
					{
						dfr.SetFileIcon(this.bmpSND);
					}
					else if (condOwner.HasCond("IsDataTXT"))
					{
						dfr.SetFileIcon(this.bmpTXT);
					}
					else if (condOwner.HasCond("IsDataVID"))
					{
						dfr.SetFileIcon(this.bmpVID);
					}
					else
					{
						dfr.SetFileIcon(this.bmpBIN);
					}
					duration = 0f;
					threshold = UnityEngine.Random.Range(0.02f, 0.04f);
					AudioManager.am.PlayAudioEmitter("ShipUIComputerList", false, false);
				}
				duration += Time.deltaTime;
				if (aCOs.Count == 0)
				{
					goto IL_545;
				}
				yield return null;
			}
			yield break;
		}
		IL_545:
		if (tfList == this.tfListFileSearchLeft)
		{
			this.strStorageLeft = coDevice.strID;
			this.txtStorageLeft.text = this.GetDeviceName(coDevice);
			CanvasManager.ShowCanvasGroup(this.txtStorageLeft.GetComponent<CanvasGroup>());
			this.btnLeftUp.interactable = true;
		}
		else
		{
			this.strStorageRight = coDevice.strID;
			this.txtStorageRight.text = this.GetDeviceName(coDevice);
			CanvasManager.ShowCanvasGroup(this.txtStorageRight.GetComponent<CanvasGroup>());
			this.btnRightUp.interactable = true;
		}
		yield break;
	}

	private void ToggleFile(DatafileRow dfr)
	{
		if (this.bRefreshingFiles)
		{
			return;
		}
		this.bRefreshingFiles = true;
		AudioManager.am.PlayAudioEmitter("ShipUIComputerList", false, false);
		Transform transform = this.tfListFileSearchLeft;
		Transform transform2 = this.tfListFileSearchRight;
		if (dfr.transform.parent != transform)
		{
			transform = this.tfListFileSearchRight;
			transform2 = this.tfListFileSearchLeft;
		}
		CanvasManager.HideCanvasGroup(this.cgRun);
		CanvasManager.HideCanvasGroup(this.cgDel);
		CanvasManager.HideCanvasGroup(this.cgMoveL);
		CanvasManager.HideCanvasGroup(this.cgMoveR);
		IEnumerator enumerator = transform2.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform3 = (Transform)obj;
				Toggle component = transform3.GetComponent<Toggle>();
				if (component == null)
				{
					break;
				}
				component.isOn = false;
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		this.strStorageRun = null;
		int num = 0;
		IEnumerator enumerator2 = transform.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				object obj2 = enumerator2.Current;
				Transform transform4 = (Transform)obj2;
				DatafileRow component2 = transform4.GetComponent<DatafileRow>();
				if (component2.chk.isOn)
				{
					this.strStorageRun = component2.strCOID;
					num++;
				}
			}
		}
		finally
		{
			IDisposable disposable2;
			if ((disposable2 = (enumerator2 as IDisposable)) != null)
			{
				disposable2.Dispose();
			}
		}
		if (num == 1)
		{
			CanvasManager.ShowCanvasGroup(this.cgRun);
		}
		else
		{
			this.strStorageRun = null;
		}
		if (num > 0)
		{
			CanvasManager.ShowCanvasGroup(this.cgDel);
			if (transform == this.tfListFileSearchLeft)
			{
				CanvasManager.ShowCanvasGroup(this.cgMoveR);
			}
			else
			{
				CanvasManager.ShowCanvasGroup(this.cgMoveL);
			}
		}
		this.bRefreshingFiles = false;
	}

	private void DeleteFilesAll()
	{
		this.DeleteFiles(this.tfListFileSearchLeft, this.strStorageLeft);
		this.DeleteFiles(this.tfListFileSearchRight, this.strStorageRight);
	}

	private void DeleteFiles(Transform tfList, string strStorageCOID)
	{
		CondOwner condOwner = null;
		if (!string.IsNullOrEmpty(strStorageCOID) && DataHandler.mapCOs.TryGetValue(strStorageCOID, out condOwner))
		{
			bool flag = false;
			IEnumerator enumerator = tfList.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					DatafileRow component = transform.GetComponent<DatafileRow>();
					if (component.chk.isOn && !string.IsNullOrEmpty(component.strCOID))
					{
						CondOwner objCO = null;
						if (DataHandler.mapCOs.TryGetValue(component.strCOID, out objCO))
						{
							condOwner.RemoveCO(objCO, false);
							flag = true;
						}
					}
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			if (flag && strStorageCOID == this.strStorageLeft)
			{
				this.ShowDeviceFiles(this.tfListFileSearchLeft, condOwner);
			}
			if (flag && strStorageCOID == this.strStorageRight)
			{
				this.ShowDeviceFiles(this.tfListFileSearchRight, condOwner);
			}
			if (flag)
			{
				CanvasManager.HideCanvasGroup(this.cgRun);
				CanvasManager.HideCanvasGroup(this.cgDel);
				CanvasManager.HideCanvasGroup(this.cgMoveL);
				CanvasManager.HideCanvasGroup(this.cgMoveR);
			}
		}
	}

	private void MoveFiles(string strFrom, string strTo)
	{
		if (string.IsNullOrEmpty(strFrom) || string.IsNullOrEmpty(strTo) || strFrom == strTo)
		{
			return;
		}
		CondOwner condOwner = null;
		CondOwner condOwner2 = null;
		if (!DataHandler.mapCOs.TryGetValue(strFrom, out condOwner))
		{
			return;
		}
		if (!DataHandler.mapCOs.TryGetValue(strTo, out condOwner2))
		{
			return;
		}
		Transform transform = this.tfListFileSearchLeft;
		Transform tfList = this.tfListFileSearchRight;
		if (strFrom == this.strStorageRight)
		{
			transform = this.tfListFileSearchRight;
			tfList = this.tfListFileSearchLeft;
		}
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				DatafileRow component = transform2.GetComponent<DatafileRow>();
				if (component.chk.isOn && !string.IsNullOrEmpty(component.strCOID))
				{
					CondOwner condOwner3 = null;
					if (DataHandler.mapCOs.TryGetValue(component.strCOID, out condOwner3))
					{
						condOwner.RemoveCO(condOwner3, false);
						CondOwner condOwner4 = condOwner2.AddCO(condOwner3, false, true, true);
						if (condOwner4 != null)
						{
							Debug.LogWarning("Could not add DataFile " + condOwner4.strName + " to destination CO " + condOwner2.strName);
						}
						if (condOwner3.HasCond("IsDataBINNAV") && this.CTNav.Triggered(condOwner2, null, true))
						{
							GUIOrbitDraw.ImportNavDataCO(condOwner2, condOwner3);
						}
					}
					condOwner3 = null;
				}
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		this.ShowDeviceFiles(transform, condOwner);
		this.ShowDeviceFiles(tfList, condOwner2);
	}

	private void RunFile()
	{
		IEnumerator enumerator = this.tfListDetailLinks.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		if (string.IsNullOrEmpty(this.strStorageRun))
		{
			return;
		}
		CondOwner condOwner = null;
		if (!DataHandler.mapCOs.TryGetValue(this.strStorageRun, out condOwner))
		{
			condOwner = this.COFileTemp;
			condOwner.strID = this.strStorageRun;
			condOwner.strName = this.strStorageRun;
		}
		Interaction fileInteraction = this.GetFileInteraction(condOwner.strName);
		if (fileInteraction == null)
		{
			return;
		}
		if (fileInteraction.strName == "TEMPDataGeneric")
		{
			fileInteraction.strDesc = condOwner.strDesc;
			fileInteraction.strTitle = condOwner.FriendlyName;
		}
		fileInteraction.objUs = this.COUser;
		fileInteraction.objThem = condOwner;
		fileInteraction.ApplyEffects(null, false);
		this.dictPropMap["Datafile_" + condOwner.strName] = StarSystem.fEpoch.ToString();
		if (this.strStorageLeft != null)
		{
			this.TintToggledFiles(this.tfListFileSearchLeft);
			this.TintToggledFiles(this.tfListFileSearchRight);
		}
		CanvasManager.ShowCanvasGroup(this.cgDetails);
		this.txtDetailTitle.text = fileInteraction.strTitle;
		this.txtDetail.text = fileInteraction.strDesc;
		this.bmpDetail.texture = DataHandler.LoadPNG(fileInteraction.strImage + ".png", false, false);
		string[] aInverse = fileInteraction.aInverse;
		for (int i = 0; i < aInverse.Length; i++)
		{
			string text = aInverse[i];
			string[] array = text.Split(new char[]
			{
				','
			});
			Interaction iaReply = DataHandler.GetInteraction(array[0], null, false);
			if (iaReply != null)
			{
				iaReply.objUs = this.COUser;
				iaReply.objThem = condOwner;
				iaReply.bManual = true;
				iaReply.strPlot = fileInteraction.strPlot;
				if (iaReply.objUs == this.COUser && iaReply.Triggered(this.COUser, condOwner, false, false, false, true, null))
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.goDetailLinkTemplate, this.tfListDetailLinks);
					gameObject.transform.Find("txt").GetComponent<TMP_Text>().text = iaReply.strTitle;
					gameObject.GetComponent<Button>().onClick.AddListener(delegate()
					{
						this.strStorageRun = iaReply.strName;
						this.RunFile();
					});
					AudioManager.AddBtnAudio(gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
				}
			}
		}
		base.StartCoroutine(CrewSim.objInstance.ScrollTop(this.srDetailLinks));
		base.StartCoroutine(CrewSim.objInstance.ScrollTop(this.srDetailText));
	}

	private void TintToggledFiles(Transform tfList)
	{
		IEnumerator enumerator = tfList.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				DatafileRow component = transform.GetComponent<DatafileRow>();
				if (!(component == null) && component.chk.isOn)
				{
					component.Tint(Color.gray);
					GUIBtnLitRim component2 = transform.GetComponent<GUIBtnLitRim>();
					component2.Tint(Color.gray, false);
				}
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
	}

	private Interaction GetFileInteraction(string strFile)
	{
		Interaction interaction = DataHandler.GetInteraction(strFile, null, false);
		if (interaction == null)
		{
			interaction = DataHandler.GetInteraction("TEMPDataGeneric", null, false);
		}
		return interaction;
	}

	private void ShowNav(Transform tfList, CondOwner co)
	{
		AudioManager.am.PlayAudioEmitter("ShipUIComputerProcessing", false, false);
		this.coNAV = co;
		CanvasGroup component = base.transform.Find("MiddleGround/Search/pnlNavStation").GetComponent<CanvasGroup>();
		CanvasManager.ShowCanvasGroup(component);
		TMP_Text component2 = base.transform.Find("MiddleGround/Search/pnlNavStation/txtDataValue").GetComponent<TMP_Text>();
		TMP_Text component3 = base.transform.Find("MiddleGround/Search/pnlNavStation/txtStatus").GetComponent<TMP_Text>();
		Button component4 = base.transform.Find("MiddleGround/Search/pnlNavStation/btnLink").GetComponent<Button>();
		Button component5 = base.transform.Find("MiddleGround/Search/pnlNavStation/btnUnlink").GetComponent<Button>();
		CanvasGroup component6 = component4.GetComponent<CanvasGroup>();
		CanvasGroup component7 = component5.GetComponent<CanvasGroup>();
		component2.text = co.ship.publicName + "\n";
		TMP_Text tmp_Text = component2;
		tmp_Text.text = tmp_Text.text + co.ship.strRegID + "\n";
		TMP_Text tmp_Text2 = component2;
		tmp_Text2.text = tmp_Text2.text + co.ship.make + "\n";
		TMP_Text tmp_Text3 = component2;
		tmp_Text3.text = tmp_Text3.text + co.ship.model + "\n";
		TMP_Text tmp_Text4 = component2;
		tmp_Text4.text = tmp_Text4.text + co.ship.year + "\n";
		CanvasManager.HideCanvasGroup(component6);
		CanvasManager.HideCanvasGroup(component7);
		component4.onClick.RemoveAllListeners();
		component5.onClick.RemoveAllListeners();
		if (MonoSingleton<ObjectiveTracker>.Instance.subscribedShips.IndexOf(co.ship.strRegID) < 0)
		{
			CanvasManager.ShowCanvasGroup(component6);
			component4.onClick.AddListener(delegate()
			{
				MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(co.ship.strRegID);
				this.ShowNav(tfList, co);
				AudioManager.am.PlayAudioEmitter("ShipUIComputerLinkOn", false, false);
			});
			component3.text = "STATUS:\n<color=#FF5100>UNLINKED</color>";
		}
		else
		{
			CanvasManager.ShowCanvasGroup(component7);
			component5.onClick.AddListener(delegate()
			{
				MonoSingleton<ObjectiveTracker>.Instance.RemoveShipSubscription(co.ship.strRegID);
				this.ShowNav(tfList, co);
				AudioManager.am.PlayAudioEmitter("ShipUIComputerLinkOff", false, false);
			});
			component3.text = "STATUS:\n<color=#25FF78>LINKED</color>";
		}
	}

	private GUIComputer2.ScreenState GetCondState()
	{
		if (this.COSelf.HasCond("IsPDAModeNAVLink"))
		{
			return GUIComputer2.ScreenState.Search;
		}
		if (this.COSelf.HasCond("IsPDAModeFiles"))
		{
			return GUIComputer2.ScreenState.Stored;
		}
		return GUIComputer2.ScreenState.Home;
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
		if (this._coFileTemp != null)
		{
			this._coFileTemp.RemoveFromCurrentHome(true);
			this._coFileTemp.Destroy();
		}
	}

	private CondTrigger CTFile
	{
		get
		{
			if (this._ctFile == null)
			{
				this._ctFile = DataHandler.GetCondTrigger("TIsFitContainerDAT");
			}
			return this._ctFile;
		}
	}

	private CondTrigger CTNav
	{
		get
		{
			if (this._ctNav == null)
			{
				this._ctNav = DataHandler.GetCondTrigger("TIsNavStationNotOff");
			}
			return this._ctNav;
		}
	}

	private CondTrigger CTPDA
	{
		get
		{
			if (this._ctPDA == null)
			{
				this._ctPDA = DataHandler.GetCondTrigger("TIsComputerPDA");
			}
			return this._ctPDA;
		}
	}

	private CondTrigger CTComputerOn
	{
		get
		{
			if (this._ctComputerOn == null)
			{
				this._ctComputerOn = DataHandler.GetCondTrigger("TIsComputerAccessible");
			}
			return this._ctComputerOn;
		}
	}

	private CondTrigger CTDataCard
	{
		get
		{
			if (this._ctDataCard == null)
			{
				this._ctDataCard = DataHandler.GetCondTrigger("TIsDataCard");
			}
			return this._ctDataCard;
		}
	}

	private CondOwner COUser
	{
		get
		{
			if (this._coUser == null)
			{
				this._coUser = CrewSim.GetSelectedCrew();
			}
			return this._coUser;
		}
	}

	private CondOwner COFileTemp
	{
		get
		{
			if (this._coFileTemp == null)
			{
				this._coFileTemp = DataHandler.GetCondOwner("DataFile");
			}
			return this._coFileTemp;
		}
	}

	private GUIComputer2.ScreenState State
	{
		get
		{
			return this._state;
		}
		set
		{
			if (this._state == value)
			{
				return;
			}
			foreach (CanvasGroup cg in this.aScreens)
			{
				CanvasManager.HideCanvasGroup(cg);
			}
			CanvasManager.HideCanvasGroup(this.cgBtnHome);
			CanvasManager.HideCanvasGroup(this.cgBtnQuit);
			switch (value)
			{
			case GUIComputer2.ScreenState.Login:
				CanvasManager.ShowCanvasGroup(this.cgLogin);
				base.transform.Find("MiddleGround/Login/LoginBar/txtWelcome").GetComponent<TMP_Text>().text = "Welcome, " + this.COUser.pspec.strFirstName;
				base.StartCoroutine("LoginAnimation");
				break;
			case GUIComputer2.ScreenState.Home:
				CanvasManager.ShowCanvasGroup(this.cgHome);
				CanvasManager.ShowCanvasGroup(this.cgBtnHome);
				CanvasManager.ShowCanvasGroup(this.cgBtnQuit);
				break;
			case GUIComputer2.ScreenState.Logo:
				base.StartCoroutine("LogoAnimation");
				break;
			case GUIComputer2.ScreenState.Search:
				this.COSelf.ZeroCondAmount("IsPDAModeNAVLink");
				CanvasManager.ShowCanvasGroup(this.cgSearch);
				CanvasManager.ShowCanvasGroup(this.cgBtnHome);
				CanvasManager.ShowCanvasGroup(this.cgBtnQuit);
				base.StartCoroutine(this.ShowNAVDevices(null));
				break;
			case GUIComputer2.ScreenState.Stored:
				this.COSelf.ZeroCondAmount("IsPDAModeFiles");
				CanvasManager.ShowCanvasGroup(this.cgStored);
				CanvasManager.ShowCanvasGroup(this.cgBtnHome);
				CanvasManager.ShowCanvasGroup(this.cgBtnQuit);
				base.StartCoroutine(this.ShowStorageDevices(true, true));
				break;
			}
			this._state = value;
		}
	}

	private CondOwner _coUser;

	private CondOwner _coFileTemp;

	private CondOwner coNAV;

	private GUIComputer2.ScreenState _state;

	private CondTrigger _ctFile;

	private CondTrigger _ctNav;

	private CondTrigger _ctPDA;

	private CondTrigger _ctComputerOn;

	private CondTrigger _ctDataCard;

	private string strStorageLeft;

	private string strStorageRight;

	private string strStorageRun;

	private GameObject rowTempDevice;

	private List<CanvasGroup> aScreens;

	private CanvasGroup cgLogin;

	private CanvasGroup cgHome;

	private CanvasGroup cgSearch;

	private CanvasGroup cgStored;

	private CanvasGroup cgDetails;

	private CanvasGroup cgBtnHome;

	private CanvasGroup cgBtnQuit;

	private CanvasGroup cgVesselNameChange;

	private TMP_Text txtTime;

	private TMP_InputField txtShipNameInput;

	private CanvasGroup cgShipNameInput;

	[SerializeField]
	private Transform tfListNAVSearch;

	[SerializeField]
	private Transform tfListFileSearchLeft;

	[SerializeField]
	private Transform tfListFileSearchRight;

	[SerializeField]
	private Transform tfListDetailLinks;

	[SerializeField]
	private CanvasGroup cgMoveR;

	[SerializeField]
	private CanvasGroup cgMoveL;

	[SerializeField]
	private CanvasGroup cgDel;

	[SerializeField]
	private CanvasGroup cgRun;

	[SerializeField]
	private CanvasGroup cgStorageSearching;

	[SerializeField]
	private CanvasGroup cgSearchNoneFound;

	[SerializeField]
	private Button btnDetailsExit;

	[SerializeField]
	private Button btnLeftUp;

	[SerializeField]
	private Button btnRightUp;

	[SerializeField]
	private Image bmpBIN;

	[SerializeField]
	private Image bmpSND;

	[SerializeField]
	private Image bmpVID;

	[SerializeField]
	private Image bmpIMG;

	[SerializeField]
	private Image bmpTXT;

	[SerializeField]
	private RawImage bmpDetail;

	[SerializeField]
	private TMP_Text txtStorageLeft;

	[SerializeField]
	private TMP_Text txtStorageRight;

	[SerializeField]
	private TMP_Text txtDetail;

	[SerializeField]
	private TMP_Text txtDetailTitle;

	[SerializeField]
	private GameObject goDetailLinkTemplate;

	[SerializeField]
	private ScrollRect srDetailLinks;

	[SerializeField]
	private ScrollRect srDetailText;

	private bool bRefreshingFiles;

	private bool bBlink = true;

	private bool bSearchingLeft;

	private bool bSearchingRight;

	private float fBlinkPeriod = 0.4f;

	private float fBlink;

	private enum ScreenState
	{
		Login,
		Home,
		Logo,
		Search,
		Stored
	}
}
