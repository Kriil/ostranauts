using System;
using System.Collections.Generic;
using UnityEngine;

// Dynamic line-of-sight and local light mesh generator.
// CrewSim instantiates these around actors and light sources to carve visible
// wedges out of nearby blocks, then feeds the result into custom LOS materials.
public class Visibility : MonoBehaviour
{
	// Enforces the user-configured lower bound for light flicker intensity.
	public float fFlickerAmount
	{
		get
		{
			return this._fFlickerAmount;
		}
		set
		{
			this._fFlickerAmount = Mathf.Max(value, Visibility._minFlickerAmount);
		}
	}

	// Initializes mesh/render state and preallocates a large occluder pool.
	private void Awake()
	{
		this.fRadius = Visibility.DEFAULTVISIBILITYRANGE;
		this.bRedraw = true;
		this.center = default(Vector2);
		this.ptOffset = default(Vector2);
		this.meshShadow = new Mesh();
		this.GO = base.gameObject;
		this.tf = base.transform;
		this.GO.GetComponent<MeshFilter>().mesh = this.meshShadow;
		this.mr = this.GO.GetComponent<MeshRenderer>();
		this.mpb = new MaterialPropertyBlock();
		Visibility.visibilityList.Add(this);
		this.occluderPool = new List<Occluder>(1024);
		this.occluders2 = new List<Occluder>(1024);
		this.occluders3 = new List<Occluder>(1024);
		for (int i = 0; i < 1024; i++)
		{
			this.occluderPool.Add(new Occluder(0f, 0f));
		}
		this.overrideRedraw = (this.tf.hierarchyCount <= 2);
		Visibility.UpdateBaseFlickerAmount();
		this.NotifyRTChanged();
	}

	// Pulls a reusable occluder record from the internal pool.
	private Occluder GetOccFromPool()
	{
		Occluder result = this.occluderPool[this.occluderPool.Count - 1];
		this.occluderPool.RemoveAt(this.occluderPool.Count - 1);
		return result;
	}

	// Clears object-specific links before the occluder is returned to the pool.
	public void ResetOccluder(Occluder occluder)
	{
		occluder.block = null;
	}

	// Unregisters this visibility source when the GameObject is destroyed.
	private void OnDestroy()
	{
		Visibility.visibilityList.Remove(this);
	}

	// Applies the player's flicker setting to all visibility/light instances.
	public static void UpdateBaseFlickerAmount()
	{
		int nFlickerAmount = DataHandler.GetUserSettings().nFlickerAmount;
		if (nFlickerAmount < 0)
		{
			Visibility._minFlickerAmount = 1f;
		}
		else if (nFlickerAmount == 1)
		{
			Visibility._minFlickerAmount = 0.8f;
		}
		else
		{
			Visibility._minFlickerAmount = 0f;
		}
	}

	// Rebinds render textures after the main render targets are recreated.
	public void NotifyRTChanged()
	{
		foreach (Material material in this.mr.materials)
		{
			if (this.IsLos(material.name))
			{
				material.SetTexture("_MainTex", GameRenderer.RTAlbedo);
				material.SetTexture("_BumpMap", GameRenderer.RTNormal);
			}
		}
	}

	// Pushes per-frame light values into the LOS material before rendering.
	public void NotifyPreRender()
	{
		if (!this.GO.activeInHierarchy)
		{
			return;
		}
		this.mr.GetPropertyBlock(this.mpb);
		this.mpb.SetColor("_LightColor", this.color * this.fFlickerAmount);
		this.mpb.SetColor("_LightAmbient", GameRenderer.clrAmbient);
		this.mpb.SetFloat("_LightZ", this.fLightZ);
		this.mpb.SetFloat("_LightFalloff", (float)this.nLightFalloff);
		this.mpb.SetVector("_LightRot", this.vLightRot);
		this.mr.SetPropertyBlock(this.mpb);
	}

