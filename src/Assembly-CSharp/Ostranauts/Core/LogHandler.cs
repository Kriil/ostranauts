using System;
using UnityEngine;

namespace Ostranauts.Core
{
	public class LogHandler
	{
		public LogHandler()
		{
			this.Log = string.Empty;
			AppDomain.CurrentDomain.UnhandledException += this.LogException;
			Application.logMessageReceived += this.LogNonStandardLogs;
		}

		public string Log { get; private set; }

		public void LoadLog(string loadedLog)
		{
			if (string.IsNullOrEmpty(loadedLog))
			{
				return;
			}
			this.Log = loadedLog;
		}

		private void LogNonStandardLogs(string logString, string stackTrace, LogType type)
		{
			if (type == LogType.Log)
			{
				return;
			}
			stackTrace = ((!string.IsNullOrEmpty(stackTrace) && type != LogType.Warning) ? (" StackTrace: " + stackTrace) : string.Empty);
			this.LogMessage(string.Concat(new object[]
			{
				type,
				" ",
				logString,
				stackTrace
			}));
		}

		public void LogMessage(string logString)
		{
			if (this.Log == null || string.IsNullOrEmpty(logString) || this.IsDuplicate(logString))
			{
				return;
			}
			string log = this.Log;
			this.Log = string.Concat(new string[]
			{
				log,
				this._lineStart,
				DateTime.Now.ToString("G"),
				" ",
				logString,
				this._lineEnd
			});
			this.TrimLog();
		}

		private bool IsDuplicate(string logString)
		{
			if (string.IsNullOrEmpty(this.Log))
			{
				return false;
			}
			string[] array = this.Log.Split(new string[]
			{
				this._lineStart
			}, StringSplitOptions.RemoveEmptyEntries);
			return array.Length - 1 > 0 && array[array.Length - 1].Contains(logString);
		}

		private void LogException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e != null)
			{
				this.LogMessage(e.ToString());
			}
		}

		public void Unsubscribe()
		{
			AppDomain.CurrentDomain.UnhandledException -= this.LogException;
			Application.logMessageReceived -= this.LogNonStandardLogs;
		}

		private void TrimLog()
		{
			if (string.IsNullOrEmpty(this.Log))
			{
				return;
			}
			string[] array = this.Log.Split(new string[]
			{
				this._lineStart
			}, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length < this.MaxLogSize)
			{
				return;
			}
			int num = (int)((float)this.MaxLogSize * 0.8f);
			string value = array[array.Length - num];
			int num2 = this.Log.IndexOf(value, StringComparison.Ordinal);
			this.Log = this.Log.Substring(num2, this.Log.Length - num2);
		}

		private readonly string _lineEnd = "\n ";

		private readonly string _lineStart = "* ";

		private readonly int MaxLogSize = 200;
	}
}
