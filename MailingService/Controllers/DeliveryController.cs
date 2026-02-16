using MailingService.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace MailingService.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class DeliveryController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public DeliveryController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }
    
    [HttpPost]
    public async Task<IActionResult> ReceiveMailingRequest([FromBody] BulkEmailRequest request)
    {
        if (!request.Recipients.Any())
        {
            return BadRequest("Recipient list is empty.");
        }
        
        var tasks = new List<Task>();

        foreach (var recipient in request.Recipients)
        {
            string personalizedBody = request.EmailBodyTemplate;
            
            foreach (var kvp in recipient.Placeholders)
            {
                personalizedBody = personalizedBody.Replace($"{{{kvp.Key}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
            }
            
            var message = new EmailMessage
            {
                Recipient = recipient.Email,
                Subject = request.Subject,
                Body = personalizedBody
            };
            
            tasks.Add(_publishEndpoint.Publish(message));
        }

        await Task.WhenAll(tasks);

        return Ok(new { Message = $"Принято в обработку {request.Recipients.Count} писем." });
    }
}