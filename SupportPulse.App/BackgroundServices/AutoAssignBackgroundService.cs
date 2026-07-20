#region Usings

using System.Threading.Channels;
using SupportPulse.Core.DTOs.Admin.AutoLock;
using SupportPulse.Core.Services.Admin.Assign;

#endregion

namespace SupportPulse.App.BackgroundServices
{
    /// <summary>
    /// Background service that continuously reads from a channel and performs automatic
    /// assignment of unlocked chats to the most suitable online admin.
    /// </summary>
    public class AutoAssignBackgroundService : BackgroundService
    {
        #region Constructor & Dependencies

        private readonly Channel<AssignChatDto> _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoAssignBackgroundService> _logger;

        public AutoAssignBackgroundService(
            Channel<AssignChatDto> channel,
            IServiceScopeFactory scopeFactory,
            ILogger<AutoAssignBackgroundService> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #endregion

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoAssignBackgroundService started.");
            ColorLog(ConsoleColor.Green, "AutoAssign Service started and listening for chat assignment commands...");

            await foreach (var command in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    ColorLog(ConsoleColor.Cyan,
                        $"[AutoAssign] Processing chat {command.ChatId} for category {command.SupportCategoryId}");
                    _logger.LogInformation("Processing assignment for ChatId={ChatId}, CategoryId={CategoryId}",
                        command.ChatId, command.SupportCategoryId);

                    using var scope = _scopeFactory.CreateScope();
                    var assignService = scope.ServiceProvider.GetRequiredService<IAssignChatService>();
                    await assignService.AssignChatAsync(command, stoppingToken);

                    ColorLog(ConsoleColor.Green,
                        $"[AutoAssign] Chat {command.ChatId} assignment flow completed successfully.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    ColorLog(ConsoleColor.Yellow, "[AutoAssign] Service cancellation requested. Exiting gracefully.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing assign command for ChatId={ChatId}", command.ChatId);
                    ColorLog(ConsoleColor.Red, $"[AutoAssign] Error processing chat {command.ChatId}: {ex.Message}");
                }
            }
        }

        #region Helpers

        /// <summary>
        /// Writes a colored log message to the console.
        /// </summary>
        private static void ColorLog(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
            Console.ResetColor();
        }

        #endregion
    }
}