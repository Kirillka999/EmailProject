using System.Text.Json;

namespace Shared.Events;

public static class EmailEventFactory
{
    public static EmailNotificationEvent Create<TTemplate>(string email, TTemplate templateData)
        where TTemplate : class
    {
        if (String.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email));
        }

        if (templateData is null)
        {
            throw new ArgumentNullException(nameof(templateData));
        }

        Type type = typeof(TTemplate);
        var typeName = type.AssemblyQualifiedName!;
        var assembly = type.Assembly;
        string expectedResourceName = $"{type.FullName}.cshtml";
        
        bool templateExists = assembly.GetManifestResourceNames()
            .Any(name => name.Equals(expectedResourceName, StringComparison.OrdinalIgnoreCase));

        if (!templateExists)
        {
            throw new FileNotFoundException(
                $"CSHTML template missing! Expected to find embedded resource: {expectedResourceName}");
        }
        
        var payload = JsonSerializer.Serialize(templateData);
        
        return new EmailNotificationEvent(email, typeName, payload);
    }
}