	// Matches the special LOS material by name.
	private bool IsLos(string matName)
	{
		return matName.Length >= 11 && (matName[0] == 'L' && matName[1] == 'i' && matName[2] == 'n' && matName[3] == 'e' && matName[4] == 'O' && matName[5] == 'f' && matName[6] == 'S' && matName[7] == 'i' && matName[8] == 'g' && matName[9] == 'h' && matName[10] == 't');
	}

	// Loads a cookie texture from the PNG cache for patterned lights.
	public void SetCookie(string strPNG)
	{
		this.mr.material.SetTexture("_CookieTex", DataHandler.LoadPNG(strPNG + ".png", false, false));
	}

	private float RadiusAtIntercept(Vector2 a, float dist1, Vector2 b, float dist2, float cosIntercept, float sinIntercept)
	{
		float num = -1f;
		float num2 = -1f;
		if (Visibility.SolveST0(ref num, ref num2, a, b, cosIntercept, sinIntercept) && num2 > 0f)
		{
			return num2;
		}
		return (dist1 + dist2) / 2f;
	}

	private float RadiusAtIntercept(Vector2 a, float dist1, Vector2 b, float dist2, float angle)
	{
		return this.RadiusAtIntercept(a, dist1, b, dist2, Mathf.Cos(angle), Mathf.Sin(angle));
	}

	// Splits one occluder wedge into two segments at the given angle.
	private bool SplitOccluder(int key, float dist, float angle)
	{
		if (this.occluders[key].fAngleLeft >= angle)
		{
			return false;
		}
		if (this.occluders[key].fAngleRight <= angle)
		{
			return false;
		}
		Occluder occFromPool = this.GetOccFromPool();
		occFromPool.SetAngles(this.occluders[key].fAngleLeft, angle);
		occFromPool.fRadiusLeft = this.occluders[key].fRadiusLeft;
		occFromPool.fRadiusRight = dist;
		occFromPool.block = this.occluders[key].block;
		occFromPool.vPositionLeft = this.occluders[key].vPositionLeft;
		this.occluders[key].fRadiusLeft = dist;
		this.occluders[key].fAngleLeft = angle;
		this.occluders[key].UpdatePositionLeft();
		occFromPool.vPositionRight = this.occluders[key].vPositionLeft;
		this.occluders.Insert(key, occFromPool);
		return true;
	}

	// Splits only when the proposed radius sits inside the current occluder wedge.
	private bool TrySplitOccluder(int key, float splitRadius, float angle)
	{
		float cosIntercept = Mathf.Cos(angle);
		float sinIntercept = Mathf.Sin(angle);
		float num = this.RadiusAtIntercept(this.occluders[key].vPositionLeft, this.occluders[key].fRadiusLeft, this.occluders[key].vPositionRight, this.occluders[key].fRadiusRight, cosIntercept, sinIntercept);
		return num * Occluder.sfSign > splitRadius * Occluder.sfSign && this.SplitOccluder(key, num, angle);
	}

