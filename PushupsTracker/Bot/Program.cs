using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PushupsTracker.Bot.Services;
using PushupsTracker.Core.Interfaces;
using PushupsTracker.Infrastructure.Repositories;
using Telegram.Bot;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
        logging.SetMinimumLevel(LogLevel.Information);
    });

builder.ConfigureServices((context, services) =>
{
    // Регистрация конфигурации
    services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));

    // Явная регистрация BotConfiguration для инъекции
    services.AddSingleton(provider =>
    {
        var config = provider.GetRequiredService<IConfiguration>();
        return config.GetSection("BotConfiguration").Get<BotConfiguration>()
               ?? throw new InvalidOperationException("BotConfiguration not found");
    });

    // Database
    services.AddScoped<IPushupsRepository>(_ =>
        new PushupsRepository(context.Configuration.GetConnectionString("MySQL")));

    // Telegram Bot
    services.AddHttpClient("telegram_bot_client")
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            var botConfig = sp.GetRequiredService<BotConfiguration>();
            return new TelegramBotClient(botConfig.BotToken, httpClient);
        });

    // Services
    services.AddScoped<PushupsBotService>();
    services.AddHostedService<BotBackgroundService>();
});

var host = builder.Build();
await host.RunAsync();

// Configuration class
public class BotConfiguration
{
    public string BotToken { get; set; } = string.Empty;
}