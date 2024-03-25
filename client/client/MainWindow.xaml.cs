using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

namespace client 
{
    public partial class MainWindow : Window
    {
        private TcpClient tcpClient;
        private Thread clientThread;
        private bool ok = false;
        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
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

        private void ConnectToServer()
        {
            try
            {
                tcpClient = new TcpClient(GetLocalIPAddress(), 8080);
                clientThread = new Thread(new ThreadStart(ListenForMessages));
                clientThread.Start();
                DisplayMessage($"Conectat la server: {tcpClient.Client.LocalEndPoint}");
            }
            catch (Exception ex)
            {
                ok = true;
                MessageBox.Show($"Nu exista nici-un server activ.\n{ex.Message}");              
                Close();
            }
        }

        private void ListenForMessages()
        {
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
                if (receivedMessage.Split("/").Length == 2 && receivedMessage.Split("/")[1] == "shutdown")
                {
                    MessageBox.Show(receivedMessage.Split("/")[0]);
                    Dispatcher.Invoke(() =>
                        {
                            Close();
                        });
                        break;                 
                }
                DisplayMessage(receivedMessage);
            }
        }

        private void SendMessage(string message)
        {
            if (tcpClient == null || !tcpClient.Connected)
            {
                MessageBox.Show("Nu sunt conectat la server.");
                return;
            }
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message);
            clientStream.Write(data, 0, data.Length);
            clientStream.Flush();
        }

        private void DisplayMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                clientMessages.AppendText($"{message}{Environment.NewLine}");
            });
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = inputTextBox.Text;
            SendMessage(message);
            inputTextBox.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ok == false)
            {        
                SendMessage("/disconnect");
                tcpClient.Close();
            }
            Application.Current.Shutdown();

        }
    }
}
