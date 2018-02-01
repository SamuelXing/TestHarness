///////////////////////////////////////////////////////////////////////////
// program.cs  - This package acts as the entry for run.bat              //
//                                                                       //
// Zihao Xing CSE681 - Software Modeling and Analysis, Fall 2016         //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operation:
 * - used by run.bat as the entry for this solution
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Entry
{
    class Program
    {
        static void Main(string[] args)
        {
            Process.Start("TestHarnessGUI.exe");
            Process.Start("TestHarness.exe");
            Process.Start("Repository.exe");
        }
    }
}
