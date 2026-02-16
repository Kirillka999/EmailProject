using RazorLight;
using Shared.Templates;

namespace MailingService.Services;

public class TemplateRenderer
{
    private readonly IRazorLightEngine _engine;
    
    public TemplateRenderer()
    {
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(IEmailTemplate).Assembly, "Shared.Templates")
            .UseMemoryCachingProvider()
            .Build();
    }
    
    public async Task<string> RenderAsync<T>(T model)
    {
        string templateKey = $"{typeof(T).Name}.cshtml";
        
        return await _engine.CompileRenderAsync(templateKey, model);
    }
}