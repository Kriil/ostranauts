using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.UI.MegaToolTip;
using UnityEngine;
using UnityEngine.Events;

// Unity-facing visual/placement component for an item CondOwner.
// This appears to render sprites/materials, placement fit previews, and PDA
// overlays after the owning CondOwner resolves an item definition from
// StreamingAssets/data/items.
public class Item : MonoBehaviour
{
	// Likely used by message consoles or notification-capable devices to flash when ship comms has unread mail.
	private bool _ShowNotification
	{
		get
		{
			if (this.IsNotification == null)
			{
				return false;
			}
			if (this._ship == null)
			{
				CondOwner componentInParent = base.GetComponentInParent<CondOwner>();
				if (componentInParent != null)
				{
					this._ship = componentInParent.ship;
				}
			}
			return this._ship != null && this._ship.Comms != null && this._ship.Comms.HasUnreadMessage();
		}
	}

	// Unity lifecycle setup for render-time helpers.
	public void Awake()
	{
		this._mpb = new MaterialPropertyBlock();
		this._fieldsInitialized = true;
	}

	// Recomputes the PDA overlay shader values used by scanner/inspection views.
	// The named overlays (_Price, _Damage, _Heat, _Pressure, etc.) are UI-driven
	// visualizations rather than gameplay state changes.
	public void VisualizeOverlays(bool force = false)
	{
		CondOwner co = this.CO;
		if (this.rend == null || co == null)
		{
			return;
		}
		this._fOverlayMode = (float)GUIPDA.instance.pdaVisualisers.Gradient;
		this._fOverlayPriority = 0f;
		this._fOverlayAmount = 0f;
		this._fOverlayBlend = GUIPDA.instance.pdaVisualisers.Opacity;
		string overlayVariable = GUIPDA.instance.pdaVisualisers.OverlayVariable;
		if (overlayVariable[0] == '_')
		{
			switch (overlayVariable)
			{
			case "_None":
				this._fOverlayMode = 0f;
				goto IL_366;
			case "_Price":
				this._fOverlayAmount = (float)co.GetBasePrice(true);
				this._fOverlayAmount = GUIPDA.instance.pdaVisualisers.InverseLerp(this._fOverlayAmount);
				goto IL_366;
			case "_Damage":
				this._fOverlayAmount = co.GetDamage();
				if (co.HasCond("IsDamaged"))
				{
					this._fOverlayPriority = 1f;
					this._fOverlayAmount = 1f;
				}
				goto IL_366;
			case "_Heat":
			{
				this._fOverlayAmount = (float)co.GetCondAmount("StatSolidTemp");
				this._fOverlayAmount = Mathf.Max(this._fOverlayAmount, (float)co.GetCondAmount("StatGasTemp"));
				Room room = null;
				if (co.ship != null)
				{
					room = co.ship.GetRoomAtWorldCoords1(co.transform.position, true);
				}
				if (room != null && room.CO != null)
				{
					this._fOverlayAmount = Mathf.Max(this._fOverlayAmount, (float)room.CO.GetCondAmount("StatGasTemp"));
				}
				this._fOverlayAmount = GUIPDA.instance.pdaVisualisers.InverseLerp(this._fOverlayAmount);
				goto IL_366;
			}
			case "_Mass":
				this._fOverlayAmount = (float)co.GetTotalMass();
				this._fOverlayAmount = GUIPDA.instance.pdaVisualisers.InverseLerp(this._fOverlayAmount);
				goto IL_366;
			case "_Pressure":
			{
				Room room = null;
				if (co.ship != null)
				{
					room = co.ship.GetRoomAtWorldCoords1(co.transform.position, true);
				}
				if (room != null && room.CO != null)
				{
					this._fOverlayAmount = (float)room.CO.GetCondAmount("StatGasPressure");
				}
				if (co.GetCondAmount("StatGasPressure") > 0.0)
				{
					this._fOverlayAmount = (float)co.GetCondAmount("StatGasPressure");
				}
				this._fOverlayAmount = GUIPDA.instance.pdaVisualisers.InverseLerp(this._fOverlayAmount);
				goto IL_366;
			}
			case "_Power":
				if (co.HasCond("IsPowered"))
				{
					this._fOverlayAmount = 1f;
				}
				goto IL_366;
			}
			Debug.LogWarning("Overlay variable not recognised! rendering scene like normal!");
			this._fOverlayMode = 0f;
			IL_366:;
		}
		else
		{
			this._fOverlayAmount = GUIPDA.instance.pdaVisualisers.InverseLerp((float)co.GetCondAmount(overlayVariable));
		}
		if (this._fOverlayMode == 0f)
		{
			this._fOverlayAmount = co.GetDamage();
		}
		this._vShaderOffset = new Vector4(co.tf.position.x, co.tf.position.y, this.ZScale, 0f);
		if (!force && (double)Math.Abs(this._fOverlayAmount - this._fShaderOverlayLastFrame) < 0.001 && this._vShaderOffsetLastFrame == this._vShaderOffset)
		{
			return;
		}
		this.rend.GetPropertyBlock(this._mpb);
		this._mpb.SetFloat("_OverlayAmount", this._fOverlayAmount);
		this._mpb.SetVector("_PositionOffset", this._vShaderOffset);
		this._mpb.SetFloat("_OverlayPriority", this._fOverlayPriority);
		this._mpb.SetFloat("_OverlayMode", this._fOverlayMode);
		this._mpb.SetFloat("_OverlayBlend", this._fOverlayBlend);
		this.rend.SetPropertyBlock(this._mpb);
		this._fShaderOverlayLastFrame = this._fOverlayAmount;
		this._vShaderOffsetLastFrame = this._vShaderOffset;
	}

