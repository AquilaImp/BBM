using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "MyPipe", PipeDirection.InOut))
        {
            Console.WriteLine("Подключение к серверу...");
            await pipeClient.ConnectAsync();

            while (true)
            {
                
                Console.Write("Введите данные (Имя Возраст Приоритет): ");
                string input = Console.ReadLine();

                byte[] requestBuffer = Encoding.UTF8.GetBytes(input);
                await pipeClient.WriteAsync(requestBuffer, 0, requestBuffer.Length);

                byte[] responseBuffer = new byte[1024];
                int bytesRead = await pipeClient.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                Console.WriteLine($"Ответ от сервера: {response}");
            }
        }
    }
}
