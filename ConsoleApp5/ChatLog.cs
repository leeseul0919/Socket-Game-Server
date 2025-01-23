using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    class ChatLog
    {
        NetworkService server_network;
        string connection_string;
        public ChatLog(NetworkService server_network)
        {
            this.server_network = server_network;
            connection_string = server_network.getConnectionString();
        }

        public void recordChatLog(string nickname, int room_num, string message, string receiver)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    DateTime messageLog = DateTime.Now;

                    string recordChatLogQuery = "INSERT INTO Chat_Log (Nickname, Receiver, Room_Num, message, message_Log) VALUES (@Nickname, @Receiver, @RoomNum, @Message, @MessageLog)";
                    using (MySqlCommand cmd = new MySqlCommand(recordChatLogQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        cmd.Parameters.AddWithValue("@Receiver", receiver);
                        cmd.Parameters.AddWithValue("@RoomNum", room_num);
                        cmd.Parameters.AddWithValue("@Message", message);
                        cmd.Parameters.AddWithValue("@MessageLog", messageLog); // DateTime 객체를 추가
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("MySQL 오류 발생: " + e.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("일반 오류 발생: " + ex.Message);
                    Console.WriteLine("스택 트레이스 : " + ex.StackTrace);
                }
            }
        }
    }
}
