Requirement 14
------------PART 1: README --------------
To run this solution, here are the steps:
=========================================
- 1, run complie.bat
- 2£¬run run.bat as Administrator: right click run.bat->select "run as Administrator"

Note: 
1, all the Dlls at Client end which used for test are saved under "~/Client/DLLFiles"
2, all TestResults and DLLs at Repository end are saved under "~/Repository/Storage"
3, client can download testResult from repository and these files are save under "~/TestHarnessGUI/TestResults/"
*****************************
4, About sending test Request.
- Under the tab "Send Test Requests" in the GUI, for the Item "Test Libraries", 
   you can using ';' to separate multiple testcode.
   For example, "testcode1.dll;testcode2.dll"
- Using "Add Another" to add an test for an existed Test Request.
- Using "New" to New a Test Request.
- Using "Send" to Send TestRequest to TestHarness. 


------------PART 2: Interfaces-----------
- - - Itest.cs provides interfaces:

	- ITestHarness   used by TestHarness
		-  void sendTestRequest(CommMessage testRequest);
    	-  void sendMessage(CommMessage msg);

 	- ICallback      used by child AppDomain to send messages to TestHarness
 		-	void sendMessage(CommMessage msg);

 	- IRequestInfo   used by TestHarness
 		-	List<ITestInfo> requestInfo { get; set; }

 	- ITestInfo      used by TestHarness
 		-	string testName { get; set; }
    	-	List<string> files { get; set; }

 	- ILoadAndTest   used by TestHarness
 		-	ITestResults test(IRequestInfo requestInfo,string path);
    	-	void setCallback(ICallback cb);

 	- ITest          used by LoadAndTest
 		-	bool test();
    	-	string getLog();

 	- IRepository    used by Repository
 		-	bool getFiles(string path, string fileList);  // fileList is comma separated list of files
    	-	void sendLog(string log);
    	-	List<string> queryLogs(string queryText);

 	- IClient        used by Client
 		-	void sendResults(CommMessage result);
    	-	void makeQuery(CommMessage queryText);

    - ITestResult 	 used by TestHarness
    	-	string testName { get; set; }
    	-	string testResult { get; set; }
    	-	string testLog { get; set; }

    - ITestResults 	 used by TestHarness
    	-	string testKey { get; set; }
    	-	DateTime dateTime { get; set; }
    	-	List<ITestResult> testResults { get; set; }

- - - IService.cs provides interfaces:
	- IService       defines WCF Service Contract, used to pass message
		-	void PostMessage(CommMessage msg);
        -	CommMessage GetMessage();

    - IStreamSerice  defines WCF Service Contract, used to send file
    	void upLoadFile(FileTransferMessage msg);
        Stream downLoadFile(string filename);
        List<string> GetAllDLLs();

------------PART 3: StepsToConstruct-----------

Steps to Construct this Solution
================================

- Based on Professor Jim, Fawcett's Project2 Solution, I made some changes as below:
	- Add Serialization
	- Add IService ----This package defines WCF Service 
	- Add Communication channel for Client, TestHarness, Repository
	- Test Communication Channel
	- Add ClientGUI
	- Revise TestHarness
	- Revise Repository
	- Add Concurrency control for TestHarness
	- Test 

Communication is important between packages. For Testing we need to:
----------------------
- implement sending file and message between TH and Repo
- implement sending message between Client and TH
- implement sending message and file between Repo and Client

----------PART 4: Comparaision with Project #3----------

- Dose this project fully implement its concept?
  Basically, it has implemented its concept.

- Was the original concept practical?
  The original concept is still practical?  my original concepts in project #3 had mentioned the structure (Peer-to-Peer) I implemented now.

- Were there things you learned during the implementation 
  that made the original concept less relevant or incomplete?
  1, Version Control. I mention Version control in OCD3, but I found it is hard
  to implement without using MongoDB.
  2, File Message. I defined 9 kinds of Message Types at first,
  but actually I did not use that much.
  3, About the Logger. Since now, the communication among Client, TH and Repo has to be passed by WCF, 
  Logger become a little bit meaningless .














