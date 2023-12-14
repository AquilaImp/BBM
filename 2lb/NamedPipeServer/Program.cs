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

public class PriorityQueue<T>
{
    private List<T> data;
    private readonly Comparison<T> comparison;

    public PriorityQueue(Comparison<T> comparison)
    {
        this.data = new List<T>();
        this.comparison = comparison;
    }

    public void Enqueue(T item)
    {
        data.Add(item);
        int ci = data.Count - 1;

        while (ci > 0)
        {
            int pi = (ci - 1) / 2;

            if (comparison(data[ci], data[pi]) >= 0)
                break;

            Swap(ci, pi);
            ci = pi;
        }
    }

    public T Dequeue()
    {
        if (data.Count == 0)
            throw new InvalidOperationException("Queue is empty");

        T frontItem = data[0];
        int li = data.Count - 1;

        data[0] = data[li];
        data.RemoveAt(li);

        --li;
        int pi = 0;

        while (true)
        {
            int ci = pi * 2 + 1;

            if (ci > li)
                break;

            int rc = ci + 1;

            if (rc <= li && comparison(data[rc], data[ci]) < 0)
                ci = rc;

            if (comparison(data[pi], data[ci]) <= 0)
                break;

            Swap(pi, ci);
            pi = ci;
        }

        return frontItem;
    }

    public int Count => data.Count;

    private void Swap(int i, int j)
    {
        T temp = data[i];
        data[i] = data[j];
        data[j] = temp;
    }
}

class Program
{
    static PriorityQueue<UserInfo> dataQueue = new PriorityQueue<UserInfo>((x, y) => x.Age.CompareTo(y.Age));
    static CancellationTokenSource cts = new CancellationTokenSource();

    static async Task Main(string[] args)
    {
        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.InOut))
        {
            Console.WriteLine("Ожидание подключения клиента...");
            await pipeServer.WaitForConnectionAsync(cts.Token);

            // Запуск асинхронного чтения из очереди и обработки данных
            Task processQueueTask = ProcessQueueAsync();

            while (!cts.Token.IsCancellationRequested)
            {
                // Ввод данных через консоль
                Console.Write("Введите имя: ");
                string name = Console.ReadLine();

                Console.Write("Введите возраст: ");
                int age = int.Parse(Console.ReadLine());

                // Создание и отправка объекта UserInfo на сервер
                UserInfo user = new UserInfo { Name = name, Age = age };

                // Добавление данных в очередь с учетом приоритета (возраст)
                dataQueue.Enqueue(user);

                // Отправка ответа клиенту
                string response = $"Данные приняты: Имя: {user.Name}, Возраст: {user.Age}";
                byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                await pipeServer.WriteAsync(responseBuffer, 0, responseBuffer.Length, cts.Token);
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
