///////////////////////////////////////////////////////////////////////////
// CommService.cs  - This package defines WCF Service Contracts          //
//                                                                       //
// Zihao Xing CSE681 - Software Modeling and Analysis, Fall 2016         //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The main functionalities of this package are:
 * - define WCF Service Contract
 * - including IService and IStreamService
 * 
 * Required Files:
 * - IService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Oct 2016
 * - first release
 * ver 1.1 : 10 Nov 2016
 * - implement send files to Repository Server
 * ver 1.2 : 14 Nov 2016
 * - make changes to support concurrency in TestHarness
 * ver 2.0: 15 Nov 2016
 * - Second release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using SWTools;
using System.IO;

namespace TestHarness
{
    public class Receiver<T>: IService
    {
        static BlockingQueue<CommMessage> rcvBlockingQ=null;
        ServiceHost service=null;

        public string name { get; set; }

        public Receiver()
        {
            if (rcvBlockingQ == null)
                rcvBlockingQ = new BlockingQueue<CommMessage>();
        }
        public Thread start(ThreadStart rcvThreadProc)
        {
            Thread rcvThread = new Thread(rcvThreadProc);
            //rcvThread.Start();
            return rcvThread;
        }

        public void Close()
        {
            service.Close();
        }
        //Create ServiceHost for Communication
        public void CreateRecvChannel(string address)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            Uri baseAddress = new Uri(address);
            service = new ServiceHost(typeof(Receiver<T>), baseAddress);
            service.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            service.Open();
            Console.Write("\n Service is open listening on {0}", address);
        }
        //Implement Service Method to recieve messages
        public void PostMessage(CommMessage msg)
        {
            Console.Write("\n service enqueue message: \"{0}\" ---------------------- Req #2", msg.MessageBody);
            rcvBlockingQ.enQ(msg);
        }
        //Implement Service Method to extract messages
        public CommMessage GetMessage()
        {
            CommMessage msg= rcvBlockingQ.deQ();
            Console.Write("\n {0} dequeues message from {1}", name, msg.FromUrl);
            return msg;
        }
    }
    public class Sender
    {
        public string name { get; set; }

        IService channel;
        string lastError = "";
        BlockingQueue<CommMessage> sndBlockingQ = null;
        Thread sndThrd = null;
        int tryCount = 0, MaxCount = 10;
        string currEndpoint = "";
        void ThreadProc()
        {
            tryCount = 0;
            while (true)
            {
                CommMessage msg = sndBlockingQ.deQ();
                if (msg.ToUrl != currEndpoint)
                {
                    currEndpoint = msg.ToUrl;
                    CreateMessageSndChannel(currEndpoint);
                }
                while (true)
                {
                    try
                    {
                        channel.PostMessage(msg);
                        Console.Write("\n posted message from {0} to {1}", name, msg.ToUrl);
                        tryCount = 0;
                        break;
                    }
                    catch
                    {
                        Console.Write("\n connection failed");
                        if (++tryCount < MaxCount)
                        {
                            Thread.Sleep(100);
                        }
                        else
                        {
                            Console.Write("\n cannot connect\n");
                            currEndpoint = "";
                            tryCount = 0;
                            break;
                        }
                    }
                }
                if (msg.MessageBody == "quit") break;
            }
        }
        public Sender()
        {
            sndBlockingQ = new BlockingQueue<CommMessage>();
            sndThrd = new Thread(ThreadProc);
            sndThrd.IsBackground = true;
            sndThrd.Start();
        }
        ///Send Message
        public void CreateMessageSndChannel(string ToUrl)
        {
            EndpointAddress baseAddress = new EndpointAddress(ToUrl);
            BasicHttpBinding binding = new BasicHttpBinding();
            ChannelFactory<IService> factory 
                = new ChannelFactory<IService>(binding, ToUrl);
            channel = factory.CreateChannel();
            Console.Write("\n service proxy created for {0}", ToUrl);
        }

        //post message to another peer's queue
        public void PostMessage(CommMessage msg)
        {
            sndBlockingQ.enQ(msg);
        }
        public string GetLastError()
        {
            string temp = lastError;
            lastError = "";
            return temp;
        }
        public void Close()
        {
            ChannelFactory<IService> temp = (ChannelFactory<IService>)channel;
            temp.Close();           
        }
    }
    //Comm class is used to aggregates a Sender and a Reciever
    public class Comm<T>
    {
        public string name { get; set; } = typeof(T).Name;
        public Receiver<T> rcvR { get; set; } = new Receiver<T>();
        public Sender sndR { get; set; } = new Sender();
        public Comm()
        {
            rcvR.name = name;
            sndR.name = name;
            Console.Write("\n Initialize Communication Channel, using WCF ----------------Req #10");
        }
        public static string makeEndpoint(string url, int port)
        {
            string endpoint = url + ":" + port.ToString() + "/Iservice";
            return endpoint;
        }
        public void ThreadProc()
        {
            while (true)
            {
                CommMessage msg = rcvR.GetMessage();
                Console.Write(msg.ToString());
                if (msg.MessageBody == "quit")
                {
                    break;
                }
            }
        }
    }
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class StreamService : IStreamService
    {
        string filename;
        string DLLStorage = "..\\..\\..\\Repository\\Storage\\";
        int BlockSize = 1024;
        byte[] block;
        HiResTimer timer = null;

        public StreamService()
        {
            block = new byte[BlockSize];
            timer = new HiResTimer();
        }
        public void upLoadFile(FileTransferMessage msg)
        {
            string StoreName="";
            int totalBytes = 0;
            timer.Start();          
            filename = msg.filename;
            StoreName = Path.Combine(DLLStorage, filename);
            if (!Directory.Exists(DLLStorage))
                Directory.CreateDirectory(DLLStorage);
            using (var outputStream = new FileStream(StoreName, FileMode.Create))
            {
                while (true)
                {
                    int byteread = msg.transferStream.Read(block, 0, BlockSize);
                    totalBytes += byteread;
                    if (byteread > 0)
                        outputStream.Write(block, 0, byteread);
                    else
                        break;
                }
            }
            timer.Stop();
            Console.Write("\n  Received file \"{0}\" of {1} bytes in {2} microsec.",
            filename, totalBytes, timer.ElapsedMicroseconds);
            if (System.IO.Path.GetExtension(filename) == ".txt")
            {
                Console.Write("Saved Test Results in Repository(~/Repository/Storage/) ------------------Req #7 Req #8");
            }
        }

        public Stream downLoadFile(string filename)
        {
            timer.Start();
            string SaveName = Path.Combine(DLLStorage, filename);
            FileStream outStream = null;
            if (File.Exists(SaveName))
            {
                outStream = new FileStream(SaveName, FileMode.Open);
                timer.Stop();
                Console.Write("\n  Sent \"{0}\" in {1} microsec.", filename, timer.ElapsedMicroseconds);
            }
            else
            {
                Console.Write("\n  {0} not found \""
                    ,filename);
            }          
            return outStream;
        }
        public List<string> GetAllDLLs()
        {
            List<string> Dlls = new List<string>();
            string[] files = Directory.GetFiles(DLLStorage,"*.dll");
            
            foreach (var file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);
                Dlls.Add(fileName);
            }
            return Dlls;
        }
        public static ServiceHost CreateStreamSeriveChannel(string url)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 500000000;
            Uri baseAddress = new Uri(url);
            Type service = typeof(StreamService);
            ServiceHost host = new ServiceHost(service, baseAddress);
            host.AddServiceEndpoint(typeof(IStreamService), binding, baseAddress);
            return host;
        }      
    }
    //----------------------<Test Stub>-------------------------
    class CommService
    {
        public Comm<CommService> cms = new Comm<CommService>();
        public static void Main(string[] args)
        {
            CommService sv = new CommService();
            CommMessage msg = new CommMessage();
            msg.FromUrl = "Local";
            msg.ToUrl = "";
            msg.MessageBody = "test";
            sv.cms.sndR.PostMessage(msg);
            CommMessage rcv=sv.cms.rcvR.GetMessage();
            Console.Write("\n  {0}", rcv.ToString());   
        }
    }
}


