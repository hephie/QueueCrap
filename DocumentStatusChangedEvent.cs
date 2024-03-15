using NServiceBus;
using System;

namespace QueueCrap.Shared
{
    public class DocumentStatusChangedEvent : IEvent
    {
        public Guid DocumentGuid { get; set; }

        public string NewStatus { get; set; }
    }
}
