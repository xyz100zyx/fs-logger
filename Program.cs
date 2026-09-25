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


var reader = new JsonFileReader(@"D:\projects\c_sharp_edu\file-logger\logs\2026-09-15.jsonl");

long count = 0;

reader.Read((in LogRecord r) =>
{
    count++;
    Console.WriteLine(r.ToString());
});

var host = builder.Build();
await host.RunAsync();