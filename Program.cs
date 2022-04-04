// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;

Console.WriteLine("Preparing to transfer mail...");

void copyAllMail(ImapConfig sourceConfig, ImapConfig destConfig)
{
    using (var clientSource = new ImapClient())
    {
        clientSource.Connect(sourceConfig.Address, sourceConfig.Port, true, CancellationToken.None);
        clientSource.Authenticate(sourceConfig.Username, sourceConfig.Password);

        using (var clientDest = new ImapClient())
        {
            clientDest.Connect(destConfig.Address, destConfig.Port, true, CancellationToken.None);
            clientDest.Authenticate(destConfig.Username, destConfig.Password);

            // The Inbox folder is always available on all IMAP servers...
            var namespaceSource = clientSource.GetFolder(clientSource.PersonalNamespaces[0]);
            var folderSource = namespaceSource.GetSubfolder("[Gmail]");
            // var folderDest = clientDest.GetFolder(clientDest.PersonalNamespaces[0]);
            var folderDest = clientDest.Inbox;

            copyFolderMail(clientSource, clientSource.Inbox, clientDest, clientDest.Inbox, null, null, sourceConfig, destConfig);
            copyFolderMail(clientSource, folderSource, clientDest, folderDest, null, null, sourceConfig, destConfig);
        }
        clientSource.Disconnect(true);
    }
}

void displaySubfolders(IMailFolder rootFolder)
{
    // The Inbox folder is always available on all IMAP servers...
    var subfolders = rootFolder.GetSubfolders();
    foreach (var folder in subfolders)
    {
        Console.WriteLine(folder.FullName);
        displaySubfolders(folder);
    }
}

void displayAllFolders(ImapConfig sourceConfig)
{
    using (var clientSource = new ImapClient())
    {
        clientSource.Connect(sourceConfig.Address, sourceConfig.Port, true, CancellationToken.None);
        clientSource.Authenticate(sourceConfig.Username, sourceConfig.Password);

        displaySubfolders(clientSource.GetFolder(clientSource.PersonalNamespaces[0]));

        clientSource.Disconnect(true);
    }
}

void copySpecificMail(ImapConfig sourceConfig, ImapConfig destConfig)
{
    using (var clientSource = new ImapClient())
    {
        clientSource.Connect(sourceConfig.Address, sourceConfig.Port, true, CancellationToken.None);
        clientSource.Authenticate(sourceConfig.Username, sourceConfig.Password);

        using (var clientDest = new ImapClient())
        {
            clientDest.Connect(destConfig.Address, destConfig.Port, true, CancellationToken.None);
            clientDest.Authenticate(destConfig.Username, destConfig.Password);

            using (var clientDestExclude = new ImapClient())
            {
                clientDestExclude.Connect(destConfig.Address, destConfig.Port, true, CancellationToken.None);
                clientDestExclude.Authenticate(destConfig.Username, destConfig.Password);

                // The Inbox folder is always available on all IMAP servers...
                var namespaceSource = clientSource.GetFolder(clientSource.PersonalNamespaces[0]);
                var folderDestRoot = clientDest.GetFolder(clientDest.PersonalNamespaces[0]);
                // var folderDest = clientDest.Inbox;

                IMailFolder folderSource;
                // copyFolderMail(clientSource, clientSource.Inbox, clientDest, folderDestRoot, null, null, sourceConfig, destConfig);
                // folderSource = namespaceSource.GetSubfolder("[Gmail]").GetSubfolder("Papirkurv");
                // copyFolderMail(clientSource, folderSource, clientDest, folderDestRoot, null, clientDestExclude.Inbox, sourceConfig, destConfig);
                // folderSource = namespaceSource.GetSubfolder("[Gmail]").GetSubfolder("Sendt e-post");
                // copyFolderMail(clientSource, folderSource, clientDest, folderDestRoot, null, clientDestExclude.Inbox, sourceConfig, destConfig);
                // folderSource = namespaceSource.GetSubfolder("[Gmail]").GetSubfolder("Stjernemerket");
                // copyFolderMail(clientSource, folderSource, clientDest, folderDestRoot, null, clientDestExclude.Inbox, sourceConfig, destConfig);
                // folderSource = namespaceSource.GetSubfolder("[Gmail]").GetSubfolder("Søppelpost");
                // copyFolderMail(clientSource, folderSource, clientDest, folderDestRoot, null, clientDestExclude.Inbox, sourceConfig, destConfig);
                // folderSource = namespaceSource.GetSubfolder("[Gmail]").GetSubfolder("Chatteøkter");
                // copyFolderMail(clientSource, folderSource, clientDest, folderDestRoot, null, clientDestExclude.Inbox, sourceConfig, destConfig);
                folderSource = namespaceSource.GetSubfolder("[Gmail]").GetSubfolder("All e-post");
                copyFolderMail(clientSource, folderSource, clientDest, folderDestRoot, folderDestRoot.GetSubfolder("Arkiv"), clientDestExclude.Inbox, sourceConfig, destConfig);
            }
            clientDest.Disconnect(true);
        }
        clientSource.Disconnect(true);
    }

}

