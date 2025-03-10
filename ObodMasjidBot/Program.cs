using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using ObodMasjidBot.Data;

class Program
{
    private static ITelegramBotClient _botClient;
    private static ReceiverOptions _receiverOptions;
    private static List<HasharEvent> _hasharEvents = new List<HasharEvent>();
    private static Dictionary<long, List<HasharParticipation>> _userStats 
        = new Dictionary<long, List<HasharParticipation>>();
    private static List<long> _subscribedUsers = new List<long>();
    private static Dictionary<long, UserState> _userStates = new Dictionary<long, UserState>();
    private static List<Masjid> _masjids = new List<Masjid>();
    private static IConfiguration _configuration;
    private static List<string> _adminUsernames;

    static async Task Main()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var botToken = _configuration["BotSettings:BotToken"];
        _adminUsernames = _configuration.GetSection("BotSettings:AdminUsernames").Get<List<string>>();

        _botClient = new TelegramBotClient(botToken);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            DropPendingUpdates = false
        };

        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"{me.FirstName} ishga tushdi!");

        await Task.Delay(-1);
    }

    private static ReplyKeyboardMarkup GetPersistentKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📊 Mening Statistikam"), new KeyboardButton("📅 Bo'lajak Hasharlar") },
            new[] { new KeyboardButton("ℹ️ Yordam"), new KeyboardButton("🔄 Yangilash") }
        })
        {
            ResizeKeyboard = true 
        };
    }

    private static ReplyKeyboardMarkup GetAdminKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Hashar qo'shish"), new KeyboardButton("Masjid qo'shish") },
            new[] { new KeyboardButton(
                "Ayol hasharchilarni ko'rish"), 
                new KeyboardButton("Erkak hasharchilarni ko'rish"),
                new KeyboardButton("Ortga qaytish") 
            }
        })
        {
            ResizeKeyboard = true
        };

    }

    private static ReplyKeyboardMarkup GetGenderKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Erkak 👨"), new KeyboardButton("Ayol 👩") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }
    
    private static InlineKeyboardMarkup GetMasjidSearchKeyboard()
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Search Masjids", "search_masjid") },
            new[] { InlineKeyboardButton.WithCallbackData("Ortga qaytish", "back") }
        });
        return keyboard;
    }
    
    private static ReplyKeyboardMarkup GetMasjidSelectionKeyboard()
    {
        var keyboardButtons = _masjids.Select(m => new[] { new KeyboardButton(m.Name) }).ToList();
        
        // Add a back button
        keyboardButtons.Add(new[] { new KeyboardButton("Ortga qaytish") });
        
        return new ReplyKeyboardMarkup(keyboardButtons)
        {
            ResizeKeyboard = true
        };
    }

    private static ReplyKeyboardMarkup GetPhoneNumberKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { KeyboardButton.WithRequestContact("Telefon raqamimni yuborish 📱") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }
    
    private static InlineKeyboardMarkup GetMasjidResultsKeyboard(string searchTerm = "")
    {
        var filteredMasjids = string.IsNullOrEmpty(searchTerm) 
            ? _masjids 
            : _masjids.Where(m => m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        var keyboardButtons = filteredMasjids
            .Select(m => new[] 
            { 
                InlineKeyboardButton.WithCallbackData(m.Name, $"select_masjid_{m.Id}") 
            })
            .ToList();

        keyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("Ortga qaytish", "back") });

        return new InlineKeyboardMarkup(keyboardButtons);
    }

    private static async Task UpdateHandler(
        ITelegramBotClient botClient, 
        Update update, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        var message = update.Message;
                        if (message == null) return;

                        var chat = message.Chat;
                        var user = message.From;

                        if (!_userStates.ContainsKey(user.Id))
                        {
                            _userStates[user.Id] = new UserState();
                        }

                        var userState = _userStates[user.Id];

                        if (message.Contact != null && userState.RegistrationState == RegistrationState.AwaitingPhoneNumber)
                        {
                            userState.PhoneNumber = message.Contact.PhoneNumber;
                            userState.RegistrationState = RegistrationState.Complete;

                            if (!_subscribedUsers.Contains(user.Id))
                            {
                                _subscribedUsers.Add(user.Id);
                            }

                            await botClient.SendTextMessageAsync(
                                chatId: chat.Id,
                                text: $"Tabriklaymiz! Ro'yxatdan muvaffaqiyatli o'tdingiz." +
                                      $"\n\nSizning ma'lumotlaringiz:" +
                                      $"\nJins: {userState.Gender}\nTelefon: {userState.PhoneNumber}",
                                replyMarkup: GetPersistentKeyboard()
                            );

                            return;
                        }

                        if (message.Type == MessageType.Text)
                        {
                            if (_adminUsernames.Contains(user.Username) &&
                                userState.AddingMasjidState == AddingMasjidStateEnum.AddingName)
                            {
                                
                            }
                                
                            if (_adminUsernames.Contains(user.Username) &&
                                userState.AddingMasjidState == AddingMasjidStateEnum.AddingLocation)
                            {
                                
                            }
                            
                            if (_adminUsernames.Contains(user.Username) &&
                                userState.AddingMasjidState == AddingMasjidStateEnum.AddingPhoto)
                            {
                                
                            }
                            
                            if (_adminUsernames.Contains(user.Username) &&
                                userState.AddingMasjidState == AddingMasjidStateEnum.AddingNumber)
                            {
                                
                            }
                            
                            if (userState.IsAddingHashar && _adminUsernames.Contains(user.Username) && 
                                message.Text != "Hashar qo'shish" && message.Text != "Ortga qaytish")
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: $"Qidiruv natijalari: '{message.Text}'",
                                    replyMarkup: GetMasjidResultsKeyboard(message.Text)
                                );
                                userState.IsAddingHashar = false;
                                return;
                            }
                            if (userState.RegistrationState == RegistrationState.AwaitingGender)
                            {
                                if (message.Text == "Erkak 👨" || message.Text == "Ayol 👩")
                                {
                                    userState.Gender = message.Text == "Erkak 👨" ? "Erkak" : "Ayol";
                                    userState.RegistrationState = RegistrationState.AwaitingPhoneNumber;

                                    await botClient.SendTextMessageAsync(
                                        chatId: chat.Id,
                                        text: "Iltimos, telefon raqamingizni yuboring:",
                                        replyMarkup: GetPhoneNumberKeyboard()
                                    );
                                    
                                    return;
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId: chat.Id,
                                        text: "Iltimos, jinsingizi tanlang:",
                                        replyMarkup: GetGenderKeyboard()
                                    );
                                    return;
                                }
                            }

                            if (message.Text == "/start")
                            {
                                bool isNewUser = !_subscribedUsers.Contains(user.Id) 
                                                 && userState.RegistrationState != RegistrationState.Complete;

                                if (isNewUser)
                                {
                                    await SendWelcomeMessage(chat.Id, user.FirstName);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId: chat.Id,
                                        text: $"Assalomu alaykum, {user.FirstName}! " +
                                              $"Obod masjid botiga qayta xush kelibsiz!" +
                                              $"\nQuyidagi tugmalardan foydalanishingiz mumkin:",
                                        replyMarkup: GetPersistentKeyboard()
                                    );
                                }
                                return;
                            }
                            else if (message.Text == "📊 Mening Statistikam")
                            {
                                await SendUserStats(chat.Id, user.Id);
                                return;
                            }
                            else if (message.Text == "📅 Bo'lajak Hasharlar")
                            {
                                await ShowUpcomingEvents(chat.Id);
                                return;
                            }
                            else if (message.Text == "ℹ️ Yordam")
                            {
                                await SendHelpMessage(chat.Id);
                                return;
                            }
                            else if (message.Text == "Ortga qaytish") 
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "Ortga qaytdingiz",
                                    replyMarkup: GetPersistentKeyboard()
                                );
                                return;
                            }
                            else if (message.Text == "Masjid qo'shish" && _adminUsernames.Contains(user.Username))
                            {
                                userState.AddingMasjidState = AddingMasjidStateEnum.AddingName;
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "Masjid nomini kiriting"
                                );
                            }
                            else if (message.Text == "Hashar qo'shish" && _adminUsernames.Contains(user.Username))
                            {
                                userState.IsAddingHashar = true;
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "Masjidni tanlang",
                                    replyMarkup: GetMasjidSearchKeyboard());
                            }
                            else if (message.Text == "🔄 Yangilash")
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "Bot yangilandi. Quyidagi tugmalardan foydalanishingiz mumkin:",
                                    replyMarkup: GetPersistentKeyboard()
                                );
                                return;
                            }
                            else if (message.Text == "/adminPanel" && _adminUsernames.Contains(user.Username))
                            {
                                await ShowAdminPanel(message.Chat.Id);
                            }
                        }
                        return;
                    }

                case UpdateType.CallbackQuery:
                    {
                        var callbackQuery = update.CallbackQuery;
                        if (callbackQuery == null) return;

                        var chat = callbackQuery.Message.Chat;
                        var userId = callbackQuery.From.Id;

                        if (!_userStates.ContainsKey(userId))
                        {
                            _userStates[userId] = new UserState();
                        }

                        if (callbackQuery.Data == "subscribe")
                        {
                            _userStates[userId].RegistrationState = RegistrationState.AwaitingGender;

                            await botClient.AnswerCallbackQueryAsync(
                                callbackQueryId: callbackQuery.Id,
                                text: "Ro'yxatdan o'tish boshlandi!"
                            );

                            await botClient.EditMessageTextAsync(
                                chatId: chat.Id,
                                messageId: callbackQuery.Message.MessageId,
                                text: "Ro'yxatdan o'tish uchun quyidagi ma'lumotlarni kiritishingiz kerak."
                            );

                            await botClient.SendTextMessageAsync(
                                chatId: chat.Id,
                                text: "Iltimos, jinsingizni tanlang:",
                                replyMarkup: GetGenderKeyboard()
                            );

                            return;
                        }
                        else if (callbackQuery.Data.StartsWith("rsvp_"))
                        {
                            var parts = callbackQuery.Data.Split('_');
                            var response = parts[1];
                            var hasharDate = parts[2];

                            if (response == "yes")
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: $"Sizni {FormatDateString(hasharDate)} kuni hasharda kutib qolamiz!",
                                    replyMarkup: GetPersistentKeyboard()  // Re-send the keyboard
                                );
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "Xabaringiz uchun rahmat. Keyingi hasharlarda ko'rishguncha!",
                                    replyMarkup: GetPersistentKeyboard()
                                );
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

                            _userStats[userId].Add(new HasharParticipation
                            {
                                Date = DateTime.ParseExact(hasharDate, "yyyyMMdd", null),
                                Attended = attended
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: chat.Id,
                                text: $"Qayd etildi: Siz {FormatDateString(hasharDate)}" +
                                      $" kunidagi hasharga " +
                                      $"{(attended ? "qatnashdingiz ✅" : "qatnashmasdingiz ❌")}.",
                                replyMarkup: GetPersistentKeyboard()
                            );
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                            return;
                        }
                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateHandler: {ex}");
        }
    }

    private static async Task ShowAdminPanel(long chatId)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Admin panel",
            replyMarkup: GetAdminKeyboard()
            );
    }

    private static async Task SendWelcomeMessage(long chatId, string firstName)
    {
        // Create welcome message with formatting
        var welcomeText = $"Assalomu alaykum, {firstName}! 👋\n\n" +
                         "Obod masjid botiga xush kelibsiz! " +
                         "Bu bot masjidlarni tozalash bo'yicha hashar tadbirlariga qo'shilishingizga yordam beradi.\n\n" +
                         "• Kanalimizda e'lon qilinadigan bo'lajak hasharlar (haftada 1-3 marta) haqida xabar olasiz\n" +
                         "• Hasharlarni ko'ngillilik asosida ro'yxatdan o'tkazishingiz mumkin\n" +
                         "• O'z ishtirokingizni kuzatib borishingiz mumkin\n\n" +
                         "Obuna bo'lish va tadbirlarimiz haqida yangilanishlarni olish uchun 'Boshlash' tugmasini bosing.";

        // Create keyboard with Start button
        var startButton = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Boshlash ▶️", "subscribe") }
        });

        // Send welcome message with button
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: welcomeText,
            replyMarkup: startButton,
            parseMode: ParseMode.Html
        );
    }

    private static async Task ShowUpcomingEvents(long chatId)
    {
        if (_hasharEvents.Count == 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Hozirda rejalashtirilgan hasharlar yo'q. Iltimos, keyinroq qayta tekshiring.",
                replyMarkup: GetPersistentKeyboard() 
            );
            return;
        }

        var eventsText = "Bo'lajak hasharlar:\n\n";
        foreach (var hashar in _hasharEvents.OrderBy(h => h.Date))
        {
            eventsText += $"📆 {hashar.Date:yyyy-MM-dd} - {hashar.Time}\n" +
                          $"📍 {hashar.Masjid}\n\n";
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: eventsText,
            replyMarkup: GetPersistentKeyboard()  // Always include the keyboard
        );
    }

    private static async Task SendHelpMessage(long chatId)
    {
        var helpText = "💡 <b>Obod masjid bot qo'llanmasi</b>\n\n" +
                     "Quyidagi buyruqlardan foydalanishingiz mumkin:\n\n" +
                     "/start - Botni qayta ishga tushirish\n" +
                     "/stats - Hashar statistikangizni ko'rish\n" +
                     "/events - Bo'lajak hasharlarni ko'rish\n" +
                     "/help - Ushbu yordam xabarini ko'rish\n\n" +
                     "Bot haqida:\n" +
                     "Bu bot masjidlarni tozalash bo'yicha hashar tadbirlarida ishtirok etishingizga yordam beradi. " +
                     "Bot orqali hasharlarga ro'yxatdan o'tishingiz va qatnashganligingizni tasdiqlashingiz mumkin.";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: helpText,
            parseMode: ParseMode.Html,
            replyMarkup: GetPersistentKeyboard()  // Always include the keyboard
        );
    }

    private static async Task AnnounceHashar()
    {
        var hashar = _hasharEvents.OrderBy(h => h.Date).First();
        var message = $"E'lon! {hashar.Date:MMMM d, yyyy} sanasida soat {hashar.Time} da " +
                      $"{hashar.Masjid} masjidida hashar uyushtirilmoqda, kelasizmi?";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] {
                InlineKeyboardButton.WithCallbackData("Ha ✅", $"rsvp_yes_{hashar.Date:yyyyMMdd}"),
                InlineKeyboardButton.WithCallbackData("Yo'q ❌", $"rsvp_no_{hashar.Date:yyyyMMdd}")
            }
        });

        foreach (var userId in _subscribedUsers)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: message,
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send announcement to user {userId}: {ex.Message}");
            }
        }
    }

    private static async Task CheckParticipation(HasharEvent hashar)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] {
                InlineKeyboardButton.WithCallbackData("Ha ✅", $"attended_yes_{hashar.Date:yyyyMMdd}"),
                InlineKeyboardButton.WithCallbackData("Yo'q ❌", $"attended_no_{hashar.Date:yyyyMMdd}")
            }
        });

        foreach (var userId in _subscribedUsers)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: $"{hashar.Masjid} masjidida bo'lib o'tgan hasharga keldingizmi?",
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send participation check to user {userId}: {ex.Message}");
            }
        }
    }

    private static async Task SendUserStats(long chatId, long userId)
    {
        if (!_userStats.ContainsKey(userId) || _userStats[userId].Count == 0)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Siz hali hech qanday hasharlarda qatnashmagansiz!",
                replyMarkup: GetPersistentKeyboard()  // Always include the keyboard
            );
            return;
        }

        var stats = _userStats[userId];
        var attended = stats.Count(s => s.Attended);
        var total = stats.Count;
        var attendanceRate = total > 0 ? (double)attended / total * 100 : 0;

        var summary = $"<b>Sizning Hashar Statistikangiz:</b>\n\n" +
                      $"{string.Join("\n", stats.OrderByDescending(s => s.Date)
                          .Select(s => $"{s.Date:yyyy-MM-dd}: " +
                                       $"{(s.Attended ? "✅ Qatnashgan" : "❌ Qatnashmagan")}"))}\n\n" +
                      $"<b>Jami:</b> {attended}/{total} hashar ({attendanceRate:F1}%)";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: summary,
            parseMode: ParseMode.Html,
            replyMarkup: GetPersistentKeyboard()
        );
    }
    private static async Task ShowHasharParticipants(long chatId, long userId)
    {
        
    }
    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n" +
                                                       $"[{apiRequestException.ErrorCode}]" +
                                                       $"\n{apiRequestException.Message}",
            _ => error.ToString()
        };
        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }

    private static string FormatDateString(string yyyyMMdd)
    {
        try
        {
            var date = DateTime.ParseExact(yyyyMMdd, "yyyyMMdd", null);
            return date.ToString("MMMM d, yyyy");
        }
        catch
        {
            return yyyyMMdd;
        }
    }
}

class HasharParticipation
{
    public DateTime Date { get; set; }
    public bool Attended { get; set; }
}