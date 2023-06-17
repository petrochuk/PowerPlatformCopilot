using DataverseCopilot.Dialog;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel.Orchestration;

namespace DataverseCopilot.Resources.Email;

public class EmailIterator : Iterator
{
    GraphServiceClient _graphClient;
    PageIterator<Message, MessageCollectionResponse>? _pageIterator;
    Message? _currentMessage;

    public EmailIterator(GraphServiceClient graphClient)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        ResourceObject = Resource.Email;


    }

    public override async Task<bool> Next()
    {
        if (_pageIterator != null)
            return _pageIterator.State != PagingState.Complete;

        var emails = await _graphClient.Me.MailFolders["Inbox"].Messages.GetAsync(
            q =>
            {
                q.QueryParameters.Top = 10;
                q.QueryParameters.Filter = "isRead eq false";
            }
        );

        _pageIterator = PageIterator<Message, MessageCollectionResponse>.CreatePageIterator(
        _graphClient, emails, ProcessMessage);

        await _pageIterator.IterateAsync();

        return _pageIterator.State != PagingState.Complete;
    }

    public Message? CurrentMessage => _currentMessage;

    private async Task<bool> ProcessMessage(Message message)
    {
        _currentMessage = null;

        if (message is EventMessageRequest eventMessage)
        {
            /* TODO Enable this secenario
            var replyAllBody = new Microsoft.Graph.Me.Events.Item.Accept.AcceptPostRequestBody()
            {
                Comment = "Accepted by AI",
                SendResponse = true
            };

            await _graphClient.Me.Events[eventMessage.Id].Accept.PostAsync(replyAllBody);
            */
        }
        else
        {
            _currentMessage = message;
        }

        return false;
    }
}
