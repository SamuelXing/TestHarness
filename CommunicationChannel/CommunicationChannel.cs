////////////////////////////////////////////////////////////////////////////////
// CommunicationChannel.cs - Demonstrate use of channel with a single process //
// Ver 1.0                                                                    //
// Jim Fawcett,ZihaoXing CSE681 - Software Modeling and Analysis, Fall 2016   //
////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The ChannelDemo package defines one class, ChannelDemo, that uses 
 * the Comm<Client> and Comm<Server> classes to pass messages to one 
 * another.
 * 
 * Required Files:
 * ---------------
 * - ChannelDemo.cs
 * - ICommunicator.cs, CommServices.cs
 * - Messages.cs, MessageTest, Serialization
 *
 * Maintenance History:
 * --------------------
 * Ver 1.0 : 10 Nov 2016
 * - first release 
 *  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    class MessageChannel<T>
    {
        public Comm<T> comm { get; set; } = new Comm<T>();
        static void Main(string[] args)
        {

        }
    }
}
