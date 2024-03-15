using NServiceBus;
using QueueCrap.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueCrap
{
    internal class Program
    {
        static string publisherQueueName = "selor-documentmanagement-publisher";
        static string subscriberOneQueName = "selor-subscriber-one.document-status-changed";
        static async Task Main(string[] args)
        {
            try
            {
                //Register eerste subscriber, via message intent te posten in de DM queue
                var subscriberEndpointConfig = RegisterSubscriber();
                var subcriberEndpointInstance = await Endpoint.Start(subscriberEndpointConfig).ConfigureAwait(false);
                Console.WriteLine("Subscriber registered on publish queue.");

                var loop = true;
                while (loop)
                {
                    Console.WriteLine("Press any key to stop the subscriber;");
                    Console.ReadKey();
                    loop = false;

                }
                // Stop publish & endpoint
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

        private static EndpointConfiguration RegisterSubscriber()
        {
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //    System.Transactions.TransactionManager.ImplicitDistributedTransactions = true;

            var endpointConfiguration = new EndpointConfiguration($"{subscriberOneQueName}");

            //endpointConfiguration.Conventions().Add(MessageConvention.Create(typeof(InteractionModule))); //why ? what?
            endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.SendFailedMessagesTo($"{subscriberOneQueName}.error");
            endpointConfiguration.AuditProcessedMessagesTo($"{subscriberOneQueName}.audit");
            endpointConfiguration.EnableInstallers();

            var routing = endpointConfiguration.UseTransport(new MsmqTransport());
            routing.DisablePublishing();
            routing.RegisterPublisher(typeof(DocumentStatusChangedEvent), publisherQueueName);

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


    }
}
