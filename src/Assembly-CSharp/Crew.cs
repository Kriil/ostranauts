using System;
using System.Collections.Generic;
using UnityEngine;

// Visual/avatar layer for a crew member. Likely owns the sprite stacks, face
// parts, wearable renderers, and attached personal lights used by characters.
public class Crew : MonoBehaviour
{
	// Unity setup: caches renderers, builds the base clothing slots, chooses a
	// random face, and spawns the shared body-light visuals.
	protected virtual void Awake()
	{
		this.tf = base.transform;
		this.dictSpritesClothes = new Dictionary<string, Dictionary<int, List<JsonStringPair>>>();
		this.dictSpritesClothesMRs = new Dictionary<string, Renderer>();
		this.dictSpritesClothes["Base"] = new Dictionary<int, List<JsonStringPair>>();
		JsonStringPair jsonStringPair = new JsonStringPair();
		jsonStringPair.strName = "Crew2BaseA01";
		jsonStringPair.strValue = "blank";
		this.dictSpritesClothes["Base"][0] = new List<JsonStringPair>
		{
			jsonStringPair
		};
		this.dictSpritesClothes["EVA"] = new Dictionary<int, List<JsonStringPair>>();
		this.dictSpritesClothes["EVAHelmet"] = new Dictionary<int, List<JsonStringPair>>();
		this.dictSpritesClothes["EVABackpack"] = new Dictionary<int, List<JsonStringPair>>();
		this.dictSpritesClothes["Outfit01"] = new Dictionary<int, List<JsonStringPair>>();
		this.dictSpritesClothesMRs["Base"] = this.tf.Find("Base").GetComponent<Renderer>();
		this.dictSpritesClothesMRs["EVA"] = this.tf.Find("EVA").GetComponent<Renderer>();
		this.dictSpritesClothesMRs["EVAHelmet"] = this.tf.Find("EVAHelmet").GetComponent<Renderer>();
		this.dictSpritesClothesMRs["EVABackpack"] = this.tf.Find("EVABackpack").GetComponent<Renderer>();
		this.dictSpritesClothesMRs["Outfit01"] = this.tf.Find("Outfit01").GetComponent<Renderer>();
		this.dictSpritesClothesMRs["EVA"].gameObject.SetActive(false);
		this.dictSpritesClothesMRs["EVAHelmet"].gameObject.SetActive(false);
		this.dictSpritesClothesMRs["EVABackpack"].gameObject.SetActive(false);
		this.dictSpritesClothesMRs["Outfit01"].gameObject.SetActive(false);
		this.FaceParts = FaceAnim2.GetRandomFace(true, true, null);
		this.AddLamps();
		this.AddLight(DataHandler.GetLight("SparkBlue"), "ItmLitSphere01", this.tf, out this.visSparks, out this.tfSparks);
		Transform transform = Resources.Load<GameObject>("vfxSparks01").transform;
		this.tfSparksVFX = UnityEngine.Object.Instantiate<Transform>(transform, this.tf);
		this.tfSparksVFX.position = this.tfSparks.position;
		this.tfSparksVFX.eulerAngles = new Vector3(this.tfSparksVFX.eulerAngles.x - 180f, this.tfSparksVFX.eulerAngles.y, 0f);
		this.tfSparksVFX.gameObject.SetActive(false);
		if (Crew.mrRender == null)
		{
			this.InitSharedBodyMeshRenderer();
		}
	}

