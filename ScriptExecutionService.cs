using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BatchWorker
{
    public sealed class ScriptExecutionService
    {
        private readonly ILogger<ScriptExecutionService> _logger;
        public ScriptExecutionService(ILogger<ScriptExecutionService> logger)
        {
            _logger = logger;
        }


        public void ExecuteScript(String filePath, String args,User user,bool is_user_driven=false, bool is_ps=false)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process? process;

            string fileName = "cmd.exe";
            string arguments = $"{args} {filePath}";

            if (is_ps)
            {
                fileName = "powershell.exe";
                arguments = $"{args} -File {filePath}";
            }


            ProcessInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (is_user_driven)
            {
                ProcessInfo.UserName = user.username;
                ProcessInfo.PasswordInClearText = user.password;
            }

            process = Process.Start(ProcessInfo);
            string output;
            string error;

            if (process != null)
            {
                process.WaitForExit();
                // *** Read the streams ***
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();

                ExitCode = process.ExitCode;
                process.Close();
            }
            else
            {
                throw new Exception($"Process was not created for: File-{filePath}, args-{args}");
            }
            if (!String.IsNullOrEmpty(error))
            {
                _logger.LogError($"error >>>{error}");
            }
            if (!String.IsNullOrEmpty(output))
            {
                _logger.LogInformation($"output>>>{output}");
            }
            _logger.LogInformation($"ExitCode: {ExitCode}");

        }

    }
}
