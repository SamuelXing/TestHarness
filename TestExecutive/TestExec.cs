/////////////////////////////////////////////////////////////////////
// TestExec.cs - Demonstrate TestHarness, Client, and Repository   //
// ver 1.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * TestExec package orchestrates TestHarness, Client, and Repository
 * operations to show that all requirements for Project #2 have been
 * satisfied. 
 *
 * Required files:
 * ---------------
 * - TestExec.cs
 * - ITest.cs
 * - Client.cs, Repository.cs, TestHarness.cs
 * - LoadAndTest, Logger, Messages
 * 
 * Maintanence History:
 * --------------------
 * ver 1.0 : 16 Oct 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
  class TestExec
  {
    public TestHarness testHarness { get; set; }
    public Client client { get; set; }
    public Repository repository { get; set; }
    TestExec()
    {
      RLog.write("\n  creating Test Executive - Req #9");
      testHarness = new TestHarness(repository);
      client = new Client(testHarness as ITestHarness);
      repository = new Repository();
      testHarness.setClient(client);
      //client.setRepository(repository);
    }
    void sendTestRequest(Message testRequest)
    {
      //client.sendTestRequest(testRequest);
    }
    Message buildTestMessage()
    {
      Message msg = new Message();
      msg.to = "TH";
      msg.from = "CL";
      msg.author = "Fawcett";

      testElement te1 = new testElement("test1");
      te1.addDriver("testdriver.dll");
      te1.addCode("testedcode.dll");
      testElement te2 = new testElement("test2");
      te2.addDriver("td1.dll");
      te2.addCode("tc1.dll");
      testElement te3 = new testElement("test3");
      te3.addDriver("anothertestdriver.dll");
      te3.addCode("anothertestedcode.dll");
      testElement tlg = new testElement("loggerTest");
      tlg.addDriver("logger.dll");
      testRequest tr = new testRequest();
      tr.author = "Jim Fawcett";
      tr.tests.Add(te1);
      tr.tests.Add(te2);
      tr.tests.Add(te3);
      tr.tests.Add(tlg);
      msg.body = tr.ToString();
      return msg;
    }
    static void Main(string[] args)
    {
      RLog.attach(RLog.makeConsoleStream());
      RLog.start();
      DLog.attach(DLog.makeConsoleStream());
      DLog.start();
      RLog.write("\n  Demonstrating TestHarness - Project #2");
      RLog.write("\n ========================================");
      TestExec te = new TestExec();
      Message msg = te.buildTestMessage();
      te.sendTestRequest(msg);
      te.sendTestRequest(msg);
      te.testHarness.processMessages();
      //te.client.makeQuery("test1");
      DLog.flush();
      RLog.flush();
      Console.Write("\n  press key to exit");
      Console.ReadKey();
      DLog.stop();
      RLog.stop();
    }
  }
}
