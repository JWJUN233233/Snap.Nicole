using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed partial class ChatViewModel : ObservableObject
{
    private readonly IChatService chatService;
    private readonly IOptionsMonitor<AppSettings> settings;
    private CancellationTokenSource? cts;

    public ChatViewModel(IServiceProvider serviceProvider)
    {
        chatService = serviceProvider.GetRequiredService<IChatService>();
        settings = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
    }

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        Messages.Add(new ChatMessage
        {
            Role = ChatRole.User,
            Content = input,
        });

        InputText = "";
        IsBusy = true;
        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            ChatRequestOptions options = new()
            {
                Model = settings.CurrentValue.DefaultModel,
                Temperature = 0.7f,
            };

            string modelId = options.Model;
            await foreach (ChatMessage response in chatService.StreamCompletionAsync(Messages, options, cts.Token))
            {
                ChatMessage tagged = new()
                {
                    Role = response.Role,
                    Content = response.Content,
                    Timestamp = response.Timestamp,
                    ToolCalls = response.ToolCalls,
                    ToolCallId = response.ToolCallId,
                    ModelId = modelId,
                };

                if (Messages.Count > 0 && Messages[^1].Role == ChatRole.Assistant)
                {
                    Messages[^1] = tagged;
                }
                else
                {
                    Messages.Add(tagged);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = $"Error: {ex.Message}",
            });
        }
        finally
        {
            cts?.Dispose();
            cts = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        cts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
    }
}
