using System.Reflection;
using Microsoft.CodeAnalysis;
using RazorLight;
using Shared.Templates;

namespace MailingService.Services;

public class TemplateRenderer
{
    private readonly IRazorLightEngine _engine;

    public TemplateRenderer()
    {
        var sharedAssembly = typeof(IEmailTemplate).Assembly;
        var entryAssembly = Assembly.GetEntryAssembly();

        var builder = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(sharedAssembly, "Shared.Templates")
            .UseMemoryCachingProvider();
        
        builder.AddMetadataReferences(MetadataReference.CreateFromFile(sharedAssembly.Location));
        
        if (entryAssembly != null)
        {
            builder.AddMetadataReferences(MetadataReference.CreateFromFile(entryAssembly.Location));
        }

        var trustedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .GroupBy(a => a.GetName().Name)
            .Select(g => g.First())
            .ToList();

        foreach (var assembly in trustedAssemblies)
        {
            string name = assembly.GetName().Name ?? "";
            if (name.StartsWith("System.") || name.StartsWith("Microsoft.Extensions") || name == "mscorlib" || name == "netstandard")
            {
                builder.AddMetadataReferences(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        _engine = builder.Build();
    }

    public async Task<string> RenderAsync<T>(T model)
    {
        string templateKey = $"{typeof(T).Name}.cshtml";
        return await _engine.CompileRenderAsync(templateKey, model);
    }
}