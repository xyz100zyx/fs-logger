using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using file_logger.Native;

using DevLogger = file_logger.DevConsoleLogger.DevConsoleLogger;

namespace file_logger.FileMetaDataCollector;


using PROCESS_DESC = (int? pid, string? pName);

public sealed class FileMetaDataCollector
{
    private readonly LoggerOptions _loggerOptions;

    public FileMetaDataCollector(LoggerOptions opts)
    {
        _loggerOptions = opts;
    }

    public long? GetResourceObjectSize(string resourcrObjectPath, bool isDirectory)
    {

        if (isDirectory) return null;


        try
        {
            var fileInfo = new FileInfo(resourcrObjectPath);

            if (fileInfo is not FileInfo) return null;

            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            DevLogger.LogError($"Error during execute getResourceObjectSize. ErrMsg={ex.Message}");
            return null;
        }

    }

    public string? ComputeSHA256(string resourceObjectPath, bool isDir)
    {
        if (isDir || _loggerOptions.ComputeHash) return null;


        try
        {
            var fileInfo = new FileInfo(resourceObjectPath);

            if (fileInfo is not FileInfo)
            {
                throw new Exception("fileInfo is not FileInfo");
            }

            bool isUnavailableToComputeHash_SHA256 = fileInfo.Length > _loggerOptions.MaxHashFileSizeBytes; 
        
            if(isUnavailableToComputeHash_SHA256) return null;

            using var fStream = File.OpenRead(resourceObjectPath);
            using var hasher = SHA256.Create();
            byte[] hash = hasher.ComputeHash(fStream);
            return Convert.ToHexString(hash).ToLowerInvariant();

        }
        catch (Exception ex)
        {
            DevLogger.LogError($"Error during execute getResourceObjectSize. ErrMsg={ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получение информации о процессе (name), который сейчас лочит ресурс (объект)
    /// </summary>
    /// <param name="resourceObjectPath">Пусть к объекту</param>
    /// <returns>(int? processId, string? processName)</returns>
    public PROCESS_DESC TryGetLockingProcessInfo(string resourceObjectPath)
    {


        PROCESS_DESC fallback = (null, null);

        try
        {
            var lockingProcessids = Win32.GetLockingProcessIds(resourceObjectPath);
        
           if(lockingProcessids.Count == 0) return fallback;



            int procId = lockingProcessids[0];
            string? procName = null;

            try
            {
                var proc = Process.GetProcessById(procId);
                procName = proc.ProcessName;
            }
            catch
            {
                DevLogger.Log($"We cannot get process name for pid {procId}");
            }

            return (procId, procName);    
        }
        catch (Exception ex) 
        {
            DevLogger.LogError($"Error in method TryGetLockingProcessInfo. ErrMsg={ex.Message}");
            return fallback;
        }
    }
}