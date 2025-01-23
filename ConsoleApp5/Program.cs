using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ConsoleApp5
{
    class Program
    {
        static async Task Main(string[] args)
        {
            NetworkService server = new NetworkService();
            server.start("192.168.0.189", 7979, 100); // Start listening

            while (true)
            {
                await Task.Delay(1000); // 1초 대기
            }
        }
    }
}
