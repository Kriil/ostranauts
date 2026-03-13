using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ConstructionTweaks;

[HarmonyPatch(typeof(CrewSim), "Update")]
public static class Patch_DragSelectionOverlay
{
	private static readonly AccessTools.FieldRef<CrewSim, object> LineSelectRectRef =
		AccessTools.FieldRefAccess<CrewSim, object>("lineSelectRect");

	private static readonly AccessTools.FieldRef<CrewSim, Camera> CamMainRef =
		AccessTools.FieldRefAccess<CrewSim, Camera>("camMain");

	private static readonly List<Tile> TilePool = new List<Tile>();
	private static GameObject _tileRoot;
	private static GameObject _labelRoot;
	private static Text _labelText;
	private static RectTransform _labelRect;

	[HarmonyPostfix]
	private static void UpdatePostfix(CrewSim __instance)
	{
		if (Plugin.DragSelectionOverlay == null || !Plugin.DragSelectionOverlay.Value)
		{
			HideOverlay();
			return;
		}

		if (__instance == null || CrewSim.shipCurrentLoaded == null || LineSelectRectRef(__instance) == null)
		{
			HideOverlay();
			return;
		}

		Camera camMain = CamMainRef(__instance);
		if (camMain == null || CrewSim.CanvasManager == null || CrewSim.CanvasManager.goCanvasGUI == null)
		{
			HideOverlay();
			return;
		}

		Bounds bounds = __instance.GetViewportBounds(
			camMain,
			__instance.vDragStart,
			camMain.ScreenToWorldPoint(Input.mousePosition)
		);

		List<Tile> selectedTiles = CollectTiles(bounds);
		if (selectedTiles.Count == 0)
		{
			HideOverlay();
			return;
		}

		GetTileDimensions(selectedTiles, out int width, out int height);
		UpdateTileOverlay(selectedTiles);
		UpdateLabel(__instance, camMain, width, height);
	}

	private static List<Tile> CollectTiles(Bounds bounds)
	{
		List<Tile> tiles = new List<Tile>();
		foreach (Ship ship in CrewSim.GetAllLoadedShips())
		{
			if (ship?.aTiles == null)
			{
				continue;
			}

				foreach (Tile tile in ship.aTiles)
				{
					if (tile == null || !IsTileIncluded(tile, bounds))
					{
						continue;
					}

				tiles.Add(tile);
			}
		}

		return tiles;
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

	private static void GetTileDimensions(List<Tile> selectedTiles, out int width, out int height)
	{
		int minX = int.MaxValue;
		int maxX = int.MinValue;
		int minY = int.MaxValue;
		int maxY = int.MinValue;

		for (int i = 0; i < selectedTiles.Count; i++)
		{
			Vector3 pos = selectedTiles[i].transform.position;
			int x = Mathf.RoundToInt(pos.x);
			int y = Mathf.RoundToInt(pos.y);
			minX = Mathf.Min(minX, x);
			maxX = Mathf.Max(maxX, x);
			minY = Mathf.Min(minY, y);
			maxY = Mathf.Max(maxY, y);
		}

		width = maxX - minX + 1;
		height = maxY - minY + 1;
	}

	private static void UpdateTileOverlay(List<Tile> selectedTiles)
	{
		EnsureTileRoot();
		EnsureTilePool(selectedTiles.Count);

		for (int i = 0; i < selectedTiles.Count; i++)
		{
			Tile source = selectedTiles[i];
			if (source == null || i >= TilePool.Count)
			{
				continue;
			}

			Tile preview = TilePool[i];
			if (preview == null)
			{
				continue;
			}

			preview.transform.position = new Vector3(source.transform.position.x, source.transform.position.y, -8f);
			preview.SetMat(Item.strFit);
			preview.SetColor(new Color(1f, 1f, 1f, 0.12f));
			preview.gameObject.SetActive(true);
		}

		for (int i = selectedTiles.Count; i < TilePool.Count; i++)
		{
			if (TilePool[i] != null)
			{
				TilePool[i].gameObject.SetActive(false);
			}
		}
	}

	private static void UpdateLabel(CrewSim crewSim, Camera camMain, int width, int height)
	{
		EnsureLabel();
		if (_labelRoot == null || _labelRect == null || _labelText == null)
		{
			return;
		}

		_labelText.text = width + " x " + height + " (" + width * height + ") tiles";
		Vector3 dragStartScreen = camMain.WorldToScreenPoint(crewSim.vDragStart);
		Vector2 centerScreen = ((Vector2)dragStartScreen + (Vector2)Input.mousePosition) * 0.5f;
		Canvas canvas = CrewSim.CanvasManager.goCanvasGUI.GetComponent<Canvas>();
		RectTransform canvasRect = canvas.transform as RectTransform;
		if (canvasRect == null)
		{
			return;
		}

		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			canvasRect,
			centerScreen,
			crewSim.UICamera,
			out Vector2 localPoint
		);

		_labelRect.anchoredPosition = localPoint;
		_labelRoot.SetActive(true);
	}

