﻿using Microsoft.Extensions.Logging;
using PushupsTracker.Core.Interfaces;
using PushupsTracker.Core.Models;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PushupsTracker.Bot.Services;

public class PushupsBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IPushupsRepository _repository;
    private readonly ILogger<PushupsBotService> _logger;

    public PushupsBotService(
        ITelegramBotClient botClient,
        IPushupsRepository repository,
        ILogger<PushupsBotService> logger)
    {
        _botClient = botClient;
        _repository = repository;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message)
                return;

            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var userName = message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}";

            switch (message.Text)
            {
                case "/start":
                    await ShowMainMenu(chatId);
                    return;

                case "Добавить отжимания":
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введите количество отжиманий:",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                    return;

                case "Статистика за сегодня":
                    await ShowTodayStatistics(chatId);
                    return;

                case "Статистика за все время":
                    await ShowAllTimeStatistics(chatId);
                    return;
            }

            if (int.TryParse(message.Text, out var count) && count > 0)
            {
                await HandlePushupsCountInput(chatId, userId, userName, count);
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пожалуйста, используйте кнопки меню.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }
    private async Task HandlePushupsCountInput(long chatId, long userId, string userName, int count)
    {
        try
        {
            // Добавляем запись
            await _repository.AddRecord(userId, userName, count);

            // Получаем статистику
            var userTotal = await _repository.GetTodayUserPushupsCount(userId);
            var todayStats = await _repository.GetTodayStatistics();

            // Формируем сообщение
            var sb = new StringBuilder();
            sb.AppendLine($"✅ Добавлено {count} отжиманий!");
            sb.AppendLine($"📊 Твой прогресс: {userTotal}/100");

            if (userTotal < 100)
            {
                var achievers = todayStats
                    .Where(s => s.UserId != userId && s.TotalCount >= 100)
                    .ToList();

                if (achievers.Any())
                {
                    var randomAchiever = achievers[new Random().Next(achievers.Count)];
                    sb.AppendLine($"\n🏆 {randomAchiever.UserName} уже выполнил норму!");
                    sb.AppendLine("А ты, жиробасик, уже скоро?");
                }
                else
                {
                    sb.AppendLine("\nНикто еще не выполнил норму сегодня. Может, ты будешь первым?");
                }
            }
            else
            {
                sb.AppendLine("\n🎉 Ты выполнил дневную норму! Так держать!");
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: sb.ToString(),
                replyMarkup: new ReplyKeyboardRemove());

            await ShowMainMenu(chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling pushups input");
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при обработке запроса. Попробуйте позже.");
        }
    }

    private async Task ShowMainMenu(long chatId)
    {
        var replyMarkup = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Добавить отжимания" },
            new KeyboardButton[] { "Статистика за сегодня" },
            new KeyboardButton[] { "Статистика за все время" }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите действие:",
            replyMarkup: replyMarkup);
    }

    private async Task ShowTodayStatistics(long chatId)
    {
        var stats = await _repository.GetTodayStatistics();
        var message = FormatStatisticsTable("Статистика за сегодня:", stats);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
    }

    private async Task ShowAllTimeStatistics(long chatId)
    {
        var stats = await _repository.GetAllTimeStatistics();
        var message = FormatDailyStatisticsTable("Статистика за все время:", stats);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
    }

    private string FormatStatisticsTable(string title, IEnumerable<UserStatistic> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{title}</b>");
        sb.AppendLine();
        sb.AppendLine("<pre>");
        sb.AppendLine("┌────────────────────┬───────────────┐");
        sb.AppendLine("│ Имя                │ Отжимания     │");
        sb.AppendLine("├────────────────────┼───────────────┤");

        foreach (var stat in stats)
        {
            sb.AppendLine($"│ {stat.UserName,-18} │ {stat.TotalCount,13} │");
        }

        sb.AppendLine("└────────────────────┴───────────────┘");
        sb.AppendLine("</pre>");

        return sb.ToString();
    }

    private string FormatDailyStatisticsTable(string title, IEnumerable<DailyStatistic> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{title}</b>");
        sb.AppendLine();
        sb.AppendLine("<pre>");
        sb.AppendLine("┌────────────┬────────────────────┬───────────────┐");
        sb.AppendLine("│ Дата       │ Имя                │ Отжимания     │");
        sb.AppendLine("├────────────┼────────────────────┼───────────────┤");

        foreach (var stat in stats)
        {
            sb.AppendLine($"│ {stat.Date:dd.MM.yyyy} │ {stat.UserName,-18} │ {stat.TotalCount,13} │");
        }

        sb.AppendLine("└────────────┴────────────────────┴───────────────┘");
        sb.AppendLine("</pre>");

        return sb.ToString();
    }
}