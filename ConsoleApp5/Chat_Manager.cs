using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;

namespace ConsoleApp5
{
    class Chat_Manager
    {
        private ChatLog chatManager;

        class chat_queue_info
        {
            public Token owner_info { get; set; }
            public message chat_message { get; set; }
        }
        Queue<chat_queue_info> chat_message_queue;
        NetworkService server_network;
        int member_maximum = 4;
        class group_chat_room
        {
            int member_maximum = 4;
            public int room_num { get; set; }
            public Dictionary<int, Token> Members { get; set; } = new Dictionary<int, Token>();

            public group_chat_room(int roomNumber, Token creator)
            {
                room_num = roomNumber;
                Members[1] = creator;
            }

            public int add_member(Token new_member)
            {
                int decide_member_num = 1;
                for (int i = 1; i <= member_maximum; i++)
                {
                    if (!Members.ContainsKey(i)) // 키가 없으면 빈 번호임
                    {
                        Members[i] = new_member;  // 빈 번호로 멤버 추가
                        decide_member_num = i;
                        break;
                    }
                }
                return decide_member_num;
            }
            public void delete_member(int member_num)
            {
                Members.Remove(member_num);
            }

            public List<string> get_members()
            {
                List<string> current_members = new List<string>();

                for (int i = 1; i <= member_maximum; i++)
                {
                    if (Members.ContainsKey(i)) // 해당 키가 존재하는지 확인
                    {
                        current_members.Add(Members[i].client_nickname);
                    }
                    else
                    {
                        current_members.Add(null); // 빈 번호일 경우 null 추가
                    }
                }

                return current_members;
            }
        }

        List<group_chat_room> current_rooms = new List<group_chat_room>();

        int decide_room_num()
        {
            int min = 1;
            foreach(var r in current_rooms)
            {
                if (r.room_num == min) min++;
                else break;
            }
            return min;
        }
        int addGroupChatRoom(Token creator)
        {
            int new_room_num = decide_room_num();
            group_chat_room new_room = new group_chat_room(new_room_num, creator);
            current_rooms.Add(new_room);
            current_rooms.Sort((x, y) => x.room_num.CompareTo(y.room_num));
            return new_room_num;
        }


