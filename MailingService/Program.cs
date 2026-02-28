using MailingService.BackgroundServices;
using MailingService.Consumers;
using MailingService.Database;
using MailingService.Exceptions;
using MailingService.Models;
using MailingService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MailingService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        
        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
        
        builder.Services.AddSingleton<SmtpConnectionManager>();
        
        // Add services to the container.
        builder.Services.AddAuthorization();
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(5100); 
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services.AddSingleton<TemplateRenderer>(); 
        
        builder.Services.AddHostedService<ErrorQueueReprocessorService>();
        
        builder.Services.AddDbContext<MailingDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("MailingDb")
                ));
        
        var rmqConfig = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

        if (rmqConfig == null)
        {
            throw new ApplicationException("RabbitMqSettings are missing in appsettings.json");
        }
        
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<EmailConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rmqConfig.Host, "/", h =>
                {
                    h.Username(rmqConfig.Username);
                    h.Password(rmqConfig.Password);
                });
        
                cfg.PrefetchCount = 1;
        
                cfg.ReceiveEndpoint(rmqConfig.QueueName, e =>
                {
                    e.UseKillSwitch(options => options
                        .SetActivationThreshold(3)
                        .SetTripThreshold(0.15)
                        .SetExceptionFilter(filter =>
                        {
                            filter.Handle<RateLimitException>();
                        })
                        .SetRestartTimeout(TimeSpan.FromMinutes(rmqConfig.KillSwitchTimeoutMinutes)));
                    
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(10)));

                    e.ConfigureConsumer<EmailConsumer>(context);
                });
                
                // var errorQueueName = $"{rmqConfig.QueueName}_error";
                
                // cfg.ReceiveEndpoint(errorQueueName, e =>
                // {
                //     e.ConfigureConsumeTopology = false; 
                //     
                //     e.SetQueueArgument("x-message-ttl", (int)TimeSpan.FromMinutes(rmqConfig.KillSwitchTimeoutMinutes).TotalMilliseconds);
                //     
                //     e.SetQueueArgument("x-dead-letter-exchange", "");
                //     
                //     e.SetQueueArgument("x-dead-letter-routing-key", rmqConfig.QueueName);
                // });
            });
        });
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();
        
        app.MapControllers();

        app.Run();
    }
}