	// Configures sprite-sheet animation metadata for items with animated art.
	private void SetupAnimation(JsonItemAnimation jAnim)
	{
		this._itemAnimation = new ItemAnimationDTO(jAnim);
		if (jAnim.bRandomStartingFrame)
		{
			this.nSheetIndex = UnityEngine.Random.Range(0, this._itemAnimation.FrameCount / 2) * 2;
		}
		int nIndex = this.ConvertArrayIndexToTopLeftIndex(this.nSheetIndex, this._itemAnimation.Columns, this._itemAnimation.Rows);
		this.rend.sharedMaterial = DataHandler.GetMaterialSheet(this.rend, this.strImgOverride, nIndex, this.strImgNormOverride, this.strImgDamagedOverride, this.strDmgColorOverride, this.nWidthInTiles, this.nHeightInTiles);
		float num = 1f * (float)this.rend.sharedMaterial.GetTexture("_MainTex").width / (float)this._itemAnimation.Columns;
		float num2 = 1f * (float)this.rend.sharedMaterial.GetTexture("_MainTex").height / (float)this._itemAnimation.Rows;
		this.vScale.x = (float)Mathf.Max(MathUtils.RoundToInt(num / 16f), 1);
		this.vScale.y = (float)Mathf.Max(MathUtils.RoundToInt(num2 / 16f), 1);
		Item.ItemAnimationUpdate.AddListener(new UnityAction(this.OnItemAnimationUpdate));
	}