	// Adds the standard personal lamps used by suits and handheld work lights.
	protected void AddLamps()
	{
		this.AddLight(DataHandler.GetLight("HeadLamp"), "ItmLitCone04", this.tf.Find("RotationAdjuster/Armature/Hips/LowerSpine/Chest/Neck/Head"), out this.visHeadLamp, out this.tfHeadlamp);
		this.AddLight(DataHandler.GetLight("HandLLamp"), "ItmLitSphere01", this.tf.Find("RotationAdjuster/Armature/Hips/LowerSpine/Chest/Shoulder_L/UpperArm_L/LowerArm_L/Hand_L"), out this.visHandLLamp, out this.tfHandLlamp);
		this.AddLight(DataHandler.GetLight("HandRLamp"), "ItmLitSphere01", this.tf.Find("RotationAdjuster/Armature/Hips/LowerSpine/Chest/Shoulder_R/UpperArm_R/LowerArm_R/Hand_R"), out this.visHandRLamp, out this.tfHandRlamp);
		this.AddLight(DataHandler.GetLight("ArmLLamp"), "ItmLitSphere01", this.tf.Find("RotationAdjuster/Armature/Hips/LowerSpine/Chest/Shoulder_L/UpperArm_L/LowerArm_L"), out this.visArmLLamp, out this.tfArmLlamp);
		this.AddLight(DataHandler.GetLight("ArmRLamp"), "ItmLitSphere01", this.tf.Find("RotationAdjuster/Armature/Hips/LowerSpine/Chest/Shoulder_R/UpperArm_R/LowerArm_R"), out this.visArmRLamp, out this.tfArmRlamp);
	}

	// Creates one shared off-screen mesh renderer/material used to render crew
	// body sprites into the custom lighting pipeline.
	protected void InitSharedBodyMeshRenderer()
	{
		Crew.mrRender = DataHandler.GetMesh("prefabQuad", null).GetComponent<MeshRenderer>();
		if (CrewSim.objInstance != null)
		{
			Crew.mrRender.transform.SetParent(CrewSim.objInstance.transform, true);
		}
		Crew.mrRender.transform.position = new Vector3(200f, 200f, 0f);
		Crew.mrRender.gameObject.name = "Crew_mrRender";
		Crew.matRender = new Material(Crew.mrRender.material);
		Crew.matRender.mainTextureScale = new Vector2(1f, 1f);
		Crew.matRender.mainTextureOffset = default(Vector2);
		Crew.matRender.SetTextureScale("_BumpMap", new Vector2(1f, 1f));
		Crew.matRender.SetTextureOffset("_BumpMap", default(Vector2));
		Crew.matRender.SetFloat("_DmgPresent", 0f);
		Crew.matRender.name = "Body Mesh Render";
		Crew.shAlbedo = Shader.Find("Sprites/AlbedoPass");
		Crew.shNormal = Shader.Find("Sprites/NormalPass1");
	}

	// Keeps attached lights and spark effects aligned with the crew transform.
	private void Update()
	{
		if (this.visHeadLamp != null && this.visHeadLamp.GO.activeInHierarchy)
		{
			this.visHeadLamp.Position = this.tf.position;
			this.visHeadLamp.Rotation = this.tf.rotation.eulerAngles.z;
		}
		if (this.visHandLLamp != null && this.visHandLLamp.GO.activeInHierarchy)
		{
			this.visHandLLamp.Position = this.tf.position;
			this.visHandLLamp.Rotation = this.tf.rotation.eulerAngles.z;
		}
		if (this.visHandRLamp != null && this.visHandRLamp.GO.activeInHierarchy)
		{
			this.visHandRLamp.Position = this.tf.position;
			this.visHandRLamp.Rotation = this.tf.rotation.eulerAngles.z;
		}
		if (this.visArmLLamp != null && this.visArmLLamp.GO.activeInHierarchy)
		{
			this.visArmLLamp.Position = this.tf.position;
			this.visArmLLamp.Rotation = this.tf.rotation.eulerAngles.z;
		}
		if (this.visArmRLamp != null && this.visArmRLamp.GO.activeInHierarchy)
		{
			this.visArmRLamp.Position = this.tf.position;
			this.visArmRLamp.Rotation = this.tf.rotation.eulerAngles.z;
		}
		if (this.visSparks != null && this.visSparks.GO.activeInHierarchy)
		{
			this.visSparks.Position = this.tf.position;
			float num = UnityEngine.Random.Range(0f, 1f);
			num *= num * num;
			this.visSparks.LightColor = new Color(num, num, num, num);
			this.tfSparks.localScale = new Vector3(num / this.tfSparks.parent.lossyScale.x, num / this.tfSparks.parent.lossyScale.y, num / this.tfSparks.parent.lossyScale.z);
		}
	}

