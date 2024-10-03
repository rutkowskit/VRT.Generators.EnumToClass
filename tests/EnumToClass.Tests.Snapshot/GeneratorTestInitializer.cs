using System.Runtime.CompilerServices;

namespace EnumToClass.Tests.Snapshot;

public static class GeneratorTestInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
