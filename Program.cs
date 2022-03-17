using System.Net;
using System.Net.Sockets;
using System.Text;

public static class ServerClass
{
    static Dictionary<string, users> accounts = new Dictionary<string, users>();
    private const string k_GlobalIp = "127.0.0.1";
    private const string k_LocalIp = "127.0.0.1";
    private const int k_Port = 7777;

    public static void Main(string[] args)
    {
        Server();
    }

    public static void Server()
    {
        LoadUsers();
        var ipAddress = IPAddress.Parse(k_LocalIp);
        var localEp = new IPEndPoint(ipAddress, k_Port);
        using var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(localEp);
        listener.Listen();
        Console.WriteLine("Waiting...");
        //listen forever for connections
        for (; ; )
        {
            try
            {
                var handler = listener.Accept();
                var thread = new Thread(new ThreadStart(() => ClientHandler(handler)));
                thread.Start();
            }
            catch
            {
                Console.WriteLine("user Forcefully Disconnected");
            }
        }
    }

    public static void ClientHandler(Socket handler)
    {
        try
        {
            
            Console.WriteLine("{0} connected", handler.RemoteEndPoint);
            var buffer = new byte[1024];
            for(; ; )
            {
                string loginORcreate = recievemessage(handler);
                if (loginORcreate == "C")
                {
                    Console.WriteLine("{0} is trying to create a new user", handler.RemoteEndPoint);


                    CreateNewUser(handler);
                }
                else if (loginORcreate == "L")
                {
                    string username = recievemessage(handler);
                    SignIn(handler, username);
                }
            }
        }
        catch
        {
            Console.WriteLine("user Forcefully Disconnected");
            handler.Shutdown(SocketShutdown.Both);
        }
    }

    private static void LoadUsers()
    {
        using (var reader = new StreamReader("users.csv"))
        {
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] parts = line.Split(',');
                accounts.Add(parts[0], new users(parts[0], parts[1], parts[2]));
                //Console.WriteLine(login.GetValueOrDefault(parts[0]).print());
            }
        }
    }

    private static void SignIn(Socket handler,string username)
    {

    }
    private static void CreateNewUser(Socket handler)
    {
        string username = recievemessage(handler);
        if(username == "quit")
        {
            return;
        }
        for (; ; )
        {
            if (accounts.ContainsKey(username))
            {
                sendmessage(handler, "E:1\r");
            }
            else
            {
                sendmessage(handler, "null");
                accounts.Add(username, new users(username, recievemessage(handler)));
                Console.WriteLine("New User: '" + username + "' has been created.");
                ExportUsers();
                break;
            }
        }
        
            
    }
    static void sendmessage(Socket socket, string message)
    {
        var bytes = Encoding.ASCII.GetBytes(message+"\r");
        socket.Send(bytes);
    }
    static string recievemessage(Socket socket)
    {
        var buffer = new byte[1024];
        var numBytesReceived = socket.Receive(buffer);
        var textReceived = Encoding.ASCII.GetString(buffer, 0, numBytesReceived);
        return textReceived;
    }
    static void ExportUsers()
    {
        Console.WriteLine("saving log");
        using (var adder = new StreamWriter("users.csv"))
        {
            adder.WriteLine("Username,Password,Account Type");
            var keys = accounts.Keys.ToList();
            foreach (var key in keys)
            {
                adder.WriteLine(key.ToString() + "," + accounts[key].print());
            }
        }
    }
};

public class users {
    private string username;
    private string password_hash;
    private string account_type; // 1=standard 2=Admin

    public users(string username, string password_hash, string account_type)
    {
        this.username = username;
        this.password_hash = password_hash;
        this.account_type = account_type;
    }

    public users(string username, string password_hash)
    {
        this.username=username;
        this.password_hash=password_hash;
        this.account_type = "1";
    }

    public string getUsername()
    {
        return username;
    }
    public string getPasswordHash()
    {
        return password_hash;
    }
    public string getAccountType()
    {
        return account_type;
    }

    public string print()
    {
        return password_hash + "," + account_type;
    }
};