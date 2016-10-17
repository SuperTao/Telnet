using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalisticTelnet
{
    class Program
    {
        static void Main(string[] args)
        {
            //create a new telnet connection to host "192.168.2.111" on port "23"
            //连接telnet服务器， 23号端口就是telnet端口
            TelnetConnection tc = new TelnetConnection("192.168.2.111", 23);

            string s = tc.Login("root", "root",100);
            Console.Write(s);

            string prompt = "";
            // while connected
            while (tc.IsConnected && prompt.Trim() != "exit" )
            {
                // display server output
                Console.Write(tc.Read());

                // send client input to server
                prompt = Console.ReadLine();
                tc.WriteLine(prompt);

                // display server output
                Console.Write(tc.Read());
            }
            Console.WriteLine("***DISCONNECTED");
            Console.ReadLine();
            
        }
    }
}
