public class ImapConfig
{
    public string Address { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }

    public ImapConfig(string address, int port, string username, string password)
    {
        this.Address = address;
        this.Port = port;
        this.Username = username;
        this.Password = password;
    }
}