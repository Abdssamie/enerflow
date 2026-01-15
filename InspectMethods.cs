using System.Reflection;
using DWSIM.Automation;

Console.WriteLine("Inspecting Automation3...");
var type = typeof(Automation3);
foreach (var method in type.GetMethods())
{
    if (method.Name.StartsWith("Calculate"))
    {
        Console.WriteLine($"{method.Name} -> {method.ReturnType.Name}");
    }
}