	// Main item-definition load step.
	// `strName` should be an item id from StreamingAssets/data/items, which then
	// drives art, sockets, lights, placement size, and other render/fit behavior.
	public void SetData(string strName, float fX, float fY)
	{
		if (!this._fieldsInitialized)
		{
			this.Awake();
		}
		bool bIsWall = false;
		this.aLights = Item._aLightsDefault;
		this.aLightRenderers = Item._aRendsDefault;
		this.dictLightSprites = Item._dictLightSpritesDefault;
		this.fFlickerAmount = 1f;
		this.aSocketAdds = Item._aLootsDefault;
		this.aSocketReqs = Item._aLootsDefault;
		this.aSocketForbids = Item._aLootsDefault;
		Item.aPreRender.Remove(this);
		this.rend = base.gameObject.GetComponent<Renderer>();
		this.jid = DataHandler.GetItemDef(strName);
		if (this.jid == null)
		{
			Debug.Log("null jid setting data on: " + strName);
			return;
		}
		this.strImgOverride = this.jid.strImg;
		this.strImgNormOverride = this.jid.strImgNorm;
		this.strImgDamagedOverride = this.jid.strImgDamaged;
		if (this.strImgDamagedOverride == string.Empty || this.strImgDamagedOverride == null)
		{
			this.strImgDamagedOverride = "blank";
		}
		this.strDmgColorOverride = this.jid.strDmgColor;
		if (string.IsNullOrEmpty(this.strDmgColorOverride))
		{
			this.strDmgColorOverride = "blank";
		}
		this.vScale.z = this.jid.fZScale;
		foreach (string text in this.jid.aSocketAdds)
		{
			if (this.aSocketAdds == Item._aLootsDefault)
			{
				this.aSocketAdds = new List<Loot>();
			}
			if (text == "TILWallAdds")
			{
				bIsWall = true;
			}
			this.aSocketAdds.Add(DataHandler.GetLoot(text));
		}
		foreach (string strName2 in this.jid.aSocketReqs)
		{
			if (this.aSocketReqs == Item._aLootsDefault)
			{
				this.aSocketReqs = new List<Loot>();
			}
			this.aSocketReqs.Add(DataHandler.GetLoot(strName2));
		}
		foreach (string strName3 in this.jid.aSocketForbids)
		{
			if (this.aSocketForbids == Item._aLootsDefault)
			{
				this.aSocketForbids = new List<Loot>();
			}
			this.aSocketForbids.Add(DataHandler.GetLoot(strName3));
		}
		this.bHasSpriteSheet = this.jid.bHasSpriteSheet;
		if (this.jid.ctSpriteSheet != null)
		{
			this.ctSpriteSheet = DataHandler.GetCondTrigger(this.jid.ctSpriteSheet);
		}
		this.nWidthInTiles = this.jid.nCols;
		this.nHeightInTiles = this.aSocketAdds.Count / this.jid.nCols;
		if (this.jid.objAnimation != null)
		{
			this.SetupAnimation(this.jid.objAnimation);
		}
		else if (this.bHasSpriteSheet)
		{
			this.rend.sharedMaterial = DataHandler.GetMaterialSheet(this.rend, this.strImgOverride, 0, this.strImgNormOverride, this.strImgDamagedOverride, this.strDmgColorOverride, 1, 1);
			this.vScale.x = (this.vScale.y = 1f);
		}
		else
		{
			this.rend.sharedMaterial = DataHandler.GetMaterial(this.rend, this.strImgOverride, this.strImgNormOverride, this.strImgDamagedOverride, this.strDmgColorOverride);
			this.vScale.x = (float)Mathf.Max(MathUtils.RoundToInt(1f * (float)this.rend.sharedMaterial.GetTexture("_MainTex").width / 16f), 1);
			this.vScale.y = (float)Mathf.Max(MathUtils.RoundToInt(1f * (float)this.rend.sharedMaterial.GetTexture("_MainTex").height / 16f), 1);
		}
		this.rend.sharedMaterial.renderQueue = 2000 + MathUtils.RoundToInt(this.ZScale * 100f);
		this.rend.sharedMaterial.SetVector("_Aspect", new Vector4((float)this.nWidthInTiles, (float)this.nHeightInTiles, (float)this.rend.sharedMaterial.GetTexture("_MainTex").width, (float)this.rend.sharedMaterial.GetTexture("_MainTex").height));
		if (this.jid.fDmgComplexity != 0f)
		{
			this.rend.sharedMaterial.SetFloat("_Complexity", this.jid.fDmgComplexity);
		}
		if (this.jid.fDmgIntensity != 0f)
		{
			this.rend.sharedMaterial.SetFloat("_Intensity", this.jid.fDmgIntensity);
		}
		if (this.jid.fDmgCut != -999f)
		{
			this.rend.sharedMaterial.SetFloat("_Cut", this.jid.fDmgCut);
		}
		if (this.jid.fDmgTrim != -999f)
		{
			this.rend.sharedMaterial.SetFloat("_Trim", this.jid.fDmgTrim);
		}
		if (!this.jid.bLerp)
		{
			this.rend.sharedMaterial.SetFloat("_Lerp", 0f);
		}
		if (!this.jid.bSinew)
		{
			this.rend.sharedMaterial.SetFloat("_Sinew", 0f);
		}
		switch (this.jid.nDmgMode)
		{
		case 1:
			this.rend.sharedMaterial.SetFloat("_DmgPassThrough", 1f);
			this.rend.sharedMaterial.SetFloat("_DmgExtend", 0f);
			break;
		case 2:
			this.rend.sharedMaterial.SetFloat("_DmgPassThrough", 0f);
			this.rend.sharedMaterial.SetFloat("_DmgExtend", 1f);
			break;
		case 3:
			this.rend.sharedMaterial.SetFloat("_DmgPassThrough", 1f);
			this.rend.sharedMaterial.SetFloat("_DmgExtend", 1f);
			break;
		}
		this.aBlocks = new List<Block>();
		foreach (string text2 in this.jid.aShadowBoxes)
		{
			if (this.aBlocks == Item._aBlocksDefault)
			{
				this.aBlocks = new List<Block>();
			}
			string[] array4 = text2.Split(new char[]
			{
				','
			});
			if (array4.Length >= 4)
			{
				float num = 0f;
				float num2 = 0f;
				float rx = 1f;
				float ry = 1f;
				float.TryParse(array4[0], out num);
				float.TryParse(array4[1], out num2);
				float.TryParse(array4[2], out rx);
				float.TryParse(array4[3], out ry);
				bool bIsGlass = false;
				if (array4.Length > 4)
				{
					bool.TryParse(array4[4], out bIsGlass);
				}
				GameObject gameObject = new GameObject(strName + "block" + this.aBlocks.Count);
				Block block = gameObject.AddComponent<Block>();
				block.rx = rx;
				block.ry = ry;
				block.TF.SetParent(this.TF);
				block.TF.localPosition = new Vector3(num / this.vScale.x, num2 / this.vScale.y, 0f);
				block.UpdateStats();
				block.bIsWall = bIsWall;
				block.bIsGlass = bIsGlass;
				this.aBlocks.Add(block);
			}
		}
		if (!this.bPlaceholder)
		{
			foreach (string text3 in this.jid.aLights)
			{
				JsonLight light = DataHandler.GetLight(text3);
				if (light != null)
				{
					Vector2 vector = default(Vector2);
					vector = light.ptPos;
					float x = 1f * vector.x / 16f / this.vScale.x;
					float y = 1f * vector.y / 16f / this.vScale.y;
					if (light.strColor != "Blank")
					{
						Visibility visibility = UnityEngine.Object.Instantiate<Visibility>(Visibility.visTemplate, this.TF);
						visibility.LightColor = DataHandler.GetColor(light.strColor);
						visibility.GO.name = text3;
						visibility.Parent = this.TF;
						visibility.tfParent = this.TF;
						if (light.fRadius > 0f)
						{
							visibility.Radius = light.fRadius;
						}
						visibility.ptOffset = new Vector2(x, y);
						if (this.aLights == Item._aLightsDefault)
						{
							this.aLights = new List<Visibility>();
						}
						this.aLights.Add(visibility);
					}
					if (light.strImg != null)
					{
						Transform transform = DataHandler.GetMesh("prefabQuadLightSprite", null).transform;
						transform.SetParent(this.TF);
						transform.localPosition = new Vector3(x, y, transform.localPosition.z);
						Renderer component = transform.GetComponent<Renderer>();
						component.sharedMaterial = DataHandler.GetMaterial(component, light.strImg, "blank", "blank", "blank");
						Texture texture = component.sharedMaterial.GetTexture("_MainTex");
						if (this.dictLightSprites == Item._dictLightSpritesDefault)
						{
							this.dictLightSprites = new Dictionary<Transform, Vector2>();
						}
						this.dictLightSprites[transform] = new Vector2((float)texture.width / 16f, (float)texture.height / 16f);
						if (this.aLightRenderers == Item._aRendsDefault)
						{
							this.aLightRenderers = new List<Renderer>();
						}
						this.aLightRenderers.Add(component);
						if (light.bIsNotification)
						{
							this.IsNotification = component;
						}
					}
				}
			}
		}
		if (this.aLights.Count > 0)
		{
			Item.aPreRender.Add(this);
			this.mpbLights = new MaterialPropertyBlock();
		}
		this.fLastRotation = this.TF.rotation.eulerAngles.z;
		this.ResetTransforms(fX, fY);
		this.VisualizeOverlays(false);
	}

