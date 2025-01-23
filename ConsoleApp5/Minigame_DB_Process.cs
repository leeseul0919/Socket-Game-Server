using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace ConsoleApp5
{
    class Minigame_DB_Process
    {
        private Game_Manager gameManager;
        NetworkService server_network;
        string connection_string;

        public Minigame_DB_Process(NetworkService server_network)
        {
            this.server_network = server_network;
            connection_string = server_network.getConnectionString();
        }

        // 미니게임 생성
        public void addMinigame(string nickanme)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string addMinigameQuery = "INSERT INTO MinigameRecord (Nickname, Minigame_Num, score) VALUES (@Nickname, @MinigameNum, @Score)";
                    using (MySqlCommand cmd = new MySqlCommand(addMinigameQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickanme);
                        cmd.Parameters.AddWithValue("@MinigameNum", 1);
                        cmd.Parameters.AddWithValue("@Score", 0);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("미니게임 정보 생성 실패 : " + ex.Message);
                }
            }
        }

        // 미니게임 기록을 업데이트하는 메서드
        public void UpdateMinigameRecord(string nickname, int minigameNum, double subQuestScore)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                Console.WriteLine("닉네임이 null이거나 비어 있습니다.");
                return;
            }
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string query = "SELECT score FROM MinigameRecord WHERE Nickname = @Nickname AND Minigame_Num = @MinigameNum";
                    double? existingScore = null;

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        cmd.Parameters.AddWithValue("@MinigameNum", minigameNum);
                        existingScore = cmd.ExecuteScalar() as double?;
                    }

                    if (existingScore.HasValue) // 이미 기록이 있을 때
                    {
                        if (existingScore.Value < subQuestScore) // 새로운 점수가 더 높을 경우
                        {
                            string updateQuery = "UPDATE MinigameRecord SET score = @NewScore WHERE Nickname = @Nickname AND Minigame_Num = @MinigameNum";
                            using (MySqlCommand cmd = new MySqlCommand(updateQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@NewScore", subQuestScore);
                                cmd.Parameters.AddWithValue("@Nickname", nickname);
                                cmd.Parameters.AddWithValue("@MinigameNum", minigameNum);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else // 기록이 없을 때
                    {
                        string insertQuery = "INSERT INTO MinigameRecord (Nickname, Minigame_Num, score) VALUES (@Nickname, @MinigameNum, @Score)";
                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@Nickname", nickname);
                            cmd.Parameters.AddWithValue("@MinigameNum", minigameNum);
                            cmd.Parameters.AddWithValue("@Score", subQuestScore);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("미니게임 기록 업데이트 실패 : " + ex.Message);
                }
            }
        }
        // minigame 기록 호출
        // 미니게임 기록을 가져오는 메서드
        public (List<int> minigame_nums, List<double> scores) GetMinigameRecord(string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    List<int> minigame_nums = new List<int>();
                    List<double> scores = new List<double>();

                    string minigameRecordQuery = "SELECT Minigame_Num, score FROM MinigameRecord WHERE Nickname = @Nickname";
                    using (MySqlCommand cmd = new MySqlCommand(minigameRecordQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                minigame_nums.Add(reader.GetInt32("Minigame_num"));  // 미니게임 번호 저장
                                scores.Add(reader.GetDouble("score"));               // 점수 저장
                            }
                        }
                    }

                    // 미니게임 번호 리스트와 점수 리스트를 튜플로 반환
                    return (minigame_nums, scores);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("미니게임 기록 호출 실패 : " + ex.Message);
                    return (null, null);
                }
            }
        }
    }
}
