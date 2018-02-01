////////////////////////////////////////////////////////////////////////////////
// TestHarness.cs - TestHarness Engine: creates child domains                 //
// ver 1.0                                                                    //
// Jim Fawcett, Zihao Xing CSE681 - Software Modeling and Analysis, Fall 2016 //
////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * TestHarness package provides integration testing services.  It:
 * - receives structured test requests
 * - retrieves cited files from a repository
 * - executes tests on all code that implements an ITest interface,
 *   e.g., test drivers.
 * - reports pass or fail status for each test in a test request
 * - stores test logs in the repository
 * It contains classes:
 * - TestHarness that runs all tests in child AppDomains
 * - Callback to support sending messages from a child AppDomain to
 *   the TestHarness primary AppDomain.
 * - Test and RequestInfo to support transferring test information
 *   from TestHarness to child AppDomain
 * 
 * Required Files:
 * ---------------
 * - TestHarness.cs, BlockingQueue.cs
 * - ITest.cs
 * - LoadAndTest, Logger, Messages
 *
 * Maintanence History:
 * --------------------
 * ver 1.0 : 16 Oct 2016
 * - first release
 * ver 1.1 : 10 Nov 2016
 * - add Communication Channel
 * ver 1.2 : 13 Nov 2016
 * - revise some functions to adapt to remote version
 * - List<ITestInfo> extractTests(Message testRequest)
 * ->List<ITestInfo> extractTests(CommMessage testRequest)
 * ver 1.3 : 15 Nov 2016
 * - add Concurrency Control
 * ver 2.0 : 16 Nov 2016
 * - Second release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Policy;    // defines evidence needed for AppDomain construction
using System.Runtime.Remoting;   // provides remote communication between AppDomains
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.IO;

