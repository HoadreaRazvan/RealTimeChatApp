using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;

namespace server
{
    public partial class Window1 : Window
    {
        private TcpListener tcpListener;
        private Thread listenerThread;
        private ConcurrentDictionary<string, TcpClient> connectedClients = new ConcurrentDictionary<string, TcpClient>();
        private bool isClosing = false;

        public Window1()
        {
            InitializeComponent();
            StartServer();
        }

        private string GetLocalIPAddress()
        {
            string? ipAddress = "N/A";
            try
            {
                ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
            }
            catch (Exception ex)
            {
                ipAddress = $"Eroare la obtinerea adresei IP: {ex.Message}";
            }
            return ipAddress;
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
                    DisplayMessage($"Client conectat: {client.Client.RemoteEndPoint}");
                    BroadcastMessage($"Client conectat: {client.Client.RemoteEndPoint}");
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
            Dispatcher.Invoke(() => UpdateClientList());
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
                string receivedMessage = Encoding.ASCII.GetString(message, 0, bytesRead);
                DisplayMessage($"Client {clientEndPoint}: {receivedMessage}");
                BroadcastMessage($"Client {clientEndPoint}: {receivedMessage}");
            }
            connectedClients.TryRemove(clientEndPoint, out _);
            Dispatcher.Invoke(() => UpdateClientList());
            tcpClient.Close();
        }

        private void UpdateClientList()
        {
            connectedClientsList.Text = string.Join(Environment.NewLine, connectedClients.Keys);
        }

        private void DisplayMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                serverMessages.AppendText($"{message}{Environment.NewLine}");
            });
        }

        private void BroadcastMessage(string message)
        {
            foreach (var clientPair in connectedClients)
            {
                NetworkStream clientStream = clientPair.Value.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(message);
                clientStream.Write(data, 0, data.Length);
                clientStream.Flush();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BroadcastMessage("Server inchis! Toti clientii vor fi deconectati./shutdown");
            isClosing = true;
            foreach (var clientPair in connectedClients)
                clientPair.Value.Close();
            connectedClients.Clear();
            tcpListener.Stop();
            Application.Current.Shutdown();
        }
    }
}
