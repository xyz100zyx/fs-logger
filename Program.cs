using file_logger.Configuration;
using file_logger.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using file_logger.Core;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("FSLOGGER_")
    .AddCommandLine(args);

builder.Services.Configure<LoggerOptions>(
    builder.Configuration.GetSection(LoggerOptions.SectionName));

builder.Services.AddHostedService<FsWatcherWorker>();

var logsReadingSessionManager = new LogsReaderSessionManager(@"D:\projects\c_sharp_edu\file-logger\logs\2026-09-26.jsonl");
logsReadingSessionManager.RunSession();

var host = builder.Build();
await host.RunAsync();