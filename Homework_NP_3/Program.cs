using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Homework_NP_3
{
    class Program
    {
        static async Task Main()
        {
            Client client = new Client("127.0.0.1", 8888);
            Console.WriteLine("Adding items: \n");
            client.SendOrder(orderId: 1, quantity: 1);
            client.SendOrder(orderId: 2, quantity: 4);


            Console.WriteLine("\n\nChecking order status:\n");
            client.CheckOrderStatus();

            Console.WriteLine("Adding items: \n");
            client.SendOrder(orderId: 3, quantity: 3);

            Console.WriteLine("\nWaiting...");
            await Task.Delay(3000);


            while (true)
            {
                Console.WriteLine("\n\nChecking order status:\n");
                client.CheckOrderStatus();
                Console.WriteLine("\n\nPress enter to ask again...");
                Console.ReadLine();
            }
        }
    }

    public class Client
    {
        private readonly string serverIp;
        private readonly int serverPort;

        public Client(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
        }

        public void SendOrder(int orderId, int quantity)
        {
            using (TcpClient client = new TcpClient(serverIp, serverPort))
            {
                NetworkStream stream = client.GetStream();

                try
                {
                    string orderData = $"{orderId}, {quantity}";
                    byte[] data = Encoding.UTF8.GetBytes(orderData);
                    stream.Write(data, 0, data.Length);

                    byte[] responseBuffer = new byte[1024];
                    int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                    string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                    Console.WriteLine($"Server response: {response}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending order: {ex.Message}");
                }
                finally
                {
                    stream.Close();
                }
            }
        }

        public void CheckOrderStatus()
        {
            using (TcpClient client = new TcpClient(serverIp, serverPort))
            {
                NetworkStream stream = client.GetStream() ;

                try
                {
                    byte[] statusRequest = Encoding.UTF8.GetBytes("STATUS");
                    stream.Write(statusRequest, 0, statusRequest.Length);

                    byte[] responseBuffer = new byte[1024];
                    int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                    string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                    Console.WriteLine($"Server response: {response}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking order status: {ex.Message}");
                }
                finally
                {
                    stream.Close();
                }
            }
        }
    }
}