	private int ConvertArrayIndexToTopLeftIndex(int collectionIndex, int columns, int rows)
	{
		int num = collectionIndex % columns;
		int num2 = collectionIndex / columns;
		return (rows - 1 - num2) * columns + num;
	}

	public Material SetUpInventoryMaterial(Texture tex = null)
	{
		Texture texture = tex;
		Material material = UnityEngine.Object.Instantiate<Material>(Resources.Load<Material>("Materials/WearInv"));
		material.renderQueue = 3000;
		if (texture == null && this.strImgOverride != "blank" && this.strImgOverride != string.Empty && this.strImgOverride != null)
		{
			texture = DataHandler.LoadPNG(this.strImgOverride + ".png", false, false);
			material.SetTexture("_MainTex", texture);
		}
		if (this.strImgNormOverride != "blank" && this.strImgNormOverride != string.Empty && this.strImgNormOverride != null)
		{
			material.SetTexture("_BumpMap", DataHandler.LoadPNG(this.strImgOverride + ".png", false, false));
		}
		if (this.strImgDamagedOverride != "blank" && this.strImgDamagedOverride != string.Empty && this.strImgDamagedOverride != null)
		{
			material.SetTexture("_DmgTex", DataHandler.LoadPNG(this.strImgDamagedOverride + ".png", false, false));
			material.SetFloat("_DmgPresent", 1f);
		}
		if (this.jid.fDmgComplexity != 0f)
		{
			material.SetFloat("_Complexity", this.jid.fDmgComplexity);
		}
		else
		{
			material.SetFloat("_Complexity", 5000f);
		}
		if (this.jid.fDmgIntensity != 0f)
		{
			material.SetFloat("_Intensity", this.jid.fDmgIntensity);
		}
		if (this.jid.fDmgCut != -999f)
		{
			material.SetFloat("_Cut", this.jid.fDmgCut);
		}
		if (this.jid.fDmgTrim != -999f)
		{
			material.SetFloat("_Trim", this.jid.fDmgTrim);
		}
		if (!this.jid.bLerp)
		{
			material.SetFloat("_Lerp", 0f);
		}
		if (!this.jid.bSinew)
		{
			material.SetFloat("_Sinew", 0f);
		}
		material.SetVector("_PositionOffset", new Vector4(0f, 0f, 0f, 0f));
		float num = (float)(Mathf.Abs(this.CO.strID.GetHashCode()) % 200);
		if (num == 0f)
		{
			num = 0.1f;
		}
		material.SetFloat("_Seed", num);
		if (GUIMegaToolTip.Selected == this.CO)
		{
			material.SetFloat("_Highlight", 1f);
		}
		else
		{
			material.SetFloat("_Highlight", 0f);
		}
		material.SetFloat("_Wear", this.CO.CondPercentage("StatDamage", "StatDamageMax"));
		material.SetVector("_WearCol", Item.GetWearColor(this.strDmgColorOverride, this.strImgDamagedOverride));
		if (texture != null)
		{
			material.SetVector("_Aspect", new Vector4((float)this.nWidthInTiles, (float)this.nHeightInTiles, (float)texture.width, (float)texture.height));
		}
		else
		{
			texture = material.GetTexture("_MainTex");
			if (texture != null)
			{
				material.SetVector("_Aspect", new Vector4((float)this.nWidthInTiles, (float)this.nHeightInTiles, (float)texture.width, (float)texture.height));
			}
			else
			{
				material.SetVector("_Aspect", new Vector4((float)this.nWidthInTiles, (float)this.nHeightInTiles, 16f, 16f));
			}
		}
		switch (this.jid.nDmgMode)
		{
		case 1:
			material.SetFloat("_DmgPassThrough", 1f);
			material.SetFloat("_DmgExtend", 0f);
			break;
		case 2:
			material.SetFloat("_DmgPassThrough", 0f);
			material.SetFloat("_DmgExtend", 1f);
			break;
		case 3:
			material.SetFloat("_DmgPassThrough", 1f);
			material.SetFloat("_DmgExtend", 1f);
			break;
		}
		return material;
	}

