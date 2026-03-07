using System;
using System.Collections.Generic;
using Parallax;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class ParallaxController : MonoBehaviour
{
	public static float fRotationWorld
	{
		get
		{
			return (!ParallaxController.DisableParallaxRotation) ? ParallaxController._fRotationWorld : 0f;
		}
		set
		{
			ParallaxController._fRotationWorld = value;
		}
	}

	private void Start()
	{
		ParallaxController.vOffsetWorld = Vector2.zero;
		ParallaxController.vPosFull = Vector2.zero;
		ParallaxController.fRotationWorld = 0f;
		this.m_layers = new List<GameObject>();
		this.aLightsSuns = new HashSet<Visibility>();
		ParallaxController.ActiveParallax = null;
		ParallaxController.DisableParallaxRotation = DataHandler.GetUserSettings().bDisableParallaxRotation;
		this.ResetLayers();
	}

	private void Update()
	{
		if (!this.m_camSetup)
		{
			this.SetupCanvas();
		}
		this.PositionLayers();
		if (this.m_toggle)
		{
			this.m_toggle = false;
			if (this.m_world)
			{
				this.SetWorldSpace();
			}
			else
			{
				this.SetCameraSpace();
			}
			this.bResetLayers = true;
		}
		if (this.bResetLayers)
		{
			this.ResetLayers();
		}
		Quaternion rotation = Quaternion.AngleAxis(ParallaxController.fRotationWorld, Vector3.forward);
		CrewSim.goSun.transform.rotation = rotation;
		this.CheckSunRedraw(rotation.eulerAngles.z);
	}

	private void CheckSunRedraw(float currentSunRotation)
	{
		if (this.aLightsSuns == null || CrewSim.bRaiseUI)
		{
			return;
		}
		float num = Mathf.Abs(this._sunsLastRedrawRotation - currentSunRotation);
		if (num <= 0.2f)
		{
			return;
		}
		this._sunsLastRedrawRotation = currentSunRotation;
		foreach (Visibility visibility in this.aLightsSuns)
		{
			visibility.bRedraw = true;
		}
	}

	public void SetPattern(Pattern pattern)
	{
		this.m_pattern = pattern;
		this.bResetLayers = true;
	}

	public void SetLayerAmount(int layers)
	{
		this.m_childLayers = layers;
		this.bResetLayers = true;
	}

	public void SetScaleFactor(float scaleFactor)
	{
		this.m_layerScaleFactor = scaleFactor;
		this.bResetLayers = true;
	}

	public void InsertSprites(int index, List<Sprite> sprites)
	{
		if (index >= this.m_sprites.Count)
		{
			this.m_sprites.AddRange(sprites);
		}
		else
		{
			this.m_sprites.InsertRange(index, sprites);
		}
		this.bResetLayers = true;
	}

	public void SetSprites(List<Sprite> sprites)
	{
		this.m_childLayers = sprites.Count;
		this.m_sprites = sprites;
		this.bResetLayers = true;
	}

	public void ResetLayers()
	{
		if (this.m_sprites.Count < this.m_childLayers)
		{
			int count = this.m_sprites.Count;
			Pattern pattern = this.m_pattern;
			if (pattern != Pattern.RoundRobin)
			{
				if (pattern != Pattern.RepeatLast)
				{
					if (pattern == Pattern.RepeatFirst)
					{
						for (int i = count; i <= this.m_childLayers; i++)
						{
							this.m_sprites.Insert(0, this.m_sprites[0]);
						}
					}
				}
				else
				{
					for (int j = count; j <= this.m_childLayers; j++)
					{
						this.m_sprites.Add(this.m_sprites[this.m_sprites.Count - 1]);
					}
				}
			}
			else
			{
				for (int k = count; k <= this.m_childLayers; k++)
				{
					this.m_sprites.Add(this.m_sprites[k % count]);
				}
			}
		}
		for (int l = this.m_layers.Count - 1; l >= 0; l--)
		{
			GameObject gameObject = this.m_layers[l];
			this.m_layers.Remove(gameObject);
			UnityEngine.Object.Destroy(gameObject);
		}
		this.m_layers.Clear();
		GameObject gameObject2 = base.transform.gameObject;
		RenderTexture shipAlpha = this.m_gameRenderer.GetShipAlpha();
		this.m_biases = new List<Vector2>();
		ParallaxController.vPosFull = Vector2.zero;
		for (int m = 0; m < this.m_childLayers; m++)
		{
			GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, gameObject2.transform);
			gameObject3.transform.localPosition = new Vector3(0f, 0f, -0.1f);
			gameObject3.transform.localScale = new Vector3(this.m_layerScaleFactor, this.m_layerScaleFactor, 1f);
			gameObject3.layer = gameObject2.layer;
			this.m_layers.Add(gameObject3);
			RawImage component = gameObject3.GetComponent<RawImage>();
			if (this.m_alpha && m == this.m_childLayers - 1)
			{
				component.texture = shipAlpha;
			}
			else
			{
				component.texture = this.m_sprites[m].texture;
			}
			component.material = UnityEngine.Object.Instantiate<Material>(this.m_material);
			component.material.renderQueue = this.nRenderQueueStart + this.nRenderQueueStep * m;
			float num = this.m_layers[0].GetComponent<RectTransform>().rect.width / this.m_layers[0].GetComponent<RectTransform>().rect.height;
			component.material.mainTextureScale = new Vector2(1f, 1f / num);
			gameObject2 = gameObject3;
			this.m_biases.Add(new Vector2(UnityEngine.Random.Range(-this.m_distortion, this.m_distortion), UnityEngine.Random.Range(-this.m_distortion, this.m_distortion)));
			if (m != 0)
			{
				RectTransform component2 = gameObject3.GetComponent<RectTransform>();
				component2.anchorMin = Vector2.zero;
				component2.anchorMax = Vector2.one;
				component2.anchoredPosition = Vector2.zero;
				component2.sizeDelta = Vector2.zero;
			}
		}
		this.bResetLayers = false;
	}

	public void PositionLayers()
	{
		float d = this.m_watching.GetComponent<Camera>().orthographicSize / this.m_gameRenderer.GetComponent<Camera>().orthographicSize;
		Vector2 a = new Vector2(this.m_watching.transform.position.x - ParallaxController.vPosLast.x, this.m_watching.transform.position.y - ParallaxController.vPosLast.y);
		ParallaxController.vPosLast = this.m_watching.transform.position;
		ParallaxController.vPosFull += a * d;
		Vector3 vector;
		if (this.m_rotateDock)
		{
			vector = this.m_watching.GetComponent<Camera>().WorldToViewportPoint(CrewSim.shipCurrentLoaded.aDocksys[0].transform.position);
		}
		else
		{
			vector = new Vector3(0.5f, 0.5f, 0f);
		}
		Vector4 value = new Vector4(ParallaxController.vOffsetWorld.x, ParallaxController.vOffsetWorld.y, vector.x, vector.y);
		float value2 = ParallaxController.fRotationWorld / 57.2958f;
		for (int i = 0; i < this.m_layers.Count; i++)
		{
			value.x += ParallaxController.vPosFull.x * this.rate.x * (float)i;
			value.y += ParallaxController.vPosFull.y * this.rate.y * (float)i;
			RawImage component = this.m_layers[i].GetComponent<RawImage>();
			if (this.m_biases.Count > i)
			{
				component.material.mainTextureOffset = this.m_biases[i] + ParallaxController.vOffsetWorld * (float)i;
			}
			component.material.SetFloat("_Rotation", value2);
			component.material.SetVector("_WorldOffset", value);
		}
	}

	private void SetupCanvas()
	{
		Camera parallaxCamera = this.m_gameRenderer.GetParallaxCamera();
		if (parallaxCamera == null)
		{
			return;
		}
		base.GetComponent<Canvas>().worldCamera = parallaxCamera;
		this.m_camSetup = true;
	}

	public void SetTextures(List<Texture> textures)
	{
		this.m_childLayers = textures.Count;
		this.ResetLayers();
		for (int i = 0; i < this.m_layers.Count; i++)
		{
			RawImage component = this.m_layers[i].GetComponent<RawImage>();
			component.texture = textures[i];
		}
	}

	public int GetLayersAmount()
	{
		return this.m_layers.Count;
	}

	public void SetWorldSpace()
	{
		base.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
	}

	public void SetCameraSpace()
	{
		base.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
	}

	public void SetData(JsonParallax jp)
	{
		if (jp == null)
		{
			Debug.LogError("ERROR: Cannot set parallax to null.");
			return;
		}
		Debug.Log("#Info# Setting parallax to " + jp.strName);
		ParallaxController.ActiveParallax = jp.strName;
		this.m_distortion = jp.fDistortion;
		this.rate = new Vector2(jp.fRateX, jp.fRateY);
		this.SetScaleFactor(jp.fLayerScaleFactor);
		this.SetPattern(jp.Pattern());
		this.SetLayerAmount(jp.nLayers);
		this.m_sprites = new List<Sprite>();
		Loot loot = DataHandler.GetLoot(jp.strLootSpriteList);
		List<string> lootNames = loot.GetLootNames(null, false, null);
		if (lootNames.Count == 0)
		{
			lootNames.Add("blank");
		}
		foreach (string str in lootNames)
		{
			Texture2D texture2D = DataHandler.LoadPNG(str + ".png", false, false);
			texture2D.wrapMode = TextureWrapMode.Repeat;
			Sprite item = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f), 16f, 0U, SpriteMeshType.FullRect);
			this.m_sprites.Add(item);
		}
		foreach (Visibility visibility in this.aLightsSuns)
		{
			CrewSim.objInstance.RemoveLight(visibility);
			UnityEngine.Object.Destroy(visibility.gameObject);
		}
		this.aLightsSuns.Clear();
		if (jp.aSunLights != null)
		{
			foreach (string strName in jp.aSunLights)
			{
				JsonLight light = DataHandler.GetLight(strName);
				if (light != null)
				{
					Visibility visibility2 = UnityEngine.Object.Instantiate<Visibility>(Visibility.visTemplate, CrewSim.goSun.transform);
					visibility2.LightColor = DataHandler.GetColor(light.strColor);
					visibility2.GO.name = "Sun" + light.ptPos;
					visibility2.Parent = CrewSim.goSun.transform;
					visibility2.tfParent = CrewSim.goSun.transform;
					if (light.fRadius > 0f)
					{
						visibility2.Radius = light.fRadius;
					}
					else
					{
						visibility2.Radius = 1000f;
					}
					visibility2.ptOffset = default(Vector2);
					visibility2.LocalPosition = light.ptPos;
					CrewSim.objInstance.AddLight(visibility2);
					this.aLightsSuns.Add(visibility2);
				}
			}
		}
		this.bResetLayers = true;
	}

	public bool m_toggle;

	public bool m_alpha;

	public bool m_world;

	public bool m_rotateDock;

	public int m_childLayers = 6;

	public float m_layerScaleFactor = 1.2f;

	public float m_distortion = 1f;

	public Vector2 rate = new Vector2(0.0002f, 0.0002f);

	public Pattern m_pattern;

	public List<Sprite> m_sprites;

	public int nRenderQueueStart = 2000;

	public int nRenderQueueStep = 20;

	public static Vector2 vOffsetWorld;

	public static bool DisableParallaxRotation;

	private static float _fRotationWorld;

	public static float fRadiusWorld;

	public static Vector2 vPosLast;

	public static Vector2 vPosFull;

	public GameRenderer m_gameRenderer;

	public GameObject m_watching;

	public Material m_material;

	public GameObject m_prefab;

	public List<Vector2> m_biases;

	public List<GameObject> m_layers;

	public static string ActiveParallax;

	private HashSet<Visibility> aLightsSuns;

	private bool m_camSetup;

	private bool bResetLayers = true;

	private float _sunsLastRedrawRotation;
}
