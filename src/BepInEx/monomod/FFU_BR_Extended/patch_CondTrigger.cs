using System;
using System.Text;
using MonoMod;
// Extended condtrigger runtime for FFU_BR-specific parameters.
// Likely: this evaluates `nMaxDepth`, `strMathCond`, and `aMathOps`, plus any
// extra same-ship or room-aware trigger checks added by the API.
public class patch_CondTrigger : CondTrigger
{
	public int nMaxDepth { get; set; }
	public string strMathCond { get; set; }
	public JsonMathOp[] aMathOps
	{
		get
		{
			return this._aMathOps;
		}
		set
		{
			this._valuesWereChanged = true;
			this._aMathOps = value;
		}
	}
	private extern void orig_Init();
	private void Init()
	{
		this.orig_Init();
		this.aMathOps = new JsonMathOp[0];
		this._valuesWereChanged = false;
	}
	public extern CondTrigger orig_Clone();
	public CondTrigger Clone()
	{
		patch_CondTrigger patch_CondTrigger = this.orig_Clone() as patch_CondTrigger;
		patch_CondTrigger.nMaxDepth = this.nMaxDepth;
		patch_CondTrigger.strMathCond = this.strMathCond;
		patch_CondTrigger.aMathOps = this.aMathOps;
		return patch_CondTrigger;
	}
	public extern CondTrigger orig_CloneDeep(string strFind, string strReplace);
	public CondTrigger CloneDeep(string strFind, string strReplace)
	{
		patch_CondTrigger patch_CondTrigger = this.orig_CloneDeep(strFind, strReplace) as patch_CondTrigger;
		bool flag = this.aMathOps != null;
		if (flag)
		{
			patch_CondTrigger.aMathOps = new JsonMathOp[this.aMathOps.Length];
			for (int i = 0; i < this.aMathOps.Length; i++)
			{
				patch_CondTrigger.aMathOps[i] = new JsonMathOp();
				patch_CondTrigger.aMathOps[i].strID = this.aMathOps[i].strID;
				patch_CondTrigger.aMathOps[i].strCond = CondTrigger.CloneDeep(this.aMathOps[i].strCond, strReplace, strFind);
				patch_CondTrigger.aMathOps[i].nMathOp = this.aMathOps[i].nMathOp;
				patch_CondTrigger.aMathOps[i].fMathVal = this.aMathOps[i].fMathVal;
				patch_CondTrigger._isBlank = false;
			}
		}
		return patch_CondTrigger;
	}
	[MonoModReplace]
	public bool IsBlank()
	{
		bool flag = (this._isBlank || this._valuesWereChanged) && ((double)base.fChance < 1.0 || base.aReqs.Length != 0 || base.aForbids.Length != 0 || base.aTriggers.Length != 0 || base.aTriggersForbid.Length != 0 || base.aLowerConds.Length != 0 || this.aMathOps.Length != 0);
		if (flag)
		{
			this._isBlank = false;
		}
		return this._isBlank;
	}
	[MonoModReplace]
	public bool Triggered(CondOwner objOwner, string strIAStatsName = null, bool logOutcome = true)
	{
		bool logReason = this.logReason;
		if (logReason)
		{
			this.logReason = logOutcome;
		}
		this.strFailReasonLast = string.Empty;
		bool flag = objOwner == null;
		bool result;
		if (flag)
		{
			result = false;
		}
		else
		{
			bool flag2 = this.nMaxDepth > 0 && patch_CondTrigger.GetDepth(objOwner) > this.nMaxDepth;
			if (flag2)
			{
				result = false;
			}
			else
			{
				bool flag3 = this.IsBlank();
				if (flag3)
				{
					result = true;
				}
				else
				{
					objOwner.ValidateParent();
					SocialStats socialStats = null;
					bool flag4 = strIAStatsName != null && DataHandler.dictSocialStats.TryGetValue(strIAStatsName, out socialStats);
					if (flag4)
					{
						socialStats.nChecked++;
					}
					bool flag5 = !CondTrigger.bChanceSkip && base.fChance < 1f;
					if (flag5)
					{
						float num = MathUtils.Rand(0f, 1f, 0, null);
						bool flag6 = num > base.fChance;
						if (flag6)
						{
							bool flag7 = socialStats != null;
							if (flag7)
							{
								socialStats.nChecked++;
							}
							bool logReason2 = this.logReason;
							if (logReason2)
							{
								this.strFailReasonLast = string.Format("Chance: {0} / {1}", num, base.fChance);
							}
							return false;
						}
					}
					bool bAND = this.bAND;
					if (bAND)
					{
						bool flag8 = this.strMathCond != null;
						Condition condition;
						if (flag8)
						{
							condition = null;
							double num2 = 0.0;
							objOwner.mapConds.TryGetValue(this.strMathCond, out condition);
							bool flag9 = condition != null;
							if (flag9)
							{
								num2 = condition.fCount;
							}
							JsonMathOp[] aMathOps = this.aMathOps;
							foreach (JsonMathOp jsonMathOp in aMathOps)
							{
								bool flag10 = string.IsNullOrEmpty(jsonMathOp.strID);
								if (!flag10)
								{
									bool flag11 = jsonMathOp.strCond == null;
									if (flag11)
									{
										int nMathOp = jsonMathOp.nMathOp;
										double num3 = (double)jsonMathOp.fMathVal;
										bool flag12 = !patch_CondTrigger.MathTrigger(nMathOp, num2, num3);
										if (flag12)
										{
											bool logReason3 = this.logReason;
											if (logReason3)
											{
												this.strFailReasonLast = "Math Lacking: " + string.Format("{0} ({1}) is {2} {3}", new object[]
												{
													this.strMathCond,
													num2,
													patch_CondTrigger.MathToString(nMathOp),
													num3
												});
											}
											return false;
										}
									}
									else
									{
										bool flag13 = objOwner.mapConds.TryGetValue(jsonMathOp.strCond, out condition);
										if (flag13)
										{
											int nMathOp2 = jsonMathOp.nMathOp;
											double num4 = (double)jsonMathOp.fMathVal;
											double num5 = condition.fCount * (double)jsonMathOp.fMathVal;
											bool flag14 = !patch_CondTrigger.MathTrigger(nMathOp2, num2, num5);
											if (flag14)
											{
												bool logReason4 = this.logReason;
												if (logReason4)
												{
													this.strFailReasonLast = "Math Lacking: " + string.Format("{0} ({1}) is {2} ", this.strMathCond, num2, patch_CondTrigger.MathToString(nMathOp2)) + string.Format("{0}% of {1} ({2})", patch_CondTrigger.FormatTwoDecimals(num4 * 100.0), jsonMathOp, num5);
												}
												return false;
											}
										}
									}
								}
							}
						}
						bool flag15 = base.strHigherCond != null;
						if (flag15)
						{
							condition = null;
							double num6 = 0.0;
							objOwner.mapConds.TryGetValue(base.strHigherCond, out condition);
							bool flag16 = condition != null;
							if (flag16)
							{
								num6 = condition.fCount;
							}
							string[] aLowerConds = base.aLowerConds;
							foreach (string text in aLowerConds)
							{
								condition = null;
								double num7 = objOwner.mapConds.TryGetValue(text, out condition) ? condition.fCount : 0.0;
								bool flag17 = num7 > num6;
								if (flag17)
								{
									bool logReason5 = this.logReason;
									if (logReason5)
									{
										this.strFailReasonLast = "Higher Lacking: " + string.Format("{0} ({1}) is higher than {2} ({3})", new object[]
										{
											base.strHigherCond,
											num6,
											text,
											num7
										});
									}
									return false;
								}
							}
						}
						string[] aReqs = base.aReqs;
						foreach (string text2 in aReqs)
						{
							bool flag18 = !objOwner.mapConds.TryGetValue(text2, out condition);
							if (flag18)
							{
								base.StatsTrackReqs(strIAStatsName, text2, 1f);
								bool logReason6 = this.logReason;
								if (logReason6)
								{
									this.strFailReasonLast = "Lacking: " + text2;
								}
								return false;
							}
							bool flag19 = condition == null || condition.fCount <= 0.0;
							if (flag19)
							{
								base.StatsTrackReqs(strIAStatsName, text2, 1f);
								bool logReason7 = this.logReason;
								if (logReason7)
								{
									this.strFailReasonLast = "Lacking: " + text2;
								}
								return false;
							}
						}
						condition = null;
						string[] aForbids = base.aForbids;
						foreach (string text3 in aForbids)
						{
							bool flag20 = objOwner.mapConds.TryGetValue(text3, out condition) && condition.fCount > 0.0;
							if (flag20)
							{
								base.StatsTrackForbids(strIAStatsName, text3, 1f);
								bool logReason8 = this.logReason;
								if (logReason8)
								{
									this.strFailReasonLast = "Forbidden: " + text3;
								}
								return false;
							}
						}
						string[] aTriggers = base.aTriggers;
						foreach (string text4 in aTriggers)
						{
							CondTrigger trigger = base.GetTrigger(text4, 0);
							bool flag21 = !trigger.Triggered(objOwner, strIAStatsName, this.logReason);
							if (flag21)
							{
								bool logReason9 = this.logReason;
								if (logReason9)
								{
									this.strFailReasonLast = trigger.strFailReasonLast;
								}
								return false;
							}
						}
						result = true;
					}
					else
					{
						string[] aForbids2 = base.aForbids;
						foreach (string text5 in aForbids2)
						{
							Condition condition;
							bool flag22 = objOwner.mapConds.TryGetValue(text5, out condition) && condition.fCount > 0.0;
							if (flag22)
							{
								base.StatsTrackForbids(strIAStatsName, text5, 1f);
								bool logReason10 = this.logReason;
								if (logReason10)
								{
									this.strFailReasonLast = "Forbidden: " + text5;
								}
								return false;
							}
						}
						string[] aTriggersForbid = base.aTriggersForbid;
						foreach (string text6 in aTriggersForbid)
						{
							CondTrigger trigger2 = base.GetTrigger(text6, 1);
							bool flag23 = !trigger2.Triggered(objOwner, strIAStatsName, this.logReason);
							if (flag23)
							{
								bool logReason11 = this.logReason;
								if (logReason11)
								{
									this.strFailReasonLast = trigger2.strFailReasonLast;
								}
								return false;
							}
						}
						string text7 = "Math Lacking: (";
						bool flag24 = false;
						bool flag25 = false;
						bool flag26 = this.strMathCond != null;
						if (flag26)
						{
							Condition condition = null;
							int mOperation = 0;
							double mTarget = 0.0;
							double num9 = 0.0;
							double num10 = 0.0;
							objOwner.mapConds.TryGetValue(this.strMathCond, out condition);
							bool flag27 = condition != null;
							if (flag27)
							{
								mTarget = condition.fCount;
							}
							JsonMathOp[] aMathOps2 = this.aMathOps;
							foreach (JsonMathOp jsonMathOp2 in aMathOps2)
							{
								bool flag28 = string.IsNullOrEmpty(jsonMathOp2.strID);
								if (!flag28)
								{
									bool flag29 = jsonMathOp2.strCond == null;
									if (flag29)
									{
										mOperation = jsonMathOp2.nMathOp;
										num10 = (double)jsonMathOp2.fMathVal;
										bool flag30 = patch_CondTrigger.MathTrigger(mOperation, mTarget, num10);
										if (flag30)
										{
											return true;
										}
									}
									else
									{
										bool flag31 = objOwner.mapConds.TryGetValue(jsonMathOp2.strCond, out condition);
										if (flag31)
										{
											mOperation = jsonMathOp2.nMathOp;
											num9 = (double)jsonMathOp2.fMathVal;
											num10 = condition.fCount * num9;
											bool flag32 = patch_CondTrigger.MathTrigger(mOperation, mTarget, num10);
											if (flag32)
											{
												return true;
											}
										}
									}
									bool logReason12 = this.logReason;
									if (logReason12)
									{
										bool flag33 = jsonMathOp2.strCond == null;
										if (flag33)
										{
											bool flag34 = flag24;
											if (flag34)
											{
												text7 = text7 + ", " + string.Format("{0} is {1} {2}", this.strMathCond, patch_CondTrigger.MathToString(mOperation), num10);
											}
											else
											{
												text7 += string.Format("{0} is {1} {2}", this.strMathCond, patch_CondTrigger.MathToString(mOperation), num10);
											}
										}
										else
										{
											bool flag35 = flag24;
											if (flag35)
											{
												text7 = string.Concat(new string[]
												{
													text7,
													", ",
													this.strMathCond,
													" is ",
													patch_CondTrigger.MathToString(mOperation),
													" ",
													string.Format("{0}% of {1}", patch_CondTrigger.FormatTwoDecimals(num9 * 100.0), jsonMathOp2)
												});
											}
											else
											{
												text7 = string.Concat(new string[]
												{
													text7,
													this.strMathCond,
													" is ",
													patch_CondTrigger.MathToString(mOperation),
													" ",
													string.Format("{0}% of {1}", patch_CondTrigger.FormatTwoDecimals(num9 * 100.0), jsonMathOp2)
												});
											}
										}
									}
									flag24 = true;
								}
							}
						}
						else
						{
							flag25 = true;
						}
						bool flag36 = flag24 && this.logReason;
						if (flag36)
						{
							this.strFailReasonLast = this.strFailReasonLast + ((this.strFailReasonLast.Length > 0) ? " " : "") + text7 + ")";
						}
						bool logReason13 = this.logReason;
						if (logReason13)
						{
							text7 = "Higher Lacking: (";
						}
						flag24 = false;
						bool flag37 = false;
						bool flag38 = base.strHigherCond != null;
						if (flag38)
						{
							Condition condition = null;
							double num12 = 0.0;
							objOwner.mapConds.TryGetValue(base.strHigherCond, out condition);
							bool flag39 = condition != null;
							if (flag39)
							{
								num12 = condition.fCount;
							}
							string[] aLowerConds2 = base.aLowerConds;
							foreach (string text8 in aLowerConds2)
							{
								condition = null;
								bool flag40 = objOwner.mapConds.TryGetValue(text8, out condition) && condition.fCount <= num12;
								if (flag40)
								{
									return true;
								}
								bool logReason14 = this.logReason;
								if (logReason14)
								{
									bool flag41 = flag24;
									if (flag41)
									{
										text7 = string.Concat(new string[]
										{
											text7,
											", ",
											base.strHigherCond,
											" is higher than ",
											text8
										});
									}
									else
									{
										text7 = text7 + base.strHigherCond + " is higher than " + text8;
									}
								}
								flag24 = true;
							}
						}
						else
						{
							flag37 = true;
						}
						bool flag42 = flag24 && this.logReason;
						if (flag42)
						{
							this.strFailReasonLast = this.strFailReasonLast + ((this.strFailReasonLast.Length > 0) ? " " : "") + text7 + ")";
						}
						bool logReason15 = this.logReason;
						if (logReason15)
						{
							text7 = "Reqs Lacking: (";
						}
						flag24 = false;
						string[] aReqs2 = base.aReqs;
						foreach (string text9 in aReqs2)
						{
							Condition condition;
							bool flag43 = objOwner.mapConds.TryGetValue(text9, out condition) && condition != null && condition.fCount > 0.0;
							if (flag43)
							{
								return true;
							}
							bool logReason16 = this.logReason;
							if (logReason16)
							{
								bool flag44 = flag24;
								if (flag44)
								{
									text7 = text7 + ", " + text9;
								}
								else
								{
									text7 += text9;
								}
							}
							flag24 = true;
						}
						bool flag45 = flag24 && this.logReason;
						if (flag45)
						{
							this.strFailReasonLast = this.strFailReasonLast + ((this.strFailReasonLast.Length > 0) ? " " : "") + text7 + ")";
						}
						bool logReason17 = this.logReason;
						if (logReason17)
						{
							text7 = "Triggers Lacking: (";
						}
						flag24 = false;
						string[] aTriggers2 = base.aTriggers;
						foreach (string text10 in aTriggers2)
						{
							CondTrigger trigger3 = base.GetTrigger(text10, 0);
							bool flag46 = trigger3.Triggered(objOwner, strIAStatsName, this.logReason);
							if (flag46)
							{
								return true;
							}
							bool logReason18 = this.logReason;
							if (logReason18)
							{
								bool flag47 = flag24;
								if (flag47)
								{
									text7 = string.Concat(new string[]
									{
										text7,
										", ",
										text10,
										" (",
										trigger3.strFailReasonLast,
										")"
									});
								}
								else
								{
									text7 = string.Concat(new string[]
									{
										text7,
										text10,
										" (",
										trigger3.strFailReasonLast,
										")"
									});
								}
							}
							flag24 = true;
						}
						bool flag48 = flag24 && this.logReason;
						if (flag48)
						{
							this.strFailReasonLast = this.strFailReasonLast + ((this.strFailReasonLast.Length > 0) ? " " : "") + text7 + ")";
						}
						bool flag49 = base.aReqs.Length + base.aTriggers.Length == 0 && flag25 && flag37;
						if (flag49)
						{
							result = true;
						}
						else
						{
							string[] aReqs3 = base.aReqs;
							foreach (string text11 in aReqs3)
							{
								base.StatsTrackReqs(strIAStatsName, text11, 1f / (float)base.aReqs.Length);
							}
							result = false;
						}
					}
				}
			}
		}
		return result;
	}
	public string RulesInfo
	{
		[MonoModReplace]
		get
		{
			bool flag = this.strFailReason != null;
			string strFailReason;
			if (flag)
			{
				strFailReason = this.strFailReason;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				bool bAND = this.bAND;
				if (bAND)
				{
					for (int i = 0; i < base.aReqs.Length; i++)
					{
						bool flag2 = i == 0;
						if (flag2)
						{
							bool flag3 = stringBuilder.Length > 0;
							if (flag3)
							{
								stringBuilder.Append(" ");
							}
							stringBuilder.Append("Is ");
						}
						else
						{
							bool flag4 = i == base.aReqs.Length - 1;
							if (flag4)
							{
								stringBuilder.Append(", and ");
							}
							else
							{
								bool flag5 = i > 0;
								if (flag5)
								{
									stringBuilder.Append(", ");
								}
							}
						}
						Condition cond = DataHandler.GetCond(base.aReqs[i]);
						stringBuilder.Append(cond.strNameFriendly);
						bool flag6 = i == base.aReqs.Length - 1;
						if (flag6)
						{
							stringBuilder.Append(".");
						}
					}
					for (int j = 0; j < base.aForbids.Length; j++)
					{
						bool flag7 = j == 0;
						if (flag7)
						{
							bool flag8 = stringBuilder.Length > 0;
							if (flag8)
							{
								stringBuilder.Append(" ");
							}
							stringBuilder.Append("Is NOT ");
						}
						else
						{
							bool flag9 = j == base.aForbids.Length - 1;
							if (flag9)
							{
								stringBuilder.Append(", and ");
							}
							else
							{
								bool flag10 = j > 0;
								if (flag10)
								{
									stringBuilder.Append(", ");
								}
							}
						}
						Condition cond = DataHandler.GetCond(base.aForbids[j]);
						stringBuilder.Append(cond.strNameFriendly);
						bool flag11 = j == base.aForbids.Length - 1;
						if (flag11)
						{
							stringBuilder.Append(".");
						}
					}
					bool flag12 = this.strMathCond != null && this.aMathOps.Length != 0;
					if (flag12)
					{
						Condition cond = DataHandler.GetCond(this.strMathCond);
						bool flag13 = stringBuilder.Length > 0;
						if (flag13)
						{
							stringBuilder.Append(" And ");
						}
						stringBuilder.Append(cond.strNameFriendly + " is");
						for (int k = 0; k < this.aMathOps.Length; k++)
						{
							bool flag14 = string.IsNullOrEmpty(this.aMathOps[k].strID);
							if (!flag14)
							{
								bool flag15 = k == 0;
								if (flag15)
								{
									stringBuilder.Append(" ");
								}
								else
								{
									bool flag16 = k == this.aMathOps.Length - 1;
									if (flag16)
									{
										stringBuilder.Append(", and ");
									}
									else
									{
										bool flag17 = k > 0;
										if (flag17)
										{
											stringBuilder.Append(", ");
										}
									}
								}
								bool flag18 = this.aMathOps[k].strCond == null;
								if (flag18)
								{
									stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[k].nMathOp) + " ");
									stringBuilder.Append(this.aMathOps[k].fMathVal);
								}
								else
								{
									cond = DataHandler.GetCond(this.aMathOps[k].strCond);
									stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[k].nMathOp) + " ");
									stringBuilder.Append(patch_CondTrigger.FormatTwoDecimals((double)this.aMathOps[k].fMathVal * 100.0) + "% of ");
									stringBuilder.Append(cond.strNameFriendly);
								}
								bool flag19 = k == this.aMathOps.Length - 1;
								if (flag19)
								{
									stringBuilder.Append(".");
								}
							}
						}
					}
					bool flag20 = base.strHigherCond != null && base.aLowerConds.Length != 0;
					if (flag20)
					{
						Condition cond = DataHandler.GetCond(base.strHigherCond);
						bool flag21 = stringBuilder.Length > 0;
						if (flag21)
						{
							stringBuilder.Append(" And ");
						}
						stringBuilder.Append(cond.strNameFriendly + " is higher than");
						for (int l = 0; l < base.aLowerConds.Length; l++)
						{
							bool flag22 = l == 0;
							if (flag22)
							{
								stringBuilder.Append(" ");
							}
							else
							{
								bool flag23 = l == base.aLowerConds.Length - 1;
								if (flag23)
								{
									stringBuilder.Append(", and ");
								}
								else
								{
									bool flag24 = l > 0;
									if (flag24)
									{
										stringBuilder.Append(", ");
									}
								}
							}
							cond = DataHandler.GetCond(base.aLowerConds[l]);
							stringBuilder.Append(cond.strNameFriendly);
							bool flag25 = l == base.aLowerConds.Length - 1;
							if (flag25)
							{
								stringBuilder.Append(".");
							}
						}
					}
					string[] aTriggers = base.aTriggers;
					foreach (string text in aTriggers)
					{
						bool flag26 = stringBuilder.Length > 0;
						if (flag26)
						{
							stringBuilder.Append(" And ");
						}
						CondTrigger trigger = base.GetTrigger(text, 0);
						stringBuilder.Append("{" + trigger.RulesInfo + "}");
					}
				}
				else
				{
					for (int n = 0; n < base.aForbids.Length; n++)
					{
						bool flag27 = n == 0;
						if (flag27)
						{
							bool flag28 = stringBuilder.Length > 0;
							if (flag28)
							{
								stringBuilder.Append(" ");
							}
							stringBuilder.Append("Is NOT ");
						}
						else
						{
							bool flag29 = n == base.aForbids.Length - 1;
							if (flag29)
							{
								stringBuilder.Append(", or ");
							}
							else
							{
								bool flag30 = n > 0;
								if (flag30)
								{
									stringBuilder.Append(", ");
								}
							}
						}
						Condition cond = DataHandler.GetCond(base.aForbids[n]);
						stringBuilder.Append(cond.strNameFriendly);
						bool flag31 = n == base.aForbids.Length - 1;
						if (flag31)
						{
							stringBuilder.Append(".");
						}
					}
					for (int num = 0; num < base.aReqs.Length; num++)
					{
						bool flag32 = num == 0;
						if (flag32)
						{
							bool flag33 = stringBuilder.Length > 0;
							if (flag33)
							{
								stringBuilder.Append(" ");
							}
							stringBuilder.Append("Is ");
						}
						else
						{
							bool flag34 = num == base.aReqs.Length - 1;
							if (flag34)
							{
								stringBuilder.Append(", or ");
							}
							else
							{
								bool flag35 = num > 0;
								if (flag35)
								{
									stringBuilder.Append(", ");
								}
							}
						}
						Condition cond = DataHandler.GetCond(base.aReqs[num]);
						stringBuilder.Append(cond.strNameFriendly);
						bool flag36 = num == base.aReqs.Length - 1;
						if (flag36)
						{
							stringBuilder.Append(".");
						}
					}
					bool flag37 = this.strMathCond != null && this.aMathOps.Length != 0;
					if (flag37)
					{
						Condition cond = DataHandler.GetCond(this.strMathCond);
						bool flag38 = stringBuilder.Length > 0;
						if (flag38)
						{
							stringBuilder.Append(" Or ");
						}
						stringBuilder.Append(cond.strNameFriendly + " is");
						for (int num2 = 0; num2 < this.aMathOps.Length; num2++)
						{
							bool flag39 = string.IsNullOrEmpty(this.aMathOps[num2].strID);
							if (!flag39)
							{
								bool flag40 = num2 == 0;
								if (flag40)
								{
									stringBuilder.Append(" ");
								}
								else
								{
									bool flag41 = num2 == this.aMathOps.Length - 1;
									if (flag41)
									{
										stringBuilder.Append(", or ");
									}
									else
									{
										bool flag42 = num2 > 0;
										if (flag42)
										{
											stringBuilder.Append(", ");
										}
									}
								}
								bool flag43 = this.aMathOps[num2].strCond == null;
								if (flag43)
								{
									stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[num2].nMathOp) + " ");
									stringBuilder.Append(this.aMathOps[num2].fMathVal);
								}
								else
								{
									cond = DataHandler.GetCond(this.aMathOps[num2].strCond);
									stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[num2].nMathOp) + " ");
									stringBuilder.Append(patch_CondTrigger.FormatTwoDecimals((double)this.aMathOps[num2].fMathVal * 100.0) + "% of ");
									stringBuilder.Append(cond.strNameFriendly);
								}
								bool flag44 = num2 == this.aMathOps.Length - 1;
								if (flag44)
								{
									stringBuilder.Append(".");
								}
							}
						}
					}
					bool flag45 = base.strHigherCond != null && base.aLowerConds.Length != 0;
					if (flag45)
					{
						Condition cond = DataHandler.GetCond(base.strHigherCond);
						bool flag46 = stringBuilder.Length > 0;
						if (flag46)
						{
							stringBuilder.Append(" Or ");
						}
						stringBuilder.Append(cond.strNameFriendly + " is higher than");
						for (int num3 = 0; num3 < base.aLowerConds.Length; num3++)
						{
							bool flag47 = num3 == 0;
							if (flag47)
							{
								stringBuilder.Append(" ");
							}
							else
							{
								bool flag48 = num3 == base.aLowerConds.Length - 1;
								if (flag48)
								{
									stringBuilder.Append(", or ");
								}
								else
								{
									bool flag49 = num3 > 0;
									if (flag49)
									{
										stringBuilder.Append(", ");
									}
								}
							}
							cond = DataHandler.GetCond(base.aLowerConds[num3]);
							stringBuilder.Append(cond.strNameFriendly);
							bool flag50 = num3 == base.aLowerConds.Length - 1;
							if (flag50)
							{
								stringBuilder.Append(".");
							}
						}
					}
					string[] aTriggers2 = base.aTriggers;
					foreach (string text2 in aTriggers2)
					{
						bool flag51 = stringBuilder.Length > 0;
						if (flag51)
						{
							stringBuilder.Append(" Or ");
						}
						CondTrigger trigger2 = base.GetTrigger(text2, 0);
						stringBuilder.Append("{" + trigger2.RulesInfo + "}");
					}
					string[] aTriggersForbid = base.aTriggersForbid;
					foreach (string text3 in aTriggersForbid)
					{
						bool flag52 = stringBuilder.Length > 0;
						if (flag52)
						{
							stringBuilder.Append(" Not ");
						}
						else
						{
							stringBuilder.Append("Not ");
						}
						CondTrigger trigger3 = base.GetTrigger(text3, 1);
						stringBuilder.Append("{" + trigger3.RulesInfo + "}");
					}
				}
				this.strFailReason = stringBuilder.ToString();
				strFailReason = this.strFailReason;
			}
			return strFailReason;
		}
	}
	public string RulesInfoDev
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool bAND = this.bAND;
			if (bAND)
			{
				for (int i = 0; i < base.aReqs.Length; i++)
				{
					bool flag = i == 0;
					if (flag)
					{
						bool flag2 = stringBuilder.Length > 0;
						if (flag2)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is ");
					}
					else
					{
						bool flag3 = i == base.aReqs.Length - 1;
						if (flag3)
						{
							stringBuilder.Append(", and ");
						}
						else
						{
							bool flag4 = i > 0;
							if (flag4)
							{
								stringBuilder.Append(", ");
							}
						}
					}
					Condition cond = DataHandler.GetCond(base.aReqs[i]);
					stringBuilder.Append(cond.strName);
					bool flag5 = i == base.aReqs.Length - 1;
					if (flag5)
					{
						stringBuilder.Append(".");
					}
				}
				for (int j = 0; j < base.aForbids.Length; j++)
				{
					bool flag6 = j == 0;
					if (flag6)
					{
						bool flag7 = stringBuilder.Length > 0;
						if (flag7)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is NOT ");
					}
					else
					{
						bool flag8 = j == base.aForbids.Length - 1;
						if (flag8)
						{
							stringBuilder.Append(", and ");
						}
						else
						{
							bool flag9 = j > 0;
							if (flag9)
							{
								stringBuilder.Append(", ");
							}
						}
					}
					Condition cond = DataHandler.GetCond(base.aForbids[j]);
					stringBuilder.Append(cond.strName);
					bool flag10 = j == base.aForbids.Length - 1;
					if (flag10)
					{
						stringBuilder.Append(".");
					}
				}
				bool flag11 = this.strMathCond != null && this.aMathOps.Length != 0;
				if (flag11)
				{
					Condition cond = DataHandler.GetCond(this.strMathCond);
					bool flag12 = stringBuilder.Length > 0;
					if (flag12)
					{
						stringBuilder.Append(" And ");
					}
					stringBuilder.Append(cond.strName + " is");
					for (int k = 0; k < this.aMathOps.Length; k++)
					{
						bool flag13 = string.IsNullOrEmpty(this.aMathOps[k].strID);
						if (!flag13)
						{
							bool flag14 = k == 0;
							if (flag14)
							{
								stringBuilder.Append(" ");
							}
							else
							{
								bool flag15 = k == this.aMathOps.Length - 1;
								if (flag15)
								{
									stringBuilder.Append(", and ");
								}
								else
								{
									bool flag16 = k > 0;
									if (flag16)
									{
										stringBuilder.Append(", ");
									}
								}
							}
							bool flag17 = this.aMathOps[k].strCond == null;
							if (flag17)
							{
								stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[k].nMathOp) + " ");
								stringBuilder.Append(this.aMathOps[k].fMathVal);
							}
							else
							{
								cond = DataHandler.GetCond(this.aMathOps[k].strCond);
								stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[k].nMathOp) + " ");
								stringBuilder.Append(patch_CondTrigger.FormatTwoDecimals((double)this.aMathOps[k].fMathVal * 100.0) + "% of ");
								stringBuilder.Append(cond.strName);
							}
							bool flag18 = k == this.aMathOps.Length - 1;
							if (flag18)
							{
								stringBuilder.Append(".");
							}
						}
					}
				}
				bool flag19 = base.strHigherCond != null && base.aLowerConds.Length != 0;
				if (flag19)
				{
					Condition cond = DataHandler.GetCond(base.strHigherCond);
					bool flag20 = stringBuilder.Length > 0;
					if (flag20)
					{
						stringBuilder.Append(" And ");
					}
					stringBuilder.Append(cond.strName + " is higher than");
					for (int l = 0; l < base.aLowerConds.Length; l++)
					{
						bool flag21 = l == 0;
						if (flag21)
						{
							stringBuilder.Append(" ");
						}
						else
						{
							bool flag22 = l == base.aLowerConds.Length - 1;
							if (flag22)
							{
								stringBuilder.Append(", and ");
							}
							else
							{
								bool flag23 = l > 0;
								if (flag23)
								{
									stringBuilder.Append(", ");
								}
							}
						}
						cond = DataHandler.GetCond(base.aLowerConds[l]);
						stringBuilder.Append(cond.strName);
						bool flag24 = l == base.aLowerConds.Length - 1;
						if (flag24)
						{
							stringBuilder.Append(".");
						}
					}
				}
				string[] aTriggers = base.aTriggers;
				foreach (string text in aTriggers)
				{
					bool flag25 = stringBuilder.Length > 0;
					if (flag25)
					{
						stringBuilder.Append(" And ");
					}
					patch_CondTrigger patch_CondTrigger = base.GetTrigger(text, 0) as patch_CondTrigger;
					stringBuilder.Append("{" + patch_CondTrigger.RulesInfoDev + "}");
				}
			}
			else
			{
				for (int n = 0; n < base.aForbids.Length; n++)
				{
					bool flag26 = n == 0;
					if (flag26)
					{
						bool flag27 = stringBuilder.Length > 0;
						if (flag27)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is NOT ");
					}
					else
					{
						bool flag28 = n == base.aForbids.Length - 1;
						if (flag28)
						{
							stringBuilder.Append(", or ");
						}
						else
						{
							bool flag29 = n > 0;
							if (flag29)
							{
								stringBuilder.Append(", ");
							}
						}
					}
					Condition cond = DataHandler.GetCond(base.aForbids[n]);
					stringBuilder.Append(cond.strName);
					bool flag30 = n == base.aForbids.Length - 1;
					if (flag30)
					{
						stringBuilder.Append(".");
					}
				}
				for (int num = 0; num < base.aReqs.Length; num++)
				{
					bool flag31 = num == 0;
					if (flag31)
					{
						bool flag32 = stringBuilder.Length > 0;
						if (flag32)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is ");
					}
					else
					{
						bool flag33 = num == base.aReqs.Length - 1;
						if (flag33)
						{
							stringBuilder.Append(", or ");
						}
						else
						{
							bool flag34 = num > 0;
							if (flag34)
							{
								stringBuilder.Append(", ");
							}
						}
					}
					Condition cond = DataHandler.GetCond(base.aReqs[num]);
					stringBuilder.Append(cond.strName);
					bool flag35 = num == base.aReqs.Length - 1;
					if (flag35)
					{
						stringBuilder.Append(".");
					}
				}
				bool flag36 = this.strMathCond != null && this.aMathOps.Length != 0;
				if (flag36)
				{
					Condition cond = DataHandler.GetCond(this.strMathCond);
					bool flag37 = stringBuilder.Length > 0;
					if (flag37)
					{
						stringBuilder.Append(" Or ");
					}
					stringBuilder.Append(cond.strName + " is");
					for (int num2 = 0; num2 < this.aMathOps.Length; num2++)
					{
						bool flag38 = string.IsNullOrEmpty(this.aMathOps[num2].strID);
						if (!flag38)
						{
							bool flag39 = num2 == 0;
							if (flag39)
							{
								stringBuilder.Append(" ");
							}
							else
							{
								bool flag40 = num2 == this.aMathOps.Length - 1;
								if (flag40)
								{
									stringBuilder.Append(", or ");
								}
								else
								{
									bool flag41 = num2 > 0;
									if (flag41)
									{
										stringBuilder.Append(", ");
									}
								}
							}
							bool flag42 = this.aMathOps[num2].strCond == null;
							if (flag42)
							{
								stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[num2].nMathOp) + " ");
								stringBuilder.Append(this.aMathOps[num2].fMathVal);
							}
							else
							{
								cond = DataHandler.GetCond(this.aMathOps[num2].strCond);
								stringBuilder.Append(patch_CondTrigger.MathToString(this.aMathOps[num2].nMathOp) + " ");
								stringBuilder.Append(patch_CondTrigger.FormatTwoDecimals((double)this.aMathOps[num2].fMathVal * 100.0) + "% of ");
								stringBuilder.Append(cond.strName);
							}
							bool flag43 = num2 == this.aMathOps.Length - 1;
							if (flag43)
							{
								stringBuilder.Append(".");
							}
						}
					}
				}
				bool flag44 = base.strHigherCond != null && base.aLowerConds.Length != 0;
				if (flag44)
				{
					Condition cond = DataHandler.GetCond(base.strHigherCond);
					bool flag45 = stringBuilder.Length > 0;
					if (flag45)
					{
						stringBuilder.Append(" Or ");
					}
					stringBuilder.Append(cond.strName + " is higher than");
					for (int num3 = 0; num3 < base.aLowerConds.Length; num3++)
					{
						bool flag46 = num3 == 0;
						if (flag46)
						{
							stringBuilder.Append(" ");
						}
						else
						{
							bool flag47 = num3 == base.aLowerConds.Length - 1;
							if (flag47)
							{
								stringBuilder.Append(", or ");
							}
							else
							{
								bool flag48 = num3 > 0;
								if (flag48)
								{
									stringBuilder.Append(", ");
								}
							}
						}
						cond = DataHandler.GetCond(base.aLowerConds[num3]);
						stringBuilder.Append(cond.strName);
						bool flag49 = num3 == base.aLowerConds.Length - 1;
						if (flag49)
						{
							stringBuilder.Append(".");
						}
					}
				}
				string[] aTriggers2 = base.aTriggers;
				foreach (string text2 in aTriggers2)
				{
					bool flag50 = stringBuilder.Length > 0;
					if (flag50)
					{
						stringBuilder.Append(" Or ");
					}
					patch_CondTrigger patch_CondTrigger2 = base.GetTrigger(text2, 0) as patch_CondTrigger;
					stringBuilder.Append("{" + patch_CondTrigger2.RulesInfoDev + "}");
				}
				string[] aTriggersForbid = base.aTriggersForbid;
				foreach (string text3 in aTriggersForbid)
				{
					bool flag51 = stringBuilder.Length > 0;
					if (flag51)
					{
						stringBuilder.Append(" Not ");
					}
					else
					{
						stringBuilder.Append("Not ");
					}
					patch_CondTrigger patch_CondTrigger3 = base.GetTrigger(text3, 1) as patch_CondTrigger;
					stringBuilder.Append("{" + patch_CondTrigger3.RulesInfoDev + "}");
				}
			}
			return stringBuilder.ToString();
		}
	}
	public static int GetDepth(CondOwner objCO)
	{
		int num = 1;
		CondOwner objCOParent = objCO.objCOParent;
		while (objCOParent != null)
		{
			objCOParent = objCOParent.objCOParent;
			num++;
		}
		return num;
	}
	public static bool MathTrigger(int mOperation, double mTarget, double mValue)
	{
		if (!true)
		{
		}
		bool result;
		switch (mOperation)
		{
		case 1:
			result = (mTarget != mValue);
			break;
		case 2:
			result = (mTarget == mValue);
			break;
		case 3:
			result = (mTarget > mValue);
			break;
		case 4:
			result = (mTarget >= mValue);
			break;
		case 5:
			result = (mTarget < mValue);
			break;
		case 6:
			result = (mTarget <= mValue);
			break;
		default:
			result = true;
			break;
		}
		if (!true)
		{
		}
		return result;
	}
	public static string MathToString(int mOperation)
	{
		if (!true)
		{
		}
		string result;
		switch (mOperation)
		{
		case 1:
			result = "not equal to";
			break;
		case 2:
			result = "equal to";
			break;
		case 3:
			result = "greater than";
			break;
		case 4:
			result = "greater or equal to";
			break;
		case 5:
			result = "less than";
			break;
		case 6:
			result = "less or equal to";
			break;
		default:
			result = "invalid for";
			break;
		}
		if (!true)
		{
		}
		return result;
	}
	public static string FormatTwoDecimals(double dVal)
	{
		double num = Math.Abs(dVal);
		bool flag = num >= 1.0;
		string result;
		if (flag)
		{
			result = dVal.ToString("0.##");
		}
		else
		{
			bool flag2 = dVal == 0.0;
			if (flag2)
			{
				result = "0";
			}
			else
			{
				int num2 = 0;
				int num3 = 0;
				double num4 = num;
				bool flag3 = false;
				while (num2 < 1 && num4 > 0.0 && !flag3)
				{
					num4 *= 10.0;
					num3++;
					int num5 = (int)num4 % 10;
					bool flag4 = num3 == 20;
					if (flag4)
					{
						flag3 = true;
					}
					bool flag5 = num5 != 0;
					if (flag5)
					{
						num2++;
						num3++;
					}
				}
				bool flag6 = flag3;
				if (flag6)
				{
					result = "0";
				}
				else
				{
					string format = "0." + new string('#', num3);
					result = dVal.ToString(format);
				}
			}
		}
		return result;
	}
	private JsonMathOp[] _aMathOps;
}
