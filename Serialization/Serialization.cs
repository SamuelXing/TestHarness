///////////////////////////////////////////////////////////////////////////////
// Serialization.cs - Serialization: Object->XML->String                     //
//                  - DeSerialization: XMLString-> Object                    //
//                  - Used to generate a message body                        //
//                                                                           //
// Jim Fawcett,Zihao Xing CSE681 - Software Modeling and Analysis, Fall 2016 //
///////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * - Serializa an object instance to XML string
 * - DeSerializa a XML string to an object
 * 
 * Maintanence History:
 * --------------------
 * ver 1.0 : 7 Nov 2016
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;

namespace TestHarness
{
    public static class ToAndFromXml
    {
        //----< serialize object to XML >--------------------------------

        static public string ToXml(this object obj)
        {
            // suppress namespace attribute in opening tag

            XmlSerializerNamespaces nmsp = new XmlSerializerNamespaces();
            nmsp.Add("", "");

            var sb = new StringBuilder();
            try
            {
                var serializer = new XmlSerializer(obj.GetType());
                using (StringWriter writer = new StringWriter(sb))
                {
                    serializer.Serialize(writer, obj, nmsp);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:");
                Console.Write("\n  {0}", ex.Message);
            }
            return sb.ToString();
        }
        //----< deserialize XML to object >------------------------------

        static public T FromXml<T>(this string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(new StringReader(xml));
            }
            catch (Exception ex)
            {
                Console.Write("\n  deserialization failed\n  {0}", ex.Message);
                return default(T);
            }
        }
    }
    public static class Utilities
    {
        public static void title(this string aString, char underline = '-')
        {
            Console.Write("\n  {0}", aString);
            Console.Write("\n {0}", new string(underline, aString.Length + 2));
        }
    }
    public static class extMethods
    {
        public static void show(this CommMessage msg, int shift = 2)
        {
            Console.Write("\n  formatted message:");
            string[] lines = msg.ToString().Split(',');
            foreach (string line in lines)
                Console.Write("\n    {0}", line.Trim());
            Console.WriteLine();
        }
        public static string shift(this string str, int n = 2)
        {
            string insertString = new string(' ', n);
            string[] lines = str.Split('\n');
            for (int i = 0; i < lines.Count(); ++i)
            {
                lines[i] = insertString + lines[i];
            }
            string temp = "";
            foreach (string line in lines)
                temp += line + "\n";
            return temp;
        }
        public static string formatXml(this string xml, int n = 2)
        {
            XDocument doc = XDocument.Parse(xml);
            return doc.ToString().shift(n);
        }
    }

    /// <summary>
    /// test stub: see MessageTemplate.cs
    /// </summary>
}
