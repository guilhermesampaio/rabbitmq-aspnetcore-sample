using System.Threading.Tasks;

namespace BuildingBlock.EventBus
{
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(IntegrationEvent @event);
    }
}
