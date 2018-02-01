using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MessageTemplate
{
    public class testElement
    {
        public string testName { get; set; }
        public string testDriver { get; set; }
        public List<string> testCodes { get; set; } = new List<string>();
        public testElement() { }
        public testElement(string name)
        {
            testName = name;
        }
        public void addDriver(string DriverName)
        {
            testDriver = DriverName;
        }
        public void addCode(string CodeName)
        {
            testCodes.Add(CodeName);
        }
        public override string ToString()
        {
            string temp = "<test name=\"" + testName + "\">";
            temp += "<testDriver>" + testDriver + "</testDriver>";
            foreach (string code in testCodes)
                temp += "<library>" + code + "</library>";
            temp += "</test>";
            return temp;
        }

    }
    public class testRequest
    {
        public string author { get; set; }
        public List<testElement> tests { get; set; } = new List<testElement>();
        public override string ToString()
        {
            string temp = "<Author>" + author+"/<Author>";
            foreach (testElement te in tests)
            {
                temp += te.ToString();
            }
            return temp;
        }
    }
    public class testResult
    {
        public string testName { get; set; }
        public bool passed { get; set; }
        public string log { get; set; }
        public testResult() { }
        public testResult(string name,bool status)
        {
            testName = name;
            passed = status;
        }
        public void addLog(string logItem)
        {
            log += logItem;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TestName>"+testName+"/<TestName>");
            sb.Append("<TestResult>"+passed+"/<TestResult>");
            sb.Append("<log>"+log+"/<log>");
            return sb.ToString();
        }
    }
    class MessageTemplate
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Messaage Model");

            testElement t1 = new testElement();
            t1.testName = "test1";
            t1.addCode("tc1.dll");
            t1.addCode("tc2.dll");
            t1.addDriver("td1.dll");

            Console.WriteLine("TestElement1");
            Console.WriteLine(t1.ToString());

            testElement t2 = new testElement("test2");
            t2.addCode("tc3.dll");
            t2.addCode("tc4.dll");
            t2.addDriver("td2.dll");
            Console.WriteLine("TestElement2");
            Console.WriteLine(t2.ToString());

            testRequest tr1 = new testRequest();
            tr1.author = "SamuelXing";
            tr1.tests.Add(t1);
            tr1.tests.Add(t2);
            Console.WriteLine("TestRequest1");
            Console.WriteLine(tr1.ToString());
            testResult TR = new testResult();
            
        }
    }
}
