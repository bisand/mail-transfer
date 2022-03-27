// See https://aka.ms/new-console-template for more information

using System.Text;
using MailKit;
using MailKit.Net.Imap;

Console.WriteLine("Hello, World!");

static void copyAllMail(ImapConfig sourceConfig, ImapConfig destConfig)
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
            var personal = clientSource.GetFolder(clientSource.PersonalNamespaces[0]);
            // personal.Open(FolderAccess.ReadOnly);

            int mailCount = countFolderMail(clientSource, personal);
            Console.WriteLine("Total messages: {0}", mailCount);
            copyFolderMail(clientSource, clientDest, personal);
        }
        clientSource.Disconnect(true);
    }

}

static void copyFolderMail(ImapClient clientSource, ImapClient clientDest, IMailFolder folderSource)
{
    try
    {
        if (!folderSource.IsNamespace && folderSource.Exists && !folderSource.IsOpen)
            folderSource.Open(FolderAccess.ReadOnly);

        //TODO: Figure out how to check if folder exists.
        IMailFolder folderDest = clientDest.GetFolder(folderSource.FullName);
        for (int i = 0; i < folderSource.Count; i++)
        {
            var message = folderSource.GetMessage(i);
        }
        foreach (var folder in folderSource.GetSubfolders(false))
        {
            copyFolderMail(clientSource, clientDest, folder);
        }
    }
    catch (System.Exception ex)
    {
        Console.WriteLine("Error: {0}", ex.Message);
    }
}

static int countFolderMail(ImapClient client, IMailFolder imapFolder)
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

