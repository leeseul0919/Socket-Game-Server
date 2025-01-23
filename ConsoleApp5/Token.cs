using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace ConsoleApp5
{
    class Token
    {
        public Socket socket { get; set; }
        public SocketAsyncEventArgs receive_event_args { get; private set; }
        public SocketAsyncEventArgs send_event_args { get; private set; }

        public delegate void ClosedDelegate(Token token);
        public ClosedDelegate on_session_closed;

        public string client_ID;
        public string client_nickname;
        int is_closed;
        public int scene_num;

        private MemoryStream messageStream = new MemoryStream();
        private int expectedMessageLength = -1;

        public List<string> friend_nickname_list = new List<string>();
        public Token(string client_ID)
        {
            this.client_ID = client_ID;
        }
        public void set_event_args(SocketAsyncEventArgs send_args, SocketAsyncEventArgs receive_args)
        {
            this.send_event_args = send_args;
            this.receive_event_args = receive_args;
        }


        public void token_close()
        {
            Console.WriteLine("token close");
            if (Interlocked.CompareExchange(ref this.is_closed, 1, 0) == 1)
            {
                return;
            }
            this.socket.Close();
            this.socket = null;
            this.send_event_args = null;
            this.receive_event_args = null;
        }

        public void AppendData(byte[] data, int bytesReceived)
        {
            messageStream.Write(data, 0, bytesReceived);
        }
        public bool HasMessageLength => expectedMessageLength != -1;
        public bool TryReadMessageLength()
        {
            if (expectedMessageLength == -1 && messageStream.Length >= 4)
            {
                messageStream.Position = 0; // 스트림의 처음으로 이동
                byte[] lengthBytes = new byte[4];
                messageStream.Read(lengthBytes, 0, 4);
                expectedMessageLength = BitConverter.ToInt32(lengthBytes, 0);

                // 메시지 길이만큼의 공간 확보를 위해 스트림 리셋
                MemoryStream tempStream = new MemoryStream();
                tempStream.Write(messageStream.GetBuffer(), 4, (int)messageStream.Length - 4);
                messageStream = tempStream;
                return true;
            }
            return false;
        }

        public string GetCompleteMessage()
        {
            if (expectedMessageLength > 0 && messageStream.Length >= expectedMessageLength)
            {
                messageStream.Position = 0; // 스트림의 처음으로 이동
                byte[] messageBytes = new byte[expectedMessageLength];
                messageStream.Read(messageBytes, 0, expectedMessageLength);

                // 다음 메시지를 위해 스트림 리셋
                MemoryStream tempStream = new MemoryStream();
                tempStream.Write(messageStream.GetBuffer(), expectedMessageLength, (int)messageStream.Length - expectedMessageLength);
                messageStream = tempStream;

                expectedMessageLength = -1; // 다음 메시지를 위해 길이 리셋
                return System.Text.Encoding.UTF8.GetString(messageBytes);
            }
            return null;
        }

        public void update_friend_nick_list(int i, string nick)
        {
            if(i==0)
            {
                friend_nickname_list.Remove(nick);
            }
            else
            {
                friend_nickname_list.Add(nick);
            }
        }
    }
}
