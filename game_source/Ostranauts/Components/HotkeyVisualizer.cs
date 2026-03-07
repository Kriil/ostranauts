using System;
using System.Reflection;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Components
{
	public class HotkeyVisualizer : MonoBehaviour
	{
		private void Awake()
		{
			if (this._hotKeyImgContainer != null)
			{
				this._hotKeyImgContainer.alpha = 0f;
				this._txt = this._hotKeyImgContainer.GetComponentInChildren<TMP_Text>();
				if (base.transform.parent != null)
				{
					this._canvas.overrideSorting = true;
					if (this._customSortingOrder != 0)
					{
						this._canvas.sortingOrder = this._customSortingOrder;
					}
					else
					{
						Canvas componentInParent = base.transform.parent.GetComponentInParent<Canvas>();
						if (componentInParent != null)
						{
							this._canvas.sortingOrder = componentInParent.sortingOrder + 1;
						}
					}
				}
				if (HotkeyVisualizer.OnShowHotKeys == null)
				{
					HotkeyVisualizer.OnShowHotKeys = new ShowHotKeysEvent();
				}
				HotkeyVisualizer.OnShowHotKeys.AddListener(new UnityAction<float>(this.OnShowHotKeysDown));
				return;
			}
			Debug.LogWarning("Missing HotKey Object");
			UnityEngine.Object.Destroy(this);
		}

		public void Test(bool show)
		{
			TMP_Text componentInChildren = this._hotKeyImgContainer.GetComponentInChildren<TMP_Text>();
			if (componentInChildren != null)
			{
				componentInChildren.text = ((!show) ? " " : "long key name");
				this.SetPosition(show);
			}
		}

		private void SetKeyName()
		{
			if (string.IsNullOrEmpty(this.selectedActionKeyName))
			{
				this._txt.text = string.Empty;
				return;
			}
			string containerClassName = "GUIActionKeySelector";
			string targetClassName = this.selectedActionKeyName;
			object staticFieldValue = this.GetStaticFieldValue(containerClassName, targetClassName);
			if (staticFieldValue != null)
			{
				object keyNameProperty = this.GetKeyNameProperty(staticFieldValue);
				if (keyNameProperty != null)
				{
					this._txt.text = keyNameProperty.ToString().Replace("Alpha", string.Empty);
				}
			}
			this.SetPosition(this._txt.text.Length > 1);
		}

		private void SetPosition(bool expand)
		{
			RectTransform rectTransform = base.transform as RectTransform;
			if (rectTransform == null)
			{
				return;
			}
			Vector2? anchoredPosition = this._anchoredPosition;
			if (anchoredPosition == null)
			{
				this._anchoredPosition = new Vector2?(new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y));
			}
			if (expand)
			{
				switch (this._hotKeyExpansion)
				{
				case HotKeyExpansion.down:
				{
					RectTransform rectTransform2 = rectTransform;
					Vector2? anchoredPosition2 = this._anchoredPosition;
					float x = anchoredPosition2.Value.x;
					Vector2? anchoredPosition3 = this._anchoredPosition;
					rectTransform2.anchoredPosition = new Vector2(x, anchoredPosition3.Value.y - rectTransform.sizeDelta.y - 4f);
					break;
				}
				case HotKeyExpansion.up:
				{
					RectTransform rectTransform3 = rectTransform;
					Vector2? anchoredPosition4 = this._anchoredPosition;
					float x2 = anchoredPosition4.Value.x;
					float num = 4f;
					Vector2? anchoredPosition5 = this._anchoredPosition;
					rectTransform3.anchoredPosition = new Vector2(x2, num + anchoredPosition5.Value.y + rectTransform.sizeDelta.y);
					break;
				}
				case HotKeyExpansion.left:
				case HotKeyExpansion.right:
					break;
				default:
				{
					RectTransform rectTransform4 = rectTransform;
					Vector2? anchoredPosition6 = this._anchoredPosition;
					rectTransform4.anchoredPosition = anchoredPosition6.Value;
					break;
				}
				}
			}
			else
			{
				RectTransform rectTransform5 = rectTransform;
				Vector2? anchoredPosition7 = this._anchoredPosition;
				rectTransform5.anchoredPosition = anchoredPosition7.Value;
			}
		}

		private object GetKeyNameProperty(object instance)
		{
			Type type = instance.GetType();
			PropertyInfo property = type.GetProperty("KeyNameShort");
			if (property == null)
			{
				return null;
			}
			return property.GetValue(instance, null);
		}

		private object GetStaticFieldValue(string containerClassName, string targetClassName)
		{
			Type type = Type.GetType(containerClassName);
			if (type == null)
			{
				return null;
			}
			Type type2 = Type.GetType(targetClassName);
			if (type2 == null)
			{
				return null;
			}
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Static | BindingFlags.Public))
			{
				if (fieldInfo.FieldType == type2)
				{
					return fieldInfo.GetValue(null);
				}
			}
			return null;
		}

		private void Update()
		{
			if (this._inactive || Time.unscaledTime < HotkeyVisualizer._lastInvokeTimestamp + HotkeyVisualizer.VisibleTime)
			{
				return;
			}
			if (this._hotKeyImgContainer != null)
			{
				this._hotKeyImgContainer.alpha = 0f;
			}
			this._inactive = true;
			HotkeyVisualizer._lastInvokeTimestamp = 0f;
		}

		private void OnShowHotKeysDown(float timeStamp)
		{
			if (this._hotKeyImgContainer == null || this._hotKeyImgContainer.alpha == 1f)
			{
				return;
			}
			this.SetKeyName();
			this._hotKeyImgContainer.alpha = 1f;
			HotkeyVisualizer._lastInvokeTimestamp = timeStamp;
			this._inactive = false;
		}

		public static ShowHotKeysEvent OnShowHotKeys = new ShowHotKeysEvent();

		[SerializeField]
		private CanvasGroup _hotKeyImgContainer;

		[SerializeField]
		public HotKeyExpansion _hotKeyExpansion;

		[SerializeField]
		private Canvas _canvas;

		[SerializeField]
		private int _customSortingOrder;

		private TMP_Text _txt;

		public string selectedActionKeyName;

		private bool _inactive = true;

		private static float _lastInvokeTimestamp;

		private static readonly float VisibleTime = 0.5f;

		private Vector2? _anchoredPosition;
	}
}
