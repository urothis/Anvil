using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.CodeAnalysis;
using NLog;
using NUnit.Framework.Internal.Commands;
using NUnitLite;

namespace Anvil.TestRunner
{
  [ServiceBinding(typeof(TestRunnerService))]
  [ServiceBindingOptions(BindingPriority = BindingPriority.Lowest)]
  internal sealed class TestRunnerService
  {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwServer nwServer;

    private readonly Queue<Assembly> testAssemblyQueue = new Queue<Assembly>();
    private readonly string outputDir;

    private Thread testWorkerThread;

    public TestRunnerService(PluginStorageService pluginStorageService, NwServer nwServer)
    {
      this.nwServer = nwServer;

      outputDir = pluginStorageService.GetPluginStoragePath(typeof(TestRunnerService).Assembly);
      NwModule.Instance.OnModuleLoad += OnModuleLoad;
      PopulateTestQueue();
    }

    private void PopulateTestQueue()
    {
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (assembly.GetCustomAttribute<ExecutePluginTestsAttribute>() != null)
        {
          testAssemblyQueue.Enqueue(assembly);
        }
      }
    }

    private void OnModuleLoad(ModuleEvents.OnModuleLoad eventData)
    {
      TestCommand.DefaultSynchronizationContext = NwTask.MainThreadSynchronizationContext;
      testWorkerThread = new Thread(RunTests);
      testWorkerThread.Start();
    }

    private void RunTests()
    {
      while (testAssemblyQueue.Count > 0)
      {
        Assembly testAssembly = testAssemblyQueue.Dequeue();

        Log.Info($"Running tests for assembly {testAssembly.FullName}");
        TextRunner testRunner = new TextRunner(testAssembly);
        testRunner.Execute(GetRunnerArguments(testAssembly));
      }

      Shutdown();
    }

    private async void Shutdown()
    {
      testWorkerThread = null;
      await NwTask.SwitchToMainThread();
      nwServer.ShutdownServer();
    }

    private string[] GetRunnerArguments(Assembly assembly)
    {
      string outputPath = Path.Combine(outputDir, assembly.GetName().Name!);
      string args = $"--mainthread --work={outputPath}";
      return string.IsNullOrEmpty(args) ? Array.Empty<string>() : CommandLineParser.SplitCommandLineIntoArguments(args, false).ToArray();
    }
  }
}