	// Merges a new blocking segment into the active occluder list.
	// This is part of the core LOS polygon build, trimming the segment to the
	// visible angle range and cutting existing wedges where intersections occur.
	private void MergeOccluder(Vector2 pos1, float angle1, float dist1, Vector2 pos2, float angle2, float dist2, Block block)
	{
		if (angle1 < this.fStartAngle)
		{
			dist1 = this.RadiusAtIntercept(pos1, dist1, pos2, dist2, this.fStartAngleCos, this.fStartAngleSin);
			angle1 = this.fStartAngle;
			pos1 = Occluder.FromAR(angle1, dist1);
		}
		if (angle2 > this.fFinishAngle)
		{
			dist2 = this.RadiusAtIntercept(pos1, dist1, pos2, dist2, this.fFinishAngleCos, this.fFinishAngleSin);
			angle2 = this.fFinishAngle;
			pos2 = Occluder.FromAR(angle2, dist2);
		}
		if (angle2 <= angle1)
		{
			return;
		}
		Occluder occFromPool = this.GetOccFromPool();
		occFromPool.SetAngles(angle1, angle1);
		int num = this.occluders.BinarySearch(occFromPool);
		if (num < 0)
		{
			num = ~num;
			this.TrySplitOccluder(num, dist1, angle1);
		}
		Occluder occFromPool2 = this.GetOccFromPool();
		occFromPool2.SetAngles(angle2, angle2);
		int num2 = this.occluders.BinarySearch(occFromPool2);
		if (num2 < 0)
		{
			num2 = ~num2;
			if (this.TrySplitOccluder(num2, dist2, angle2))
			{
				num2++;
			}
		}
		this.ResetOccluder(occFromPool);
		this.ResetOccluder(occFromPool2);
		this.occluderPool.Add(occFromPool);
		this.occluderPool.Add(occFromPool2);
		for (int i = num2; i >= num; i--)
		{
			float num3 = -1f;
			float num4 = -1f;
			if (Visibility.SolveST(ref num3, ref num4, this.occluders[i].vPositionLeft, this.occluders[i].vPositionRight, pos1, pos2) && 0f < num3 && num3 < 1f && 0f < num4 && num4 < 1f)
			{
				Vector2 vector = pos1 + num4 * (pos2 - pos1);
				float angle3 = Mathf.Atan2(vector.y, vector.x);
				if (this.SplitOccluder(i, vector.magnitude, angle3))
				{
					num2++;
				}
			}
		}
		int num5 = 0;
		for (int j = num2; j >= num; j--)
		{
			float num6 = (this.occluders[j].fAngleLeft + this.occluders[j].fAngleRight) * 0.5f;
			if (num6 >= angle1)
			{
				if (angle2 >= num6)
				{
					float num7 = this.RadiusAtIntercept(pos1, dist1, pos2, dist2, num6);
					float num8 = this.RadiusAtIntercept(this.occluders[j].vPositionLeft, this.occluders[j].fRadiusLeft, this.occluders[j].vPositionRight, this.occluders[j].fRadiusRight, num6);
					if (num7 * Occluder.sfSign < num8 * Occluder.sfSign)
					{
						float fRadiusLeft = this.RadiusAtIntercept(pos1, dist1, pos2, dist2, this.occluders[j].fAngleLeft);
						float fRadiusRight = this.RadiusAtIntercept(pos1, dist1, pos2, dist2, this.occluders[j].fAngleRight);
						this.occluders[j].fRadiusLeft = fRadiusLeft;
						this.occluders[j].fRadiusRight = fRadiusRight;
						this.occluders[j].block = block;
						this.occluders[j].UpdatePositionLeft();
						this.occluders[j].UpdatePositionRight();
						num5++;
					}
					else
					{
						num5 = 0;
					}
					if (num5 >= 2 && this.occluders[j].block == this.occluders[j + 1].block)
					{
						this.occluders[j].fAngleRight = this.occluders[j + 1].fAngleRight;
						this.occluders[j].fRadiusRight = this.occluders[j + 1].fRadiusRight;
						this.occluders[j].vPositionRight = this.occluders[j + 1].vPositionRight;
						this.ResetOccluder(this.occluders[j + 1]);
						this.occluderPool.Add(this.occluders[j + 1]);
						this.occluders.RemoveAt(j + 1);
					}
				}
			}
		}
	}

	private void AddOccluder(Vector2 pos1, Vector2 pos2, Block block)
	{
		float num = Mathf.Atan2(pos1.y, pos1.x);
		float num2 = Mathf.Atan2(pos2.y, pos2.x);
		if (num == num2)
		{
			return;
		}
		if (num2 < num)
		{
			num2 += 6.2831855f;
		}
		if (num + 3.1415927f < num2)
		{
			return;
		}
		float magnitude = pos1.magnitude;
		float magnitude2 = pos2.magnitude;
		this.MergeOccluder(pos1, num, magnitude, pos2, num2, magnitude2, block);
		if (num2 >= 3.1415927f)
		{
			this.MergeOccluder(pos1, num - 6.2831855f, magnitude, pos2, num2 - 6.2831855f, magnitude2, block);
		}
	}

