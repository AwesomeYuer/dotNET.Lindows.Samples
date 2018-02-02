//Server.cs
namespace Server
{
    using System;
    using System.Net;
    using System.Text;
    public class Class1
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        //[STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Server";
            IPAddress ipa;
            IPAddress.TryParse("127.0.0.1", out ipa);
            var receiveEncoding = Encoding.Default;
            var sendEncoding = Encoding.UTF8;
            sendEncoding = Encoding.Default;
            var decoder = receiveEncoding.GetDecoder();
            var es = new EchoServer<string>
                            (
                                new IPEndPoint(ipa, 18180)
                                , (x, y) =>
                                {
                                    var l = decoder.GetCharCount(y, 0, y.Length);
                                    var chars = new char[l];
                                    decoder.GetChars(y, 0, y.Length, chars, 0, false);
                                    var s = new string(chars);
                                    Console.Write(s);
                                    //s = string.Format("Echo: {0}{1}{0}", "\r\n", s);
                                    var buffer = sendEncoding.GetBytes(s);
                                    x.SendDataSync(buffer);
                                }
                            );
            Console.WriteLine("Hello World");
            Console.WriteLine(Environment.Version.ToString());
            Console.ReadLine();
        }
    }
}

namespace Server
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Microshaoft;
    class EchoServer<T>
    {
        //Socket _socketListener;
        private Action<SocketAsyncDataHandler<T>, byte[]> _onReceivedDataProcessAction;
        public EchoServer
                    (
                        IPEndPoint localPoint
                        , Action
                            <
                                SocketAsyncDataHandler<T>
                                , byte[]
                            >
                            onReceivedDataProcessAction
                    )
        {
            _onReceivedDataProcessAction = onReceivedDataProcessAction;
            var listener = new Socket
                            (
                                localPoint.AddressFamily
                                , SocketType.Stream
                                , ProtocolType.Tcp
                            );
            listener.Bind(localPoint);
            listener.Listen(5);
            AcceptSocketAsyc(listener);
        }
        private void AcceptSocketAsyc(Socket listener)
        {
            var acceptSocketAsyncEventArgs = new SocketAsyncEventArgs();
            acceptSocketAsyncEventArgs.Completed += acceptSocketAsyncEventArgs_AcceptOneCompleted;
            listener.AcceptAsync(acceptSocketAsyncEventArgs);
        }
        private int _socketID = 0;
        void acceptSocketAsyncEventArgs_AcceptOneCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= acceptSocketAsyncEventArgs_AcceptOneCompleted;
            var client = e.AcceptSocket;
            var listener = sender as Socket;
            AcceptSocketAsyc(listener);
            var handler = new SocketAsyncDataHandler<T>
                                                        (
                                                            client
                                                            , _socketID++
                                                        );
            handler.StartReceiveData
                        (
                            1
                            , (x, y, z) =>
                            {
                                //var s = Encoding.UTF8.GetString(y);
                                ////Console.WriteLine("SocketID: {1}{0}Length: {2}{0}Data: {2}", "\r\n", x.SocketID, y.Length ,s);
                                //Console.Write(s);
                                if (_onReceivedDataProcessAction != null)
                                {
                                    _onReceivedDataProcessAction(x, y);
                                }
                                return true;
                            }
                        );
            //handler.StartReceiveWholeDataPackets
            //					(
            //						1024 * 1024
            //						, 2
            //						, 0
            //						, 2
            //						, (x, y, z) =>
            //						{
            //							var s = Encoding.UTF8.GetString(y);
            //							//Console.WriteLine("SocketID: {1}{0}Length: {2}{0}Data: {2}", "\r\n", x.SocketID, y.Length ,s);
            //							Console.Write(s);
            //							return true;
            //						}
            //					);
        }
    }
}
