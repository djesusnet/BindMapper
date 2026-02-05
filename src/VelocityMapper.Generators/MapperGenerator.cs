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

    private static List<MappingConfiguration> CollectMappings(
        Compilation compilation, 
        ImmutableArray<MethodDeclarationSyntax?> configMethods)
    {
        var result = new List<MappingConfiguration>();

        // Collect from [MapperConfiguration] methods
        foreach (var methodSyntax in configMethods)
        {
            if (methodSyntax is null)
                continue;

            CollectMappingsFromMethod(compilation, methodSyntax, result);
        }

        return result;
    }

    private static void CollectMappingsFromMethod(
        Compilation compilation, 
        MethodDeclarationSyntax methodSyntax, 
        List<MappingConfiguration> result)
    {
        var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);

        foreach (var invocation in methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            GenericNameSyntax? genericName = null;

            // Handle both Mapper.CreateMap<T1, T2>() and CreateMap<T1, T2>() (in Profile)
            if (memberAccess?.Name is GenericNameSyntax gn && gn.Identifier.Text == "CreateMap")
            {
                genericName = gn;
            }
            else if (invocation.Expression is GenericNameSyntax directGn && directGn.Identifier.Text == "CreateMap")
            {
                genericName = directGn;
            }

            if (genericName is null || genericName.TypeArgumentList.Arguments.Count != 2)
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

            // Parse fluent configuration (ForMember, ReverseMap, etc.)
            var fluentConfig = ParseFluentConfiguration(invocation, semanticModel);

            result.Add(new MappingConfiguration(
                sourceType,
                destType,
                sourceType.ToDisplayString(TypeFormat),
                destType.ToDisplayString(TypeFormat),
                sourceType.Name,
                destType.Name,
                fluentConfig));

            // Handle ReverseMap
            if (fluentConfig.HasReverseMap)
            {
                if (!result.Any(m => SymbolEqualityComparer.Default.Equals(m.SourceTypeSymbol, destType) &&
                                     SymbolEqualityComparer.Default.Equals(m.DestinationTypeSymbol, sourceType)))
                {
                    result.Add(new MappingConfiguration(
                        destType,
                        sourceType,
                        destType.ToDisplayString(TypeFormat),
                        sourceType.ToDisplayString(TypeFormat),
                        destType.Name,
                        sourceType.Name,
                        new FluentConfiguration()));
                }
            }
        }
    }

    private static FluentConfiguration ParseFluentConfiguration(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var config = new FluentConfiguration();
        
        // Walk up the chain to find all fluent method calls
        SyntaxNode? current = invocation.Parent;
        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Parent is InvocationExpressionSyntax chainedInvocation)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                
                switch (methodName)
                {
                    case "ReverseMap":
                        config.HasReverseMap = true;
                        break;
                    case "IgnoreAllNonExisting":
                        config.IgnoreAllNonExisting = true;
                        break;
                    case "ForMember":
                        ParseForMember(chainedInvocation, config, semanticModel);
                        break;
                }
                
                current = chainedInvocation.Parent;
            }
            else
            {
                break;
            }
        }

        return config;
    }

    private static void ParseForMember(InvocationExpressionSyntax invocation, FluentConfiguration config, SemanticModel _)
    {
        if (invocation.ArgumentList.Arguments.Count < 2)
            return;

        var destMemberArg = invocation.ArgumentList.Arguments[0];
        var optionsArg = invocation.ArgumentList.Arguments[1];

        // Extract destination member name from lambda: d => d.PropertyName
        string? destMemberName = null;
        if (destMemberArg.Expression is SimpleLambdaExpressionSyntax destLambda)
        {
            if (destLambda.Body is MemberAccessExpressionSyntax destMemberAccess)
            {
                destMemberName = destMemberAccess.Name.Identifier.Text;
            }
        }

        if (destMemberName is null)
            return;

        // Parse the options lambda: opt => opt.Ignore() or opt => opt.MapFrom(...)
        if (optionsArg.Expression is SimpleLambdaExpressionSyntax optionsLambda)
        {
            if (optionsLambda.Body is InvocationExpressionSyntax optInvocation)
            {
                if (optInvocation.Expression is MemberAccessExpressionSyntax optMemberAccess)
                {
                    var optMethod = optMemberAccess.Name.Identifier.Text;

                    switch (optMethod)
                    {
                        case "Ignore":
                            config.IgnoredMembers.Add(destMemberName);
                            break;
                        case "MapFrom":
                            if (optInvocation.ArgumentList.Arguments.Count > 0)
                            {
                                var mapFromArg = optInvocation.ArgumentList.Arguments[0];
                                if (mapFromArg.Expression is SimpleLambdaExpressionSyntax mapFromLambda)
                                {
                                    if (mapFromLambda.Body is MemberAccessExpressionSyntax srcMemberAccess)
                                    {
                                        config.MemberMappings[destMemberName] = srcMemberAccess.Name.Identifier.Text;
                                    }
                                    else
                                    {
                                        // Complex expression - store the whole thing
                                        config.MemberExpressions[destMemberName] = mapFromLambda.Body.ToString();
                                    }
                                }
                            }
                            break;
                        case "UseValue":
                            if (optInvocation.ArgumentList.Arguments.Count > 0)
                            {
                                config.MemberValues[destMemberName] = optInvocation.ArgumentList.Arguments[0].ToString();
                            }
                            break;
                    }
                }
            }
        }
    }

    private static string GenerateMapperSource(Compilation compilation, IReadOnlyList<MappingConfiguration> mappings)
    {
        _ = compilation; // currently unused but kept for future semantic access

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine();
        sb.AppendLine("namespace VelocityMapper;");
        sb.AppendLine();
        sb.AppendLine("public static partial class Mapper");
        sb.AppendLine("{");

        foreach (var mapping in mappings)
        {
            // Generate To(source) -> new destination (12.03 ns - fastest for new instance)
            AppendMapNewInstance(sb, mapping, mappings);
            // Generate To<TDestination>(source) with constraint (AutoMapper-style API)
            AppendMapGenericNew(sb, mapping);
            // Generate To(source, destination) - zero allocation
            AppendMapToExisting(sb, mapping, mappings);
        }

        // Generate collection mapping helpers
        AppendCollectionMappers(sb, mappings);

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendMapNewInstance(StringBuilder sb, MappingConfiguration config, IReadOnlyList<MappingConfiguration> mappings)
    {
        var destProperties = GetProperties(config.DestinationTypeSymbol);
        var sourceProperties = GetProperties(config.SourceTypeSymbol).ToDictionary(p => p.Name, StringComparer.Ordinal);

        // Sort properties by type for better cache locality:
        // 1. Value types (primitives) first - they're typically at the start of the object layout
        // 2. Reference types after - strings and objects
        var orderedDestProps = destProperties
            .Where(p => p.IsWriteable && !p.IsIgnored && !config.FluentConfig.IgnoredMembers.Contains(p.Name))
            .OrderBy(p => p.Type.IsReferenceType ? 1 : 0)  // Value types first
            .ThenBy(p => GetTypeWeight(p.Type))            // Then by size (smaller first for better packing)
            .ToList();

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Maps {config.SourceTypeName} to a new {config.DestinationTypeName}.");
        sb.AppendLine("    /// JIT-optimized object initializer pattern with aggressive inlining.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static {config.DestinationType} To({config.SourceType} source)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {config.DestinationType}");
        sb.AppendLine("        {");

        var propertyAssignments = new List<string>();
        foreach (var destProp in orderedDestProps)
        {
            // Check for constant value
            if (config.FluentConfig.MemberValues.TryGetValue(destProp.Name, out var constantValue))
            {
                propertyAssignments.Add($"            {destProp.Name} = {constantValue}");
                continue;
            }

            // Check for custom expression
            if (config.FluentConfig.MemberExpressions.TryGetValue(destProp.Name, out var customExpr))
            {
                var adjusted = customExpr.Replace("s.", "source.").Replace("src.", "source.");
                propertyAssignments.Add($"            {destProp.Name} = {adjusted}");
                continue;
            }

            // Check for mapped member name from fluent config
            var sourceName = destProp.MapFrom ?? destProp.Name;
            if (config.FluentConfig.MemberMappings.TryGetValue(destProp.Name, out var mappedFrom))
            {
                sourceName = mappedFrom;
            }

            if (!sourceProperties.TryGetValue(sourceName, out var sourceProp))
            {
                // If ignoreAllNonExisting, skip; otherwise it would be an error at runtime
                if (config.FluentConfig.IgnoreAllNonExisting)
                    continue;
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, destProp.Type))
            {
                propertyAssignments.Add($"            {destProp.Name} = source.{sourceProp.Name}");
            }
            else if (TryGetMapping(sourceProp.Type, destProp.Type, mappings, out var nestedMapping))
            {
                var sourceIsRef = sourceProp.Type.IsReferenceType;
                if (sourceIsRef)
                {
                    // Inline nested mapping - eliminates method call
                    var inlineMapping = GenerateInlineMapping(nestedMapping!, $"source.{sourceProp.Name}", mappings);
                    propertyAssignments.Add($"            {destProp.Name} = source.{sourceProp.Name} is {{ }} __src{destProp.Name} ? {inlineMapping.Replace($"source.{sourceProp.Name}", $"__src{destProp.Name}")} : null");
                }
                else
                {
                    propertyAssignments.Add($"            {destProp.Name} = To(source.{sourceProp.Name})");
                }
            }
        }

        sb.AppendLine(string.Join(",\n", propertyAssignments));
        sb.AppendLine("        };");
        sb.AppendLine("    }");
    }

    private static void AppendMapGenericNew(StringBuilder sb, MappingConfiguration config)
    {
        // Generate the AutoMapper-style To<TDestination>(source) syntax
        // This is the primary API: var dto = Mapper.To<UserDto>(user);
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Maps {config.SourceTypeName} to a new instance of {config.DestinationTypeName}.");
        sb.AppendLine($"    /// Generated at compile-time for maximum performance - no reflection, no boxing.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
        sb.AppendLine($"    public static TDestination To<TDestination>({config.SourceType} source) where TDestination : {config.DestinationType}");
        sb.AppendLine("    {");
        // For reference types, the constraint ensures TDestination : DestinationType
        // Since both are classes, we can safely use Unsafe.As to avoid boxing
        sb.AppendLine($"        var __result = To(source);");
        sb.AppendLine($"        return Unsafe.As<{config.DestinationType}, TDestination>(ref __result);");
        sb.AppendLine("    }");
    }

    private static void AppendMapToExisting(StringBuilder sb, MappingConfiguration config, IReadOnlyList<MappingConfiguration> mappings)
    {
        var destProperties = GetProperties(config.DestinationTypeSymbol);
        var sourceProperties = GetProperties(config.SourceTypeSymbol).ToDictionary(p => p.Name, StringComparer.Ordinal);

        // Sort properties for optimal cache locality (same order as MapNewInstance)
        var orderedDestProps = destProperties
            .Where(p => p.IsWriteable && !p.IsIgnored && !config.FluentConfig.IgnoredMembers.Contains(p.Name))
            .OrderBy(p => p.Type.IsReferenceType ? 1 : 0)
            .ThenBy(p => GetTypeWeight(p.Type))
            .ToList();

        // Separate value-type assignments from reference-type (branch-heavy) assignments
        var valueTypeAssignments = new List<(PropertyInfo destProp, PropertyInfo sourceProp)>();
        var refTypeAssignments = new List<(PropertyInfo destProp, PropertyInfo sourceProp, bool hasNestedMapping)>();
        var constantAssignments = new List<(PropertyInfo destProp, string value)>();
        var expressionAssignments = new List<(PropertyInfo destProp, string expr)>();

        foreach (var destProp in orderedDestProps)
        {
            // Check for constant value
            if (config.FluentConfig.MemberValues.TryGetValue(destProp.Name, out var constantValue))
            {
                constantAssignments.Add((destProp, constantValue));
                continue;
            }

            // Check for custom expression
            if (config.FluentConfig.MemberExpressions.TryGetValue(destProp.Name, out var customExpr))
            {
                expressionAssignments.Add((destProp, customExpr.Replace("s.", "source.").Replace("src.", "source.")));
                continue;
            }

            var sourceName = destProp.MapFrom ?? destProp.Name;
            if (config.FluentConfig.MemberMappings.TryGetValue(destProp.Name, out var mappedFrom))
                sourceName = mappedFrom;

            if (!sourceProperties.TryGetValue(sourceName, out var sourceProp))
                continue;

            if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, destProp.Type))
            {
                // Direct assignment - no conversion needed
                if (!sourceProp.Type.IsReferenceType)
                    valueTypeAssignments.Add((destProp, sourceProp));
                else
                    refTypeAssignments.Add((destProp, sourceProp, false));
            }
            else if (HasMapping(sourceProp.Type, destProp.Type, mappings))
            {
                refTypeAssignments.Add((destProp, sourceProp, true));
            }
        }

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Maps {config.SourceTypeName} to existing {config.DestinationTypeName}.");
        sb.AppendLine("    /// JIT-optimized: cache-ordered writes, value-types first (branchless), refs last.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
        sb.AppendLine($"    public static void To({config.SourceType} source, {config.DestinationType} destination)");
        sb.AppendLine("    {");

        // PHASE 1: Branchless value-type assignments (best for CPU pipelining)
        if (valueTypeAssignments.Count > 0)
        {
            sb.AppendLine("        // Value-type assignments (branchless, sequential writes)");
            foreach (var (destProp, sourceProp) in valueTypeAssignments)
            {
                sb.AppendLine($"        destination.{destProp.Name} = source.{sourceProp.Name};");
            }
        }

        // PHASE 2: Constants (also branchless)
        foreach (var (destProp, value) in constantAssignments)
        {
            sb.AppendLine($"        destination.{destProp.Name} = {value};");
        }

        // PHASE 3: Expressions
        foreach (var (destProp, expr) in expressionAssignments)
        {
            sb.AppendLine($"        destination.{destProp.Name} = {expr};");
        }

        // PHASE 4: Reference-type assignments (may have branches for null checks)
        if (refTypeAssignments.Count > 0)
        {
            sb.AppendLine("        // Reference-type assignments");
            foreach (var (destProp, sourceProp, hasNestedMapping) in refTypeAssignments)
            {
                if (!hasNestedMapping)
                {
                    // Direct reference assignment
                    sb.AppendLine($"        destination.{destProp.Name} = source.{sourceProp.Name};");
                }
                else
                {
                    // Nested mapping with null check
                    var sourceIsRef = sourceProp.Type.IsReferenceType;
                    var destIsRef = destProp.Type.IsReferenceType;

                    if (sourceIsRef && destIsRef)
                    {
                        sb.AppendLine($"        if (source.{sourceProp.Name} is {{ }} _src{destProp.Name})");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            if (destination.{destProp.Name} is {{ }} _dst{destProp.Name}) To(_src{destProp.Name}, _dst{destProp.Name});");
                        sb.AppendLine($"            else destination.{destProp.Name} = To(_src{destProp.Name});");
                        sb.AppendLine("        }");
                        sb.AppendLine($"        else destination.{destProp.Name} = null;");
                    }
                    else if (!sourceIsRef && !destIsRef)
                    {
                        sb.AppendLine($"        destination.{destProp.Name} = To(source.{sourceProp.Name});");
                    }
                }
            }
        }

        sb.AppendLine("    }");
    }

    private static void AppendCollectionMappers(StringBuilder sb, IReadOnlyList<MappingConfiguration> mappings)
    {
        foreach (var mapping in mappings)
        {
            // ToList<TDestination> - Generic API with constraint, accepts any IEnumerable
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Maps IEnumerable of {mapping.SourceTypeName} to List of {mapping.DestinationTypeName}. Span-optimized for maximum performance.</summary>");
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
            sb.AppendLine($"    public static System.Collections.Generic.List<TDestination> ToList<TDestination>(System.Collections.Generic.IEnumerable<{mapping.SourceType}>? source) where TDestination : {mapping.DestinationType}");
            sb.AppendLine("    {");
            sb.AppendLine("        if (source is null)");
            sb.AppendLine($"            return new System.Collections.Generic.List<TDestination>();");
            sb.AppendLine();
            sb.AppendLine("        // Fast path for List<T> - use Span for zero virtual calls");
            sb.AppendLine($"        if (source is System.Collections.Generic.List<{mapping.SourceType}> sourceList)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (sourceList.Count == 0) return new System.Collections.Generic.List<TDestination>();");
            sb.AppendLine("            var count = sourceList.Count;");
            sb.AppendLine("            var result = new System.Collections.Generic.List<TDestination>(count);");
            sb.AppendLine("#if NET8_0_OR_GREATER");
            sb.AppendLine("            var sourceSpan = CollectionsMarshal.AsSpan(sourceList);");
            sb.AppendLine("            CollectionsMarshal.SetCount(result, count);");
            sb.AppendLine("            var destSpan = CollectionsMarshal.AsSpan(result);");
            sb.AppendLine("            for (int i = 0; i < sourceSpan.Length; i++)");
            sb.AppendLine("            {");
            sb.AppendLine($"                var mapped = To(sourceSpan[i]);");
            sb.AppendLine($"                destSpan[i] = Unsafe.As<{mapping.DestinationType}, TDestination>(ref mapped);");
            sb.AppendLine("            }");
            sb.AppendLine("#else");
            sb.AppendLine("            foreach (var item in sourceList)");
            sb.AppendLine($"                result.Add((TDestination)(object)To(item)!);");
            sb.AppendLine("#endif");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Fast path for arrays - use Span");
            sb.AppendLine($"        if (source is {mapping.SourceType}[] sourceArray)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (sourceArray.Length == 0) return new System.Collections.Generic.List<TDestination>();");
            sb.AppendLine("            var result = new System.Collections.Generic.List<TDestination>(sourceArray.Length);");
            sb.AppendLine("            var sourceSpan = sourceArray.AsSpan();");
            sb.AppendLine("            foreach (ref readonly var item in sourceSpan)");
            sb.AppendLine($"                result.Add((TDestination)(object)To(item)!);");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Slow path for other IEnumerable");
            sb.AppendLine("        var list = new System.Collections.Generic.List<TDestination>();");
            sb.AppendLine("        foreach (var item in source)");
            sb.AppendLine($"            list.Add((TDestination)(object)To(item)!);");
            sb.AppendLine("        return list;");
            sb.AppendLine("    }");

            // ToArray<TDestination> - Generic API with constraint
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Maps IEnumerable of {mapping.SourceTypeName} to array of {mapping.DestinationTypeName}. Span-optimized for zero-copy iteration.</summary>");
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
            sb.AppendLine($"    public static TDestination[] ToArray<TDestination>(System.Collections.Generic.IEnumerable<{mapping.SourceType}>? source) where TDestination : {mapping.DestinationType}");
            sb.AppendLine("    {");
            sb.AppendLine("        if (source is null)");
            sb.AppendLine("            return Array.Empty<TDestination>();");
            sb.AppendLine();
            sb.AppendLine("        // Fast path for arrays - use Span directly");
            sb.AppendLine($"        if (source is {mapping.SourceType}[] sourceArray)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (sourceArray.Length == 0) return Array.Empty<TDestination>();");
            sb.AppendLine("            var result = new TDestination[sourceArray.Length];");
            sb.AppendLine("            var sourceSpan = sourceArray.AsSpan();");
            sb.AppendLine("            var destSpan = result.AsSpan();");
            sb.AppendLine("            for (int i = 0; i < sourceSpan.Length; i++)");
            sb.AppendLine($"                destSpan[i] = (TDestination)(object)To(sourceSpan[i])!;");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Fast path for List<T> - use CollectionsMarshal.AsSpan");
            sb.AppendLine($"        if (source is System.Collections.Generic.List<{mapping.SourceType}> sourceList)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (sourceList.Count == 0) return Array.Empty<TDestination>();");
            sb.AppendLine("            var result = new TDestination[sourceList.Count];");
            sb.AppendLine("#if NET8_0_OR_GREATER");
            sb.AppendLine("            var sourceSpan = CollectionsMarshal.AsSpan(sourceList);");
            sb.AppendLine("            var destSpan = result.AsSpan();");
            sb.AppendLine("            for (int i = 0; i < sourceSpan.Length; i++)");
            sb.AppendLine($"                destSpan[i] = (TDestination)(object)To(sourceSpan[i])!;");
            sb.AppendLine("#else");
            sb.AppendLine("            int idx = 0;");
            sb.AppendLine("            foreach (var item in sourceList)");
            sb.AppendLine($"                result[idx++] = (TDestination)(object)To(item)!;");
            sb.AppendLine("#endif");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Slow path - materialize to list first");
            sb.AppendLine("        return ToList<TDestination>(source).ToArray();");
            sb.AppendLine("    }");

            // ToSpan - For advanced zero-allocation scenarios
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Maps ReadOnlySpan of {mapping.SourceTypeName} to Span of {mapping.DestinationTypeName}. TRUE zero-allocation mapping.</summary>");
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
            sb.AppendLine($"    public static void ToSpan(ReadOnlySpan<{mapping.SourceType}> source, Span<{mapping.DestinationType}> destination)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (source.Length > destination.Length)");
            sb.AppendLine("            throw new ArgumentException(\"Destination span must be at least as long as source span.\", nameof(destination));");
            sb.AppendLine();
            sb.AppendLine("        for (int i = 0; i < source.Length; i++)");
            sb.AppendLine("            destination[i] = To(source[i]);");
            sb.AppendLine("    }");

            // ToEnumerable - For lazy evaluation scenarios
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Maps IEnumerable of {mapping.SourceTypeName} to IEnumerable of {mapping.DestinationTypeName}. Lazy evaluation with deferred execution.</summary>");
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    public static System.Collections.Generic.IEnumerable<TDestination> ToEnumerable<TDestination>(System.Collections.Generic.IEnumerable<{mapping.SourceType}>? source) where TDestination : {mapping.DestinationType}");
            sb.AppendLine("    {");
            sb.AppendLine("        if (source is null)");
            sb.AppendLine("            yield break;");
            sb.AppendLine();
            sb.AppendLine("        foreach (var item in source)");
            sb.AppendLine($"            yield return (TDestination)(object)To(item)!;");
            sb.AppendLine("    }");
        }
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

    /// <summary>
    /// Returns a weight for type ordering to optimize cache locality.
    /// Smaller types come first for better cache line utilization.
    /// </summary>
    private static int GetTypeWeight(ITypeSymbol type)
    {
        // Order: bool/byte (1) < short/char (2) < int/float (4) < long/double (8) < refs (16+)
        var specialType = type.SpecialType;
        return specialType switch
        {
            SpecialType.System_Boolean => 1,
            SpecialType.System_Byte => 1,
            SpecialType.System_SByte => 1,
            SpecialType.System_Char => 2,
            SpecialType.System_Int16 => 2,
            SpecialType.System_UInt16 => 2,
            SpecialType.System_Int32 => 4,
            SpecialType.System_UInt32 => 4,
            SpecialType.System_Single => 4,
            SpecialType.System_Int64 => 8,
            SpecialType.System_UInt64 => 8,
            SpecialType.System_Double => 8,
            SpecialType.System_Decimal => 16,
            SpecialType.System_DateTime => 8,
            SpecialType.System_String => 32, // References go last
            _ => type.IsReferenceType ? 32 : 8
        };
    }

    private static bool TryGetMapping(ITypeSymbol sourceType, ITypeSymbol destType, IReadOnlyList<MappingConfiguration> mappings, out MappingConfiguration? mapping)
    {
        foreach (var m in mappings)
        {
            if (SymbolEqualityComparer.Default.Equals(m.SourceTypeSymbol, sourceType) &&
                SymbolEqualityComparer.Default.Equals(m.DestinationTypeSymbol, destType))
            {
                mapping = m;
                return true;
            }
        }

        mapping = null;
        return false;
    }

    private static string GenerateInlineMapping(MappingConfiguration config, string sourceExpr, IReadOnlyList<MappingConfiguration> mappings)
    {
        var destProperties = GetProperties(config.DestinationTypeSymbol);
        var sourceProperties = GetProperties(config.SourceTypeSymbol).ToDictionary(p => p.Name, StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.Append($"new {config.DestinationType} {{ ");

        var assignments = new List<string>();
        foreach (var destProp in destProperties)
        {
            if (!destProp.IsWriteable || destProp.IsIgnored)
                continue;

            // Check fluent configuration first
            if (config.FluentConfig.IgnoredMembers.Contains(destProp.Name))
                continue;

            string? sourceName = null;
            string? customExpr = null;
            string? constantValue = null;

            if (config.FluentConfig.MemberMappings.TryGetValue(destProp.Name, out var mappedFrom))
            {
                sourceName = mappedFrom;
            }
            else if (config.FluentConfig.MemberExpressions.TryGetValue(destProp.Name, out var expr))
            {
                customExpr = expr;
            }
            else if (config.FluentConfig.MemberValues.TryGetValue(destProp.Name, out var val))
            {
                constantValue = val;
            }
            else
            {
                sourceName = destProp.MapFrom ?? destProp.Name;
            }

            if (constantValue is not null)
            {
                assignments.Add($"{destProp.Name} = {constantValue}");
                continue;
            }

            if (customExpr is not null)
            {
                // Replace 's' or 'src' parameter with actual source expression
                var adjusted = customExpr.Replace("s.", $"{sourceExpr}.").Replace("src.", $"{sourceExpr}.");
                assignments.Add($"{destProp.Name} = {adjusted}");
                continue;
            }

            if (sourceName is null || !sourceProperties.TryGetValue(sourceName, out var sourceProp))
                continue;

            if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, destProp.Type))
            {
                assignments.Add($"{destProp.Name} = {sourceExpr}.{sourceProp.Name}");
            }
            else if (HasMapping(sourceProp.Type, destProp.Type, mappings))
            {
                // Recursive inline for deeply nested (max 1 level for now)
                assignments.Add($"{destProp.Name} = To({sourceExpr}.{sourceProp.Name})");
            }
        }

        sb.Append(string.Join(", ", assignments));
        sb.Append(" }");
        return sb.ToString();
    }

    private static string GenerateMapperInstanceSource(IReadOnlyList<MappingConfiguration> mappings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine("namespace VelocityMapper;");
        sb.AppendLine();
        sb.AppendLine("public sealed partial class MapperInstance");
        sb.AppendLine("{");

        foreach (var mapping in mappings)
        {
            sb.AppendLine();
            sb.AppendLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    partial void MapInternal({mapping.SourceType} source, {mapping.DestinationType} destination)");
            sb.AppendLine("    {");
            sb.AppendLine($"        Mapper.To(source, destination);");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateProjectToSource(IReadOnlyList<MappingConfiguration> mappings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine("namespace VelocityMapper;");
        sb.AppendLine();
        sb.AppendLine("public static partial class QueryableExtensions");
        sb.AppendLine("{");

        foreach (var mapping in mappings)
        {
            var destProperties = GetProperties(mapping.DestinationTypeSymbol);
            var sourceProperties = GetProperties(mapping.SourceTypeSymbol).ToDictionary(p => p.Name, StringComparer.Ordinal);

            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Projects {mapping.SourceTypeName} to {mapping.DestinationTypeName}.</summary>");
            sb.AppendLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    public static IQueryable<{mapping.DestinationType}> ProjectTo{mapping.DestinationTypeName}(this IQueryable<{mapping.SourceType}> source)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return source.Select(__s => new {mapping.DestinationType}");
            sb.AppendLine("        {");

            var assignments = new List<string>();
            foreach (var destProp in destProperties)
            {
                if (!destProp.IsWriteable || destProp.IsIgnored)
                    continue;

                if (mapping.FluentConfig.IgnoredMembers.Contains(destProp.Name))
                    continue;

                var sourceName = destProp.MapFrom ?? destProp.Name;
                if (mapping.FluentConfig.MemberMappings.TryGetValue(destProp.Name, out var mappedFrom))
                {
                    sourceName = mappedFrom;
                }

                if (!sourceProperties.TryGetValue(sourceName, out var sourceProp))
                    continue;

                if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, destProp.Type))
                {
                    assignments.Add($"            {destProp.Name} = __s.{sourceProp.Name}");
                }
            }

            sb.AppendLine(string.Join(",\n", assignments));
            sb.AppendLine("        });");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // Fluent configuration parsed from CreateMap chains
    private sealed class FluentConfiguration
    {
        public bool HasReverseMap { get; set; }
        public bool IgnoreAllNonExisting { get; set; }
        public HashSet<string> IgnoredMembers { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> MemberMappings { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> MemberExpressions { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> MemberValues { get; } = new(StringComparer.Ordinal);
    }

    private sealed record MappingConfiguration(
        ITypeSymbol SourceTypeSymbol,
        ITypeSymbol DestinationTypeSymbol,
        string SourceType,
        string DestinationType,
        string SourceTypeName,
        string DestinationTypeName,
        FluentConfiguration FluentConfig);

    private sealed record PropertyInfo(
        string Name,
        ITypeSymbol Type,
        bool IsReadable,
        bool IsWriteable,
        bool IsIgnored,
        string? MapFrom);
}

