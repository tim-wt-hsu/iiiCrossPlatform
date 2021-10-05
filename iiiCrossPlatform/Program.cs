using System;
using System.Collections.Generic;
using System.IO;
namespace iiiCrossPlatform
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowMessage.showLicenseMessage();

            while (true)
            {
    
                string path = Console.ReadLine().Trim();

                if (path.Contains("break")) break;
                else
                {


                    if (File.Exists(path))
                    {
                        System.IO.StreamReader file = new System.IO.StreamReader(path);

                        string s = "";

                        while ((s = file.ReadLine()) != null)
                        {
                            char[] c = { ' ', '=' };
                            if (s.Contains("///"))
                            {
                                // donothing
                            }
                            else if (s.Contains("VerilogFileDir"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.VerilogFileDir = ss[1];
                            }
                            else if (s.Contains("CellLibraryFileDir"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.CellLibraryDir = ss[1];
                            }
                            else if (s.Contains("BenchmarkFileDir"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.BenchmarkFileDir = ss[1];
                            }
                            else if (s.Contains("CellLibraryFileDir"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.CellLibraryDir = ss[1];
                            }
                            else if (s.Contains("MappingFileDir"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.MappingFileDir = ss[1];
                            }
                            else if (s.Contains("Clk"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.Clk = ss[1];
                            }
                            else if (s.Contains("ThresholdZero"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                bool check = Double.TryParse(ss[1], out Tool.SettingFile.ThresholdZero);
                                if (!check) Console.WriteLine("  => Error : Can't parse ThresholdZero. Threshold Zero in setting file : " + ss[1]);
                            }
                            else if (s.Contains("ThresholdOne"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                bool check = Double.TryParse(ss[1], out Tool.SettingFile.ThresholdOne);
                                if (!check) Console.WriteLine("  => Error : Can't parse ThresholdOne. Threshold One in setting file : " + ss[1]);
                            }
                            else if (s.Contains("SimulateRound"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                bool check = Int32.TryParse(ss[1], out Tool.SettingFile.SimulateRound);
                                if (!check) Console.WriteLine("  => Error : Can't parse Simulate Round. Simulate Round in setting file : " + ss[1]);
                            }
                            else if (s.Contains("TotalFolderDir"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                Tool.SettingFile.TotalFolderDir = ss[1];
                            }
                            else if (s.Contains("TrojanGate"))
                            {
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 1; i < ss.Length; i++)
                                    Tool.VerilogFile.TrojanGate.Add(ss[i]);
                            }
                            else if (s.Contains("TrainingSetRatio"))
                            {
                                s = s.Replace("%", "");
                                string[] ss = s.Split(c, StringSplitOptions.RemoveEmptyEntries);
                                bool check = Int32.TryParse(ss[1], out Tool.SettingFile.trainingSetRatio);
                                if (!check) Console.WriteLine("  => Error : Can't parse Trainging Set Ratio. Trainging Set Ratio in setting file : " + ss[1]);
                            }
                            else
                            {
                                Console.WriteLine("   => Don't have command " + s);
                            }
                        }
                    }
                }

                if (Tool.SettingFile.TotalFolderDir == "")
                {
                    Console.WriteLine("  => Setting File Information : ");
                    Console.WriteLine("   => Clk : " + Tool.SettingFile.Clk);
                    Console.WriteLine("   => Threshold(0) : " + Tool.SettingFile.ThresholdZero);
                    Console.WriteLine("   => Threshold(1) : " + Tool.SettingFile.ThresholdOne);
                    Console.WriteLine("   => Simulate Round : " + Tool.SettingFile.SimulateRound);
                    Console.WriteLine("   => Cell Library Dir : " + Tool.SettingFile.CellLibraryDir);
                    Console.WriteLine("   => Verilog File Dir : " + Tool.SettingFile.VerilogFileDir);
                    Console.WriteLine("   => Training Set Ratio : " + Tool.SettingFile.trainingSetRatio + "%");

                    if (Tool.SettingFile.Clk != "" &&
                        Tool.SettingFile.ThresholdZero != 0 &&
                        Tool.SettingFile.ThresholdOne != 0 &&
                        Tool.SettingFile.SimulateRound != 0 &&
                        Tool.SettingFile.CellLibraryDir != "" &&
                        Tool.SettingFile.VerilogFileDir != "")
                    {
                        ProcessCellLibraryFile.ReadingCellLibrary();
                        ProcessVerilogFile.ReadingVerilogFile();
                        ProbabilityMethod.ProcessProbabilityMethod();
                        Console.WriteLine("  => Start Writing Feature");

                        PathFeature.setMinLevelToOutput();
                        PathFeature.setDiversitySimilarity();
                        PathFeature.setTrojanInputGate();
                        PathFeature.FindPath("test");

                        Console.WriteLine("  => End Writing Feature");
                    }
                }
            }

            Console.WriteLine(" Tool End. ");
            Console.ReadKey();
        }
    }

    public static class Tool
    {
        public static SettingFile SettingFile = new SettingFile();
        public static VerilogFile VerilogFile = new VerilogFile();
        public static CellFile CellFile = new CellFile();

    }

    public class SettingFile
    {
        public int SimulateRound = 0;
        public double ThresholdZero = 1;
        public double ThresholdOne = 1;

        public string Clk = "";
        public string TotalFolderDir = "";
        public string VerilogFileDir = "";
        public string CellLibraryDir = "";
        public string MappingFileDir = "";
        public string BenchmarkFileDir = "";
        public int trainingSetRatio = 0;

    }

    public class VerilogFile
    {
        public List<VerilogNet> All = new List<VerilogNet>();

        public List<VerilogNet> Input = new List<VerilogNet>();
        public List<VerilogNet> Internal = new List<VerilogNet>();
        public List<VerilogNet> Output = new List<VerilogNet>();

        public List<string> TrojanGate = new List<string>();
        public List<VerilogGate> Gate = new List<VerilogGate>();
        public List<VerilogGate> Dff = new List<VerilogGate>();
        public List<VerilogGate> TopologySort = new List<VerilogGate>();

        public Clock clk = Clock.LowToLow;
    }

    public class CellFile
    {
        public List<Cell> CellLib = new List<Cell>();
    }

    public class AssignNet
    {
        public string NetOne = "";
        public string NetTwo = "";
    }
}
