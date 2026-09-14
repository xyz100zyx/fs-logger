using System.Runtime.InteropServices;

namespace file_logger.Native;

using DevLogger = file_logger.DevConsoleLogger.DevConsoleLogger;

/// <summary>
/// Windows SDK -> RestartManager.h
/// </summary>
static class Win32
{
    private const int CCH_RM_SESSION_KEY = 32;
    private const int CCH_RM_MAX_APP_NAME = 255;
    private const int CCH_RM_MAX_SVC_NAME = 63;

    private enum RM_APP_TYPE
    {
        RmUnknownApp = 0,
        RmMainWindow = 1,
        RmOtherWindow = 2,
        RmService = 3,
        RmExplorer = 4,
        RmConsole = 5,
        RmCritical = 1000
    }

    private enum START_SESSION_WIN_32_ERROR_CODES
    {
        ERROR_SUCCESS = 0,
        ERROR_INVALID_PARAMETER = 87,
        ERROR_ACCESS_DENIED = 5
    }

    private enum REG_SESSION_RESOURCES_WIN_32_ERROR_CODES
    {
        ERROR_SUCCESS = 0,
        ERROR_INVALID_HANDLE = 6, // Передан недействительный дескриптор сессии
        ERROR_OUTOFMEMORY = 14, // Недостаточно памяти
        ERROR_WRITE_FAULT = 29, // Не удалось прочитать/записать в реестр
        ERROR_SEM_TIMEOUT = 121, // Не удалось получить мьютекс реестра; рекомендуется перезагрузка
        ERROR_BAD_ARGUMENTS = 160 // Один или несколько аргументов неверны
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public string strAppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public string strServiceShortName;

        public RM_APP_TYPE ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint pSessionHandle,
        uint nFiles,
        string[] rgsFilenames,
        uint nApplications,
        RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames
    );

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        uint dwSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
        ref uint lpdwRebootReasons);

    public static IReadOnlyList<int> GetLockingProcessIds(string objectPath)
    {
        if (!OperatingSystem.IsWindows())
            return Array.Empty<int>();

        uint sessionHandle = 0;

        string sessionKey = Guid.NewGuid().ToString("N");

        var result = new List<int>();

        try
        {
            int errorResult = RmStartSession(out sessionHandle, 0, sessionKey);

            switch (errorResult)
            {
                case (int)START_SESSION_WIN_32_ERROR_CODES.ERROR_ACCESS_DENIED:
                    throw new Exception("Access denied error during open the session in RmStartSession");
                case (int)START_SESSION_WIN_32_ERROR_CODES.ERROR_INVALID_PARAMETER:
                    throw new Exception("Invalid arguments error during open the session in RmStartSession");
                case (int)START_SESSION_WIN_32_ERROR_CODES.ERROR_SUCCESS:
                    DevLogger.Log("RmStartSession execute correctly. Runs the next step as objects registration");
                    break;
                default:
                    throw new Exception("Undefined erorr result code after RmStartSession execution");

            }

            string[] resources = { objectPath };

            errorResult = RmRegisterResources(sessionHandle, (uint)resources.Length, resources, 0, null, 0, null);

            switch (errorResult)
            {
                case (int)REG_SESSION_RESOURCES_WIN_32_ERROR_CODES.ERROR_BAD_ARGUMENTS:
                    throw new Exception("Bad args passed in fn RmRegisterResources");
                case (int)REG_SESSION_RESOURCES_WIN_32_ERROR_CODES.ERROR_INVALID_HANDLE:
                    throw new Exception("Invalid session descriptor for RmRegisterResources");
                case (int)REG_SESSION_RESOURCES_WIN_32_ERROR_CODES.ERROR_OUTOFMEMORY:
                    DevLogger.Log("Memory out of bounds in RmRegisterResources");
                    break;
                case (int)REG_SESSION_RESOURCES_WIN_32_ERROR_CODES.ERROR_SEM_TIMEOUT:
                    DevLogger.Log("Timeout for get mutex in RmRegisterResources");
                    break;
                case (int)REG_SESSION_RESOURCES_WIN_32_ERROR_CODES.ERROR_WRITE_FAULT:
                    DevLogger.Log("Cannot write in registry in RmRegisterResources");
                    break;
                case (int)REG_SESSION_RESOURCES_WIN_32_ERROR_CODES.ERROR_SUCCESS:
                    DevLogger.Log("RmRegisterResources execute correctly. Runs the next step as get processes list");
                    break;
                default:
                    throw new Exception("Undefined erorr result code after RmStartSession execution");
            }

            uint pnProcInfoNeeded = 0;
            uint pnProcInfo = 0;
            uint rebootReasons = 0;

            errorResult = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref rebootReasons);

            switch (errorResult)
            {
                case 234:
                    if (pnProcInfoNeeded > 0)
                    {
                        DevLogger.Log("RmGetList executed successfully. Go to next step get processes metadata");
                    }
                    break;
                default:
                    DevLogger.Log($"errRes={errorResult}");
                    break;
                    // throw new Exception("Undefined erorr result code during RmGetList execution");
            }

            var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
            pnProcInfo = pnProcInfoNeeded;

            errorResult = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref rebootReasons);

            switch (errorResult)
            {
                // TODO: make enum for getList error codes
                case 0:
                    for (int i = 0; i < pnProcInfo; i++)
                        result.Add(processInfo[i].Process.dwProcessId);
                    break;
                default:
                    throw new Exception("Undefined erorr result code during RmGetList for make processes list");
            }
        }
        catch (Exception ex)
        {
            DevLogger.Log($"RmStartSession execute correctly. Runs the next step as objects registration: {ex}");


            /* TODO: replace close process with return empty result list */
            Environment.Exit(1);
        }
        finally
        {
            if (sessionHandle != 0)
            {
                if (RmEndSession(sessionHandle) != 0)
                {
                    Environment.Exit(1);
                }
            }
        }
        return result;
    }
}