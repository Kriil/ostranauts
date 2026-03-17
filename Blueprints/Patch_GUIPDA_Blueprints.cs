using HarmonyLib;
using Ostranauts.UI.PDA;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.Blueprints;

[HarmonyPatch(typeof(GUIPDA), "ShowJobPaintUI")]
public static class Patch_GUIPDA_BlueprintsShowJobPaintUI
{
	private const string BlueprintButtonName = "GUIJobItem_Blueprint";
	private const string BlueprintSelectorRootName = "BlueprintSelectorPanel";
	private const string BlueprintSelectorHeaderName = "BlueprintSelectorHeader";
	private const string BlueprintSelectorDisplayName = "BlueprintSelectorFileName";
	private const string BlueprintSelectorButtonName = "BlueprintSelectorButton";

	private static readonly AccessTools.FieldRef<GUIPDA, GUIJobItem> PrefabGUIJobItemRef =
		AccessTools.FieldRefAccess<GUIPDA, GUIJobItem>("prefabGUIJobItem");

	private static readonly AccessTools.FieldRef<GUIJobItem, TMP_Text> TitleRef =
		AccessTools.FieldRefAccess<GUIJobItem, TMP_Text>("_title");

	private static TMP_Text _selectorFileNameText;

	[HarmonyPostfix]
	private static void Postfix(GUIPDA __instance, string btn)
	{
		if (__instance == null || btn != "actions")
		{
			return;
		}

		GameObject jobTypes = __instance.goJobTypes;
		if (jobTypes == null)
		{
			Plugin.LogWarning("Blueprint PDA injection skipped: goJobTypes was null.");
			return;
		}

		GUIJobItem prefab = PrefabGUIJobItemRef(__instance);
		if (prefab == null)
		{
			Plugin.LogWarning("Blueprint PDA injection skipped: prefabGUIJobItem was null.");
			return;
		}

		Transform parent = jobTypes.transform;
		if (parent.Find(BlueprintButtonName) == null)
		{
			GUIJobItem blueprintButton = Object.Instantiate(prefab, parent);
			blueprintButton.name = BlueprintButtonName;
			blueprintButton.SetData("BLUE", "GUIActionBlueprint", BlueprintRuntime.StartSelectionModeFromPda);
			Plugin.LogInfo("Inserted Blueprint PDA action button after the vanilla jobs actions.");
		}
		else
		{
			Plugin.LogInfo("Blueprint PDA button already present for this rebuild; skipping duplicate insert.");
		}

		EnsureBlueprintSelectorUi(__instance, prefab);
	}

	internal static void RefreshSelectorUI()
	{
		if (_selectorFileNameText != null)
		{
			_selectorFileNameText.text = BlueprintRuntime.SelectedBlueprintFileName;
		}
	}

