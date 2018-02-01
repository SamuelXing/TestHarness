//////////////////////////////////////////////////////////////
//MainWindow.xaml.cs         - provides GUI's view control  //
//                                                          //
//Zihao Xing, CSE681 - Software Modeling and Analysis       //
//////////////////////////////////////////////////////////////
/*
 * Package Operations: 
 *---------------------
 * This package contains Graphic User Interface, 
 * which is demonstrated in MainWindow.xaml, 
 * and view-control behind GUI.
 * 
 * Required Files:
 * Client.cs, CommerService.cs, Timer.cs, Serialization.cs
 * 
 * Maintenance History:
 * --------------------
 * version 1.0: 11 Nov 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using TestHarness;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Collections.ObjectModel;

namespace TestHarnessGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Client clnt = new Client();
        testRequest tstR = new testRequest();
        CommMessage msg = new CommMessage();
        HiResTimer timer = null;
        private void BuildMessage()
        {       
            string localPort = LocalPort0.Text;
            string ClientEndPoint = Comm<Client>.makeEndpoint("http://localhost", Int32.Parse(localPort));
            msg.FromUrl = ClientEndPoint;
            string TestHarnessAddress = THAdddress_TextBox.Text;
            string TestHarnessPort = RemotePort0.Text;
            string THAddress = Comm<Client>.makeEndpoint(TestHarnessAddress, Int32.Parse(TestHarnessPort));
            msg.ToUrl = THAddress;
            msg.Author = textBox.Text.Trim();
            msg.TimeStamp = DateTime.Now;
            msg.messageType = CommMessage.MessageType.TestRequest;
            msg.MessageBody = tstR.ToXml();
        }

        delegate void NewMessage(CommMessage msg);
        event NewMessage OnNewMessage;
        //-----<Receieve Thread Processiing>
        void RcvThreadProc()
        {
            while (true)
            {
                //Get message out of recieve queu.
                CommMessage msg = clnt.Comm.rcvR.GetMessage();
                //Call window function on UI thread
                this.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            OnNewMessage,
                            msg
                            );         
            }
        }
        //----------<This class is defined to combine each test result with a checkBox>--------
        public class Passage
        {
            public string file { get; set; }
            public bool IsSelected { get; set; }
        }
        public ObservableCollection<Passage> Passages { get; set; } = new ObservableCollection<Passage>();
        void OnNewMessageHandler(CommMessage msg)
        {
            switch (msg.messageType)
            {
                case CommMessage.MessageType.TestResult:
                    TestResults_ListBox.Items.Clear();
                    if (timer!=null)
                    {
                        timer.Stop();
                        StringBuilder stb = new StringBuilder();
                        stb.Append("Execution Time: ");
                        stb.Append(timer.ElapsedMicroseconds);
                        stb.Append(" microSeconds");
                        TestResults_ListBox.Items.Add(stb.ToString());
                        Console.Write("\n Execution Time: {0} microSeconds --------------------------Req #12", timer.ElapsedMicroseconds);
                    }
                    TestResults_ListBox.Items.Add(msg.MessageBody.ToString());
                    return;
                case CommMessage.MessageType.TestResultReply:
                    Passages.Clear();
                    testResultsReply trr = msg.MessageBody.FromXml<testResultsReply>();
                    foreach (var file in trr.files)
                    {
                        Passage passage = new Passage();
                        passage.file = file;
                        passage.IsSelected = false;
                        Passages.Add(passage);
                    }
                    QeryResults_ListBox.ItemsSource = Passages;
                    if (Passages != null)
                    {
                        DownLoad_button.IsEnabled = true;
                    }
                    return;
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            Title = "Client";
            OnNewMessage += new NewMessage(OnNewMessageHandler);
            ConnectButton.IsEnabled = false;
            ConnectButton1.IsEnabled = true;
        }

        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            string localPort = LocalPort0.Text;
            string ClientEndPoint = Comm<Client>.makeEndpoint("http://localhost",Int32.Parse(localPort));
            try
            {
                clnt.Comm.rcvR.CreateRecvChannel(ClientEndPoint);            
                clnt.rcvThread = clnt.Comm.rcvR.start(this.RcvThreadProc);
                clnt.rcvThread.Start();
                clnt.rcvThread.IsBackground = true;
                ConnectButton.IsEnabled = true;
                ListenButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                msg.Append("\nport = ");
                msg.Append(localPort);
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
           
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string TestHarnessAddress = THAdddress_TextBox.Text;
            string TestHarnessPort = RemotePort0.Text;
            //////////////////////////
            string THAddress = Comm<Client>.makeEndpoint(TestHarnessAddress, Int32.Parse(TestHarnessPort));
            ConnectButton.IsEnabled = false;
        }

        private void ConnectButton2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectButton2.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }

        private void ConnectButton1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                clnt.str_svc = Client.CreateServiceChannel(
                RepoAddress_TextBox.Text.Trim() + ":"
                + Remoteport1.Text.Trim() + "/StreamService"
                );
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
           
            ConnectButton1.IsEnabled = false;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddDLLs_Click(object sender, RoutedEventArgs e)
        {
            listBox2.Items.Clear();
            string path = System.IO.Path.GetTempPath();
            var OpenFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Excel Files (*.dll)|*.dll",
               InitialDirectory=System.IO.Path.GetFullPath("../../../Client/DLLFiles"),
            };
            var result = OpenFileDialog.ShowDialog();
            if (result == true)
            {
                this.listBox2.Items.Add(OpenFileDialog.FileName);
            }
            UploadFiles.IsEnabled = true;
        }

        private void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.Write("\n  upload file: ---------------------- Req #5");
                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    clnt.upLoadFile(listBox2.Items[i].ToString());
                    Console.Write("\n {0}",listBox2.Items[i].ToString());
                }
                listBox2.Items.Clear();
                listBox2.Items.Insert(0, "Success!");
                //clnt.CloseStream();
                SendRequest.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
          
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            testElement tstE = new testElement();
            tstR.author = textBox.Text.Trim();
            tstE.testName = textBox1.Text.Trim();
            tstE.addDriver(textBox2.Text.Trim());
            string[] tCode = textBox3.Text.Trim().Split(';');
            foreach (var tc in tCode)
            {
                tstE.addCode(tc);
            }
            tstR.tests.Add(tstE);
            button3.IsEnabled = false;
            button.IsEnabled = true;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            button3.IsEnabled = true;           
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {           
            try
            {
                BuildMessage();
                timer = new HiResTimer();
                timer.Start();
                clnt.sendTestRequest(msg);
                Console.Write("\n Send Message ----------------------Req #5 \n {0}", msg.ToString());
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }          
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            textBox.Text = "";
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            tstR = new testRequest();
            BuildMessage();
            button3.IsEnabled = true;
            button.IsEnabled = false;
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            testResultsQuery trr = new testResultsQuery();
            trr.queryText = TextBoxM.Text.Trim();
            CommMessage Query = new CommMessage();
            string localPort = LocalPort0.Text.Trim();
            string ClientEndPoint = Comm<Client>.makeEndpoint("http://localhost", Int32.Parse(localPort));
            Query.FromUrl = ClientEndPoint;
            string RepositporyAddress = RepoAddress_TextBox.Text.Trim();
            string RepositoryPort = LocalPort1x.Text.Trim();
            string Repository = Comm<Client>.makeEndpoint(RepositporyAddress, Int32.Parse(RepositoryPort));
            Query.ToUrl = Repository;
            Query.Author = LableY.Text.Trim();
            Query.TimeStamp = DateTime.Now;
            Query.messageType = CommMessage.MessageType.TestResultsQuery;
            Query.MessageBody = trr.ToXml();
            clnt.makeQuery(Query);
        }

        private void QeryResults_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DownLoad_button_Click(object sender, RoutedEventArgs e)
        {
            List<Passage> selects = new List<Passage>();
            for (int i = 0; i < QeryResults_ListBox.Items.Count; i++)
            {
                Passage ps=QeryResults_ListBox.Items[i] as Passage;
                if (ps.IsSelected)
                {
                    selects.Add(ps);
                }
            }
            foreach (var ps in selects)
            {
                try
                {
                    clnt.downLoadFile(ps.file);
                }
                catch
                {
                    Console.Write("\n  can't download file");
                }
            }
            Window temp = new Window();
            StringBuilder msg = new StringBuilder();
            msg.Append("success! Test Results are saved in \"~/ClientGUI/TestResults/ \"");
            temp.Content = msg.ToString();
            temp.Height = 100;
            temp.Width = 500;
            temp.Show();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            List<string> files=clnt.GetAllDLLs();
            listBox2.Items.Clear();
            foreach (var file in files)
            {
                listBox2.Items.Add(file);
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            CommMessage msg = new CommMessage();
            msg.ToUrl = Comm<Client>.makeEndpoint("http://localhost", Int32.Parse(LocalPort0.Text));
            msg.MessageBody = "quit";
            clnt.Comm.sndR.PostMessage(msg);
            clnt.Comm.sndR.Close();
            clnt.Comm.rcvR.Close();
        }
    }
}