	private static void HideOverlay()
	{
		if (_labelRoot != null)
		{
			_labelRoot.SetActive(false);
		}

		for (int i = 0; i < TilePool.Count; i++)
		{
			if (TilePool[i] != null)
			{
				TilePool[i].gameObject.SetActive(false);
			}
		}
	}

	private static void EnsureTileRoot()
	{
		if (_tileRoot != null)
		{
			return;
		}

		_tileRoot = new GameObject("ConstructionTweaks Drag Selection Tiles");
	}

	private static void EnsureTilePool(int count)
	{
		while (TilePool.Count < count)
		{
			GameObject tilePrefab = Resources.Load<GameObject>("prefabQuadTile");
			if (tilePrefab == null)
			{
				Plugin.Log?.LogWarning("DragSelectionOverlay could not load prefabQuadTile.");
				return;
			}

			GameObject tileObject = Object.Instantiate(tilePrefab);
			tileObject.transform.SetParent(_tileRoot.transform, false);
			Tile tile = tileObject.GetComponent<Tile>();
			if (tile == null)
			{
				Plugin.Log?.LogWarning("DragSelectionOverlay spawned a tile preview without a Tile component.");
				Object.Destroy(tileObject);
				return;
			}

			tile.SetMat(Item.strFit);
			tile.SetColor(new Color(1f, 1f, 1f, 0.12f));
			tileObject.SetActive(false);
			TilePool.Add(tile);
		}
	}

	private static void EnsureLabel()
	{
		if (_labelRoot != null && _labelRect != null && _labelText != null)
		{
			return;
		}

		if (CrewSim.CanvasManager == null || CrewSim.CanvasManager.goCanvasGUI == null)
		{
			return;
		}

		GameObject canvas = CrewSim.CanvasManager.goCanvasGUI;
		_labelRoot = new GameObject("ConstructionTweaks Drag Selection Label");
		_labelRoot.transform.SetParent(canvas.transform, false);

		Image background = _labelRoot.AddComponent<Image>();
		background.color = new Color(0f, 0f, 0f, 0.35f);

		_labelRect = _labelRoot.GetComponent<RectTransform>();
		_labelRect.anchorMin = new Vector2(0.5f, 0.5f);
		_labelRect.anchorMax = new Vector2(0.5f, 0.5f);
		_labelRect.pivot = new Vector2(0.5f, 0.5f);
		_labelRect.sizeDelta = new Vector2(120f, 24f);

		GameObject textObject = new GameObject("Text");
		textObject.transform.SetParent(_labelRoot.transform, false);
		_labelText = textObject.AddComponent<Text>();
		_labelText.alignment = TextAnchor.MiddleCenter;
		_labelText.color = new Color(1f, 1f, 1f, 0.95f);
		_labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		_labelText.fontSize = 14;
		_labelText.resizeTextForBestFit = true;
		_labelText.resizeTextMinSize = 10;
		_labelText.resizeTextMaxSize = 16;

		RectTransform textRect = textObject.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = Vector2.zero;
		textRect.offsetMax = Vector2.zero;

		_labelRoot.SetActive(false);
	}
}
