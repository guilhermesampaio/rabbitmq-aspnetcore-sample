using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlock.EventBus
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);

        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }

    public class IntegrationEvent
    {

    }

    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(IntegrationEvent @event);
    }

    public interface IIntegrationEventHandler { }
}
