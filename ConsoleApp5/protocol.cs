using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    public enum PROTOCOL : short
    {
        Setting = 0,

        SIGNUP_Request = 1,
        SIGNUP_Success = 2,
        SIGNUP_Fail = 3,

        LOGIN_Request = 4,
        LOGIN_Success = 5,
        LOGIN_Fail = 6,

        Position_Update = 7,
        Deliver_Position = 8,   //다른 유저의 위치 <- 2단계
        Send_Message = 9,
        Deliver_Message = 10,

        Quest_Start_Request = 11,
        Quest_Start_Success = 12,
        MiniGame_End_Request = 13,
        MiniGame_End_Success = 14,
        Quest_Complete_Request = 15,
        Quest_Complete_Success = 16,

        Delete_User = 17,
        Sub_Quest_End_Request = 18,
        Sub_Quest_End_Success = 19,

        Friend_Online = 20,
        Friend_Offline = 21,

        Friend_Delete_Request = 22,
        Friend_Delete_Success = 23,
        Friend_Delete_Fail = 24,

        User_Search_Request = 25,
        User_Search_Success = 26,
        User_Search_Fail = 27,

        Friend_Request = 28,
        Friend_Request_Success = 29,
        Friend_Request_Fail = 30,

        Friend_Request_Cancel = 31,
        Friend_Request_Cancel_Success = 32,
        Friend_Request_Cancel_Fail = 33,

        Friend_Access_Request = 34,
        Friend_Access_Success = 35,
        Friend_Access_Fail = 36,

        Friend_Reject_Reqeust = 37,
        Friend_Reject_Success = 38,
        Friend_Reject_Fail = 39,

        Friend_Delete_Receive = 40,     //삭제한 유저의 닉네임(InGame_message의 friendnick필드)과 함께 전달
        Friend_Request_Receive = 41,    //요청한 유저의 닉네임(InGame_message의 friendnick필드)과 함께 전달
        Friend_Request_Cancel_Receive = 42,     //요청 취소한 유저의 닉네임(InGame_message의 friendnick필드)과 함께 전달
        Friend_Access_Receive = 43,     //친구 수락한 유저의 닉네임(InGame_message의 friendnick필드)과 함께 전달
        Friend_Reject_Receive = 44,      //친구 거절한 유저의 닉네임(InGame_message의 friendnick필드)과 함께 전달

        GroupChatRoom_Create_Request = 45,
        GroupChatRoom_Create_Success = 46,
        GroupChatRoom_Create_Fail = 47,

        GroupChat_Invite_Request = 48,
        GroupChat_Invite_Success = 49,
        GroupChat_Invite_Fail = 50,

        Invite_Accept_Request = 51,
        Invite_Accept_Success = 52,
        Invite_Accept_Fail = 53,

        Invite_Reject_Request = 54,
        Invite_Reject_Success = 55,
        Invite_Reject_Fail = 56,

        GroupChat_Exit_Request = 57,
        GroupChat_Exit_Success = 58,
        GroupChat_Exit_Fail = 59,

        GroupChat_Invite_Receive = 60,
        GroupChat_Enter_Receive = 61,
        GroupChat_Exit_Receive = 62
    }
}
