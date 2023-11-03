using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Linq;

namespace Server
{
    class Program
    {
        public struct Ad
        {
            public int X;
            public int Y;
            public bool DA;
        }
        
        static PriorityQueue<Ad>
        dataQueue=new PQueue<Ad>((x,y)=>X.x-Y.y);
    
        static object daraQueueLock=new object();

        static List<Ad> = new List<Ad>();
        static object bufferLock=new object();
        static  CancellationTokenSource cts=new CancellationTokenSource();

        static async Task Main()
        {
            using(HASANAOEV)
            Console.WriteLine("Ожидание клиента...\n");

            using (var serverPipe = new NamedPipeServerStream("tonel", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                await serverPipe.WaitForConnectionAsync();

                Console.WriteLine("Клиент подключен.\n");

                Ad receivedData = ReceiveData(serverPipe);
                Console.WriteLine($"Получены данные от клиента: X={receivedData.X}, Y={receivedData.Y}, DA={receivedData.DA}\n");

                receivedData.X += receivedData.Y;
                receivedData.Y -= receivedData.X;
                receivedData.DA = true;

                Console.WriteLine($"Отправляем данные клиенту: X={receivedData.X}, Y={receivedData.Y}, dA={receivedData.DA}\n");
                SendData(serverPipe, receivedData);
            }

            Console.WriteLine("Сервер завершил работу.");
        }
        static Ad ReceiveData(NamedPipeServerStream pipe)
        {
            byte[] buffer = new byte[Marshal.SizeOf<Ad>()];
            pipe.Read(buffer, 0, buffer.Length);
            return MemoryMarshal.Read<Ad>(buffer);
        }

        static void SendData(NamedPipeServerStream pipe, Ad data)
        {
            byte[] buffer = new byte[Marshal.SizeOf<Ad>()];
            MemoryMarshal.Write(buffer, ref data);
            pipe.Write(buffer, 0, buffer.Length);
        }

        
    }
}