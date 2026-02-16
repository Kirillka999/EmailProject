namespace Shared.Templates;

public class WelcomeTemplate : IEmailTemplate
{
    public string UserName { get; set; } = string.Empty;
    public string Test { get; set; } = string.Empty;
}