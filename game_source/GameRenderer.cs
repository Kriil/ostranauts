using System;
using System.Collections;
using Ostranauts.Rendering;
using UnityEngine;

// Central render-composition camera. Likely the main scene camera that renders
// multiple passes (albedo, normals, depth, parallax, AO) into shared textures,
// then composites the final view in OnRenderImage.
public class GameRenderer : MonoBehaviour
{
	// Looks up the active scene camera named "Main Camera" so other systems can
	// share one render target owner instead of caching camera references.
	public static Camera MainCamera
	{
		get
		{
			Camera[] allCameras = Camera.allCameras;
			foreach (Camera camera in allCameras)
			{
				if (camera.isActiveAndEnabled && camera.name == "Main Camera")
				{
					return camera;
				}
			}
			return null;
		}
	}

	public static int Width
	{
		get
		{
			if (GameRenderer.RTAlbedo != null)
			{
				return GameRenderer.RTAlbedo.width;
			}
			return Screen.width;
		}
	}

	public static int Height
	{
		get
		{
			if (GameRenderer.RTAlbedo != null)
			{
				return GameRenderer.RTAlbedo.height;
			}
			return Screen.height;
		}
	}

	// Reads graphics-related PlayerPrefs used by the main camera instance.
	private void Start()
	{
		Camera component = base.GetComponent<Camera>();
		if (component != null && GameRenderer.MainCamera == component)
		{
			this.HideLoS = (PlayerPrefs.GetInt("LineOfSight", 1) == 0);
		}
		this.AOLoops = Mathf.Clamp(PlayerPrefs.GetInt("AmbientOcclusion", 8), 0, 8);
		this.ShowParallax = (PlayerPrefs.GetInt("Parallax", 1) == 1);
		this.AOIntensity = float.Parse(PlayerPrefs.GetString("AOIntensity", "0.3"));
		this.AOFade = float.Parse(PlayerPrefs.GetString("AOFade", "0.4"));
		this.AOZoomBase = float.Parse(PlayerPrefs.GetString("AOZoomBase", "22.5"));
	}

	// Recreates the shared intermediate render targets when the screen size or
	// dynamic render size changes, then notifies Visibility lights to rebuild.
	private void SetWidthHeight(int width, int height)
	{
		if (GameRenderer.RTAlbedo != null && (GameRenderer.RTAlbedo.width != width || GameRenderer.RTAlbedo.height != height))
		{
			GameRenderer.RTAlbedo.Release();
			GameRenderer.RTAlbedo = null;
			GameRenderer.RTAlpha.Release();
			GameRenderer.RTAlpha = null;
			GameRenderer.RTParallax.Release();
			GameRenderer.RTParallax = null;
			GameRenderer.RTNormal.Release();
			GameRenderer.RTNormal = null;
			GameRenderer.RTDepth.Release();
			GameRenderer.RTDepth = null;
			GameRenderer.RTAO.Release();
			GameRenderer.RTAO = null;
		}
		if (GameRenderer.RTAlbedo == null)
		{
			GameRenderer.RTAlbedo = new RenderTexture(width, height, 24);
			GameRenderer.RTAlbedo.name = "Dynamic Albedo RT";
			GameRenderer.RTAlpha = new RenderTexture(width, height, 24);
			GameRenderer.RTAlpha.name = "Dynamic AlphaStencil RT";
			GameRenderer.RTParallax = new RenderTexture(width, height, 24);
			GameRenderer.RTParallax.name = "Dynamic Parallax RT";
			GameRenderer.RTNormal = new RenderTexture(width, height, 24);
			GameRenderer.RTNormal.name = "Dynamic Normal RT";
			GameRenderer.RTDepth = new RenderTexture(width, height, 24);
			GameRenderer.RTDepth.name = "Dynamic Depth RT";
			GameRenderer.RTAO = new RenderTexture(width, height, 8);
			GameRenderer.RTAO.name = "Dynamic AO RT";
			Debug.Log(string.Concat(new object[]
			{
				"#Info# Create Albedo/Normal RenderTexture ",
				GameRenderer.RTAlbedo.width,
				"x",
				GameRenderer.RTAlbedo.height
			}));
			foreach (Visibility visibility in Visibility.visibilityList)
			{
				visibility.NotifyRTChanged();
			}
		}
	}

