using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Client
{
    class Program
    {
        public struct Ad
        {
            public int X;
            public int Y;
            public bool DA;
        }

        static async Task Main()
        {
            Console.WriteLine("Соединяю с сервером...\n");

            using (var clientPipe = new NamedPipeClientStream(".", "tonel", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await clientPipe.ConnectAsync();

                Console.WriteLine("Соединение установлено, отправляем данные...\n");

                Ad dataToSend = new Ad
                {
                    X = 42,
                    Y = 24,
                    DA = false
                };

                Console.WriteLine($"Отправлены данные: X={dataToSend.X}, Y={dataToSend.Y}, DA={dataToSend.DA}\n");

                // Отправляем данные на сервер
                byte[] buffer = new byte[Marshal.SizeOf<Ad>()];
                MemoryMarshal.Write(buffer, ref dataToSend);
                await clientPipe.WriteAsync(buffer, 0, buffer.Length);

                // Ждем ответ от сервера
                buffer = new byte[Marshal.SizeOf<Ad>()];
                await clientPipe.ReadAsync(buffer, 0, buffer.Length);

                Ad receivedData = MemoryMarshal.Read<Ad>(buffer);

                Console.WriteLine($"Получены данные от сервера: X={receivedData.X}, Y={receivedData.Y}, DA={receivedData.DA}\n");

                // Сохраняем полученные данные в файл или выводим на экран
                Console.WriteLine("Выберите действие: \n1. Вывести на экран\n2. Сохранить в файл");
                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    Console.WriteLine($"Полученные данные: X={receivedData.X}, Y={receivedData.Y}, DA={receivedData.DA}\n");
                }
                else if (choice == "2")
                {
                    Console.Write("Введите имя файла для сохранения: ");
                    var fileName = Console.ReadLine();
                    File.WriteAllText(fileName, $"Полученные данные: X={receivedData.X}, Y={receivedData.Y}, DA={receivedData.DA}\n");
                    Console.WriteLine($"Данные сохранены в файле: {fileName}\n");
                }
                else
                {
                    Console.WriteLine("Некорректный выбор.");
                }
            }

            Console.WriteLine("Клиент завершил работу.");
        }
    }
}
