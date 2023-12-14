using System;
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
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "MyPipe", PipeDirection.InOut))
        {
            Console.WriteLine("Подключение к серверу...");
            await pipeClient.ConnectAsync(cts.Token);

            // Запуск асинхронного чтения из очереди и обработки данных
            Task processQueueTask = ProcessQueueAsync();

            Console.CancelKeyPress += (sender, e) =>
            {
                // Обработка события Ctrl+C
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Завершение работы...");
            };

            while (!cts.Token.IsCancellationRequested)
            {
                // Ввод данных через консоль
                Console.Write("Введите имя: ");
                string name = Console.ReadLine();

                Console.Write("Введите возраст: ");
                int age = int.Parse(Console.ReadLine());

                // Создание и отправка объекта UserInfo на сервер
                UserInfo user = new UserInfo { Name = name, Age = age };
                string jsonUserData = JsonSerializer.Serialize(user);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonUserData);
                await pipeClient.WriteAsync(buffer, 0, buffer.Length, cts.Token);
                Console.WriteLine("Данные отправлены серверу.");
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
                Console.WriteLine($"Получены данные от сервера: Имя: {user.Name}, Возраст: {user.Age}");
            }

            // Добавьте здесь нужные операции с полученными данными, например, запись в файл

            await Task.Delay(100); // Имитация обработки данных
        }
    }
}
