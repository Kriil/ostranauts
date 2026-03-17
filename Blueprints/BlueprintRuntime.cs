using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using LitJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vectrosity;

namespace Ostranauts.Blueprints;

internal enum BlueprintMode
{
	Inactive,
	Selecting,
	Placing
}

internal static class BlueprintRuntime
{
	private static readonly AccessTools.FieldRef<CrewSim, Camera> CamMainRef =
		AccessTools.FieldRefAccess<CrewSim, Camera>("camMain");

	private static readonly System.Reflection.FieldInfo LineSelectRectField =
		AccessTools.Field(typeof(CrewSim), "lineSelectRect");

	private static readonly MethodInfo InstallFinishMethod =
		AccessTools.Method(typeof(CrewSim), "InstallFinish");

	private static readonly List<CondOwner> PreviewObjects = new List<CondOwner>();
	private static readonly Dictionary<string, JsonInstallable> InstallableCache = new Dictionary<string, JsonInstallable>();
	private static readonly Dictionary<string, float> PendingPlaceholderRotations = new Dictionary<string, float>();
	private static CondTrigger _installedTrigger;

	private static BlueprintMode _mode;
	private static BlueprintData _currentBlueprint;
	private static readonly List<BlueprintPart> _parts = new List<BlueprintPart>();
	private static int _rotationSteps;
	private static int _lastModeChangeFrame;
	private static string _selectedBlueprintFileName;

	internal static bool IsActive => _mode != BlueprintMode.Inactive;
	internal static bool IsPlacing => _mode == BlueprintMode.Placing;
	internal static string SelectedBlueprintFileName => _selectedBlueprintFileName ?? string.Empty;
	private static CondTrigger InstalledTrigger
	{
		get
		{
			if (_installedTrigger == null)
			{
				_installedTrigger = DataHandler.GetCondTrigger("TIsInstalled");
			}

			return _installedTrigger;
		}
	}

	internal static void Initialize()
	{
		_mode = BlueprintMode.Inactive;
		_parts.Clear();
		PreviewObjects.Clear();
		InstallableCache.Clear();
		PendingPlaceholderRotations.Clear();
		_selectedBlueprintFileName = string.Empty;
	}

	internal static void Shutdown()
	{
		ExitMode();
		PendingPlaceholderRotations.Clear();
	}

	internal static void StartSelectionMode()
	{
		if (CrewSim.objInstance == null)
		{
			Plugin.LogWarning("Blueprint mode requested before CrewSim exists.");
			return;
		}

		ExitMode();
		_mode = BlueprintMode.Selecting;
		_lastModeChangeFrame = Time.frameCount;
		CrewSim.objInstance.StartAction("GUIActionBlueprint.png");
		Plugin.LogInfo("Blueprint selection mode started.");
	}

	internal static void StartSelectionModeFromPda()
	{
		Plugin.LogInfo("Blueprint PDA button clicked.");
		StartSelectionMode();
	}

	internal static void SelectBlueprintFromDialog()
	{
		string path = BlueprintDialog.SelectBlueprintFile();
		if (string.IsNullOrEmpty(path))
		{
			Plugin.LogInfo("Blueprint file selection cancelled.");
			return;
		}

		TryStartPlacementFromFile(path);
	}

	internal static bool TryStartPlacementFromFile(string path)
	{
		if (path == null || path.Trim().Length == 0)
		{
			Plugin.LogWarning("Blueprint file load skipped: path was empty.");
			return false;
		}

		try
		{
			string fullPath = Path.GetFullPath(path);
			if (!File.Exists(fullPath))
			{
				Plugin.LogWarning("Blueprint file load skipped: file was not found at " + fullPath);
				return false;
			}

			BlueprintData blueprint = JsonMapper.ToObject<BlueprintData>(File.ReadAllText(fullPath));
			if (blueprint == null || blueprint.aItems == null || blueprint.aItems.Length == 0)
			{
				Plugin.LogWarning("Blueprint file load skipped: file contained no blueprint items. Path=" + fullPath);
				return false;
			}

			if (!TryBeginPlacement(blueprint, Path.GetFileName(fullPath), "file"))
			{
				return false;
			}

			Plugin.LogInfo("Loaded blueprint from file " + fullPath);
			return true;
		}
		catch (Exception ex)
		{
			Plugin.LogException("TryStartPlacementFromFile", ex);
			return false;
		}
	}

