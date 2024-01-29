using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace DotJEM.Json.Storage2.Code.Generator;

[Generator]
public class HelloSourceGenerator : IIncrementalGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        IMethodSymbol? mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

        
        context.AddSource("SqlScriptFiles.g.cs", "");
        // Code generation goes here
    }

    //public void Initialize(GeneratorInitializationContext context)
    //{
    //    context.

    //    // No initialization required for this one
    //}

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //    if (!Debugger.IsAttached)
        //    {
        //        Debugger.Launch();
        //    }'
        // SEE:https://github.com/podimo/Podimo.ConstEmbed/blob/develop/src/Podimo.ConstEmbed/ConstEmbedGenerator.cs
        //context.AdditionalTextsProvider.Select((text, token) =>
        //{

        //    text.

        //});
    }
}