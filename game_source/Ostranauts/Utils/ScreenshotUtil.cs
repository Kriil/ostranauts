using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Ships;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.UI.ShipRating;
using UnityEngine;

namespace Ostranauts.Utils
{
	public class ScreenshotUtil : MonoSingleton<ScreenshotUtil>
	{
		public bool IsRunning
		{
			get
			{
				return this._activeRun != null;
			}
		}

		public void CreateAndSave(JsonShip jship, string savePath)
		{
			if (this._activeRun != null)
			{
				base.StopCoroutine(this._activeRun);
			}
			this._activeRun = base.StartCoroutine(this.GetScreenShots(jship, savePath));
		}

		public void CreateAndSaveSingleImage(Ship ship, string savePath, string fileName)
		{
			if (this._activeRun != null)
			{
				base.StopCoroutine(this._activeRun);
			}
			this._activeRun = base.StartCoroutine(this.GetScreenShot(ship, savePath, fileName));
		}

		public IEnumerator CreateShipImages(Ship ship)
		{
			if (ship == null || ship.json == null)
			{
				yield break;
			}
			ship.ToggleDockedVis(false);
			ship.ToggleCrewVisibility(false);
			string jsonRegId = ship.json.strRegID;
			ship.json.strRegID = ship.strRegID;
			yield return this.GetScreenShots(ship.json);
			ship.json.strRegID = jsonRegId;
			ship.ToggleCrewVisibility(true);
			ship.ToggleDockedVis(true);
			yield break;
		}

		private IEnumerator GetScreenShots(JsonShip jship, string savePath)
		{
			if (string.IsNullOrEmpty(savePath))
			{
				yield break;
			}
			yield return this.GetScreenShots(jship);
			Dictionary<string, Texture2D> images;
			if (jship != null && DataHandler.dictShipImages.TryGetValue(jship.strRegID, out images))
			{
				if (Directory.Exists(savePath))
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(savePath);
					string pattern = "_\\d+\\.png$";
					foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.png"))
					{
						if (fileInfo != null)
						{
							if (!Regex.IsMatch(fileInfo.Name, pattern))
							{
								DataHandler.RemoveFile(fileInfo.FullName);
							}
						}
					}
				}
				this.SaveToDisk(savePath, images, false);
			}
			yield break;
		}

		private IEnumerator GetScreenShots(JsonShip jship)
		{
			Ship ship = CrewSim.GetLoadedShipByRegId(jship.strRegID);
			Camera cam = CrewSim.objInstance.ScreenShotCam;
			if (ship == null || cam == null || ship.LoadState <= Ship.Loaded.Shallow)
			{
				yield break;
			}
			GameRenderer gameRenderer = cam.GetComponent<GameRenderer>();
			cam.depth = -2f;
			cam.gameObject.SetActive(true);
			gameRenderer.HideLoS = true;
			gameRenderer.ToggleCrt(false);
			this.ToggleDefaultLayer(cam, jship.DMGStatus == Ship.Damage.Derelict);
			List<int> layerlist = new List<int>();
			List<CondOwner> cos = ship.GetCOs(DataHandler.GetCondTrigger("TIsLootSpawner"), true, true, true);
			foreach (CondOwner condOwner in cos)
			{
				layerlist.Add(condOwner.gameObject.layer);
				condOwner.gameObject.layer = LayerMask.NameToLayer("Ship Offscreen");
			}
			string dictKey = (!CrewSim.bShipEdit) ? jship.strRegID : jship.strName;
			Dictionary<string, List<Tile>> screenshotTargetDict = ScreenshotUtil.BuildTargetDict(ship, dictKey, false);
			Dictionary<string, Texture2D> screenshotDict = new Dictionary<string, Texture2D>();
			foreach (KeyValuePair<string, List<Tile>> kvp in screenshotTargetDict)
			{
				Vector3 camPos = this.GetCenteredPosition(kvp.Value);
				cam.transform.position = new Vector3(camPos.x, camPos.y, cam.transform.position.z);
				cam.orthographicSize = camPos.z;
				gameRenderer.SetZoom(camPos.z);
				yield return null;
				Texture2D screenshot = this.TakeScreenShot(cam);
				if (screenshot != null)
				{
					screenshotDict.Add(kvp.Key, screenshot);
				}
			}
			cam.gameObject.SetActive(false);
			for (int i = 0; i < cos.Count; i++)
			{
				if (!(cos[i] == null) && !cos[i].bDestroyed)
				{
					cos[i].gameObject.layer = layerlist[i];
				}
			}
			DataHandler.dictShipImages[jship.strRegID] = screenshotDict;
			this._activeRun = null;
			yield break;
		}

