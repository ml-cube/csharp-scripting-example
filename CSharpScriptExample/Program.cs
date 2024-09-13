using CSharpScriptExample;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

// --------------------------
// Approach #1 - CSharpScript
// --------------------------

Console.WriteLine($"------ Approach 1 (CSharpScript) -------{Environment.NewLine}");

var sourceCode = await File.ReadAllTextAsync("Model.cs");

// You can use FileSystemWatcher to reload and recompile
// the script after the file Model.cs is changed
var script =
    CSharpScript.Create(
        code: sourceCode,
        options: ScriptOptions.Default,
        globalsType: typeof(ScriptGlobals));

// This might be slow, so it's best to dispatch it to a separate thread
Console.WriteLine("Compiling the script...");
var sw = Stopwatch.StartNew();
script.Compile();
sw.Stop();
Console.WriteLine($"Compiled in {sw.ElapsedMilliseconds} ms");

var globals = new ScriptGlobals
{
    MaxValue = 100
};

// Now that it's compiled, we can run the script as often as desired with lightning-fast speed
Console.WriteLine("Executing the script...");
sw.Restart();
var state = await script.RunAsync(globals, null, CancellationToken.None);
sw.Stop();
Console.WriteLine($"Execution finished in {sw.ElapsedMilliseconds} ms");

// The first run is slower than the next ones, so we run it twice to show that it's fast
Console.WriteLine("Executing the script again...");
sw.Restart();
state = await script.RunAsync(globals, null, CancellationToken.None);
sw.Stop();
Console.WriteLine($"Execution finished in {sw.ElapsedMilliseconds} ms");

var prediction = (int)state.Variables.First(v => v.Name == "prediction").Value;

Console.WriteLine($"Prediction: {prediction}");

// ----------------------------------------
// Approach #2 - CSharpCompilation (faster)
// ----------------------------------------

Console.WriteLine($"{Environment.NewLine}------ Approach 2 (CSharpCompilation) -------{Environment.NewLine}");

sourceCode = await File.ReadAllTextAsync("Model2.cs");

// These are the assemblies that should be referenced
var references = new List<string>
{
    typeof(object).GetTypeInfo().Assembly.Location, // System assembly for accessing the System.Random type
}.Select(path => MetadataReference.CreateFromFile(path)).ToArray();

// Parse the syntax tree and plan the compilation process
var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
var assemblyName = Path.GetRandomFileName();
var compilation = CSharpCompilation.Create(
    assemblyName,
    syntaxTrees: new[] { syntaxTree },
    references: references,
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

using var memoryStream = new MemoryStream();

// This might be slow, so it's best to dispatch it to a separate thread
Console.WriteLine("Emitting the DLL to memory...");
sw.Restart();
var emitResult = compilation.Emit(memoryStream);
sw.Stop();
Console.WriteLine($"DLL emitted in {sw.ElapsedMilliseconds} ms");

// If there are compilation errors, they will be printed to the console
if (!emitResult.Success)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Failed to compile!");
    var errors = emitResult.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
        .Select(d => $"{d.Id} - {d.GetMessage()}");

    Console.WriteLine(string.Join(Environment.NewLine, errors));
    return;
}

memoryStream.Seek(0, SeekOrigin.Begin);

// Here we could save the memory stream to a DLL file if we wanted, but we
// can simply load it directly as an assembly into memory
Console.WriteLine("Loading the assembly from memory...");
sw.Restart();
var assembly = AssemblyLoadContext.Default.LoadFromStream(memoryStream);
sw.Stop();
Console.WriteLine($"Assembly loaded in {sw.ElapsedMilliseconds} ms");

// Get the method info through reflection
var predictMethod = assembly.GetType("CSharpScriptExample.Model2")!.GetMember("Predict").First() as MethodInfo;

// Invoke the method
Console.WriteLine("Executing the method...");
sw.Restart();
prediction = (int)predictMethod!.Invoke(null, new object[] { globals.MaxValue })!;
sw.Stop();
Console.WriteLine($"Execution finished in {sw.ElapsedMilliseconds} ms");

Console.WriteLine($"Prediction: {prediction}");
