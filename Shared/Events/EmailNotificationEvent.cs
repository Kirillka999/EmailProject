using System.Text.Json;

namespace Shared.Events;

public class EmailNotificationEvent
{
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    private string _templateName;
    private string _modelTypeName;
    private string _payload;

    public string TemplateName
    {
        get => _templateName;
        init
        {
            CheckIsNotNullOrEmpty(value);
        
            _templateName = value.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? value : $"{value}.cshtml";
        
            var assembly = typeof(EmailNotificationEvent).Assembly;
            
            bool templateExists = assembly.GetManifestResourceNames()
                .Any(name => name.EndsWith($".{_templateName}", StringComparison.OrdinalIgnoreCase));

            if (!templateExists)
            {
                throw new FileNotFoundException($"Template not found: {_templateName}");
            }
        }
    }

    public string ModelTypeName
    {
        get => _modelTypeName;
        init
        {
            CheckIsNotNullOrEmpty(value);
            
            Type? type = Type.GetType(value);
            if (type is null)
            {
                throw new TypeLoadException($"Type not found: {value}. Make sure to use AssemblyQualifiedName.");
            }

            _modelTypeName = value;
        }
    }
    
    public string Payload
    {
        get => _payload;
        init
        {
            CheckIsNotNullOrEmpty(value);

            _payload = value;
        }
    }
    
    public EmailNotificationEvent() { }
    
    public EmailNotificationEvent(string recipient, string subject, string templateName, string modelTypeName, string payload)
    {
        Email =  recipient;
        Subject = subject;
        TemplateName = templateName;
        ModelTypeName = modelTypeName;
        Payload = payload;
        
        ValidatePayloadMatchesModel();
    }
    
    private void ValidatePayloadMatchesModel()
    {
        Type targetType = Type.GetType(_modelTypeName)!;

        try
        {
            object? result = JsonSerializer.Deserialize(_payload, targetType);
            
            if (result is null)
            {
                throw new ArgumentException("JSON is valid, but resulted in null object.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Deserialization validation failed for type '{targetType.Name}'.", ex);
        }
    }
    
    private void CheckIsNotNullOrEmpty(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentNullException();
        }
    }
}