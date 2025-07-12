using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;

namespace ConsoleApp5
{
    public class FriendInfo
    {
        public string Nickname { get; set; }
        public bool IsOnline { get; set; }
    }
    public class message
    {
        public PROTOCOL pt_id { get; set; }
        public login_info signup_login_info { get; set; }
        public login_success_info first_login_info { get; set; }
        public InGame_message ingame_info { get; set; }
    }
    public class login_info
    {
        public string Email { get; set; }
        public string PW { get; set; }
        public string Nickname { get; set; }
    }
    public class login_success_info
    {
        public string Nickname { get; set; }
        public int scene_num { get; set; }
        public double x_position { get; set; }
        public double y_position { get; set; }
        public int semester { get; set; }
        public int main_quest_num { get; set; }
        public int detail_quest_num { get; set; }
        public int quest_state { get; set; }
        public List<int> minigame_nums { get; set; }
        public List<double> scores { get; set; }
        public List<FriendInfo> friends { get; set; }
        public List<string> friendrequest { get; set; }
        public List<string> requestreceived { get; set; }
    }
    public class InGame_message
    {
        public double x_position { get; set; }
        public double y_position { get; set; }
        public double DirX { get; set; }
        public double DirY { get; set; }
        public int scene_num { get; set; }
        public int room_num { get; set; }
        public string target_nickname { get; set; }
        public string message { get; set; }
        public int semester { get; set; }
        public int main_quest_num { get; set; }
        public int detail_quest_num { get; set; }
        public int quest_state { get; set; }
        public string own_nickname { get; set; }
        public double sub_quest_score { get; set; }
        public int minigame_num { get; set; }
        public string friendnick { get; set; }
        public bool friend_loginst { get; set; }
        public List<string> search_nicks { get; set; }
        public int GroupMember_num { get; set; }
    }
    class every_message
    {
        public Token user_token;
        public message new_message;
    }
    class NetworkService
    {
        private User_DB_Process userManager;
        private Minigame_DB_Process minigameManager;
        private Friend_DB_Process friendManager;
        //private Friend friendManager;
        private const string ConnectionString = "Server=DB_IP;Port=3306;Database=DB_Name;Uid=ID;Pwd=PW";
        public string getConnectionString()
        {
            return ConnectionString;
        }
        class accept_receive_data
        {
            public PROTOCOL pt_id { get; set; }
            public login_info info { get; set; }
        }

        //listen 소켓
        Socket listen_socket;
        SocketAsyncEventArgs accept_event;
        AutoResetEvent flow_control_event;
        public delegate void ClientHandler(Socket client_socket, object token, string client_ID, string client_nickname, int scene, List<string> friendslist);
        public ClientHandler callback_on_newclient;

        public delegate void SessionHandler(Token token);
        public SessionHandler session_created_callback { get; set; }

        public Stack<SocketAsyncEventArgs> receive_event_pool;
        public Stack<SocketAsyncEventArgs> send_event_pool;

        //buffer 설정
        public byte[] m_buffer;
        public int max_connections;
        public int buffer_size;
        public int pre_alloc_count = 1;
        public int connected_count;

        public int m_numBytes;
        public int m_currentIndex = 0;
        public int m_bufferSize;
        public Stack<int> m_freeIndexPool;

        List<Socket> client_sockets;
        List<Token> users;
        //List<login_info> fake_user_db;

        //public const string MONGODB_URI_FORMAT = "mongodb+srv://capstone:20211275@cluster0.ynjxsbf.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        //private const string TEST_DB = "Game_DB";
        //private const string User_info_Collection = "User_info";
        //private const string User_Character_Collection = "User_Character";
        //private const string Minigame_Record_Collection = "Minigame_Record";

        //public MongoClient Mongo_Server;
        //public IMongoDatabase database;
        //public IMongoCollection<BsonDocument> UsersCollection;
        //public IMongoCollection<BsonDocument> UserCharacter;
        //public IMongoCollection<BsonDocument> MinigameRecord;

        public Chat_Manager chat_manager_cs;
        public Game_Manager game_manager_cs;
        public Friend_Manager friend_manager_cs;
        public NetworkService()
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            userManager = new User_DB_Process(this);
            minigameManager = new Minigame_DB_Process(this);
            friendManager = new Friend_DB_Process(this);
            //friendManager = new Friend(db);