	private void ResetLightMesh()
	{
		this.meshShadow.Clear();
		this.shadowVerts = new List<Vector3>
		{
			default(Vector3)
		};
		this.shadowIndices = new List<int>();
		this.shadowUVs = new List<Vector2>
		{
			new Vector2(0.5f, 0.5f)
		};
		this.skirt = new List<Vector2>();
		this.skirtW = new List<float>();
		this.fRotation = -this.tfParent.rotation.eulerAngles.z * 0.017453292f;
		this.fRotationScaledCos = Mathf.Cos(this.fRotation) / this.fRadius / 2f;
		this.fRotationScaledSin = Mathf.Sin(this.fRotation) / this.fRadius / 2f;
		this.vLightRot.x = Mathf.Cos(-this.fRotation);
		this.vLightRot.y = Mathf.Sin(-this.fRotation);
		this.tf.eulerAngles = default(Vector3);
		this.center.x = this.tf.position.x;
		this.center.y = this.tf.position.y;
		this.fStartAngleCos = Mathf.Cos(this.fStartAngle);
		this.fStartAngleSin = Mathf.Sin(this.fStartAngle);
		this.fFinishAngleCos = Mathf.Cos(this.fFinishAngle);
		this.fFinishAngleSin = Mathf.Sin(this.fFinishAngle);
	}

	private void OnTransformParentChanged()
	{
		Crew componentInParent = this.tf.GetComponentInParent<Crew>();
		this.overrideRedraw = !(componentInParent == null);
	}

	private void LateUpdate()
	{
		if (!this.bRedraw && !this.overrideRedraw)
		{
			return;
		}
		if (this.GO == null || !this.GO.activeInHierarchy)
		{
			return;
		}
		this.bRedraw = false;
		this.overrideRedraw = false;
		this.ResetLightMesh();
		this.occluders = this.occluders2;
		Occluder.sfSign = 1f;
		this.AddOccludersAtRadius(this.Radius - 0.5f);
		this.AddOccludersFromCrewSimBlocks();
		this.EmitOccluders(true);
		this.occluders = this.occluders3;
		Occluder.sfSign = -1f;
		this.AddOccludersAtRadius(0.5f);
		this.EmitSkirt();
		this.EmitOccluders(false);
		this.meshShadow.vertices = this.shadowVerts.ToArray();
		this.meshShadow.triangles = this.shadowIndices.ToArray();
		this.meshShadow.uv = this.shadowUVs.ToArray();
		for (int i = 0; i < this.occluders2.Count; i++)
		{
			this.ResetOccluder(this.occluders2[i]);
		}
		this.occluderPool.AddRange(this.occluders2);
		this.occluders2.Clear();
		for (int j = 0; j < this.occluders3.Count; j++)
		{
			this.ResetOccluder(this.occluders3[j]);
		}
		this.occluderPool.AddRange(this.occluders3);
		this.occluders3.Clear();
		this.shadowVerts = null;
		this.shadowIndices = null;
		this.shadowUVs = null;
		this.skirt = null;
		this.skirtW = null;
		this.occluders = null;
	}

	private void AddOccludersAtRadius(float radius)
	{
		int num = 64;
		float angleLeft = this.fStartAngle;
		for (int i = 1; i <= num; i++)
		{
			float num2 = (float)i * (this.fFinishAngle - this.fStartAngle) / (float)num + this.fStartAngle;
			Occluder occFromPool = this.GetOccFromPool();
			occFromPool.SetAngles(angleLeft, num2);
			occFromPool.fRadiusLeft = radius;
			occFromPool.fRadiusRight = radius;
			occFromPool.UpdatePositionLeft();
			occFromPool.UpdatePositionRight();
			this.occluders.Add(occFromPool);
			angleLeft = num2;
		}
	}

