/////////////////////////////////////////////////////////////////////
// LoadAndTest.cs - loads and executes tests using reflection      //
// ver 1.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * LoadAndTest package operates in child AppDomain.  It loads and
 * executes test code defined by a TestRequest message.
 *
 * Required files:
 * ---------------
 * - LoadAndTest.cs
 * - ITest.cs
 * - Logger, Messages
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
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

namespace TestHarness
{
  public class LoadAndTest : MarshalByRefObject, ILoadAndTest
  {
    private ICallback cb_ = null;

    ///////////////////////////////////////////////////////
    // Data Structures used to store test information
    //
    [Serializable]
    public class TestResult : ITestResult
    {
      public string testName { get; set; }
      public string testResult { get; set; }
      public string testLog { get; set; }
    }
    [Serializable]
    public class TestResults : ITestResults
    {
      public string testKey { get; set; }
      public DateTime dateTime { get; set; }
      public List<ITestResult> testResults { get; set; } = new List<ITestResult>();
    }
    TestResults testResults_ = new TestResults();

    //----< initialize loggers >-------------------------------------

    public LoadAndTest()
    {
      // need to attach and start because
      // DLog in child AppDomain doesn't share the same static logger
      // as DLog in the parent AppDomain

      RLog.attach(DLog.makeConsoleStream());
      RLog.start();
      DLog.attach(DLog.makeConsoleStream());
      DLog.start();
      DLog.write("\n  creating instance of LoadAndTest");
    }
    //----< load libraries into child AppDomain and test >-----------
    /*
     * ToDo:
     * - refactor this function to make it more testable.
     */
    public ITestResults test(IRequestInfo testRequest,string localDir)
    {
      TestResults testResults = new TestResults();
      foreach(ITestInfo test in testRequest.requestInfo)
      {
        TestResult testResult = new TestResult();
        testResult.testName = test.testName;
        try
        {
          DLog.write("\n  -- \"" + test.testName + "\" --");

          ITest tdr = null;
          string testDriverName = "";

          foreach (string file in test.files)
          {
            Assembly assem = null;
            try
            {
              assem = Assembly.LoadFrom(localDir+"/"+file);
            }
            catch
            {
              testResult.testResult = "failed";
              testResult.testLog = "file not loaded";
              DLog.write("\n    could not load \"" + file + "\"");
              continue;
            }
            DLog.write("\n    loaded: \"" + file + "\"");
            Type[] types = assem.GetExportedTypes();

            foreach (Type t in types)
            {
              if (t.IsClass && typeof(ITest).IsAssignableFrom(t))  // does this type derive from ITest ?
              {
                tdr = (ITest)Activator.CreateInstance(t);    // create instance of test driver
                testDriverName = file;
                RLog.write("\n    " + testDriverName + " implements ITest interface ---------------------- Req #5");
              }
            }
          }
          DLog.write("\n    testing " + testDriverName);
          if (tdr != null && tdr.test() == true)
          {
            testResult.testResult = "passed";
            testResult.testLog = tdr.getLog();
            DLog.write("\n    test passed");
            DLog.flush();
            if (cb_ != null)
            {
              //cb_.sendMessage(new Message(testDriverName + " passed"));
            }
          }
          else
          {
            testResult.testResult = "failed";
            if (tdr != null)
              testResult.testLog = tdr.getLog();
            else
              testResult.testLog = "file not loaded";
            DLog.write("\n    test failed");
            DLog.flush();
            if (cb_ != null)
            {
              //cb_.sendMessage(new Message(testDriverName + " failed"));
            }
          }
        }
        catch(Exception ex)
        {
          testResult.testResult = "failed";
          testResult.testLog = "exception thrown";
          RLog.write("\n  " + ex.Message);
        }
        testResults_.testResults.Add(testResult);
      }

      testResults_.dateTime = DateTime.Now;
      return testResults_;
    }
    //----< TestHarness calls to pass ref to Callback function >-----
     
    public void setCallback(ICallback cb)
    {
      cb_ = cb;
    }

#if (TEST_LOADANDTEST)
    static void Main(string[] args)
    {
      /*
       * ToDo: add code to test
       * - Use Callback to write to log instead of using log here.  That would be
       *   an improvement since the static logger in StaticLogger<LogType> is not
       *   shared between the child and parent AppDomains so logs get out of synch.
       * - Used TestHarness for testing, but the plan is to create a test class
       *   that derives from ITest and run tests either from this project or in 
       *   TestHarness. 
       */
    }
#endif
  }
}
