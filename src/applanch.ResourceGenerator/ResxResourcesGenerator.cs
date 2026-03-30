using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace applanch.ResourceGenerator;

[Generator]
public sealed class ResxResourcesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var resxFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith("Resources.resx", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) => new ResxFile(file.Path, file.GetText(cancellationToken)?.ToString()));

        var parsedFiles = resxFiles
            .Select(static (file, _) => ResxParser.Parse(file))
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(parsedFiles), static (productionContext, source) =>
        {
            var compilation = source.Left;
            var files = source.Right;
            if (files.IsDefaultOrEmpty)
            {
                return;
            }

            var targetFile = SelectResourcesFile(files);
            if (targetFile.Entries.IsDefaultOrEmpty)
            {
                return;
            }

            var rootNamespace = string.IsNullOrWhiteSpace(compilation.AssemblyName)
                ? "applanch"
                : compilation.AssemblyName!;

            var generated = ResourcesCodeBuilder.Build(rootNamespace, targetFile.Entries);
            productionContext.AddSource("Resources.g.cs", generated);
        });
    }

    private static ParsedResxFile SelectResourcesFile(ImmutableArray<ParsedResxFile> files)
    {
        var directMatch = files.FirstOrDefault(static file => file.Path.EndsWith("Properties\\Resources.resx", StringComparison.OrdinalIgnoreCase));
        return directMatch ?? files[0];
    }
}
