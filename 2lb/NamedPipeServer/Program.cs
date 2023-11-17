using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    public struct Ad
    {
        public int X;
        public int Y;
        public bool DA;
    }

    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += (sender, eventArgs) => { eventArgs.Cancel = true; };

        CancellationTokenSource cts = new CancellationTokenSource();
        var token = cts.Token;

        // Очередь для отправки данных
        var dataQueue = new PriorityQueue<DataItem>();

        // Буфер для сохранения полученных данных
        var dataBuffer = new List<Ad>();

        // Запуск потока для обработки данных в очереди
        var processingThread = new Thread(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (dataQueue.TryDequeue(out var dataItem))
                {
                    // Обработка данных
                    Console.WriteLine($"Processing data: X={dataItem.Data.X}, Y={dataItem.Data.Y}, DA={dataItem.Data.DA}, Priority: {dataItem.Priority}");
                    dataBuffer.Add(dataItem.Data);
                }
                else
                {
                    // Ждем некоторое время, чтобы не нагружать процессор
                    Thread.Sleep(100);
                }
            }
        });
        processingThread.Start();

        // Добавление данных в очередь
        Console.WriteLine("Server is running. Press Ctrl+C to stop.");
        while (!token.IsCancellationRequested)
        {
            // Ожидаем подключение клиента
            using (var serverPipe = new NamedPipeServerStream("tonel", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                await serverPipe.WaitForConnectionAsync(token);

                // Принимаем данные от клиента
                byte[] buffer = new byte[Marshal.SizeOf<Ad>()];
                await serverPipe.ReadAsync(buffer, 0, buffer.Length);

                Ad receivedData = MemoryMarshal.Read<Ad>(buffer);

                Console.WriteLine($"Received data from client: X={receivedData.X}, Y={receivedData.Y}, DA={receivedData.DA}");

                // Добавляем данные в очередь с приоритетом
                Console.Write("Enter priority for the received data: ");
                if (int.TryParse(Console.ReadLine(), out int priority))
                {
                    dataQueue.Enqueue(new DataItem { Data = receivedData, Priority = priority });
                }
                else
                {
                    Console.WriteLine("Invalid priority value. Please enter an integer.");
                }
            }
        }

        // Ожидание завершения работы сервера при нажатии Ctrl+C
        Console.WriteLine("Press Ctrl+C to stop the server.");
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true; // отменяем завершение приложения
            cts.Cancel(); // отменяем выполнение задачи
        };
        await Task.Delay(-1); // ждем, пока не будет нажата Ctrl+C
    }
}

// Класс для представления данных с приоритетом
class DataItem : IComparable<DataItem>
{
    public Ad Data { get; set; }
    public int Priority { get; set; }

    public int CompareTo(DataItem other)
    {
        // Сравнение по приоритету (меньший приоритет - выше)
        return Priority.CompareTo(other.Priority);
    }
}

// Очередь с приоритетами
class PriorityQueue<T> where T : IComparable<T>
{
    private readonly SortedSet<T> _set = new SortedSet<T>();

    public void Enqueue(T item)
    {
        _set.Add(item);
    }

    public bool TryDequeue(out T item)
    {
        if (_set.Count > 0)
        {
            item = _set.Min;
            _set.Remove(item);
            return true;
        }

        item = default;
        return false;
    }
}
