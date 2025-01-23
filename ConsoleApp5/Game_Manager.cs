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
    class Game_Manager
    {
        private User_DB_Process userManager;
        private Minigame_DB_Process minigameManager;

        class game_queue_info
        {
            public Token owner_info { get; set; }
            public message game_message { get; set; }
        }
        NetworkService server_network;
        Queue<game_queue_info> game_message_queue;
        List<Token> scene_users = new List<Token>();

        public Game_Manager()
        {
            game_message_queue = new Queue<game_queue_info>();

        }
        public void start_gamemanager(NetworkService server_network)
        {
            this.server_network = server_network;
            userManager = new User_DB_Process(server_network);
            minigameManager = new Minigame_DB_Process(server_network);
            Thread game_manager_thread = new Thread(game_manager_do_thread);
            game_manager_thread.Start();
        }
        public void game_manager_do_thread()
        {
            while (true)
            {
                game_queue_info game_request;
                lock (game_message_queue)
                {
                    if (game_message_queue.Count == 0) continue;
                    game_request = game_message_queue.Dequeue();
                }
                Console.WriteLine(game_request.game_message.pt_id);
                switch (game_request.game_message.pt_id)
                {
                    case PROTOCOL.Quest_Start_Request:
                        message new_message1 = new message();
                        new_message1.pt_id = PROTOCOL.Quest_Start_Success;
                        InGame_message quest_info1 = new InGame_message();
                        quest_info1.semester = game_request.game_message.ingame_info.semester;
                        quest_info1.main_quest_num = game_request.game_message.ingame_info.main_quest_num;
                        quest_info1.detail_quest_num = game_request.game_message.ingame_info.detail_quest_num;
                        quest_info1.quest_state = 1;
                        new_message1.ingame_info = quest_info1;
                        string new_deliver_message1 = JsonConvert.SerializeObject(new_message1);

                        Console.WriteLine("game_request.owner_info.client_nickname" + game_request.owner_info.client_nickname + "quest_info1.main_quest_num" + quest_info1.main_quest_num + "quest_info1.detail_quest_num" + quest_info1.detail_quest_num, "quest_info1.quest_state" + quest_info1.quest_state);
                        Console.WriteLine();

                        try
                        {
                            if (userManager == null)
                            {
                                Console.WriteLine("userManager가 초기화되지 않았습니다.");
                                continue;
                            }

                            userManager.setCharacterQuest(game_request.owner_info.client_nickname, quest_info1.main_quest_num, quest_info1.detail_quest_num, quest_info1.quest_state);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"게임 매니저 오류: {ex.Message}");
                            Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
                        }
                        server_network.SendDataToToken(game_request.owner_info, new_deliver_message1);
                        Console.WriteLine("Sent to Client Quest_Start_Success");
                        break;
                    case PROTOCOL.Quest_Complete_Request:
                        message new_message2 = new message();
                        new_message2.pt_id = PROTOCOL.Quest_Complete_Success;
                        InGame_message quest_info2 = new InGame_message();
                        quest_info2.semester = game_request.game_message.ingame_info.semester;
                        quest_info2.main_quest_num = game_request.game_message.ingame_info.main_quest_num;
                        quest_info2.detail_quest_num = game_request.game_message.ingame_info.detail_quest_num;
                        quest_info2.quest_state = 0;
                        new_message2.ingame_info = quest_info2;
                        string new_deliver_message2 = JsonConvert.SerializeObject(new_message2);
                        Console.WriteLine("Quest Start Request...");
                        //DB 작업 => this.server_network.UserCharacter
                        try
                        {
                            if (userManager == null)
                            {
                                Console.WriteLine("userManager가 초기화되지 않았습니다.");
                                continue;
                            }

                            userManager.setCharacterQuest(game_request.owner_info.client_nickname, quest_info2.main_quest_num, quest_info2.detail_quest_num, quest_info2.quest_state);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"게임 매니저 오류: {ex.Message}");
                            Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
                        }
                        server_network.SendDataToToken(game_request.owner_info, new_deliver_message2);
                        break;
                    case PROTOCOL.MiniGame_End_Request:
                        message new_message3 = new message();
                        new_message3.pt_id = PROTOCOL.MiniGame_End_Success;
                        InGame_message minigame_info = new InGame_message();
                        minigame_info.scene_num = game_request.owner_info.scene_num;
                        new_message3.ingame_info = minigame_info;
                        string new_deliver_message3 = JsonConvert.SerializeObject(new_message3);

                        server_network.SendDataToToken(game_request.owner_info, new_deliver_message3);
                        Console.WriteLine("Send to Client MiniGame_End_Success");

                        break;
                    case PROTOCOL.Sub_Quest_End_Request:
                        // game_request와 owner_info, game_message 확인
                        if (game_request == null)
                        {
                            Console.WriteLine("game_request가 null입니다.");
                            return;
                        }

                        if (game_request.owner_info == null)
                        {
                            Console.WriteLine("owner_info가 null입니다.");
                            return;
                        }

                        if (string.IsNullOrEmpty(game_request.owner_info.client_nickname))
                        {
                            Console.WriteLine("client_nickname이 null이거나 비어 있습니다.");
                            return;
                        }

                        if (game_request.game_message == null)
                        {
                            Console.WriteLine("game_message가 null입니다.");
                            return;
                        }

                        if (game_request.game_message.ingame_info == null)
                        {
                            Console.WriteLine("ingame_info가 null입니다.");
                            return;
                        }

                        // ingame_info의 속성 확인
                        if (game_request.game_message.ingame_info.minigame_num == 0)
                        {
                            Console.WriteLine("minigame_num이 0입니다.");
                            // 필요 시 추가 처리
                        }

                        if (game_request.game_message.ingame_info.sub_quest_score == 0)
                        {
                            Console.WriteLine("sub_quest_score이 0입니다.");
                            // 필요 시 추가 처리
                        }

                        // Minigame 기록 업데이트
                        minigameManager.UpdateMinigameRecord(
                            game_request.owner_info.client_nickname,
                            game_request.game_message.ingame_info.minigame_num,
                            game_request.game_message.ingame_info.sub_quest_score
                        );

                        // 메시지 생성 및 전송
                        message new_message4 = new message();
                        new_message4.pt_id = PROTOCOL.Sub_Quest_End_Success;

                        InGame_message subquest_info = new InGame_message();
                        subquest_info.scene_num = game_request.owner_info.scene_num;
                        new_message4.ingame_info = subquest_info;

                        string new_deliver_message4 = JsonConvert.SerializeObject(new_message4);
                        server_network.SendDataToToken(game_request.owner_info, new_deliver_message4);
                        Console.WriteLine("Send to Client Sub Quest End Success");
                        break;
                    default:
                        break;
                }
            }
        }
        public void enqueue_game_message(Token client_token, message received_game_message)
        {
            game_queue_info request_message = new game_queue_info();
            request_message.owner_info = client_token;
            message ingame_new_message = new message();
            ingame_new_message.pt_id = received_game_message.pt_id;
            InGame_message new_info = new InGame_message();
            new_info.x_position = received_game_message.ingame_info.x_position;
            new_info.y_position = received_game_message.ingame_info.y_position;
            new_info.scene_num = received_game_message.ingame_info.scene_num;
            new_info.room_num = received_game_message.ingame_info.room_num;
            new_info.semester = received_game_message.ingame_info.semester;
            new_info.main_quest_num = received_game_message.ingame_info.main_quest_num;
            new_info.detail_quest_num = received_game_message.ingame_info.detail_quest_num;
            new_info.quest_state = received_game_message.ingame_info.quest_state;
            new_info.own_nickname = received_game_message.ingame_info.own_nickname;
            new_info.sub_quest_score = received_game_message.ingame_info.sub_quest_score;
            new_info.minigame_num = received_game_message.ingame_info.minigame_num;
            ingame_new_message.ingame_info = new_info;
            request_message.game_message = ingame_new_message;
            game_message_queue.Enqueue(request_message);
        }
    }
}
