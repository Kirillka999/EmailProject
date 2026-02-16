using Microsoft.CodeAnalysis;
using RazorLight;
using Shared.Interfaces;
using Shared.Templates;

namespace MailingService.Services;

public class TemplateRenderer
{
    private readonly IRazorLightEngine _engine;

    public TemplateRenderer()
    {
        var sharedAssembly = typeof(IEmailTemplate).Assembly;

        var builder = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(sharedAssembly, "Shared.Templates")
            .UseMemoryCachingProvider();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Distinct()
            .ToList();

        foreach (var assembly in assemblies)
        {
            string name = assembly.GetName().Name ?? "";
            if (name.StartsWith("System.") || 
                name.StartsWith("Microsoft.Extensions") || 
                name == "mscorlib" || 
                name == "netstandard" ||
                assembly == sharedAssembly)
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