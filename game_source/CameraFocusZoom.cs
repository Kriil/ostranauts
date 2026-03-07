using System;
using System.Collections;
using UnityEngine;

public class CameraFocusZoom : MonoBehaviour
{
	private void Start()
	{
		this.cam = base.GetComponent<Camera>();
	}

	public void QuickZoomIn()
	{
		if (!this.bZooming)
		{
			base.StartCoroutine("QuickZoom", 3f);
		}
	}

	public void QuickZoomOut()
	{
		if (!this.bZooming)
		{
			base.StartCoroutine("QuickBlahBlah", 11f);
		}
	}

	public void FocusOnPlayer()
	{
		this.bZooming = true;
		base.StopAllCoroutines();
		base.StartCoroutine("MoveToPlayerPosition", 1.5f);
		base.StartCoroutine("Zoom", 1.5f);
	}

	public void Unfocus()
	{
		base.StartCoroutine("UnZoom", 1.5f);
	}

	public IEnumerator QuickBlahBlah(float destination)
	{
		this.bZooming = true;
		Camera cam = CrewSim.objInstance.camMain;
		float nextLerp = (destination - cam.orthographicSize) * 0.12f;
		while (nextLerp > 0.0001f)
		{
			cam.orthographicSize += nextLerp;
			nextLerp = (destination - cam.orthographicSize) * 0.12f;
			yield return null;
		}
		cam.orthographicSize = destination;
		this.bZooming = false;
		yield break;
	}

	public IEnumerator QuickZoom(float destination)
	{
		this.bZooming = true;
		Camera cam = CrewSim.objInstance.camMain;
		float nextLerp = Mathf.Abs(destination - cam.orthographicSize) * 0.12f;
		while (nextLerp > 0.0001f)
		{
			cam.orthographicSize -= Mathf.Abs(nextLerp);
			nextLerp = Mathf.Abs(destination - cam.orthographicSize) * 0.12f;
			yield return null;
		}
		cam.orthographicSize = destination;
		this.bZooming = false;
		yield break;
	}

	private IEnumerator Zoom(float duration)
	{
		float timePassed = 0f;
		this.fCamOrthoSizeOld = this.cam.orthographicSize;
		while (timePassed < duration)
		{
			timePassed += Time.deltaTime;
			this.cam.orthographicSize = Mathf.Lerp(this.cam.orthographicSize, 5.625f, 0.1f);
			IEnumerator enumerator = base.transform.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					if (transform.GetComponent<Camera>() != null)
					{
						transform.GetComponent<Camera>().orthographicSize = Mathf.Lerp(this.cam.orthographicSize, 5.625f, 0.1f);
					}
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
			yield return null;
		}
		this.cam.orthographicSize = 5.625f;
		yield break;
	}

	private IEnumerator UnZoom(float duration)
	{
		this.bZooming = true;
		if (this.fCamOrthoSizeOld < 0f)
		{
			yield break;
		}
		float timePassed = 0f;
		while (timePassed < duration)
		{
			timePassed += Time.deltaTime;
			this.cam.orthographicSize = Mathf.Lerp(this.cam.orthographicSize, this.fCamOrthoSizeOld, 0.1f);
			IEnumerator enumerator = base.transform.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					if (transform.GetComponent<Camera>() != null)
					{
						transform.GetComponent<Camera>().orthographicSize = Mathf.Lerp(this.cam.orthographicSize, this.fCamOrthoSizeOld, 0.1f);
					}
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
			yield return null;
		}
		this.cam.orthographicSize = this.fCamOrthoSizeOld;
		this.bZooming = false;
		yield break;
	}

	public IEnumerator MoveToPlayerPosition(float duration)
	{
		float timePassed = 0f;
		while (timePassed < duration)
		{
			timePassed += Time.deltaTime;
			CrewSim.objInstance.camFollow = true;
			yield return null;
		}
		yield break;
	}

	public Camera cam;

	private float fCamOrthoSizeOld = -1f;

	private bool bZooming;
}
