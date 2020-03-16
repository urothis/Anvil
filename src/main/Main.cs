using System;
using System.IO;
using System.Reflection;
using NWM.Core;

namespace NWM
{
  internal static class Main
  {
    private static ServiceManager serviceManager;
    private static ScriptHandlerDispatcher scriptHandlerDispatcher;
    private static TimeService timeService;

    private static bool initialized;

    public static void OnStart()
    {
      serviceManager = new ServiceManager();
      AppendAssemblyToPath();
    }

    public static void OnMainLoop(ulong frame)
    {
      timeService.Update();
    }

    public static int OnRunScript(string script, uint oidSelf)
    {
      if (!initialized)
      {
        Init();
      }

      return scriptHandlerDispatcher.ExecuteScript(script, oidSelf);
    }

    // Needed to allow native libs to be loaded.
    private static void AppendAssemblyToPath()
    {
      string envPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
      string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

      Environment.SetEnvironmentVariable("PATH", $"{envPath}; {assemblyDir}");
    }

    private static void Init()
    {
      initialized = true;
      serviceManager.Verify();
      scriptHandlerDispatcher = serviceManager.GetService<ScriptHandlerDispatcher>();
      timeService = serviceManager.GetService<TimeService>();
      scriptHandlerDispatcher.Init(serviceManager.GetRegisteredServices());
    }
  }
}