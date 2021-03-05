using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NWN
{
  internal static class Assemblies
  {
    internal static readonly Assembly Managed = typeof(Assemblies).Assembly;
    internal static readonly Assembly Core = typeof(Core.NWNCore).Assembly;
    internal static readonly Assembly Native = typeof(Native.API.NWNXLib).Assembly;

    internal static readonly string AssemblyDir = Path.GetDirectoryName(Managed.Location);

    public static readonly Assembly[] AllAssemblies =
    {
      Managed,
      Core,
      Native,
      typeof(NLog.Logger).Assembly,
      typeof(LightInject.ServiceContainer).Assembly
    };

    public static readonly List<string> ReservedNames = AllAssemblies
      .Select(assembly => assembly.GetName().Name)
      .ToList();
  }
}
