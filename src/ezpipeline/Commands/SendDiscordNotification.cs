using Discord.Webhook;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class SendDiscordNotification : AbstractCommand<SendDiscordNotification.Options>
{
    public SendDiscordNotification() : base("notify-discord", "Send a notification message via WebHook")
    {
    }


    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        var hook = new DiscordWebhookClient(options.Url);
        await hook.SendMessageAsync(options.Message);
    }

    public class Options
    {
        [CommandLineOption("-u", "Web Hook URL")]
        public string? Url { get; set; }

        [CommandLineOption("-m", "Text message")]
        public string? Message { get; set; }
    }
}