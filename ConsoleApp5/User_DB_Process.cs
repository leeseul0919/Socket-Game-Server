using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    public class CharacterInfo
    {
        public string Nickname { get; set; }
        public int SceneNum { get; set; }
        public double XPosition { get; set; }
        public double YPosition { get; set; }
        public int Semester { get; set; }
        public int MainQuestNum { get; set; }
        public int DetailQuestNum { get; set; }
        public int QuestState { get; set; }
    }
    class User_DB_Process
    {
        NetworkService server_network;
        string connection_string;
        public User_DB_Process(NetworkService server_network)
        {
            try
            {
                this.server_network = server_network;
                connection_string = server_network.getConnectionString();
                Console.WriteLine("데이터베이스 연결 객체가 성공적으로 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 연결 객체 초기화 실패 : " + ex.Message);
            }
        }
        // 유저 추가 LoginST는 DEFALUT로
        public void addUser(string email, string password, string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string addUserQuery = "INSERT INTO User_info (Email, PW, Nickname) VALUES (@Email, @Password, @Nickname)";
                    using (MySqlCommand cmd = new MySqlCommand(addUserQuery, connection)) // 새로운 연결 사용
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", password);
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("회원가입 실패 : " + ex.Message);
                }
            }
        }

        // 캐릭터 생성
        public void addCharactor(string nickname, int scene_num, double x_position, double y_position, int semester, int main_quest_num, int detail_quest_num, int quest_state)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string addCharactorQuery = "INSERT INTO User_Character (Nickname, scene_num, x_position, y_position, Semester, Main_Quest_Num, Detail_Quest_Num, Quest_State) " + "VALUES (@Nickname, @SceneNum, @XPosition, @YPosition, @Semester, @MainQuestNum, @DetailQuestNum, @QuestState)";
                    using (MySqlCommand cmd = new MySqlCommand(addCharactorQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        cmd.Parameters.AddWithValue("@SceneNum", scene_num);
                        cmd.Parameters.AddWithValue("@XPosition", x_position);
                        cmd.Parameters.AddWithValue("@YPosition", y_position);
                        cmd.Parameters.AddWithValue("@Semester", semester);
                        cmd.Parameters.AddWithValue("@MainQuestNum", main_quest_num);
                        cmd.Parameters.AddWithValue("@DetailQuestNum", detail_quest_num);
                        cmd.Parameters.AddWithValue("@QuestState", quest_state);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("캐릭터 생성 실패 : " + ex.Message);
                }
            }
        }

        // 유저 로그인
        public bool userCheck(string email, string password)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string userCheckQuery = "SELECT COUNT(*) FROM User_info WHERE Email = @Email AND PW = @Password";
                    using (MySqlCommand cmd = new MySqlCommand(userCheckQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", password);
                        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("유저확인 실패 : " + ex.Message);
                    return false;
                }
            }
        }

        // 사용자 정보 가져오기
        public string getUserInfo(string email)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string getUserInfoQuery = "SELECT Nickname FROM User_info WHERE Email = @Email";
                    using (MySqlCommand cmd = new MySqlCommand(getUserInfoQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        return (string)cmd.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("사용자 정보 가져오기 실패: " + ex.Message);
                    return "";
                }
            }
        }



        // 캐릭터 정보 가져오기
        public CharacterInfo GetCharacterInfo(string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string query = "SELECT * FROM User_Character WHERE Nickname = @Nickname";
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new CharacterInfo
                                {
                                    Nickname = reader.GetString("Nickname"),
                                    SceneNum = reader.GetInt32("scene_num"),
                                    XPosition = reader.GetDouble("x_position"),
                                    YPosition = reader.GetDouble("y_position"),
                                    Semester = reader.GetInt32("Semester"),
                                    MainQuestNum = reader.GetInt32("Main_Quest_Num"),
                                    DetailQuestNum = reader.GetInt32("Detail_Quest_Num"),
                                    QuestState = reader.GetInt32("Quest_State")
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("캐릭터 정보 불러오기 실패 : " + ex.Message);
                }
            }
            return null; // 해당 닉네임의 캐릭터가 없는 경우
        }

        // 사용자 로그인 정보 업데이트
        public void setUserLoginST(string email, int LoginST)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string updateUserLoginSTQuery = "UPDATE User_info SET LoginST = @LoginST WHERE email = @Email";
                    using (MySqlCommand cmd = new MySqlCommand(updateUserLoginSTQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@LoginST", LoginST);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("사용자 로그인 정보 업데이트 실패 : " + ex.Message);
                }
            }
        }


        // 캐릭터 정보 좌표 업데이트
        public void setCharacterInfo(string nickname, int scene_num, double x_position, double y_position)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string)) // 새로운 연결 생성
            {
                connection.Open(); // 필요할 때마다 연결 열기
                try
                {
                    string updateCharacterQuery = "UPDATE User_Character SET " +
                    "scene_num = @SceneNum, " +
                    "x_position = @XPosition, " +
                    "y_position = @YPosition " +
                    "WHERE Nickname = @Nickname";

                    using (MySqlCommand cmd = new MySqlCommand(updateCharacterQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        cmd.Parameters.AddWithValue("@SceneNum", scene_num);
                        cmd.Parameters.AddWithValue("@XPosition", x_position);
                        cmd.Parameters.AddWithValue("@YPosition", y_position);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("캐릭터 좌표 업데이트 실패 : " + ex.Message);
                }
            }
        }

        // 퀘스트 정보 업데이트
        // 퀘스트 데이터가 없을 시 오류 발생
        public void setCharacterQuest(string nickname, int main_quest_num, int detail_quest_num, int quest_state)
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
                    // 데이터 존재 여부 확인
                    string checkExistence = "SELECT COUNT(*) FROM User_Character WHERE Nickname = @Nickname";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkExistence, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@Nickname", nickname);
                        object result = checkCmd.ExecuteScalar();

                        if (result != null && int.TryParse(result.ToString(), out int count) && count > 0)
                        {
                            // 데이터가 존재할 경우 업데이트
                            string updateQuestInfo = "UPDATE User_Character SET Main_Quest_Num = @MainQuestNum, Detail_Quest_Num = @DetailQuestNum, Quest_State = @QuestState WHERE Nickname = @Nickname";
                            using (MySqlCommand cmd = new MySqlCommand(updateQuestInfo, connection))
                            {
                                cmd.Parameters.AddWithValue("@Nickname", nickname);
                                cmd.Parameters.AddWithValue("@MainQuestNum", main_quest_num);
                                cmd.Parameters.AddWithValue("@DetailQuestNum", detail_quest_num);
                                cmd.Parameters.AddWithValue("@QuestState", quest_state);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            Console.WriteLine("해당 닉네임의 데이터가 존재하지 않습니다.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("퀘스트 정보 설정 중 오류 발생 : " + ex.Message);
                    Console.WriteLine("스택 트레이스 : " + ex.StackTrace);
                }
            }
        }
    }
}