	public void NotifyPreRender()
	{
		foreach (Renderer renderer in this.aLightRenderers)
		{
			if (!(renderer == null))
			{
				renderer.GetPropertyBlock(this.mpbLights);
				if (this.IsNotification != null && this.IsNotification == renderer)
				{
					if (this._ShowNotification)
					{
						this.mpbLights.SetColor("_LightColor", new Color(1f, 1f, 1f, Mathf.PingPong(Time.unscaledTime, 0.5f)));
					}
					else
					{
						this.mpbLights.SetColor("_LightColor", new Color(0f, 0f, 0f, 0f));
					}
				}
				else
				{
					this.mpbLights.SetColor("_LightColor", new Color(this.fFlickerAmount, this.fFlickerAmount, this.fFlickerAmount));
				}
				renderer.SetPropertyBlock(this.mpbLights);
			}
		}
	}

	private void OnDestroy()
	{
		Item.aPreRender.Remove(this);
		Item.ItemAnimationUpdate.RemoveListener(new UnityAction(this.OnItemAnimationUpdate));
	}

	private void OnItemAnimationUpdate()
	{
		this._animationTimer += Time.deltaTime;
		if (this._animationTimer >= 1f / (float)this._itemAnimation.FrameRate)
		{
			int num = this.nSheetIndex;
			this.nSheetIndex = (this.nSheetIndex + 1) % this._itemAnimation.FrameCount;
			if (!this._itemAnimation.Loop && this.nSheetIndex == 0 && num > 0)
			{
				Item.ItemAnimationUpdate.RemoveListener(new UnityAction(this.OnItemAnimationUpdate));
			}
			this.rend.sharedMaterial = DataHandler.GetMaterialSheet(this.rend, this.strImgOverride, this.ConvertArrayIndexToTopLeftIndex(this.nSheetIndex, this._itemAnimation.Columns, this._itemAnimation.Rows), this.strImgNormOverride, this.strImgDamagedOverride, this.strDmgColorOverride, this.nWidthInTiles, this.nHeightInTiles);
			this._animationTimer = 0f;
		}
	}

	public void ResetTransforms(float fX, float fY)
	{
		Vector3 size = this.rend.bounds.size;
		this._tf.position = new Vector3(fX, fY, this.GetZPos());
		this._tf.rotation = Quaternion.Euler(0f, 0f, this.fRotLast);
		this._tf.localScale = new Vector3(this.vScale.x, this.vScale.y, 1f);
		foreach (Block block in this.aBlocks)
		{
			block.UpdateStats();
		}
		this.SetLocalVis();
		foreach (KeyValuePair<Transform, Vector2> keyValuePair in this.dictLightSprites)
		{
			keyValuePair.Key.localScale = new Vector3(keyValuePair.Value.x / this.vScale.x, keyValuePair.Value.y / this.vScale.y, 1f / this._tf.localScale.z);
		}
		BoxCollider component = base.GetComponent<BoxCollider>();
		component.center = new Vector3(component.center.x, component.center.y, 5f);
		component.size = new Vector3(component.size.x, component.size.y, 10f);
	}

	public float GetZPos()
	{
		if (this.rend.bounds.size.z > 0.01f)
		{
			return 0f;
		}
		return -this.vScale.z * 4f;
	}

	// Applies an alternate look, often from a COOverlay or damage/variant swap.
	public void SetAlt(string strItemDef)
	{
		if (strItemDef == null)
		{
			this.SetAlt(null, null, "blank", "blank", null);
		}
		else
		{
			JsonItemDef itemDef = DataHandler.GetItemDef(strItemDef);
			if (itemDef == null)
			{
				return;
			}
			this.SetAlt(itemDef.strImg, itemDef.strImgNorm, itemDef.strImgDamaged, itemDef.strDmgColor, null);
		}
	}

