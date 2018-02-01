using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.IO;


namespace TestHarness
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract(IsOneWay = true)]
        void PostMessage(CommMessage msg);
        CommMessage GetMessage();
    }

    [DataContract]
    public class CommMessage
    {
        [DataMember]
        public string ToUrl { get; set; }
        [DataMember]
        public string FromUrl { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public DateTime TimeStamp { get; set; }

        public enum MessageType
        {
            [EnumMember]
            TestRequest, //Client to TestHarness
            [EnumMember]
            TestResult,  //TestHarness to Client
            [EnumMember]
            TestResultsQuery, //Client to Repository
            [EnumMember]
            TestResultReply,  //Repository to Client
            [EnumMember]
            Logsquery,        //Client to Repository
            [EnumMember]
            LogsReply,        //Repository to Client
            [EnumMember]
            FilesQuery,       //Client to Repository
            [EnumMember]      
            FilesReply,       //Repository to Client
            [EnumMember]
            FilesRequest      // TestHarness to Repository(push model only)
        }
        [DataMember]
        public MessageType messageType{ get; set; }
        [DataMember]
        public string MessageBody { get; set; }
        [OperationContract]
        private CommMessage fromString(string msgStr)
        {
            CommMessage msg = new CommMessage();
            try
            {
                string[] parts = msgStr.Split(',');
                for (int i = 0; i < parts.Count(); ++i)
                {
                    parts[i] = parts[i].Trim();
                }
                msg.ToUrl = parts[0].Substring(4);
                msg.FromUrl = parts[1].Substring(6);
                msg.Author = parts[2].Substring(8);
                DateTime dt=Convert.ToDateTime(parts[3].Substring(6));
                msg.TimeStamp = dt;
                msg.messageType 
                    = (CommMessage.MessageType)Enum.Parse(typeof(MessageType), parts[4].Substring(6));
                if (parts[5].Count() > 6)
                    msg.MessageBody = parts[5].Substring(6);
            }
            catch
            {
                Console.Write("\n   string parsing failed");
                return null;
            }
            return msg;
        }
        [OperationContract]
        public override string ToString()
        {
            string temp = "to: " + ToUrl;
            temp += ", from: " + FromUrl;
            temp += ", author: " + Author;
            temp += ", time: " + TimeStamp.ToString();
            temp += ", type: " + messageType.ToString();
            temp += ", body: " + MessageBody;
            return temp;
        }
    }
    [ServiceContract]
    public interface IStreamService
    {
        [OperationContract(IsOneWay = true)]
        void upLoadFile(FileTransferMessage msg);
        [OperationContract]
        Stream downLoadFile(string filename);
        [OperationContract]
        List<string> GetAllDLLs();
    }

    [MessageContract]
    public class FileTransferMessage
    {
        [MessageHeader(MustUnderstand = true)]
        public string filename { get; set; }
        [MessageBodyMember(Order = 1)]
        public Stream transferStream { get; set; }
    }
}
