using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using Shared.Templates.Test;
using Shared.Templates.Welcome;

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

        var emailEvent = EmailEventFactory.Create("kirill93549@gmail.com", templateData);
        
        await _publishEndpoint.Publish(emailEvent);

        return Ok(new { Message = "Событие отправлено в очередь" });
    }
    
    // =========================================================
    // БЛОК ТЕСТИРОВАНИЯ (НАЧАЛО)
    // =========================================================
    
    [HttpPost]
    public async Task<IActionResult> TriggerRateLimitError()
    {
        var templateData = new RateLimitTestTemplate();
        
        var emailEvent = EmailEventFactory.Create("kirill93549@gmail.com", templateData);
        
        await _publishEndpoint.Publish(emailEvent);
        
        return Ok(new { Message = "Отправлено письмо для симуляции 429 Rate Limit" });
    }

    [HttpPost]
    public async Task<IActionResult> TriggerSocketError()
    {
        var templateData = new SocketErrorTestTemplate();
        
        var emailEvent = EmailEventFactory.Create("kirill93549@gmail.com", templateData);
        
        await _publishEndpoint.Publish(emailEvent);
        
        return Ok(new { Message = "Отправлено письмо для симуляции Socket Exception" });
    }
    
    // =========================================================
    // БЛОК ТЕСТИРОВАНИЯ (КОНЕЦ)
    // =========================================================
}