	private void AddOccludersFromCrewSimBlocks()
	{
		List<Segment> list = new List<Segment>();
		foreach (Block block in CrewSim.blocks)
		{
			block.bVisible = false;
			if (!block.bIsGlass)
			{
				float num = block.x - this.center.x;
				float num2 = block.y - this.center.y;
				float num3 = Mathf.Max(block.rx, block.ry) * 2f;
				if (num * num + num2 * num2 <= (this.fRadius + num3) * (this.fRadius + num3))
				{
					block.GetSegments(ref list);
					foreach (Segment segment in list)
					{
						this.AddOccluder(segment.pos1 - this.center, segment.pos2 - this.center, block);
					}
				}
			}
		}
	}

	public static bool IsCondOwnerLOSVisible(CondOwner coOrigin, CondOwner coTarget)
	{
		Vector3 vector = new Vector3(coOrigin.tf.position.x, coOrigin.tf.position.y, 0f);
		Vector3 target = new Vector3(coTarget.tf.position.x, coTarget.tf.position.y, 0f);
		return Visibility.IsCondOwnerLOSVisible(coOrigin, target);
	}

	public static bool IsCondOwnerLOSVisible(CondOwner coOrigin, Vector3 target)
	{
		Vector3 vector = new Vector3(coOrigin.tf.position.x, coOrigin.tf.position.y, 0f);
		Vector3 direction = target - vector;
		float maxDistance = Vector3.Distance(vector, target);
		if (Visibility._defaultLOSLayer == 0)
		{
			Visibility._defaultLOSLayer = 1 << LayerMask.NameToLayer("Default");
		}
		if (Visibility._losResults == null)
		{
			Visibility._losResults = new RaycastHit[512];
		}
		int num = Physics.RaycastNonAlloc(vector, direction, Visibility._losResults, maxDistance, Visibility._defaultLOSLayer);
		if (num <= 0)
		{
			return true;
		}
		int num2 = 0;
		while (num2 < num && num2 < Visibility._losResults.Length)
		{
			CondOwner component = Visibility._losResults[num2].transform.GetComponent<CondOwner>();
			if (!(component == null))
			{
				if (component.HasCond("IsInstalled"))
				{
					if (component.HasCond("IsWall") || (component.HasCond("IsPortal") && !component.HasCond("IsOpen")))
					{
						return false;
					}
				}
			}
			num2++;
		}
		return true;
	}

	public static bool IsCondOwnerLOSVisibleBlocks(CondOwner co, Vector2 where, bool bIgnoreEndpoints = false, bool bIgnoreGlass = true)
	{
		Vector2 pos = co.GetPos("LOS", false);
		Vector2 vector = where;
		if (MathUtils.GetDistanceSquared(pos, vector) <= 1f)
		{
			return true;
		}
		List<Segment> list = new List<Segment>();
		foreach (Block block in CrewSim.blocks)
		{
			if (!bIgnoreGlass || !block.bIsGlass)
			{
				if (block.TF == null)
				{
					Debug.LogWarning("Warning: Viz block TF null on " + co.strCODef + ". Skipping LOS check.");
				}
				else if (!(block.TF.parent == co.tf))
				{
					float num = Mathf.Abs(block.x - pos.x);
					float num2 = Mathf.Abs(block.y - pos.y);
					if (num >= block.rx || num2 >= block.ry)
					{
						num = Mathf.Abs(block.x - vector.x);
						num2 = Mathf.Abs(block.y - vector.y);
						if (num >= block.rx || num2 >= block.ry)
						{
							block.GetSegments(ref list);
							foreach (Segment segment in list)
							{
								float num3 = 0f;
								float num4 = 0f;
								if (Visibility.SolveST(ref num3, ref num4, pos, vector, segment.pos1, segment.pos2))
								{
									if (bIgnoreEndpoints)
									{
										if (num3 <= 0f || 1f <= num3)
										{
											continue;
										}
										if (num4 <= 0f || 1f <= num4)
										{
											continue;
										}
									}
									else
									{
										if (num3 < 0f || 1f < num3)
										{
											continue;
										}
										if (num4 < 0f || 1f < num4)
										{
											continue;
										}
									}
									return false;
								}
							}
						}
					}
				}
			}
		}
		return true;
	}

