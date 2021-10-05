using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iiiCrossPlatform
{
    public static class ProbabilityMethod
    {
        private static Random rand = new Random();

        public static void ProcessProbabilityMethod()
        {
            Console.WriteLine(" Start Processing Topology Sort ...... ");

            bool check = InitialTopology();

            if (check)
            {
                Topology();
                CheckAllNetCanGetSignal();
                InitialProbability();

                for (int i = 1; i <= Tool.SettingFile.SimulateRound; i++)
                {
                    ProbabilitySimulate();

                    if (i % 1000 == 0)
                    {
                        Console.WriteLine("   => Process " + i + "  round. ");
                    }
                }

                CaculateActiveProbability();

                foreach(var n in Tool.VerilogFile.All)
                {
                    Console.WriteLine("    => " + n.name + " " + n.value.activeProbability);
                }

                Console.WriteLine("   => Simulate End. ");
            }
        }
        
        private static bool InitialTopology()
        {
            bool check = true;
            Tool.VerilogFile.All.ForEach(net => {
                if (net.name == Tool.SettingFile.Clk)
                {
                    check = false;
                }

                if (!net.assignByOthers)
                {
                    net.value.signal = Signal.None;
                }
            });

            if (check)
            {
                Console.WriteLine("  => Can't find CLK signal.");
                return false;
            }

            return true;
        }

        private static void Topology()
        {

            int reg_counter = 0;

            Tool.VerilogFile.Gate.ForEach(gate =>
            {
                if(gate.classification == VerilogGateClass.Dff)
                {
                    reg_counter++;
                    gate.pinList.ForEach(p =>
                    {
                        if(p.classification == VerilogPinClass.Output)
                        {
                            p.connectNet.value.signal = Signal.High;
                        }
                    });
                }
            });
            Console.WriteLine("  => Total " + reg_counter + " registers.");

            Tool.VerilogFile.Input.ForEach(net => { 
                if(!net.assignByOthers)
                {
                    net.value.signal = Signal.High;
                }
            });
            Console.WriteLine("  => Total " + Tool.VerilogFile.Input.Count + " inputs.");

            
            while (true)
            {
                int gateNum = Tool.VerilogFile.TopologySort.Count;

                Tool.VerilogFile.Gate.ForEach(gate =>
               {
                   bool ch = true;

                   gate.pinList.ForEach(pin =>
                  {
                      if(pin.classification == VerilogPinClass.Input && pin.connectNet.value.signal == Signal.None)
                      {
                          ch = false;
                      }
                  });

                   if (ch)
                   {
                       bool e = Tool.VerilogFile.TopologySort.Exists(t => t.name == gate.name);

                       if(!e)
                       {
                           Tool.VerilogFile.TopologySort.Add(gate);

                           gate.pinList.ForEach(pin =>
                           {
                               if( pin.classification == VerilogPinClass.Output)
                               {
                                   pin.connectNet.value.signal = Signal.High;
                               }
                           });
                       }
                   }
               });

                if( Tool.VerilogFile.TopologySort.Count == gateNum)
                {
                    Console.WriteLine("  => End Topology Sort of Circuit. ");
                    break;
                }
            }


            if (Tool.VerilogFile.Gate.Count == Tool.VerilogFile.TopologySort.Count) Console.WriteLine("  => Topology Circuit Successful.");
            else Console.WriteLine("  => Warning : Lose some gate in topology circuit.");

            Console.WriteLine("  => Total Gate : " + Tool.VerilogFile.Gate.Count);
            Console.WriteLine("  => Topology Gate : " + Tool.VerilogFile.TopologySort.Count);

            Console.WriteLine();
        }
        
        private static void InitialProbability()
        {
            Tool.VerilogFile.All.ForEach(net =>
            {
                if (net.name == "1'b1")
                {
                    net.value.signal = Signal.High;
                }
                else if (net.name == "1'b0")
                {
                    net.value.signal = Signal.Low;
                }
                else if (net.name == "CK") 
                {
                    net.value.signal = Signal.High;
                }
                else if (!net.assignByOthers)
                {
                    net.value.signal = Signal.None;
                }
                net.value.activeCounter = 0;
                net.value.activeProbability = 0;
            });

            Tool.VerilogFile.Dff.ForEach(gate =>
            {
                gate.pinList.ForEach(pin => {
                    if(pin.classification == VerilogPinClass.Output)
                    {
                        if (pin.postfixOfFunction[0] == "IQ") pin.connectNet.value.signal = gate.dffValue.IQ;
                        else if (pin.postfixOfFunction[0] == "IQN") pin.connectNet.value.signal = gate.dffValue.IQN;
                        else Console.WriteLine("  => Error : Not IQ or IQN in " + gate.name);
                    }
                });
            });
        }

        private static void CheckAllNetCanGetSignal()
        {
            Tool.VerilogFile.All.ForEach(net => {
                if(net.value.signal == Signal.None)
                {
                    Console.WriteLine("  => " + net.name + " can't get signal. Set " + net.name + " in Input list.");
                    Tool.VerilogFile.Input.Add(net);
                }
            });
        }
        
        private static void ProbabilitySimulate()
        {

            Tool.VerilogFile.Input.ForEach(net =>
            {
                if (net.name == "1'b1")
                {
                    net.value.signal = Signal.High;
                } else if (net.name == "1'b0")
                {
                    net.value.signal = Signal.Low;
                } else if (net.name == Tool.SettingFile.Clk) 
                {
                    if (net.value.signal == Signal.High)
                        net.value.signal = Signal.Low;
                    else
                        net.value.signal = Signal.High;

                    if (net.value.signal == Signal.High)
                    {
                        if (Tool.VerilogFile.clk == Clock.LowToLow || Tool.VerilogFile.clk == Clock.HighToLow)
                        {
                            Tool.VerilogFile.clk = Clock.LowToHigh;
                        }
                        else
                        {
                            Tool.VerilogFile.clk = Clock.HighToHigh;
                        }
                    }
                    else
                    {
                        if (Tool.VerilogFile.clk == Clock.HighToHigh || Tool.VerilogFile.clk == Clock.LowToHigh)
                        {
                            Tool.VerilogFile.clk = Clock.HighToLow;
                        }
                        else
                        {
                            Tool.VerilogFile.clk = Clock.LowToLow;
                        }
                    }
                } else if (!net.assignByOthers)
                {
                    double r = rand.NextDouble();

                    if (r >= 0.5)
                    {
                        net.value.signal = Signal.High;
                    }
                    else
                    {

                        net.value.signal = Signal.Low;
                    }
                }
            });

            Tool.VerilogFile.Dff.ForEach(gate =>
            {
                gate.pinList.ForEach(pin => {
                    if(pin.classification == VerilogPinClass.Output)
                    {
                        if((gate.cell.ff.clock == Clock.LowToHigh && Tool.VerilogFile.clk == Clock.LowToHigh) ||
                            (gate.cell.ff.clock == Clock.HighToLow && Tool.VerilogFile.clk == Clock.HighToLow))
                        {
                            if (pin.postfixOfFunction[0] == "IQ") pin.connectNet.value.signal = gate.dffValue.IQ;
                            else if (pin.postfixOfFunction[0] == "IQN") pin.connectNet.value.signal = gate.dffValue.IQN;
                            else Console.WriteLine("  => Error : Not IQ or IQN in " + gate.name);

                        }
                    }
                });
            });

            Tool.VerilogFile.TopologySort.ForEach(gate => 
            {
                gate.pinList.ForEach(pin => {
                    if(pin.classification == VerilogPinClass.Output)
                    {
                        if(gate.classification == VerilogGateClass.Dff)
                        {
                            gate.dffValue.IQ = CaculatePostfixFunction(gate.pinList, gate.cell.ff.postfixOfNextState);

                            if (gate.dffValue.IQ == Signal.High) gate.dffValue.IQN = Signal.Low;
                            else if (gate.dffValue.IQ == Signal.Low) gate.dffValue.IQN = Signal.High;
                            else Console.WriteLine("  => Error : Wrong in QQQQQQQQ " + gate.name);
                        }
                        else
                        {
                            pin.connectNet.value.signal = CaculatePostfixFunction(gate.pinList, pin.postfixOfFunction);
                        }
                    }
                });
            });

            CaculateActiveCounter();
        }

        private static Signal CaculatePostfixFunction(List<VerilogPin> PinList, List<string> Postfix)
        {
            try
            {

                Stack<bool> Caculator = new Stack<bool>();

                foreach (var s in Postfix)
                {
                    if (s == "*")
                    {
                        bool v1 = Caculator.Pop();
                        bool v2 = Caculator.Pop();
                        Caculator.Push(v1 & v2);
                    }
                    else if (s == "+")
                    {
                        bool v1 = Caculator.Pop();
                        bool v2 = Caculator.Pop();
                        Caculator.Push(v1 | v2);
                    }
                    else if (s == "\'")
                    {
                        bool v1 = Caculator.Pop();
                        Caculator.Push(!v1);
                    }
                    else if (s == "^")
                    {
                        bool v1 = Caculator.Pop();
                        bool v2 = Caculator.Pop();
                        Caculator.Push(v1 ^ v2);
                    }
                    else if (s == "!")  //Peter
                    {
                        bool v1 = Caculator.Pop();
                        Caculator.Push(!v1);
                    }
                    else
                    {
                        foreach (var p in PinList)
                        {
                            if (p.name == s)
                            {

                                if (p.connectNet.value.signal == Signal.High)
                                {
                                    Caculator.Push(true);
                                }
                                else if (p.connectNet.value.signal == Signal.Low)
                                {
                                    Caculator.Push(false);
                                }
                                else
                                {
                                    Console.WriteLine("  => Can't get signal in " + p.name + " " + p.connectNet.name + " " + p.connectNet.value.signal);
                                    return Signal.None;
                                }
                            }
                        }
                    }
                }

                bool result = Caculator.Pop();

                if (result && Caculator.Count == 0)
                {
                    return Signal.High;
                }
                else if (!result && Caculator.Count == 0)
                {
                    return Signal.Low;
                }
                else
                {
                    Console.WriteLine("  => Counter Not 0");
                    return Signal.None;
                }
            }
            catch
            {
                Console.WriteLine("  => Error : Wrong when caculate postfix. ");
                return Signal.None;
            }
        }
    
        private static void CaculateActiveCounter()
        {
            Tool.VerilogFile.All.ForEach(net =>
            {
                if(net.value.signal == Signal.High && !net.assignByOthers)
                {
                    net.value.activeCounter++;
                }
            });
        }

        private static void CaculateActiveProbability()
        {
            Tool.VerilogFile.All.ForEach(net =>
            {
                if (!net.assignByOthers)
                {
                    net.value.activeProbability = (double)((double)net.value.activeCounter / (double)Tool.SettingFile.SimulateRound);
                }
            });
        }
    }
}
