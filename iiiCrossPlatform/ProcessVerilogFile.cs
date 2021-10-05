using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace iiiCrossPlatform
{
    public enum VerilogNetTrojanClass
    {
        Normal, Trojan, TrojanInput, TrojanOutput
    }

    public enum VerilogGateClass
    {
        Normal, Dff
    }

    public enum VerilogNetClass
    {
        Input, Internal, Output
    }

    public enum Signal
    {
        None, High, Low
    }

    public enum VerilogPinClass
    {
        Input, Output
    }

    public static class ProcessVerilogFile
    {
        private static bool GateCheck = false;
        private static bool AssignCheck = false;

        enum Status
        {
            Module, Input, Output, Wire, Assign, Gate, EndModule
        }

        public static void ReadingVerilogFile()
        {
            string path = Tool.SettingFile.VerilogFileDir;

            Console.WriteLine();
            Console.WriteLine(" Start Processing Verilog file ...... ");
            Console.WriteLine(File.Exists(path) ? "  => Verilog file exists." :
                                                  "  => Verilog file does not exist.");
            Tool.VerilogFile.All.Clear();
            Tool.VerilogFile.Input.Clear();
            Tool.VerilogFile.Internal.Clear();
            Tool.VerilogFile.Output.Clear();
            Tool.VerilogFile.TrojanGate.Clear();
            Tool.VerilogFile.Gate.Clear();

            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string m = sr.ReadToEnd();

                    // find input / output / wire / assign untile no input/output/wire/assign keyWords

                    VerilogNet netHigh = new VerilogNet();
                    VerilogNet netLow = new VerilogNet();

                    netHigh.name = "1'b1";
                    netLow.name = "1'b0";
                    netHigh.classification = VerilogNetClass.Input;
                    netLow.classification = VerilogNetClass.Input;
                    netHigh.value.signal = Signal.High;
                    netLow.value.signal = Signal.Low;
                    netHigh.from = true;
                    netLow.from = true;

                    Tool.VerilogFile.Input.Add(netHigh);
                    Tool.VerilogFile.Input.Add(netLow);
                    Tool.VerilogFile.All.Add(netHigh);
                    Tool.VerilogFile.All.Add(netLow);

                    Status status = GetNextStatus(in m);
                    
                    while ( status != Status.EndModule)
                    {
                        if (status == Status.Module)
                        {
                            m = moduleStatus(m);
                        } else if (status == Status.Input)
                        {
                            m = inputStatus(m);
                        } else if (status == Status.Output)
                        {
                            m = outputSatus(m);
                        } else if (status == Status.Wire) 
                        {
                            m = wireStatus(m);
                        } else if (status == Status.Assign)
                        {
                            m = assignStatus(m);
                        } else if (status == Status.Gate)
                        {
                            m = gateStatus(ref m);
                        }
                        if (status == Status.Gate && m.IndexOf(";") == -1)
                            status = GetNextStatus(in m);
                        else if (status == Status.Gate)
                            status = Status.Gate;
                        else
                            status = GetNextStatus(m);
                        
                    }
                }
            }

            setFromAndToGate();
            Console.WriteLine();
        }

        public static void FindTrojanNet()
        {
            List<VerilogGate> l = new List<VerilogGate>();

            Tool.VerilogFile.Gate.ForEach(g =>
            {
                /*
                foreach(var tg in Tool.)
                {
                    if(g.name == tg)
                    {
                        l.Add(g);
                        break;
                    }
                }
                */
            });

            l.ForEach(g =>
            {
                g.pinList.ForEach(p =>
                {
                    p.connectNet.trojanClassification = VerilogNetTrojanClass.TrojanOutput;
                });
            });

            l.ForEach(g =>
            {
                g.pinList.ForEach(p =>
                {
                    if(p.classification == VerilogPinClass.Input)
                    {
                        bool check = true;

                        l.ForEach(g2 =>
                        {
                            g2.pinList.ForEach(p2 =>
                            {
                                if(p2.classification == VerilogPinClass.Output)
                                {
                                    if(p.connectNet.name == p2.connectNet.name)
                                    {
                                        check = false;
                                        p.connectNet.trojanClassification = VerilogNetTrojanClass.Trojan;
                                    }
                                }
                            });
                        });

                        if (check)
                        {
                            p.connectNet.trojanClassification = VerilogNetTrojanClass.TrojanInput;
                        }
                    }
                });
            });
        }

        private static Status GetNextStatus(in string  s)
        {
            Status nextLevel;

            // first keyWord "module" place
            int modulePlace = s.IndexOf("module");
            if (modulePlace == -1) modulePlace = 100000; // -1 means not found, replace by a large number
            // first keyWord "input" place
            int inputPlace = s.IndexOf("input");
            if (inputPlace == -1) inputPlace = 100000;
            // first keyWord "output" place
            int outputPlace = s.IndexOf("output");
            if (outputPlace == -1 || outputPlace>50) outputPlace = 100000;
            // first keyWord "wire" place
            int wirePlace = s.IndexOf("wire");
            if (wirePlace == -1) wirePlace = 100000;
            // first keyWord "assign" place
            int assignPlace = s.IndexOf("assign");
            if (assignPlace == -1) assignPlace = 100000;

            // no input, output, wire, assign
            if (s.IndexOf(";") == -1)
            {
                nextLevel = Status.EndModule;
            }
            else if (inputPlace == 100000 && outputPlace == 100000 && wirePlace == 100000 && assignPlace == 100000)
            {
                nextLevel = Status.Gate;
            }
            else if (modulePlace < inputPlace &&
                     modulePlace < outputPlace &&
                     modulePlace < wirePlace &&
                     modulePlace < assignPlace)
            {
                nextLevel = Status.Module;
            }
            else if (inputPlace < modulePlace &&
                     inputPlace < outputPlace &&
                     inputPlace < wirePlace &&
                     inputPlace < assignPlace)
            {
                nextLevel = Status.Input;
            }
            else if (outputPlace < modulePlace &&
                     outputPlace < inputPlace &&
                     outputPlace < wirePlace &&
                     outputPlace < assignPlace)
            {
                nextLevel = Status.Output;
            }
            else if (wirePlace < modulePlace &&
                     wirePlace < inputPlace &&
                     wirePlace < outputPlace &&
                     wirePlace < assignPlace)
            {
                nextLevel = Status.Wire;
            }
            else
            {
                nextLevel = Status.Assign;
            }

            return nextLevel;
        }

        private static string moduleStatus(string s)
        {
            Console.WriteLine("  => Process Module Status.");
            // format : module ..... ;
            int moduleStringEnd = s.IndexOf(";");

            // delete module ...... ;
            s = s.Substring(moduleStringEnd + 1);

            return s;
        }

        private static string inputStatus(string s)
        {
            int stringBeginIndex;

            Console.WriteLine("  => Process Input Status.");
            stringBeginIndex = s.IndexOf("input");

            int stringEndIndex = s.IndexOf(";");

            int l = stringEndIndex - stringBeginIndex + 1;
            string ss = s.Substring(stringBeginIndex, l);
            ss = ss.Replace("\n", "").Replace("\r", "").Replace("] ", "]");
            char[] c = { '\n', ',', ';', ' ' };

            // get all wire name and store in n
            string[] n = ss.Split(c, StringSplitOptions.RemoveEmptyEntries);

            // Construct Wire and store in WireList
            for (int i = 1; i < n.Length; i++)
            {
                int v = n[i].IndexOf(":");

                char[] se = { '[', ':', ']', ' ', '\t' };

                // [a:b] c
                //string[] nn = n[i].Split(se, StringSplitOptions.RemoveEmptyEntries);學長寫的我改掉
                string[] nn;//我寫的
                int wireMax = 0;
                int wireMin = 0;

                if (v != -1)
                {
                    nn = n[i].Split(se, StringSplitOptions.RemoveEmptyEntries);//我寫的
                    int.TryParse(nn[0], out wireMax);
                    int.TryParse(nn[1], out wireMin);
                }
                else //else我加的
                    nn = n[i].Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);//我寫的

                for (int j = wireMin; j <= wireMax; j++)
                {
                    VerilogNet net = new VerilogNet();
                    if (v != -1)
                    {
                        net.name = nn[2] + "[" + j + "]";
                    }
                    else
                    {
                        net.name = nn[0];
                    }

                    net.classification = VerilogNetClass.Input;
                    net.from = true;
                    Tool.VerilogFile.Input.Add(net);
                    Tool.VerilogFile.All.Add(net);
                }
            }

            s = s.Substring(stringEndIndex + 1);
            return s;
        }

        private static string outputSatus(string s)
        {
            int stringBeginIndex;

            Console.WriteLine("  => Process Output Status.");
            stringBeginIndex = s.IndexOf("output");

            int stringEndIndex = s.IndexOf(";");

            int l = stringEndIndex - stringBeginIndex + 1;
            string ss = s.Substring(stringBeginIndex, l);
            ss = ss.Replace("\n", "").Replace("\r", "").Replace("] ", "]");
            char[] c = { '\n', ',', ';', ' ' };

            // get all wire name and store in n
            string[] n = ss.Split(c, StringSplitOptions.RemoveEmptyEntries);

            // Construct Wire and store in WireList
            for (int i = 1; i < n.Length; i++)
            {
                int v = n[i].IndexOf(":");

                char[] se = { '[', ':', ']', ' ', '\t' };

                // [a:b] c
                //string[] nn = n[i].Split(se, StringSplitOptions.RemoveEmptyEntries);學長寫的我改掉
                string[] nn;//我寫得
                int wireMax = 0;
                int wireMin = 0;
                if (v != -1)
                {
                    nn = n[i].Split(se, StringSplitOptions.RemoveEmptyEntries);//我寫的
                    int.TryParse(nn[0], out wireMax);
                    int.TryParse(nn[1], out wireMin);
                }
                else //else我加的
                    nn = n[i].Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);//我寫的

                for (int j = wireMin; j <= wireMax; j++)
                {
                    VerilogNet net = new VerilogNet();

                    if (v != -1)
                    {
                        net.name = nn[2] + "[" + j + "]";
                    }
                    else
                    {
                        net.name = nn[0];
                    }

                    net.classification = VerilogNetClass.Output;
                    Tool.VerilogFile.Output.Add(net);
                    Tool.VerilogFile.All.Add(net);
                }
            }

            s = s.Substring(stringEndIndex + 1);
            return s;
        }

        private static string wireStatus(string s)
        {
            int stringBeginIndex;

            Console.WriteLine("  => Process Wire Status.");
            stringBeginIndex = s.IndexOf("wire");

            int stringEndIndex = s.IndexOf(";");

            int l = stringEndIndex - stringBeginIndex + 1;
            string ss = s.Substring(stringBeginIndex, l);
            ss = ss.Replace("\n", "").Replace("\r", "").Replace("] ", "]");
            char[] c = { '\n', ',', ';', ' ' };

            // get all wire name and store in n
            string[] n = ss.Split(c, StringSplitOptions.RemoveEmptyEntries);

            // Construct Wire and store in WireList
            for (int i = 1; i < n.Length; i++)
            {
                int v = n[i].IndexOf(":");

                char[] se = { '[', ':', ']', ' ', '\t' };

                // [a:b] c
                //string[] nn = n[i].Split(se, StringSplitOptions.RemoveEmptyEntries); 學長寫的 我改掉
                string[] nn;//我寫的
                int wireMax = 0;
                int wireMin = 0;
                if (v != -1)
                {
                    nn = n[i].Split(se, StringSplitOptions.RemoveEmptyEntries);//我寫的
                    int.TryParse(nn[0], out wireMax);
                    int.TryParse(nn[1], out wireMin);
                }
                else //else我加的
                    nn = n[i].Split(new char[2] {' ','\t'}, StringSplitOptions.RemoveEmptyEntries);//我寫的

                for (int j = wireMin; j <= wireMax; j++)
                {
                    string name = "";
                    if (v != -1)
                    {
                        name = nn[2] + "[" + j + "]";
                    }
                    else
                    {
                        name = nn[0];
                    }

                    bool checkInput = Tool.VerilogFile.Input.Exists(t => t.name == name);
                    bool checkOutupt = Tool.VerilogFile.Output.Exists(t => t.name == name);

                    if( !checkInput && !checkOutupt)
                    {
                        VerilogNet net = new VerilogNet();
                        net.name = name;
                        net.classification = VerilogNetClass.Internal;
                        Tool.VerilogFile.Internal.Add(net);
                        Tool.VerilogFile.All.Add(net);
                    }
                }
            }

            s = s.Substring(stringEndIndex + 1);
            return s;
        }

        private static string assignStatus(string s)
        {
            if (AssignCheck == false)
            {
                AssignCheck = true;
                Console.WriteLine("  => Process Assign Status.");
            }

            int assignStringBegin = s.IndexOf("assign");
            int assignStringEnd = s.IndexOf(";");
            int l = assignStringEnd - assignStringBegin + 1;

            string ss = s.Substring(assignStringBegin, l);
            char[] c = { ' ', '=', ';' };
            string[] n = ss.Split(c, StringSplitOptions.RemoveEmptyEntries);

            string netOneName = n[1];
            string netTwoName = n[2];

            int netOneIndex = Tool.VerilogFile.All.FindIndex(t => t.name == netOneName);
            int netTwoIndex = Tool.VerilogFile.All.FindIndex(t => t.name == netTwoName);

            VerilogNet net1, net2 ;

            if(netOneIndex == -1)
            {
                net1 = new VerilogNet();
                net1.name = netOneName;
            }
            else
            {
                net1 = Tool.VerilogFile.All[netOneIndex];
            }
            
            if(netTwoIndex == -1)
            {
                net2 = new VerilogNet();
                net2.name = netTwoName;
            }
            else
            {
                net2 = Tool.VerilogFile.All[netTwoIndex];
            }

            net1.assignByOthers = true;
            net1.value = net2.value;

            net1.FromWire.Add(net2);  //這行我自己加的

            s = s.Substring(assignStringEnd + 1);
            return s;
        }

        private static string gateStatus(ref string s)
        {

            if (GateCheck == false)
            {
                GateCheck = true;
                Console.WriteLine("  => Process Gate Status.");
            }

            int gateStringEnd = s.IndexOf(";");
            string ss = s.Substring(0, gateStringEnd + 1);
            ss = ss.Replace("\n", "").Replace("\r", "");

            // regex format => CellName GateName ( IO );
            string pattern = @"(?<CellName>[^\s]+)\s+(?<GateName>[^\s]+)\s+\((?<IO>.+)\);";

            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            if (rgx.IsMatch(ss))
            {
                Match m = rgx.Match(ss);

                int cellIndex = -1;

                for (int i = 0; i < Tool.CellFile.CellLib.Count; i++)
                {
                    if (m.Groups["CellName"].Value == Tool.CellFile.CellLib[i].name)
                    {
                        cellIndex = i;
                        break;
                    }
                }

                if (cellIndex != -1)
                {
                    VerilogGate g = new VerilogGate();

                    if (Tool.CellFile.CellLib[cellIndex].ff != null)
                    {
                        g.classification = VerilogGateClass.Dff;
                    }
                    else
                    {
                        g.classification = VerilogGateClass.Normal;
                    }

                    g.name = m.Groups["GateName"].Value;

                    string b = " ";

                    string IO = m.Groups["IO"].Value;

                    IO = IO.Replace(b, string.Empty);
                    ///////////////////////////////////////////////////////這一塊我加的
                    IO = IO.Replace("\n", "").Replace("\r", "");
                    string pattern2 = @"\.(?<Pin>.+?\(.+?\))";
                    Regex rgx2 = new Regex(pattern2, RegexOptions.IgnoreCase);
                    MatchCollection m2= rgx2.Matches(IO);
                    List<String> pinInformation = new List<String>();
                    foreach (Match result in m2)
                    {
                        pinInformation.Add(result.Groups["Pin"].Value);
                        //Console.WriteLine(result.Groups["Pin"].Value);
                    }
                    ///////////////////////////////////////////////////////這一塊我加的

                    /*這兩行學長寫的
                    char[] cs = { '.', '\n', ';', ',' };

                    string[] pinInformation = IO.Split( cs, StringSplitOptions.RemoveEmptyEntries);
                    */
                    char[] cs2 = { '(', ')' };

                    for (int i = 0; i < pinInformation.Count; i++) //原本學長的pinInformation是陣列所以是Length，不過我用list所以是count
                    {
                        string[] PinAndNet = pinInformation[i].Split( cs2, StringSplitOptions.RemoveEmptyEntries);

                        if (PinAndNet.Length == 2)
                        {
                            string pinName = PinAndNet[0];
                            string netName = PinAndNet[1];

                            VerilogPin p = new VerilogPin();
                            p.name = pinName;

                            bool checkPin = false;

                            g.cell = Tool.CellFile.CellLib[cellIndex];

                            for (int j = 0; j < g.cell.pinList.Count; j++)
                            {
                                if (g.cell.pinList[j].name == p.name)
                                {
                                    if (g.cell.pinList[j].classification == CellPinClass.Input)
                                    {
                                        p.classification = VerilogPinClass.Input;
                                    }
                                    else if (g.cell.pinList[j].classification == CellPinClass.Output)
                                    {
                                        p.classification = VerilogPinClass.Output;
                                        p.postfixOfFunction = g.cell.pinList[j].postfixOfFunction;
                                    }

                                    int netIndex = Tool.VerilogFile.All.FindIndex(t => t.name == netName);

                                    if (netIndex == -1)
                                    {
                                        VerilogNet net = new VerilogNet();
                                        net.name = netName;
                                        net.classification = VerilogNetClass.Internal;
                                        Tool.VerilogFile.Internal.Add(net);
                                        Tool.VerilogFile.All.Add(net);
                                        p.connectNet = net;
                                        Console.WriteLine("注意!! " + net.name + " 沒有出現在wire or intput or output就直接出現在gate，可能是程式有錯或電路本來就這樣");//我寫的
                                    }
                                    else
                                    {
                                        p.connectNet = Tool.VerilogFile.All[netIndex];
                                    }

                                    checkPin = true;
                                    break;
                                }
                            }

                            if (checkPin == false)
                            {
                                Console.WriteLine("  => Error : Can't find Pin " + p.name + " in " + g.cell.name);
                            }
                            else
                            {
                                g.pinList.Add(p);
                            }
                        }
                        else
                        {
                            Console.WriteLine("  => Warring : Can't find Pin or Wire in " + g.name);
                        }
                    }

                    if (g.classification == VerilogGateClass.Dff) Tool.VerilogFile.Dff.Add(g);
                    Tool.VerilogFile.Gate.Add(g);
                }
                else
                {
                    Console.WriteLine("  => Error : Can't  find  cell \"" + m.Groups["CellName"].Value + "\"");
                }

            }
            else
            {
                Console.WriteLine("  => Error : Can't parse \"" + ss + "\"");
            }

            s = s.Substring(gateStringEnd + 1);
            return s;
        }

        private static void setFromAndToGate()
        {
            Tool.VerilogFile.Gate.ForEach(gate =>
            {
                gate.pinList.ForEach(p =>
                {
                    if(p.classification == VerilogPinClass.Input)
                    {
                        p.connectNet.ToGate.Add(gate);
                    }
                    else
                    {
                        p.connectNet.FromGate.Add(gate);
                    }
                });
            });
        }
    }

    public class VerilogNet
    {
        public string name = "";
        public bool from = false;
        public bool assignByOthers = false;
        public VerilogNetClass classification = VerilogNetClass.Internal;
        public VerilogNetTrojanClass trojanClassification = VerilogNetTrojanClass.Normal;
        public List<VerilogGate> ToGate = new List<VerilogGate>();
        public List<VerilogGate> FromGate = new List<VerilogGate>();
        public Value value = new Value();

        public List<VerilogNet> FromWire = new List<VerilogNet>();    //我家的
    }

    public class VerilogGate
    {
        public string name = "";
        public VerilogGateClass classification = VerilogGateClass.Normal;
        public Cell cell;
        public DffValue dffValue = new DffValue();
        public List<VerilogPin> pinList = new List<VerilogPin>();

        public bool TrojanInputGate = false;
        public bool findPathVisited = false;
        public int minLevelToOutput = int.MaxValue;  // maximum level to PO or FF
    }

    public class Value
    {
        public Signal signal = Signal.None;
        public float activeCounter = 0;
        public double activeProbability = 0;
    }

    public class DffValue
    {
        public Signal IQ = Signal.Low;
        public Signal IQN = Signal.High;
    }

    public class VerilogPin
    {
        public string name;
        public VerilogNet connectNet;
        public VerilogPinClass classification = VerilogPinClass.Input;
        public List<string> postfixOfFunction = new List<string>();
    }
}
