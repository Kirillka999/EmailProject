using System.Dynamic;
using RazorLight;
using Shared.Events;

namespace MailingService.Razor;

public class TemplateRenderer
{
    private readonly IRazorLightEngine _engine;
    private readonly string _assemblyName;
    private const string SubjectKey = "Subject";
    
    public TemplateRenderer()
    {
        var sharedAssembly = typeof(EmailNotificationEvent).Assembly;
        _assemblyName = sharedAssembly.GetName().Name!;
    
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(sharedAssembly, _assemblyName)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<(string HtmlBody, string? Subject)> RenderAsync(Type modelType, object model)
    {
        string templateKey = modelType.FullName!.Replace($"{_assemblyName}.", "") + ".cshtml";
        
        var template = await _engine.CompileTemplateAsync(templateKey);
        
        var viewBag = new ExpandoObject();
        
        string htmlBody = await _engine.RenderTemplateAsync(template, model, viewBag);
        
        var viewBagDict = (IDictionary<string, object?>)viewBag;
        
        viewBagDict.TryGetValue(SubjectKey, out object? subjectObj);

        return (htmlBody, subjectObj as string);
    }
}