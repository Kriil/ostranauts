using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Utilities
{
	public static class SilhouetteUtility
	{
		public static int GetSilhouetteLength(List<Vector2> floorPlan)
		{
			if (floorPlan == null || floorPlan.Count == 0)
			{
				return 1;
			}
			List<Vector2> source = (from x in floorPlan
			orderby x.x descending
			select x).ToList<Vector2>();
			int num = (int)source.First<Vector2>().x;
			int num2 = (int)source.Last<Vector2>().x;
			return Mathf.Abs(num - num2);
		}

		public static List<Vector2> GetFloorVectors(JsonItem[] items)
		{
			List<Vector2> list = new List<Vector2>();
			if (items == null)
			{
				return list;
			}
			for (int i = 0; i < items.Length; i++)
			{
				if (!items[i].strName.Contains("Placeholder"))
				{
					if (items[i].strName.Contains("Floor") || (items[i].strName.Contains("Wall") && !items[i].strName.Contains("LitWall")))
					{
						if (items[i].strName.Contains("1x3Slant"))
						{
							SilhouetteUtility.TryAddToPlan(list, items[i]);
							Vector2[] array = (items[i].fRotation != 90f && items[i].fRotation != 270f) ? SilhouetteUtility._1x3SlantHorizontalItemPositions : SilhouetteUtility._1x3SlantVerticalItemPositions;
							foreach (Vector2 a in array)
							{
								SilhouetteUtility.TryAddToPlan(list, a + new Vector2(items[i].fX, items[i].fY));
							}
						}
						else if (items[i].strName.Contains("1x2Slant"))
						{
							Vector2[] array3 = (items[i].fRotation != 90f && items[i].fRotation != 270f) ? SilhouetteUtility._1x2SlantHorizontalItemPositions : SilhouetteUtility._1x2SlantVerticalItemPositions;
							foreach (Vector2 a2 in array3)
							{
								SilhouetteUtility.TryAddToPlan(list, a2 + new Vector2(items[i].fX, items[i].fY));
							}
						}
						else
						{
							SilhouetteUtility.TryAddToPlan(list, items[i]);
						}
					}
					else if (items[i].strName.Contains("ItmDockSys02Closed") || items[i].strName.Contains("ItmDockSys02Open"))
					{
						Vector2[] array5 = SilhouetteUtility.RotateAirlockSubTiles(items[i].fRotation);
						foreach (Vector2 a3 in array5)
						{
							Vector2 item = a3 + new Vector2(items[i].fX, items[i].fY);
							if (!list.Contains(item))
							{
								list.Insert(0, item);
							}
						}
					}
					else if (items[i].strName.Contains("ItmHeavyLiftRotor"))
					{
						SilhouetteUtility.TryAddToPlan(list, items[i]);
						foreach (Vector2 vector in SilhouetteUtility._allDirectionVectors)
						{
							SilhouetteUtility.TryAddToPlan(list, new Vector2(items[i].fX + vector.x, items[i].fY + vector.y));
						}
					}
				}
			}
			return list;
		}

		private static void TryAddToPlan(List<Vector2> vectorList, JsonItem item)
		{
			SilhouetteUtility.TryAddToPlan(vectorList, new Vector2(item.fX, item.fY));
		}

		private static void TryAddToPlan(List<Vector2> vectorList, Vector2 pos)
		{
			if (!vectorList.Contains(pos))
			{
				vectorList.Add(pos);
			}
		}

		public static List<Vector2> ScaleFloorplan(List<Vector2> originalFloorplan, int resolutionFactor)
		{
			if (originalFloorplan == null || originalFloorplan.Count == 0)
			{
				return originalFloorplan;
			}
			List<Vector2> list = new List<Vector2>();
			foreach (Vector2 vector in originalFloorplan)
			{
				Vector2 vector2 = new Vector2(vector.x * (float)resolutionFactor, vector.y * (float)resolutionFactor);
				for (int i = 0; i < resolutionFactor; i++)
				{
					for (int j = 0; j < resolutionFactor; j++)
					{
						list.Add(new Vector2(vector2.x + (float)i, vector2.y + (float)j));
					}
				}
			}
			return list;
		}

		private static Vector2[] RotateAirlockSubTiles(float rotation)
		{
			List<Vector2> list = new List<Vector2>();
			if (rotation == 180f)
			{
				for (int i = 0; i < SilhouetteUtility._airlockHorizontalSubItemPositions.Length; i++)
				{
					list.Add(new Vector2(SilhouetteUtility._airlockHorizontalSubItemPositions[i].x, SilhouetteUtility._airlockHorizontalSubItemPositions[i].y * -1f));
				}
			}
			else
			{
				if (rotation == 90f)
				{
					return SilhouetteUtility._airlockVerticalSubItemPositions;
				}
				if (rotation != 270f && rotation != -90f)
				{
					return SilhouetteUtility._airlockHorizontalSubItemPositions;
				}
				for (int j = 0; j < SilhouetteUtility._airlockVerticalSubItemPositions.Length; j++)
				{
					list.Add(new Vector2(SilhouetteUtility._airlockVerticalSubItemPositions[j].x * -1f, SilhouetteUtility._airlockVerticalSubItemPositions[j].y));
				}
			}
			return list.ToArray();
		}

		public static List<Vector2> GenerateVectorPoints(List<Vector2> floorPlan)
		{
			if (floorPlan == null || floorPlan.Count < 3)
			{
				return null;
			}
			floorPlan = SilhouetteUtility.ScaleFloorplan(floorPlan, 3);
			float num = float.MinValue;
			float num2 = float.MaxValue;
			float num3 = float.MinValue;
			float num4 = float.MaxValue;
			foreach (Vector2 vector in floorPlan)
			{
				if (vector.x > num)
				{
					num = vector.x;
				}
				if (vector.x < num2)
				{
					num2 = vector.x;
				}
				if (vector.y > num3)
				{
					num3 = vector.y;
				}
				if (vector.y < num4)
				{
					num4 = vector.y;
				}
			}
			int num5 = (int)num;
			int num6 = (int)num2;
			int num7 = (int)num3;
			int num8 = (int)num4;
			int num9 = (num6 >= 0) ? 0 : Mathf.Abs(num6);
			int num10 = (num8 >= 0) ? 0 : Mathf.Abs(num8);
			bool[,] array = new bool[num5 + 3 + num9, num7 + 3 + num10];
			for (int i = 0; i < floorPlan.Count; i++)
			{
				Vector2 vector2 = floorPlan[i];
				array[(int)(vector2.x + (float)num9 + 1f), (int)(vector2.y + (float)num10 + 1f)] = true;
			}
			int num11 = Mathf.CeilToInt((float)(num5 - num6) / 2f) + 1;
			int num12 = Mathf.CeilToInt((float)(num7 - num8) / 2f) + 1;
			Vector2 vector3 = SilhouetteUtility.FindStartingPoint(floorPlan, array, num9, num10);
			List<Vector2> list = new List<Vector2>();
			HashSet<Vector2> hashSet = new HashSet<Vector2>();
			list.Add(vector3);
			hashSet.Add(vector3);
			bool[,] array2 = array;
			int length = array2.GetLength(0);
			int length2 = array2.GetLength(1);
			for (int j = 0; j < length; j++)
			{
				for (int k = 0; k < length2; k++)
				{
					bool flag = array2[j, k];
					bool flag2 = false;
					int num13 = 1;
					while (num13 <= 3 && !flag2)
					{
						Vector2[] array3 = new Vector2[]
						{
							new Vector2(0f, (float)num13),
							new Vector2((float)(-(float)num13), 0f),
							new Vector2(0f, (float)(-(float)num13)),
							new Vector2((float)num13, 0f)
						};
						Vector2 a = list.LastOrDefault<Vector2>();
						int num14 = 0;
						while (num14 < array3.Length && !flag2)
						{
							Vector2 item = new Vector2(a.x, a.y) + array3[num14];
							if (!hashSet.Contains(item))
							{
								if (item.x >= 0f && item.y >= 0f && item.x < (float)array.GetLength(0) && item.y < (float)array.GetLength(1))
								{
									if (!array[(int)item.x, (int)item.y])
									{
										for (int l = 0; l < SilhouetteUtility._allDirectionVectors.Length; l++)
										{
											Vector2 vector4 = new Vector2(item.x, item.y) + SilhouetteUtility._allDirectionVectors[l];
											if (vector4.x >= 0f && vector4.y >= 0f && vector4.x < (float)array.GetLength(0) && vector4.y < (float)array.GetLength(1))
											{
												if (array[(int)vector4.x, (int)vector4.y])
												{
													list.Add(item);
													hashSet.Add(item);
													flag2 = true;
												}
											}
										}
									}
								}
							}
							num14++;
						}
						if (!flag2 && Vector2.Distance(a, vector3) > 4f)
						{
							Vector2? closestUnclaimedPoint = SilhouetteUtility.GetClosestUnclaimedPoint(array, list);
							if (closestUnclaimedPoint != null)
							{
								flag2 = true;
								list.Add(closestUnclaimedPoint.Value);
								hashSet.Add(closestUnclaimedPoint.Value);
							}
						}
						num13++;
					}
					if (!flag2)
					{
						goto IL_49E;
					}
				}
			}
			IL_49E:
			for (int m = 0; m < list.Count; m++)
			{
				Vector2 vector5 = list[m];
				list[m] = new Vector2(vector5.x - (float)num11 - 1f, vector5.y - (float)num12 - 1f);
			}
			return list;
		}

		private static Vector2? GetClosestUnclaimedPoint(bool[,] projectionMatrix, List<Vector2> silhouette)
		{
			Vector2 vector = silhouette.LastOrDefault<Vector2>();
			int num = (int)vector.x;
			int num2 = (int)vector.y;
			int num3 = 3;
			for (int i = 1; i <= num3; i++)
			{
				for (int j = num - i; j <= num + i; j++)
				{
					for (int k = num2 - i; k <= num2 + i; k++)
					{
						if (j >= 0 && j < projectionMatrix.GetLength(0) && k >= 0 && k < projectionMatrix.GetLength(1))
						{
							if (!projectionMatrix[j, k] && !silhouette.Contains(new Vector2((float)j, (float)k)))
							{
								return new Vector2?(new Vector2((float)j, (float)k));
							}
						}
					}
				}
			}
			return null;
		}

		private static Vector2 FindStartingPoint(List<Vector2> floorPlan, bool[,] projectionMatrix, int xshifter, int yshifter)
		{
			for (int i = 0; i < floorPlan.Count; i++)
			{
				Vector2 a = new Vector2((float)((int)(floorPlan[i].x + (float)xshifter + 1f)), (float)((int)(floorPlan[i].y + (float)yshifter + 1f)));
				foreach (Vector2 b in SilhouetteUtility._allDirectionVectors)
				{
					Vector2 result = a + b;
					if (!projectionMatrix[(int)result.x, (int)result.y])
					{
						return result;
					}
				}
			}
			Debug.Log("Silhouette failed to find next point");
			return Vector2.zero;
		}

		public static Texture GenerateTexture(List<Vector2> floorVectors, Color pixelColor, Vector2 textureSize)
		{
			Texture2D texture2D = new Texture2D((int)textureSize.x, (int)textureSize.y);
			texture2D.name = "Silhouette " + Guid.NewGuid().ToString();
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			foreach (Vector2 vector in floorVectors)
			{
				if (vector.x < num)
				{
					num = vector.x;
				}
				else if (vector.x > num2)
				{
					num2 = vector.x;
				}
				if (vector.y < num3)
				{
					num3 = vector.y;
				}
				else if (vector.y > num4)
				{
					num4 = vector.y;
				}
			}
			num = (num + num2) / 2f;
			num3 = (num3 + num4) / 2f;
			bool[,] array = new bool[texture2D.width, texture2D.height];
			foreach (Vector2 vector2 in floorVectors)
			{
				int num5 = (int)vector2.x + (array.GetLength(0) / 2 - (int)num);
				int num6 = (int)vector2.y + (array.GetLength(1) / 2 - (int)num3);
				if (num5 >= 0 && num5 < array.GetLength(0) && num6 >= 0 && num6 < array.GetLength(1))
				{
					array[num5, num6] = true;
				}
			}
			for (int i = 0; i < array.GetLength(0); i++)
			{
				for (int j = 0; j < array.GetLength(1); j++)
				{
					if (array[i, j])
					{
						texture2D.SetPixel(i, j, pixelColor);
					}
					else
					{
						texture2D.SetPixel(i, j, new Color(0f, 0f, 0f, 0f));
					}
				}
			}
			texture2D.Apply();
			return texture2D;
		}

		public static Texture GenerateTexture(List<Vector2> floorVectors, Color pixelColor, List<Vector2> subSection, Color subSectionColor)
		{
			List<Vector2> list = SilhouetteUtility.ScaleFloorplan(floorVectors, 3);
			List<Vector2> list2 = SilhouetteUtility.ScaleFloorplan(subSection, 3);
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			foreach (Vector2 vector in list)
			{
				if (vector.x < num)
				{
					num = vector.x;
				}
				else if (vector.x > num2)
				{
					num2 = vector.x;
				}
				if (vector.y < num3)
				{
					num3 = vector.y;
				}
				else if (vector.y > num4)
				{
					num4 = vector.y;
				}
			}
			num = (num + num2) / 2f;
			num3 = (num3 + num4) / 2f;
			int width = 3 * (int)(num2 - num);
			int height = 3 * (int)(num4 - num3);
			Texture2D texture2D = new Texture2D(width, height);
			texture2D.name = "Silhouette2 " + Guid.NewGuid().ToString();
			int[,] array = new int[texture2D.width, texture2D.height];
			foreach (Vector2 rhs in list)
			{
				int num5 = (int)rhs.x + (array.GetLength(0) / 2 - (int)num);
				int num6 = (int)rhs.y + (array.GetLength(1) / 2 - (int)num3);
				if (num5 >= 0 && num5 < array.GetLength(0) && num6 >= 0 && num6 < array.GetLength(1))
				{
					array[num5, num6] = 1;
					if (list2 != null)
					{
						for (int i = subSection.Count - 1; i >= 0; i--)
						{
							if (!(list2[i] != rhs))
							{
								array[num5, num6] = 2;
								list2.RemoveAt(i);
								break;
							}
						}
					}
				}
			}
			for (int j = 0; j < array.GetLength(0); j++)
			{
				for (int k = 0; k < array.GetLength(1); k++)
				{
					if (array[j, k] == 1)
					{
						texture2D.SetPixel(j, k, pixelColor);
					}
					else if (array[j, k] == 2)
					{
						texture2D.SetPixel(j, k, subSectionColor);
					}
					else
					{
						texture2D.SetPixel(j, k, new Color(0f, 0f, 0f, 0f));
					}
				}
			}
			texture2D.Apply();
			return texture2D;
		}

		private static readonly Vector2[] _airlockHorizontalSubItemPositions = new Vector2[]
		{
			new Vector2(-2f, 0.5f),
			new Vector2(-1f, 0.5f),
			new Vector2(0f, 0.5f),
			new Vector2(1f, 0.5f),
			new Vector2(2f, 0.5f),
			new Vector2(-2f, -0.5f),
			new Vector2(2f, -0.5f)
		};

		private static readonly Vector2[] _airlockVerticalSubItemPositions = new Vector2[]
		{
			new Vector2(-0.5f, 0f),
			new Vector2(-0.5f, 1f),
			new Vector2(-0.5f, 2f),
			new Vector2(-0.5f, -1f),
			new Vector2(-0.5f, -2f),
			new Vector2(0.5f, 2f),
			new Vector2(0.5f, -2f)
		};

		private static readonly Vector2[] _1x3SlantHorizontalItemPositions = new Vector2[]
		{
			new Vector2(0f, -1f),
			new Vector2(0f, 1f)
		};

		private static readonly Vector2[] _1x3SlantVerticalItemPositions = new Vector2[]
		{
			new Vector2(-1f, 0f),
			new Vector2(1f, 0f)
		};

		private static readonly Vector2[] _1x2SlantHorizontalItemPositions = new Vector2[]
		{
			new Vector2(0f, -0.5f),
			new Vector2(0f, 0.5f)
		};

		private static readonly Vector2[] _1x2SlantVerticalItemPositions = new Vector2[]
		{
			new Vector2(-0.5f, 0f),
			new Vector2(0.5f, 0f)
		};

		private static Vector2[] _allDirectionVectors = new Vector2[]
		{
			new Vector2(0f, 1f),
			new Vector2(-1f, 1f),
			new Vector2(-1f, 0f),
			new Vector2(-1f, -1f),
			new Vector2(0f, -1f),
			new Vector2(1f, -1f),
			new Vector2(1f, 0f),
			new Vector2(1f, 1f)
		};
	}
}
