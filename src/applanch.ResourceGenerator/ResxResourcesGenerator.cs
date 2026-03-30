using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace applanch.ResourceGenerator;

[Generator]
public sealed class ResxResourcesGenerator : IIncrementalGenerator
{
    private const string BaseResxFileName = "Resources.resx";
    private const string ProjectResxPathSuffix = "Properties\\Resources.resx";
    private const string FallbackRootNamespace = "applanch";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var resxFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(BaseResxFileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) => new ResxFile(file.Path, file.GetText(cancellationToken)?.ToString()));

        var parsedFiles = resxFiles
            .Select(static (file, _) => ResxParser.Parse(file))
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(parsedFiles), static (productionContext, source) =>
        {
            var compilation = source.Left;
            var files = source.Right;
            if (!TrySelectTargetFile(files, out var targetFile))
            {
                return;
            }

            var rootNamespace = ResolveRootNamespace(compilation.AssemblyName);

            var generated = ResourcesCodeBuilder.Build(rootNamespace, targetFile.Entries);
            productionContext.AddSource("Resources.g.cs", generated);
        });
    }

    private static bool TrySelectTargetFile(ImmutableArray<ParsedResxFile> files, out ParsedResxFile targetFile)
    {
        if (files.IsDefaultOrEmpty)
        {
            targetFile = null!;
            return false;
        }

        var directMatch = files.FirstOrDefault(static file => file.Path.EndsWith(ProjectResxPathSuffix, StringComparison.OrdinalIgnoreCase));
        targetFile = directMatch ?? files[0];

        return !targetFile.Entries.IsDefaultOrEmpty;
    }

    private static string ResolveRootNamespace(string? assemblyName)
    {
        return string.IsNullOrWhiteSpace(assemblyName)
            ? FallbackRootNamespace
            : assemblyName!;
    }
}