	// Lower-level variant setter used by overlays to swap images, damage colors, and optional animation.
	public void SetAlt(string strImg, string strImgNorm, string strImgDamaged = "blank", string strDmgColor = "blank", JsonItemAnimation jAnim = null)
	{
		if (strImg == null)
		{
			this.strImgOverride = this.jid.strImg;
			this.strImgNormOverride = this.jid.strImgNorm;
			this.strImgDamagedOverride = this.jid.strImgDamaged;
			this.strDmgColorOverride = this.jid.strDmgColor;
		}
		else
		{
			this.strImgOverride = strImg;
			this.strImgNormOverride = strImgNorm;
			this.strImgDamagedOverride = strImgDamaged;
			if (!string.IsNullOrEmpty(strDmgColor) && strDmgColor != "blank")
			{
				this.strDmgColorOverride = strDmgColor;
			}
			else
			{
				this.strDmgColorOverride = this.jid.strDmgColor;
			}
		}
		if (string.IsNullOrEmpty(this.strImgDamagedOverride))
		{
			this.strImgDamagedOverride = "blank";
		}
		if (jAnim != null)
		{
			this.SetupAnimation(jAnim);
		}
		else if (this.bHasSpriteSheet)
		{
			this.rend.sharedMaterial = DataHandler.GetMaterialSheet(this.rend, this.strImgOverride, Item.SpriteSheetIndices[this.nSheetIndex], this.strImgNormOverride, this.strImgDamagedOverride, this.strDmgColorOverride, 1, 1);
		}
		else
		{
			this.rend.sharedMaterial = DataHandler.GetMaterial(this.rend, this.strImgOverride, this.strImgNormOverride, this.strImgDamagedOverride, this.strDmgColorOverride);
		}
	}

	// Placement test against ship tiles/zones before installing or dropping an item.
	// Likely used by install interactions, drag/drop placement, and slot fitting.
	public bool CheckFit(Vector3 vCenter, Ship objShip, List<Tile> aGridSprites = null, JsonZone jz = null)
	{
		bool flag = true;
		if (GUIInventory.instance.Selected != null)
		{
			Vector3 vector = GUIInventory.instance.CODoll.GetPos(null, false);
			float num = 3f;
			if (Mathf.Abs((float)Mathf.RoundToInt(vector.x) - vCenter.x) > num || Mathf.Abs((float)Mathf.RoundToInt(vector.y) - vCenter.y) > num)
			{
				flag = false;
			}
			if (flag)
			{
				flag = Visibility.IsCondOwnerLOSVisible(GUIInventory.instance.CODoll, vCenter);
			}
		}
		Vector2 vector2 = new Vector2(vCenter.x - ((float)this.nWidthInTiles / 2f - 0.5f) * 1f, vCenter.y + ((float)this.nHeightInTiles / 2f - 0.5f) * 1f);
		vector2.x -= 1f;
		vector2.y += 1f;
		int num2 = 0;
		bool result = true;
		Tile tilHit = null;
		Item.rgbFit.a = 0.2f + Mathf.Sin(6f * Time.realtimeSinceStartup) * 0.15f;
		Item.rgbUnfit.a = 0.2f + Mathf.Sin(6f * Time.realtimeSinceStartup) * 0.15f;
		Vector2 vector3 = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 vector4 = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		foreach (CondOwner condOwner in objShip.aDocksys)
		{
			Vector2 pos = condOwner.GetPos("DockA", false);
			Vector2 pos2 = condOwner.GetPos("DockB", false);
			Vector2 vector5 = pos2 - pos;
			float num3 = vector5.magnitude / 2f;
			if (vector5.y > 0.5f)
			{
				vector3.y = pos.y + num3;
			}
			else if (vector5.y < -0.5f)
			{
				vector4.y = pos.y - num3;
			}
			else if (vector5.x > 0.5f)
			{
				vector3.x = pos.x + num3;
			}
			else if (vector5.x < -0.5f)
			{
				vector4.x = pos.x - num3;
			}
		}
		for (int k = 0; k < this.nHeightInTiles + 2; k++)
		{
			int j = 0;
			while (j < this.nWidthInTiles + 2)
			{
				Tile tile = null;
				tilHit = null;
				bool flag2 = k == 0 || k == this.nHeightInTiles + 1 || j == 0 || j == this.nWidthInTiles + 1;
				num2 = k * (this.nWidthInTiles + 2) + j;
				if (aGridSprites != null)
				{
					if (aGridSprites.Count <= num2)
					{
						TileUtils.NewGridSprite();
					}
					tile = aGridSprites[num2];
					tile.transform.position = new Vector3(vector2.x + (float)j, vector2.y - (float)k, tile.transform.position.z);
					tile.gameObject.SetActive(flag2);
					tile.SetColor(Item.rgbFit);
					tile.SetMat(Item.strFit);
				}
				Vector2 vector6 = new Vector2(vector2.x + (float)j, vector2.y - (float)k);
				bool flag3 = flag && (vector6.x <= vector3.x && vector6.x >= vector4.x && vector6.y <= vector3.y && vector6.y >= vector4.y);
				if (!flag3)
				{
					goto IL_548;
				}
				if (num2 >= this.aSocketReqs.Count)
				{
					break;
				}
				bool flag4 = this.aSocketReqs[num2].aCOs.Length + this.aSocketReqs[num2].aLoots.Length == 0;
				bool flag5 = this.aSocketForbids[num2].aCOs.Length + this.aSocketForbids[num2].aLoots.Length == 0;
				if (!flag4 || !flag5)
				{
					tilHit = objShip.GetTileAtWorldCoords1(vector6.x, vector6.y, true, true);
					if (tilHit == null)
					{
						flag3 = (jz == null && flag4);
						goto IL_548;
					}
					CondOwner coProps = tilHit.coProps;
					if (!new CondTrigger
					{
						aReqs = this.aSocketReqs[num2].GetLootNames(null, false, null).ToArray(),
						aForbids = this.aSocketForbids[num2].GetLootNames(null, false, null).ToArray()
					}.Triggered(coProps, null, true))
					{
						flag3 = false;
					}
					if (jz != null && !flag2 && Array.FindIndex<int>(jz.aTiles, (int i) => i == tilHit.Index) < 0)
					{
						flag3 = false;
						goto IL_548;
					}
					goto IL_548;
				}
				IL_583:
				j++;
				continue;
				IL_548:
				if (flag3)
				{
					goto IL_583;
				}
				result = false;
				if (tile == null)
				{
					return result;
				}
				tile.gameObject.SetActive(true);
				tile.SetColor(Item.rgbUnfit);
				tile.SetMat(Item.strUnfit);
				goto IL_583;
			}
			if (num2 >= this.aSocketReqs.Count)
			{
				break;
			}
		}
		return result;
	}

