using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public static class Runner
{
    public static int Main()
    {
        int passed = 0, failed = 0;
        var fixtures = typeof(Runner).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null).OrderBy(t => t.FullName);
        foreach (var type in fixtures)
        {
            var setUp = type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
            foreach (var m in type.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null).OrderBy(m => m.Name))
            {
                var inst = Activator.CreateInstance(type);
                try
                {
                    setUp?.Invoke(inst, null);
                    m.Invoke(inst, null);
                    passed++;
                    Console.WriteLine($"  PASS {type.Name}.{m.Name}");
                }
                catch (TargetInvocationException e)
                {
                    failed++;
                    Console.WriteLine($"  FAIL {type.Name}.{m.Name}: {e.InnerException?.Message}");
                }
            }
        }
        Console.WriteLine($"\n{passed} passed, {failed} failed, {passed + failed} total");
        return failed == 0 ? 0 : 1;
    }
}
