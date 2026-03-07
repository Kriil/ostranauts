using System;
using UnityEngine;

[CreateAssetMenu]
public class AnimationCurveAsset : ScriptableObject
{
	public static implicit operator AnimationCurve(AnimationCurveAsset me)
	{
		return me.curve;
	}

	public static implicit operator AnimationCurveAsset(AnimationCurve curve)
	{
		AnimationCurveAsset animationCurveAsset = ScriptableObject.CreateInstance<AnimationCurveAsset>();
		animationCurveAsset.curve = curve;
		return animationCurveAsset;
	}

	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}
