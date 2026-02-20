using Microsoft.CodeAnalysis;
using RazorLight;
using Shared.Events;

namespace MailingService.Services;

public class TemplateRenderer
{
    private readonly IRazorLightEngine _engine;

    public TemplateRenderer()
    {
        var sharedAssembly = typeof(NotificationEvent).Assembly;
        
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(sharedAssembly, "Shared.Templates")
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(string templateName, object model)
    {
        return await _engine.CompileRenderAsync(templateName, model);
    }
}