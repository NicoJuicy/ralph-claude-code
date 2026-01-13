using System.Reflection;
using System.Text.RegularExpressions;

namespace Ralph.Cli.Services;

/// <summary>
/// Information about an agent template
/// </summary>
public class AgentTemplateInfo
{
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public required string Description { get; init; }
    public required string ResourceName { get; init; }
}

/// <summary>
/// Service for loading templates from embedded resources
/// </summary>
public static class TemplateService
{
    private static readonly Assembly Assembly = typeof(TemplateService).Assembly;

    /// <summary>
    /// Gets all available agent templates including the default one
    /// </summary>
    public static IReadOnlyList<AgentTemplateInfo> GetAgentTemplates()
    {
        var templates = new List<AgentTemplateInfo>();
        var allResources = Assembly.GetManifestResourceNames();

        // Add default template first
        var defaultResourceName = allResources.FirstOrDefault(r => r.EndsWith(".Templates.AGENT.md"));
        if (defaultResourceName != null)
        {
            var defaultContent = GetResourceContent(defaultResourceName);
            if (defaultContent != null)
            {
                templates.Add(new AgentTemplateInfo
                {
                    Name = "Default (Generic)",
                    FileName = "AGENT.md",
                    Description = ExtractDescription(defaultContent) ?? "Generic agent template for any project type",
                    ResourceName = defaultResourceName
                });
            }
        }

        // Add specialized agent templates
        var resourceNames = allResources
            .Where(n => n.Contains(".Templates.Agents.") && n.EndsWith(".md"))
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            var content = GetResourceContent(resourceName);
            if (content == null) continue;

            // Extract file name from resource name (e.g., "yolo.Templates.Agents.dotnet-cli.md" -> "dotnet-cli.md")
            var agentsIndex = resourceName.IndexOf(".Agents.");
            if (agentsIndex < 0) continue;

            var fileName = resourceName[(agentsIndex + ".Agents.".Length)..];
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var name = string.Join(" ", nameWithoutExt
                .Split('-')
                .Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));

            templates.Add(new AgentTemplateInfo
            {
                Name = name,
                FileName = fileName,
                Description = ExtractDescription(content) ?? "Specialized agent template",
                ResourceName = resourceName
            });
        }

        return templates;
    }

    /// <summary>
    /// Gets the content of a template by its template path (e.g., "PROMPT.md")
    /// </summary>
    public static string? GetTemplateContent(string templatePath)
    {
        var allResources = Assembly.GetManifestResourceNames();
        var normalizedPath = templatePath.Replace("/", ".").Replace("\\", ".");

        var matchingResource = allResources.FirstOrDefault(r =>
            r.EndsWith($".Templates.{normalizedPath}") ||
            r.EndsWith($".{normalizedPath}"));

        return matchingResource != null ? GetResourceContent(matchingResource) : null;
    }

    /// <summary>
    /// Gets the content of an agent template by its resource name
    /// </summary>
    public static string? GetAgentTemplateContent(string resourceName)
    {
        return GetResourceContent(resourceName);
    }

    private static string? GetResourceContent(string resourceName)
    {
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string? ExtractDescription(string content)
    {
        // Look for ## Description section and extract its content
        var match = Regex.Match(content, @"##\s*Description\s*\n(.+?)(?=\n#|\n\n#|\z)", RegexOptions.Singleline);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return null;
    }
}
