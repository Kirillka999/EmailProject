using RazorLight;
using Shared.Events;

namespace MailingService.Razor;

public class TemplateRenderer
{
    private readonly IRazorLightEngine _engine;
    
    public TemplateRenderer()
    {
        var sharedAssembly = typeof(EmailNotificationEvent).Assembly;
        
        string rootNamespace = $"{sharedAssembly.GetName().Name}.Templates"; 
    
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(sharedAssembly, rootNamespace)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(string templateName, object model)
    {
        return await _engine.CompileRenderAsync(templateName, model);
    }
}