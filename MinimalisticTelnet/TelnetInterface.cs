// minimalistic telnet implementation
// conceived by Tom Janssens on 2007/06/06  for codeproject
//
// http://www.corebvba.be


using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace MinimalisticTelnet
{
    enum Verbs {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    enum Options
    {
        SGA = 3
    }

    class TelnetConnection
    {
        TcpClient tcpSocket;

        int TimeOutMs = 100;
        //新建tcp对象
        public TelnetConnection(string Hostname, int Port)
        {
            tcpSocket = new TcpClient(Hostname, Port);
        }

        public string Login(string Username,string Password,int LoginTimeOutMs)
        {
            int oldTimeOutMs = TimeOutMs;
            TimeOutMs = LoginTimeOutMs;
            string s = Read();
            //由于我这边得开发板不需要账户，所以这一段就注释了
/*            if (!s.TrimEnd().EndsWith(":"))
               throw new Exception("Failed to connect : no login prompt");
            WriteLine(Username);
*/
//            s += Read();
//            if (!s.TrimEnd().EndsWith(":"))
//                throw new Exception("Failed to connect : no password prompt");
            WriteLine(Password);
            //读取回应的数据
            s += Read();
            TimeOutMs = oldTimeOutMs;
            return s;
        }

        public void WriteLine(string cmd)
        {
            Write(cmd + "\n");
        }

        public void Write(string cmd)
        {
            if (!tcpSocket.Connected) return;
            // 由于Telnet中的IAC也是0xFF,如果想发送0xFF这个对应的字符，就要发送2个
            byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF","\0xFF\0xFF"));
            tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }

        public string Read()
        {
            //判断是否连接
            if (!tcpSocket.Connected) return null;
            StringBuilder sb=new StringBuilder();
            //移植读取，知道读取完
            do
            {
                ParseTelnet(sb);
                System.Threading.Thread.Sleep(TimeOutMs);
            } while (tcpSocket.Available > 0);
            return sb.ToString();
        }

        public bool IsConnected
        {
            get { return tcpSocket.Connected; }
        }

        void ParseTelnet(StringBuilder sb)
        {
            //当接受的数据的数量大于0时
            //tcpSocket.Available 从网络读取的数据数量
            //这里相当于每一将一个字节封装成telnet的包进行发送，知道全部发送完
            while (tcpSocket.Available > 0)
            {
                //读取数据的第一个字符，一般是IAC,表示interpret as command，意思是“作为命令来解释”
                int input = tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case -1 :
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        //读取第二个字节，查看选项协商的类型
                        int inputverb = tcpSocket.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int)Verbs.IAC: 
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO: 
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                //读取第三个字节，表示请求的类型
                                int inputoption = tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                //发送命令返回给对方
                                //第一个字节任然是IAC 
                                tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
                                //发送第二个字节,表示针对对方发出的第三个字节而做出的回应，DO or DONT
                                if (inputoption == (int)Options.SGA )
                                    tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL:(byte)Verbs.DO); 
                                else
                                    tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT); 
                                //将接收到的请求类型也返回，好让对方知道，我的回应是针对那条请求的。
                                tcpSocket.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append( (char)input );
                        break;
                }
            }
        }
    }
}
