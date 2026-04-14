using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace applanch.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AppIdResolverAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    private const string InterfaceMetadataName = "applanch.Infrastructure.Launch.AppIdResolvers.IAppIdResolver";
    private const string AppIdSourceAttributeMetadataName = "applanch.Infrastructure.Launch.AppIdResolvers.AppIdSourceAttribute";
    private const string AppIdSourcePrefixAttributeMetadataName = "applanch.Infrastructure.Launch.AppIdResolvers.AppIdSourcePrefixAttribute";

    internal const string DiagnosticId = "APPL001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "AppId resolver attributes require IAppIdResolver",
        messageFormat: "Type '{0}' must implement IAppIdResolver to use '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "AppIdSource and AppIdSourcePrefix attributes can only be applied to IAppIdResolver implementations.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var resolverInterface = compilationContext.Compilation.GetTypeByMetadataName(InterfaceMetadataName);
            if (resolverInterface is null)
            {
                return;
            }

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, resolverInterface),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol resolverInterface)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!IsResolverAttribute(attribute.AttributeClass))
            {
                continue;
            }

            if (ImplementsResolver(typeSymbol, resolverInterface))
            {
                continue;
            }

            var attributeName = attribute.AttributeClass?.Name ?? "attribute";
            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                ?? typeSymbol.Locations.FirstOrDefault();
            if (location is null)
            {
                continue;
            }

            var diagnostic = Diagnostic.Create(Rule, location, typeSymbol.Name, attributeName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool ImplementsResolver(INamedTypeSymbol typeSymbol, INamedTypeSymbol resolverInterface)
    {
        return typeSymbol.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface, resolverInterface));
    }

    private static bool IsResolverAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass is null)
        {
            return false;
        }

        if (attributeClass.ContainingNamespace.ToDisplayString() != "applanch.Infrastructure.Launch.AppIdResolvers")
        {
            return false;
        }

        return attributeClass.MetadataName == AppIdSourceAttributeMetadataName
            || attributeClass.MetadataName == AppIdSourcePrefixAttributeMetadataName;
    }
}
