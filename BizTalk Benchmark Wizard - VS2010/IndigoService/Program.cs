using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace IndigoService
{
    internal class Program
    {
        // Fields
        public static bool Logging = false;
        public static string pathLogMsgsReceived = (Environment.CurrentDirectory + @"\IndigoService.MsgsReceived.log");
        public static string PathRespMsgFile = null;

        // Methods
        private void Functionality_Statistics(string[] args)
        {
            bool flag = false;
            string str = null;
            long num = 0L;
            ArrayList list = new ArrayList();
            str = args[0];
            if (str.ToLower() != "statistics")
            {
                flag = true;
                Console.WriteLine(string.Format("{0}{1}{2}{3}{4}", new object[] { "The value \"", str, "\" of the \"statistics\" parameter ", " is not valid.", " Valid values are: statistics." }));
            }
            string str3 = args[1];
            try
            {
                num = Convert.ToInt64(str3);
            }
            catch (FormatException exception)
            {
                flag = true;
                string str2 = exception.ToString();
                Console.WriteLine(string.Format("{0}{1}{2}{3}", new object[] { "The value \"", str3, "\" of the \"numberExpectedMessages\" parameter", " is not a valid long integer." }));
            }
            Console.WriteLine();
            Console.WriteLine("Messages we haven't received so far = ");
            if (!File.Exists(pathLogMsgsReceived))
            {
                Console.WriteLine(string.Format("The log file \"{0}\" does not exist!", pathLogMsgsReceived));
                flag = true;
            }
            if (!flag)
            {
                string str4 = new StreamReader(pathLogMsgsReceived).ReadToEnd();
                string str5 = "";
                list.Clear();
                for (long i = 1L; i < (num + 1L); i += 1L)
                {
                    string str6 = string.Format("MessageCount {0}&", i.ToString());
                    if (!str4.Contains(str6))
                    {
                        list.Add(i);
                    }
                }
                if (list.Count > 0)
                {
                    long num2 = (long)list[0];
                    str5 = "[" + list[0].ToString() + "-";
                    int num4 = 1;
                    while (num4 < list.Count)
                    {
                        if ((((long)list[num4]) - ((long)list[num4 - 1])) > 1L)
                        {
                            str5 = str5 + list[num4 - 1].ToString() + "]";
                            str5 = ((str5 + " = " + (((((long)list[num4 - 1]) - num2) + 1L)).ToString() + " message(s).") + Environment.NewLine) + "[" + list[num4].ToString() + "-";
                            num2 = (long)list[num4];
                        }
                        num4++;
                    }
                    str5 = str5 + list[num4 - 1].ToString() + "]";
                    str5 = str5 + " = " + (((((long)list[num4 - 1]) - num2) + 1L)).ToString() + " message(s).";
                }
                Console.WriteLine(str5);
            }
        }

        private void Funtionality_IndigoService(string[] args)
        {
            string str2;
            FormatException exception;
            bool flag = false;
            string str = null;
            bool flag2 = false;
            if (File.Exists(pathLogMsgsReceived))
            {
                File.Delete(pathLogMsgsReceived);
            }
            str = args[0];
            if (((str.ToLower() != "indigooneway") && (str.ToLower() != "indigotwowaysvoid")) && (str.ToLower() != "indigotwoways"))
            {
                flag = true;
                Console.WriteLine(string.Format("{0}{1}{2}{3}{4}", new object[] { "The value \"", str, "\" of the \"serviceType\" parameter ", " is not valid.", " Valid values are: IndigoOneWay, IndigoTwoWaysVoid, IndigoTwoWays." }));
            }
            PathRespMsgFile = args[1];
            if (!File.Exists(PathRespMsgFile))
            {
                flag = true;
                Console.WriteLine(string.Format("{0}{1}{2}", "The message file \"", PathRespMsgFile, "\" you indicated does not exist."));
            }
            string str3 = args[2];
            try
            {
                flag2 = Convert.ToBoolean(str3);
            }
            catch (FormatException exception1)
            {
                exception = exception1;
                flag = true;
                str2 = exception.ToString();
                Console.WriteLine(string.Format("{0}{1}{2}{3}", new object[] { "The value \"", str3, "\" of the \"transactional\" parameter", " is not a valid boolean." }));
            }
            if (args.Length == 4)
            {
                string str4 = args[3];
                try
                {
                    Logging = Convert.ToBoolean(str4);
                }
                catch (FormatException exception2)
                {
                    exception = exception2;
                    flag = true;
                    str2 = exception.ToString();
                    Console.WriteLine(string.Format("{0}{1}{2}{3}", new object[] { "The value \"", str4, "\" of the \"logging\" parameter", " is not a valid boolean." }));
                }
            }
            new IndigoServiceTwoWaysVoidNonTransactional().SelfHostService();
            
        }

        public static void Main(string[] args)
        {
            try
            {
                try
                {
                    new IndigoServiceTwoWaysVoidNonTransactional().SelfHostService();
                    //for (int i = 0; i < args.Length; i++)
                    //{
                    //    Console.WriteLine(string.Format("Command line argument {0} = {1}", i.ToString(), args[i]));
                    //}
                    //Console.WriteLine();
                    //Program program = new Program();
                    //switch (args.Length)
                    //{
                    //    case 2:
                    //        program.Functionality_Statistics(args);
                    //        return;

                    //    case 3:
                    //    case 4:
                    //        program.Funtionality_IndigoService(args);
                    //        return;
                    //}
                    //string str = "IndigoService.exe statistics <numberExpectedMessages>";
                    //string str2 = "IndigoService.exe <ServiceType[IndigoOneWay|IndigoTwoWaysVoid|IndigoTwoWays]> <pathResponseMsgFile> <Transctional[true|false]>";
                    //Console.WriteLine("Incorect arguments." + Environment.NewLine + str + Environment.NewLine + str2);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            }
            finally
            {
            }
        }
    }
}
