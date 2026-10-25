using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Maps component types to the rules that check them. Built once, then read concurrently.</summary>
internal sealed class ComponentRuleRegistry {
    private readonly Dictionary<string, List<Action<ComponentContext>>> _handlers = new(StringComparer.Ordinal);
    private readonly List<Action<ComponentContext>> _anyHandlers = [];
    private readonly List<DiagnosticDescriptor> _descriptors = [];

    private ComponentRuleRegistry() {
    }

    public ImmutableArray<DiagnosticDescriptor> Descriptors { get; private set; }

    public static ComponentRuleRegistry Create(params Action<ComponentRuleRegistry>[] ruleSets) {
        var registry = new ComponentRuleRegistry();
        foreach (var register in ruleSets) {
            register(registry);
        }

        registry.Descriptors = [.. registry._descriptors];
        return registry;
    }

    public void Describe(params DiagnosticDescriptor[] descriptors) => _descriptors.AddRange(descriptors);

    /// <summary>Runs <paramref name="handler" /> for the component with this metadata name and any type derived from it.</summary>
    public void On(string componentMetadataName, Action<ComponentContext> handler) {
        if (!_handlers.TryGetValue(componentMetadataName, out var handlers)) {
            _handlers[componentMetadataName] = handlers = [];
        }

        handlers.Add(handler);
    }

    /// <summary>Runs <paramref name="handler" /> for every component frame.</summary>
    public void OnAny(Action<ComponentContext> handler) => _anyHandlers.Add(handler);

    public void Analyze(ComponentContext context) {
        foreach (var handler in _anyHandlers) {
            handler(context);
        }

        for (var type = context.Frame.Type.OriginalDefinition; type is not null; type = type.BaseType?.OriginalDefinition) {
            if (_handlers.TryGetValue(ComponentFrame.MetadataNameOf(type), out var handlers)) {
                foreach (var handler in handlers) {
                    handler(context);
                }
            }
        }
    }
}
