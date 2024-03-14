using DocumentManagement.Events;
using NServiceBus;
using NServiceBus.Features;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueCrap
{
    internal class Program
    {
        static string publisherQueueName = "selor-documentmanagement-publisher";
        static string documentStatusChangedEventName = "document-status-changed";

        static string subscriberOneQueName = "selor-subscriber-one";
        static async Task Main(string[] args)
        {
            try
            {
                // DM: Create queue in which events will be published
                var publisherEndpointConfig = await CreatePublisherQueue();
                var publisherEndpointIntance = await Endpoint.Start(publisherEndpointConfig).ConfigureAwait(false);
                Console.WriteLine("Publish queue created.");

                // DM: Publish event, nothing will happen to it yet, because there are no subscribers
                await PublishNewEvent(publisherEndpointIntance);

                //Register eerste subscriber, via message intent te posten in de DM queue
                var subscriberEndpointConfig = RegisterSubscriber();
                var subcriberEndpointInstance = await Endpoint.Start(subscriberEndpointConfig).ConfigureAwait(false);
                Console.WriteLine("Subscriber registered on publish queue.");

                // DM: Publish another event, subscriber is registered, so should be picked up.
                bool loop = true;
                while (loop)
                {
                    await PublishNewEvent(publisherEndpointIntance);
                    Console.WriteLine("Press c to add another event");
                    loop = Console.ReadKey().KeyChar.ToString().ToLower() == "c";
                }

                await PublishNewEvent(publisherEndpointIntance);



                // Stop publish & subscriber endpoints
                await publisherEndpointIntance.Stop().ConfigureAwait(false);
                await subcriberEndpointInstance.Stop().ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("This is the end. Press any key to exit...");
                Console.ReadKey();
            }
        }

        static async Task PublishNewEvent(IEndpointInstance endpointInstance)
        {
            Console.WriteLine();

            var newEvent = new DocumentStatusChangedEvent() { DocumentGuid = Guid.NewGuid(), NewStatus = "Signed" };
            await endpointInstance.Publish(newEvent).ConfigureAwait(false);
            Console.WriteLine($"Event {newEvent.GetType().Name} - {newEvent.DocumentGuid} published!");

            var dsEvent = new DocumentSignedEvent() { DocumentGuid = Guid.NewGuid(), DocumentId = 123 };
            await endpointInstance.Publish(dsEvent).ConfigureAwait(false);
            Console.WriteLine($"Event {dsEvent.GetType().Name} - {dsEvent.DocumentGuid} published!");
        }

        private static async Task<EndpointConfiguration> CreatePublisherQueue()
        {
            //Todo:
            //Needed mapping file to register when publishing event from web server to app s erver

            var endpointConfiguration = new EndpointConfiguration(publisherQueueName);
            //endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.UsePersistence<MsmqPersistence>();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.UseTransport(new MsmqTransport());
            endpointConfiguration.DisableFeature<AutoSubscribe>();
            //endpointConfiguration.SendOnly();

            endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);
            endpointConfiguration.SendFailedMessagesTo($"{publisherQueueName}.error");
            endpointConfiguration.AuditProcessedMessagesTo($"{publisherQueueName}.audit");
            endpointConfiguration.EnableInstallers();

            return endpointConfiguration;
        }

        private static async Task OnCriticalError(ICriticalErrorContext context, CancellationToken cancellationToken)
        {
            try
            {
                await context.Stop(cancellationToken);
            }
            finally
            {
                FailFast($"Critical error, shutting down: {context.Error}", context.Exception);
            }
        }

        private static void FailFast(string message, Exception exception) => Environment.FailFast(message, exception);

        private static async Task ConfigureSubscriber()
        {
            var endpointConfiguration = new EndpointConfiguration(subscriberOneQueName);
            //endpointConfiguration.UsePersistence<NonDurablePersistence>();
            //endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            endpointConfiguration.SendFailedMessagesTo($"{subscriberOneQueName}.error");
            endpointConfiguration.AuditProcessedMessagesTo($"{subscriberOneQueName}.audit");
            endpointConfiguration.EnableInstallers();

            var routing = endpointConfiguration.UseTransport(new MsmqTransport());
            routing.RegisterPublisher(typeof(DocumentStatusChangedEvent), publisherQueueName);

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
            Console.WriteLine($"{subscriberOneQueName} should now be informed if an ${typeof(DocumentStatusChangedEvent).Name} is publisehd to {publisherQueueName}.");
            await endpointInstance.Stop().ConfigureAwait(false);
        }

        private static EndpointConfiguration RegisterSubscriber()
        {
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //    System.Transactions.TransactionManager.ImplicitDistributedTransactions = true;

            var endpointConfiguration = new EndpointConfiguration($"{subscriberOneQueName}.{documentStatusChangedEventName}");

            //endpointConfiguration.Conventions().Add(MessageConvention.Create(typeof(InteractionModule))); //why ? what?

            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.UsePersistence<MsmqPersistence>();
            endpointConfiguration.SendFailedMessagesTo($"{subscriberOneQueName}.{documentStatusChangedEventName}.error");
            endpointConfiguration.AuditProcessedMessagesTo($"{subscriberOneQueName}.{documentStatusChangedEventName}.audit");
            endpointConfiguration.EnableInstallers();

            var routing = endpointConfiguration.UseTransport(new MsmqTransport());
            routing.RegisterPublisher(typeof(DocumentStatusChangedEvent), publisherQueueName);
            routing.RegisterPublisher(typeof(DocumentSignedEvent), publisherQueueName);

            return endpointConfiguration;
        }
    }
}
