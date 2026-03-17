using MailingService.BackgroundServices;
using MailingService.Consumers;
using MailingService.Database;
using MailingService.Entities;
using MailingService.Exceptions;
using MailingService.Interfaces;
using MailingService.Razor;
using MailingService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MailingService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // IOptions<T>
        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
        builder.Services.Configure<ErrorEmailQueueServiceSettings>(builder.Configuration.GetSection("ErrorEmailQueueServiceSettings"));

        // Database
        builder.Services.AddDbContext<MailingDbContext>(opt => 
            opt.UseNpgsql(builder.Configuration.GetConnectionString("MailingDb")));

        // Services
        builder.Services.AddSingleton<TemplateRenderer>();
        builder.Services.AddSingleton<SmtpConnectionManager>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        
        // Background services
        builder.Services.AddHostedService<ErrorEmailQueueService>();
        
        // MassTransit + Rabbit
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<EmailConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()!;

                cfg.Host(rabbitSettings.Host, h =>
                {
                    h.Username(rabbitSettings.Username);
                    h.Password(rabbitSettings.Password);
                });

                cfg.PrefetchCount = 1;

                cfg.ReceiveEndpoint(rabbitSettings.QueueName, e =>
                {
                    e.UseKillSwitch(options => options
                        .SetActivationThreshold(3)
                        .SetTripThreshold(0.15)
                        .SetExceptionFilter(filter => { filter.Handle<RateLimitException>(); })
                        .SetRestartTimeout(TimeSpan.FromMinutes(rabbitSettings.KillSwitchTimeoutMinutes)));

                    e.UseMessageRetry(r =>
                    {
                        r.Interval(3, TimeSpan.FromSeconds(10));

                        r.Ignore<RateLimitException>();
                    });

                    e.ConfigureConsumer<EmailConsumer>(context);
                });
            });
        });
        
        var app = builder.Build();
        
        app.Run();
    }
}