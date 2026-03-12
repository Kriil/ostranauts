using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

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

	private static readonly AccessTools.MethodDelegate<Action<CrewSim>> InstallFinishDelegate =
		AccessTools.MethodDelegate<Action<CrewSim>>(AccessTools.Method(typeof(CrewSim), "InstallFinish"));

	private static readonly List<CondOwner> PreviewObjects = new List<CondOwner>();
	private static readonly Dictionary<string, JsonInstallable> InstallableCache = new Dictionary<string, JsonInstallable>();

	private static BlueprintMode _mode;
	private static BlueprintData _currentBlueprint;
	private static readonly List<BlueprintPart> _parts = new List<BlueprintPart>();
	private static int _rotationSteps;
	private static int _lastModeChangeFrame;

	internal static bool IsActive => _mode != BlueprintMode.Inactive;
	internal static bool IsPlacing => _mode == BlueprintMode.Placing;

	internal static void Initialize()
	{
		_mode = BlueprintMode.Inactive;
		_parts.Clear();
		PreviewObjects.Clear();
		InstallableCache.Clear();
	}

	internal static void Shutdown()
	{
		ExitMode();
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
		CrewSim.objInstance.StartAction("GUIActionRepair.png");
		Plugin.LogInfo("Blueprint selection mode started.");
	}

	internal static void HandleMouse(CrewSim crewSim)
	{
		if (crewSim == null)
		{
			return;
		}

		if (Input.GetMouseButtonDown(1))
		{
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

		if (_mode == BlueprintMode.Selecting)
		{
			if (crewSim.goPaintJob == null)
			{
				crewSim.StartAction("GUIActionRepair.png");
			}
			return;
		}

		if (_mode == BlueprintMode.Placing)
		{
			if (crewSim.goPaintJob == null)
			{
				crewSim.StartAction("GUIActionRepair.png");
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
		if (string.IsNullOrWhiteSpace(value))
		{
			return "blueprint";
		}

		char[] invalidChars = Path.GetInvalidFileNameChars();
		char[] chars = value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray();
		return new string(chars).Replace(' ', '_');
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
		HashSet<string> seen = new HashSet<string>();
		List<BlueprintPart> results = new List<BlueprintPart>();
		List<CondOwner> selected = new List<CondOwner>();

		foreach (Ship ship in CrewSim.GetAllLoadedShips())
		{
			if (ship == null)
			{
				continue;
			}

			foreach (CondOwner co in ship.GetICOs1(null, false, true, true))
			{
				if (co == null || string.IsNullOrEmpty(co.strID) || !seen.Add(co.strID))
				{
					continue;
				}

				if (!bounds.Contains(co.transform.position))
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

				selected.Add(co);
				results.Add(new BlueprintPart
				{
					Item = new JsonItem
					{
						strName = installable.strStartInstall,
						fX = co.transform.position.x,
						fY = co.transform.position.y,
						fRotation = co.transform.rotation.eulerAngles.z
					},
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

		float minX = results.Min(x => x.Item.fX);
		float minY = results.Min(x => x.Item.fY);
		foreach (BlueprintPart part in results)
		{
			part.Item.fX -= minX;
			part.Item.fY -= minY;
		}

		return results.OrderBy(x => x.Item.fX).ThenBy(x => x.Item.fY).ToList();
	}

	private static void QueueUninstallTasks(List<BlueprintPart> parts)
	{
		foreach (BlueprintPart part in parts)
		{
			if (string.IsNullOrEmpty(part.UninstallInteractionName) || string.IsNullOrEmpty(part.TargetCOID))
			{
				continue;
			}

			CondOwner target = DataHandler.GetCondOwner(part.TargetCOID);
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
		}
	}

	private static BlueprintData CreateBlueprintData(List<BlueprintPart> parts)
	{
		float maxX = parts.Max(x => x.Item.fX);
		float maxY = parts.Max(x => x.Item.fY);
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
			aItems = parts.Select(x => x.Item.Clone()).ToArray()
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
			preview.transform.rotation = Quaternion.Euler(0f, 0f, part.Item.fRotation + _rotationSteps * 90f);
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
				JsonInstallable installable = Installables.GetJsonInstallable(part.Item.strName);
				if (installable == null)
				{
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
				crewSim.goSelPart.transform.position = new Vector3(position.x, position.y, crewSim.goSelPart.transform.position.z);
				crewSim.goSelPart.transform.rotation = Quaternion.Euler(0f, 0f, part.Item.fRotation + _rotationSteps * 90f);
				Item item = crewSim.goSelPart.GetComponent<Item>();
				if (item != null)
				{
					item.fLastRotation = crewSim.goSelPart.transform.rotation.eulerAngles.z;
				}

				InstallFinishDelegate(crewSim);
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

	private static bool BlueprintFits(Ship ship, Vector2 anchor)
	{
		if (ship == null)
		{
			return false;
		}

		foreach (int index in Enumerable.Range(0, _parts.Count))
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
			preview.transform.rotation = Quaternion.Euler(0f, 0f, _parts[index].Item.fRotation + _rotationSteps * 90f);
			item.fLastRotation = preview.transform.rotation.eulerAngles.z;
			if (!item.CheckFit(preview.transform.position, ship, null, null))
			{
				return false;
			}
		}

		return true;
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

		if (!DataHandler.dictInstallables2.TryGetValue(uninstallAction, out JsonInstallable uninstallInstallable))
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

			fallback ??= installable.Clone();
		}

		JsonInstallable result = exact ?? fallback;
		if (result != null)
		{
			InstallableCache[co.strCODef] = result;
		}

		return result;
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

	private static bool IsPointerOverUi()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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
}
