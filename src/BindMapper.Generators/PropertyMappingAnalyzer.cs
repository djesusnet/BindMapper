using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace BindMapper.Generators;

/// <summary>
/// Analyzes and caches property mapping information for a type pair.
/// Resolves all mappings in a single pass to avoid repeated dictionary lookups.
/// This significantly improves performance for types with many properties.
/// </summary>
internal sealed class PropertyMappingAnalyzer
{
    private readonly IReadOnlyList<MappingConfiguration> _globalMappings;

    public PropertyMappingAnalyzer(IReadOnlyList<MappingConfiguration> globalMappings)
    {
        _globalMappings = globalMappings;
    }

    /// <summary>
    /// Analyzes how each destination property should be mapped.
    /// Returns categorized assignments suitable for code generation.
    /// Single pass: O(destProps.Count * sourceProps.Count) but with caching.
    /// OPTIMIZED: Manual dictionary construction to avoid LINQ allocation overhead.
    /// </summary>
    public PropertyMappingPlan AnalyzeMappings(
        MappingConfiguration config,
        IReadOnlyList<PropertyInfo>? destProperties = null,
        IReadOnlyList<PropertyInfo>? sourceProperties = null)
    {
        destProperties ??= SymbolAnalysisHelper.GetPublicProperties(config.DestinationTypeSymbol);
        sourceProperties ??= SymbolAnalysisHelper.GetPublicProperties(config.SourceTypeSymbol);

        // OPTIMIZED: Create source lookup for O(1) access - manual construction avoids LINQ overhead
        var sourceLookup = new Dictionary<string, PropertyInfo>(sourceProperties.Count, StringComparer.Ordinal);
        for (int i = 0; i < sourceProperties.Count; i++)
        {
            var prop = sourceProperties[i];
            sourceLookup[prop.Name] = prop;
        }

        var plan = new PropertyMappingPlan();

        foreach (var destProp in destProperties)
        {
            // Skip non-writable or ignored properties
            if (!destProp.IsWriteable || destProp.IsIgnored || 
                config.FluentConfig.IgnoredMembers.Contains(destProp.Name))
            {
                plan.Ignored.Add((destProp, PropertyMappingInfo.CreateIgnored(destProp)));
                continue;
            }

            // Check for constant value first (no property lookup needed)
            if (config.FluentConfig.MemberValues.TryGetValue(destProp.Name, out var constantValue))
            {
                var info = PropertyMappingInfo.CreateConstant(destProp, constantValue);
                plan.ByResolutionType[MappingResolutionType.Constant].Add((destProp, info));
                continue;
            }

            // Check for custom expression
            if (config.FluentConfig.MemberExpressions.TryGetValue(destProp.Name, out var customExpr))
            {
                var info = PropertyMappingInfo.CreateExpression(destProp, customExpr);
                plan.ByResolutionType[MappingResolutionType.Expression].Add((destProp, info));
                continue;
            }

            // Determine source property name
            var sourceName = destProp.MapFrom ?? destProp.Name;
            if (config.FluentConfig.MemberMappings.TryGetValue(destProp.Name, out var mappedFrom))
                sourceName = mappedFrom;

            // Try to find source property
            if (!sourceLookup.TryGetValue(sourceName, out var sourceProp))
            {
                // CRITICAL FIX: Validate null-safety
                // If destination is non-nullable but source is missing, error
                if (destProp.Type.NullableAnnotation == NullableAnnotation.NotAnnotated &&
                    !config.FluentConfig.IgnoreAllNonExisting &&
                    !config.FluentConfig.MemberValues.ContainsKey(destProp.Name))
                {
                    // Non-nullable destination with no source = will fail at runtime
                    plan.HasMissingSource = true;
                }
                
                plan.Ignored.Add((destProp, PropertyMappingInfo.CreateUnresolved(destProp)));
                continue;
            }

            // Check if types are directly compatible
            if (SymbolAnalysisHelper.AreTypesDirectlyAssignable(sourceProp.Type, destProp.Type))
            {
                var info = PropertyMappingInfo.CreateDirect(destProp, sourceProp);
                var resType = destProp.Type.IsReferenceType 
                    ? MappingResolutionType.Direct 
                    : MappingResolutionType.Direct;
                plan.ByResolutionType[resType].Add((destProp, info));
            }
            else if (SymbolAnalysisHelper.TryFindMapping(sourceProp.Type, destProp.Type, _globalMappings, out var nestedMapping) && nestedMapping != null)
            {
                // Found a nested mapping
                var info = PropertyMappingInfo.CreateNested(destProp, sourceProp, nestedMapping);
                plan.ByResolutionType[MappingResolutionType.Nested].Add((destProp, info));
            }
            else
            {
                // Type mismatch
                plan.HasTypeErrors = true;
                plan.Ignored.Add((destProp, PropertyMappingInfo.CreateUnresolved(destProp)));
            }
        }

        return plan;
    }
}

