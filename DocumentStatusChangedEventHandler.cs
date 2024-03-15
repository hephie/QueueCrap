using NServiceBus;
using NServiceBus.Logging;
using QueueCrap.Shared;
using System.Threading.Tasks;

public class DocumentStatusChangedEventHandler : IHandleMessages<DocumentStatusChangedEvent>
{
    static ILog log = LogManager.GetLogger<DocumentStatusChangedEventHandler>();

    public Task Handle(DocumentStatusChangedEvent @event, IMessageHandlerContext context)
    {
        log.Info($"Subscriber has received event {@event.GetType().Name} with DocumentId {@event.DocumentGuid}.");
        return Task.CompletedTask;
    }
}