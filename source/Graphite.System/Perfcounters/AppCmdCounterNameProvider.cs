using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Graphite.System.Perfcounters
{
    public class AppCmdCounterNameProvider : ICounterNameProvider
    {
        public string GetCounterName(string poolName)
        {
            string result;

            this.Execute("list WP", out result, 1000);

            var match = Regex.Match(
                result,
                "WP \"(?<id>[0-9]+)\" \\(applicationPool:" + Regex.Escape(poolName) + "\\)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            int processId;

            if (match.Success && match.Groups["id"].Success && int.TryParse(match.Groups["id"].Value, out processId))
            {
                return this.ProcessNameById("w3wp", processId);
            }

            return null;
        }

        public void ReportInvalid(string appPoolName, string instanceName)
        {
        }

        private string ProcessNameById(string prefix, int processId)
        {
            var localCategory = new PerformanceCounterCategory("Process");

            string[] instances = localCategory.GetInstanceNames()
                .Where(p => p.StartsWith(prefix))
                .ToArray();

            foreach (string instance in instances)
            {
                using (var localCounter = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    long val = localCounter.RawValue;

                    if (val == processId)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }

        private bool Execute(string arguments, out string result, int maxMilliseconds = 30000)
        {
            string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(systemPath, "inetsrv\\appcmd.exe"),
                Arguments = arguments,

                RedirectStandardOutput = true,

                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var standardOut = new StringBuilder();

            Process p = Process.Start(startInfo);

            p.OutputDataReceived += (s, d) => standardOut.AppendLine(d.Data);
            p.BeginOutputReadLine();

            bool success = p.WaitForExit(maxMilliseconds);
            p.CancelOutputRead();

            if (!success)
            {
                try
                {
                    p.Kill();
                }
                catch (Win32Exception)
                {
                    // unable to kill the process
                }
                catch (InvalidOperationException)
                {
                    // process already stopped
                }
            }

            result = standardOut.ToString();

            return success;
        }

    }
}