	public void RotateCW()
	{
		if (this.bHasSpriteSheet)
		{
			return;
		}
		this.TF.Rotate(0f, 0f, -90f);
		this.fRotLast = MathUtils.NormalizeAngleDegrees(this.fRotLast - 90f);
		this.aSocketReqs = TileUtils.RotateTilesCW<Loot>(this.aSocketReqs, this.nWidthInTiles + 2);
		this.aSocketForbids = TileUtils.RotateTilesCW<Loot>(this.aSocketForbids, this.nWidthInTiles + 2);
		this.aSocketAdds = TileUtils.RotateTilesCW<Loot>(this.aSocketAdds, this.nWidthInTiles);
		MathUtils.Swap(ref this.nWidthInTiles, ref this.nHeightInTiles);
		if (this.aBlocks != null)
		{
			foreach (Block block in this.aBlocks)
			{
				block.RotateCW();
			}
		}
		this.SetLocalVis();
		IEnumerator enumerator2 = base.transform.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				object obj = enumerator2.Current;
				Transform transform = (Transform)obj;
				if (CrewSim.objInstance.workManager.constructionSigns.Contains(transform.gameObject))
				{
					transform.rotation = Quaternion.identity;
				}
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator2 as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
	}

	public void SetLocalVis()
	{
		float x = this.vScale.x;
		float y = this.vScale.y;
		if (MathUtils.IsRotationVertical(this.TF.rotation.eulerAngles.z))
		{
			MathUtils.Swap(ref x, ref y);
		}
		foreach (Visibility visibility in this.aLights)
		{
			visibility.LocalPosition = new Vector3(visibility.ptOffset.x, visibility.ptOffset.y, 0f);
			visibility.LocalScale = new Vector3(1f / x, 1f / y, 1f / this.vScale.z);
		}
	}

	public int SetSpriteSheetIndex(Tile[] aTiles)
	{
		if (!this.bHasSpriteSheet || aTiles == null || this.ctSpriteSheet == null)
		{
			return 0;
		}
		this.nSheetIndex = 0;
		if (aTiles[1] != null && this.ctSpriteSheet.Triggered(aTiles[1].coProps, null, true))
		{
			this.nSheetIndex += 8;
		}
		if (aTiles[3] != null && this.ctSpriteSheet.Triggered(aTiles[3].coProps, null, true))
		{
			this.nSheetIndex += 4;
		}
		if (aTiles[4] != null && this.ctSpriteSheet.Triggered(aTiles[4].coProps, null, true))
		{
			this.nSheetIndex += 2;
		}
		if (aTiles[6] != null && this.ctSpriteSheet.Triggered(aTiles[6].coProps, null, true))
		{
			this.nSheetIndex++;
		}
		this.rend.sharedMaterial = DataHandler.GetMaterialSheet(this.rend, this.strImgOverride, Item.SpriteSheetIndices[this.nSheetIndex], this.strImgNormOverride, this.strImgDamagedOverride, "blank", 1, 1);
		return this.nSheetIndex;
	}

	public int SpriteSheetIndex
	{
		get
		{
			return this.nSheetIndex;
		}
	}

	public override string ToString()
	{
		return this.TF.name;
	}

	public float ZScale
	{
		get
		{
			return this.vScale.z;
		}
	}

	public string ImgOverride
	{
		get
		{
			return this.strImgOverride;
		}
	}

