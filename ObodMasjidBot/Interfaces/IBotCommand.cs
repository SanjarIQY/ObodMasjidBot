using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using System.Threading;
using ObodMasjidBot.Data;

namespace ObodMasjidBot.Interfaces;

public interface IBotCommand
{
    bool CanHandle(Update update);
    Task ExecuteAsync(ITelegramBotClient botClient, Update update, UserState userState, CancellationToken cancellationToken);
}
