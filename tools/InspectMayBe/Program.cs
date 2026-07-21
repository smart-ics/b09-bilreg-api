using System.Reflection;

var dll = @"C:\Users\drury\.nuget\packages\nuna.lib.netstandard\3.5.161\lib\netstandard2.0\Nuna.Lib.dll";
var asm = Assembly.LoadFrom(dll);
foreach (var t in asm.GetTypes().Where(t => t.FullName!.Contains("DataAccess") || t.Name.Contains("Sql") || t.Name.Contains("Connection")))
{
    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(m => m.Name is "Read" or "ReadSingle" or "Query")
        .ToList();
    if (methods.Count == 0) continue;
    Console.WriteLine("TYPE " + t.FullName);
    foreach (var m in methods) Console.WriteLine("  " + m);
}
