//Client.cs
namespace Client
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Microshaoft;
    class Class1
    {
        static void Main(string[] args)
        {
            Console.Title = "Client";
            var ipa = IPAddress.Parse("127.0.0.1");
            var socket = new Socket
                                (
                                    AddressFamily.InterNetwork
                                    , SocketType.Stream
                                    , ProtocolType.Tcp
                                );
            var ipep = new IPEndPoint(ipa, 18180);
            socket.Connect(ipep);
            var handler = new SocketAsyncDataHandler<string>
                                                        (
                                                            socket
                                                            , 1
                                                        );
            var sendEncoding = Encoding.Default;
            var receiveEncoding = Encoding.UTF8;
            receiveEncoding = Encoding.Default;
            var decoder = receiveEncoding.GetDecoder();
            handler.StartReceiveData
                            (
                                //1024 * 8
                                10
                                , (x, y, z) =>
                                {
                                    var l = decoder.GetCharCount(y, 0, y.Length);
                                    var chars = new char[l];
                                    decoder.GetChars(y, 0, y.Length, chars, 0, false);
                                    var s = new string(chars);
                                    Console.Write("[{0}]", s);
                                    return true;
                                }
                            );
            char c;
            while ('q' != (c = (Console.ReadKey().KeyChar)))
            {
                handler.SendDataSync(new[] { (byte)c });
            }
            return;
            string input = string.Empty;
            while ((input = Console.ReadLine()) != "q")
            {
                try
                {
                    var buffer = sendEncoding.GetBytes(input);
                    Array.ForEach
                            (
                                buffer
                                , (x) =>
                                {
                                    handler.SendDataSync(new[] { x });
                                    //Thread.Sleep(100);
                                }
                            );
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}