using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public struct UserInfo
{
    public string Name { get; set; }
    public int Age { get; set; }
    public int Priority { get; set; }

    public UserInfo(string name, int age, int priority)
    {
        Name = name;
        Age = age;
        Priority = priority;
    }
}

class Program
{
    static Queue<UserInfo> dataQueue = new Queue<UserInfo>();
    static CancellationTokenSource cts = new CancellationTokenSource();

    static async Task Main(string[] args)
    {
        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.InOut))
        {
            Console.WriteLine("Ожидание подключения клиента...");
            Task serverTask = WaitForConnectionAsync(pipeServer);

            
            Task processQueueTask = ProcessQueueAsync();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                cts.Cancel(); 
                eventArgs.Cancel = true; 
            };


            await Task.WhenAll(serverTask, processQueueTask);
        }
    }

    static async Task WaitForConnectionAsync(NamedPipeServerStream pipeServer)
    {
        await pipeServer.WaitForConnectionAsync(cts.Token);

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                byte[] requestBuffer = new byte[1024];
                int bytesRead = await pipeServer.ReadAsync(requestBuffer, 0, requestBuffer.Length, cts.Token);

                if (bytesRead == 0)
                    break; 

                string request = Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);
                Console.WriteLine($"Получен запрос от клиента: {request}");

              
                Console.WriteLine($"Обработка запроса: {request}");

              
                string response = $"Данные приняты: {request}";
                byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                await pipeServer.WriteAsync(responseBuffer, 0, responseBuffer.Length, cts.Token);
            }
            catch (IOException)
            {
                
                break;
            }
        }

        Console.WriteLine("Сервер завершил работу.");
    }

    static async Task ProcessQueueAsync()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (dataQueue.Count > 0)
            {
                UserInfo user = dataQueue.Dequeue();
                Console.WriteLine($"Обработка данных из очереди: Имя: {user.Name}, Возраст: {user.Age}, Приоритет: {user.Priority}");
            }
            await Task.Delay(100); 
        }
    }
}
