using System.Text.Json;

namespace Shared.Events;

public class NotificationEvent
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
            
            var assembly = typeof(NotificationEvent).Assembly;
            string resourceName = $"Shared.Templates.{_templateName}";
            
            var resourceInfo = assembly.GetManifestResourceInfo(resourceName);

            if (resourceInfo is null)
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
            
            try
            {
                using var doc = JsonDocument.Parse(value);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Payload is not a valid JSON string.", nameof(Payload), ex);
            }

            _payload = value;
        }
    }
    
    public NotificationEvent() { }
    
    public NotificationEvent(string templateName, string modelTypeName, string payload)
    {
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
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"Payload cannot be deserialized into type '{targetType.Name}'. Error: {ex.Message}", 
                nameof(Payload), ex);
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
            throw new ArgumentNullException(nameof(payload));
        }
    }
}