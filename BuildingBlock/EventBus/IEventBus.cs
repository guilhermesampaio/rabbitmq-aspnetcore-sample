using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlock.EventBus
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);

        void Subscribe<T, TH>(string exchange, string queue)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }
}