            //Mongo_Server = new MongoClient(MONGODB_URI_FORMAT);
            //database = Mongo_Server.GetDatabase(TEST_DB);

            chat_manager_cs = new Chat_Manager();
            game_manager_cs = new Game_Manager();
            friend_manager_cs = new Friend_Manager();

            client_sockets = new List<Socket>();
            users = new List<Token>();

            //fake_user_db = new List<login_info>();

            this.connected_count = 0;
            this.session_created_callback = null;

            this.max_connections = 10000;
            this.buffer_size = 1024;

            this.m_numBytes = this.max_connections * this.buffer_size * this.pre_alloc_count;
            this.m_bufferSize = this.buffer_size;
            this.m_buffer = new byte[m_numBytes];
            m_freeIndexPool = new Stack<int>();

            this.callback_on_newclient = null;

            this.receive_event_pool = new Stack<SocketAsyncEventArgs>(max_connections);
            this.send_event_pool = new Stack<SocketAsyncEventArgs>(max_connections);

            SocketAsyncEventArgs arg;
            for (int i = 0; i < max_connections; i++)
            {
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
                    arg.UserToken = null;
                    SetBuffer(arg);
                    this.receive_event_pool.Push(arg);
                }

                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
                    arg.UserToken = null;
                    arg.SetBuffer(null, 0, 0);
                    this.send_event_pool.Push(arg);
                }
            }
        }
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }
        void receive_completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                chat_client_receive_completed(null, e);
                return;
            }
            throw new ArgumentException("The last operation completed on the socket was not a receive");
        }
        void send_completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                Token token = e.UserToken as Token;
            }
            catch (Exception)
            {

            }
        }

        //서버 시작
        public void start(string host, int port, int backlog)
        {
            this.listen_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine(this.listen_socket);
            IPAddress address;
            if (host == "0.0.0.0")
            {
                address = IPAddress.Any;
            }
            else
            {
                address = IPAddress.Parse(host);
            }
            IPEndPoint endpoint = new IPEndPoint(address, port);
            this.callback_on_newclient += on_new_client;
            this.session_created_callback += add_users;
            //UsersCollection = database.GetCollection<BsonDocument>(User_info_Collection);
            //UserCharacter = database.GetCollection<BsonDocument>(User_Character_Collection);
            //MinigameRecord = database.GetCollection<BsonDocument>(Minigame_Record_Collection);
            chat_manager_cs.start_chatmanager(this);
            game_manager_cs.start_gamemanager(this);
            friend_manager_cs.start_friendmanager(this);
            try
            {
                listen_socket.Bind(endpoint);
                listen_socket.Listen(backlog);
                this.accept_event = new SocketAsyncEventArgs();
                this.accept_event.Completed += new EventHandler<SocketAsyncEventArgs>(on_accept_completed);

                Thread listen_thread = new Thread(do_listen);
                listen_thread.Start();

                StartMessageProcessingThread();
                StartSendMessageProcessingThread();
            }
            catch (Exception e)
            {

            }
        }
        public void do_listen()
        {
            Console.WriteLine("Start listen");
            this.flow_control_event = new AutoResetEvent(false);
            while (true)
            {
                this.accept_event.AcceptSocket = null;
                bool pending = true;
                try
                {
                    pending = listen_socket.AcceptAsync(this.accept_event);
                }
                catch (Exception e)
                {
                    continue;
                }
                if (!pending)
                {
                    on_accept_completed(null, this.accept_event);
                }

                this.flow_control_event.WaitOne();
                //Thread new_thread = new Thread(begin_receive);
                //new_thread.Start();
            }
        }
        void on_accept_completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket client_socket = e.AcceptSocket;
                this.client_sockets.Add(client_socket);
                Console.WriteLine("Client connected >> " + client_sockets.Count);

                // 클라이언트와의 비동기 통신 시작
                StartReceive(client_socket);
            }
            else
            {
                // 에러 처리
            }
            this.flow_control_event.Set();
            return;
        }
        void StartReceive(Socket clientSocket)
        {
            try
            {
                // 비동기 수신 작업 설정
                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                byte[] buffer = new byte[1024];
                receiveArgs.SetBuffer(buffer, 0, buffer.Length);
                receiveArgs.UserToken = clientSocket;
                receiveArgs.Completed += ReceiveCompleted;

                // 비동기 수신 시작
                bool willRaiseEvent = clientSocket.ReceiveAsync(receiveArgs);
                if (!willRaiseEvent)
                {
                    // 동기적으로 완료됨, 직접 완료 처리
                    ReceiveCompleted(clientSocket, receiveArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting receive: " + ex.Message);
                // 에러 처리
            }
        }
        void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket clientSocket = (Socket)sender;
            try
            {
                bool islogon = false;
                int bytesReceived = e.BytesTransferred;
                if (bytesReceived > 0 && e.SocketError == SocketError.Success)
                {
                    byte[] buffer = e.Buffer;
                    string receivedJson = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    message receivedInfo = JsonConvert.DeserializeObject<message>(receivedJson);
                    Console.WriteLine(receivedJson);

                    PROTOCOL client_select_menu = receivedInfo.pt_id;
                    Console.WriteLine(client_select_menu);

                    PROTOCOL pt_id = PROTOCOL.Setting;
                    string nickname = "";
                    int scene = -1;
                    List<string> friendslist = new List<string>();

                    message signup_login_send = new message();
                    login_success_info signup_login_state = new login_success_info();
                    if (client_select_menu == PROTOCOL.SIGNUP_Request)
                    {
                        try
                        {
                            userManager.addUser(receivedInfo.signup_login_info.Email, receivedInfo.signup_login_info.PW, receivedInfo.signup_login_info.Nickname);
                            userManager.addCharactor(receivedInfo.signup_login_info.Nickname, 0, -2715.0, 2612.0, 0, 0, 0, 0);
                            //minigameManager.addMinigame(receivedInfo.signup_login_info.Nickname);
                            Console.WriteLine("Send Signup Access");
                            pt_id = PROTOCOL.SIGNUP_Success;

                            // UserCharactor 정보 생성 
                        }
                        catch (MySqlException)
                        {
                            Console.WriteLine("이미 존재하는 유저입니다.");
                            pt_id = PROTOCOL.SIGNUP_Fail;
                        }
                    }
                    else if (client_select_menu == PROTOCOL.LOGIN_Request)
                    {
                        if (userManager.userCheck(receivedInfo.signup_login_info.Email, receivedInfo.signup_login_info.PW))
                        {
                            Console.WriteLine("로그인 요청...");
                            pt_id = PROTOCOL.LOGIN_Success;

                            // 추가 사용자 정보 가져오기
                            nickname = userManager.getUserInfo(receivedInfo.signup_login_info.Email); // 이메일을 사용해야 함
                            userManager.setUserLoginST(nickname, 1);

                            Console.WriteLine(userManager.GetCharacterInfo(nickname));
                            // 캐릭터 정보 가져오기
                            CharacterInfo characterInfo = userManager.GetCharacterInfo(nickname); // nickname으로 가져오기

                            if (characterInfo != null)
                            {
                                string current_nickname = characterInfo.Nickname;
                                Console.WriteLine("캐릭터 정보 불러오는중...");
                                // 캐릭터 정보를 signup_login_state에 저장
                                signup_login_state.Nickname = current_nickname;
                                signup_login_state.scene_num = characterInfo.SceneNum;
                                signup_login_state.x_position = characterInfo.XPosition;
                                signup_login_state.y_position = characterInfo.YPosition;
                                signup_login_state.semester = characterInfo.Semester;
                                signup_login_state.main_quest_num = characterInfo.MainQuestNum;
                                signup_login_state.detail_quest_num = characterInfo.DetailQuestNum;
                                signup_login_state.quest_state = characterInfo.QuestState;

                                // 미니게임 기록 가져오기
                                var (minigame_nums, scores) = minigameManager.GetMinigameRecord(nickname); // 튜플로 반환된 값 받기
                                                                                                           // 받은 미니게임 번호와 점수를 signup_login_state에 저장
                                                                                                           // 미니게임 기록이 없는 경우
                                if (minigame_nums == null || minigame_nums.Count == 0)
                                {
                                    // 새로운 미니게임 기록 생성
                                    minigameManager.addMinigame(nickname); // 닉네임을 사용하여 미니게임을 생성합니다.

                                    // 미니게임 기록을 다시 가져옴
                                    (minigame_nums, scores) = minigameManager.GetMinigameRecord(nickname);
                                    Console.WriteLine("미니게임 기록 다시 불러오기 성공");
                                }

                                // 미니게임 기록이 있는 경우
                                if (minigame_nums != null && minigame_nums.Count > 0)
                                {
                                    // 기록을 사용하여 필요한 로직 수행
                                    for (int i = 0; i < minigame_nums.Count; i++)
                                    {
                                        Console.WriteLine($"Minigame Num: {minigame_nums[i]}, Score: {scores[i]}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("미니게임 기록을 가져올 수 없습니다.");
                                }

                                signup_login_state.minigame_nums = minigame_nums;
                                signup_login_state.scores = scores;
                                Console.WriteLine(minigame_nums);
                                Console.WriteLine(scores);

                                // 현재 장면 번호 설정
                                scene = signup_login_state.scene_num;

                                friendslist = friendManager.showFriends(current_nickname);  //Friends table select
                                List<FriendInfo> send_friend_info = new List<FriendInfo>();
                                foreach(string nick in friendslist)
                                {
                                    FriendInfo into_send_friend_info = new FriendInfo();
                                    into_send_friend_info.Nickname = nick;
                                    into_send_friend_info.IsOnline = checkOnline(nick, current_nickname, 0);

                                    send_friend_info.Add(into_send_friend_info);
                                }
                                signup_login_state.friends = send_friend_info;
                                signup_login_state.friendrequest = friendManager.showFriendRequest(current_nickname);   //FriendRequest table select(Friend1)
                                signup_login_state.requestreceived = friendManager.showRequestReceive(current_nickname);    //FriendRequest table select(Friend2)

                                islogon = true;
                                signup_login_send.first_login_info = signup_login_state;

                                Console.WriteLine("캐릭터 정보 불러오기 완료");
                            }
                            else
                            {
                                Console.WriteLine("캐릭터 정보 불러오기 실패");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Send Login Fail");
                            pt_id = PROTOCOL.LOGIN_Fail;
                        }

                    }

                    signup_login_send.pt_id = pt_id;
                    string jsonData = JsonConvert.SerializeObject(signup_login_send);
                    Console.WriteLine(jsonData);

                    byte[] messageBuffer = Encoding.UTF8.GetBytes(jsonData);
                    int messageLength = messageBuffer.Length;
                    byte[] lengthBuffer = BitConverter.GetBytes(messageLength);

                    clientSocket.Send(lengthBuffer);
                    clientSocket.Send(messageBuffer);

                    if (islogon == false) StartReceive(clientSocket);
                    else callback_on_newclient(clientSocket, null, receivedInfo.signup_login_info.Email, nickname, scene, friendslist);
                }
                else
                {
                    Console.WriteLine(e.SocketError);
                    client_sockets.Remove(clientSocket);
                    clientSocket.Close();
                    Console.WriteLine("Client disconnected >> " + client_sockets.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling receive: " + ex.Message);
                // 에러 처리
            }
        }

        public bool checkOnline(string nickname, string onlineoffline_nickname, int OnlineOffline)
        {
            foreach(var t in users)
            {
                if (t.client_nickname == nickname)
                {
                    if (OnlineOffline == 0) sendFriendOnline(t, onlineoffline_nickname);
                    else sendFriendOffline(t, onlineoffline_nickname);
                    return true;
                }
            }
            return false;
        }
        public void sendFriendOnline(Token friend, string online_nickname)
        {
            message sendfriendOnline = new message();
            sendfriendOnline.pt_id = PROTOCOL.Friend_Online;
            InGame_message Online_nick = new InGame_message();
            Online_nick.friendnick = online_nickname;
            sendfriendOnline.ingame_info = Online_nick;
            string new_sendfriendOnline = JsonConvert.SerializeObject(sendfriendOnline);
            SendDataToToken(friend, new_sendfriendOnline);
        }
        public void sendFriendOffline(Token friend, string offline_nickname)
        {
            message sendfriendOffline = new message();
            sendfriendOffline.pt_id = PROTOCOL.Friend_Offline;
            InGame_message Offline_nick = new InGame_message();
            Offline_nick.friendnick = offline_nickname;
            sendfriendOffline.ingame_info = Offline_nick;
            string new_sendfriendOffline = JsonConvert.SerializeObject(sendfriendOffline);
            SendDataToToken(friend, new_sendfriendOffline);
        }
        void on_new_client(Socket client_socket, object token, string client_ID, string client_nickname, int scene_num, List<string> friendslist)
        {
            Console.WriteLine("on_new_client");
            //멀티 스레딩에서 connected_count를 안전하게 증가시키는 역할
            //다른 스레드가 이 변수에 동시에 접근하여 값을 변경하는 것을 방지하고 원자적으로 증가
            Interlocked.Increment(ref this.connected_count);
            SocketAsyncEventArgs receive_args = this.receive_event_pool.Pop();
            SocketAsyncEventArgs send_args = this.send_event_pool.Pop();
            Token client_token = new Token(client_ID);
            client_token.on_session_closed += this.on_session_closed;
            receive_args.UserToken = client_token;
            send_args.UserToken = client_token;
            client_token.client_nickname = client_nickname;
            client_token.scene_num = scene_num;
            client_token.friend_nickname_list = friendslist;
            begin_receive(client_socket, receive_args, send_args);

            if (this.session_created_callback != null)
            {
                Console.WriteLine("add user");
                this.session_created_callback(client_token);
            }
            Console.WriteLine("connected_count >> " + users.Count + "\n");

        }
        void begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
        {
            Console.WriteLine("Chat Program Start");
            Token user_token = receive_args.UserToken as Token;
            user_token.set_event_args(send_args, receive_args);
            user_token.socket = socket;

            byte[] client_receive = new byte[1024];
            user_token.receive_event_args.SetBuffer(client_receive, 0, client_receive.Length);

            bool pending = socket.ReceiveAsync(receive_args);
            if (!pending)
            {
                chat_client_receive_completed(socket, receive_args);
            }
        }
        void chat_client_receive_completed(object sender, SocketAsyncEventArgs e)
        {
            Token user_token = e.UserToken as Token;
            try
            {
                int bytesReceived = e.BytesTransferred;
                if (bytesReceived > 0 && e.SocketError == SocketError.Success)
                {
                    user_token.AppendData(e.Buffer, bytesReceived);

                    // 메시지 길이가 설정되지 않았다면 시도
                    if (!user_token.HasMessageLength)
                    {
                        if (user_token.TryReadMessageLength())
                        {
                            ProcessCompleteMessages(user_token);
                        }
                    }
                    else
                    {
                        ProcessCompleteMessages(user_token);
                    }

                    bool pending = user_token.socket.ReceiveAsync(e);
                    if (!pending) chat_client_receive_completed(sender, e);
                }
                else
                {
                    Console.WriteLine("Logon Client disconnected");
                    client_sockets.Remove(user_token.socket);
                    user_token.socket.Close();
                    Console.WriteLine("Client disconnected >> " + client_sockets.Count);
                    on_session_closed(user_token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling receive: " + ex.Message);
                Console.WriteLine("Logon Client disconnected");
                client_sockets.Remove(user_token.socket);
                user_token.socket.Close();
                Console.WriteLine("Client disconnected >> " + client_sockets.Count);
                on_session_closed(user_token);
            }
        }
        private BlockingCollection<every_message> messageQueue = new BlockingCollection<every_message>();
        void ProcessCompleteMessages(Token user_token)
        {
            string completeMessage;
            while ((completeMessage = user_token.GetCompleteMessage()) != null)
            {
                // 완전한 메시지가 수신되었을 경우 처리
                message received_info = JsonConvert.DeserializeObject<message>(completeMessage);
                every_message new_tokens_message = new every_message();
                new_tokens_message.user_token = user_token;
                new_tokens_message.new_message = received_info;

                // 큐에 추가 (TryAdd를 사용하여 예외 방지)
                if (!messageQueue.TryAdd(new_tokens_message))
                {
                    Console.WriteLine("Message queue is full, dropping message");
                }
            }
        }
        void StartMessageProcessingThread()
        {
            Thread messageProcessingThread = new Thread(ProcessMessagesFromQueue);
            messageProcessingThread.IsBackground = true;
            messageProcessingThread.Start();
        }
        void ProcessMessagesFromQueue()
        {
            foreach (var message in messageQueue.GetConsumingEnumerable())
            {
                // 메시지 처리
                HandleMessage(message);
            }
        }
        void HandleMessage(every_message received_info)
        {
            Token owner = received_info.user_token;
            message new_message_content = received_info.new_message;
            if (new_message_content.pt_id == PROTOCOL.Position_Update)
            {
                //Console.WriteLine(completeMessage);
                // 사용자 위치, 씬 넘버 업데이트
                userManager.setCharacterInfo(owner.client_nickname, new_message_content.ingame_info.scene_num, new_message_content.ingame_info.x_position, new_message_content.ingame_info.y_position);
                //Console.WriteLine("Opcode: " + received_info.pt_id + ", Token's nick: " + user_token.client_nickname + ", ingame_info(about position): { scene_num: " + received_info.ingame_info.scene_num + ", x_position: " + received_info.ingame_info.x_position + ", y_position: " + received_info.ingame_info.y_position + ", DirX: " + received_info.ingame_info.DirX + ", DirY: " + received_info.ingame_info.DirY + " }");

                message new_message1 = new message();
                new_message1.pt_id = PROTOCOL.Deliver_Position;
                InGame_message other_user_position = new InGame_message();
                other_user_position.own_nickname = new_message_content.ingame_info.own_nickname;
                other_user_position.scene_num = new_message_content.ingame_info.scene_num;
                other_user_position.x_position = new_message_content.ingame_info.x_position;
                other_user_position.y_position = new_message_content.ingame_info.y_position;
                other_user_position.DirX = new_message_content.ingame_info.DirX;
                other_user_position.DirY = new_message_content.ingame_info.DirY;
                new_message1.ingame_info = other_user_position;
                string new_deliver_message1 = JsonConvert.SerializeObject(new_message1);

                message new_message2 = new message();
                new_message2.pt_id = PROTOCOL.Delete_User;
                InGame_message other_user_off = new InGame_message();
                other_user_off.own_nickname = owner.client_nickname;
                other_user_off.scene_num = owner.scene_num;
                new_message2.ingame_info = other_user_off;
                string new_deliver_message2 = JsonConvert.SerializeObject(new_message2);

                if (owner.scene_num == new_message_content.ingame_info.scene_num)    //씬 이동이 없을 때
                {
                    foreach (Token t in users)  //같은 씬에 있는 유저들에게 위치 정보 공유 메시지
                    {
                        if (t.client_nickname != owner.client_nickname && t.scene_num == owner.scene_num)
                        {
                            try
                            {
                                SendDataToToken(t, new_deliver_message1);
                            }
                            catch (Exception sendEx)
                            {
                                Console.WriteLine("Error sending to client: " + sendEx.Message);
                            }
                        }
                    }
                }
                else    //씬 이동이 있을 때
                {
                    foreach (Token t in users)
                    {
                        if (t.client_nickname != owner.client_nickname && t.scene_num == owner.scene_num)
                        {
                            //이전 씬에 있는 유저들에게 캐릭터 삭제 메시지
                            try
                            {
                                SendDataToToken(t, new_deliver_message2);
                            }
                            catch (Exception sendEx)
                            {
                                Console.WriteLine("Error sending to client: " + sendEx.Message);
                            }
                        }
                    }
                    owner.scene_num = new_message_content.ingame_info.scene_num; //씬 정보 업데이트
                    foreach (Token t in users)  //이동한 씬에 있는 유저들에게 캐릭터 위치 정보 공유
                    {
                        if (t.client_nickname != owner.client_nickname && t.scene_num == owner.scene_num)
                        {
                            try
                            {
                                SendDataToToken(t, new_deliver_message1);
                            }
                            catch (Exception sendEx)
                            {
                                Console.WriteLine("Error sending to client: " + sendEx.Message);
                            }
                        }
                    }
                }
            }
            else if (new_message_content.pt_id == PROTOCOL.Send_Message || new_message_content.pt_id == PROTOCOL.GroupChatRoom_Create_Request || new_message_content.pt_id == PROTOCOL.GroupChat_Invite_Request || new_message_content.pt_id == PROTOCOL.Invite_Accept_Request || new_message_content.pt_id == PROTOCOL.Invite_Reject_Request || new_message_content.pt_id == PROTOCOL.GroupChat_Exit_Request)
            {
                Console.WriteLine("Chat Manager >> " + "Nick: " + owner.client_nickname + " >> " + new_message_content.pt_id);
                chat_manager_cs.enqueue_chat_message(owner, new_message_content);
            }
            else if (new_message_content.pt_id == PROTOCOL.Quest_Start_Request || new_message_content.pt_id == PROTOCOL.Quest_Complete_Request || new_message_content.pt_id == PROTOCOL.MiniGame_End_Request || new_message_content.pt_id == PROTOCOL.Sub_Quest_End_Request)
            {
                Console.WriteLine("\nClient Send Quest Message\n");
                game_manager_cs.enqueue_game_message(owner, new_message_content);
            }
            else if (new_message_content.pt_id == PROTOCOL.Friend_Delete_Request || new_message_content.pt_id == PROTOCOL.User_Search_Request || new_message_content.pt_id == PROTOCOL.Friend_Request || new_message_content.pt_id == PROTOCOL.Friend_Request_Cancel || new_message_content.pt_id == PROTOCOL.Friend_Access_Request || new_message_content.pt_id == PROTOCOL.Friend_Reject_Reqeust)
            {
                friend_manager_cs.enqueue_friend_message(owner, new_message_content);
            }

        }
        class send_queue_data
        {
            public Token owner;
            public string message;
        }
        private BlockingCollection<send_queue_data> SendMessageQueue = new BlockingCollection<send_queue_data>();

        public void SendDataToToken(Token token, string message)
        {
            send_queue_data new_send_data = new send_queue_data();
            new_send_data.owner = token;
            new_send_data.message = message;
            if (!SendMessageQueue.TryAdd(new_send_data))
            {
                Console.WriteLine("Send Messasge Queue is full, dropping message");
            }
        }
        void StartSendMessageProcessingThread()
        {
            Thread messageProcessingThread = new Thread(SendMessageProcessing);
            messageProcessingThread.IsBackground = true;
            messageProcessingThread.Start();
        }
        void SendMessageProcessing()
        {
            foreach (var message in SendMessageQueue.GetConsumingEnumerable())
            {
                HandleSendMessage(message);
            }
        }
        void HandleSendMessage(send_queue_data new_send_message)
        {
            Token owner = new_send_message.owner;
            string new_send = new_send_message.message;
            try
            {
                byte[] messageBuffer = Encoding.UTF8.GetBytes(new_send);
                int messageLength = messageBuffer.Length;

                byte[] lengthBuffer = BitConverter.GetBytes(messageLength);

                byte[] combinedBuffer = new byte[lengthBuffer.Length + messageBuffer.Length];
                Buffer.BlockCopy(lengthBuffer, 0, combinedBuffer, 0, lengthBuffer.Length);
                Buffer.BlockCopy(messageBuffer, 0, combinedBuffer, lengthBuffer.Length, messageBuffer.Length);

                lock (owner.socket) // 동기화 추가
                {
                    owner.socket.Send(combinedBuffer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending data to token: " + ex.Message);
            }
        }

        void add_users(Token token)
        {
            Console.WriteLine("on_session_created");
            lock (users)
            {
                this.users.Add(token);
            }
        }
        void on_session_closed(Token token)
        {
            Interlocked.Decrement(ref this.connected_count);
            Console.WriteLine("on_session_closed");
            lock (users)
            {
                this.users.Remove(token);
                if (this.receive_event_pool != null) this.receive_event_pool.Push(token.receive_event_args);
                if (this.send_event_pool != null) this.send_event_pool.Push(token.send_event_args);
                userManager.setUserLoginST(token.client_ID, 0);

                message new_message = new message();
                new_message.pt_id = PROTOCOL.Delete_User;
                InGame_message other_user_off = new InGame_message();
                other_user_off.own_nickname = token.client_nickname;
                other_user_off.scene_num = token.scene_num;
                new_message.ingame_info = other_user_off;
                string new_deliver_message = JsonConvert.SerializeObject(new_message);

                foreach (Token t in users)
                {
                    try
                    {
                        SendDataToToken(t, new_deliver_message);
                    }
                    catch (Exception sendEx)
                    {
                        Console.WriteLine("Error sending to client: " + sendEx.Message);
                    }
                }

                foreach (string nick in token.friend_nickname_list)
                {
                    checkOnline(nick, token.client_nickname, 1);
                }
                Console.WriteLine("user connected_count >> " + users.Count + "\n");

                chat_manager_cs.handle_group_chat_exit_on_session_closed(token);
                token.token_close();
            }
        }
        public List<Token> deliver_current_tokens()
        {
            return users;
        }
        public Token find_user(string nick)
        {
            foreach (Token t in users)
            {
                if (t.client_nickname.Equals(nick)) return t;
            }
            return null;
        }
    }
}