	// Hides or shows the visible clothing renderers without changing crew data.
	public void ToggleVisibility(bool show)
	{
		if (this.dictSpritesClothesMRs == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Renderer> keyValuePair in this.dictSpritesClothesMRs)
		{
			if (!(keyValuePair.Value == null))
			{
				keyValuePair.Value.enabled = show;
			}
		}
	}

	// Applies the item-def visual scale and world transform for this crew sprite.
	// Likely used when the character body is represented by an item/overlay def.
	public void SetData(string strName, float fX, float fY, float fRot)
	{
		JsonItemDef itemDef = DataHandler.GetItemDef(strName);
		float fZScale = itemDef.fZScale;
		this.tf.position = new Vector3(fX, fY, 0f);
		this.tf.rotation = Quaternion.Euler(0f, 0f, fRot);
		this.tf.localScale = new Vector3(this.tf.localScale.x, this.tf.localScale.y, fZScale);
		if (this.tfHeadlamp != null)
		{
			this.tfHeadlamp.localScale = new Vector3(1f / this.tfHeadlamp.parent.lossyScale.x, 1f / this.tfHeadlamp.parent.lossyScale.y, 1f / this.tfHeadlamp.parent.lossyScale.z);
		}
		if (this.tfHandLlamp != null)
		{
			this.tfHandLlamp.localScale = new Vector3(1f / this.tfHandLlamp.parent.lossyScale.x, 1f / this.tfHandLlamp.parent.lossyScale.y, 1f / this.tfHandLlamp.parent.lossyScale.z);
		}
		if (this.tfHandRlamp != null)
		{
			this.tfHandRlamp.localScale = new Vector3(1f / this.tfHandRlamp.parent.lossyScale.x, 1f / this.tfHandRlamp.parent.lossyScale.y, 1f / this.tfHandRlamp.parent.lossyScale.z);
		}
		if (this.tfArmLlamp != null)
		{
			this.tfArmLlamp.localScale = new Vector3(1f / this.tfArmLlamp.parent.lossyScale.x, 1f / this.tfArmLlamp.parent.lossyScale.y, 1f / this.tfArmLlamp.parent.lossyScale.z);
		}
		if (this.tfArmRlamp != null)
		{
			this.tfArmRlamp.localScale = new Vector3(1f / this.tfArmRlamp.parent.lossyScale.x, 1f / this.tfArmRlamp.parent.lossyScale.y, 1f / this.tfArmRlamp.parent.lossyScale.z);
		}
	}

	// Spawns one attached Visibility light and its cookie sprite at a named bone.
	public void AddLight(JsonLight jl, string strCookie, Transform tfSprite, out Visibility vis, out Transform tfLight)
	{
		vis = UnityEngine.Object.Instantiate<Visibility>(Visibility.visTemplate, this.tf);
		vis.LightColor = DataHandler.GetColor(jl.strColor);
		vis.GO.name = jl.strName;
		vis.Parent = this.tf;
		vis.Radius = 12f;
		vis.tfParent = this.tf;
		vis.SetCookie(strCookie);
		Vector2 vector = default(Vector2);
		vector = jl.ptPos;
		float num = 1f * vector.x / 16f;
		float num2 = 1f * vector.y / 16f;
		vis.ptOffset = new Vector2(num, num2);
		tfLight = DataHandler.GetMesh("prefabQuadLightSprite", null).transform;
		tfLight.rotation = Quaternion.Euler(-90f, 0f, 0f);
		tfLight.SetParent(tfSprite);
		tfLight.position = new Vector3(tfSprite.position.x + num, tfSprite.position.y + num2, tfSprite.position.z);
		Renderer component = tfLight.GetComponent<Renderer>();
		component.sharedMaterial = DataHandler.GetMaterial(component, jl.strImg, "blank", "blank", "blank");
		CrewSim.objInstance.AddLight(vis);
		vis.GO.SetActive(false);
		tfLight.gameObject.SetActive(false);
	}

