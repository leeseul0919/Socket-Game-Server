using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    class Friend_DB_Process
    {
        NetworkService server_network;
        string connection_string;
        public Friend_DB_Process(NetworkService server_network)
        {
            this.server_network = server_network;
            connection_string = server_network.getConnectionString();
        }
        // 친구 삭제 기능 -> 친구 테이블에 Friend1, Friend2 찾아서 친구 튜플 삭제
        public void deleteFriend(string nickname, string friendNickname)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();
                try
                {
                    string deleteFriendQuery = "DELETE FROM Friends WHERE (Friend1 = @Nickname AND Friend2 = @Friend) OR (Friend1 = @Friend AND Friend2 = @Nickname)";
                    using (MySqlCommand cmd = new MySqlCommand(deleteFriendQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        cmd.Parameters.AddWithValue("@Friend", friendNickname);
                        cmd.ExecuteNonQuery();
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine("친구 삭제 기능 쿼리 오류 : " + ex.Message);
                }
            }
        }

        // 친구 요청 수락 기능 
        public void acceptFriendRequest(string friend1, string friend2)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // DB에 친구 상태로 추가 SQL
                        string addFriend = "INSERT INTO Friends (Friend1, Friend2) VALUES (@Friend1, @Friend2)";
                        using (MySqlCommand cmd = new MySqlCommand(addFriend, connection))
                        {
                            cmd.Parameters.AddWithValue("@Friend1", friend1);
                            cmd.Parameters.AddWithValue("@Friend2", friend2);
                            cmd.ExecuteNonQuery();
                        }

                        // 친구 요청 상태 업데이트 -> 친구 상태가 되면 친구 요청 튜플 삭제
                        string deleteFriendReqSts = "DELETE FROM FriendRequest WHERE Friend1 = @Friend1 AND Friend2 = @Friend2";
                        using (MySqlCommand cmd = new MySqlCommand(deleteFriendReqSts, connection))
                        {
                            cmd.Parameters.AddWithValue("@Friend1", friend1);
                            cmd.Parameters.AddWithValue("@Friend2", friend2);
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("친구 요청 수락 기능 쿼리 오류 : " + ex.Message);
                    }
                }
            }
        }

        // 친구 요청 거절 기능
        public void rejectFriendRequest(string friend1, string friend2)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                try
                {
                    // 친구 요청 상태를 거절로 업데이트 
                    string updateFriendStatus = "UPDATE FriendRequest SET status = 3 WHERE Friend1 = @Friend1 AND Friend2 = @Friend2";
                    using (MySqlCommand cmd = new MySqlCommand(updateFriendStatus, connection))
                    {
                        cmd.Parameters.AddWithValue("@Friend1", friend1);
                        cmd.Parameters.AddWithValue("@Friend2", friend2);
                        //int rowsAffected = cmd.ExecuteNonQuery();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("친구 요청 거절 기능 쿼리 오류 : " + ex.Message);
                }
            }
        }
        // 친구 요청 보내기
        public void SendFriendRequest(string friend1, string friend2)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                // 현재 상태를 확인하기 위한 쿼리
                string checkFriendReqQuery = "SELECT status FROM FriendRequest WHERE Friend1 = @Friend1 AND Friend2 = @Friend2";

                using (MySqlCommand checkCmd = new MySqlCommand(checkFriendReqQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@Friend1", friend1);
                    checkCmd.Parameters.AddWithValue("@Friend2", friend2);
                    object statusObj = checkCmd.ExecuteScalar();

                    if (statusObj != null)
                    {
                        int currentStatus = Convert.ToInt32(statusObj);
                        if (currentStatus == 1)
                        {
                            Console.WriteLine("이미 친구 요청이 존재합니다.");
                            return; // 이미 친구 요청이 존재하는 경우
                        }
                        else
                        {
                            // 기존 요청이 거절된 경우 상태를 업데이트
                            string updateStatusQuery = "UPDATE FriendRequest SET status = 1 WHERE Friend1 = @Friend1 AND Friend2 = @Friend2";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateStatusQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@Friend1", friend1);
                                updateCmd.Parameters.AddWithValue("@Friend2", friend2);
                                updateCmd.ExecuteNonQuery();
                            }
                            Console.WriteLine("거절당한 요청을 다시 보내기로 상태를 업데이트합니다.");
                            return;
                        }
                    }
                    else
                    {
                        string insertRequestQuery = "Insert INTO FriendRequest Values (1, @Friend1, @Friend2)";
                        using(MySqlCommand insertCmd = new MySqlCommand(insertRequestQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@Friend1", friend1);
                            insertCmd.Parameters.AddWithValue("@Friend2", friend2);
                            insertCmd.ExecuteNonQuery();
                        }
                        Console.WriteLine("새로운 친구 요청 데이터 추가");
                        return;
                    }
                }
            }
        }

        // 친구목록 조회
        public List<string> showFriends(string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                List<string> friends = new List<string>();

                // Friend1과 Friend2 모두에서 친구 목록 가져오기
                string query = "SELECT Friend2 AS Friend FROM Friends WHERE Friend1 = @Nickname UNION SELECT Friend1 AS Friend FROM Friends WHERE Friend2 = @Nickname";

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Nickname", nickname);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                friends.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                return friends;
            }

        }
        // 보낸 친구 요청 목록 불러오기
        public List<string> showFriendRequest(string friend1)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                List<string> showFriend = new List<string>();

                // status가 1인 경우에도 친구 요청을 가져오는 쿼리
                string showFriendReqQuery = "SELECT Friend2 FROM FriendRequest WHERE Friend1 = @Nickname AND status = 1";

                using (MySqlCommand cmd = new MySqlCommand(showFriendReqQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Nickname", friend1);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            showFriend.Add(reader.GetString(0));
                        }
                    }
                }

                return showFriend;
            }
        }
        // 받은 친구 요청 목록 불러오기
        public List<string> showRequestReceive(string friend2)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                List<string> showFriend = new List<string>();

                // status가 1인 경우에도 친구 요청을 가져오는 쿼리
                string showFriendReqQuery = "SELECT Friend1 FROM FriendRequest WHERE Friend2 = @Nickname AND status = 1";

                using (MySqlCommand cmd = new MySqlCommand(showFriendReqQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Nickname", friend2);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            showFriend.Add(reader.GetString(0));
                        }
                    }
                }

                return showFriend;
            }
        }
        public List<String> UserSearchResult(string nick, string owner_nick)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();

                List<string> searchresult = new List<string>();

                string searchQuery = @"
                    SELECT Nickname 
                    FROM User_info 
                    WHERE Nickname LIKE @search_nick
                    AND Nickname NOT IN (
                        SELECT Friend2 FROM Friends WHERE Friend1 = @Nickname
                        UNION
                        SELECT Friend1 FROM Friends WHERE Friend2 = @Nickname
                        UNION
                        SELECT Friend2 FROM FriendRequest WHERE Friend1 = @Nickname AND status = 1
                        UNION
                        SELECT Friend1 FROM FriendRequest WHERE Friend2 = @Nickname AND status = 1
                    )
                    AND Nickname != @Nickname;";

                using (MySqlCommand cmd = new MySqlCommand(searchQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@search_nick", "%" + nick + "%");
                    cmd.Parameters.AddWithValue("@Nickname", owner_nick);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            searchresult.Add(reader.GetString(0));
                        }
                    }
                }

                return searchresult;
            }
        }

        // 친구 아님 : 0 친구 요청 상태 : 1, 친구 상태 : 2, 친구 거절 상태 : 3 
        // 친구 요청 취소 기능  
        public void cancleFriendRequest(string friend1, string friend2)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();
                // 현재 상태를 확인하기 위한 쿼리
                string cancleFriendReqQuery = "SELECT status FROM FriendRequest WHERE Friend1 = @Friend1 AND Friend2 = @Friend2";
                using (MySqlCommand checkCmd = new MySqlCommand(cancleFriendReqQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@Friend1", friend1);
                    checkCmd.Parameters.AddWithValue("@Friend2", friend2);
                    object statusObj = checkCmd.ExecuteScalar();
                    if (statusObj != null)
                    {
                        int currentStatus = Convert.ToInt32(statusObj);
                        if (currentStatus == 1) // 친구 요청 상태일 때
                        {
                            // 친구 요청을 취소하기 위해 status를 0으로 업데이트
                            string updateStatusQuery = "UPDATE FriendRequest SET status = 0 WHERE Friend1 = @Friend1 AND Friend2 = @Friend2";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateStatusQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@Friend1", friend1);
                                updateCmd.Parameters.AddWithValue("@Friend2", friend2);
                                updateCmd.ExecuteNonQuery();
                            }
                            Console.WriteLine("친구 요청이 취소되었습니다.");
                            return;
                        }
                    }
                }
            }
        }
        public int GetFriendCount(string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection(connection_string))
            {
                connection.Open();
                try
                {
                    // nickname이 Friend1이나 Friend2로 존재하는 모든 친구 관계의 수를 계산
                    string countQuery = "SELECT COUNT(*) FROM Friends WHERE Friend1 = @Nickname OR Friend2 = @Nickname";

                    using (MySqlCommand cmd = new MySqlCommand(countQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nickname", nickname);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("친구 수 조회 쿼리 오류 : " + ex.Message);
                    return 0; // 에러 발생 시 0 반환
                }
            }
        }
    }
}
