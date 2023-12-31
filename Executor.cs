using Microsoft.Extensions.Logging;
using System.Globalization;
using NCrontab;

namespace BatchWorker
{
    public sealed class Executor : BackgroundService
    {
        private readonly ILogger<Executor> _logger;
        private readonly IConfiguration _config;
        private readonly ScriptExecutionService _scriptExecutionService;

        public Executor(ILogger<Executor> logger, IConfiguration config, ScriptExecutionService scriptExecutionService) =>
            (_logger, _config, _scriptExecutionService) = (logger, config, scriptExecutionService);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                String? format = _config.GetValue<String>("DateTimeFormat");
                format = CheckNullable<String>(format, "DateTimeFormat");

                String? startDateTime = _config.GetValue<String>("StartDateTime");
                startDateTime = CheckNullable<String>(startDateTime, "StartDateTime");

                String? endDateTime = _config.GetValue<String>("EndDateTime");
                endDateTime = CheckNullable<String>(endDateTime, "EndDateTime");

                String? cronExpression = _config.GetValue<String>("CRONExpression");
                cronExpression = CheckNullable<String>(cronExpression, "CRONExpression");

                CultureInfo provider = CultureInfo.InvariantCulture;

                DateTime start = DateTime.ParseExact(startDateTime, format, provider);
                DateTime end = DateTime.ParseExact(endDateTime, format, provider);

                CrontabSchedule schedule = CrontabSchedule.Parse(cronExpression);

                List<String>? BATPaths = _config.GetSection("BATFilePaths").Get<List<String>>();
                BATPaths = CheckNullable<List<String>>(BATPaths, "BATFilePaths");
                String? BATArgs = _config.GetValue<String>("BATArgs");
                BATArgs = CheckNullable<String>(BATArgs, "BATArgs");

                List<String>? PSPaths = _config.GetSection("PSFilePaths").Get<List<String>>();
                PSPaths = CheckNullable<List<String>>(PSPaths, "PSFilePaths");
                String? PSArgs = _config.GetValue<String>("PSArgs");
                PSArgs = CheckNullable<String>(PSArgs, "PSArgs");

                String? username = _config.GetValue<String>("Username");
                username = CheckNullable<String>(username, "Username");

                String? password = _config.GetValue<String>("Password");
                password = CheckNullable<String>(password, "Password");

                bool is_user_driven = _config.GetValue<bool>("IS_USER_DRIVEN");
                User user=new User();
                if (is_user_driven)
                {
                    user = new User(username,password);
                }

                if (start < DateTime.Now)
                {
                    start = DateTime.Now;
                }
                if (end < DateTime.Now)
                {
                    throw new Exception("End Date is in Past");
                }

                while (!stoppingToken.IsCancellationRequested)
                {

                    if (end - start < TimeSpan.Zero)
                    {
                        Environment.Exit(0);
                    }
                    DateTime NextRun = schedule.GetNextOccurrence(start, end);
                    TimeSpan WaitBeforeNextRun = NextRun - DateTime.Now;
                    _logger.LogInformation($"Next Run at: {NextRun}");
                    _logger.LogInformation($"Wait Before Next Run: {WaitBeforeNextRun}");
                    await Task.Delay(WaitBeforeNextRun < TimeSpan.Zero ? TimeSpan.Zero : WaitBeforeNextRun, stoppingToken);
                    await ExecuteBatFileAsync(BATPaths,BATArgs,is_user_driven, user);
                    await ExecuteScriptFileAsync(PSPaths,PSArgs, is_user_driven, user);
                    start = DateTime.Now;


                }

            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
        }


        private T CheckNullable<T>(T? varibale, String varname)
        {
            if (varibale == null)
            {
                throw new ArgumentNullException(varname);
            }
            return varibale;
        }


        private Task ExecuteBatFileAsync(List<String> paths,string args,bool is_user_driven,User user)
        {
            foreach (string path in paths)
            {
                _scriptExecutionService.ExecuteScript(path, args,user,is_user_driven);
            }
            _logger.LogInformation("Completed Executing Batch File");
            return Task.FromResult("Completed Executing Batch File");
        }

        private Task ExecuteScriptFileAsync(List<String> paths, string args, bool is_user_driven, User user)
        {
            foreach (string path in paths)
            {
                _scriptExecutionService.ExecuteScript(path, args,user,is_user_driven, true);
            }
            _logger.LogInformation("Completed Executing Powershell Script File");
            return Task.FromResult("Completed Executing Powershell Script File");
        }
    }

   public readonly record struct User(string username, string password);
}
