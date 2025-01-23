using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace ConsoleApp5
{
    class Friend_Manager
    {
        private Friend_DB_Process friendManager;
        class friend_queue_info
        {
            public Token owner_info { get; set; }
            public message friend_message { get; set; }
        }
        Queue<friend_queue_info> friend_message_queue;
        NetworkService server_network;

        public Friend_Manager()
        {
            friend_message_queue = new Queue<friend_queue_info>();
        }
        public void start_friendmanager(NetworkService server_network)
        {
            this.server_network = server_network;
            friendManager = new Friend_DB_Process(server_network);
            Thread friend_manager_thread = new Thread(friend_manager_do_thread);
            friend_manager_thread.Start();
        }
        public void friend_manager_do_thread()
        {
            while (true)
            {
                friend_queue_info friend_request;
                lock (friend_message_queue)
                {
                    if (friend_message_queue.Count == 0) continue;
                    friend_request = friend_message_queue.Dequeue();
                }
                Console.WriteLine(friend_request.friend_message.pt_id);

                switch (friend_request.friend_message.pt_id)
                {
                    case PROTOCOL.Friend_Delete_Request:
                        string delete_request_nick = friend_request.owner_info.client_nickname;
                        string delete_receive_nick = friend_request.friend_message.ingame_info.friendnick;

                        friendManager.deleteFriend(delete_request_nick, delete_receive_nick);

                        friend_message_send(friend_request.owner_info, delete_request_nick, PROTOCOL.Friend_Delete_Success, delete_receive_nick, false, null);

                        List<Token> currentUsers1 = server_network.deliver_current_tokens();
                        Token deletedFriendToken = currentUsers1.Find(user => user.client_nickname == delete_receive_nick);
                        if (deletedFriendToken != null)
                        {
                            friend_message_send(deletedFriendToken, delete_receive_nick, PROTOCOL.Friend_Delete_Receive, delete_request_nick, false, null);

                            friend_request.owner_info.update_friend_nick_list(0, delete_receive_nick);
                            deletedFriendToken.update_friend_nick_list(0, delete_request_nick);
                        }
                        else friend_request.owner_info.update_friend_nick_list(0, delete_receive_nick);

                        Console.WriteLine($"Friend deletion successful: {delete_request_nick} and {delete_receive_nick}");
                        break;
                    case PROTOCOL.User_Search_Request:
                        List<string> usersearch_list = new List<string>();
                        string search_nick = friend_request.friend_message.ingame_info.friendnick;
                        usersearch_list = friendManager.UserSearchResult(search_nick, friend_request.owner_info.client_nickname);

                        friend_message_send(friend_request.owner_info, friend_request.owner_info.client_nickname, PROTOCOL.User_Search_Success, null, false, usersearch_list);

                        Console.WriteLine($"User Search successful: {search_nick}");
                        break;
                    case PROTOCOL.Friend_Request:
                        string request_request_nick = friend_request.owner_info.client_nickname;
                        string request_receive_nick = friend_request.friend_message.ingame_info.friendnick;

                        friendManager.SendFriendRequest(request_request_nick, request_receive_nick);
                        friend_message_send(friend_request.owner_info, request_request_nick, PROTOCOL.Friend_Request_Success, request_receive_nick, false, null);

                        List<Token> currentUsers2 = server_network.deliver_current_tokens();
                        Token sendFriendToken = currentUsers2.Find(user => user.client_nickname == request_receive_nick);
                        if (sendFriendToken != null) friend_message_send(sendFriendToken, request_receive_nick, PROTOCOL.Friend_Request_Receive, request_request_nick, false, null);

                        Console.WriteLine($"Friend request successful: {request_request_nick} and {request_receive_nick}");
                        break;
                    case PROTOCOL.Friend_Request_Cancel:
                        string cancel_request_nick = friend_request.owner_info.client_nickname;
                        string cancel_receive_nick = friend_request.friend_message.ingame_info.friendnick;

                        friendManager.cancleFriendRequest(cancel_request_nick, cancel_receive_nick);
                        
                        friend_message_send(friend_request.owner_info, cancel_request_nick, PROTOCOL.Friend_Request_Cancel_Success, cancel_receive_nick, false, null);

                        List<Token> currentUsers3 = server_network.deliver_current_tokens();
                        Token cancleFriendToken = currentUsers3.Find(user => user.client_nickname == cancel_receive_nick);
                        if (cancleFriendToken != null) friend_message_send(cancleFriendToken, cancel_receive_nick, PROTOCOL.Friend_Request_Cancel_Receive, cancel_request_nick, false, null);

                        Console.WriteLine($"Friend request cancle successful: {cancel_request_nick} and {cancel_receive_nick}");
                        break;
                    case PROTOCOL.Friend_Access_Request:
                        string accept_request_nick = friend_request.owner_info.client_nickname;
                        string accept_receive_nick = friend_request.friend_message.ingame_info.friendnick;

                        int friendCount = friendManager.GetFriendCount(accept_receive_nick);
                        if (friendCount >= 10)
                        {
                            // 친구 수락 대상의 친구 수가 10명 이상일 경우
                            friend_message_send(friend_request.owner_info, accept_request_nick, PROTOCOL.Friend_Access_Fail, accept_receive_nick, false, null);
                            Console.WriteLine($"Friend access failed (friend limit exceeded): {accept_request_nick} and {accept_receive_nick}");
                            break;
                        }
                        else
                        {
                            // 친구 수락 처리
                            friendManager.acceptFriendRequest(accept_receive_nick, accept_request_nick);

                            // 현재 접속 중인 유저 목록 가져오기
                            List<Token> currentUsers4 = server_network.deliver_current_tokens();
                            // 친구 수락 대상이 온라인인지 확인
                            Token acceptedFriendToken = currentUsers4.Find(user => user.client_nickname == accept_receive_nick);

                            if (acceptedFriendToken != null)
                            {
                                // 온라인인 경우
                                // 1. 친구 수락 요청을 보낸 유저에게 메시지 전송
                                friend_message_send(friend_request.owner_info, accept_request_nick, PROTOCOL.Friend_Access_Success, accept_receive_nick, true, null);

                                // 2. 친구 수락 된 유저에게 메시지 전송
                                friend_message_send(acceptedFriendToken, accept_receive_nick, PROTOCOL.Friend_Access_Receive, accept_request_nick, true, null);

                                friend_request.owner_info.update_friend_nick_list(1, accept_receive_nick);
                                acceptedFriendToken.update_friend_nick_list(1, accept_request_nick);
                            }
                            else
                            {
                                // 오프라인인 경우
                                // 친구 수락 요청을 보낸 유저에게만 메시지 전송
                                friend_message_send(friend_request.owner_info, accept_request_nick, PROTOCOL.Friend_Access_Success, accept_receive_nick, false, null);

                                friend_request.owner_info.update_friend_nick_list(1, accept_receive_nick);
                            }
                        }

                        //친구 수락 요청한 토큰의 friend_nickname_list 업데이트 (추가)
                        //acceptedFriendToken이 null이 아니라면 이 토큰의 friend_nickname_list도 업데이트 (추가)
                        Console.WriteLine($"Friend access successful: {accept_request_nick} and {accept_receive_nick}");
                        break;
                    case PROTOCOL.Friend_Reject_Reqeust:
                        string reject_request_nick = friend_request.owner_info.client_nickname;
                        string reject_receive_nick = friend_request.friend_message.ingame_info.friendnick;

                        friendManager.rejectFriendRequest(reject_receive_nick, reject_request_nick);

                        friend_message_send(friend_request.owner_info, reject_request_nick, PROTOCOL.Friend_Reject_Success, reject_receive_nick, false, null);

                        List<Token> currentUsers5 = server_network.deliver_current_tokens();
                        Token rejectFriendToken = currentUsers5.Find(user => user.client_nickname == reject_receive_nick);
                        if (rejectFriendToken != null) friend_message_send(rejectFriendToken, reject_receive_nick, PROTOCOL.Friend_Reject_Receive, reject_request_nick, false, null);

                        Console.WriteLine($"Friend reject successful: {reject_request_nick} and {reject_receive_nick}");
                        break;
                    default:
                        break;
                }
            }
        }
        public void enqueue_friend_message(Token client_token, message received_friend_message) //Friend_Manager가 관리하는 message 큐에 넣는 함수
        {
            Console.WriteLine("Friend_Manager >> " + received_friend_message.pt_id);
            friend_queue_info request_message = new friend_queue_info();
            request_message.owner_info = client_token;
            message ingame_new_message = new message();
            ingame_new_message.pt_id = received_friend_message.pt_id;
            InGame_message new_info = new InGame_message();
            
            //친구 시스템에서는 Ingame_message 필드 중에 
            new_info.friendnick = received_friend_message.ingame_info.friendnick;   //friend_nick 필드랑
            new_info.search_nicks = received_friend_message.ingame_info.search_nicks;   //search_nicks 필드만
            new_info.friend_loginst = received_friend_message.ingame_info.friend_loginst;
            //사용하므로 이거만 복사

            ingame_new_message.ingame_info = new_info;
            request_message.friend_message = ingame_new_message;
            friend_message_queue.Enqueue(request_message);
            Console.WriteLine(friend_message_queue.Count);
        }

        private void friend_message_send(Token owner_token, string target_nickname, PROTOCOL protocol, string friend_nickname, bool friend_logintST, List<string> searchnicks)
        {
            try
            {
                //친구 관련 모든 메시지를 전송해야 하기 때문에 항상 친구 메시지 필드 모두 복사 (3가지)
                message response = new message();
                response.pt_id = protocol;
                InGame_message info = new InGame_message();
                info.friendnick = friend_nickname;  //친구 닉네임
                info.friend_loginst = friend_logintST;  //로그인 상태
                info.search_nicks = searchnicks;    //유저 검색 시 결과 리스트
                response.ingame_info = info;
                string jsonResponse = JsonConvert.SerializeObject(response);

                // 메시지 전송
                server_network.SendDataToToken(owner_token, jsonResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Message send failed: {ex.Message}");
                if(protocol < PROTOCOL.Friend_Delete_Receive)
                {
                    // 실패 메시지 전송
                    message failResponse = new message();
                    failResponse.pt_id = get_fail_protocol(protocol);  // 각 프로토콜에 맞는 실패 프로토콜 반환
                    string jsonFailResponse = JsonConvert.SerializeObject(failResponse);
                    server_network.SendDataToToken(owner_token, jsonFailResponse);
                }
            }
        }

        private PROTOCOL get_fail_protocol(PROTOCOL protocol)
        {
            switch (protocol)
            {
                case PROTOCOL.Friend_Delete_Success:
                    return PROTOCOL.Friend_Delete_Fail;
                case PROTOCOL.Friend_Request_Cancel_Success:
                    return PROTOCOL.Friend_Request_Cancel_Fail;
                case PROTOCOL.Friend_Access_Success:
                    return PROTOCOL.Friend_Access_Fail;
                case PROTOCOL.Friend_Reject_Success:
                    return PROTOCOL.Friend_Reject_Fail;
                case PROTOCOL.Friend_Request_Success:
                    return PROTOCOL.Friend_Request_Fail;
                default:
                    return PROTOCOL.Setting; // 임의로 아무 프로토콜
            }
        }

    }
}