	internal static void HandleMouse(CrewSim crewSim)
	{
		if (crewSim == null)
		{
			return;
		}

		if (GUIActionKeySelector.commandEscape != null && GUIActionKeySelector.commandEscape.Down)
		{
			Plugin.LogInfo("Blueprint mode cancelled via Escape.");
			ExitMode();
			return;
		}

		if (Input.GetMouseButtonDown(1))
		{
			Plugin.LogInfo("Blueprint mode cancelled via right-click.");
			ExitMode();
			return;
		}

		switch (_mode)
		{
			case BlueprintMode.Selecting:
				HandleSelectingMouse(crewSim);
				break;
			case BlueprintMode.Placing:
				HandlePlacingMouse(crewSim);
				break;
		}
	}

	internal static void Tick(CrewSim crewSim)
	{
		if (crewSim == null || _mode == BlueprintMode.Inactive)
		{
			return;
		}

		UpdatePaintCursorPosition();

		if (_mode == BlueprintMode.Selecting)
		{
			if (crewSim.goPaintJob == null)
			{
				crewSim.StartAction("GUIActionBlueprint.png");
			}
			return;
		}

		if (_mode == BlueprintMode.Placing)
		{
			if (crewSim.goPaintJob == null)
			{
				crewSim.StartAction("GUIActionBlueprint.png");
			}
			UpdatePlacementPreview(crewSim);
		}
	}

	internal static void RotatePlacement()
	{
		if (_mode != BlueprintMode.Placing || _parts.Count == 0)
		{
			return;
		}

		_rotationSteps = (_rotationSteps + 1) % 4;
		_currentBlueprint.nRotationSteps = _rotationSteps;
		_lastModeChangeFrame = Time.frameCount;
		AudioManager.am?.PlayAudioEmitter("UIRotate", false, false);
	}

