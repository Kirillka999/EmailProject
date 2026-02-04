using EmailProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmailProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MailingController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public MailingController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulk([FromBody] BulkEmailRequest request)
    {
        var mailingServiceUrl = "http://localhost:5100/api/delivery/receive";

        var client = _httpClientFactory.CreateClient();
        
        var response = await client.PostAsJsonAsync(mailingServiceUrl, request);

        if (response.IsSuccessStatusCode)
        {
            return Ok(new { Message = "Запрос успешно передан в сервис рассылок." });
        }

        return StatusCode((int)response.StatusCode, "Ошибка при передаче в сервис рассылок");
    }
}