	public static bool IsCondOwnerLOSVisibleFromPlayer(CondOwner co, bool bIgnoreGlass = true)
	{
		Vector2 vector = default(Vector2);
		if (CrewSim.GetSelectedCrew() != null)
		{
			vector = new Vector2(CrewSim.GetSelectedCrew().tf.position.x, CrewSim.GetSelectedCrew().tf.position.y);
		}
		Vector2 where = vector;
		return Visibility.IsCondOwnerLOSVisibleBlocks(co, where, false, bIgnoreGlass);
	}

	public static bool IsCondOwnerLOSVisibleFromCo(CondOwner coCenter, CondOwner coToCheck)
	{
		return Visibility.IsCondOwnerLOSVisible(coCenter, coToCheck);
	}

	public static bool SolveST(ref float s, ref float t, Vector2 fromS, Vector2 toS, Vector2 fromT, Vector2 toT)
	{
		float num = toS.x - fromS.x;
		float num2 = toS.y - fromS.y;
		float num3 = toT.x - fromT.x;
		float num4 = toT.y - fromT.y;
		float num5 = num2 * num3 - num * num4;
		if (-1E-05f < num5 && num5 < 1E-05f)
		{
			return false;
		}
		float num6 = fromT.x - fromS.x;
		float num7 = fromT.y - fromS.y;
		s = (num3 * num7 - num4 * num6) / num5;
		t = (num * num7 - num2 * num6) / num5;
		return true;
	}

	public static bool SolveST0(ref float s, ref float t, Vector2 origin, Vector2 target, float dxt, float dyt)
	{
		float num = target.x - origin.x;
		float num2 = target.y - origin.y;
		float num3 = num2 * dxt - num * dyt;
		if (-1E-05f < num3 && num3 < 1E-05f)
		{
			return false;
		}
		s = (dyt * origin.x - dxt * origin.y) / num3;
		t = (num2 * origin.x - num * origin.y) / num3;
		return true;
	}

	private int AppendShadowVert(Vector2 p)
	{
		int num = this.shadowVerts.Count - 1;
		Vector3 vector = this.shadowVerts[num];
		if (p.x == vector.x && p.y == vector.y)
		{
			this.shadowIndices.Add(num);
			return num + 1;
		}
		this.shadowVerts.Add(p);
		this.shadowIndices.Add(num + 1);
		float num2 = this.fRotationScaledCos * p.x - this.fRotationScaledSin * p.y;
		float num3 = this.fRotationScaledSin * p.x + this.fRotationScaledCos * p.y;
		this.shadowUVs.Add(new Vector2(0.5f + num2, 0.5f + num3));
		return num + 1;
	}

	private void AppendTriangle(Vector2 a, Vector2 b, Block block, bool useSkirt)
	{
		if (useSkirt)
		{
			this.skirt.Add(a);
			this.skirt.Add(b);
			float item = Helper.HelperGetThicknessFromBlock(block);
			this.IlluminateBlock(block);
			this.skirtW.Add(item);
			return;
		}
		this.shadowIndices.Add(0);
		this.AppendShadowVert(a);
		this.AppendShadowVert(b);
	}

	private void IlluminateBlock(Block block)
	{
		if (block == null)
		{
			return;
		}
		if (block.bVisible)
		{
			return;
		}
		block.bVisible = true;
		if (block.bIsWall)
		{
			return;
		}
		float num = block.x - this.center.x;
		float num2 = block.y - this.center.y;
		Vector2 p = new Vector2(num - block.rx, num2 + block.ry);
		Vector2 p2 = new Vector2(num - block.rx, num2 - block.ry);
		Vector2 p3 = new Vector2(num + block.rx, num2 - block.ry);
		Vector2 p4 = new Vector2(num + block.rx, num2 + block.ry);
		int item = this.AppendShadowVert(p);
		int num3 = this.AppendShadowVert(p2);
		int item2 = this.AppendShadowVert(p3);
		int num4 = this.AppendShadowVert(p4);
		this.shadowIndices.Add(item);
		this.shadowIndices.Add(item2);
	}