	public void SetHighlight(float fAmount)
	{
		foreach (Renderer renderer in this.dictSpritesClothesMRs.Values)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetFloat("_Highlight", fAmount);
			renderer.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public void VisualizeDamage(bool force = false)
	{
		CondOwner component = base.GetComponent<CondOwner>();
		if (Crew.mrRender == null || component == null)
		{
			return;
		}
		float num = (float)GUIPDA.instance.pdaVisualisers.Gradient;
		float value = 0f;
		float num2 = 0f;
		float opacity = GUIPDA.instance.pdaVisualisers.Opacity;
		string overlayVariable = GUIPDA.instance.pdaVisualisers.OverlayVariable;
		if (overlayVariable[0] == '_')
		{
			switch (overlayVariable)
			{
			case "_None":
				num = 0f;
				goto IL_2E4;
			case "_Price":
				num2 = (float)component.GetBasePrice(true);
				num2 = GUIPDA.instance.pdaVisualisers.InverseLerp(num2);
				goto IL_2E4;
			case "_Damage":
				num2 = component.GetDamage();
				if (component.HasCond("IsDamaged"))
				{
					value = 1f;
					num2 = 1f;
				}
				goto IL_2E4;
			case "_Mass":
				num2 = (float)component.GetTotalMass();
				num2 = GUIPDA.instance.pdaVisualisers.InverseLerp(num2);
				goto IL_2E4;
			case "_Heat":
				num2 = (float)component.GetCondAmount("StatSolidTemp");
				num2 = Mathf.Max(num2, (float)component.GetCondAmount("StatGasTemp"));
				num2 = GUIPDA.instance.pdaVisualisers.InverseLerp(num2);
				goto IL_2E4;
			case "_Pressure":
			{
				Room room = null;
				if (component.ship != null)
				{
					room = component.ship.GetRoomAtWorldCoords1(component.transform.position, true);
				}
				if (room != null && room.CO != null)
				{
					num2 = (float)room.CO.GetCondAmount("StatGasPressure");
				}
				if (component.GetCondAmount("StatGasPressure") > 0.0)
				{
					num2 = (float)component.GetCondAmount("StatGasPressure");
				}
				num2 = GUIPDA.instance.pdaVisualisers.InverseLerp(num2);
				goto IL_2E4;
			}
			case "_Power":
			{
				double num4 = component.GetCondAmount("StatPowerMax") * component.GetDamageState();
				double condAmount = component.GetCondAmount("StatPower");
				if (condAmount == 0.0)
				{
					num2 = 0f;
				}
				else if (num4 == 0.0)
				{
					num2 = 1f;
				}
				else
				{
					num2 = Mathf.Clamp01((float)condAmount / (float)num4);
				}
				goto IL_2E4;
			}
			}
			Debug.LogWarning("Overlay variable not recognised! rendering scene like normal!");
			num = 0f;
			IL_2E4:;
		}
		else
		{
			num2 = GUIPDA.instance.pdaVisualisers.InverseLerp((float)component.GetCondAmount(overlayVariable));
		}
		if (num == 0f)
		{
			num2 = component.GetDamage();
		}
		Vector4 value2 = new Vector4(component.tf.position.x, component.tf.position.y, 1f, 0f);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		Crew.mrRender.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetFloat("_OverlayAmount", num2);
		materialPropertyBlock.SetVector("_PositionOffset", value2);
		materialPropertyBlock.SetFloat("_OverlayPriority", value);
		materialPropertyBlock.SetFloat("_OverlayMode", num);
		materialPropertyBlock.SetFloat("_OverlayBlend", opacity);
		Crew.mrRender.SetPropertyBlock(materialPropertyBlock);
		foreach (Renderer renderer in this.dictSpritesClothesMRs.Values)
		{
			if (!(renderer == null))
			{
				renderer.GetPropertyBlock(materialPropertyBlock);
				materialPropertyBlock.SetFloat("_OverlayAmount", num2);
				materialPropertyBlock.SetVector("_PositionOffset", value2);
				materialPropertyBlock.SetFloat("_OverlayPriority", value);
				materialPropertyBlock.SetFloat("_OverlayMode", num);
				materialPropertyBlock.SetFloat("_OverlayBlend", opacity);
				renderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}

	public static string GetBodyType(CondOwner co)
	{
		Loot loot;
		if (co.HasCond("IsMale"))
		{
			loot = DataHandler.GetLoot("TXTBodyTypesMale");
		}
		else if (co.HasCond("IsFemale"))
		{
			loot = DataHandler.GetLoot("TXTBodyTypesFemale");
		}
		else
		{
			loot = DataHandler.GetLoot("TXTBodyTypesNB");
		}
		return loot.GetLootNameSingle(null);
	}

	public void SetBodyFaceSkin(string strBodyTypeNew, string[] aFacePartsNew)
	{
		if (strBodyTypeNew == null)
		{
			strBodyTypeNew = this.strBodyType;
		}
		if (aFacePartsNew != null)
		{
			this.aFaceParts = aFacePartsNew;
		}
		string[] faceGroups = FaceAnim2.GetFaceGroups(this.aFaceParts);
		string text = faceGroups[0];
		string text2 = "Crew2Base" + text + faceGroups[1];
		string text3 = "Crew2Basen";
		Renderer renderer;
		if (this.dictSpritesClothesMRs.TryGetValue("Base", out renderer))
		{
			Material material = DataHandler.GetMaterial(renderer, text2, text3, "blank", "blank");
			renderer.sharedMaterial = material;
			JsonStringPair jsonStringPair = new JsonStringPair();
			jsonStringPair.strName = text2;
			jsonStringPair.strValue = text3;
			this.dictSpritesClothes["Base"][0][0] = jsonStringPair;
		}
		if (this.strSkinGroup == text && this.strBodyType == strBodyTypeNew)
		{
			return;
		}
		CondOwner component = base.gameObject.GetComponent<CondOwner>();
		bool flag = false;
		int num = -1;
		if (component != null && component.compSlots != null)
		{
			bool bLogConds = component.bLogConds;
			component.bLogConds = false;
			List<string> lootNames = DataHandler.GetLoot("TXTHumanPartNames").GetLootNames(null, false, null);
			List<string> lootNames2 = DataHandler.GetLoot("TXTHumanSlotNames").GetLootNames(null, false, null);
			num = lootNames2.Count;
			for (int i = 0; i < lootNames2.Count; i++)
			{
				string strSlot = lootNames2[i];
				string str = lootNames[i];
				Slot slot = component.compSlots.GetSlot(strSlot);
				if (slot != null)
				{
					foreach (CondOwner condOwner in slot.aCOs)
					{
						if (!(condOwner == null))
						{
							string b = str + this.strSkinGroup + this.strBodyType;
							string text4 = str + text + strBodyTypeNew;
							if (condOwner.strCODef == b)
							{
								CondOwner condOwner2 = DataHandler.GetCondOwner(text4, null, null, false, null, null, condOwner.strID, null);
								if (!(condOwner2 == null))
								{
									condOwner.bSlotLocked = false;
									condOwner.ModeSwitch(condOwner2, condOwner.tf.position);
									flag = true;
									break;
								}
							}
							else if (condOwner.strCODef == text4)
							{
								num--;
							}
						}
					}
				}
			}
			component.bLogConds = bLogConds;
		}
		if (flag || num == 0)
		{
			this.strBodyType = strBodyTypeNew;
			this.strSkinGroup = text;
			if (component.pspec != null)
			{
				component.pspec.strSkin = this.strSkinGroup;
				component.pspec.strBodyType = ((!(this.strBodyType == string.Empty)) ? this.strBodyType : "01");
			}
		}
	}

	public string Name
	{
		get
		{
			return base.gameObject.GetComponent<CondOwner>().strName;
		}
	}

	public string[] FaceParts
	{
		get
		{
			return this.aFaceParts;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.aFaceParts = value;
			this.SetBodyFaceSkin(this.strBodyType, this.aFaceParts);
		}
	}

	public string BodyType
	{
		get
		{
			return this.strBodyType;
		}
		set
		{
			this.SetBodyFaceSkin(value, this.aFaceParts);
		}
	}

	public void OuterParts(string strMeshPart, Slot slot, string strValue, string strValueNorm, bool bRemove)
	{
		if (strMeshPart == null || slot == null || !this.dictSpritesClothes.ContainsKey(strMeshPart))
		{
			Debug.Log(string.Concat(new object[]
			{
				"Error: Cannot set key ",
				strMeshPart,
				" using slot ",
				slot,
				" on Crew clothes."
			}));
			return;
		}
		Dictionary<int, List<JsonStringPair>> dictionary = this.dictSpritesClothes[strMeshPart];
		if (!dictionary.ContainsKey(slot.nDepth))
		{
			dictionary[slot.nDepth] = new List<JsonStringPair>();
		}
		if (bRemove)
		{
			JsonStringPair jsonStringPair = null;
			foreach (JsonStringPair jsonStringPair2 in dictionary[slot.nDepth])
			{
				if (jsonStringPair2.strName == strValue && jsonStringPair2.strValue == strValueNorm)
				{
					jsonStringPair = jsonStringPair2;
					break;
				}
			}
			if (jsonStringPair != null)
			{
				dictionary[slot.nDepth].Remove(jsonStringPair);
			}
		}
		else if (strValue != "blank" && strValueNorm != "blank")
		{
			JsonStringPair jsonStringPair3 = new JsonStringPair();
			jsonStringPair3.strName = strValue;
			jsonStringPair3.strValue = strValueNorm;
			dictionary[slot.nDepth].Add(jsonStringPair3);
		}
		List<JsonStringPair> list = new List<JsonStringPair>();
		string text = string.Empty;
		string text2 = string.Empty;
		if (dictionary.Count > 0)
		{
			for (int i = 0; i < 100; i++)
			{
				if (dictionary.ContainsKey(i))
				{
					foreach (JsonStringPair jsonStringPair4 in dictionary[i])
					{
						if (jsonStringPair4 != null && jsonStringPair4.strName != "blank")
						{
							list.Add(jsonStringPair4);
							text += jsonStringPair4.strName;
							text2 += jsonStringPair4.strValue;
						}
					}
				}
			}
		}
		bool flag = list.Count > 0;
		Renderer renderer;
		if (!this.dictSpritesClothesMRs.TryGetValue(strMeshPart, out renderer))
		{
			return;
		}
		renderer.gameObject.SetActive(flag);
		if (flag)
		{
			text2 = "Crew2Basen";
			string text3 = text;
			string text4 = text2;
			text3 += ".png";
			text4 += ".png";
			bool bSuppressGetErrors = DataHandler.bSuppressGetErrors;
			DataHandler.bSuppressGetErrors = true;
			Texture2D texture2D = DataHandler.LoadPNG(text3, false, false);
			Texture2D texture2D2 = DataHandler.LoadPNG(text4, true, false);
			DataHandler.bSuppressGetErrors = bSuppressGetErrors;
			if (texture2D.name == "missing.png")
			{
				Crew.MergeTextures(list, text, false);
			}
			if (texture2D2.name == "missing.png")
			{
				Crew.MergeTextures(list, text2, true);
			}
			Material material = DataHandler.GetMaterial(renderer, text, text2, "blank", "blank");
			material.SetOverrideTag("RenderType", "Transparent");
			material.SetInt("_SrcBlend", 5);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 3000;
			renderer.sharedMaterial = material;
		}
	}

	public static void MergeTextures(List<JsonStringPair> aPNGsNonBlank, string strMergedName, bool bNorm)
	{
		RenderTexture renderTexture = null;
		RenderTexture active = RenderTexture.active;
		Texture2D texture2D;
		for (int i = 0; i < aPNGsNonBlank.Count; i++)
		{
			string str = aPNGsNonBlank[i].strName;
			if (bNorm)
			{
				str = aPNGsNonBlank[i].strValue;
			}
			texture2D = DataHandler.LoadPNG(str + ".png", bNorm, false);
			if (renderTexture == null)
			{
				renderTexture = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0, RenderTextureFormat.ARGB32);
				RenderTexture.active = renderTexture;
				if (bNorm)
				{
					GL.Clear(true, true, new Color(0.5f, 0.5f, 1f, 1f));
				}
				else
				{
					GL.Clear(true, true, Color.clear);
				}
			}
			Crew.matRender.shader = Crew.shAlbedo;
			if (bNorm)
			{
				Crew.matRender.shader = Crew.shNormal;
			}
			Graphics.Blit(texture2D, renderTexture, Crew.matRender);
		}
		texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.ReadPixels(new Rect(0f, 0f, (float)renderTexture.width, (float)renderTexture.height), 0, 0);
		texture2D.Apply();
		texture2D.name = strMergedName;
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(renderTexture);
		DataHandler.AddPNG(strMergedName + ".png", texture2D);
	}

	public bool HighlightObjective
	{
		get
		{
			if (this.dictSpritesClothesMRs == null)
			{
				return false;
			}
			using (Dictionary<string, Renderer>.ValueCollection.Enumerator enumerator = this.dictSpritesClothesMRs.Values.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					Renderer renderer = enumerator.Current;
					return renderer.gameObject.layer == LayerMask.NameToLayer("ObjectiveHighlight");
				}
			}
			return false;
		}
		set
		{
			if (this.dictSpritesClothesMRs == null)
			{
				return;
			}
			int layer = LayerMask.NameToLayer("Default");
			if (value)
			{
				layer = LayerMask.NameToLayer("ObjectiveHighlight");
			}
			foreach (Renderer renderer in this.dictSpritesClothesMRs.Values)
			{
				renderer.gameObject.layer = layer;
			}
		}
	}

	public double HandLLamp
	{
		get
		{
			return (double)this.visHandLLamp.Radius;
		}
		set
		{
			this.tfHandLlamp.gameObject.SetActive(value > 0.0);
			this.visHandLLamp.GO.SetActive(value > 0.0);
			this.visHandLLamp.Radius = (float)value;
		}
	}

	public double HandRLamp
	{
		get
		{
			return (double)this.visHandRLamp.Radius;
		}
		set
		{
			this.tfHandRlamp.gameObject.SetActive(value > 0.0);
			this.visHandRLamp.GO.SetActive(value > 0.0);
			this.visHandRLamp.Radius = (float)value;
		}
	}

	public double ArmLLamp
	{
		get
		{
			return (double)this.visArmLLamp.Radius;
		}
		set
		{
			this.tfArmLlamp.gameObject.SetActive(value > 0.0);
			this.visArmLLamp.GO.SetActive(value > 0.0);
			this.visArmLLamp.Radius = (float)value;
		}
	}

	public double ArmRLamp
	{
		get
		{
			return (double)this.visArmRLamp.Radius;
		}
		set
		{
			this.tfArmRlamp.gameObject.SetActive(value > 0.0);
			this.visArmRLamp.GO.SetActive(value > 0.0);
			this.visArmRLamp.Radius = (float)value;
		}
	}

	public double HeadLamp
	{
		get
		{
			return (double)this.visHeadLamp.Radius;
		}
		set
		{
			this.tfHeadlamp.gameObject.SetActive(value > 0.0);
			this.visHeadLamp.GO.SetActive(value > 0.0);
			this.visHeadLamp.Radius = (float)value;
		}
	}

	public bool Sparks
	{
		get
		{
			return this.visSparks.GO.activeInHierarchy;
		}
		set
		{
			this.tfSparks.gameObject.SetActive(value);
			this.tfSparksVFX.gameObject.SetActive(value);
			this.visSparks.GO.SetActive(value);
		}
	}

	protected Dictionary<string, Dictionary<int, List<JsonStringPair>>> dictSpritesClothes;

	protected Dictionary<string, Renderer> dictSpritesClothesMRs;

	private string[] aFaceParts;

	public string strNameGiven;

	private string strSkinGroup = "A";

	private string strBodyType = string.Empty;

	private Visibility visHeadLamp;

	private Visibility visHandLLamp;

	private Visibility visHandRLamp;

	private Visibility visArmLLamp;

	private Visibility visArmRLamp;

	private Visibility visSparks;

	private Transform tfHeadlamp;

	private Transform tfHandLlamp;

	private Transform tfHandRlamp;

	private Transform tfArmLlamp;

	private Transform tfArmRlamp;

	protected Transform tfSparks;

	protected Transform tfSparksVFX;

	protected Transform tf;

	private static MeshRenderer mrRender;

	private static Material matRender;

	private static Shader shAlbedo;

	private static Shader shNormal;
}