		private IEnumerator GetScreenShot(Ship ship, string savePath, string fileName)
		{
			Camera cam = CrewSim.objInstance.ScreenShotCam;
			if (ship == null || cam == null || ship.LoadState <= Ship.Loaded.Shallow)
			{
				yield break;
			}
			GameRenderer gameRenderer = cam.GetComponent<GameRenderer>();
			cam.depth = -2f;
			cam.gameObject.SetActive(true);
			gameRenderer.HideLoS = true;
			gameRenderer.ToggleCrt(false);
			this.ToggleDefaultLayer(cam, ship.DMGStatus == Ship.Damage.Derelict);
			List<int> layerlist = new List<int>();
			List<CondOwner> cos = ship.GetCOs(DataHandler.GetCondTrigger("TIsLootSpawner"), true, true, true);
			foreach (CondOwner condOwner in cos)
			{
				layerlist.Add(condOwner.gameObject.layer);
				condOwner.gameObject.layer = LayerMask.NameToLayer("Ship Offscreen");
			}
			Dictionary<string, List<Tile>> screenshotTargetDict = ScreenshotUtil.BuildTargetDict(ship, fileName, true);
			Dictionary<string, Texture2D> screenshotDict = new Dictionary<string, Texture2D>();
			foreach (KeyValuePair<string, List<Tile>> kvp in screenshotTargetDict)
			{
				Vector3 camPos = this.GetCenteredPosition(kvp.Value);
				cam.transform.position = new Vector3(camPos.x, camPos.y, cam.transform.position.z);
				cam.orthographicSize = camPos.z;
				gameRenderer.SetZoom(camPos.z);
				yield return null;
				Texture2D screenshot = this.TakeScreenShot(cam);
				if (screenshot != null)
				{
					screenshotDict.Add(kvp.Key, screenshot);
				}
			}
			cam.gameObject.SetActive(false);
			for (int i = 0; i < cos.Count; i++)
			{
				if (!(cos[i] == null) && !cos[i].bDestroyed)
				{
					cos[i].gameObject.layer = layerlist[i];
				}
			}
			this.SaveToDisk(savePath, screenshotDict, false);
			this._activeRun = null;
			yield break;
		}

		public IEnumerator GetAsyncScreenShot(Ship asyncShip, Dictionary<string, Tuple<Texture2D, Texture2D>> targetDict, Vector2 textureSize)
		{
			Camera cam = CrewSim.objInstance.ScreenShotCam;
			if (asyncShip == null || cam == null || targetDict == null)
			{
				yield break;
			}
			GameRenderer gameRenderer = cam.GetComponent<GameRenderer>();
			cam.depth = -2f;
			cam.gameObject.SetActive(true);
			gameRenderer.HideLoS = true;
			this.ToggleDefaultLayer(cam, asyncShip.DMGStatus == Ship.Damage.Derelict);
			Vector3 camPos = this.GetCenteredPosition(asyncShip.aTiles);
			cam.transform.position = new Vector3(camPos.x, camPos.y, cam.transform.position.z);
			int widest = asyncShip.nCols;
			float shipAspec = (float)asyncShip.nRows / (float)asyncShip.nCols;
			if (asyncShip.nRows > asyncShip.nCols)
			{
				widest = asyncShip.nRows;
				shipAspec = (float)asyncShip.nCols / (float)asyncShip.nRows;
				cam.transform.Rotate(new Vector3(0f, 0f, 90f));
			}
			float orthoSize = (float)(widest + 10) * textureSize.y / textureSize.x + 5f;
			cam.orthographicSize = orthoSize;
			gameRenderer.SetZoom(orthoSize);
			AsyncRatingShip preShip = (AsyncRatingShip)asyncShip;
			foreach (FloatingPanel floatingPanel in preShip.poiLabels)
			{
				floatingPanel.Show(orthoSize);
			}
			yield return null;
			Tuple<Texture2D, Texture2D> screenShots = new Tuple<Texture2D, Texture2D>();
			screenShots.Item1 = this.TakeScreenShot(cam, textureSize);
			foreach (FloatingPanel floatingPanel2 in preShip.poiLabels)
			{
				floatingPanel2.Hide();
			}
			foreach (FloatingPanel floatingPanel3 in preShip.roomLabels)
			{
				floatingPanel3.Show(orthoSize);
			}
			preShip.ShowRoomTiles(true);
			yield return null;
			screenShots.Item2 = this.TakeScreenShot(cam, textureSize);
			targetDict[asyncShip.strRegID] = screenShots;
			preShip.ShowRoomTiles(false);
			cam.gameObject.SetActive(false);
			cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
			this._activeRun = null;
			yield break;
		}

		private void ToggleDefaultLayer(Camera cam, bool show)
		{
			if (show)
			{
				cam.cullingMask |= 1 << LayerMask.NameToLayer("Default");
			}
			else
			{
				cam.cullingMask &= ~(1 << LayerMask.NameToLayer("Default"));
			}
		}

		private static Dictionary<string, List<Tile>> BuildTargetDict(Ship ship, string name, bool singleImage)
		{
			Dictionary<string, List<Tile>> dictionary = new Dictionary<string, List<Tile>>();
			dictionary.Add(name, ship.aTiles);
			if (ship.aRooms == null || singleImage)
			{
				return dictionary;
			}
			foreach (Room room in ship.aRooms)
			{
				if (!room.Void && !room.GetRoomSpec().IsBlank && room.aTiles != null && room.aTiles.Count > 3)
				{
					string str = string.Empty;
					int num = 1;
					while (!dictionary.TryAdd(room.GetRoomSpec().strName + str, room.aTiles))
					{
						str = "_" + num;
						num++;
						if (num > 100)
						{
							break;
						}
					}
				}
			}
			return dictionary;
		}