	private void EmitSkirt()
	{
		if (this.skirt.Count < 2)
		{
			return;
		}
		for (int i = this.skirt.Count - 4; i >= 0; i -= 2)
		{
			if (this.skirt[i + 1] == this.skirt[i + 2])
			{
				float num = this.skirt[i + 3].x - this.skirt[i].x;
				float num2 = this.skirt[i + 3].y - this.skirt[i].y;
				float num3 = this.skirt[i + 1].x - this.skirt[i].x;
				float num4 = this.skirt[i + 1].y - this.skirt[i].y;
				float num5 = (num3 * num + num4 * num2) / (num * num + num2 * num2);
				float num6 = num * num5 - num3;
				float num7 = num2 * num5 - num4;
				if (num6 * num6 + num7 * num7 < 0.0001f)
				{
					this.skirt.RemoveAt(i + 2);
					this.skirt.RemoveAt(i + 1);
					this.skirtW.RemoveAt(i / 2);
				}
			}
		}
		for (int j = 0; j < 6; j += 2)
		{
			this.skirt.Add(this.skirt[j]);
			this.skirt.Add(this.skirt[j + 1]);
			this.skirtW.Add(this.skirtW[j / 2]);
		}
		int num8 = 0;
		while (num8 + 2 < this.skirt.Count)
		{
			if (Vector2.Distance(this.skirt[num8 + 1], this.skirt[num8 + 2]) < 0.01f)
			{
				this.skirt[num8 + 1] = this.skirt[num8 + 2];
			}
			num8 += 2;
		}
		List<Vector2> list = new List<Vector2>();
		for (int k = 0; k < this.skirt.Count; k += 2)
		{
			Vector2 a = this.skirt[k];
			Vector2 vector = this.skirt[k + 1];
			Vector2 normalized = (a - vector).normalized;
			float d = this.skirtW[k / 2];
			Vector2 b = new Vector2(-normalized.y, normalized.x) * d;
			list.Add(a + b);
			list.Add(vector + b);
		}
		int num9 = 0;
		while (num9 + 2 < this.skirt.Count)
		{
			if (this.skirt[num9 + 1] == this.skirt[num9 + 2])
			{
				float d2 = -1f;
				float num10 = -1f;
				if (Visibility.SolveST(ref d2, ref num10, list[num9], list[num9 + 1], list[num9 + 2], list[num9 + 3]))
				{
					Vector2 value = list[num9] + d2 * (list[num9 + 1] - list[num9]);
					list[num9 + 1] = value;
					list[num9 + 2] = value;
				}
			}
			else
			{
				float num11 = -1f;
				float num12 = -1f;
				if (Visibility.SolveST0(ref num11, ref num12, list[num9], list[num9 + 1], this.skirt[num9 + 1].x, this.skirt[num9 + 1].y))
				{
					float num13 = Vector2.Distance(list[num9 + 1], list[num9]);
					float num14 = 1f + 0.5f / num13;
					if (num11 > num14)
					{
						num11 = num14;
					}
					list[num9 + 1] = list[num9] + num11 * (list[num9 + 1] - list[num9]);
				}
				else
				{
					list[num9 + 1] = this.skirt[num9 + 1];
				}
				if (Visibility.SolveST0(ref num11, ref num12, list[num9 + 3], list[num9 + 2], this.skirt[num9 + 2].x, this.skirt[num9 + 2].y))
				{
					float num15 = Vector2.Distance(list[num9 + 2], list[num9 + 3]);
					float num16 = 1f + 0.5f / num15;
					if (num11 > num16)
					{
						num11 = num16;
					}
					list[num9 + 2] = list[num9 + 3] + num11 * (list[num9 + 2] - list[num9 + 3]);
				}
				else
				{
					list[num9 + 2] = this.skirt[num9 + 2];
				}
			}
			num9 += 2;
		}
		int num17 = 4;
		while (num17 + 2 < this.skirt.Count)
		{
			this.AddOccluder(list[num17 - 1], list[num17], null);
			this.AddOccluder(list[num17], list[num17 + 1], null);
			num17 += 2;
		}
	}

