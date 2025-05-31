using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PushupsTracker.Bot.Services;

public class BotBackgroundService : IHostedService
{
    private readonly ILogger<BotBackgroundService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly PushupsBotService _botService;

    public BotBackgroundService(
        ILogger<BotBackgroundService> logger,
        ITelegramBotClient botClient,
        PushupsBotService botService)
    {
        _logger = logger;
        _botClient = botClient;
        _botService = botService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _botClient.StartReceiving(
            updateHandler: _botService.HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: new ReceiverOptions
            {
                ThrowPendingUpdates = true,
                AllowedUpdates = Array.Empty<UpdateType>()
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Bot started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot stopped");
        return Task.CompletedTask;
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram bot polling error");
        return Task.CompletedTask;
    }
}