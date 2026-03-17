using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ostranauts.Blueprints;

internal static class BlueprintDialog
{
	internal static string SelectBlueprintFile()
	{
		try
		{
			string script = string.Join(
				Environment.NewLine,
				"Add-Type -AssemblyName System.Windows.Forms",
				"$dialog = New-Object System.Windows.Forms.OpenFileDialog",
				"$dialog.InitialDirectory = '" + EscapePowerShellString(GetInitialDirectory()) + "'",
				"$dialog.Filter = 'Blueprint JSON (*.json)|*.json'",
				"$dialog.Title = 'Select Blueprint'",
				"$dialog.Multiselect = $false",
				"$dialog.CheckFileExists = $true",
				"$dialog.CheckPathExists = $true",
				"$dialog.RestoreDirectory = $false",
				"if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {",
				"  [Console]::OutputEncoding = [System.Text.Encoding]::UTF8",
				"  Write-Output $dialog.FileName",
				"}"
			);

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = "-NoProfile -STA -EncodedCommand " + EncodePowerShellCommand(script),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using (Process process = Process.Start(startInfo))
			{
				if (process == null)
				{
					Plugin.LogWarning("Blueprint file selection failed: PowerShell process could not be started.");
					return null;
				}

				string output = process.StandardOutput.ReadToEnd();
				string error = process.StandardError.ReadToEnd();
				process.WaitForExit();

				if (error != null && error.Trim().Length > 0)
				{
					Plugin.LogWarning("Blueprint file selection stderr: " + error.Trim());
				}

				if (process.ExitCode != 0)
				{
					Plugin.LogWarning("Blueprint file selection failed: PowerShell exited with code " + process.ExitCode + ".");
					return null;
				}

				string selectedPath = output == null ? null : output.Trim();
				return selectedPath != string.Empty ? selectedPath : null;
			}
		}
		catch (Exception ex)
		{
			Plugin.LogException("SelectBlueprintFile", ex);
			return null;
		}
	}

	private static string GetInitialDirectory()
	{
		string directory = Plugin.BlueprintDirectory?.Value;
		if (directory == null || directory.Trim().Length == 0)
		{
			directory = Path.Combine(Environment.CurrentDirectory, "Ostranauts_Data");
			directory = Path.Combine(directory, "Mods");
			directory = Path.Combine(directory, Plugin.PluginName);
			directory = Path.Combine(directory, "saved_blueprints");
		}

		try
		{
			directory = Path.GetFullPath(directory);
		}
		catch
		{
		}

		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		return directory;
	}

	private static string EscapePowerShellString(string value)
	{
		return (value ?? string.Empty).Replace("'", "''");
	}

	private static string EncodePowerShellCommand(string script)
	{
		byte[] bytes = Encoding.Unicode.GetBytes(script ?? string.Empty);
		return Convert.ToBase64String(bytes);
	}
}
