using NServiceBus;
using System;

namespace QueueCrap
{
    public class DocumentStatusChangedEvent : IEvent
    {
        public Guid DocumentGuid { get; set; }

        public string NewStatus { get; set; }
    }
}
