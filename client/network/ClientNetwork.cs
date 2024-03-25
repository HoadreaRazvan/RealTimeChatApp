using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;

namespace network
{
    public class ClientNetwork
    {
        private TcpClient tcpClient;
        private Thread clientThread;

        public string send,receive;

        public ClientNetwork()
        {
            ConnectToServer();
        }

        public string adresaIpRouter()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in networkInterfaces)
                if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    UnicastIPAddressInformationCollection unicastIPAddresses = ipProperties.UnicastAddresses;
                    foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
                        if (unicastIPAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            return unicastIPAddress.Address.ToString();
                }
            return null;
        }

        private void ConnectToServer()
        {

            this.tcpClient = new TcpClient(adresaIpRouter(), 8080);
            this.clientThread = new Thread(new ThreadStart(ListenForMessages));
            this.clientThread.Start();
        }

        private void ListenForMessages()
        {
            NetworkStream clientStream = this.tcpClient.GetStream();
            byte[] message = new byte[4096];
            int bytesRead;
            while (true)
            {
                bytesRead = 0;
                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }
                if (bytesRead == 0)
                    break;
                receive = Encoding.ASCII.GetString(message, 0, bytesRead);
            }
        }

        private void SendMessage()
        {
            if (tcpClient == null || !tcpClient.Connected)
            {
                MessageBox.Show("Not connected to the server.");
                return;
            }
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(send);
            clientStream.Write(data, 0, data.Length);
            clientStream.Flush();
        }


        private void close()
        {
            this.send = "/disconnect";
            SendMessage();
            tcpClient.Close();
            Application.Current.Shutdown();
        }
    }
}