	// Runs the replacement-shader prepass before Unity draws the final image.
	// Likely fills the shared buffers used by lighting, LoS, and post effects.
	private void OnPreRender()
	{
		this.SetupCams();
		if (CrewSim.objInstance == null)
		{
			Debug.Log("CrewSim.objInstance is null. Aborting.");
			return;
		}
		if (CrewSim.objInstance.ActiveCam == null)
		{
			Debug.Log("CrewSim.objInstance.ACtiveCam is null. Aborting.");
			return;
		}
		this.SetWidthHeight(CrewSim.objInstance.ActiveCam.pixelWidth, CrewSim.objInstance.ActiveCam.pixelHeight);
		if (GameRenderer.RTAlbedo != null && this.AlbedoPass != null)
		{
			this.camAlbedo.targetTexture = GameRenderer.RTAlbedo;
			this.camAlbedo.SetReplacementShader(this.AlbedoPass, string.Empty);
			this.camAlbedo.Render();
			this.camAlbedo.targetTexture = null;
			this.camAlbedo.ResetReplacementShader();
		}
		if (GameRenderer.RTDepth != null && this.DepthPass != null && ((this.ShowAO && this.AOLoops > 0) || this.Style == GameRenderer.RenderStyle.Depth || this.Style == GameRenderer.RenderStyle.AO))
		{
			this.camAlbedo.targetTexture = GameRenderer.RTDepth;
			this.camAlbedo.SetReplacementShader(this.DepthPass, string.Empty);
			this.camAlbedo.Render();
			this.camAlbedo.targetTexture = null;
			this.camAlbedo.ResetReplacementShader();
			if (this.ZoomAO)
			{
				this.AOZoomCurrent = 1f / this.camAlbedo.orthographicSize;
			}
			else
			{
				this.AOZoomCurrent = 0.89f;
			}
			this.matAOPass.SetFloat("_Intensity", this.AOIntensity);
			this.matAOPass.SetFloat("_Zoom", this.AOZoomBase * this.AOZoomCurrent);
			this.matAOPass.SetFloat("_Loops", (float)this.AOLoops);
			Graphics.Blit(GameRenderer.RTDepth, GameRenderer.RTAO, this.matAOPass);
			this.matStencil.SetFloat("_IntensityMin", this.AOFade);
			this.matStencil.SetFloat("_IntensityMin", 0f);
		}
		if (GameRenderer.RTAlpha != null && this.AlphaStencil != null)
		{
			this.camAlbedo.targetTexture = GameRenderer.RTAlpha;
			this.camAlbedo.SetReplacementShader(this.AlphaStencil, string.Empty);
			this.camAlbedo.Render();
			this.camAlbedo.targetTexture = null;
			this.camAlbedo.ResetReplacementShader();
		}
		if (GameRenderer.RTParallax != null && this.ParallaxPass != null && this.ShowParallax)
		{
			this.camParallax.targetTexture = GameRenderer.RTParallax;
			this.camParallax.SetReplacementShader(this.ParallaxPass, string.Empty);
			this.camParallax.Render();
			this.camParallax.targetTexture = null;
			this.camParallax.ResetReplacementShader();
		}
		if (GameRenderer.RTNormal != null && this.NormalPass != null)
		{
			this.camNorm.targetTexture = GameRenderer.RTNormal;
			this.camNorm.SetReplacementShader(this.NormalPass, string.Empty);
			this.camNorm.Render();
			this.camNorm.targetTexture = null;
			this.camNorm.ResetReplacementShader();
		}
		foreach (Visibility visibility in Visibility.visibilityList)
		{
			visibility.NotifyPreRender();
		}
		foreach (Item item in Item.aPreRender)
		{
			item.NotifyPreRender();
		}
	}