        public Chat_Manager()
        {
            chat_message_queue = new Queue<chat_queue_info>();
        }
        public void start_chatmanager(NetworkService server_network)
        {
            this.server_network = server_network;
            chatManager = new ChatLog(server_network);
            //ChatLogCollection = server_network.database.GetCollection<BsonDocument>(ChatLog_Collection);
            Thread chat_manager_thread = new Thread(chat_manager_do_thread);
            chat_manager_thread.Start();
        }
        public void chat_manager_do_thread()
        {
            while (true)
            {
                chat_queue_info chat_request;
                lock (chat_message_queue)
                {
                    if (chat_message_queue.Count == 0) continue;
                    chat_request = chat_message_queue.Dequeue();
                }
                Console.WriteLine("Receive >> " + chat_request.chat_message.pt_id);
                switch(chat_request.chat_message.pt_id)
                {
                    case PROTOCOL.Send_Message:
                        chatManager.recordChatLog(chat_request.owner_info.client_nickname, chat_request.chat_message.ingame_info.room_num, chat_request.chat_message.ingame_info.message, chat_request.chat_message.ingame_info.target_nickname);
                        switch (chat_request.chat_message.ingame_info.room_num)
                        {
                            case 0:
                                if (chat_request.chat_message.ingame_info.target_nickname != "")
                                {
                                    Token receiver = server_network.find_user(chat_request.chat_message.ingame_info.target_nickname);
                                    if (receiver == null)
                                    {
                                        string message1 = "해당 유저가 존재하지 않거나 접속중이 아닙니다.";
                                        chat_message_send(chat_request.owner_info, PROTOCOL.Deliver_Message, null, chat_request.chat_message.ingame_info.room_num, null, -1, message1);
                                    }
                                    else
                                    {
                                        string message2 = "[귓] " + chat_request.owner_info.client_nickname + " >> " + receiver.client_nickname + ": " + chat_request.chat_message.ingame_info.message;
                                        chat_message_send(chat_request.owner_info, PROTOCOL.Deliver_Message, null, chat_request.chat_message.ingame_info.room_num, null, -1, message2);
                                        chat_message_send(receiver, PROTOCOL.Deliver_Message, null, chat_request.chat_message.ingame_info.room_num, null, -1, message2);
                                    }
                                }
                                else
                                {
                                    List<Token> current_users = new List<Token>();
                                    current_users = server_network.deliver_current_tokens();
                                    string message3 = chat_request.owner_info.client_nickname + ": " + chat_request.chat_message.ingame_info.message;
                                    foreach (var current_token in current_users)
                                    {
                                        chat_message_send(current_token, PROTOCOL.Deliver_Message, null, chat_request.chat_message.ingame_info.room_num, null, -1, message3);
                                    }
                                }
                                break;
                            default:
                                /*
                                room_num
                                message
                                GroupMember_num
                                 */
                                int message_room_num = chat_request.chat_message.ingame_info.room_num;
                                int message_member_num = chat_request.chat_message.ingame_info.GroupMember_num;
                                string message4 = chat_request.chat_message.ingame_info.message;
                                
                                var room1 = current_rooms.FirstOrDefault(r => r.room_num == message_room_num);
                                if(room1 != null)
                                {
                                    lock (room1)
                                    {
                                        for (int i = 1; i <= member_maximum; i++)
                                        {
                                            if (room1.Members.ContainsKey(i))
                                            {
                                                chat_message_send(room1.Members[i], PROTOCOL.Deliver_Message,null,-1,null,message_member_num,message4);
                                            }
                                        }
                                    }
                                }
                                //message, GroupMember_num
                                break;
                        }
                        break;
                    case PROTOCOL.GroupChatRoom_Create_Request:
                        int new_room_num = addGroupChatRoom(chat_request.owner_info);
                        chat_message_send(chat_request.owner_info, PROTOCOL.GroupChatRoom_Create_Success,null, new_room_num, null, 1, null);
                        break;
                    case PROTOCOL.GroupChat_Invite_Request:
                        string request_nickname = chat_request.owner_info.client_nickname;
                        string friendNickname = chat_request.chat_message.ingame_info.friendnick;
                        Console.WriteLine("friendNickname: " + friendNickname);
                        int room_num = chat_request.chat_message.ingame_info.room_num;

                        List<Token> currentUsers1 = server_network.deliver_current_tokens();
                        Token invited_user = currentUsers1.Find(user => user.client_nickname == friendNickname);
                        if (invited_user != null) Console.WriteLine(invited_user.client_nickname);
                        else Console.WriteLine("not found");

                        bool issendsuccess = chat_message_send(chat_request.owner_info, PROTOCOL.GroupChat_Invite_Success, null, -1, null, -1, null);
                        if (issendsuccess) chat_message_send(invited_user, PROTOCOL.GroupChat_Invite_Receive, request_nickname, room_num, null, -1, null);
                        break;
                    case PROTOCOL.Invite_Accept_Request:
                        int try_enter_room_num = chat_request.chat_message.ingame_info.room_num;
                        var room = current_rooms.FirstOrDefault(r => r.room_num == try_enter_room_num);
                        if (room != null)
                        {
                            lock (room)
                            {
                                if (room.Members.Count() < member_maximum)
                                {
                                    int decide_member_num = room.add_member(chat_request.owner_info);
                                    chat_message_send(chat_request.owner_info, PROTOCOL.Invite_Accept_Success, null, try_enter_room_num, room.get_members(), decide_member_num, null);

                                    // 기존 멤버들에게 새 멤버 입장 알림
                                    for (int i = 1; i <= member_maximum; i++)
                                    {
                                        if (room.Members.ContainsKey(i) && i != decide_member_num)
                                        {
                                            chat_message_send(room.Members[i], PROTOCOL.GroupChat_Enter_Receive, chat_request.owner_info.client_nickname, -1, null, decide_member_num, null);
                                        }
                                    }
                                }
                                else
                                {
                                    // 방이 가득 찬 경우
                                    chat_message_send(chat_request.owner_info, PROTOCOL.Invite_Accept_Fail, null, -1, null, -1, null);
                                }
                            }
                        }
                        else
                        {
                            // 방을 찾지 못한 경우
                            chat_message_send(chat_request.owner_info, PROTOCOL.Invite_Accept_Fail, null, -1, null, -1, null);
                        }
                        break;
                    case PROTOCOL.Invite_Reject_Request:
                        chat_message_send(chat_request.owner_info, PROTOCOL.Invite_Reject_Success, null, -1, null, -1, null);
                        break;
                    case PROTOCOL.GroupChat_Exit_Request:
                        int exit_room_num = chat_request.chat_message.ingame_info.room_num;
                        var exit_room = current_rooms.FirstOrDefault(r => r.room_num == exit_room_num);
                        if (exit_room != null)
                        {
                            lock (exit_room)
                            {
                                int exiting_member_num = chat_request.chat_message.ingame_info.GroupMember_num;
                                // 해당 멤버 제거
                                exit_room.delete_member(exiting_member_num);

                                // 나가는 멤버에게 성공 메시지 전송
                                chat_message_send(chat_request.owner_info, PROTOCOL.GroupChat_Exit_Success, null, exit_room_num, null, -1, null);

                                // 남은 멤버들에게 나간 멤버 알림
                                for (int i = 1; i <= member_maximum; i++)
                                {
                                    if (exit_room.Members.ContainsKey(i))
                                    {
                                        chat_message_send(exit_room.Members[i], PROTOCOL.GroupChat_Exit_Receive, chat_request.owner_info.client_nickname, -1, null, exiting_member_num, null);
                                    }
                                }

                                // 방에 더 이상 멤버가 없으면 방 제거
                                if (exit_room.Members.Count == 0)
                                {
                                    current_rooms.Remove(exit_room);
                                }
                            }
                        }
                        else
                        {
                            // 방을 찾지 못한 경우
                            chat_message_send(chat_request.owner_info, PROTOCOL.GroupChat_Exit_Fail, null, -1, null, -1, null);
                        }
                        break;
                    default:
                        break;
                }
                
            }
        }
        public void handle_group_chat_exit_on_session_closed(Token token)
        {
            // 사용자가 속한 모든 그룹 채팅방 확인 및 제거
            foreach (var room in current_rooms.ToList())
            {
                lock (room)
                {
                    // 사용자가 멤버인 방 찾기
                    var memberEntry = room.Members.FirstOrDefault(x => x.Value == token);
                    if (memberEntry.Value != null)
                    {
                        int exiting_member_num = memberEntry.Key;

                        // 방에서 멤버 제거
                        room.delete_member(exiting_member_num);

                        // 남은 멤버들에게 퇴장 알림
                        for (int i = 1; i <= member_maximum; i++)
                        {
                            if (room.Members.ContainsKey(i))
                            {
                                chat_message_send(room.Members[i], PROTOCOL.GroupChat_Exit_Receive, token.client_nickname, -1, null, exiting_member_num, null);
                            }
                        }

                        // 멤버가 없으면 방 삭제
                        if (room.Members.Count == 0)
                        {
                            current_rooms.Remove(room);
                        }
                    }
                    break;
                }
            }
        }
        private bool chat_message_send(Token owner_token, PROTOCOL protocol, string friendnick, int room_num, List<string> searchnicks, int GroupMember_num, string message)
        {
            try
            {
                //친구 관련 모든 메시지를 전송해야 하기 때문에 항상 친구 메시지 필드 모두 복사 (3가지)
                message response = new message();
                response.pt_id = protocol;
                InGame_message info = new InGame_message();
                info.friendnick = friendnick;  //그룹 멤버 닉네임
                info.room_num = room_num;
                info.search_nicks = searchnicks;    //그룹 멤버들
                info.GroupMember_num = GroupMember_num;
                info.message = message;
                response.ingame_info = info;
                string jsonResponse = JsonConvert.SerializeObject(response);

                // 메시지 전송
                server_network.SendDataToToken(owner_token, jsonResponse);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Message send failed: {ex.Message}");
                if (protocol < PROTOCOL.GroupChat_Invite_Receive)
                {
                    // 실패 메시지 전송
                    message failResponse = new message();
                    failResponse.pt_id = get_fail_protocol(protocol);  // 각 프로토콜에 맞는 실패 프로토콜 반환
                    string jsonFailResponse = JsonConvert.SerializeObject(failResponse);
                    server_network.SendDataToToken(owner_token, jsonFailResponse);
                }
                return false;
            }
        }

