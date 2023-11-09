using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    public struct Ad
    {
        public int X;
        public int Y;
        public bool DA;
        public override string ToString() => $"Данные = {X}, Ответ = {DA}";
    }

    static readonly Queue<Ad> dataQueue = new Queue<Ad>();
    static readonly Queue<Ad> highPriorityQueue = new Queue<Ad>();
    static readonly object dataQueueLock = new object();
    static readonly object bufferLock = new object();
    static CancellationTokenSource cts = new CancellationTokenSource();
    static List<Ad> dataBuffer = new List<Ad>();

    static async Task Main()
    {
        Console.WriteLine("Ожидание клиента...\n");

        using (var serverPipe = new NamedPipeServerStream("tonel", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
        {
            // Запускаем задачу для асинхронного чтения данных
            var readingTask = ReadDataAsync(serverPipe);

            // Запускаем задачу для обработки данных и отправки ответа
            var processingTask = ProcessDataAsync(serverPipe);

            // Запускаем задачу для обработки Ctrl+C
            var ctrlCTask = HandleCtrlCAsync();

            // Ждем, пока пользователь нажмет Ctrl+C
            Console.WriteLine("Нажмите Ctrl+C для завершения.");
            Console.CancelKeyPress += (s, e) =>
            {
                cts.Cancel();
                e.Cancel = true; // Предотвратить закрытие консоли
            };
            cts.Token.WaitHandle.WaitOne();

            // Ожидаем завершения задач чтения, обработки данных и Ctrl+C
            await Task.WhenAll(readingTask, processingTask, ctrlCTask);

            // Выводим буфер данных на экран или записываем в файл
            lock (bufferLock)
            {
                foreach (var item in dataBuffer)
                {
                    Console.WriteLine($"Сохраненные данные: X={item.X}, Y={item.Y}, DA={item.DA}");
                }
            }
        }

        Console.WriteLine("Сервер завершил работу.");
    }

    static async Task ReadDataAsync(NamedPipeServerStream pipe)
    {
        while (true)
        {
            var buffer = new byte[Marshal.SizeOf<Ad>()];
            await pipe.ReadAsync(buffer, 0, buffer.Length);
            var receivedData = MemoryMarshal.Read<Ad>(buffer);

            // Добавляем данные в очередь или очередь с высоким приоритетом
            if (receivedData.DA)
            {
                lock (dataQueueLock)
                {
                    highPriorityQueue.Enqueue(receivedData);
                }
            }
            else
            {
                lock (dataQueueLock)
                {
                    dataQueue.Enqueue(receivedData);
                }
            }
        }
    }

    static async Task ProcessDataAsync(NamedPipeServerStream pipe)
    {
        while (true)
        {
            Ad dataToSend;

            // Извлекаем данные из очереди с высоким приоритетом, если она не пуста
            lock (dataQueueLock)
            {
                if (highPriorityQueue.Count > 0)
                {
                    dataToSend = highPriorityQueue.Dequeue();
                }
                else if (dataQueue.Count > 0)
                {
                    dataToSend = dataQueue.Dequeue();
                }
                else
                {
                    continue; // Ожидаем, если обе очереди пусты
                }
            }

            dataToSend.X += dataToSend.Y;
            dataToSend.Y -= dataToSend.X;
            dataToSend.DA = true;

            // Сохраняем данные в буфер
            lock (bufferLock)
            {
                dataBuffer.Add(dataToSend);
            }

            // Отправляем данные клиенту
            var buffer = new byte[Marshal.SizeOf<Ad>()];
            MemoryMarshal.Write(buffer, ref dataToSend);
            await pipe.WriteAsync(buffer, 0, buffer.Length);
        }
    }

    static async Task HandleCtrlCAsync()
    {
        await Task.Delay(-1, cts.Token); // Ожидание нажатия Ctrl+C
    }
}
