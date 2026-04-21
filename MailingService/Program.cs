using MailingService.Consumers;
using MailingService.Database;
using MailingService.Entities;
using MailingService.Exceptions;
using MailingService.Interfaces;
using MailingService.RateLimiting;
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
        builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimitSettings"));
        
        // Database
        builder.Services.AddDbContext<MailingDbContext>(opt => 
            opt.UseNpgsql(builder.Configuration.GetConnectionString("MailingDb")));

        // Services
        builder.Services.AddSingleton<TemplateRenderer>();
        builder.Services.AddSingleton<SmtpConnectionManager>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        
        builder.Services.AddSingleton<IRateLimitStateManager, FileRateLimitStateManager>();
        builder.Services.AddSingleton<EmailQueueManager>();

        // Email Consumer configuration
        builder.Services.AddSingleton<Action<IBusRegistrationContext, IReceiveEndpointConfigurator>>(_ => 
            (context, cfg) =>
        {
            cfg.PrefetchCount = 1;
            cfg.ConcurrentMessageLimit = 1;
        
            cfg.UseConsumeFilter(typeof(GoogleRateLimitFilter<>), context);
        
            cfg.UseMessageRetry(r =>
            {
                r.Interval(3, TimeSpan.FromSeconds(10));
                r.Ignore<RateLimitException>(); 
            });
        
            cfg.ConfigureConsumer<EmailConsumer>(context);
        });
        
        // MassTransit + Rabbit
        builder.Services.AddMassTransit(x =>
        {
            x.AddDelayedMessageScheduler();

            x.AddConsumer<EmailConsumer>();

            x.UsingRabbitMq((_, cfg) =>
            {
                var rabbitSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()!;

                cfg.Host(rabbitSettings.Host, h =>
                {
                    h.Username(rabbitSettings.Username);
                    h.Password(rabbitSettings.Password);
                });
                
                cfg.UseDelayedMessageScheduler();
            });
        });
        
        builder.Services.AddHostedService<StartupEmailQueueHostedService>(); 
        
        var app = builder.Build();
        
        app.Run();
    }
}