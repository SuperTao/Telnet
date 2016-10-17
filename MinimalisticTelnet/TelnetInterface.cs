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
        //�½�tcp����
        public TelnetConnection(string Hostname, int Port)
        {
            tcpSocket = new TcpClient(Hostname, Port);
        }

        public string Login(string Username,string Password,int LoginTimeOutMs)
        {
            int oldTimeOutMs = TimeOutMs;
            TimeOutMs = LoginTimeOutMs;
            string s = Read();
            //��������ߵÿ����岻��Ҫ�˻���������һ�ξ�ע����
/*            if (!s.TrimEnd().EndsWith(":"))
               throw new Exception("Failed to connect : no login prompt");
            WriteLine(Username);
*/
//            s += Read();
//            if (!s.TrimEnd().EndsWith(":"))
//                throw new Exception("Failed to connect : no password prompt");
            WriteLine(Password);
            //��ȡ��Ӧ������
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
            // ����Telnet�е�IACҲ��0xFF,����뷢��0xFF�����Ӧ���ַ�����Ҫ����2��
            byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF","\0xFF\0xFF"));
            tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }

        public string Read()
        {
            //�ж��Ƿ�����
            if (!tcpSocket.Connected) return null;
            StringBuilder sb=new StringBuilder();
            //��ֲ��ȡ��֪����ȡ��
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
            //�����ܵ����ݵ���������0ʱ
            //tcpSocket.Available �������ȡ����������
            //�����൱��ÿһ��һ���ֽڷ�װ��telnet�İ����з��ͣ�֪��ȫ��������
            while (tcpSocket.Available > 0)
            {
                //��ȡ���ݵĵ�һ���ַ���һ����IAC,��ʾinterpret as command����˼�ǡ���Ϊ���������͡�
                int input = tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case -1 :
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        //��ȡ�ڶ����ֽڣ��鿴ѡ��Э�̵�����
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
                                //��ȡ�������ֽڣ���ʾ���������
                                int inputoption = tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                //��������ظ��Է�
                                //��һ���ֽ���Ȼ��IAC 
                                tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
                                //���͵ڶ����ֽ�,��ʾ��ԶԷ������ĵ������ֽڶ������Ļ�Ӧ��DO or DONT
                                if (inputoption == (int)Options.SGA )
                                    tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL:(byte)Verbs.DO); 
                                else
                                    tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT); 
                                //�����յ�����������Ҳ���أ����öԷ�֪�����ҵĻ�Ӧ�������������ġ�
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