	internal static void ExitMode()
	{
		ClearSelectionRect();
		ClearPreviewObjects();
		_parts.Clear();
		_currentBlueprint = null;
		_rotationSteps = 0;
		_mode = BlueprintMode.Inactive;
		if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.FinishPaintingJob();
		}
	}

	internal static string MakeSafeFileName(string value)
	{
		if (value == null || value.Trim().Length == 0)
		{
			return "blueprint";
		}

		char[] invalidChars = Path.GetInvalidFileNameChars();
		char[] chars = value.ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			for (int j = 0; j < invalidChars.Length; j++)
			{
				if (chars[i] == invalidChars[j])
				{
					chars[i] = '_';
					break;
				}
			}
			if (chars[i] == ' ')
			{
				chars[i] = '_';
			}
		}

		return new string(chars);
	}

	private static void HandleSelectingMouse(CrewSim crewSim)
	{
		Camera camMain = CamMainRef(crewSim);
		if (camMain == null)
		{
			return;
		}

		if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
		{
			crewSim.vDragStart = camMain.ScreenToWorldPoint(Input.mousePosition);
			crewSim.vDragStartScreen = Input.mousePosition;
		}

		if (Input.GetMouseButton(0) && !IsPointerOverUi())
		{
			Vector3 delta = GetWorldMouse(camMain) - crewSim.vDragStart;
			if ((Mathf.Abs(delta.x) > 1f || Mathf.Abs(delta.y) > 1f) && GetSelectionRect(crewSim) == null)
			{
				VectorLine line = new VectorLine("BlueprintSelectionRect", new List<Vector2>(5), 1.5f, LineType.Continuous, Joins.Weld);
				line.color = Color.cyan;
				line.SetCanvas(CrewSim.CanvasManager.goCanvasGUI, false);
				SetSelectionRect(crewSim, line);
			}
		}

		if (Input.GetMouseButtonUp(0))
		{
			VectorLine line = GetSelectionRect(crewSim);
			if (line != null)
			{
				Bounds bounds = crewSim.GetViewportBounds(camMain, crewSim.vDragStart, GetWorldMouse(camMain));
				ClearSelectionRect();
				CaptureBlueprint(crewSim, bounds);
			}
		}
	}

	private static void HandlePlacingMouse(CrewSim crewSim)
	{
		if (Time.frameCount == _lastModeChangeFrame)
		{
			return;
		}

		if (Input.GetMouseButtonUp(0) && !IsPointerOverUi())
		{
			TryPlaceBlueprint(crewSim);
		}
	}

	private static void CaptureBlueprint(CrewSim crewSim, Bounds bounds)
	{
		try
		{
			List<BlueprintPart> parts = CollectBlueprintParts(bounds);
			if (parts.Count == 0)
			{
				Plugin.LogInfo("Blueprint selection contained no supported installables.");
				ExitMode();
				return;
			}

			QueueUninstallTasks(parts);
			_currentBlueprint = CreateBlueprintData(parts);
			_parts.Clear();
			_parts.AddRange(parts);
			_rotationSteps = 0;
			_currentBlueprint.nRotationSteps = 0;
			string path = BlueprintPersistence.Save(_currentBlueprint);
			SetSelectedBlueprintFileName(Path.GetFileName(path));
			Plugin.LogInfo("Saved blueprint to " + path);
			PreparePreviewObjects();
			_mode = BlueprintMode.Placing;
			_lastModeChangeFrame = Time.frameCount;
			UpdatePlacementPreview(crewSim);
		}
		catch (Exception ex)
		{
			Plugin.LogException("CaptureBlueprint", ex);
			ExitMode();
		}
	}

	private static List<BlueprintPart> CollectBlueprintParts(Bounds bounds)
	{
		HashSet<string> selectedTiles = CollectSelectedTileKeys(bounds);
		HashSet<string> seen = new HashSet<string>();
		List<BlueprintPart> results = new List<BlueprintPart>();

		foreach (Ship ship in CrewSim.GetAllLoadedShips())
		{
			if (ship == null)
			{
				continue;
			}

			foreach (CondOwner co in ship.GetICOs1(null, true, true, true))
			{
				if (co == null || string.IsNullOrEmpty(co.strID) || !seen.Add(co.strID))
				{
					continue;
				}

				Tile tile = co.ship == null ? null : co.ship.GetTileAtWorldCoords1(co.transform.position.x, co.transform.position.y, true, true);
				if (tile == null || !selectedTiles.Contains(GetTileKey(tile)))
				{
					continue;
				}

				if (GUIPDA.ctJobFilter != null && !GUIPDA.ctJobFilter.Triggered(co, null, true))
				{
					continue;
				}

				string uninstallAction = GetFirstValidJobAction(co, "Uninstall");
				if (string.IsNullOrEmpty(uninstallAction))
				{
					continue;
				}

				JsonInstallable installable = ResolveInstallableForPlacedObject(co, uninstallAction);
				if (installable == null || string.IsNullOrEmpty(installable.strStartInstall))
				{
					continue;
				}

				results.Add(new BlueprintPart
				{
					Item = new BlueprintItemData
					{
						strName = installable.strStartInstall,
						strSourceCODef = co.strCODef,
						fX = co.transform.position.x,
						fY = co.transform.position.y,
						fRotation = co.transform.rotation.eulerAngles.z
					},
					SourceCODef = co.strCODef,
					InstallInteractionName = installable.strInteractionName,
					UninstallInteractionName = uninstallAction,
					TargetCOID = co.strID
				});
			}
		}

		if (results.Count == 0)
		{
			return results;
		}

		float minX = float.MaxValue;
		float minY = float.MaxValue;
		for (int i = 0; i < results.Count; i++)
		{
			if (results[i].Item.fX < minX)
			{
				minX = results[i].Item.fX;
			}
			if (results[i].Item.fY < minY)
			{
				minY = results[i].Item.fY;
			}
		}

		for (int i = 0; i < results.Count; i++)
		{
			results[i].Item.fX -= minX;
			results[i].Item.fY -= minY;
		}

		results.Sort(CompareBlueprintParts);
		return results;
	}

	private static HashSet<string> CollectSelectedTileKeys(Bounds bounds)
	{
		HashSet<string> tileKeys = new HashSet<string>();
		foreach (Ship ship in CrewSim.GetAllLoadedShips())
		{
			if (ship == null || ship.aTiles == null)
			{
				continue;
			}

			for (int i = 0; i < ship.aTiles.Count; i++)
			{
				Tile tile = ship.aTiles[i];
				if (tile == null || !IsTileIncluded(tile, bounds))
				{
					continue;
				}

				tileKeys.Add(GetTileKey(tile));
			}
		}

		return tileKeys;
	}

	private static void QueueUninstallTasks(List<BlueprintPart> parts)
	{
		int queued = 0;
		foreach (BlueprintPart part in parts)
		{
			if (string.IsNullOrEmpty(part.UninstallInteractionName) || string.IsNullOrEmpty(part.TargetCOID))
			{
				continue;
			}

			CondOwner target = null;
			if (DataHandler.mapCOs != null && DataHandler.mapCOs.ContainsKey(part.TargetCOID))
			{
				target = DataHandler.mapCOs[part.TargetCOID];
			}
			if (target == null)
			{
				continue;
			}

			Task2 task = new Task2
			{
				strDuty = "Construct",
				strInteraction = part.UninstallInteractionName,
				strTargetCOID = target.strID,
				strName = "BlueprintUninstall" + target.strID
			};
			CrewSim.objInstance.workManager.AddTask(task, 1);
			queued++;
		}

		Plugin.LogInfo("Queued " + queued + " uninstall tasks for blueprint capture.");
	}

	private static BlueprintData CreateBlueprintData(List<BlueprintPart> parts)
	{
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		BlueprintItemData[] items = new BlueprintItemData[parts.Count];
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i].Item.fX > maxX)
			{
				maxX = parts[i].Item.fX;
			}
			if (parts[i].Item.fY > maxY)
			{
				maxY = parts[i].Item.fY;
			}

			items[i] = parts[i].Item.Clone();
		}

		int rotatedItemCount = 0;
		for (int i = 0; i < items.Length; i++)
		{
			if (Mathf.Abs(Mathf.Repeat(items[i].fRotation, 360f)) > 0.01f)
			{
				rotatedItemCount++;
			}
		}

		Plugin.LogInfo("Blueprint capture serialized " + items.Length + " items; " + rotatedItemCount + " have non-zero saved rotation.");

		return new BlueprintData
		{
			strName = "blueprint_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
			strSourceShip = CrewSim.shipCurrentLoaded?.strRegID,
			strCreatedAtUtc = DateTime.UtcNow.ToString("o"),
			nRotationSteps = 0,
			fOriginX = 0f,
			fOriginY = 0f,
			fWidth = maxX + 1f,
			fHeight = maxY + 1f,
			aItems = items
		};
	}

	private static void PreparePreviewObjects()
	{
		ClearPreviewObjects();
		foreach (BlueprintPart part in _parts)
		{
			CondOwner preview = DataHandler.GetCondOwner(part.Item.strName);
			if (preview == null)
			{
				continue;
			}

			preview.gameObject.name = "BlueprintPreview_" + part.Item.strName;
			preview.gameObject.SetActive(true);
			SetPreviewTint(preview, new Color(0.45f, 0.85f, 1f, 0.35f));
			PreviewObjects.Add(preview);
		}
	}

	private static void UpdatePlacementPreview(CrewSim crewSim)
	{
		if (_parts.Count == 0 || PreviewObjects.Count == 0)
		{
			return;
		}

		Vector2 anchor = GetPlacementAnchor(crewSim);
		Ship ship = FindTargetShip(anchor);
		bool fits = ship != null && BlueprintFits(ship, anchor);

		for (int i = 0; i < _parts.Count && i < PreviewObjects.Count; i++)
		{
			BlueprintPart part = _parts[i];
			CondOwner preview = PreviewObjects[i];
			if (preview == null)
			{
				continue;
			}

			Vector2 position = RotateAndOffset(part.Item.fX, part.Item.fY, _rotationSteps) + anchor;
			preview.transform.position = new Vector3(position.x, position.y, preview.transform.position.z);
			preview.transform.rotation = Quaternion.Euler(0f, 0f, GetPartRotation(part.Item.fRotation, _rotationSteps));
			Item item = preview.GetComponent<Item>();
			if (item != null)
			{
				item.fLastRotation = preview.transform.rotation.eulerAngles.z;
			}

			SetPreviewTint(preview, fits ? new Color(0.45f, 0.85f, 1f, 0.35f) : new Color(1f, 0.35f, 0.35f, 0.35f));
		}
	}

	private static bool TryPlaceBlueprint(CrewSim crewSim)
	{
		if (_currentBlueprint == null || _parts.Count == 0)
		{
			ExitMode();
			return false;
		}

		Vector2 anchor = GetPlacementAnchor(crewSim);
		Ship ship = FindTargetShip(anchor);
		if (ship == null || !BlueprintFits(ship, anchor))
		{
			Plugin.LogInfo("Blueprint placement blocked: area is not clear.");
			return false;
		}

		try
		{
			foreach (BlueprintPart part in _parts)
			{
				JsonInstallable installable = ResolvePlacementInstallable(part.Item.strName, part.SourceCODef);
				if (installable == null)
				{
					Plugin.LogWarning("Blueprint placement skipped part '" + part.Item.strName + "': no installable could be resolved.");
					continue;
				}

				Interaction interaction = DataHandler.GetInteraction(installable.strInteractionName, null, false);
				if (interaction == null)
				{
					continue;
				}

				interaction.objThem = DataHandler.GetCondOwner(installable.strActionCO);
				interaction.objThem.strPersistentCO = null;
				crewSim.InstallStart(interaction);

				if (crewSim.goSelPart == null)
				{
					continue;
				}

				Vector2 position = RotateAndOffset(part.Item.fX, part.Item.fY, _rotationSteps) + anchor;
				float rotation = GetPartRotation(part.Item.fRotation, _rotationSteps);
				crewSim.goSelPart.transform.position = new Vector3(position.x, position.y, crewSim.goSelPart.transform.position.z);
				ApplyPlacementRotation(crewSim.goSelPart.GetComponent<CondOwner>(), rotation);
				ApplyPlacementRotation(interaction.objThem, rotation);
				Item item = crewSim.goSelPart.GetComponent<Item>();
				if (item != null)
				{
					item.fLastRotation = rotation;
				}

				float selPartTransformRotation = crewSim.goSelPart.transform.rotation.eulerAngles.z;
				float selPartItemRotation = item != null ? item.fLastRotation : selPartTransformRotation;
				Item targetItem = interaction.objThem?.GetComponent<Item>();
				float targetTransformRotation = interaction.objThem != null ? interaction.objThem.transform.rotation.eulerAngles.z : 0f;
				float targetItemRotation = targetItem != null ? targetItem.fLastRotation : targetTransformRotation;
				Plugin.LogInfo(
					"Blueprint placement handoff '" + part.Item.strName +
					"': desired=" + rotation.ToString("0.##") +
					", selPart.transform=" + selPartTransformRotation.ToString("0.##") +
					", selPart.item=" + selPartItemRotation.ToString("0.##") +
					", target.transform=" + targetTransformRotation.ToString("0.##") +
					", target.item=" + targetItemRotation.ToString("0.##") + "."
				);

				InstallFinishMethod?.Invoke(crewSim, null);
			}

			ExitMode();
			return true;
		}
		catch (Exception ex)
		{
			Plugin.LogException("TryPlaceBlueprint", ex);
			ExitMode();
			return false;
		}
	}

	private static JsonInstallable ResolvePlacementInstallable(string startInstall, string sourceCoDef)
	{
		if (string.IsNullOrEmpty(startInstall))
		{
			return null;
		}

		JsonInstallable installable = Installables.GetJsonInstallable(startInstall);
		if (installable == null)
		{
			return null;
		}

		if (string.IsNullOrEmpty(installable.strActionCO))
		{
			return installable;
		}

		JsonCOOverlay overlay = TryGetOverlay(sourceCoDef);
		if (overlay == null)
		{
			overlay = TryGetOverlay(installable.strActionCO);
		}
		if (overlay == null)
		{
			return installable;
		}

		string modeSwitch = overlay.GetModeSwitch(installable.strStartInstall);
		if (string.IsNullOrEmpty(modeSwitch) || string.Equals(modeSwitch, installable.strStartInstall, StringComparison.Ordinal))
		{
			return installable;
		}

		JsonInstallable switchedInstallable = Installables.GetJsonInstallable(modeSwitch);
		if (switchedInstallable == null)
		{
			Plugin.LogWarning("Blueprint placement mode-switch from '" + installable.strStartInstall + "' to '" + modeSwitch + "' failed: no installable found.");
			return installable;
		}

		Plugin.LogInfo("Blueprint placement mode-switch applied: '" + installable.strStartInstall + "' -> '" + modeSwitch + "'.");
		return switchedInstallable;
	}

	private static bool TryBeginPlacement(BlueprintData blueprint, string fileName, string source)
	{
		if (CrewSim.objInstance == null)
		{
			Plugin.LogWarning("Blueprint " + source + " placement skipped: CrewSim does not exist yet.");
			return false;
		}

		List<BlueprintPart> loadedParts = new List<BlueprintPart>();
		for (int i = 0; i < blueprint.aItems.Length; i++)
		{
			BlueprintItemData item = blueprint.aItems[i];
			if (item == null || string.IsNullOrEmpty(item.strName))
			{
				continue;
			}

			loadedParts.Add(new BlueprintPart
			{
				Item = item.Clone(),
				SourceCODef = item.strSourceCODef
			});
		}

		if (loadedParts.Count == 0)
		{
			Plugin.LogWarning("Blueprint " + source + " placement skipped: no valid installables were found.");
			return false;
		}

		ExitMode();
		_currentBlueprint = blueprint;
		_parts.Clear();
		_parts.AddRange(loadedParts);
		_rotationSteps = ((blueprint.nRotationSteps % 4) + 4) % 4;
		_currentBlueprint.nRotationSteps = _rotationSteps;
		SetSelectedBlueprintFileName(fileName);
		PreparePreviewObjects();
		_mode = BlueprintMode.Placing;
		_lastModeChangeFrame = Time.frameCount;
		CrewSim.objInstance.StartAction("GUIActionBlueprint.png");
		UpdatePlacementPreview(CrewSim.objInstance);
		Plugin.LogInfo("Blueprint " + source + " placement started with " + loadedParts.Count + " parts.");
		return true;
	}

	private static void SetSelectedBlueprintFileName(string fileName)
	{
		_selectedBlueprintFileName = fileName ?? string.Empty;
		Patch_GUIPDA_BlueprintsShowJobPaintUI.RefreshSelectorUI();
	}

	private static JsonCOOverlay TryGetOverlay(string coDef)
	{
		if (string.IsNullOrEmpty(coDef) || DataHandler.dictCOOverlays == null || !DataHandler.dictCOOverlays.ContainsKey(coDef))
		{
			return null;
		}

		return DataHandler.dictCOOverlays[coDef];
	}

	private static bool BlueprintFits(Ship ship, Vector2 anchor)
	{
		if (ship == null)
		{
			return false;
		}

		HashSet<string> footprintTiles = new HashSet<string>();
		for (int index = 0; index < _parts.Count; index++)
		{
			if (index >= PreviewObjects.Count)
			{
				return false;
			}

			CondOwner preview = PreviewObjects[index];
			Item item = preview?.GetComponent<Item>();
			if (item == null)
			{
				return false;
			}

			Vector2 position = RotateAndOffset(_parts[index].Item.fX, _parts[index].Item.fY, _rotationSteps) + anchor;
			preview.transform.position = new Vector3(position.x, position.y, preview.transform.position.z);
			preview.transform.rotation = Quaternion.Euler(0f, 0f, GetPartRotation(_parts[index].Item.fRotation, _rotationSteps));
			item.fLastRotation = preview.transform.rotation.eulerAngles.z;

			Tile tile = ship.GetTileAtWorldCoords1(position.x, position.y, true, true);
			if (tile == null)
			{
				return false;
			}

			string tileKey = GetTileKey(tile);
			if (tileKey != null)
			{
				footprintTiles.Add(tileKey);
			}
		}

		List<CondOwner> installed = ship.GetCOs(InstalledTrigger, true, true, true);
		for (int i = 0; i < installed.Count; i++)
		{
			CondOwner co = installed[i];
			if (co == null || co.ship == null)
			{
				continue;
			}

			Tile tile = co.ship.GetTileAtWorldCoords1(co.transform.position.x, co.transform.position.y, true, true);
			if (tile == null)
			{
				continue;
			}

			string tileKey = GetTileKey(tile);
			if (tileKey != null && footprintTiles.Contains(tileKey))
			{
				return false;
			}
		}

		return footprintTiles.Count > 0;
	}

	internal static void ApplyPlacementRotation(CondOwner target, float rotation)
	{
		if (target == null)
		{
			return;
		}

		target.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
		Item item = target.GetComponent<Item>();
		if (item != null)
		{
			item.fLastRotation = rotation;
			item.ResetTransforms(target.transform.position.x, target.transform.position.y);
		}
	}

	internal static void RegisterPlaceholderRotation(CondOwner placeholder, float rotation)
	{
		if (placeholder == null || string.IsNullOrEmpty(placeholder.strID))
		{
			return;
		}

		PendingPlaceholderRotations[placeholder.strID] = rotation;
	}

	internal static bool TryGetPlaceholderRotation(string placeholderId, out float rotation)
	{
		if (string.IsNullOrEmpty(placeholderId))
		{
			rotation = 0f;
			return false;
		}

		return PendingPlaceholderRotations.TryGetValue(placeholderId, out rotation);
	}

	internal static void ClearPlaceholderRotation(string placeholderId)
	{
		if (string.IsNullOrEmpty(placeholderId))
		{
			return;
		}

		PendingPlaceholderRotations.Remove(placeholderId);
	}

	private static string GetFirstValidJobAction(CondOwner co, string jobType)
	{
		List<string> actions = co.GetJobActions(jobType);
		if (actions == null || actions.Count == 0)
		{
			return null;
		}

		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		foreach (string actionName in actions)
		{
			Interaction interaction = DataHandler.GetInteraction(actionName, null, false);
			if (interaction != null && (selectedCrew == null || interaction.Triggered(selectedCrew, co, false, true, false, true, null)))
			{
				return actionName;
			}
		}

		return null;
	}

	private static JsonInstallable ResolveInstallableForPlacedObject(CondOwner co, string uninstallAction)
	{
		if (co == null || string.IsNullOrEmpty(uninstallAction))
		{
			return null;
		}

		if (InstallableCache.TryGetValue(co.strCODef, out JsonInstallable cached))
		{
			return cached;
		}

		JsonInstallable exactInstallable = FindInstallableByStartInstall(co.strCODef);
		if (exactInstallable != null)
		{
			InstallableCache[co.strCODef] = exactInstallable;
			return exactInstallable;
		}

		JsonInstallable actionMatchedInstallable = FindInstallableFromUninstallAction(uninstallAction);
		if (actionMatchedInstallable != null)
		{
			InstallableCache[co.strCODef] = actionMatchedInstallable;
			return actionMatchedInstallable;
		}

		string uninstallInstallableName = uninstallAction;
		if (uninstallInstallableName.StartsWith("ACT"))
		{
			uninstallInstallableName = uninstallInstallableName.Substring(3);
		}
		if (uninstallInstallableName.EndsWith("Allow"))
		{
			uninstallInstallableName = uninstallInstallableName.Substring(0, uninstallInstallableName.Length - 5);
		}

		JsonInstallable uninstallInstallable = null;
		if (!DataHandler.dictInstallables.TryGetValue(uninstallInstallableName, out uninstallInstallable))
		{
			return null;
		}

		string looseDef = uninstallInstallable.aLootCOs == null || uninstallInstallable.aLootCOs.Length == 0
			? null
			: uninstallInstallable.aLootCOs[0];
		if (string.IsNullOrEmpty(looseDef))
		{
			return null;
		}

		JsonInstallable exact = null;
		JsonInstallable fallback = null;
		foreach (JsonInstallable installable in DataHandler.dictInstallables.Values)
		{
			if (installable == null || installable.strJobType != "install" || installable.strActionCO != looseDef || string.IsNullOrEmpty(installable.strStartInstall))
			{
				continue;
			}

			if (installable.aLootCOs != null && Array.IndexOf(installable.aLootCOs, co.strCODef) >= 0)
			{
				exact = installable.Clone();
				break;
			}

			if (fallback == null)
			{
				fallback = installable.Clone();
			}
		}

		JsonInstallable result = exact ?? fallback;
		if (result != null)
		{
			InstallableCache[co.strCODef] = result;
			if (result.strStartInstall != co.strCODef)
			{
				Plugin.LogWarning("Blueprint capture fell back from installed '" + co.strCODef + "' to installer '" + result.strStartInstall + "'.");
			}
		}

		return result;
	}

	private static JsonInstallable FindInstallableByStartInstall(string installedCoDef)
	{
		if (string.IsNullOrEmpty(installedCoDef))
		{
			return null;
		}

		foreach (JsonInstallable installable in DataHandler.dictInstallables.Values)
		{
			if (installable != null &&
				installable.strJobType == "install" &&
				installable.strStartInstall == installedCoDef)
			{
				return installable.Clone();
			}
		}

		return null;
	}

	private static JsonInstallable FindInstallableFromUninstallAction(string uninstallAction)
	{
		if (string.IsNullOrEmpty(uninstallAction))
		{
			return null;
		}

		string installableName = uninstallAction;
		if (installableName.StartsWith("ACT"))
		{
			installableName = installableName.Substring(3);
		}
		if (installableName.EndsWith("Allow"))
		{
			installableName = installableName.Substring(0, installableName.Length - 5);
		}
		if (!installableName.EndsWith("Uninstall"))
		{
			return null;
		}

		installableName = installableName.Substring(0, installableName.Length - "Uninstall".Length) + "Install";
		if (!DataHandler.dictInstallables.TryGetValue(installableName, out JsonInstallable installable))
		{
			return null;
		}

		return installable != null && installable.strJobType == "install"
			? installable.Clone()
			: null;
	}

	private static Vector2 GetPlacementAnchor(CrewSim crewSim)
	{
		Camera camMain = CamMainRef(crewSim);
		Vector3 mouse = camMain == null ? Vector3.zero : GetWorldMouse(camMain);
		return new Vector2(TileUtils.GridAlign(mouse.x), TileUtils.GridAlign(mouse.y));
	}

	private static Ship FindTargetShip(Vector2 anchor)
	{
		foreach (Ship ship in CrewSim.GetAllLoadedShips())
		{
			if (ship?.GetTileAtWorldCoords1(anchor.x, anchor.y, true, true) != null)
			{
				return ship;
			}
		}

		return CrewSim.shipCurrentLoaded;
	}

	private static Vector3 GetWorldMouse(Camera camMain)
	{
		return camMain.ScreenToWorldPoint(Input.mousePosition);
	}

	private static Vector2 RotateAndOffset(float x, float y, int rotationSteps)
	{
		switch (rotationSteps & 3)
		{
			case 1:
				return new Vector2(y, -x);
			case 2:
				return new Vector2(-x, -y);
			case 3:
				return new Vector2(-y, x);
			default:
				return new Vector2(x, y);
		}
	}

	private static float GetPartRotation(float baseRotation, int rotationSteps)
	{
		return MathUtils.NormalizeAngleDegrees(baseRotation - rotationSteps * 90f);
	}

	private static bool IsTileIncluded(Tile tile, Bounds bounds)
	{
		Vector3 pos = tile.transform.position;
		float tileMinX = pos.x - 0.5f;
		float tileMaxX = pos.x + 0.5f;
		float tileMinY = pos.y - 0.5f;
		float tileMaxY = pos.y + 0.5f;

		bool intersectsFromLeft = bounds.min.x < tileMaxX;
		bool intersectsFromBottom = bounds.min.y < tileMaxY;
		bool reachesCenterFromRight = bounds.max.x >= pos.x;
		bool reachesCenterFromTop = bounds.max.y >= pos.y;

		return intersectsFromLeft && intersectsFromBottom && reachesCenterFromRight && reachesCenterFromTop;
	}

	private static bool IsPointerOverUi()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}

	private static void UpdatePaintCursorPosition()
	{
		if (CrewSim.objInstance == null || CrewSim.objInstance.goPaintJob == null || CrewSim.CanvasManager == null)
		{
			return;
		}

		Canvas canvas = CrewSim.CanvasManager.goCanvasGUI.GetComponent<Canvas>();
		if (canvas == null)
		{
			return;
		}

		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			canvas.transform as RectTransform,
			Input.mousePosition,
			canvas.worldCamera,
			out localPoint
		);
		CrewSim.objInstance.goPaintJob.transform.localPosition = localPoint;
	}

	private static void ClearSelectionRect()
	{
		if (CrewSim.objInstance == null)
		{
			return;
		}

		VectorLine line = GetSelectionRect(CrewSim.objInstance);
		if (line != null)
		{
			VectorLine.Destroy(ref line);
			SetSelectionRect(CrewSim.objInstance, null);
		}
	}

	private static void ClearPreviewObjects()
	{
		for (int i = 0; i < PreviewObjects.Count; i++)
		{
			if (PreviewObjects[i] != null)
			{
				UnityEngine.Object.Destroy(PreviewObjects[i].gameObject);
			}
		}
		PreviewObjects.Clear();
	}

	private static void SetPreviewTint(CondOwner preview, Color color)
	{
		if (preview == null)
		{
			return;
		}

		foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true))
		{
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(block);
			block.SetColor("_Color", color);
			renderer.SetPropertyBlock(block);
		}
	}

	private static VectorLine GetSelectionRect(CrewSim crewSim)
	{
		return crewSim == null ? null : LineSelectRectField?.GetValue(crewSim) as VectorLine;
	}

	private static void SetSelectionRect(CrewSim crewSim, VectorLine line)
	{
		LineSelectRectField?.SetValue(crewSim, line);
	}

	private static string GetTileKey(Tile tile)
	{
		if (tile == null || tile.coProps == null || tile.coProps.ship == null)
		{
			return null;
		}

		return tile.coProps.ship.strRegID + ":" + tile.Index;
	}

	private static int CompareBlueprintParts(BlueprintPart a, BlueprintPart b)
	{
		if (a == null && b == null)
		{
			return 0;
		}
		if (a == null)
		{
			return -1;
		}
		if (b == null)
		{
			return 1;
		}
		if (a.Item.fX < b.Item.fX)
		{
			return -1;
		}
		if (a.Item.fX > b.Item.fX)
		{
			return 1;
		}
		if (a.Item.fY < b.Item.fY)
		{
			return -1;
		}
		if (a.Item.fY > b.Item.fY)
		{
			return 1;
		}
		return 0;
	}
}
