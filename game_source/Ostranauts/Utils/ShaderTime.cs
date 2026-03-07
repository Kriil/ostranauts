using System;
using UnityEngine;

namespace Ostranauts.Utils
{
	public class ShaderTime : MonoBehaviour
	{
		private void LateUpdate()
		{
			Shader.SetGlobalFloat("_UnscaledTime", Time.unscaledTime / 20f);
		}
	}
}
