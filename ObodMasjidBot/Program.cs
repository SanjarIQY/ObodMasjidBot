using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static ITelegramBotClient _botClient;
    private static ReceiverOptions _receiverOptions;
    private static List<HasharEvent> _hasharEvents = new List<HasharEvent>();
    private static Dictionary<long, List<HasharParticipation>> _userStats = new Dictionary<long, List<HasharParticipation>>();
    private static List<long> _subscriptedUsers = new List<long>();

    static async Task Main()
    {
        _botClient = new TelegramBotClient("7559448626:AAGJa6X8d4XHoruruEgvcyEbmSKC4-TyW0c");
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            DropPendingUpdates = false
        };

        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"{me.FirstName} ishga tushdi!");

        // Simulate a hashar announcement (in real use, this could be scheduled)
        _hasharEvents.Add(new HasharEvent { Date = DateTime.Now.AddDays(1), Masjid = "Al-Noor Masjid", Time = "10:00 AM" });
        await AnnounceHashar();

        await Task.Delay(-1);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        var message = update.Message;
                        var chat = message.Chat;
                        var user = message.From;

                        if (message.Type == MessageType.Text)
                        {
                            if (message.Text == "/start")
                            {
                                _subscriptedUsers.Add(user.Id);
                                await botClient.SendTextMessageAsync(chat.Id, "Obod masjid botiga hush kelibsiz!\nHasharlaringiz statistikasini ko'rish uchun /stats ni kiriting!");

                                return;
                            }
                            else if (message.Text == "/stats")
                            {
                                await SendUserStats(chat.Id, user.Id);
                                return;
                            }
                        }
                        return;
                    }

                case UpdateType.CallbackQuery:
                    {
                        var callbackQuery = update.CallbackQuery;
                        var chat = callbackQuery.Message.Chat;
                        var userId = callbackQuery.From.Id;

                        if (callbackQuery.Data.StartsWith("rsvp_"))
                        {
                            var parts = callbackQuery.Data.Split('_');
                            var response = parts[1];
                            var hasharDate = parts[2];

                            if (response == "yes")
                            {
                                await botClient.SendTextMessageAsync(chat.Id, $"Sizni {hasharDate} kuni hasharda kutib qolamiz!");
                            }
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                            return;
                        }
                        else if (callbackQuery.Data.StartsWith("attended_"))
                        {
                            var parts = callbackQuery.Data.Split('_');
                            var attended = parts[1] == "yes";
                            var hasharDate = parts[2];

                            if (!_userStats.ContainsKey(userId))
                                _userStats[userId] = new List<HasharParticipation>();

                            _userStats[userId].Add(new HasharParticipation { Date = DateTime.ParseExact(hasharDate, "yyyyMMdd", null), Attended = attended });

                            await botClient.SendTextMessageAsync(chat.Id, $"Logged: You {(attended ? "attended" : "missed")} the hashar on {hasharDate}.");
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                            return;
                        }
                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static async Task AnnounceHashar()
    {
        var hashar = _hasharEvents.First();
        var message = $"E'lon! {hashar.Date:MMMM d, yyyy} sanasida soat {hashar.Time} da {hashar.Masjid} masjidida hashar uyushtirilmoqda, kelasizmi?"
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Ha", $"rsvp_yes_{hashar.Date:yyyyMMdd}"),
            InlineKeyboardButton.WithCallbackData("Yo'q", $"rsvp_no_{hashar.Date:yyyyMMdd}")
        });

        // Send to channel or individual chats (replace with your channel ID)

        foreach(var userId in _subscriptedUsers)
        {
            await _botClient.SendTextMessageAsync(
                chatId: userId, 
                message,
                replyMarkup: keyboard);
        }

        // Simulate post-hashar check (in reality, use a timer)
        await Task.Delay(5000); // Fake delay for demo
        await CheckParticipation(hashar);
    }

    private static async Task CheckParticipation(HasharEvent hashar)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Ha", $"attended_yes_{hashar.Date:yyyyMMdd}"),
            InlineKeyboardButton.WithCallbackData("Yo'q", $"attended_no_{hashar.Date:yyyyMMdd}")
        });

        foreach (var userId in _subscriptedUsers)
        {
            await _botClient.SendTextMessageAsync(
                chatId: userId,
                $"{hashar.Masjid} masjidida bo'lib o'tgan hasharga keldingizmi?", 
                replyMarkup: keyboard);
        }

    }

    private static async Task SendUserStats(long chatId, long userId)
    {
        if (!_userStats.ContainsKey(userId) || _userStats[userId].Count == 0)
        {
            await _botClient.SendTextMessageAsync(chatId, "You haven’t participated in any hashars yet!");
            return;
        }

        var stats = _userStats[userId];
        var summary = $"Your Hashar Stats:\n{string.Join("\n", stats.Select(s => $"{s.Date:MMMM d}: {(s.Attended ? "✅" : "❌")}"))}\nTotal: {stats.Count(s => s.Attended)}/{stats.Count} attended";
        await _botClient.SendTextMessageAsync(chatId, summary);
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };
        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}

class HasharEvent
{
    public DateTime Date { get; set; }
    public string Time { get; set; }
    public string Masjid { get; set; }
}

class HasharParticipation
{
    public DateTime Date { get; set; }
    public bool Attended { get; set; }
}