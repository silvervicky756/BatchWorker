using BatchWorker;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(option =>
{
    option.ServiceName = "DotNet Script Executor Service";
}
);

LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddSingleton<ScriptExecutionService>();
builder.Services.AddHostedService<Executor>();

var host = builder.Build();
host.Run();
