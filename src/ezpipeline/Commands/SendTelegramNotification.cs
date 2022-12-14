using System.Globalization;
using PipelineTools;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AzurePipelineTool.Commands;

public class SendTelegramNotification : AbstractCommand<SendTelegramNotification.Options>
{
    public SendTelegramNotification() : base("notify-telegram", "Send a notification message via Telegram Bot")
    {
    }


    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        var telegramBot = new TelegramBotClient(new TelegramBotClientOptions(options.Token));

        if (string.IsNullOrWhiteSpace(options.ChatId))
        {
            var updates = await telegramBot.GetUpdatesAsync(limit: 100, cancellationToken: cancellationToken);
            foreach (var groups in updates.GroupBy(_ => _.Message?.Chat?.Id))
            {
                if (groups.Key == null)
                    continue;
                var firstMessage = groups.First();
                Console.WriteLine(
                    $"{firstMessage.Message.Chat.Title ?? "@" + firstMessage.Message.Chat.Username}: --chat-id {groups.Key}");
                options.ChatId = groups.Key.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ChatId) && !string.IsNullOrWhiteSpace(options.Message))
        {
            var message = await telegramBot.SendTextMessageAsync(options.ChatId, options.Message,
                options.ParseMode, cancellationToken: cancellationToken);
        }
    }

    public class Options
    {
        [CommandLineOption("-t", "Token")] public string Token { get; set; }

        [CommandLineOption("-c", "@channelname or chat_id")]
        public string? ChatId { get; set; }

        [CommandLineOption("-m", "Text message")]
        public string? Message { get; set; }

        [CommandLineOption(description: "Text message")]
        public ParseMode ParseMode { get; set; } = ParseMode.MarkdownV2;
    }
}