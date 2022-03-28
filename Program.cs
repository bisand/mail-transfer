// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;

Console.WriteLine("Preparing to transfer mail...");
int totalCount = 0;
int currentCount = 0;

void copyAllMail(ImapConfig sourceConfig, ImapConfig destConfig)
{
    using (var clientSource = new ImapClient())
    {
        clientSource.Connect(sourceConfig.Address, sourceConfig.Port, true);
        clientSource.Authenticate(sourceConfig.Username, sourceConfig.Password);

        using (var clientDest = new ImapClient())
        {
            clientDest.Connect(destConfig.Address, destConfig.Port, true);
            clientDest.Authenticate(destConfig.Username, destConfig.Password);

            // The Inbox folder is always available on all IMAP servers...
            var namespaceSource = clientSource.GetFolder(clientSource.PersonalNamespaces[0]);
            var folderSource = namespaceSource.GetSubfolder("[Gmail]");
            var folderDest = clientDest.GetFolder(clientDest.PersonalNamespaces[0]);

            totalCount = countFolderMail(clientSource, folderSource);
            Console.WriteLine("Total messages: {0}", totalCount);
            copyFolderMail(clientSource, folderSource, clientDest, folderDest);
        }
        clientSource.Disconnect(true);
    }

}

void copyFolderMail(ImapClient clientSource, IMailFolder folderSource, ImapClient clientDest, IMailFolder folderDest)
{
    try
    {
        if (!folderSource.IsNamespace && folderSource.Exists && !folderSource.IsOpen)
        {
            folderSource.Open(FolderAccess.ReadOnly);
            folderDest.Open(FolderAccess.ReadWrite);
        }
        for (int i = 0; i < folderSource.Count; i++)
        {
            try
            {
                currentCount++;
                var message = folderSource.GetMessage(i);
                IList<UniqueId>? uniqueIds = null;
                if (!string.IsNullOrWhiteSpace(message?.MessageId))
                    uniqueIds = folderDest.Search(SearchQuery.HeaderContains("Message-Id", message?.MessageId));
                if (uniqueIds == null || uniqueIds.Count < 1)
                    folderDest.Append(message);
                Console.Write("\rProcessing folder {0}, message {1} of {2} -> {3}%                                              ", folderDest.Name, currentCount, totalCount, ((currentCount * 100) / totalCount));
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error copying message: {0} - {1}", i, ex);
            }
        }
        foreach (var folder in folderSource.GetSubfolders(false))
        {
            try
            {
                string folderName = GetName(folder.Name);
                var fdest = folderDest.Create(folderName, !folder.IsNamespace);
                if (folder.IsSubscribed)
                    fdest.Subscribe();
                copyFolderMail(clientSource, folder, clientDest, fdest);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error create folder: {0} - {1}", folder.Name, ex);
            }
        }
    }
    catch (System.Exception ex)
    {
        Console.WriteLine("Error: {0}", ex.Message);
    }
}

string GetName(string name)
{
    return Regex.Replace(name, @"[^0-9a-zA-ZæøåÆØÅ]+", "_");
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

copyAllMail(source, destination);

Console.WriteLine("Done!");

