//===========================================================================================
//Share.cs
namespace Microshaoft
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    public class SocketAsyncDataHandler<TContext>
    {
        private Socket _socket;
        public Socket WorkingSocket
        {
            get
            {
                return _socket;
            }
        }
        public int ReceiveDataBufferLength
        {
            get;
            private set;
        }
        public TContext Context
        {
            get;
            set;
        }
        public IPAddress RemoteIPAddress
        {
            get
            {
                return ((IPEndPoint)_socket.RemoteEndPoint).Address;
            }
        }
        public IPAddress LocalIPAddress
        {
            get
            {
                return ((IPEndPoint)_socket.LocalEndPoint).Address;
            }
        }
        public int SocketID
        {
            get;
            private set;
        }
        public SocketAsyncDataHandler
                            (
                                Socket socket
                                , int socketID
                            )
        {
            _socket = socket;
            _sendSocketAsyncEventArgs = new SocketAsyncEventArgs();
            SocketID = socketID;
        }
        private SocketAsyncEventArgs _sendSocketAsyncEventArgs;
        public int HeaderBytesLength
        {
            get;
            private set;
        }
        public int HeaderBytesOffset
        {
            get;
            private set;
        }
        public int HeaderBytesCount
        {
            get;
            private set;
        }
        private bool _isStartedReceiveData = false;
        private bool _isHeader = true;
        public bool StartReceiveWholeDataPackets
                            (
                                int receiveBufferLength
                                , int headerBytesLength
                                , int headerBytesOffset
                                , int headerBytesCount
                                , Func
                                    <
                                        SocketAsyncDataHandler<TContext>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onOneWholeDataPacketReceivedProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<TContext>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onDataPacketReceivedErrorProcessFunc = null
                                , Action
                                    <
                                        SocketAsyncDataHandler<TContext>
                                        , bool
                                    > onAfterDestoryWorkingSocketProcessAction = null
                            )
        {
            if (!_isStartedReceiveData)
            {
                HeaderBytesLength = headerBytesLength;
                HeaderBytesOffset = headerBytesOffset;
                HeaderBytesCount = headerBytesCount;
                var saeaReceive = new SocketAsyncEventArgs();
                int bodyLength = 0;
                saeaReceive.Completed += new EventHandler<SocketAsyncEventArgs>
                                (
                                    (sender, e) =>
                                    {
                                        var socket = sender as Socket;
                                        if (e.BytesTransferred >= 0)
                                        {
                                            byte[] buffer = e.Buffer;
                                            int r = e.BytesTransferred;
                                            int p = e.Offset;
                                            int l = e.Count;
                                            if (r < l)
                                            {
                                                p += r;
                                                e.SetBuffer(p, l - r);
                                            }
                                            else if (r == l)
                                            {
                                                if (_isHeader)
                                                {
                                                    byte[] data = new byte[headerBytesCount];
                                                    Buffer.BlockCopy
                                                                (
                                                                    buffer
                                                                    , HeaderBytesOffset
                                                                    , data
                                                                    , 0
                                                                    , data.Length
                                                                );
                                                    byte[] intBytes = new byte[4];
                                                    l = (intBytes.Length < HeaderBytesCount ? intBytes.Length : HeaderBytesCount);
                                                    Buffer.BlockCopy
                                                                (
                                                                    data
                                                                    , 0
                                                                    , intBytes
                                                                    , 0
                                                                    , l
                                                                );
                                                    //Array.Reverse(intBytes);
                                                    bodyLength = BitConverter.ToInt32(intBytes, 0);
                                                    p += r;
                                                    e.SetBuffer(p, bodyLength);
                                                    Console.WriteLine(bodyLength);
                                                    _isHeader = false;
                                                }
                                                else
                                                {
                                                    byte[] data = new byte[bodyLength + HeaderBytesLength];
                                                    bodyLength = 0;
                                                    Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                    _isHeader = true;
                                                    e.SetBuffer(0, HeaderBytesLength);
                                                    if (onOneWholeDataPacketReceivedProcessFunc != null)
                                                    {
                                                        onOneWholeDataPacketReceivedProcessFunc
                                                                                        (
                                                                                            this
                                                                                            , data
                                                                                            , e
                                                                                        );
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (onDataPacketReceivedErrorProcessFunc != null)
                                                {
                                                    byte[] data = new byte[p + r + HeaderBytesLength];
                                                    Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                    bool b = onDataPacketReceivedErrorProcessFunc
                                                                                    (
                                                                                        this
                                                                                        , data
                                                                                        , e
                                                                                    );
                                                    if (b)
                                                    {
                                                        bool i = DestoryWorkingSocket();
                                                        if (onAfterDestoryWorkingSocketProcessAction != null)
                                                        {
                                                            onAfterDestoryWorkingSocketProcessAction(this, i);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _isHeader = true;
                                                        e.SetBuffer(0, HeaderBytesLength);
                                                    }
                                                }
                                            }
                                        }
                                        try
                                        {
                                            socket.ReceiveAsync(e);
                                        }
                                        catch (Exception exception)
                                        {
                                            Console.WriteLine(exception.ToString());
                                            DestoryWorkingSocket();
                                        }
                                    }
                                );
                ReceiveDataBufferLength = receiveBufferLength;
                saeaReceive.SetBuffer
                                (
                                    new byte[ReceiveDataBufferLength]
                                    , 0
                                    , HeaderBytesLength
                                );
                _socket.ReceiveAsync(saeaReceive);
                _isStartedReceiveData = true;
            }
            return _isStartedReceiveData;
        }
        public bool DestoryWorkingSocket()
        {
            bool r = false;
            try
            {
                if (_socket.Connected)
                {
                    _socket.Disconnect(false);
                }
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socket.Dispose();
                _socket = null;
                r = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                //r = false;
            }
            return r;
        }
        public bool StartReceiveData
                            (
                                int receiveBufferLength
                                , Func
                                    <
                                        SocketAsyncDataHandler<TContext>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onDataReceivedProcessFunc
                            )
        {
            if (!_isStartedReceiveData)
            {
                var saeaReceive = new SocketAsyncEventArgs();
                saeaReceive.Completed += new EventHandler<SocketAsyncEventArgs>
                                                (
                                                    (sender, e) =>
                                                    {
                                                        var socket = sender as Socket;
                                                        int l = e.BytesTransferred;
                                                        if (l > 0)
                                                        {
                                                            byte[] data = new byte[l];
                                                            var buffer = e.Buffer;
                                                            Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                            if (onDataReceivedProcessFunc != null)
                                                            {
                                                                onDataReceivedProcessFunc(this, data, e);
                                                            }
                                                        }
                                                        try
                                                        {
                                                            socket.ReceiveAsync(e);
                                                        }
                                                        catch (Exception exception)
                                                        {
                                                            Console.WriteLine(exception.ToString());
                                                        }
                                                    }
                                                );
                ReceiveDataBufferLength = receiveBufferLength;
                saeaReceive.SetBuffer
                                (
                                    new byte[ReceiveDataBufferLength]
                                    , 0
                                    , ReceiveDataBufferLength
                                );
                _socket.ReceiveAsync(saeaReceive);
                _isStartedReceiveData = true;
            }
            return _isStartedReceiveData;
        }
        private object _sendSyncLockObject = new object();
        public int SendDataSync(byte[] data)
        {
            lock (_sendSyncLockObject)
            {
                return _socket.Send(data);
            }
        }
    }
}