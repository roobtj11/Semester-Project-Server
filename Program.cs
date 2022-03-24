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
                Console.WriteLine("user Forcefully Disconnected 1");
            }
        }
    }

    public static void ClientHandler(Socket handler)
    {
        
        try
        {
            var connected = new Thread(new ThreadStart(() => Stillconnected(handler)));
            connected.Start();
            Console.WriteLine("{0} connected", handler.RemoteEndPoint);
            var buffer = new byte[1024];
            for(; ; )
            {
                string loginORcreate = recievemessage(handler);
                if (loginORcreate == "C")
                {
                    Console.WriteLine("{0} is trying to create a new user", handler.RemoteEndPoint);


                    if (CreateNewUser(handler))
                    {
                        break;
                    }
                }
                else if (loginORcreate == "L")
                {
                    if (SignIn(handler))
                    {
                        break;
                    }
                }
            }
            for(; ; )
            {
                recievemessage(handler);
            }
            
        }
        catch
        {
            Console.WriteLine("user Forcefully Disconnected 2");
            handler.Shutdown(SocketShutdown.Both);
            
        }
    }

    private static void Stillconnected(Socket s)
    {
        for(; ; )
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
            {
                Console.WriteLine("user Forcefully Disconnected 2");
                s.Shutdown(SocketShutdown.Both);
                break;d
            }
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

    private static bool SignIn(Socket handler)
    {
        string line = recievemessage(handler);
        string[] parts = line.Split(',');
        string username = parts[0];
        string password = parts[1];
        if (accounts.ContainsKey(username) && accounts.GetValueOrDefault(username).getPasswordHash() == password)
        {
            Console.WriteLine("{0} has signed in as {1}, as a {2} account.", handler.RemoteEndPoint, username, accounts.GetValueOrDefault(username).printAccountName());
            string approved = "Approved," + accounts.GetValueOrDefault(username).printAccountPerm();
            sendmessage(handler, approved);
            sendmessage(handler, "1%hello");
            return true;
        }
        else
        {
            sendmessage(handler, "DNE");
            return false;
        }
            
    }
    private static bool CreateNewUser(Socket handler)
    {
        string username = recievemessage(handler);
        if(username == "quit")
        {
            Console.WriteLine("UserBackedOut");
            return false;
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
                Console.WriteLine("New User: '" + username + "' has been created. And is now signed in as a standard user.");
                ExportUsers();
                return true;
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
}
public class users {
    private string username;
    private string password_hash;
    private string account_type; // 1=standard 2 = Admin 3 = GOD

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

    public string printAccountName()
    {
        if (account_type == "2")
            return "Admin";
        else if (account_type == "3")
            return "GOD";
        else
            return "standard";
    }
    public int printAccountPerm()
    {
        if (account_type == "1")
            return 1; 
        else if (account_type == "2")
        {
            return 2;
        }
        else
            return 3;
    }

    public string print()
    {
        return password_hash + "," + account_type;
    }
};