using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public struct UserInfo
{
    public string Name { get; set; }
    public int Age { get; set; }

    public UserInfo(string name, int age)
    {
        Name = name;
        Age = age;
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
            await pipeServer.WaitForConnectionAsync(cts.Token);

            // Запуск асинхронного чтения из очереди и обработки данных
            Task processQueueTask = ProcessQueueAsync();

            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead > 0)
                    {
                        string jsonUserData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        UserInfo user = JsonSerializer.Deserialize<UserInfo>(jsonUserData);

                        // Добавление данных в очередь с учетом приоритета
                        dataQueue.Enqueue(user);

                        // Отправка ответа клиенту
                        string response = $"Данные приняты: Имя: {user.Name}, Возраст: {user.Age}";
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                        await pipeServer.WriteAsync(responseBuffer, 0, responseBuffer.Length, cts.Token);
                    }
                }
                catch (IOException)
                {
                    // Обработка разрыва соединения
                    break;
                }
            }

            // Ожидание завершения обработки очереди перед закрытием
            await processQueueTask;
        }
    }

    static async Task ProcessQueueAsync()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (dataQueue.Count > 0)
            {
                UserInfo user = dataQueue.Dequeue();
                Console.WriteLine($"Обработка данных из очереди: Имя: {user.Name}, Возраст: {user.Age}");
            }

            // Добавьте здесь нужные операции с данными, например, запись в файл

            await Task.Delay(100); // Имитация обработки данных
        }
    }
}
