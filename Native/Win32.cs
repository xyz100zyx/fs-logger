using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace file_logger.Native;

using SESSION_HANDLE = uint;
using PROCESS_HANDLE = uint;

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

    private enum SESSION_WIN_32_ERROR_CODES
    {
        ERROR_SUCCESS = 0,
        ERROR_INVALID_PARAMETER = 87,
        ERROR_ACCESS_DENIED = 5
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
    private static extern int RmStartSession(out SESSION_HANDLE pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        SESSION_HANDLE pSessionHandle,
        uint nFiles,
        string[] rgsFilenames,
        uint nApplications,
        RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames
    );

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        SESSION_HANDLE dwSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
        ref uint lpdwRebootReasons);

    public static IReadOnlyList<int> GetLockingProcessIds(string objectPath)
    {
        if (!OperatingSystem.IsWindows())
            return Array.Empty<int>();

        SESSION_HANDLE sessionHandle = 0;

        string sessionKey = Guid.NewGuid().ToString("N");

        var result = new List<int>();


        /* TODO: 
            2. RmGetList увидеть все дескрипторы процессов которые сейчас блочат файл/папку;
            3. Добавить в result все id процессов, дескрипторы которых получилось достать;
         */
        try
        {
            int errorResult = RmStartSession(out sessionHandle, 0, sessionKey);

            switch (errorResult)
            {
                case (int)SESSION_WIN_32_ERROR_CODES.ERROR_ACCESS_DENIED:
                    throw new Exception("Access denied error during open the session in RmStartSession");
                case (int)SESSION_WIN_32_ERROR_CODES.ERROR_INVALID_PARAMETER:
                    throw new Exception("Invalid arguments error during open the session in RmStartSession");
                case (int)SESSION_WIN_32_ERROR_CODES.ERROR_SUCCESS:
                    DevLogger.Log("RmStartSession execute correctly. Runs the next step as objects registration");
                    break;
                default:
                    throw new Exception("Undefined erorr result code after RmStartSession execution");
                    
            }

            string[] resources = { objectPath };
        }
        
        catch (Exception ex)
        {
            DevLogger.Log($"RmStartSession execute correctly. Runs the next step as objects registration: {ex}");
            Environment.Exit(0);
        }
        return result;
    }
}