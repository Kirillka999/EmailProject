using MailingService.Consumers;
using MailingService.Models;
using MailingService.Services;
using MassTransit;
using Shared.Templates;

namespace MailingService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        
        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
        
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
        
        builder.Services.AddMassTransit(x =>
        {
            var sharedAssembly = typeof(IEmailTemplate).Assembly;
            var templateTypes = sharedAssembly.GetTypes()
                .Where(t => typeof(IEmailTemplate).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
            
            foreach (var templateType in templateTypes)
            {
                var consumerType = typeof(EmailConsumer<>).MakeGenericType(templateType);
        
                x.AddConsumer(consumerType);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                cfg.PrefetchCount = 1;
                
                cfg.ConfigureEndpoints(context);
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