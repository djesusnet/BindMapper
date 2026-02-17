using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BindMapper.Generators;

/// <summary>
/// Source Generator that builds the mapper API at compile time.
/// 
/// ARCHITECTURE:
/// 1. Syntax Phase: Find methods decorated with [MapperConfiguration]
/// 2. Semantic Phase: Analyze CreateMap calls and extract configurations
/// 3. Validation Phase: Check for duplicates and incompatibilities
/// 4. Generation Phase: Emit optimized mapping code
/// 
/// Performance characteristics:
/// - O(n) complexity where n = number of CreateMap calls
/// - Minimal allocations: Uses SyntaxWalker instead of LINQ.DescendantNodes()
/// - Deterministic output: All results sorted by type name
/// - Incremental: Only recomputes when syntax tree changes
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MapperGenerator : IIncrementalGenerator
{
    private const string MapperConfigurationAttributeName = "BindMapper.MapperConfigurationAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // OPTIMIZED PHASE 1: Use ForAttributeWithMetadataName for better performance (.NET 7+)
        // This API is specifically designed for attribute-based generators and provides:
        // - Faster semantic analysis (only analyzes nodes with the target attribute)
        // - Better incremental caching (fine-grained invalidation)
        // - Lower memory usage (doesn't allocate intermediate collections)
        var configMethodsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: MapperConfigurationAttributeName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => (MethodDeclarationSyntax)ctx.TargetNode)
            .Collect();

        // OPTIMIZED PHASE 2: Combine with compilation and extract mappings with fine-grained caching
        var mappingsProvider = context.CompilationProvider
            .Combine(configMethodsProvider)
            .SelectMany(static (source, _) => CollectAllMappings(source.Left, source.Right));

        // OPTIMIZED PHASE 3: Generate mapper code with deterministic output
        context.RegisterSourceOutput(
            mappingsProvider.Collect(),
            static (spc, mappings) => GenerateMapperFile(spc, mappings));
    }

    /// <summary>
    /// Collects all mappings from configuration methods.
    /// Uses SelectMany to avoid Collect() overhead on large projects.
    /// </summary>
    private static IEnumerable<MappingConfiguration> CollectAllMappings(
        Compilation compilation,
        ImmutableArray<MethodDeclarationSyntax> configMethods)
    {
        // Create analyzer for mapping configuration extraction
        var analyzer = new MappingConfigurationAnalyzer(compilation);

        foreach (var methodSyntax in configMethods)
        {
            if (methodSyntax is null)
                continue;

            var mappings = analyzer.AnalyzeMethod(methodSyntax);
            foreach (var mapping in mappings)
            {
                yield return mapping;
            }
        }
    }

    /// <summary>
    /// Generates the Mapper.g.cs file with all mapping methods.
    /// CRITICAL: Deduplicates and sorts mappings deterministically to ensure reproducible builds.
    /// OPTIMIZED: Pre-calculates StringBuilder capacity to avoid reallocations.
    /// </summary>
    private static void GenerateMapperFile(
        SourceProductionContext context,
        ImmutableArray<MappingConfiguration> mappings)
    {
        if (mappings.Length == 0)
            return;

        // CRITICAL FIX: Deduplicate mappings first (incremental generator may produce duplicates)
        var distinctMappings = new Dictionary<string, MappingConfiguration>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            var key = $"{mapping.SourceTypeFullName}|{mapping.DestinationTypeFullName}";
            if (!distinctMappings.ContainsKey(key))
            {
                distinctMappings[key] = mapping;
            }
        }

        // CRITICAL FIX: Sort deterministically for reproducible builds
        // Different build orders must produce identical generated code
        var sortedMappings = distinctMappings.Values
            .OrderBy(m => m.SourceTypeFullName, StringComparer.Ordinal)
            .ThenBy(m => m.DestinationTypeFullName, StringComparer.Ordinal)
            .ToList();

        var generator = new MapperCodeGenerator();
        var sourceText = generator.GenerateMapperSource(sortedMappings);

        if (sourceText.Length > 0)
        {
            context.AddSource("Mapper.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        }
    }
}
