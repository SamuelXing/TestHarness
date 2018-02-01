////////////////////////////////////////////////////////////////////////////////
// Client.cs - sends TestRequests, displays results                           //
//                                                                            //
// Jim Fawcett, Zihao Xing CSE681 - Software Modeling and Analysis, Fall 2016 //
////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The main functionalities of this package are:
 * - Build and Send TestRequests
 * - Make Query
 * 
 * Required Files:
 * - Client.cs, ITest.cs, Logger.cs, CommService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Oct 2016
 * - first release
 * ver 1.1 : 10 Nov 2016
 * - implement send files to Repository Server
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace TestHarness
{
    public class Client : IClient
    {
        public IStreamService str_svc;  //FileStream Service
        int blockSize = 1024;       //Send block
        byte[] block;
        TestHarness.HiResTimer timer = null; //Set Timer
        public readonly string SavedPath = "..\\..\\TestResults";
        public Thread rcvThread = null;

        public Comm<Client> Comm { get; set; } = new Comm<Client>();
        public string ClientEndpoint { get; } = Comm<Client>.makeEndpoint("http://localhost",5001);
        public Client()
        {
            DLog.write("\n  Creating instance of Client");
            block = new byte[blockSize];
            timer = new HiResTimer();
        }

        void rcvThreadProc()
        {
            while (true)
            {
                CommMessage msg = Comm.rcvR.GetMessage();
                msg.TimeStamp = DateTime.Now;
                Console.Write("\n {0} recieved message:",Comm.name);
                Console.Write("\n "+msg.ToString());
                if (msg.MessageBody == "quit")
                {
                    break;
                }
            }
        }

        public void wait()
        {
            rcvThread.Join();
        }

        public CommMessage makeMessage(string author, string fromUrl, string ToUrl)
        {
            CommMessage msg = new CommMessage();
            msg.Author = author;
            msg.FromUrl = fromUrl;
            msg.ToUrl = ToUrl;
            return msg;
        }
        public static IStreamService CreateServiceChannel(string url)
        {
            BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
            ///////////////////////////////////
            BasicHttpBinding binding = new BasicHttpBinding(securityMode);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 500000000;
            EndpointAddress address = new EndpointAddress(url);

            ChannelFactory<IStreamService> factory =
                new ChannelFactory<IStreamService>(binding, address);
            return factory.CreateChannel();
        }

        public void upLoadFile(string FilenameWithPath)
        {
            //string fqname = Path.Combine(DLLPath,filename);
            try
            {
                timer.Start();
                using (var inputStream=new FileStream(FilenameWithPath, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = System.IO.Path.GetFileName(FilenameWithPath); ;
                    msg.transferStream = inputStream;
                    str_svc.upLoadFile(msg);
                }
                timer.Stop();
                Console.WriteLine("\n  Uploaded file \"{0}\" in {1} microsec.", System.IO.Path.GetFileName(FilenameWithPath), timer.ElapsedMicroseconds);
            }
            catch
            {
                Console.WriteLine("can't find \"{0}\"",FilenameWithPath);
            }
        }

        public void downLoadFile(string filename)
        {
            int totalBytes = 0;
            try
            {
                Stream strm = str_svc.downLoadFile(filename);
                string rfilename = Path.Combine(SavedPath,filename);
                if (!Directory.Exists(SavedPath))
                    Directory.CreateDirectory(SavedPath);
                using (var outputStream = new FileStream(rfilename, FileMode.Create, FileAccess.ReadWrite))
                {
                    while (true)
                    {
                        int byteRead = strm.Read(block,0,blockSize);
                        totalBytes += byteRead;
                        if (byteRead > 0)
                            outputStream.Write(block, 0, byteRead);
                        else
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}",ex.Message);
            }
        }

        public List<string> GetAllDLLs()
        {
            List<string> files = str_svc.GetAllDLLs();
            return files;
        }
        public void CloseStream()
        {
            ChannelFactory<IStreamService> temp = (ChannelFactory<IStreamService>)str_svc;
            temp.Close();
        }
        public void sendTestRequest(CommMessage testRequest)
        {
            this.Comm.sndR.PostMessage(testRequest);
            return;
        }
        public void sendResults(CommMessage results)
        {
            RLog.write("\n  Client received results message:");
            RLog.write("\n  " + results.ToString());
            RLog.putLine();
        }
        public void makeQuery(CommMessage QueryText)
        {
            this.Comm.sndR.PostMessage(QueryText);
            Console.Write("\n Query Test Results -------------------Req #9");
            return;
        }

#if (TEST_CLIENT)
        static void Main(string[] args)
        {
            /*
             * ToDo: add code to test 
             * - Add code in Client class to make queries into Repository for
             *   information about libraries and logs.
             * - Add code in Client class to sent files to Repository.
             * - Add ClientTest class that implements ITest so Client
             *   functionality can be tested in TestHarness.
             */

            /////////////////////////////////////////////
            // uncomment lines below to test send files to repository server
            // note: 1, run as Administrator;2, set Repository and Client as startup projects 

            //Client cln = new Client();
            //cln.str_svc = CreateServiceChannel("http://localhost:8000/StreamService");
            //HiResTimer timer = new HiResTimer();
            //timer.Start();
            //cln.upLoadFile("Logger.dll");
            //timer.Stop();
            //Console.Write("\n\n  total elapsed time for uploading = {0} microsec.\n", timer.ElapsedMicroseconds);
            //((IChannel)cln.str_svc).Close();
            //Thread.Sleep(1000);

            ///////////////////////////////////////////////// 
            //uncomment lines below to test send message to TestHarness Server
            // note: 1, run as Administrator;2, set TestHarness and Client as startup projects 

            //Console.WriteLine("Client Start Working");
            //Client clnt = new Client();
            //CommMessage cmg = new CommMessage();
            //cmg.ToUrl = "http://localhost:5000/IService";
            //cmg.FromUrl = "http://localhost:5001/IService";
            //cmg.Author = "SamuelXing";
            //cmg.TimeStamp = DateTime.Now;
            //cmg.messageType = CommMessage.MessageType.TestRequest;
            //cmg.MessageBody = "Hello, World!";

            //Console.WriteLine("Send Message #1");
            //Console.WriteLine(cmg.ToString());
            //clnt.sendTestRequest(cmg);
            //Console.WriteLine("Send Message #2");
            //Console.WriteLine(cmg.ToString());
            //clnt.sendTestRequest(cmg);
            //Console.WriteLine("Send Message #3");
            //Console.WriteLine(cmg.ToString());
            //clnt.sendTestRequest(cmg);
            //clnt.CloseService();

            Console.Write("\n Demo: Client starts working");
            Console.Write("\n +=========================+");

            Client clnt = new Client();
            clnt.Comm.rcvR.CreateRecvChannel(clnt.ClientEndpoint);
            clnt.rcvThread= clnt.Comm.rcvR.start(clnt.rcvThreadProc);

            CommMessage msg = clnt.makeMessage("SamuelXing", clnt.ClientEndpoint, Comm<Client>.makeEndpoint("http://localhost",5000));
            msg.MessageBody = "Hello-send from Client";
            //clnt.Comm.sndR.PostMessage(msg);
            clnt.sendTestRequest(msg);

            Console.Write("\n press key to exit: ");
            Console.ReadKey();
            msg.ToUrl = clnt.ClientEndpoint;
            msg.MessageBody = "quit";
            clnt.Comm.sndR.PostMessage(msg);
            clnt.wait();
            Console.Write("\n\n");

        }
#endif
    }
}
