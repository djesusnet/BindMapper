using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace VelocityMapper.Generators;

/// <summary>
/// Source Generator that builds the mapper API and per-type map methods at compile time.
/// </summary>
[Generator]
public sealed class MapperGenerator : IIncrementalGenerator
{
    private const string MapperConfigurationAttributeName = "VelocityMapper.MapperConfigurationAttribute";
    private const string IgnoreMapAttributeName = "VelocityMapper.IgnoreMapAttribute";
    private const string MapFromAttributeName = "VelocityMapper.MapFromAttribute";

    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var mapperConfigMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => GetMapperConfigurationMethod(ctx))
            .Where(static m => m is not null);

        var compilationAndConfigs = context.CompilationProvider.Combine(mapperConfigMethods.Collect());

        context.RegisterSourceOutput(compilationAndConfigs, static (spc, source) =>
        {
            var (compilation, configMethods) = source;
            var mappings = CollectMappings(compilation, configMethods!);

            if (mappings.Count == 0)
                return;

            var sourceText = GenerateMapperSource(compilation, mappings);
            spc.AddSource("Mapper.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        });
    }

    private static MethodDeclarationSyntax? GetMapperConfigurationMethod(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
            return null;

        var symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        if (symbol is null)
            return null;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == MapperConfigurationAttributeName)
                return methodDeclaration;
        }

        return null;
    }

    private static List<MappingConfiguration> CollectMappings(Compilation compilation, ImmutableArray<MethodDeclarationSyntax?> configMethods)
    {
        var result = new List<MappingConfiguration>();

        foreach (var methodSyntax in configMethods)
        {
            if (methodSyntax is null)
                continue;

            var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);

            foreach (var invocation in methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    continue;

                if (memberAccess.Name is not GenericNameSyntax { Identifier.Text: "CreateMap", TypeArgumentList.Arguments.Count: 2 } genericName)
                    continue;

                if (memberAccess.Expression is not IdentifierNameSyntax { Identifier.Text: "Mapper" })
                    continue;

                var sourceTypeSyntax = genericName.TypeArgumentList.Arguments[0];
                var destTypeSyntax = genericName.TypeArgumentList.Arguments[1];

                var sourceType = semanticModel.GetTypeInfo(sourceTypeSyntax).Type;
                var destType = semanticModel.GetTypeInfo(destTypeSyntax).Type;

                if (sourceType is null || destType is null)
                    continue;

                if (result.Any(m => SymbolEqualityComparer.Default.Equals(m.SourceTypeSymbol, sourceType) &&
                                    SymbolEqualityComparer.Default.Equals(m.DestinationTypeSymbol, destType)))
                {
                    continue;
                }

                result.Add(new MappingConfiguration(
                    sourceType,
                    destType,
                    sourceType.ToDisplayString(TypeFormat),
                    destType.ToDisplayString(TypeFormat),
                    sourceType.Name,
                    destType.Name));
            }
        }

        return result;
    }

    private static string GenerateMapperSource(Compilation compilation, IReadOnlyList<MappingConfiguration> mappings)
    {
        _ = compilation; // currently unused but kept for future semantic access

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine("namespace VelocityMapper;");
        sb.AppendLine();
        sb.AppendLine("public static partial class Mapper");
        sb.AppendLine("{");
        sb.AppendLine("    public static MapperConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new MapperConfiguration<TSource, TDestination>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static MapperConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>(Action<MapperConfiguration<TSource, TDestination>> config)");
        sb.AppendLine("    {");
        sb.AppendLine("        var cfg = new MapperConfiguration<TSource, TDestination>();");
        sb.AppendLine("        config(cfg);");
        sb.AppendLine("        return cfg;");
        sb.AppendLine("    }");

        foreach (var mapping in mappings)
        {
            AppendMapNewInstance(sb, mapping, mappings);
            AppendMapGenericNew(sb, mapping);
            AppendMapToExisting(sb, mapping, mappings);
            AppendMapGenericExisting(sb, mapping);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendMapNewInstance(StringBuilder sb, MappingConfiguration config, IReadOnlyList<MappingConfiguration> mappings)
    {
        var destProperties = GetProperties(config.DestinationTypeSymbol);
        var sourceProperties = GetProperties(config.SourceTypeSymbol).ToDictionary(p => p.Name, StringComparer.Ordinal);

        sb.AppendLine();
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static {config.DestinationType} Map({config.SourceType} source)");
        sb.AppendLine("    {");

        if (config.SourceTypeSymbol.IsReferenceType)
        {
            sb.AppendLine("#if DEBUG");
            sb.AppendLine("        if (source is null)");
            sb.AppendLine("            throw new ArgumentNullException(nameof(source));");
            sb.AppendLine("#endif");
        }

        sb.AppendLine($"        var destination = new {config.DestinationType}();");

        foreach (var destProp in destProperties)
        {
            if (!destProp.IsWriteable || destProp.IsIgnored)
                continue;

            var sourceName = destProp.MapFrom ?? destProp.Name;
            if (!sourceProperties.TryGetValue(sourceName, out var sourceProp))
                continue;

            if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, destProp.Type))
            {
                sb.AppendLine($"        destination.{destProp.Name} = source.{sourceProp.Name};");
                continue;
            }

            if (HasMapping(sourceProp.Type, destProp.Type, mappings))
            {
                var sourceIsRef = sourceProp.Type.IsReferenceType;
                var destIsRef = destProp.Type.IsReferenceType;

                if (sourceIsRef && destIsRef)
                {
                    sb.AppendLine($"        destination.{destProp.Name} = source.{sourceProp.Name} is null ? null : Map(source.{sourceProp.Name});");
                }
                else if (!sourceIsRef && !destIsRef)
                {
                    sb.AppendLine($"        destination.{destProp.Name} = Map(source.{sourceProp.Name});");
                }
            }
        }

        sb.AppendLine("        return destination;");
        sb.AppendLine("    }");
    }

    private static void AppendMapGenericNew(StringBuilder sb, MappingConfiguration config)
    {
        sb.AppendLine();
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static TDestination Map<TDestination>({config.SourceType} source) where TDestination : {config.DestinationType}");
        sb.AppendLine("    {");
        sb.AppendLine("        return (TDestination)(object)Map(source);");
        sb.AppendLine("    }");
    }

    private static void AppendMapToExisting(StringBuilder sb, MappingConfiguration config, IReadOnlyList<MappingConfiguration> mappings)
    {
        var destProperties = GetProperties(config.DestinationTypeSymbol);
        var sourceProperties = GetProperties(config.SourceTypeSymbol).ToDictionary(p => p.Name, StringComparer.Ordinal);

        sb.AppendLine();
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static void Map({config.SourceType} source, {config.DestinationType} destination)");
        sb.AppendLine("    {");

        if (config.SourceTypeSymbol.IsReferenceType)
        {
            sb.AppendLine("#if DEBUG");
            sb.AppendLine("        if (source is null)");
            sb.AppendLine("            throw new ArgumentNullException(nameof(source));");
            sb.AppendLine("#endif");
        }

        if (config.DestinationTypeSymbol.IsReferenceType)
        {
            sb.AppendLine("#if DEBUG");
            sb.AppendLine("        if (destination is null)");
            sb.AppendLine("            throw new ArgumentNullException(nameof(destination));");
            sb.AppendLine("#endif");
        }

        foreach (var destProp in destProperties)
        {
            if (!destProp.IsWriteable || destProp.IsIgnored)
                continue;

            var sourceName = destProp.MapFrom ?? destProp.Name;
            if (!sourceProperties.TryGetValue(sourceName, out var sourceProp))
                continue;

            if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, destProp.Type))
            {
                sb.AppendLine($"        destination.{destProp.Name} = source.{sourceProp.Name};");
                continue;
            }

            if (HasMapping(sourceProp.Type, destProp.Type, mappings))
            {
                var sourceIsRef = sourceProp.Type.IsReferenceType;
                var destIsRef = destProp.Type.IsReferenceType;

                if (sourceIsRef && destIsRef)
                {
                    sb.AppendLine($"        if (source.{sourceProp.Name} is null)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            destination.{destProp.Name} = null;");
                    sb.AppendLine("        }");
                    sb.AppendLine("        else");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            if (destination.{destProp.Name} is null)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                destination.{destProp.Name} = Map(source.{sourceProp.Name});");
                    sb.AppendLine("            }");
                    sb.AppendLine("            else");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                Map(source.{sourceProp.Name}, destination.{destProp.Name});");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }
                else if (!sourceIsRef && !destIsRef)
                {
                    sb.AppendLine($"        destination.{destProp.Name} = Map(source.{sourceProp.Name});");
                }
            }
        }

        sb.AppendLine("    }");
    }

    private static void AppendMapGenericExisting(StringBuilder sb, MappingConfiguration config)
    {
        sb.AppendLine();
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static void Map<TDestination>({config.SourceType} source, TDestination destination) where TDestination : {config.DestinationType}");
        sb.AppendLine("    {");
        sb.AppendLine("        Map(source, (" + config.DestinationType + ")destination);");
        sb.AppendLine("    }");
    }

    private static List<PropertyInfo> GetProperties(ITypeSymbol typeSymbol)
    {
        var properties = new List<PropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            if (property.DeclaredAccessibility != Accessibility.Public || property.IsStatic)
                continue;

            var isIgnored = property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == IgnoreMapAttributeName);
            var mapFrom = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MapFromAttributeName)?
                .ConstructorArguments.FirstOrDefault().Value as string;

            properties.Add(new PropertyInfo(
                property.Name,
                property.Type,
                property.GetMethod is not null && property.GetMethod.DeclaredAccessibility == Accessibility.Public,
                property.SetMethod is not null && property.SetMethod.DeclaredAccessibility == Accessibility.Public,
                isIgnored,
                mapFrom));
        }

        return properties;
    }

    private static bool HasMapping(ITypeSymbol sourceType, ITypeSymbol destType, IReadOnlyList<MappingConfiguration> mappings)
    {
        foreach (var mapping in mappings)
        {
            if (SymbolEqualityComparer.Default.Equals(mapping.SourceTypeSymbol, sourceType) &&
                SymbolEqualityComparer.Default.Equals(mapping.DestinationTypeSymbol, destType))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record MappingConfiguration(
        ITypeSymbol SourceTypeSymbol,
        ITypeSymbol DestinationTypeSymbol,
        string SourceType,
        string DestinationType,
        string SourceTypeName,
        string DestinationTypeName);

    private sealed record PropertyInfo(
        string Name,
        ITypeSymbol Type,
        bool IsReadable,
        bool IsWriteable,
        bool IsIgnored,
        string? MapFrom);
}