	private static Dictionary<int, int> SpriteSheetIndices
	{
		get
		{
			if (Item.mapSpriteSheetIndices == null)
			{
				Item.mapSpriteSheetIndices = new Dictionary<int, int>
				{
					{
						3,
						12
					},
					{
						7,
						13
					},
					{
						5,
						14
					},
					{
						8,
						15
					},
					{
						11,
						8
					},
					{
						15,
						9
					},
					{
						13,
						10
					},
					{
						2,
						11
					},
					{
						10,
						4
					},
					{
						14,
						5
					},
					{
						12,
						6
					},
					{
						4,
						7
					},
					{
						6,
						0
					},
					{
						0,
						1
					},
					{
						9,
						2
					},
					{
						1,
						3
					}
				};
			}
			return Item.mapSpriteSheetIndices;
		}
	}

	public float fLastRotation
	{
		get
		{
			return this.fRotLast;
		}
		set
		{
			while (!this.bHasSpriteSheet)
			{
				float num = MathUtils.NormalizeAngleDegrees(180f + value - this.fRotLast);
				if (Mathf.Abs(num - 180f) <= 45f)
				{
					return;
				}
				this.RotateCW();
			}
		}
	}

	public void SetToMousePosition(Vector2 vMouse)
	{
		this.TF.position = new Vector3(TileUtils.GridAlign(vMouse.x) + this.rend.bounds.size.x / 2f - 0.5f, TileUtils.GridAlign(vMouse.y) - this.rend.bounds.size.y / 2f + 0.5f, this.TF.position.z);
	}

	public static Color GetWearColor(string strDmgColor, string strImgDamaged)
	{
		if (strDmgColor != "blank" && !string.IsNullOrEmpty(strDmgColor))
		{
			return DataHandler.GetColor(strDmgColor);
		}
		if (strImgDamaged != "blank")
		{
			return new Color(1f, 1f, 1f);
		}
		return DataHandler.GetColor("DamageTintDefault");
	}

	public Transform TF
	{
		get
		{
			if (this._tf == null)
			{
				this._tf = base.transform;
			}
			return this._tf;
		}
	}

	public CondOwner CO
	{
		get
		{
			if (this._co == null)
			{
				this._co = base.GetComponent<CondOwner>();
			}
			return this._co;
		}
	}

	public BoxCollider BoxCollider
	{
		get
		{
			if (this._bc == null)
			{
				this._bc = base.GetComponent<BoxCollider>();
			}
			return this._bc;
		}
	}

	public static readonly UnityEvent ItemAnimationUpdate = new UnityEvent();

	public Renderer rend;

	public JsonItemDef jid;

	public int nWidthInTiles = 1;

	public int nHeightInTiles = 1;

	public bool bPlaceholder;

	public bool bHasSpriteSheet;

	public CondTrigger ctSpriteSheet;

	private Vector3 vScale = new Vector3(1f, 1f, 1f);

	private float fRotLast;

	private int nSheetIndex;

	private string strImgOverride;

	private string strImgNormOverride;

	private string strImgDamagedOverride;

	private string strDmgColorOverride;

	public List<Visibility> aLights;

	public Dictionary<Transform, Vector2> dictLightSprites;

	private List<Renderer> aLightRenderers;

	public float fFlickerAmount = 1f;

	private MaterialPropertyBlock mpbLights;

	public List<Block> aBlocks;

	public Renderer IsNotification;

	private static Dictionary<int, int> mapSpriteSheetIndices;

	public static Color rgbFit = new Color(0.2f, 0.6f, 1f, 0.2f);

	public static Color rgbUnfit = new Color(1f, 0.2f, 0.2f, 0.2f);

	public static string strFit = "GUIGrid16Horiz";

	public static string strUnfit = "GUIGrid16Diag";

	public List<Loot> aSocketReqs;

	public List<Loot> aSocketForbids;

	public List<Loot> aSocketAdds;

	public static List<Item> aPreRender = new List<Item>();

	private Transform _tf;

	private CondOwner _co;

	private BoxCollider _bc;

	private static readonly List<Visibility> _aLightsDefault = new List<Visibility>();

	private static readonly List<Renderer> _aRendsDefault = new List<Renderer>();

	private static readonly Dictionary<Transform, Vector2> _dictLightSpritesDefault = new Dictionary<Transform, Vector2>();

	private static readonly List<Loot> _aLootsDefault = new List<Loot>();

	private static readonly List<Block> _aBlocksDefault = new List<Block>();

	private MaterialPropertyBlock _mpb;

	private Vector4 _vShaderOffset;

	private float _fOverlayMode;

	private float _fOverlayPriority;

	private float _fOverlayAmount;

	private float _fOverlayBlend;

	private bool _fieldsInitialized;

	private Ship _ship;

	private float _fShaderOverlayLastFrame;

	private Vector4 _vShaderOffsetLastFrame;

	private RenderOverlayMode _overlayModeLastFrame;

	private ItemAnimationDTO _itemAnimation;

	private float _animationTimer;
}