	// Combines the intermediate render textures into the final on-screen image.
	// This is the last full-screen compositing step for the custom renderer.
	private void OnRenderImage(RenderTexture source, RenderTexture destination)

	private void OnRenderImage(RenderTexture source, RenderTexture dest)
	{
		this.SetupCams();
		RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.R8);
		if (!this.UseLighting)
		{
			source = GameRenderer.RTAlbedo;
		}
		if (this.Style == GameRenderer.RenderStyle.Default || this.Style == GameRenderer.RenderStyle.Value)
		{
			if (this.StencilPass != null)
			{
				this.camStencil.targetTexture = temporary;
				this.camStencil.SetReplacementShader(this.StencilPass, string.Empty);
				this.camStencil.Render();
				this.camStencil.targetTexture = null;
				this.camStencil.ResetReplacementShader();
			}
			if (this.ShowParallax)
			{
				Graphics.Blit(GameRenderer.RTAlpha, GameRenderer.RTParallax, this.matParallax);
				Graphics.Blit(GameRenderer.RTParallax, source, this.matFinal);
			}
			if (this.ShowAO && this.AOLoops > 0)
			{
				this.matStencil.SetFloat("_IntensityMin", this.AOFade);
				Graphics.Blit(GameRenderer.RTAO, source, this.matStencil);
				this.matStencil.SetFloat("_IntensityMin", 0f);
			}
			if (!this.HideLoS)
			{
				Graphics.Blit(temporary, source, this.matStencil);
			}
			if (this.Style == GameRenderer.RenderStyle.Value)
			{
				Graphics.Blit(source, dest, this.matPostProcess);
			}
			else
			{
				Graphics.Blit(source, dest);
			}
		}
		else if (this.Style == GameRenderer.RenderStyle.Normals)
		{
			Graphics.Blit(GameRenderer.RTNormal, dest);
		}
		else if (this.Style == GameRenderer.RenderStyle.Depth)
		{
			Graphics.Blit(GameRenderer.RTDepth, dest);
		}
		else if (this.Style == GameRenderer.RenderStyle.AO)
		{
			Graphics.Blit(GameRenderer.RTAO, dest);
		}
		else if (this.Style == GameRenderer.RenderStyle.Alpha)
		{
			Graphics.Blit(GameRenderer.RTAlpha, dest);
		}
		else if (this.Style == GameRenderer.RenderStyle.Stencil)
		{
			Graphics.Blit(temporary, dest);
		}
		RenderTexture.ReleaseTemporary(temporary);
	}

	private Camera MakeCam(Camera camMain, string strName)
	{
		Camera camera = UnityEngine.Object.Instantiate<Camera>(camMain, camMain.transform);
		camera.name = strName;
		camera.GetComponent<AudioListener>().enabled = false;
		camera.GetComponent<GameRenderer>().enabled = false;
		camera.enabled = false;
		camera.CopyFrom(camMain);
		camera.cullingMask = (1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Ghosts") | 1 << LayerMask.NameToLayer("Tile Helpers"));
		IEnumerator enumerator = camera.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		return camera;
	}

	public RenderTexture GetShipAlpha()
	{
		return GameRenderer.RTAlpha;
	}

	public void SetAmbient(Color col)
	{
		GameRenderer.clrAmbient = col;
	}

	public void SetZoom(float fOrthoSize)
	{
		this.SetupCams();
		Camera camera = this.camAlbedo;
		this.camParallax.orthographicSize = fOrthoSize;
		this.camStencil.orthographicSize = fOrthoSize;
		this.camNorm.orthographicSize = fOrthoSize;
		camera.orthographicSize = fOrthoSize;
	}

	private void SetupCams()
	{
		if (this.camNorm != null)
		{
			return;
		}
		Camera component = base.GetComponent<Camera>();
		this.camAlbedo = this.MakeCam(component, "AlbedoCam");
		this.camNorm = this.MakeCam(component, "NormCam");
		this.camStencil = this.MakeCam(component, "StencilCam");
		this.camStencil.cullingMask = (1 << LayerMask.NameToLayer("LoS") | 1 << LayerMask.NameToLayer("Tile Helpers"));
		this.camParallax = this.MakeCam(component, "ParallaxCam");
		this.camParallax.cullingMask = 1 << LayerMask.NameToLayer("Parallax");
		this.matStencil = new Material(this.StencilCombinePass);
		this.matStencil.hideFlags = HideFlags.HideAndDontSave;
		this.matParallax = new Material(this.ParallaxCombinePass);
		this.matParallax.hideFlags = HideFlags.HideAndDontSave;
		this.matFinal = new Material(this.FinalCombinePass);
		this.matFinal.hideFlags = HideFlags.HideAndDontSave;
		this.matPostProcess = new Material(this.PostProcessPass);
		this.matPostProcess.hideFlags = HideFlags.HideAndDontSave;
		this.matAOPass = new Material(this.AOPass);
		this.matAOPass.hideFlags = HideFlags.HideAndDontSave;
	}

	public Camera StencilCam
	{
		get
		{
			this.SetupCams();
			return this.camStencil;
		}
	}

	public Camera GetParallaxCamera()
	{
		return this.camParallax;
	}

	public void SwapMode()
	{
		this.Style++;
		if (this.Style > GameRenderer.RenderStyle.Stencil)
		{
			this.Style = GameRenderer.RenderStyle.Default;
		}
	}

	public void SetupForShipPreview(Vector2 pos, RenderTexture renderTex)
	{
		base.transform.position = new Vector3(pos.x, pos.y, -20f);
		Camera component = base.GetComponent<Camera>();
		component.targetTexture = renderTex;
		component.depth = -2f;
		component.orthographicSize = 10f;
		this.HideLoS = true;
		this.SetZoom(component.orthographicSize);
		this.ToggleCrt(true);
	}

	public void ToggleCrt(bool show)
	{
		Crt component = base.GetComponent<Crt>();
		if (component == null)
		{
			return;
		}
		component.enabled = show;
	}

	private Camera camAlbedo;

	private Camera camNorm;

	private Camera camStencil;

	private Camera camParallax;

	public Shader AlbedoPass;

	public Shader NormalPass;

	public Shader AlphaStencil;

	public Shader StencilPass;

	public Shader ParallaxPass;

	public Shader StencilCombinePass;

	public Shader ParallaxCombinePass;

	public Shader FinalCombinePass;

	public Shader PostProcessPass;

	public Shader DepthPass;

	public Shader AOPass;

	public bool UseLighting = true;

	public bool HideLoS;

	public bool ShowParallax;

	public bool ShowAO = true;

	public bool ZoomAO = true;

	public int AOLoops = 8;

	public float AOIntensity = 0.3f;

	public float AOZoomBase = 22.5f;

	public float AOZoomCurrent = 1f;

	public float AOFade = 0.4f;

	public GameRenderer.RenderStyle Style;

	public static int Mode = 0;

	public static int ModeModulus = 2;

	private Material matStencil;

	private Material matParallax;

	private Material matFinal;

	private Material matPostProcess;

	private Material matAOPass;

	public static RenderTexture RTAlbedo;

	public static RenderTexture RTNormal;

	public static RenderTexture RTAlpha;

	public static RenderTexture RTParallax;

	public static RenderTexture RTDepth;

	public static RenderTexture RTAO;

	public static Color clrAmbient = Color.black;

	public enum RenderStyle
	{
		Default,
		Value,
		Normals,
		Depth,
		AO,
		Stencil,
		Alpha
	}
}
