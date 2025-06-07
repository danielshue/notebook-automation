using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Custom YamlDotNet visitor to avoid quotes for the banner field.
/// </summary>
public class NoQuotesForBannerVisitor : ChainedObjectGraphVisitor
{
    public NoQuotesForBannerVisitor(IObjectGraphVisitor<IEmitter> nextVisitor) : base(nextVisitor) { }    /// <summary>
                                                                                                          /// Overrides the standard serialization behavior for the "banner" property to emit it without quotes.
                                                                                                          /// </summary>
    public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
    {
        // Special handling for banner fields to output without quotes, especially important for Obsidian wikilinks
        if (key.Name == "banner" && value.Value is string s)
        {
            // Emit as plain scalar (no quotes) and set non-canonical (not double quotes)
            // and plain style (no quotes at all) with SetNonScalar set to true
            context.Emit(new Scalar(null, null, s, ScalarStyle.Plain, true, false));
            return false; // Skip standard processing
        }

        // For all other fields, use standard processing
        return base.EnterMapping(key, value, context);
    }
}