void copyFolderMail(ImapClient clientSource, IMailFolder folderSource, ImapClient clientDest, IMailFolder folderDestRoot, IMailFolder? folderDest, IMailFolder? folderExclude, ImapConfig sourceConfig, ImapConfig destConfig)
{
    try
    {
        Console.WriteLine("Copying folder: {0}", folderSource.Name);
        string folderName = GetName(folderSource.Name);
        if (folderDest == null)
        {
            folderDest = folderDestRoot.Create(folderName, !folderSource.IsNamespace);
            if (folderSource.IsSubscribed)
                folderDest.Subscribe();
        }
        if (!folderSource.IsNamespace && folderSource.Exists && !folderSource.IsOpen)
        {
            folderSource.Open(FolderAccess.ReadOnly);
        }
        if (!folderDest.IsNamespace && folderDest.Exists && !folderDest.IsOpen)
        {
            folderDest.Open(FolderAccess.ReadWrite);
        }
        if (folderExclude != null)
        {
            folderExclude.Open(FolderAccess.ReadOnly);
        }

        var totalCount = folderSource.Count;
        for (int i = 0; i < totalCount; i++)
        {
            try
            {
                using (var message = folderSource.GetMessage(i))
                {
                    string? messageId = message?.MessageId;
                    IList<UniqueId>? uniqueIds = null;
                    if (!string.IsNullOrWhiteSpace(messageId))
                    {
                        uniqueIds = folderDest.Search(SearchQuery.HeaderContains("Message-Id", messageId));
                    }
                    else if (message != null)
                    {
                        var address = message.From.FirstOrDefault();
                        if (address != null)
                            uniqueIds = folderDest.Search(SearchQuery.FromContains(address.ToString()).And(SearchQuery.SubjectContains(message.Subject)));
                    }
                    if ((uniqueIds == null || uniqueIds.Count < 1) && folderExclude != null && !string.IsNullOrWhiteSpace(messageId))
                    {
                        uniqueIds = folderExclude.Search(SearchQuery.HeaderContains("Message-Id", messageId));
                    }
                    if (uniqueIds == null || uniqueIds.Count < 1)
                        folderDest.Append(message);
                }
                Console.Write("\rProcessing folder {0}, message {1} of {2} -> {3}%                                              ", folderDest.Name, i + 1, totalCount, (((i + 1) * 100) / totalCount));
            }
            catch (ServiceNotConnectedException ex)
            {
                clientSource.Disconnect(true);
                clientSource.Connect(sourceConfig.Address, sourceConfig.Port, true, CancellationToken.None);
                clientSource.Authenticate(sourceConfig.Username, sourceConfig.Password);

                clientDest.Disconnect(true);
                clientDest.Connect(destConfig.Address, destConfig.Port, true, CancellationToken.None);
                clientDest.Authenticate(destConfig.Username, destConfig.Password);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error copying message: {0} - {1}", i, ex);
            }
        }
        foreach (var folder in folderSource.GetSubfolders(false))
        {
            copyFolderMail(clientSource, folder, clientDest, folderDest, null, folderExclude, sourceConfig, destConfig);
        }
    }
    catch (System.Exception ex)
    {
        Console.WriteLine("Error: {0}", ex.Message);
    }
    Console.WriteLine("Done!");
}

string GetName(string name)
{
    return Regex.Replace(name, @"[^0-9a-zA-ZæøåÆØÅ\- ]+", "");
}

int countFolderMail(ImapClient client, IMailFolder imapFolder)
{
    try
    {
        if (!imapFolder.IsNamespace && imapFolder.Exists && !imapFolder.IsOpen)
            imapFolder.Open(FolderAccess.ReadOnly);
        var mailCount = imapFolder.Count;
        foreach (var folder in imapFolder.GetSubfolders(false))
        {
            mailCount += countFolderMail(client, folder);
        }
        return mailCount;
    }
    catch (System.Exception ex)
    {
        Console.WriteLine("Error: {0}", ex.Message);
    }
    return 0;
}

var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
DotEnv.Load(dotenv);

var sourceAddress = Environment.GetEnvironmentVariable("SOURCE_ADDRESS") ?? "";
var sourcePort = int.Parse(Environment.GetEnvironmentVariable("SOURCE_PORT") ?? "143");
var sourceUsername = Environment.GetEnvironmentVariable("SOURCE_USERNAME") ?? "";
var sourcePassword = Environment.GetEnvironmentVariable("SOURCE_PASSWORD") ?? "";
var destAddress = Environment.GetEnvironmentVariable("DEST_ADDRESS") ?? "";
var destPort = int.Parse(Environment.GetEnvironmentVariable("DEST_PORT") ?? "143");
var destUsername = Environment.GetEnvironmentVariable("DEST_USERNAME") ?? "";
var destPassword = Environment.GetEnvironmentVariable("DEST_PASSWORD") ?? "";

var source = new ImapConfig(sourceAddress, sourcePort, sourceUsername, sourcePassword);
var destination = new ImapConfig(destAddress, destPort, destUsername, destPassword);

displayAllFolders(source);
copySpecificMail(source, destination);
Console.WriteLine("Done!");
