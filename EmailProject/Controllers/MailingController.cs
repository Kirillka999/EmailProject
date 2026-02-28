using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using Shared.Templates;

namespace EmailProject.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class MailingController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MailingController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> SendTestEmail()
    {
        var templateData = new WelcomeTemplate
        {
            UserName = "Кирилл",
            Test = "Hello test"
        };
        
        var notification = new NotificationEvent(
            nameof(WelcomeTemplate),
            typeof(WelcomeTemplate).AssemblyQualifiedName!, 
            JsonSerializer.Serialize(templateData))
        {
            Email = "kirill93549@gmail.com",
            Subject = "Добро пожаловать в систему!",
        };
        
        await _publishEndpoint.Publish(notification);

        return Ok(new { Message = "Событие отправлено в очередь" });
    }
}