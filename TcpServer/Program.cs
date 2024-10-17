using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpServer
{
    class Program
    {
        static void Main()
        {
            Server server = new Server(8888);
            Thread serverThread = new Thread(server.Start);
            serverThread.Start();

            Console.ReadLine();
        }
    }

    public class Server
    {
        private readonly TcpListener listener;
        private readonly Queue<RestaurantOrder> orderQueue;
        private readonly List<DishItem> menuItems;

        public Server (int  port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            orderQueue = new Queue<RestaurantOrder>();
            menuItems = new List<DishItem>()
            {
                new DishItem{Id = 1, Name = "Pizza", TimeToCook = TimeSpan.FromSeconds(30)},
                new DishItem{Id = 2, Name = "Salad", TimeToCook = TimeSpan.FromSeconds(10)},
                new DishItem{Id = 3, Name = "Lemonade", TimeToCook = TimeSpan.FromSeconds(5)}
            };
        }

        public void Start ()
        {
            listener.Start ();
            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");

                Thread clientThresd = new Thread(HandleClient);
                clientThresd.Start (client);
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received data from client: {data}");

                if (data.Equals("STATUS", StringComparison.OrdinalIgnoreCase))
                {
                    //отправляем информацию о текущем заказе в очереди
                    SendStatusInfo(stream);
                    Console.WriteLine("Requesting the status of orders");
                }
                else
                {
                    //добавление заказа в очередь
                    string[] orderData = data.Split(',');
                    var order = new RestaurantOrder
                    {
                        OrderId = int.Parse(orderData[0]),
                        Dish = menuItems.FirstOrDefault(e => e.Id == int.Parse(orderData[0])),
                        Quantity = int.Parse(orderData[1]),
                        OrderTime = DateTime.Now
                    };
                    orderQueue.Enqueue(order);
                    Console.WriteLine($"Order added - {orderQueue.Peek()}");

                    //отправка подтверждения клиенту
                    byte[] response = Encoding.UTF8.GetBytes("Order received successfully.");
                    stream.Write(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                stream.Close();
            }
        }

        public void SendStatusInfo(NetworkStream stream)
        {
            //Отправляем информацию о текущем заказе в очереди
            if (orderQueue.Count > 0)
            {
                var currentOrder = orderQueue.Peek();
                if (CalculateRemainingTime(currentOrder).TotalSeconds <= 0)
                {
                    string orderReady = $"Here's your order - (ID: {currentOrder.OrderId}, Dish: {currentOrder.Dish.Name}. Take it.";
                    byte[] orderData = Encoding.UTF8.GetBytes(orderReady);
                    stream.Write(orderData, 0, orderData.Length);
                    orderQueue.Dequeue();
                }
                string statusInfo = $"Current order ID: {currentOrder.OrderId}, Dish: {currentOrder.Dish.Name}, Remaining time: {CalculateRemainingTime(currentOrder)}";
                byte[] statusData = Encoding.UTF8.GetBytes(statusInfo);
                stream.Write(statusData, 0, statusData.Length);
            }
            else
            {
                string noOrderInfo = "No orders in queue.";
                byte[] noOrderData = Encoding.UTF8.GetBytes(noOrderInfo);
                stream.Write(noOrderData, 0, noOrderInfo.Length);
            }
        }

        private TimeSpan CalculateRemainingTime(RestaurantOrder restaurantOrder)
        {
            DateTime now = DateTime.Now;
            TimeSpan elapsedTime = now - restaurantOrder.OrderTime;
            TimeSpan remainingTime = (restaurantOrder.Quantity * restaurantOrder.Dish.TimeToCook) - elapsedTime;
            return remainingTime;
        }
    }

    public class DishItem
    {
        public int Id { get; set; }
        private static int id;

        public DishItem()
        {
            Id = ++id;
        }

        public string Name { get; set; }
        public TimeSpan TimeToCook { get; set; }
    }

    public record class RestaurantOrder
    {
        public int OrderId { get; set; }
        public DishItem Dish { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderTime { get; set; }
    }
}
