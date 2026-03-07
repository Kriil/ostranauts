using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Objectives;
using Ostranauts.Racing;
using Ostranauts.Tools;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using Ostranauts.UI.Loading;
using Ostranauts.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ostranauts.Core
{
	// Save/load coordinator and async startup manager.
	// This bridges user save paths, compressed save archives, DataHandler's
	// threaded load, and the UI events exposed by the save/load menus.
	public class LoadManager : MonoSingleton<LoadManager>
	{
		// Base save root selected in user settings.
		public string BasePath
		{
			get
			{
				return DataHandler.GetUserSettings().GetSaveLocation();
			}
		}

		// Standard folder used for named save slots.
		public string SavesPath
		{
			get
			{
				return this.BasePath + "/Saves/";
			}
		}

		// Temporary extraction folder for archive-based save load operations.
		private string ExtractionFolder
		{
			get
			{
				return this.BasePath + "/Temp_Extract/";
			}
		}

		// Autosave cadence in seconds, sourced from user settings.
		public static int AutoSaveInterval
		{
			get
			{
				return (DataHandler.GetUserSettings() == null) ? 900 : DataHandler.GetUserSettings().nAutosaveInterval;
			}
		}

		// Convenience check for disabling autosaves entirely.
		public static bool IsAutoSaveEnabled
		{
			get
			{
				return DataHandler.GetUserSettings() == null || DataHandler.GetUserSettings().nAutosaveInterval > 0;
			}
		}

		// Maximum rotating autosave count to retain.
		private int MaxAutoSaveCount
		{
			get
			{
				return (DataHandler.GetUserSettings() == null) ? 5 : DataHandler.GetUserSettings().nAutosaveMaxCount;
			}
		}

		// Updated after successful save-info refreshes.
		public double LastSaveTimestamp { get; private set; }

		// Wires save/load UI events and starts the asynchronous DataHandler load bridge if needed.
		private new void Awake()
		{
			base.Awake();
			this._compressionProvider = new DotNetZipCompressor();
			if (GUILoadMenu.OnLoadSelected == null)
			{
				GUILoadMenu.OnLoadSelected = new LoadSelectedEvent();
			}
			if (GUISaveMenu.OnCreateSave == null)
			{
				GUISaveMenu.OnCreateSave = new CreateNewSaveEvent();
			}
			if (GUISaveLoadBase.OnDeleteSave == null)
			{
				GUISaveLoadBase.OnDeleteSave = new LoadSelectedEvent();
			}
			if (GUISaveMenu.OnOverwriteSelected == null)
			{
				GUISaveMenu.OnOverwriteSelected = new OverwriteSaveEvent();
			}
			if (LoadManager.OnPathChanged == null)
			{
				LoadManager.OnPathChanged = new PathChangedEvent();
			}
			if (LoadManager.OnSaveInfoUpdated == null)
			{
				LoadManager.OnSaveInfoUpdated = new UnityEvent();
			}
			if (LoadManager.OnSaveFinished == null)
			{
				LoadManager.OnSaveFinished = new UnityEvent();
			}
			if (LoadManager.OnAsyncSaveStarted == null)
			{
				LoadManager.OnAsyncSaveStarted = new UnityEvent();
			}
			if (LoadManager.OnSavingFailed == null)
			{
				LoadManager.OnSavingFailed = new SavingFailedEvent();
			}
			if (LoadManager.SaveInfoImagesLoadedEvent == null)
			{
				LoadManager.SaveInfoImagesLoadedEvent = new SaveInfoImagesLoadedEvent();
			}
			GUILoadMenu.OnLoadSelected.AddListener(new UnityAction<SaveInfo>(this.OnLoadSelectedSave));
			GUISaveMenu.OnCreateSave.AddListener(new UnityAction<string>(this.OnCreateSave));
			GUISaveMenu.OnOverwriteSelected.AddListener(new UnityAction<SaveInfo>(this.OnOverwrite));
			GUISaveLoadBase.OnDeleteSave.AddListener(new UnityAction<SaveInfo>(this.OnDelete));
			LoadManager.OnSaveInfoUpdated.AddListener(delegate()
			{
				this.LastSaveTimestamp = TimeUtils.GetCurrentEpochTimeSeconds();
			});
			LoadManager.OnSaveInfoUpdated.AddListener(delegate()
			{
				if (CrewSim.objGUISaveIndicator != null)
				{
					CrewSim.objGUISaveIndicator.EstablishSave(true);
				}
			});
			CrewSim.OnGameFinishedLoading.AddListener(new UnityAction(this.DeleteTempFolder));
			LoadManager.OnPathChanged.AddListener(new UnityAction<string>(this.OnPathWasChanged));
			this._logHandler = new LogHandler();
			if (!DataHandler.bInitialised)
			{
				DataHandler.InitComplete = (Action)Delegate.Combine(DataHandler.InitComplete, new Action(this.BeginDataHanderLoadThreads));
				base.StartCoroutine(this.AfterLoadThreadsFinish());
			}
		}

		// Spawns the background thread that runs the DataHandler load delegates.
		public void BeginDataHanderLoadThreads()
		{
			this.loadMainThread = new Thread(new ThreadStart(this.LoadDataHandlerDelegates));
			this.loadMainThread.Start();
		}

		// Waits for async JSON/data loading, then finalizes the main-thread portion of startup.
		// Data flow: DataHandler async parse -> PostModLoadMainThread -> DataHandler.LoadComplete.
		public IEnumerator AfterLoadThreadsFinish()
		{
			yield return new WaitUntil(() => DataHandler.bAsyncLoaded);
			for (int i = 0; i < LoadManager.JsonLogErrorExceptions.Count; i++)
			{
				LoadManager.JsonLogErrorExceptions[i]();
			}
			if (LoadManager.OnLoadThreadsComplete != null)
			{
				LoadManager.OnLoadThreadsComplete();
			}
			DataHandler.PostModLoadMainThread();
			if (this.loadMainThread != null)
			{
				if (this.loadMainThread.IsAlive)
				{
					this.loadMainThread.Join();
				}
				this.loadMainThread = null;
			}
			DataHandler.bLoaded = true;
			if (DataHandler.LoadComplete != null)
			{
				DataHandler.LoadComplete();
			}
			DataHandler.InitComplete = (Action)Delegate.Remove(DataHandler.InitComplete, new Action(this.BeginDataHanderLoadThreads));
			yield break;
		}

		// Requests cooperative cancellation for the main loader and any worker threads.
		public void AbortLoadThread()
		{
			if (this.loadMainThread != null && this.loadMainThread.IsAlive)
			{
				for (int i = 0; i < this.loaderThreads.Count; i++)
				{
					object handle = this.loaderThreads[i].handle;
					lock (handle)
					{
						this.loaderThreads[i].terminate = true;
					}
				}
				object obj = LoadManager.mainLoadLock;
				lock (obj)
				{
					LoadManager.mainLoadTerminate = true;
				}
			}
		}

		private IEnumerator Start()
		{
			yield return new WaitUntil(() => DataHandler.bInitialised);
			this.LoadSaveInfos();
			yield break;
		}

		private void OnDestroy()
		{
			GUILoadMenu.OnLoadSelected.RemoveListener(new UnityAction<SaveInfo>(this.OnLoadSelectedSave));
			GUISaveMenu.OnCreateSave.RemoveListener(new UnityAction<string>(this.OnCreateSave));
			GUISaveLoadBase.OnDeleteSave.RemoveListener(new UnityAction<SaveInfo>(this.OnDelete));
			GUISaveMenu.OnOverwriteSelected.RemoveListener(new UnityAction<SaveInfo>(this.OnOverwrite));
			CrewSim.OnGameFinishedLoading.RemoveListener(new UnityAction(this.DeleteTempFolder));
			LoadManager.OnPathChanged.RemoveListener(new UnityAction<string>(this.OnPathWasChanged));
			if (this._saveJob != null)
			{
				this._saveJob.Abort();
			}
			if (this._logHandler != null)
			{
				this._logHandler.Unsubscribe();
			}
			if (this._dictSaveInfos != null)
			{
				foreach (KeyValuePair<string, List<SaveInfo>> keyValuePair in this._dictSaveInfos)
				{
					foreach (SaveInfo saveInfo in keyValuePair.Value)
					{
						saveInfo.Destroy();
					}
				}
				this._dictSaveInfos.Clear();
			}
			this.AbortLoadThread();
		}

		private void OnPathWasChanged(string path)
		{
			DataHandler.GetUserSettings().strSaveLocation = path;
			DataHandler.SaveUserSettings();
			this._dictSaveInfos.Clear();
			this.LoadSaveInfos();
		}

		private void OnLoadSelectedSave(SaveInfo saveInfo)
		{
			if (saveInfo == null)
			{
				return;
			}
			bool key = Input.GetKey(KeyCode.LeftShift);
			this.DeleteTempFolder();
			string[] files = Directory.GetFiles(saveInfo.Path);
			string zipPath = null;
			Dictionary<string, byte[]> dictFiles = null;
			foreach (string text in files)
			{
				if (!(Path.GetExtension(text) != ".zip"))
				{
					if (key)
					{
						Debug.Log("Loading save via Zip Extraction to Temp Folder.");
						if (!Directory.Exists(this.ExtractionFolder))
						{
							Directory.CreateDirectory(this.ExtractionFolder);
						}
						zipPath = this._compressionProvider.ExtractArchive(text, this.ExtractionFolder);
					}
					else
					{
						Debug.Log("Loading save via Zip Extraction to Memory.");
						dictFiles = this._compressionProvider.ExtractArchive(text);
					}
					break;
				}
			}
			CrewSim.OnFinishLoading.AddListener(delegate()
			{
				if (dictFiles != null)
				{
					CrewSim.objInstance.LoadGame(saveInfo.PlayerName + ".json", "ships/", dictFiles);
					Dictionary<string, Texture2D> value = null;
					foreach (string text2 in dictFiles.Keys)
					{
						if (text2.IndexOf("ships/") >= 0 && !(Path.GetExtension(text2) != ".png"))
						{
							string directoryName = Path.GetDirectoryName(text2);
							if (!DataHandler.dictShipImages.TryGetValue(directoryName, out value))
							{
								value = new Dictionary<string, Texture2D>();
								DataHandler.dictShipImages[directoryName] = value;
							}
							Texture2D texture2D = new Texture2D(2, 2);
							texture2D.LoadImage(dictFiles[text2]);
							texture2D.name = text2;
							DataHandler.dictShipImages[directoryName][Path.GetFileNameWithoutExtension(text2)] = texture2D;
						}
					}
				}
				else if (zipPath != null)
				{
					CrewSim.objInstance.LoadGame(this.ExtractionFolder + saveInfo.PlayerName + ".json", this.ExtractionFolder + "ships/", null);
					string[] directories = Directory.GetDirectories(this.ExtractionFolder + "ships/");
					foreach (string text3 in directories)
					{
						Dictionary<string, Texture2D> dictionary = this.LoadImageFolder(text3);
						if (dictionary != null && dictionary.Count != 0)
						{
							string name = new DirectoryInfo(text3).Name;
							DataHandler.dictShipImages.TryAdd(name, dictionary);
						}
					}
				}
				else
				{
					CrewSim.objInstance.LoadGame(saveInfo.PathPlayer, saveInfo.PathShipsFolder, null);
				}
			});
			if (CrewSim.objInstance != null)
			{
				this._logHandler.LogMessage("Unloading Crewsim");
				CrewSim.objInstance.EmptyScene();
				if (CrewSim.OnGameEnd != null)
				{
					CrewSim.OnGameEnd.Invoke();
				}
				CrewSim.objInstance = null;
				SceneManager.LoadScene("MainMenu2");
				DataHandler.Init();
				this._logHandler.LogMessage("Crewsim Unloaded");
			}
			this._loadedSave = saveInfo;
			this._logHandler.LoadLog(saveInfo._jsonSaveInfo.strSaveLog);
			this._logHandler.LogMessage("Loaded from Savefile: " + saveInfo.SaveName);
			SceneManager.LoadScene("Loading");
		}

		private void OnCreateSave(string saveName)
		{
			this.SaveGame(saveName, 0, false);
		}

		private void OnOverwrite(SaveInfo saveInfo)
		{
			this.OnDelete(saveInfo);
			this.SaveGame(saveInfo.SaveName, 0, false);
		}

		private void OnDelete(SaveInfo saveInfo)
		{
			if (!Directory.Exists(saveInfo.Path))
			{
				return;
			}
			try
			{
				Directory.Delete(saveInfo.Path, true);
				this.RemoveFromDict(saveInfo);
				LoadManager.OnSaveInfoUpdated.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Could not delete directory; " + ex);
				LoadManager.OnSavingFailed.Invoke(ex);
			}
		}

		private void DeleteTempFolder()
		{
			if (!Directory.Exists(this.ExtractionFolder))
			{
				return;
			}
			try
			{
				Directory.Delete(this.ExtractionFolder, true);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Could not delete temp folder, " + ex.Message);
			}
		}

		private void AddToDict(SaveInfo saveInfo)
		{
			if (saveInfo == null)
			{
				return;
			}
			List<SaveInfo> list;
			if (this._dictSaveInfos.TryGetValue(saveInfo.GetWorldSeedID(), out list))
			{
				if (list == null)
				{
					list = new List<SaveInfo>();
				}
				list.Add(saveInfo);
			}
			else
			{
				this._dictSaveInfos[saveInfo.GetWorldSeedID()] = new List<SaveInfo>
				{
					saveInfo
				};
			}
		}

		private void RemoveFromDict(SaveInfo saveInfo)
		{
			if (saveInfo == null)
			{
				return;
			}
			List<SaveInfo> list;
			if (this._dictSaveInfos.TryGetValue(saveInfo.GetWorldSeedID(), out list))
			{
				list.Remove(saveInfo);
				if (list.Count == 0)
				{
					this._dictSaveInfos.Remove(saveInfo.GetWorldSeedID());
				}
			}
		}

		public static Texture2D LoadImageFromFile(string fullFilePath)
		{
			if (!File.Exists(fullFilePath))
			{
				Debug.LogWarning("File does not exist: " + fullFilePath);
				return null;
			}
			Texture2D texture2D = new Texture2D(2, 2);
			texture2D.name = fullFilePath;
			texture2D.LoadImage(File.ReadAllBytes(fullFilePath));
			return texture2D;
		}

		private Dictionary<string, Texture2D> LoadImageFolder(string directoryPath)
		{
			Dictionary<string, Texture2D> dictionary = new Dictionary<string, Texture2D>();
			try
			{
				if (!Directory.Exists(directoryPath))
				{
					return dictionary;
				}
				List<string> list = Directory.GetFiles(directoryPath, "*.png", SearchOption.AllDirectories).ToList<string>();
				foreach (string text in list)
				{
					dictionary.Add(Path.GetFileNameWithoutExtension(text), LoadManager.LoadImageFromFile(text));
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Could not load images from folder " + ex.Message);
			}
			return dictionary;
		}

		private void LoadSaveInfos()
		{
			if (!Directory.Exists(this.SavesPath))
			{
				return;
			}
			string[] directories = Directory.GetDirectories(this.SavesPath);
			foreach (string text in directories)
			{
				string text2 = text + "/saveInfo.json";
				if (File.Exists(text2))
				{
					Dictionary<string, JsonSaveInfo> dictionary = new Dictionary<string, JsonSaveInfo>();
					DataHandler.JsonToData<JsonSaveInfo>(text2, dictionary);
					using (Dictionary<string, JsonSaveInfo>.ValueCollection.Enumerator enumerator = dictionary.Values.GetEnumerator())
					{
						if (enumerator.MoveNext())
						{
							JsonSaveInfo jsonSaveInfo = enumerator.Current;
							SaveInfo saveInfo = new SaveInfo(jsonSaveInfo, text);
							this.AddToDict(saveInfo);
						}
					}
				}
			}
			this.LoadSaveInfoImages();
		}

		private void LoadSaveInfoImages()
		{
			base.StartCoroutine(this._LoadSaveInfoImages());
		}

		private IEnumerator _LoadSaveInfoImages()
		{
			List<SaveInfo> saveInfos = MonoSingleton<LoadManager>.Instance.GetSaveInfos();
			if (saveInfos == null || !saveInfos.Any<SaveInfo>())
			{
				yield break;
			}
			foreach (SaveInfo saveInfo in from x in saveInfos
			orderby x.EpochTimeStamp descending
			select x)
			{
				foreach (string text in Directory.GetFiles(saveInfo.Path))
				{
					if (text.EndsWith("portrait.png"))
					{
						saveInfo.Texture = LoadManager.LoadImageFromFile(text);
					}
					else if (text.EndsWith("screenshot.png"))
					{
						saveInfo.ScreenShot = LoadManager.LoadImageFromFile(text);
					}
					else if (text.EndsWith("_portrait_crew.png"))
					{
						if (saveInfo.CrewPortraits == null)
						{
							saveInfo.CrewPortraits = new List<Texture2D>();
						}
						saveInfo.CrewPortraits.Add(LoadManager.LoadImageFromFile(text));
					}
				}
				LoadManager.SaveInfoImagesLoadedEvent.Invoke(saveInfo);
				yield return null;
			}
			yield break;
		}

		private void SaveGame(string saveName, int autosaveCounter = 0, bool useThreading = false)
		{
			if (string.IsNullOrEmpty(saveName))
			{
				Debug.LogWarning("Save name is null or empty");
				return;
			}
			this._logHandler.LogMessage(string.Concat(new object[]
			{
				"Saving! Name: ",
				saveName,
				" AutosaveCounter: ",
				autosaveCounter,
				" Threading: ",
				useThreading
			}));
			this.CancelRunningThreads();
			string text = this.SavesPath + "/" + saveName + "/";
			try
			{
				if (!Directory.Exists(this.SavesPath))
				{
					Directory.CreateDirectory(this.SavesPath);
				}
				Directory.CreateDirectory(text);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Could not create directory, " + ex.Message);
				LoadManager.OnSavingFailed.Invoke(ex);
				return;
			}
			SaveDto saveDto = this.SaveGameData(text);
			saveDto.filepath = text;
			saveDto.persistenpath = DataHandler.GetUserSettings().GetSaveLocation();
			this._loadedSave = this.SaveGameInfo(text, saveName, autosaveCounter);
			saveDto.AddSaveInfo(this._loadedSave._jsonSaveInfo, text + "saveInfo.json");
			this.AddToDict(this._loadedSave);
			this._loadedSave.Texture = this.SavePortrait(text);
			this._loadedSave.CrewPortraits = this.SaveCrewPortraits(text);
			this._loadedSave.ScreenShot = this.SaveScreenShot(text);
			this._saveJob = new SavingJob();
			this._saveJob.SaveDto = saveDto;
			if (useThreading)
			{
				LoadManager.OnAsyncSaveStarted.Invoke();
				this._saveJob.Start();
			}
			else
			{
				this._saveJob.StartWithoutThreading();
			}
			base.StartCoroutine(this.NotifyListenersWhenDone());
		}

		private void EvaluateShips(SaveDto saveDto)
		{
			if (CrewSim.system.dictShips == null || saveDto == null || saveDto.dictShips == null)
			{
				return;
			}
			string text = string.Empty;
			foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
			{
				bool flag = false;
				foreach (JsonShip jsonShip in saveDto.dictShips.Values)
				{
					if (keyValuePair.Key == jsonShip.strRegID)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					text = text + keyValuePair.Key + " ";
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				Debug.LogWarning("Save is missing ships: " + text);
			}
		}

		private void CancelRunningThreads()
		{
			if (this._saveJob == null || this._saveJob.IsDone)
			{
				return;
			}
			this._saveJob.AbortSafe();
			this.RemoveFromDict(this._loadedSave);
			LoadManager.OnSaveInfoUpdated.Invoke();
			Debug.Log("Aborted thread");
		}

		private IEnumerator NotifyListenersWhenDone()
		{
			yield return base.StartCoroutine(this._saveJob.WaitFor());
			if (this._saveJob != null && this._saveJob.Exception != null)
			{
				this.HandleError();
			}
			if (this._saveJob != null)
			{
				this._saveJob.SaveDto = null;
			}
			LoadManager.OnSaveInfoUpdated.Invoke();
			LoadManager.OnSaveFinished.Invoke();
			yield break;
		}

		private void HandleError()
		{
			if (this._loadedSave != null)
			{
				this.OnDelete(this._loadedSave);
			}
			List<SaveInfo> list;
			if (this._loadedSave != null && !this._dictSaveInfos.TryGetValue(this._loadedSave.GetWorldSeedID(), out list))
			{
				this._loadedSave = null;
			}
			LoadManager.OnSavingFailed.Invoke(this._saveJob.Exception);
			this._saveJob.Exception = null;
		}

		private Texture2D SaveScreenShot(string folderPath)
		{
			Camera mainCamera = GameRenderer.MainCamera;
			if (mainCamera == null)
			{
				return null;
			}
			int width = GameRenderer.Width;
			int height = GameRenderer.Height;
			RenderTexture renderTexture = new RenderTexture(width, height, 24);
			mainCamera.targetTexture = renderTexture;
			Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
			mainCamera.Render();
			RenderTexture.active = renderTexture;
			texture2D.ReadPixels(new Rect(0f, 0f, (float)width, (float)height), 0, 0);
			texture2D.Apply();
			texture2D.name = folderPath + "screenshot.png " + Guid.NewGuid().ToString();
			mainCamera.targetTexture = null;
			RenderTexture.active = null;
			UnityEngine.Object.Destroy(renderTexture);
			File.WriteAllBytes(folderPath + "screenshot.png", texture2D.EncodeToPNG());
			return texture2D;
		}

		private SaveDto SaveGameData(string folderPath)
		{
			SaveDto saveDto = new SaveDto();
			JsonGameSave jsonGameSave = new JsonGameSave();
			jsonGameSave.strName = CrewSim.coPlayer.strID;
			Dictionary<string, JsonCondOwnerSave> dictionary = new Dictionary<string, JsonCondOwnerSave>();
			List<string> list = new List<string>();
			Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
			foreach (KeyValuePair<string, JsonCondOwnerSave> keyValuePair in DataHandler.dictCOSaves)
			{
				if (string.IsNullOrEmpty(keyValuePair.Value.strRegIDLast) || !CrewSim.system.dictShips.ContainsKey(keyValuePair.Value.strRegIDLast))
				{
					list.Add(keyValuePair.Key);
				}
				else
				{
					if (dictionary2.ContainsKey(keyValuePair.Value.strRegIDLast))
					{
						dictionary2[keyValuePair.Value.strRegIDLast].Add(keyValuePair.Key);
					}
					else
					{
						dictionary2[keyValuePair.Value.strRegIDLast] = new List<string>
						{
							keyValuePair.Key
						};
					}
					dictionary.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}
			Debug.Log("Trimming phase 1 dictCOSaves before: " + DataHandler.dictCOSaves.Count);
			foreach (string key in list)
			{
				DataHandler.dictCOSaves.Remove(key);
			}
			Debug.Log("Trimming phase 1 dictCOSaves after: " + DataHandler.dictCOSaves.Count);
			jsonGameSave.fTotalGameSec = CrewSim.fTotalGameSec;
			jsonGameSave.fTotalGameSecUnscaled = CrewSim.fTotalGameSecUnscaled;
			jsonGameSave.strVersion = CrewSim.strSaveVersion;
			jsonGameSave.strShip = CrewSim.coPlayer.ship.strRegID;
			jsonGameSave.strPlayerCO = CrewSim.coPlayer.strID;
			jsonGameSave.aCustomInfos = CrewSim.CustomInfosString();
			foreach (Ship ship in CrewSim.system.dictShips.Values)
			{
				try
				{
					List<CondOwner> list2 = new List<CondOwner>();
					if (ship.Classification != Ship.TypeClassification.Waypoint)
					{
						list2.AddRange(ship.GetCOs(null, true, false, true));
						JsonShip json = ship.GetJSON(ship.json.strName, true, list2);
						if (!dictionary2.ContainsKey(ship.strRegID))
						{
							dictionary2[ship.strRegID] = new List<string>();
						}
						List<JsonCondOwnerSave> list3 = new List<JsonCondOwnerSave>();
						List<CondOwner> list4 = new List<CondOwner>();
						foreach (CondOwner condOwner in list2)
						{
							if (!(condOwner == null))
							{
								if (ship.LoadState < Ship.Loaded.Edit && condOwner.jCOS != null)
								{
									list3.Add(condOwner.jCOS);
									dictionary.Remove(condOwner.strID);
									dictionary2[ship.strRegID].Remove(condOwner.strID);
								}
								else
								{
									JsonCondOwnerSave jsonsave = condOwner.GetJSONSave();
									if (jsonsave != null)
									{
										list3.Add(jsonsave);
										dictionary.Remove(condOwner.strID);
										dictionary2[ship.strRegID].Remove(condOwner.strID);
									}
									list4.AddRange(condOwner.GetLotCOs(true));
								}
							}
						}
						foreach (CondOwner condOwner2 in list4)
						{
							JsonCondOwnerSave jsonsave2 = condOwner2.GetJSONSave();
							if (jsonsave2 != null)
							{
								list3.Add(jsonsave2);
								dictionary.Remove(condOwner2.strID);
								dictionary2[ship.strRegID].Remove(condOwner2.strID);
							}
						}
						foreach (JsonItem jsonItem in json.aItems)
						{
							dictionary2[ship.strRegID].Remove(jsonItem.strID);
						}
						json.aCOs = list3.ToArray();
						saveDto.AddShipImages(DataHandler.GetShipImageByteArrays(ship.strRegID), folderPath + "ships/" + ship.strRegID + "/");
						saveDto.AddShip(json, folderPath + "ships/" + ship.strRegID + ".json");
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("Could not save ship " + ship.strRegID + ", skipping. Error: " + ex.Message);
				}
			}
			Debug.Log("Trimming phase 2 dictCOSaves before: " + DataHandler.dictCOSaves.Count);
			foreach (List<string> list5 in dictionary2.Values)
			{
				foreach (string key2 in list5)
				{
					dictionary.Remove(key2);
					DataHandler.dictCOSaves.Remove(key2);
				}
			}
			Debug.Log("Trimming phase 2 dictCOSaves after: " + DataHandler.dictCOSaves.Count);
			jsonGameSave.aCOs = dictionary.Values.ToArray<JsonCondOwnerSave>();
			jsonGameSave.aTasksUnclaimed = CrewSim.objInstance.workManager.GetUnclaimedTasksSaveData();
			jsonGameSave.aJobs = GigManager.aJobs.ToArray();
			jsonGameSave.objSystem = CrewSim.system.GetJSONSave();
			jsonGameSave.aLIs = Ledger.GetJSONSave();
			jsonGameSave.aPlots = PlotManager.GetJSONSave();
			jsonGameSave.aPlotsOld = PlotManager.GetJSONSaveOld();
			List<JsonObjective> list6 = new List<JsonObjective>();
			foreach (Objective objective in MonoSingleton<ObjectiveTracker>.Instance.AllObjectives)
			{
				JsonObjective json2 = objective.GetJSON();
				list6.Add(json2);
			}
			jsonGameSave.aObjectives = list6.ToArray();
			jsonGameSave.subscribedShips = MonoSingleton<ObjectiveTracker>.Instance.subscribedShips.ToArray();
			jsonGameSave.objAIShipManager = AIShipManager.GetJSONSave();
			jsonGameSave.objRacingManager = MonoSingleton<RacingLeagueManager>.Instance.GetJson();
			jsonGameSave.objMarketSave = MarketManager.GetJSONSave();
			saveDto.AddGameSave(jsonGameSave, folderPath + CrewSim.coPlayer.strName + ".json");
			this.EvaluateShips(saveDto);
			return saveDto;
		}

		private SaveInfo SaveGameInfo(string folderPath, string saveName, int autoSaveCounter)
		{
			JsonSaveInfo jsonSaveInfo = new JsonSaveInfo();
			jsonSaveInfo.strName = saveName;
			jsonSaveInfo.playerName = CrewSim.coPlayer.strName;
			jsonSaveInfo.version = CrewSim.strSaveVersion;
			jsonSaveInfo.age = CrewSim.coPlayer.GetCondAmount("StatAge");
			jsonSaveInfo.money = CrewSim.coPlayer.GetCondAmount("StatUSD");
			jsonSaveInfo.shipName = CrewSim.coPlayer.ship.publicName;
			string realWorldTime = string.Concat(new string[]
			{
				DateTime.Now.Year.ToString(),
				"-",
				DateTime.Now.Month.ToString("00"),
				"-",
				DateTime.Now.Day.ToString("00"),
				" ",
				DateTime.Now.Hour.ToString("00"),
				":",
				DateTime.Now.Minute.ToString("00"),
				":",
				DateTime.Now.Second.ToString("00")
			});
			jsonSaveInfo.realWorldTime = realWorldTime;
			jsonSaveInfo.playTimeElapsed = CrewSim.fTotalGameSecUnscaled;
			jsonSaveInfo.simTimeElapsed = (double)CrewSim.fTotalGameSec;
			jsonSaveInfo.simTimeCurrent = StarSystem.fEpoch;
			jsonSaveInfo.formerOccupation = CrewSim.coPlayer.pspec.strCareerNow;
			jsonSaveInfo.epochCreationTime = TimeUtils.ConvertStringDate(realWorldTime);
			jsonSaveInfo.autoSaveCounter = autoSaveCounter;
			jsonSaveInfo.seedId = ((this._loadedSave == null) ? DataHandler.GetNextID() : this._loadedSave.GetWorldSeedID());
			jsonSaveInfo.strSaveLog = this._logHandler.Log;
			return new SaveInfo(jsonSaveInfo, folderPath);
		}

		private Texture2D SavePortrait(string folderPath)
		{
			Texture2D png = FaceAnim2.GetPNG(CrewSim.coPlayer);
			File.WriteAllBytes(folderPath + "portrait.png", png.EncodeToPNG());
			return png;
		}

		private List<Texture2D> SaveCrewPortraits(string folderPath)
		{
			List<Texture2D> list = new List<Texture2D>();
			if (CrewSim.coPlayer == null || CrewSim.coPlayer.Company == null || CrewSim.coPlayer.Company.mapRoster == null)
			{
				return list;
			}
			foreach (string text in CrewSim.coPlayer.Company.mapRoster.Keys)
			{
				if (!(text == CrewSim.coPlayer.strID))
				{
					CondOwner condOwner;
					if (DataHandler.mapCOs.TryGetValue(text, out condOwner))
					{
						Texture2D png = FaceAnim2.GetPNG(condOwner);
						File.WriteAllBytes(folderPath + condOwner.strName + "_portrait_crew.png", png.EncodeToPNG());
						list.Add(png);
					}
				}
			}
			return list;
		}

		public static Exception CompressSaveFolder(string folderPath, ICompressionProvider compressionProvider)
		{
			Exception ex = compressionProvider.CompressFolder(folderPath);
			if (ex != null)
			{
				return ex;
			}
			foreach (string path in Directory.GetDirectories(folderPath))
			{
				Directory.Delete(path, true);
			}
			foreach (string path2 in Directory.GetFiles(folderPath))
			{
				if (!(Path.GetExtension(path2) == ".zip") && !(Path.GetExtension(path2) == ".png") && !Path.GetFileName(path2).Contains("Info"))
				{
					File.Delete(path2);
				}
			}
			return null;
		}

		private string ConstructAutoSaveName(string oldSaveName, int autosaveNumber)
		{
			string[] array = oldSaveName.Split(new char[]
			{
				'_'
			});
			int num;
			if (array[0] == "autosave" && array.Length >= 3 && int.TryParse(array[1], out num))
			{
				oldSaveName = oldSaveName.Replace(array[0] + "_" + array[1] + "_", string.Empty);
			}
			string text = string.Concat(new object[]
			{
				"autosave_",
				autosaveNumber,
				"_",
				oldSaveName
			});
			int num2 = 1;
			while (Directory.Exists(this.SavesPath + text))
			{
				text = text + "_" + num2;
				num2++;
			}
			return this.ValidatePathLength(text);
		}

		private string ValidatePathLength(string directoryName)
		{
			string text = this.SavesPath + "/" + directoryName + "/";
			return (text.Length <= 247) ? directoryName : ("autosave_" + UnityEngine.Random.Range(1000000, 2000000000));
		}

		private void DeleteAfterSave(SaveInfo markedForDeletion)
		{
			LoadManager.OnSaveInfoUpdated.RemoveListener(this._removeDelegate);
			if (markedForDeletion == null)
			{
				return;
			}
			this.OnDelete(markedForDeletion);
		}

		public void LoadDataHandlerDelegates()
		{
			int num = 8;
			for (int i = 0; i < num; i++)
			{
				this.loaderThreads.Add(new LoaderThread());
			}
			int j = 0;
			int num2 = 0;
			while (j < LoadManager.LoadingQueue.Count)
			{
				ModLoader modLoader = LoadManager.LoadingQueue[j];
				int k = 0;
				while (k < modLoader.ships.Count)
				{
					this.loaderThreads[num2 % num].fileLoaders.Add(modLoader.ships[k]);
					k++;
					num2++;
				}
				j++;
			}
			for (int l = 0; l < num; l++)
			{
				this.loaderThreads[l].t = new Thread(new ThreadStart(this.loaderThreads[l].Run));
				this.loaderThreads[l].t.Start();
			}
			while (LoadManager.LoadingQueue.Count > 0)
			{
				ModLoader modLoader2 = LoadManager.LoadingQueue[0];
				LoadManager.LoadingQueue.RemoveAt(0);
				if (modLoader2 != null && modLoader2.JsonModInfo != null && !string.IsNullOrEmpty(modLoader2.JsonModInfo.strName))
				{
					object obj = LoadManager.outputLock;
					lock (obj)
					{
						LoadManager.modNamesStartedLoading.Add(modLoader2.JsonModInfo.strName);
					}
				}
				for (int m = 0; m < modLoader2.fileLoaders.Count; m++)
				{
					object obj2 = LoadManager.mainLoadLock;
					lock (obj2)
					{
						if (LoadManager.mainLoadTerminate)
						{
							return;
						}
					}
					FileLoader fileLoader = modLoader2.fileLoaders[m];
					fileLoader.loadDelegate();
					if (fileLoader != null && !string.IsNullOrEmpty(fileLoader.fileName))
					{
						object obj3 = LoadManager.outputLock;
						lock (obj3)
						{
							LoadManager.fileNamesLoaded.Add(Path.GetFileNameWithoutExtension(fileLoader.fileName));
						}
					}
				}
				for (int n = 0; n < modLoader2.PerModPostLoadAsyncOkay.Count; n++)
				{
					modLoader2.PerModPostLoadAsyncOkay[n]();
				}
				if (modLoader2 != null && modLoader2.JsonModInfo != null && !string.IsNullOrEmpty(modLoader2.JsonModInfo.strName))
				{
					object obj4 = LoadManager.outputLock;
					lock (obj4)
					{
						LoadManager.modNamesCompletedLoading.Add(modLoader2.JsonModInfo.strName);
					}
				}
				modLoader2.complete = true;
			}
			for (int num3 = 0; num3 < num; num3++)
			{
				this.loaderThreads[num3].t.Join();
			}
			DataHandler.AllPostLoadAsync();
			DataHandler.bAsyncLoaded = true;
		}

		public bool DoesSaveExist(string saveName, out SaveInfo existingSaveInfo)
		{
			foreach (KeyValuePair<string, List<SaveInfo>> keyValuePair in this._dictSaveInfos)
			{
				if (keyValuePair.Value != null)
				{
					foreach (SaveInfo saveInfo in keyValuePair.Value)
					{
						if (!(saveInfo.SaveName.ToLower() != saveName.ToLower()))
						{
							existingSaveInfo = saveInfo;
							return true;
						}
					}
				}
			}
			existingSaveInfo = null;
			return false;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

		public long GetAvailableSpace()
		{
			try
			{
				char c = this.BasePath[0];
				ulong num;
				ulong num2;
				ulong num3;
				if (LoadManager.GetDiskFreeSpaceEx(c + ":\\", out num, out num2, out num3))
				{
					return (long)(num / 1024UL / 1024UL);
				}
				Debug.LogWarning("Error occurred trying to fetch free disk space; Error was handled");
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Error occurred trying to fetch free disk space " + ex.Message);
			}
			return -1L;
		}

		public List<SaveInfo> GetSaveInfos()
		{
			List<SaveInfo> list = new List<SaveInfo>();
			foreach (List<SaveInfo> list2 in this._dictSaveInfos.Values)
			{
				if (list2 != null)
				{
					list.AddRange(list2);
				}
			}
			return list;
		}

		public void ShowSaveMenu(Transform menuParent)
		{
			if (menuParent == null)
			{
				return;
			}
			UnityEngine.Object.Instantiate<GameObject>(this._guiSaveMenuPrefab, menuParent);
		}

		public void AutoSave(bool useThreading = true)
		{
			if (CrewSim.coPlayer == null)
			{
				return;
			}
			if (useThreading && this._saveJob != null && !this._saveJob.IsDone)
			{
				return;
			}
			if (this._loadedSave == null)
			{
				this.SaveGame(CrewSim.coPlayer.strName + "_" + TimeUtils.GetCurrentEpochTimeSeconds(), 0, true);
				return;
			}
			List<SaveInfo> list;
			if (!this._dictSaveInfos.TryGetValue(this._loadedSave.GetWorldSeedID(), out list))
			{
				return;
			}
			if (list == null)
			{
				return;
			}
			List<SaveInfo> list2 = new List<SaveInfo>();
			foreach (SaveInfo saveInfo in list)
			{
				if (saveInfo.IsAutoSave)
				{
					list2.Add(saveInfo);
				}
			}
			if (list2.Count > 1)
			{
				list2 = (from x in list2
				orderby x.AutoSaveCounter descending
				select x).ToList<SaveInfo>();
			}
			if (list2.Count >= this.MaxAutoSaveCount)
			{
				SaveInfo markedForDeletion = (from x in list2
				orderby x.EpochTimeStamp
				select x).FirstOrDefault<SaveInfo>();
				this._removeDelegate = delegate()
				{
					this.DeleteAfterSave(markedForDeletion);
				};
				LoadManager.OnSaveInfoUpdated.AddListener(this._removeDelegate);
			}
			int num = (list2.Count <= 0) ? 1 : ((list2.First<SaveInfo>().AutoSaveCounter + 1) % 1000000);
			string saveName = this.ConstructAutoSaveName(this._loadedSave.SaveName, num);
			this.SaveGame(saveName, num, useThreading);
		}

		public static SaveInfoImagesLoadedEvent SaveInfoImagesLoadedEvent;

		public static UnityEvent OnSaveInfoUpdated;

		public static UnityEvent OnSaveFinished;

		public static UnityEvent OnAsyncSaveStarted;

		public static PathChangedEvent OnPathChanged;

		public static SavingFailedEvent OnSavingFailed;

		[SerializeField]
		private GameObject _guiSaveMenuPrefab;

		private readonly Dictionary<string, List<SaveInfo>> _dictSaveInfos = new Dictionary<string, List<SaveInfo>>();

		private SaveInfo _loadedSave;

		private ICompressionProvider _compressionProvider;

		private UnityAction _removeDelegate;

		private SavingJob _saveJob;

		private LogHandler _logHandler;

		public static ModLoader LastScheduledMod;

		public static List<ModLoader> LoadingQueue = new List<ModLoader>();

		public static List<Action> JsonLogErrorExceptions = new List<Action>();

		public static Action OnLoadThreadsComplete;

		public static List<string> fileNamesLoaded = new List<string>();

		public static List<string> modNamesStartedLoading = new List<string>();

		public static List<string> modNamesCompletedLoading = new List<string>();

		public static StringBuilder asyncLoadLogShort = new StringBuilder();

		public static StringBuilder asyncLoadLogLong = new StringBuilder();

		public static object mainLoadLock = new object();

		public static bool mainLoadTerminate;

		public Thread loadMainThread;

		public List<LoaderThread> loaderThreads = new List<LoaderThread>();

		public static object outputLock = new object();
	}
}