        private PROTOCOL get_fail_protocol(PROTOCOL protocol)
        {
            switch (protocol)
            {
                case PROTOCOL.GroupChatRoom_Create_Success:
                    return PROTOCOL.GroupChatRoom_Create_Fail;
                case PROTOCOL.GroupChat_Invite_Success:
                    return PROTOCOL.GroupChat_Invite_Fail;
                case PROTOCOL.Invite_Accept_Success:
                    return PROTOCOL.Invite_Accept_Fail;
                case PROTOCOL.Invite_Reject_Success:
                    return PROTOCOL.Invite_Reject_Fail;
                case PROTOCOL.GroupChat_Exit_Success:
                    return PROTOCOL.GroupChat_Exit_Fail;
                default:
                    return PROTOCOL.Setting; // 임의로 아무 프로토콜
            }
        }

        public void enqueue_chat_message(Token client_token, message received_chat_message)
        {
            Console.WriteLine(received_chat_message.ingame_info.message);
            chat_queue_info request_message = new chat_queue_info();
            request_message.owner_info = client_token;
            message ingame_new_message = new message();
            ingame_new_message.pt_id = received_chat_message.pt_id;
            InGame_message new_info = new InGame_message();
            new_info.room_num = received_chat_message.ingame_info.room_num;
            new_info.message = received_chat_message.ingame_info.message;
            new_info.target_nickname = received_chat_message.ingame_info.target_nickname;
            new_info.friendnick = received_chat_message.ingame_info.friendnick;
            new_info.search_nicks = received_chat_message.ingame_info.search_nicks;
            new_info.GroupMember_num = received_chat_message.ingame_info.GroupMember_num;
            ingame_new_message.ingame_info = new_info;
            request_message.chat_message = ingame_new_message;
            chat_message_queue.Enqueue(request_message);
            Console.WriteLine(chat_message_queue.Count);
        }
    }
}
