using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;

namespace network
{
    public class ServerNetwork
    {
        private TcpListener tcpListener;
        private Thread listenerThread;
        private ConcurrentDictionary<string, TcpClient> connectedClients = new ConcurrentDictionary<string, TcpClient>();
        //connectedClients.Keys
        private bool isClosing = false;
        
        public string send, receive;

        public ServerNetwork()
        {
            StartServer();
        }

        private void StartServer()
        {
            tcpListener = new TcpListener(IPAddress.Any, 8080);
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            listenerThread.Start();
        }

        private void ListenForClients()
        {
            tcpListener.Start();
            while (!isClosing)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        break;
                    else
                        throw;
                }
            }
        }


        private void HandleClientComm(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            string clientEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            connectedClients.TryAdd(clientEndPoint, tcpClient);
            NetworkStream clientStream = tcpClient.GetStream();
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
            connectedClients.TryRemove(clientEndPoint, out _);
            tcpClient.Close();
        }

  
        private void BroadcastMessage()
        {
            foreach (var clientPair in connectedClients)
            {
                NetworkStream clientStream = clientPair.Value.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(send);
                clientStream.Write(data, 0, data.Length);
                clientStream.Flush();
            }
        }

        private void exit()
        {
            this.send = "Exiting...  Serverul se închide. Toti clientii vor fi deconectati.   Server closed.";
            BroadcastMessage();
            isClosing = true;
            foreach (var clientPair in connectedClients)
                clientPair.Value.Close();
            connectedClients.Clear();
            tcpListener.Stop();
            Application.Current.Shutdown();
        }
    }
}
