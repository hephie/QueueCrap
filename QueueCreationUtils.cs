using System;
using System.Messaging;
using System.Security.Principal;

public static class QueueCreationUtils
{

    public static void CreateQueue(string queueName)
    {
        //Needed mapping file to register when publishing event from web server to app s erver
        var machineName = Environment.MachineName;
        //var machineName = "STESTAPP008.SELORT.LOCAL";

        var path = $@"{machineName}\private$\{queueName}";
        if (!MessageQueue.Exists(path))
        {
            using (var messageQueue = MessageQueue.Create(path, true))
            {
                SetDefaultPermissionsForQueue(messageQueue);
            }
        }
    }

    public

    static void SetDefaultPermissionsForQueue(MessageQueue queue)
    {
        var allow = AccessControlEntryType.Allow;
        queue.SetPermissions(AdminGroup, MessageQueueAccessRights.FullControl, allow);
    }

    static string AdminGroup = GetGroupName(WellKnownSidType.BuiltinAdministratorsSid);

    static string GetGroupName(WellKnownSidType wellKnownSidType)
    {
        return new SecurityIdentifier(wellKnownSidType, null)
            .Translate(typeof(NTAccount))
            .ToString();
    }

}