using file_logger.Configuration;
using file_logger.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("FSLOGGER_")
    .AddCommandLine(args);

builder.Services.Configure<LoggerOptions>(
    builder.Configuration.GetSection(LoggerOptions.SectionName));

builder.Services.AddHostedService<FsWatcherWorker>();

var host = builder.Build();
await host.RunAsync();