	private static void EnsureBlueprintSelectorUi(GUIPDA pda, GUIJobItem prefab)
	{
		RectTransform filterPanel = pda.transform.Find("pnlJobs/pnlJobFilters") as RectTransform;
		if (filterPanel == null)
		{
			Plugin.LogWarning("Blueprint selector UI injection skipped: pnlJobFilters was null.");
			return;
		}

		TMP_Text textTemplate = TitleRef(prefab);
		if (textTemplate == null)
		{
			Plugin.LogWarning("Blueprint selector UI injection skipped: GUIJobItem title template was null.");
			return;
		}

		RectTransform lastToggle = pda.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkLoose") as RectTransform;
		Transform existingRoot = filterPanel.Find(BlueprintSelectorRootName);
		if (existingRoot == null)
		{
			GameObject selectorRoot = new GameObject(
				BlueprintSelectorRootName,
				typeof(RectTransform),
				typeof(Image),
				typeof(VerticalLayoutGroup)
			);
			selectorRoot.transform.SetParent(filterPanel, false);

			RectTransform rootRect = selectorRoot.GetComponent<RectTransform>();
			PositionSelectorRoot(rootRect, lastToggle);

			Image rootImage = selectorRoot.GetComponent<Image>();
			rootImage.color = new Color(0.08f, 0.12f, 0.18f, 0.94f);

			VerticalLayoutGroup layout = selectorRoot.GetComponent<VerticalLayoutGroup>();
			layout.padding = new RectOffset(8, 8, 6, 6);
			layout.spacing = 6f;
			layout.childAlignment = TextAnchor.UpperCenter;
			layout.childControlWidth = true;
			layout.childControlHeight = false;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;

			GameObject fileBox = new GameObject(BlueprintSelectorDisplayName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
			fileBox.transform.SetParent(selectorRoot.transform, false);
			fileBox.GetComponent<Image>().color = new Color(0.15f, 0.19f, 0.27f, 0.98f);
			LayoutElement fileLayout = fileBox.GetComponent<LayoutElement>();
			fileLayout.preferredWidth = 0f;
			fileLayout.flexibleWidth = 0f;
			fileLayout.minWidth = 0f;
			AddLayoutElement(fileBox, 28f);

			_selectorFileNameText = CreateText(textTemplate, fileBox.transform, "Label", BlueprintRuntime.SelectedBlueprintFileName, TextAlignmentOptions.Left);
			ConfigureFillRect(_selectorFileNameText.rectTransform, 8f, 8f, 4f, 4f);
			_selectorFileNameText.enableWordWrapping = false;
			_selectorFileNameText.overflowMode = TextOverflowModes.Ellipsis;

			GameObject buttonObject = new GameObject(BlueprintSelectorButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
			buttonObject.transform.SetParent(selectorRoot.transform, false);
			Image buttonImage = buttonObject.GetComponent<Image>();
			buttonImage.color = new Color(0.22f, 0.35f, 0.49f, 0.98f);
			Button button = buttonObject.GetComponent<Button>();
			button.targetGraphic = buttonImage;
			button.transition = Selectable.Transition.ColorTint;
			ColorBlock colors = button.colors;
			colors.normalColor = new Color(0.22f, 0.35f, 0.49f, 0.98f);
			colors.highlightedColor = new Color(0.30f, 0.46f, 0.63f, 1f);
			colors.pressedColor = new Color(0.16f, 0.26f, 0.38f, 1f);
			colors.disabledColor = new Color(0.12f, 0.15f, 0.20f, 0.6f);
			button.colors = colors;
			button.onClick.AddListener(BlueprintRuntime.SelectBlueprintFromDialog);
			AddLayoutElement(buttonObject, 32f);

			TMP_Text buttonText = CreateText(textTemplate, buttonObject.transform, "Label", "Select Blueprint", TextAlignmentOptions.Center);
			ConfigureFillRect(buttonText.rectTransform, 8f, 8f, 4f, 4f);

			Plugin.LogInfo("Inserted Blueprint selector UI under PDA job filters.");
			existingRoot = selectorRoot.transform;
		}
		else
		{
			RectTransform rootRect = existingRoot as RectTransform;
			PositionSelectorRoot(rootRect, lastToggle);
		}

		Transform display = existingRoot.Find(BlueprintSelectorDisplayName);
		if (display != null && _selectorFileNameText == null)
		{
			TMP_Text label = display.GetComponentInChildren<TMP_Text>(true);
			if (label != null)
			{
				_selectorFileNameText = label;
			}
		}

		RefreshSelectorUI();
	}

	private static void PositionSelectorRoot(RectTransform rootRect, RectTransform lastToggle)
	{
		if (rootRect == null)
		{
			return;
		}

		rootRect.anchorMin = new Vector2(0f, 1f);
		rootRect.anchorMax = new Vector2(1f, 1f);
		rootRect.pivot = new Vector2(0.5f, 1f);

		float sectionTop = 116f;
		if (lastToggle != null)
		{
			sectionTop = Mathf.Abs(lastToggle.anchoredPosition.y) + lastToggle.rect.height + 10f;
		}

		rootRect.offsetMin = new Vector2(10f, -(sectionTop + 68f));
		rootRect.offsetMax = new Vector2(-10f, -sectionTop);
	}

	private static TMP_Text CreateText(TMP_Text template, Transform parent, string name, string value, TextAlignmentOptions alignment)
	{
		TMP_Text text = Object.Instantiate(template, parent);
		text.name = name;
		text.text = value;
		text.alignment = alignment;
		text.enableWordWrapping = false;
		text.transform.localScale = Vector3.one;
		return text;
	}

	private static void ConfigureFillRect(RectTransform rect, float left, float right, float top, float bottom)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = new Vector2(left, bottom);
		rect.offsetMax = new Vector2(-right, -top);
	}

	private static void AddLayoutElement(GameObject gameObject, float preferredHeight)
	{
		LayoutElement element = gameObject.AddComponent<LayoutElement>();
		element.minHeight = preferredHeight;
		element.preferredHeight = preferredHeight;
		element.flexibleHeight = 0f;
	}
}