	private void EmitOccluders(bool useSkirt)
	{
		foreach (Occluder occluder in this.occluders)
		{
			this.AppendTriangle(occluder.vPositionLeft, occluder.vPositionRight, occluder.block, useSkirt);
		}
	}

	public float Radius
	{
		get
		{
			return this.fRadius;
		}
		set
		{
			if (Math.Abs(value - this.fRadius) < 0.01f)
			{
				return;
			}
			this.fRadius = value;
			this.bRedraw = true;
		}
	}

	public Color LightColor
	{
		get
		{
			return this.color;
		}
		set
		{
			this.color = value;
			this.mpb.SetColor("_LightColor", this.color);
		}
	}

	public float Rotation
	{
		get
		{
			return this.tf.rotation.eulerAngles.z;
		}
		set
		{
			if (Math.Abs(value - this.fRotLast) > 10f)
			{
				this.tf.rotation = Quaternion.Euler(0f, 0f, value);
				this.fRotLast = value;
				this.bRedraw = true;
			}
		}
	}

	public Vector3 Position
	{
		get
		{
			return this.tf.position;
		}
		set
		{
			if ((double)Math.Abs(value.x - this.vPosLast.x) > 0.25 || (double)Math.Abs(value.y - this.vPosLast.y) > 0.25)
			{
				this.tf.position = value;
				this.vPosLast = value;
				this.bRedraw = true;
			}
		}
	}

	public Vector3 LocalPosition
	{
		get
		{
			return this.tf.localPosition;
		}
		set
		{
			if (value.x != this.tf.localPosition.x || value.y != this.tf.localPosition.y || value.z != this.tf.localPosition.z)
			{
				this.tf.localPosition = value;
				this.bRedraw = true;
			}
		}
	}

	public Vector3 LocalScale
	{
		get
		{
			return this.tf.localScale;
		}
		set
		{
			if (value.x != this.tf.localScale.x || value.y != this.tf.localScale.y || value.z != this.tf.localScale.z)
			{
				this.tf.localScale = value;
				this.bRedraw = true;
			}
		}
	}

	public Transform Parent
	{
		get
		{
			return this.tf.parent;
		}
		set
		{
			if (value == this.tf.parent)
			{
				return;
			}
			this.tf.SetParent(value);
			this.bRedraw = true;
		}
	}

	private const string LOSNAMEIDENTIFIER = "LineOfSight";

	public static readonly float DEFAULTVISIBILITYRANGE = 6f;

	private Mesh meshShadow;

	private Vector2 center;

	public Vector2 ptOffset;

	private Vector3 vPosLast;

	private float fRotLast;

	public GameObject GO;

	private Transform tf;

	public Transform tfParent;

	private float fRotation;

	private float fRotationScaledCos = 1f;

	private float fRotationScaledSin;

	private Vector4 vLightRot = default(Vector4);

	private MeshRenderer mr;

	private MaterialPropertyBlock mpb;

	private float fRadius;

	public Color color = new Color(0.54901963f, 0.78431374f, 1f);

	private float fLightZ = 0.25f;

	private int nLightFalloff = 3;

	public bool bRedraw = true;

	private float _fFlickerAmount = 1f;

	private static float _minFlickerAmount = 0f;

	private bool overrideRedraw;

	private float fStartAngle = -3.1415927f;

	private float fFinishAngle = 3.1415927f;

	private float fStartAngleCos;

	private float fStartAngleSin;

	private float fFinishAngleCos;

	private float fFinishAngleSin;

	private List<Occluder> occluderPool;

	private List<Occluder> occluders;

	private List<Occluder> occluders2;

	private List<Occluder> occluders3;

	private List<Vector3> shadowVerts;

	private List<int> shadowIndices;

	private List<Vector2> shadowUVs;

	private List<Vector2> skirt;

	private List<float> skirtW;

	public static List<Visibility> visibilityList = new List<Visibility>();

	public static Visibility visTemplate;

	private static LayerMask _defaultLOSLayer;

	private static RaycastHit[] _losResults;
}