/// <summary>
/// Categorized mapping plan for a source-destination type pair.
/// Groups assignments by type to optimize code generation (value types first, etc).
/// </summary>
internal sealed class PropertyMappingPlan
{
    /// <summary>Assignments grouped by resolution type for optimal code ordering.</summary>
    public Dictionary<MappingResolutionType, List<(PropertyInfo Destination, PropertyMappingInfo Info)>> 
        ByResolutionType { get; } = new()
    {
        { MappingResolutionType.Direct, new() },
        { MappingResolutionType.Nested, new() },
        { MappingResolutionType.Constant, new() },
        { MappingResolutionType.Expression, new() },
        { MappingResolutionType.Ignored, new() },
        { MappingResolutionType.Unresolved, new() },
    };

    /// <summary>Properties that are ignored or couldn't be resolved.</summary>
    public List<(PropertyInfo Destination, PropertyMappingInfo Info)> Ignored { get; } = new();

    /// <summary>Whether any source properties were missing (for diagnostics).</summary>
    public bool HasMissingSource { get; set; }

    /// <summary>Whether any type mismatches occurred (for diagnostics).</summary>
    public bool HasTypeErrors { get; set; }

    /// <summary>Gets direct assignments (value-type first for cache locality).</summary>
    public IEnumerable<(PropertyInfo, PropertyMappingInfo)> GetDirectAssignmentsOrdered()
    {
        return ByResolutionType[MappingResolutionType.Direct]
            .OrderBy(x => x.Destination.Type.IsReferenceType ? 1 : 0)
            .ThenBy(x => SymbolAnalysisHelper.GetTypeWeightForOrdering(x.Destination.Type));
    }

    /// <summary>Gets all assignments in optimal order for code generation.</summary>
    public IEnumerable<(PropertyInfo, PropertyMappingInfo, MappingResolutionType)> GetAllAssignmentsOrdered()
    {
        // Phase 1: Value-type direct assignments (branchless)
        foreach (var (prop, info) in GetDirectAssignmentsOrdered().Where(x => !x.Item1.Type.IsReferenceType))
            yield return (prop, info, MappingResolutionType.Direct);

        // Phase 2: Constants (branchless)
        foreach (var (prop, info) in ByResolutionType[MappingResolutionType.Constant])
            yield return (prop, info, MappingResolutionType.Constant);

        // Phase 3: Custom expressions
        foreach (var (prop, info) in ByResolutionType[MappingResolutionType.Expression])
            yield return (prop, info, MappingResolutionType.Expression);

        // Phase 4: Reference-type direct assignments (may have branches)
        foreach (var (prop, info) in GetDirectAssignmentsOrdered().Where(x => x.Item1.Type.IsReferenceType))
            yield return (prop, info, MappingResolutionType.Direct);

        // Phase 5: Nested mappings (may have function calls + branches)
        foreach (var (prop, info) in ByResolutionType[MappingResolutionType.Nested])
            yield return (prop, info, MappingResolutionType.Nested);
    }
}
