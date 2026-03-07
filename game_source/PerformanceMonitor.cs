using System;
using System.Text;
using TMPro;
using UnityEngine;

public class PerformanceMonitor : MonoBehaviour
{
	private void Awake()
	{
		this._instance = this;
	}

	public void Calculate()
	{
	}

	private void Update()
	{
		if (this.editorOrDevBuild)
		{
			PerformanceMonitor.sampleCumulativeDelta += Time.unscaledDeltaTime;
			this.updateTime = Time.unscaledDeltaTime;
			if (Time.unscaledDeltaTime < PerformanceMonitor.sampleMinDelta)
			{
				PerformanceMonitor.sampleMinDelta = Time.unscaledDeltaTime;
			}
			if (Time.unscaledDeltaTime > PerformanceMonitor.sampleMaxDelta)
			{
				PerformanceMonitor.sampleMaxDelta = Time.unscaledDeltaTime;
			}
			this.fps = 1f / Time.unscaledDeltaTime;
			this.frames++;
		}
		if (this.frames == PerformanceMonitor.sampleSize)
		{
			this.frames = 0;
			PerformanceMonitor.recentAverageDelta = PerformanceMonitor.sampleCumulativeDelta / (float)PerformanceMonitor.sampleSize;
			PerformanceMonitor.sampleCumulativeDelta = 0f;
			PerformanceMonitor.recentMinDelta = PerformanceMonitor.sampleMinDelta;
			PerformanceMonitor.recentMaxDelta = PerformanceMonitor.sampleMaxDelta;
			PerformanceMonitor.sampleMinDelta = float.MaxValue;
			PerformanceMonitor.sampleMaxDelta = float.MinValue;
			this.fpsAverage = 1f / PerformanceMonitor.recentAverageDelta;
			this.minFrames = 1f / PerformanceMonitor.recentMaxDelta;
			this.maxFrames = 1f / PerformanceMonitor.recentMinDelta;
		}
		if (this.editorOrDevBuild && PerformanceMonitor.active)
		{
			if (!this.output.enabled)
			{
				this.output.enabled = true;
			}
			this._output = new StringBuilder(60);
			bool flag = this.minFrames < this.fpsAverage / 3f;
			if (flag)
			{
				this._output.AppendFormat("AVG FPS {0:00.0}     <color=red>MIN {1:00.0}</color>     {2:00.0}ms", this.fpsAverage, this.minFrames, this.updateTime * 1000f);
			}
			else
			{
				this._output.AppendFormat("AVG FPS {0:00.0}     MIN {1:00.0}    {2:00.0}ms", this.fpsAverage, this.minFrames, this.updateTime * 1000f);
			}
			this.output.SetText(this._output);
		}
		else if (this.output.enabled)
		{
			this.output.enabled = false;
		}
	}

	public TextMeshProUGUI output;

	public PerformanceMonitor _instance;

	public static bool active;

	private StringBuilder _output = new StringBuilder(50);

	public float minFrames;

	public float fps;

	public float maxFrames;

	public float fpsAverage;

	[NonSerialized]
	public static float realTimeRecentMin;

	[NonSerialized]
	public static float realTimeRecentMax;

	[NonSerialized]
	public static float sampleMinDelta;

	[NonSerialized]
	public static float recentMinDelta;

	[NonSerialized]
	public static float sampleMaxDelta;

	[NonSerialized]
	public static float recentMaxDelta;

	[NonSerialized]
	public static float sampleCumulativeDelta;

	[NonSerialized]
	public static float recentAverageDelta;

	[NonSerialized]
	public static int sampleSize = 180;

	[NonSerialized]
	public int frames;

	public float updateTime;

	public bool editorOrDevBuild;
}