		private void SaveToDisk(string pathToShipSubFolder, Dictionary<string, Texture2D> screenShotDict, bool deleteExistingFolder)
		{
			if (screenShotDict == null || screenShotDict.Count == 0)
			{
				return;
			}
			Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
			foreach (KeyValuePair<string, Texture2D> keyValuePair in screenShotDict)
			{
				if (!(keyValuePair.Value == null))
				{
					dictionary.Add(keyValuePair.Key, keyValuePair.Value.EncodeToPNG());
				}
			}
			this.SaveByteArrayToDisk(pathToShipSubFolder, dictionary, deleteExistingFolder);
		}

		public void SaveByteArrayToDisk(string pathToShipSubFolder, Dictionary<string, byte[]> screenShotDict, bool deleteExistingFolder = true)
		{
			if (string.IsNullOrEmpty(pathToShipSubFolder) || screenShotDict == null || screenShotDict.Count == 0)
			{
				return;
			}
			try
			{
				if (Directory.Exists(pathToShipSubFolder) && deleteExistingFolder)
				{
					Directory.Delete(pathToShipSubFolder, true);
					Directory.CreateDirectory(pathToShipSubFolder);
				}
				else
				{
					Directory.CreateDirectory(pathToShipSubFolder);
				}
				foreach (KeyValuePair<string, byte[]> keyValuePair in screenShotDict)
				{
					File.WriteAllBytes(pathToShipSubFolder + keyValuePair.Key + ".png", keyValuePair.Value);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Could not save screenshot to " + pathToShipSubFolder + " Reason: " + ex.Message);
			}
		}

		private Vector3 GetCenteredPosition(List<Tile> tiles)
		{
			Tile tile = null;
			Tile tile2 = null;
			foreach (Tile tile3 in tiles)
			{
				if (tile == null)
				{
					tile = tile3;
				}
				else if (tile2 == null)
				{
					tile2 = tile3;
				}
				else if (tile.tf.localPosition.x > tile3.tf.position.x || tile.tf.localPosition.y < tile3.tf.position.y)
				{
					tile = tile3;
				}
				else if (tile2.tf.localPosition.x < tile3.tf.position.x || tile2.tf.localPosition.y > tile3.tf.position.y)
				{
					tile2 = tile3;
				}
			}
			if (tile == null || tile2 == null)
			{
				return new Vector3(0f, 0f, 10f);
			}
			float z = Mathf.Max(Vector2.Distance(tile.tf.position, tile2.tf.position), 5f);
			Vector2 vector = Vector2.Lerp(tile.tf.position, tile2.tf.position, 0.5f);
			return new Vector3(vector.x, vector.y, z);
		}

		private Texture2D TakeScreenShot(Camera cam, Vector2 targetSize)
		{
			if (cam == null || !cam.gameObject.activeInHierarchy)
			{
				return null;
			}
			int num = (int)targetSize.x;
			int num2 = (int)targetSize.y;
			RenderTexture renderTexture = new RenderTexture(num, num2, 24);
			RenderTexture targetTexture = cam.targetTexture;
			cam.targetTexture = renderTexture;
			Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGB24, false);
			texture2D.name = "TakeScreenShot " + Guid.NewGuid().ToString();
			cam.Render();
			RenderTexture.active = renderTexture;
			texture2D.ReadPixels(new Rect(0f, 0f, (float)num, (float)num2), 0, 0);
			texture2D.Apply();
			cam.targetTexture = targetTexture;
			RenderTexture.active = targetTexture;
			UnityEngine.Object.Destroy(renderTexture);
			return texture2D;
		}

		private Texture2D TakeScreenShot(Camera cam)
		{
			if (cam == null || !cam.gameObject.activeInHierarchy)
			{
				return null;
			}
			int pixelWidth = cam.pixelWidth;
			int pixelHeight = cam.pixelHeight;
			RenderTexture renderTexture = new RenderTexture(pixelWidth, pixelHeight, 24);
			RenderTexture targetTexture = cam.targetTexture;
			cam.targetTexture = renderTexture;
			Texture2D texture2D = new Texture2D(800, 600, TextureFormat.RGB24, false);
			texture2D.name = "TakeScreenShot2 " + Guid.NewGuid().ToString();
			cam.Render();
			RenderTexture.active = renderTexture;
			texture2D.ReadPixels(new Rect((float)(pixelWidth - 800) / 2f, (float)(pixelHeight - 600) / 2f, 800f, 600f), 0, 0);
			texture2D.Apply();
			cam.targetTexture = targetTexture;
			RenderTexture.active = targetTexture;
			UnityEngine.Object.Destroy(renderTexture);
			return texture2D;
		}

		private Coroutine _activeRun;
	}
}
