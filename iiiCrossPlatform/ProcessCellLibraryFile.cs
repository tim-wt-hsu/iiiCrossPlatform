using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace iiiCrossPlatform
{
    public static class ProcessCellLibraryFile
    {
        public static void ReadingCellLibrary()
        {
            string path = Tool.SettingFile.CellLibraryDir;

            Console.WriteLine();
            Console.WriteLine(" Start Processing Lib file ...... ");
            Console.WriteLine(File.Exists(path) ? "  => Library file exists." :
                                                  "  => Library file does not exist.");
            Tool.CellFile.CellLib.Clear();
            {
                Cell cellOne = new Cell();
                cellOne.area = "7.116032";
                cellOne.cellLeakagePower = "2.145841e+05";
                cellOne.ff = null;
                cellOne.name = "LSDNENX1_RVT";
                CellPin pinOne = new CellPin();
                pinOne.classification = CellPinClass.Input;
                pinOne.function = "";
                pinOne.name = "EN";
                CellPin pinTwo = new CellPin();
                pinTwo.classification = CellPinClass.Input;
                pinTwo.function = "";
                pinTwo.name = "A";
                CellPin pinThree = new CellPin();
                pinThree.classification = CellPinClass.Output;
                pinThree.function = "EN+A";
                pinThree.name = "Y";
                List<string> postfunction = new List<string>();
                postfunction.Add("EN"); postfunction.Add("A"); postfunction.Add("+");
                pinThree.postfixOfFunction = postfunction;
                cellOne.pinList.Add(pinOne);
                cellOne.pinList.Add(pinTwo);
                cellOne.pinList.Add(pinThree);
                Tool.CellFile.CellLib.Add(cellOne);
                Console.WriteLine("test");
            }
            {   //加入這個latch並當成D flip-flop來解決PIC16F84出現的latch
                Cell cellOne = new Cell();
                cellOne.area = "5.08288";
                cellOne.cellLeakagePower = "1.443476e+05";
                cellOne.name = "LATCHX1_RVT";
                cellOne.cellFootprint = "LATCH";
                FF ff = new FF();
                ff.clear = "";
                ff.clock = Clock.LowToHigh;
                ff.nextState = "D";
                List<string> postfunction = new List<string>();
                postfunction.Add("D");

                ff.postfixOfNextState = postfunction;
                cellOne.ff = ff;

                Tool.CellFile.CellLib.Add(cellOne);
                Console.WriteLine("test");
            }
            //cellOne.pinList.add



            try
            {
                //Cell newCell = new Cell();
                //newCell.name = "LSDNX1_RVT";
                if (File.Exists(path))
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        int counter = 0; // count { and }
                        int cellCounter = 0;
                        int fffCounter = 0;

                        while (sr.Peek() > -1)
                        {
                            string s = sr.ReadLine();
                            counter = bracketsCaculate(s, counter);

                            if (s.Contains("cell (") && counter == 2)
                            {
                                int frontBracketsIndex = s.IndexOf("(");
                                int backBracketsIndex = s.IndexOf(")");

                                if (s[frontBracketsIndex + 1] == '\"')
                                {
                                    frontBracketsIndex++;
                                }

                                if (s[backBracketsIndex - 1] == '\"')
                                {
                                    backBracketsIndex--;
                                }

                                int nameLength = backBracketsIndex - frontBracketsIndex - 1;
                                string name = s.Substring(frontBracketsIndex + 1, nameLength);

                                Cell cell = new Cell();
                                cell.name = name;

                                while (counter != 1)
                                {
                                    string ss = sr.ReadLine();
                                    counter = bracketsCaculate(ss, counter);
                                    if (ss.Contains("cell_footprint") && counter == 2)
                                    {
                                        char[] cs = new char[] { '\"' };
                                        string[] n = ss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                        try
                                        {
                                            cell.cellFootprint = n[1];
                                        }
                                        catch
                                        {
                                            Console.WriteLine("  => Error : can't read cell footprint of {0}", cell.name);
                                        }
                                    }
                                    else if (ss.Contains("area") && counter == 2)
                                    {
                                        // format => area : XXXXXXXX;
                                        char[] cs = new char[] { ' ' };
                                        string[] n = ss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                        try
                                        {
                                            cell.area = n[2];
                                        }
                                        catch
                                        {
                                            Console.WriteLine("  => Error : Can't read area of {0}", cell.name);
                                        }
                                    }
                                    else if (ss.Contains("cell_leakage_power") && counter == 2)
                                    {
                                        char[] cs = new char[] { ' ', ';' };
                                        string[] n = ss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                        try
                                        {
                                            cell.cellLeakagePower = n[2];
                                        }
                                        catch
                                        {
                                            Console.WriteLine("  => Error: Can't read area of {0}", cell.name);
                                        }
                                    }
                                    else if (ss.Contains("ff (" )&& counter == 3) 
                                    {
                                        int ffCounter = 1;
                                        FF ff = new FF();
                                        while (ffCounter == 1)
                                        {
                                            string sss = sr.ReadLine();

                                            ffCounter = bracketsCaculate(sss, ffCounter);
                                            counter = bracketsCaculate(sss, counter);

                                            if (sss.Contains("clocked_on") && counter == 3)
                                            {
                                                // format => clocked_on : "XXX"
                                                try
                                                {
                                                    char[] cs = new char[] {'"'};
                                                    string[] n = sss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                                    if (n[1].Contains("\'"))
                                                    {
                                                        ff.clock = Clock.HighToLow;
                                                    }
                                                    else
                                                    {
                                                        ff.clock = Clock.LowToHigh;
                                                    }
                                                }
                                                catch
                                                {
                                                    Console.WriteLine("  => Error: Can't read FF Clocked_On of {0}", cell.name);
                                                }
                                            }
                                            else if (sss.Contains("next_state") && counter == 3)
                                            {
                                                // format => next_state : "XXX"
                                                try
                                                {
                                                    char[] cs = new char[] { '"' };
                                                    string[] n = sss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                                    ff.nextState = n[1];
                                                    ff.postfixOfNextState = changeInfixToPostfix(n[1]);
                                                }
                                                catch
                                                {
                                                    Console.WriteLine("  => Error: Can't read FF Next_State of {0}", cell.name);
                                                }
                                            }
                                        }
                                        fffCounter++;
                                        cell.ff = ff;
                                    }
                                    else if (ss.Contains("pg_pin") && counter == 3)
                                    {
                                        // do nothing 
                                    }
                                    else if (ss.Contains("pin (") && counter == 3)
                                    {
                                        try
                                        {
                                            CellPin pin = new CellPin();
                                            int f = ss.IndexOf("(");
                                            int e = ss.IndexOf(")");
                                            int l = e - f - 1;
                                            pin.name = ss.Substring(f + 1, l);
                                            cell.pinList.Add(pin);

                                            while (counter >= 3)
                                            {
                                                string sss = sr.ReadLine();
                                                counter = bracketsCaculate(sss, counter);

                                                if (sss.Contains("direction") && counter == 3)
                                                {
                                                    int i = sss.IndexOf('"');

                                                    if (i == -1)
                                                    {
                                                        char[] cs = { ' ', ';' };
                                                        string[] n = sss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                                        if (n[2] == "input") pin.classification = CellPinClass.Input;
                                                        else if (n[2] == "output") pin.classification = CellPinClass.Output;
                                                        else pin.classification = CellPinClass.Internal;
                                                    }
                                                    else
                                                    {
                                                        char[] cs = new char[] { '"' };
                                                        string[] n = sss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                                        if (n[1] == "input") pin.classification = CellPinClass.Input;
                                                        else if (n[1] == "output") pin.classification = CellPinClass.Output;
                                                        else pin.classification = CellPinClass.Internal;
                                                    }
                                                }
                                                else if (sss.Contains("power_down_function") && counter == 3)
                                                {
                                                    // do nothing
                                                }
                                                else if (sss.Contains("function") && counter == 3)
                                                {
                                                    int i = sss.IndexOf('\"');

                                                    if (i == -1)
                                                    {
                                                        char[] cs = { ' ', ';' };
                                                        string[] n = sss.Split(cs, StringSplitOptions.RemoveEmptyEntries);
                                                        pin.function = n[2];
                                                        pin.postfixOfFunction = changeInfixToPostfix(n[2]);
                                                    }
                                                    else
                                                    {
                                                        char[] cs = { '"' };
                                                        string[] n = sss.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                                                        pin.function = n[1];
                                                        pin.postfixOfFunction = changeInfixToPostfix(n[1]);
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("  => Error: Can't read Pin Name of {0}", cell.name);
                                        }
                                    }
                                }

                                Tool.CellFile.CellLib.Add(cell);
                                cellCounter++;
                            }
                        }

                        Console.WriteLine("  => Total Cell Num : " + cellCounter);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" => Error : The process failed: {0}", e.ToString());
            }
        }

        private static int bracketsCaculate(string s, int counter)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{')
                {
                    counter++;
                }
                if (s[i] == '}')
                {
                    counter--;
                }
            }

            return counter;
        }

        private static List<string> changeInfixToPostfix(string s)
        {
            List<string> postfix = new List<string>();

            Stack<string> stack = new Stack<string>();
            // '(', ')' , '+' , '*' , '^' , ''' ,

            string pinName = "";

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '(' || s[i] == '+' ||
                    s[i] == '*' || s[i] == '^' ||
                    s[i] == '\'' || s[i] == ')'|| s[i] == '!')  //Peter
                {
                    if(pinName != "")
                    {
                        postfix.Add(pinName);
                        pinName = "";
                    }

                    if (s[i] == ')')
                    {
                        string ss = "";
                        while (ss != "(")
                        {
                            ss = stack.Pop();
                            if (ss != "(") postfix.Add(ss);
                        }
                    }
                    else
                    {
                        stack.Push(s[i].ToString());
                    }
                }
                else
                {
                    pinName += s[i];
                }
            }

            if(pinName != "")
            {
                postfix.Add(pinName);
                pinName = "";
            }

            while (stack.Count != 0)
            {
                postfix.Add(stack.Pop());
            }

            return postfix;
        }
    }

    public class Cell
    {
        public string name = "";
        public string cellFootprint = "";
        public string area = "";
        public string cellLeakagePower = "";

        public FF ff;
        public List<CellPin> pinList = new List<CellPin>();
    }

    public class FF
    {
        public Clock clock;
        public string nextState = "";
        public string clear = "";
        public List<string> postfixOfNextState = new List<string>();
    }

    public class CellPin
    {
        public string name;
        public CellPinClass classification = CellPinClass.Input;
        public string function = "";
        public List<string> postfixOfFunction = new List<string>();
    }

    public enum CellPinClass
    {
        Input,
        Internal,
        Output
    }
}
