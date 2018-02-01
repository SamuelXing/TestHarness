
///////////////////////////////////////////////////////////////////////////
// Repository.cs - holds test code for TestHarness                       //
//                                                                       //
// Jim Fawcett,Zixing CSE681 - Software Modeling and Analysis, Fall 2016 //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package acts as file Server for client and testharness.
 * - Client can upload files from repository.
 * - Client can browse files exsited in repository.
 * - Test Harness can download files in repository.
 * 
 * Required Files:
 * - ITest.cs, Logger.cs, CommService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Oct 2016
 * - first release
 * ver 1.1 : 9 Nov 2016
 * - implement recieving files from client
 * ver 1.2 : 10 Nov 2016
 * - implement send files to TestHarness
 * ver 2.0: 15 Nov 2016
 * - implement query 
 * - second release
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
    public class Repository : IRepository
    {
        string repoStoragePath = "..\\Repository\\Storage\\";
        ServiceHost host1 = null;
        public Comm<Repository> Comm { get; set; } = new Comm<Repository>();
        public Thread rcvThread = null;
        public string RepositoryEndPoint { get; } = Comm<Repository>.makeEndpoint("http://localhost",8005);
        Object sync_ = new object();
        public Repository()
        {
            DLog.write("\n  Creating instance of Repository");
            Console.Write("\n  Creating Stream Service Channel: http://localhost:8000/StreamService");
            host1 = StreamService.CreateStreamSeriveChannel("http://localhost:8000/StreamService");
            Console.Write("\n  Creaing Service Channel {0}", RepositoryEndPoint);
            Comm.rcvR.CreateRecvChannel(RepositoryEndPoint);
            rcvThread = Comm.rcvR.start(rcvThreadProc);
            rcvThread.Start();
        }

        void rcvThreadProc()
        {
            while (true)
            {
                CommMessage msg = Comm.rcvR.GetMessage();
                Console.Write("\n  {0} recieved message:", Comm.name);
                Console.Write("\n  " + msg.ToString());
                if (msg.MessageBody == "quit") { break; }
                testResultsQuery trq = msg.MessageBody.FromXml<testResultsQuery>();
                List<string> trr = this.queryLogs(trq.queryText);
                testResultsReply trR = new testResultsReply();
                trR.addfiles(trr);
                CommMessage reply = buildReplyMessage(msg);
                reply.MessageBody = trR.ToXml();
                this.Comm.sndR.CreateMessageSndChannel(reply.ToUrl);
                this.Comm.sndR.PostMessage(reply);        
            }
        }
        public void wait()
        {
            rcvThread.Join();
        }
        CommMessage buildReplyMessage(CommMessage request)
        {
            CommMessage msg = new CommMessage();
            msg.FromUrl = RepositoryEndPoint;
            msg.ToUrl = request.FromUrl;
            msg.Author = "Repository";
            msg.TimeStamp = DateTime.Now;
            msg.messageType = CommMessage.MessageType.TestResultReply;
            return msg;
        }
        public void OpenService()
        {
            host1.Open();
            //host2.Open();
        }
        public void CloseSerive()
        {
            host1.Close();
            //host2.Close();
        }
        //----< search for text in log files >---------------------------
        /*
         * This function should return a message.  I'll do that when I
         * get a chance.
         */
        public List<string> queryLogs(string queryText)
        {
            List<string> queryResults = new List<string>();
            string path = System.IO.Path.GetFullPath("../../"+repoStoragePath);
            string[] files = System.IO.Directory.GetFiles("../Repository/Storage", "*.txt");
            foreach (string file in files)
            {
                string contents;
                lock (sync_)
                {
                   contents = File.ReadAllText(file);
                }
                if (contents.Contains(queryText))
                {
                    string name = System.IO.Path.GetFileName(file);
                    queryResults.Add(name);
                }
            }
            return queryResults;
        }

        //----< send files with names on fileList >----------------------
        /*
         * This function is not currently being used.  It may, with a
         * Message interface, become part of Project #4.
         */
        public bool getFiles(string path, string fileList)
        {
            string[] files = fileList.Split(new char[] { ',' });
            //string repoStoragePath = "..\\..\\RepositoryStorage\\";

            foreach (string file in files)
            {
                string fqSrcFile = repoStoragePath + file;
                string fqDstFile = "";
                try
                {
                    fqDstFile = path + "\\" + file;
                    File.Copy(fqSrcFile, fqDstFile);
                }
                catch
                {
                    RLog.write("\n  could not copy \"" + fqSrcFile + "\" to \"" + fqDstFile);
                    return false;
                }
            }
            return true;
        }
        //----< intended for Project #4 >--------------------------------

        public void sendLog(string Log)
        {

        }
#if (TEST_REPOSITORY)
        static void Main(string[] args)
        {
            /*
             * ToDo: add code to test 
             * - Test code in Repository class that sends files to TestHarness.
             * - Modify TestHarness code that now copies files from RepositoryStorage folder
             *   to call Repository.getFiles.
             * - Add code to respond to client queries on files and logs.
             * - Add RepositoryTest class that implements ITest so Repo
             *   functionality can be tested in TestHarness.
             */
            Repository repo = new Repository();
            repo.OpenService();
            Console.Write("\n  SelfHosted File Stream Service started");
            Console.Write("\n ========================================\n");
            Console.Write("\n  Press key to terminate service:\n");
            Console.ReadKey();
            CommMessage msg = new CommMessage();
            msg.ToUrl = repo.RepositoryEndPoint;
            msg.MessageBody = "quit";
            repo.Comm.sndR.PostMessage(msg);
            repo.wait();
            Console.Write("\n");
            repo.CloseSerive();
        }
#endif
    }
}