namespace TestHarness
{
    ///////////////////////////////////////////////////////////////////
    // Callback class is used to receive messages from child AppDomain
    //
    public class Callback : MarshalByRefObject, ICallback
    {
        public void sendMessage(CommMessage message)
        {
            RLog.flush();
            DLog.write("\n  received msg from childDomain: \"" + message.MessageBody + "\"");
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Test and RequestInfo are used to pass test request information
    // to child AppDomain
    //
    [Serializable]
    class Test : ITestInfo
    {
        public string testName { get; set; }
        public List<string> files { get; set; } = new List<string>();
    }
    [Serializable]
    class RequestInfo : IRequestInfo
    {
        public List<ITestInfo> requestInfo { get; set; } = new List<ITestInfo>();
    }


    ///////////////////////////////////////////////////////////////////
    // class TestHarness

    public class TestHarness : ITestHarness
    {
        //public SWTools.BlockingQueue<Message> inQ_ { get; set; } = new SWTools.BlockingQueue<Message>();
        private ICallback cb_;
        //private IRepository repo_;
        //private IClient client_;
        private string localDir_;
        //private string repoPath_ = "../../../Repository/DLLStorage/";
        private string RepositoryAddress = "http://localhost:8000/StreamService";
        int BlockSize=1024;
        byte[] block;
        object sync_=new object();
        public IStreamService channel;
        public Comm<TestHarness> Comm { get; set; } = new Comm<TestHarness>();
        public string TestHarnessAddress { get; } = Comm<TestHarness>.makeEndpoint("http://localhost", 7000);
        //Thread rcvThread;
        List<Thread> threads_ = new List<Thread>();
        public TestHarness()
        {
            DLog.write("\n  creating instance of TestHarness");
            block = new byte[BlockSize];
            DLog.write("\n Creating instance of TestHarness Server");
            Comm.rcvR.CreateRecvChannel(TestHarnessAddress);
            channel = CreateServiceChannel(RepositoryAddress);
            Console.Write("\n\n  TestHarness Main Thread ID: {0}  \n", Thread.CurrentThread.ManagedThreadId);
            int numThreads = 8;
            for (int i = 0; i < numThreads; i++)
            {
                Thread rcvThread = Comm.rcvR.start(rcvThreadProc);
                //rcvThread.IsBackground = true;
                threads_.Add(rcvThread);
                rcvThread.Start();
            }
            cb_ = new Callback();
        }

        void rcvThreadProc()
        {
            while (true)
            {
                CommMessage msg = Comm.rcvR.GetMessage();
                Console.Write("\n\n  Current Thread ID: {0}  ---------------------- Req #4 \n", Thread.CurrentThread.ManagedThreadId);
                Console.Write("\n " + msg.ToString());
                if (msg.MessageBody == "quit") { break; }
                //msg.TimeStamp = DateTime.Now;
                Console.Write("\n {0} received message:", Comm.name);
                this.processMessages(msg);
            }
        }

        public void wait()
        {
            //rcvThread.Join();
            foreach (Thread t in threads_)
            {
                //CommMessage msg = new CommMessage();
                //msg.ToUrl = this.TestHarnessAddress;
                //msg.FromUrl = "TH";
                //msg.MessageBody = "quit";
                //this.Comm.sndR.PostMessage(msg);
                //Console.Write("\n  Joining Thread {0}", t.ManagedThreadId);
                t.Abort();
            }
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
                using (var inputStream = new FileStream(FilenameWithPath, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = System.IO.Path.GetFileName(FilenameWithPath); ;
                    msg.transferStream = inputStream;
                    channel.upLoadFile(msg);
                }
            }
            catch
            {
                Console.WriteLine("\n  can't find \"{0}\"", FilenameWithPath);
            }
        }

        //the path here is used to save download files
        void downLoadFile(string path, string filename)
        {
            int totalBytes = 0;
            try
            {
                Stream strm = channel.downLoadFile(filename);
                string rfilename = Path.Combine(path, filename);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                using (var outputStream = new FileStream(rfilename, FileMode.Create, FileAccess.ReadWrite))
                {
                    while (true)
                    {
                        int byteRead = strm.Read(block,0,BlockSize);
                        totalBytes += byteRead;
                        if (byteRead > 0)
                            outputStream.Write(block,0,byteRead);
                        else
                            break;
                    }
                }
            }
            catch
            {
                DLog.write("\n  Cannot laod file from repository");

            }
        }


        //----< called by clients >--------------------------------------

        public void sendTestRequest(CommMessage testRequest)
        {
            RLog.write("\n  TestHarness received a testRequest ");
        }
        //----< not used for Project #2 >--------------------------------

        public void sendMessage(CommMessage msg)
        {
            this.Comm.sndR.PostMessage(msg);
        }
        //----< make path name from author and time >--------------------

        string makeKey(string author)
        {
            DateTime now = DateTime.Now;
            string nowDateStr = now.Date.ToString("d");
            string[] dateParts = nowDateStr.Split('/');
            string key = "";
            foreach (string part in dateParts)
                key += part.Trim() + '_';
            string nowTimeStr = now.TimeOfDay.ToString();
            string[] timeParts = nowTimeStr.Split(':');
            for (int i = 0; i < timeParts.Count() - 1; ++i)
                key += timeParts[i].Trim() + '_';
            key += timeParts[timeParts.Count() - 1];
            key = author + "_" + key+"_"+"ThreadID"+ Thread.CurrentThread.ManagedThreadId;
            return key;
        }
       
        List<ITestInfo> extractTests(CommMessage msg)
        {
            DLog.write("\n  Deserialize Test Request");
            List<ITestInfo> tInfo = new List<ITestInfo>();
            testRequest NewtstR = msg.MessageBody.FromXml<testRequest>();
            foreach (var test in NewtstR.tests)
            {
                Test T_Test = new Test();
                T_Test.testName = test.testName;
                T_Test.files.Add(test.testDriver);
                foreach (var testcode in test.testCodes)
                {
                    T_Test.files.Add(testcode);
                }
                tInfo.Add(T_Test);
            }
            return tInfo;
        }

        //----< retrieve test code from testRequest >--------------------
        List<string> extractCode(List<ITestInfo> testInfos)
        {
            DLog.write("\n  retrieving code files from testInfo data structure");
            List<string> codes = new List<string>();
            foreach (ITestInfo testInfo in testInfos)
                codes.AddRange(testInfo.files);
            return codes;
        }
        //----< create local directory and load from Repository >--------

        //RequestInfo processRequestAndLoadFiles(CommMessage testRequest)
        //{
        //}

        public CommMessage makeFileRequestAndBuildMessage(CommMessage msg)
        {
            CommMessage fileRequest = new CommMessage();
            //makeFilerequest
            fileRequest fR = new fileRequest();
            fR.key = msg.Author;
            List<string> files = extractCode(extractTests(msg));
            fR.addfiles(files);
            //build filerequest message
            fileRequest.FromUrl = TestHarnessAddress;
            fileRequest.ToUrl = RepositoryAddress;
            fileRequest.Author = "TestHarness";
            fileRequest.TimeStamp = DateTime.Now;
            fileRequest.messageType = CommMessage.MessageType.FilesRequest;
            fileRequest.MessageBody = fR.ToXml();
            return fileRequest;
        }
        RequestInfo processRequestAndLoadFiles(CommMessage testRequest)
        {
            RequestInfo rqi = new RequestInfo();
            rqi.requestInfo = extractTests(testRequest);
            List<string> files = extractCode(rqi.requestInfo);        
            localDir_ = makeKey(testRequest.Author);
            DLog.write("\n  creating local test directory \"" + localDir_ + "\"");
            //System.IO.Directory.CreateDirectory(localDir_);
            DLog.write("\n downloading DLLs from Repository");
            foreach (string file in files)
            {
                this.downLoadFile(localDir_, file);
            }
            DLog.write("Successed! ");
            return rqi;
        }
        //----< save results and logs in Repository >--------------------

        bool saveResultsAndLogs(ITestResults testResults)
        {
            string logName = testResults.testKey + ".txt";
            System.IO.StreamWriter sr = null;
            try
            {
                sr = new System.IO.StreamWriter(System.IO.Path.Combine(localDir_, logName));
                sr.WriteLine(logName);
                foreach (ITestResult test in testResults.testResults)
                {
                    sr.WriteLine("-----------------------------");
                    sr.WriteLine(test.testName);
                    sr.WriteLine(test.testResult);
                    sr.WriteLine(test.testLog);
                }
                sr.WriteLine("-----------------------------");
            }
            catch(Exception ex)
            {
                sr.Close();
                Console.WriteLine(ex.Message);
                return false;
            }
            sr.Close();

            this.upLoadFile(localDir_+'/'+ logName);
     ///////////////////////////////////////////////////////////////////       
            try
            {
                //System.IO.Directory.Delete(localDir_, true);
                DeleteFolder(localDir_);
            }
            catch (Exception ex)
            {
                DLog.write("\n  could not remove directory");
                DLog.write("\n  " + ex.Message);
            }
            return true;
        }
        //----<Delete Folder>---------
        // 
        static void DeleteFolder(string path)
        {
            foreach (string f in Directory.GetFileSystemEntries(path))
            {
                if (File.Exists(f))
                {
                    FileInfo fi = new FileInfo(f);
                    if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    {
                        fi.Attributes = FileAttributes.Normal;
                    }
                    fi.Attributes = FileAttributes.Normal;
                    File.Delete(f);
                }
                else
                {
                    DeleteFolder(f);
                }
            }
            Directory.Delete(path);
        }
        //----< run tests >----------------------------------------------
        /*
         * In Project #4 this function becomes the thread proc for
         * each child AppDomain thread.
         */
        ITestResults runTests(CommMessage testRequest)
        {
            RequestInfo rqi = null;
            AppDomain ad = null;
            ILoadAndTest ldandtst=null;
            ITestResults tr = null;
            try
            {
                lock (sync_)
                {
                    rqi = processRequestAndLoadFiles(testRequest);
                    ad = createChildAppDomain();
                    ldandtst = installLoader(ad);
                }
                if (ldandtst != null)
                {
                    tr = ldandtst.test(rqi, localDir_);
                    tr.testKey = localDir_;

                    DLog.flush();
                    //DLog.pause(true);
                    RLog.putLine();
                    RLog.write("\n  test results are:");
                    RLog.write("\n  - test Identifier: " + tr.testKey);
                    RLog.write("\n  - test DateTime:   " + tr.dateTime);
                    foreach (ITestResult test in tr.testResults)
                    {
                        RLog.write("\n  --------------------------------------");
                        RLog.write("\n    test name:   " + test.testName);
                        RLog.write("\n    test result: " + test.testResult);
                        RLog.write("\n    test log:    " + test.testLog);
                    }
                    RLog.write("\n  --------------------------------------");
                    RLog.putLine();
                    RLog.flush();
                    //DLog.pause(false);
  
                    if (saveResultsAndLogs(tr))
                    {
                        RLog.write("\n  saved test results and logs in Repository \n");
                        lock (sync_)
                        {
                            // unloading ChildDomain, and so unloading the library
                            Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": unloading: \"" + ad.FriendlyName + "\"\n   -------------req #7");
                            AppDomain.Unload(ad);
                            try
                            {
                                DeleteFolder(localDir_);
                                Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": removed directory " + localDir_);
                            }
                            catch (Exception ex)
                            {
                                Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": could not remove directory " + localDir_);
                                Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": " + ex.Message);
                            }
                        }
                        DLog.stop();
                        return tr;
                    }
                    else
                    {
                        RLog.write("\n  failed to save test results and logs in Repository\n");
                    }
                    DLog.putLine();
                    DLog.write("\n  removing test directory \"" + localDir_ + "\"");
                }

            }
            catch
            {
                Console.Write("\n file not exists");
                return tr;
            }
            return tr;

        }
        //----< make TestResults Message >-------------------------------

        CommMessage makeTestResultsMessageAndCreatSendChannel(ITestResults tr, CommMessage msg)
        {
            CommMessage trMsg = new CommMessage();
            testResults trs = new testResults();
            //build testResults to serialize
            trs.testkey = tr.testKey;
            trs.dateTime = tr.dateTime;
            foreach (ITestResult test in tr.testResults)
            {
                testResult trr = new testResult();
                trr.testName = test.testName;
                trr.Result = test.testResult;
                trr.testLog = test.testLog;
                trs.Results.Add(trr);
            }
            //build testResultMessage
            trMsg.Author = "TestHarness";
            trMsg.ToUrl = msg.FromUrl;
            trMsg.FromUrl = TestHarnessAddress;
            trMsg.TimeStamp = DateTime.Now;
            trMsg.messageType = CommMessage.MessageType.TestResult;
            trMsg.MessageBody = trs.ToXml();
            //creat a send channel
            this.Comm.sndR.CreateMessageSndChannel(trMsg.ToUrl);
            return trMsg;
        }
        //----< main activity of TestHarness >---------------------------

        public void processMessages(CommMessage testRequest)
        {
            Console.Write("\n\n Run test request on thread: {0}  \n", Thread.CurrentThread.ManagedThreadId); 
            AppDomain main = AppDomain.CurrentDomain;
            DLog.write("\n  Starting in AppDomain " + main.FriendlyName + "\n");
            ITestResults tr = runTests(testRequest);
            this.sendMessage(makeTestResultsMessageAndCreatSendChannel(tr,testRequest));
        }
        //----< was used for debugging >---------------------------------

        void showAssemblies(AppDomain ad)
        {
            Assembly[] arrayOfAssems = ad.GetAssemblies();
            foreach (Assembly assem in arrayOfAssems)
                DLog.write("\n  " + assem.ToString());
        }
        //----< create child AppDomain >---------------------------------

        public AppDomain createChildAppDomain()
        {
            try
            {
                DLog.flush();
                RLog.write("\n  creating child AppDomain ");

                AppDomainSetup domaininfo = new AppDomainSetup();
                domaininfo.ApplicationBase
                  = "file:///" + System.Environment.CurrentDirectory;  // defines search path for assemblies

                //Create evidence for the new AppDomain from evidence of current

                Evidence adevidence = AppDomain.CurrentDomain.Evidence;

                // Create Child AppDomain

                AppDomain ad
                  = AppDomain.CreateDomain("ChildDomain", adevidence, domaininfo);

                DLog.write("\n  created AppDomain \"" + ad.FriendlyName + "\"");
                return ad;
            }
            catch (Exception except)
            {
                RLog.write("\n  " + except.Message + "\n\n");
            }
            return null;
        }
        //----< Load and Test is responsible for testing >---------------

        ILoadAndTest installLoader(AppDomain ad)
        {
            ad.Load("LoadAndTest");
            //showAssemblies(ad);
            //Console.WriteLine();

            // create proxy for LoadAndTest object in child AppDomain

            ObjectHandle oh
              = ad.CreateInstance("LoadAndTest", "TestHarness.LoadAndTest");
            object ob = oh.Unwrap();    // unwrap creates proxy to ChildDomain
                                        // Console.Write("\n  {0}", ob);

            // set reference to LoadAndTest object in child

            ILoadAndTest landt = (ILoadAndTest)ob;

            // create Callback object in parent domain and pass reference
            // to LoadAndTest object in child

            landt.setCallback(cb_);
            return landt;
        }
        
