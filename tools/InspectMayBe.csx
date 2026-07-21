using System;
using System.Linq;
using System.Reflection;

var dll = args.Length > 0
    ? args[0]
    : @"C:\Users\drury\.nuget\packages\nuna.lib.netstandard\3.5.161\lib\netstandard2.0\Nuna.Lib.dll";
var asm = Assembly.LoadFrom(dll);
foreach (var t in asm.GetTypes().Where(t => t.Name.Contains("MayBe") || t.Name.Contains("Maybe")))
{
    Console.WriteLine("TYPE " + t.FullName);
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        Console.WriteLine("  " + m);
}
