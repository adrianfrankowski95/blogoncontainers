using Blog.Services.Discovery.API.Infrastructure;
using Blog.Services.Discovery.API.Models;
using Blog.Services.Integration.Events;
using MassTransit;

namespace Blog.Services.Discovery.API.Integration.Consumers;

public class ServiceInstanceHeartbeatEventConsumer : IConsumer<ServiceInstanceHeartbeatEvent>
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<ServiceInstanceHeartbeatEventConsumer> _logger;

    public ServiceInstanceHeartbeatEventConsumer(
        IServiceRegistry serviceRegistry,
        ILogger<ServiceInstanceHeartbeatEventConsumer> logger)
    {
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task Consume(ConsumeContext<ServiceInstanceHeartbeatEvent> context)
    {
        Guid instanceId = context.Message.InstanceId;
        string serviceType = context.Message.ServiceType;
        HashSet<string> addresses = context.Message.ServiceAddresses;

        if (instanceId.Equals(Guid.Empty))
            throw new InvalidDataException($"{nameof(context.Message.InstanceId)} must not be empty");

        if (string.IsNullOrWhiteSpace(serviceType))
            throw new InvalidDataException($"{nameof(context.Message.ServiceType)} must not be null or empty");

        if (addresses is null || !addresses.Any())
            throw new InvalidDataException($"{nameof(context.Message.ServiceAddresses)} must not be null or empty");

        string addressesString = string.Join("; ", addresses);

        _logger.LogInformation("----- Handling {ServiceType} instance heartbeat event: {InstanceId} - {Addresses}", serviceType, instanceId, addressesString);

        var serviceInfo = new ServiceInstance(instanceId, serviceType, addresses);
        bool exists = await _serviceRegistry.TryRefreshServiceInstanceExpiry(serviceInfo).ConfigureAwait(false);

        if (!exists)
        {
            _logger.LogInformation("----- Registering new {ServiceType} instance recognized by a heartbeat: {InstanceId} - {Addresses}",
                serviceType, instanceId, addressesString);

            bool success = await _serviceRegistry.RegisterServiceInstance(serviceInfo).ConfigureAwait(false);

            if (success)
            {
                _logger.LogInformation("----- Successfully registered {ServiceType} instance: {InstanceId} - {Addresses}", serviceType, instanceId, addressesString);
                await context.Publish(new ServiceInstanceRegisteredEvent(instanceId, serviceType, addresses)).ConfigureAwait(false);
            }

            _logger.LogError("----- Error registering {ServiceType} instance: {InstanceId} - {Addresses}", serviceType, instanceId, addressesString);
        }
        _logger.LogWarning("----- The {ServiceType} instance already exists: {InstanceId} - {Addresses}", serviceType, instanceId, addressesString);
    }
}