#if (TEST_TESTHARNESS)
        static void Main(string[] args)
        {
            //Thread.Sleep(2000);
            //Console.WriteLine("TestHarness Server start up");
            //TestHarness th = new TestHarness();

            //CommMessage cmg = new CommMessage();
            //cmg.ToUrl = "http://localhost:5001/IService";
            //cmg.FromUrl = "http://localhost:5000/IService";
            //cmg.Author = "SamuelXing";
            //cmg.TimeStamp = DateTime.Now;
            //cmg.messageType = CommMessage.MessageType.TestRequest;
            //cmg.MessageBody = "Hello, World!(Sent Back from TestHarness)";

            //try
            //{
            //    th.SetupMsgChannel("http://localhost:5001/IService");
            //    ThreadStart st = new ThreadStart(th.ThreadProc);
            //    Thread trd = new Thread(st);
            //    trd.IsBackground = true;
            //    trd.Start();
            //    //trd.Join();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}
            Console.Write("\n Demo: TestHarness Starts working");
            Console.Write("\n +==============================+");

            TestHarness th = new TestHarness();
            //////////////////////////////////
            //Uncomment lines below to send Message to client
            //
            //CommMessage msg = th.makeMessage("TestHarness", th.TestHarnessAddress, Comm<TestHarness>.makeEndpoint("http://localhost",5001));
            //msg.MessageBody = "hello-send from testharness Server";
            //th.Comm.sndR.PostMessage(msg);

            Console.Write("\n press key to exit: ");
            Console.ReadKey();
            CommMessage msg = new CommMessage();
            msg.ToUrl = th.TestHarnessAddress;
            msg.FromUrl = "TH";
            msg.MessageBody = "quit";
            th.Comm.sndR.PostMessage(msg);
            th.wait();
            Console.Write("\n\n");
        }
#endif
    }
}
