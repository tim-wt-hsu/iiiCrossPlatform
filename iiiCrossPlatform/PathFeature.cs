using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace iiiCrossPlatform
{
    class PathFeature
    {
        static int similarity = 0;
        static int diversity = 0;
        static long pathCount = 0;
        static double filterThresholdOne = Tool.SettingFile.ThresholdOne, filterThresholdZero = Tool.SettingFile.ThresholdZero;
        static int TjPathCount = 0, FrPathCount = 0, OtherTjPathCount = 0, OtherFrPathCount = 0;

        static string fileName = Path.GetFileNameWithoutExtension(Tool.SettingFile.VerilogFileDir);
        static Encoding utf8WithoutBom = new UTF8Encoding(false);
        static StreamWriter pathFile = new StreamWriter(Path.Combine(Tool.SettingFile.BenchmarkFileDir, fileName + "_Path.txt"), false, utf8WithoutBom, 65536);
        static StreamWriter pathTraingFile = new StreamWriter(Path.Combine(Tool.SettingFile.BenchmarkFileDir, "New_"+fileName + "_Path_Feature_TrainingData.txt"), false, utf8WithoutBom, 65536);
        static StreamWriter pathTestFile = new StreamWriter(Path.Combine(Tool.SettingFile.BenchmarkFileDir, "New_"+fileName + "_Path_Feature_TestData.txt"), false, utf8WithoutBom, 65536);
        static StreamWriter otherPathFile = new StreamWriter(Path.Combine(Tool.SettingFile.BenchmarkFileDir, "New_"+fileName + "_OtherPath_Feature_TestData.txt"), false, utf8WithoutBom, 65536);
        static int tailIndexCount = 0; 
        static int tailIndexCountOtherPath = 0; 
        // every feature of each paths are stored in one vector,
        // same indexs of each vector are mapped to features of one path
        // that is, the size of these vector should be the same
        // TODO `throughPathCount` used for gate information might need to be record
        static List<int> viPathGateCount = new List<int>();
        static List<List<string>> vsPathGateName = new List<List<string>>();
        //vector<vector<string>> vsPathGateFootprint;       這行是我自己註解掉的
        //vector<vector<vector<cWire*>>> vpPathGateWirelist;  // number of gates of different paths might be different      這行是我自己註解掉的
        static List<bool> viTjFlag = new List<bool>();
        static List<double> vdMaxArea = new List<double>(),
                             vdMinArea = new List<double>(),
                             vdSumArea = new List<double>(),
                             vdAvgArea = new List<double>(),
                             vdStdArea = new List<double>();
        // max, min, sum convert to double for later normalization
        static List<double> vdMaxFin = new List<double>(),
                            vdMinFin = new List<double>(),
                            vdSumFin = new List<double>(),
                            vdAvgFin = new List<double>(),
                            vdStdFin = new List<double>();
        static List<double> vdMaxFout = new List<double>(),
                            vdMinFout = new List<double>(),
                            vdSumFout = new List<double>(),
                            vdAvgFout = new List<double>(),
                            vdStdFout = new List<double>();
        static List<double> vdMaxSig0Prob = new List<double>(),
                            vdMinSig0Prob = new List<double>(),
                            vdSumSig0Prob = new List<double>(),
                            vdAvgSig0Prob = new List<double>(),
                            vdStdSig0Prob = new List<double>();
        static List<double> vdMaxSig1Prob = new List<double>(),
                            vdMinSig1Prob = new List<double>(),
                            vdSumSig1Prob = new List<double>(),
                            vdAvgSig1Prob = new List<double>(),
                            vdStdSig1Prob = new List<double>();
        static List<double> vdMaxPower = new List<double>(),
                            vdMinPower = new List<double>(),
                            vdSumPower = new List<double>(),
                            vdAvgPower = new List<double>(),
                            vdStdPower = new List<double>();

        // Option 0 for area, 1 for fin, 2 for fout, 3 for signal probability
        public static double StdDeviation(VerilogGate[] vPathArray, double Avg, int n, int Option)
        {
            double dAns = 0;
            double dSum = 0;

            for (int i = 0; i < vPathArray.Length; i++)
            {
                if (i == 0 || i == vPathArray.Length - 1)  // 頭跟尾不列入計算, 所以扣掉頭尾如果是一個gate的話會-nan
                    continue;
                if (Option == 0)
                {
                    dSum += (Convert.ToDouble(vPathArray[i].cell.area) - Avg) * (Convert.ToDouble(vPathArray[i].cell.area) - Avg);
                }
                else if (Option == 1)
                {
                    double fanIn = 0;
                    foreach (var p in vPathArray[i].pinList)
                    {
                        if (p.classification == VerilogPinClass.Input)
                            fanIn++;
                    }

                    dSum += (fanIn - Avg) * (fanIn - Avg);
                }
                else if (Option == 2)
                {
                    double fanOut = 0;
                    foreach (var p in vPathArray[i].pinList)
                    {
                        if (p.classification == VerilogPinClass.Output)
                            fanOut += p.connectNet.ToGate.Count;
                    }
                    dSum += (fanOut - Avg) * (fanOut - Avg);
                }
                else if (Option == 3)
                {
                    double dSig0Probability = -1;
                    foreach (var p in vPathArray[i].pinList)
                    {
                        if (p.classification == VerilogPinClass.Output)
                        {
                            dSig0Probability = 1 - p.connectNet.value.activeProbability; //把gate的第一個output wire的機率當成gate的機率
                            break;
                        }
                    }
                    if (dSig0Probability < 0)
                        Console.WriteLine("GG 發生錯誤 在path的時候機率小於0");
                    dSum += (dSig0Probability - Avg) * (dSig0Probability - Avg);
                }
                else if (Option == 4)
                {
                    double dSig1Probability = -1;
                    foreach (var p in vPathArray[i].pinList)
                    {
                        if (p.classification == VerilogPinClass.Output)
                        {
                            dSig1Probability = p.connectNet.value.activeProbability; //把gate的第一個output wire的機率當成gate的機率
                            break;
                        }
                    }
                    if (dSig1Probability < 0)
                        Console.WriteLine("GG 發生錯誤 在path的時候機率小於0");
                    dSum += (dSig1Probability - Avg) * (dSig1Probability - Avg);
                }
                else if (Option == 5)
                {
                    dSum += (Convert.ToDouble(vPathArray[i].cell.cellLeakagePower) - Avg) * (Convert.ToDouble(vPathArray[i].cell.cellLeakagePower) - Avg);
                }
            }
            dAns = Math.Sqrt(dSum / (n - 1));  // n-1 會造成 -nan
            return dAns;
        }

        public static void writePathToFiles(StreamWriter file, int pathIndex, bool tailIndex, int tailIndexCount)
        {
            string feature = "";
            feature += viPathGateCount[pathIndex] + " ";
            foreach (var name in vsPathGateName[pathIndex])
            {
                feature += (name + " ");
            }
            feature += Convert.ToInt32(viTjFlag[pathIndex]).ToString() + ' ';
            feature += vdMaxArea[pathIndex].ToString() + ' ' + vdMinArea[pathIndex].ToString() + ' ' + vdSumArea[pathIndex].ToString().ToString() + ' '
                + vdAvgArea[pathIndex].ToString() + ' ' + vdStdArea[pathIndex].ToString() + ' '
                + vdMaxFin[pathIndex].ToString() + ' ' + vdMinFin[pathIndex].ToString() + ' ' + vdSumFin[pathIndex].ToString() + ' '
                + vdAvgFin[pathIndex].ToString() + ' ' + vdStdFin[pathIndex].ToString() + ' '
                + vdMaxFout[pathIndex].ToString() + ' ' + vdMinFout[pathIndex].ToString() + ' ' + vdSumFout[pathIndex].ToString() + ' '
                + vdAvgFout[pathIndex].ToString() + ' ' + vdStdFout[pathIndex].ToString() + ' '
                + vdMaxSig0Prob[pathIndex].ToString() + ' ' + vdMinSig0Prob[pathIndex].ToString() + ' ' + vdSumSig0Prob[pathIndex].ToString() + ' '
                + vdAvgSig0Prob[pathIndex].ToString() + ' ' + vdStdSig0Prob[pathIndex].ToString() + ' '
                + vdMaxSig1Prob[pathIndex].ToString() + ' ' + vdMinSig1Prob[pathIndex].ToString() + ' ' + vdSumSig1Prob[pathIndex].ToString() + ' '
                + vdAvgSig1Prob[pathIndex].ToString() + ' ' + vdStdSig1Prob[pathIndex].ToString() + ' '
                + vdMaxPower[pathIndex].ToString() + ' ' + vdMinPower[pathIndex].ToString() + ' ' + vdSumPower[pathIndex].ToString() + ' '
                + vdAvgPower[pathIndex].ToString() + ' ' + vdStdPower[pathIndex].ToString();
            if (tailIndex == false)
                file.WriteLine(feature);
            else
                file.WriteLine(feature + ' ' + tailIndexCount.ToString());

        }
        public static void writeFiles(ref int tailIndexCount)
        {
            //string strPathFile = "", strOtherPathFile = "";
            
            if (vdAvgArea.Count != vdMinSig1Prob.Count)
                Console.WriteLine("GG 為啥pathCount 數目不等於path數目(PathFeature)");
            // go through every path record
            //int tailIndexCount = 0;
            Random random = new Random();
            for (int i = 0; i < vdAvgArea.Count; ++i)
            {
                // TODO automatically calculate the threshold instead of given manually
                // if (vdMinSig1Prob[i] < allSig1Prob[allSig1Prob.size() * 0.2]
                //         || vdMinSig0Prob[i] < allSig0Prob[allSig0Prob.size() * 0.2]) {
                if (vdMinSig1Prob[i] < filterThresholdOne ||
                        vdMinSig0Prob[i] < filterThresholdZero)
                {
                    writePathToFiles(pathFile, i, false, tailIndexCount);

                    if (random.Next(1, 101) <= Tool.SettingFile.trainingSetRatio) //根據交大是用50%機率
                        writePathToFiles(pathTraingFile, i, true, tailIndexCount++);
                    else
                        writePathToFiles(pathTestFile, i, true, tailIndexCount++);

                    if (viTjFlag[i])
                        TjPathCount++;
                    else
                        FrPathCount++;
                }
                else
                {  // other paths
                    writePathToFiles(otherPathFile, i, true, tailIndexCountOtherPath++);
                    if (viTjFlag[i])
                        OtherTjPathCount++;
                    else
                        OtherFrPathCount++;
                }
            }
            



        }


        public static void addPath(Stack<VerilogGate> vPathStack,
        int iPathGateCount,
        double dMaxArea,
        double dMinArea,
        double dSumArea,
        int iMaxFin,
        int iMinFin,
        int iSumFin,
        int iMaxFout,
        int iMinFout,
        int iSumFout,
        double dMaxSig0Prob,
        double dMinSig0Prob,
        double dSumSig0Prob,
        double dMaxSig1Prob,
        double dMinSig1Prob,
        double dSumSig1Prob,
        double dMaxPower,
        double dMinPower,
        double dSumPower)
        {
            if (iPathGateCount <= 1)
                return;
            pathCount++;
            iPathGateCount = vPathStack.Count - 2;  // FIXME not sure here

            VerilogGate[] vPathArray = vPathStack.ToArray();//這段我自己加的，因為寫到這才發現stack要頭尾都可以存取才能算std
            Array.Reverse(vPathArray);

            // avg, std are not yet calculated
            double dAvgArea, dStdArea, dAvgFin, dStdFin, dAvgFout, dStdFout,
                dAvgSig0Prob, dStdSig0Prob, dAvgSig1Prob, dStdSig1Prob,
                dAvgPower, dStdPower;

            // calculating avg of features
            dAvgArea = (double)dSumArea / iPathGateCount;
            dAvgFin = (double)iSumFin / iPathGateCount;
            dAvgFout = (double)iSumFout / iPathGateCount;
            dAvgSig0Prob = (double)dSumSig0Prob / iPathGateCount;
            dAvgSig1Prob = (double)dSumSig1Prob / iPathGateCount;
            dAvgPower = (double)dSumPower / iPathGateCount;

            // calculating standard deviation of features
            dStdArea = StdDeviation(vPathArray, dAvgArea, iPathGateCount, 0);
            dStdFin = StdDeviation(vPathArray, dAvgFin, iPathGateCount, 1);
            dStdFout = StdDeviation(vPathArray, dAvgFout, iPathGateCount, 2);
            dStdSig0Prob = StdDeviation(vPathArray, dAvgSig0Prob, iPathGateCount, 3);
            dStdSig1Prob = StdDeviation(vPathArray, dAvgSig1Prob, iPathGateCount, 4);
            dStdPower = StdDeviation(vPathArray, dAvgPower, iPathGateCount, 5);

            // record the path
            viPathGateCount.Add(iPathGateCount);

            bool TjFlag = false;
            List<string> singlePathGateName = new List<string>();
            //vector<string> singlePathGateFootprint;           這行我自己註解掉
            //vector<vector<cWire*>> singlePathGateWirelist;    這行我自己註解掉

            for (int i = 0; i < vPathArray.Length; ++i)
            {
                if (TjFlag == false && vPathArray[i].TrojanInputGate == true)
                    /*(vPathArray[i]->sName.find("Trigger") != string::npos
                     || vPathArray[i]->sName.find("Enable") != string::npos
                     && vPathStack[i]->sName.find("Payload") == string::npos))
                    */
                    TjFlag = true;

                // record path gate name, footprint, wire list
                singlePathGateName.Add(vPathArray[i].name);
                //singlePathGateFootprint.push_back(vPathStack[i]->sFootprint);  這行我自己註解掉
                //singlePathGateWirelist.push_back(vPathStack[i]->vWire_list);   這行我自己註解掉
            }
            vsPathGateName.Add(singlePathGateName);
            //vsPathGateFootprint.push_back(singlePathGateFootprint);
            //vpPathGateWirelist.push_back(singlePathGateWirelist); = new List<double>
            viTjFlag.Add(TjFlag);

            vdMaxArea.Add(dMaxArea);
            vdMinArea.Add(dMinArea);
            vdSumArea.Add(dSumArea);
            vdAvgArea.Add(dAvgArea);
            vdStdArea.Add(dStdArea);

            vdMaxFin.Add((double)iMaxFin);
            vdMinFin.Add((double)iMinFin);
            vdSumFin.Add((double)iSumFin);
            vdAvgFin.Add(dAvgFin);
            vdStdFin.Add(dStdFin);

            vdMaxFout.Add((double)iMaxFout);
            vdMinFout.Add((double)iMinFout);
            vdSumFout.Add((double)iSumFout);
            vdAvgFout.Add(dAvgFout);
            vdStdFout.Add(dStdFout);

            vdMaxSig1Prob.Add(dMaxSig1Prob);
            vdMinSig1Prob.Add(dMinSig1Prob);
            vdSumSig1Prob.Add(dSumSig1Prob);
            vdAvgSig1Prob.Add(dAvgSig1Prob);
            vdStdSig1Prob.Add(dStdSig1Prob);

            vdMaxSig0Prob.Add(dMaxSig0Prob);
            vdMinSig0Prob.Add(dMinSig0Prob);
            vdSumSig0Prob.Add(dSumSig0Prob);
            vdAvgSig0Prob.Add(dAvgSig0Prob);
            vdStdSig0Prob.Add(dStdSig0Prob);

            vdMaxPower.Add(dMaxPower);
            vdMinPower.Add(dMinPower);
            vdSumPower.Add(dSumPower);
            vdAvgPower.Add(dAvgPower);
            vdStdPower.Add(dStdPower);


            if(vdAvgArea.Count==100000)
            {
                writeFiles(ref tailIndexCount);

                viPathGateCount.Clear();
                //foreach (var list in vsPathGateName)
                    //list.Clear();
                vsPathGateName.Clear();
                //vector<vector<string>> vsPathGateFootprint;       這行是我自己註解掉的
                //vector<vector<vector<cWire*>>> vpPathGateWirelist;  // number of gates of different paths might be different      這行是我自己註解掉的
                viTjFlag.Clear();
                vdMaxArea.Clear();
                vdMinArea.Clear();
                vdSumArea.Clear();
                vdAvgArea.Clear();
                vdStdArea.Clear();
                // max, min, sum convert to double for later normalization
                vdMaxFin.Clear();
                vdMinFin.Clear();
                vdSumFin.Clear();
                vdAvgFin.Clear();
                vdStdFin.Clear();
                vdMaxFout.Clear();
                vdMinFout.Clear();
                vdSumFout.Clear();
                vdAvgFout.Clear();
                vdStdFout.Clear();
                vdMaxSig0Prob.Clear();
                vdMinSig0Prob.Clear();
                vdSumSig0Prob.Clear();
                vdAvgSig0Prob.Clear();
                vdStdSig0Prob.Clear();
                vdMaxSig1Prob.Clear();
                vdMinSig1Prob.Clear();
                vdSumSig1Prob.Clear();
                vdAvgSig1Prob.Clear();
                vdStdSig1Prob.Clear();
                vdMaxPower.Clear();
                vdMinPower.Clear();
                vdSumPower.Clear();
                vdAvgPower.Clear();
                vdStdPower.Clear();
            }
        }

        public static void setTrojanInputGate()
        {
            foreach (var g in Tool.VerilogFile.Gate)
                if (Tool.VerilogFile.TrojanGate.Contains(g.name))
                    g.TrojanInputGate = true;
        }
        public static void CalPathFeature(ref int iPathGateCount,
        double dCurArea,
        ref double dMaxArea,
        ref double dMinArea,
        ref double dSumArea,
        int iCurFin,
        ref int iMaxFin,
        ref int iMinFin,
        ref int iSumFin,
        int iCurFout,
        ref int iMaxFout,
        ref int iMinFout,
        ref int iSumFout,
        double dCurSig0Prob,
        ref double dMaxSig0Prob,
        ref double dMinSig0Prob,
        ref double dSumSig0Prob,
        double dCurSig1Prob,
        ref double dMaxSig1Prob,
        ref double dMinSig1Prob,
        ref double dSumSig1Prob,
        double dCurPower,
        ref double dMaxPower,
        ref double dMinPower,
        ref double dSumPower)
        {
            // get features for each path
            // path count
            iPathGateCount += 1;

            // Power
            dSumPower += dCurPower;
            if (dCurPower > dMaxPower)
            {
                dMaxPower = dCurPower;
            }
            if (dCurPower < dMinPower)
            {
                dMinPower = dCurPower;
            }

            // Area
            dSumArea += dCurArea;
            if (dCurArea > dMaxArea)
            {
                dMaxArea = dCurArea;
            }
            if (dCurArea < dMinArea)
            {
                dMinArea = dCurArea;
            }

            // Fanin
            iSumFin += iCurFin;
            if (iCurFin > iMaxFin)
            {
                iMaxFin = iCurFin;
            }
            if (iCurFin < iMinFin)
            {
                iMinFin = iCurFin;
            }

            // Fanout
            iSumFout += iCurFout;
            if (iCurFout > iMaxFout)
            {
                iMaxFout = iCurFout;
            }
            if (iCurFout < iMinFout)
            {
                iMinFout = iCurFout;
            }

            // Signal 0 probability
            dSumSig0Prob += dCurSig0Prob;
            if (dCurSig0Prob > dMaxSig0Prob)
            {
                dMaxSig0Prob = dCurSig0Prob;
            }
            if (dCurSig0Prob < dMinSig0Prob)
            {
                dMinSig0Prob = dCurSig0Prob;
            }

            // Signal 1 probability
            dSumSig1Prob += dCurSig1Prob;
            if (dCurSig1Prob > dMaxSig1Prob)
            {
                dMaxSig1Prob = dCurSig1Prob;
            }
            if (dCurSig1Prob < dMinSig1Prob)
            {
                dMinSig1Prob = dCurSig1Prob;
            }
        }
        public static void RecurrGetPath(Stack<VerilogGate> vPathStack,    //vector<cGate*> vPathStack,
        VerilogGate CurGptr,     //cGate* CurGptr,
        VerilogNet Wptr,      //cWire* Wptr,
        int iPathGateCount,
        double dMaxArea,
        double dMinArea,
        double dSumArea,
        int iMaxFin,
        int iMinFin,
        int iSumFin,
        int iMaxFout,
        int iMinFout,
        int iSumFout,
        double dMaxSig0Prob,
        double dMinSig0Prob,
        double dSumSig0Prob,
        double dMaxSig1Prob,
        double dMinSig1Prob,
        double dSumSig1Prob,
        double dMaxPower,
        double dMinPower,
        double dSumPower)
        {
            if (CurGptr != null)
            {
                if (CurGptr.findPathVisited == false)
                    CurGptr.findPathVisited = true;
                // else if (vPathStack.size() <= 1 && CurGptr->findPathVisited) 這行被交大註解掉不知道為啥
                else if (CurGptr.findPathVisited && vPathStack.Count <= diversity && CurGptr.minLevelToOutput >= similarity)
                {
                    return;  // stop traversing
                }

                if (CurGptr.classification != VerilogGateClass.Dff)
                {
                    vPathStack.Push(CurGptr);  // since FF has been pushed in findPath()
                }
                /*
                if (CurGptr->sFootprint != "SDFF" && CurGptr->sFootprint != "DFF" &&
                CurGptr->sFootprint != "DFFN" && CurGptr->sFootprint != "DFFAR" &&
                CurGptr->sFootprint != "SDFFAR")
                {
                    vPathStack.push_back(CurGptr);  // since FF has been pushed in findPath()
                }
                */
                int fanIn = 0, fanOut = 0;
                foreach (var p in CurGptr.pinList)
                {
                    if (p.classification == VerilogPinClass.Input)
                        fanIn++;
                    else //p.InOutput == IO.Output
                    {
                        if (p.connectNet != null)
                            fanOut += p.connectNet.ToGate.Count;
                        else
                            Console.WriteLine("test");
                    }
                }

                CalPathFeature(ref iPathGateCount, Convert.ToDouble(CurGptr.cell.area), ref dMaxArea, ref dMinArea,  //CurGptr->dArea
                ref dSumArea, fanIn, ref iMaxFin, ref iMinFin, ref iSumFin,     //CurGptr->getFin()
                fanOut, ref iMaxFout, ref iMinFout, ref iSumFout,               //CurGptr->getFout()
                1 - Wptr.value.activeProbability, ref dMaxSig0Prob, ref dMinSig0Prob, ref dSumSig0Prob,//CurGptr->dSig0Probability
                Wptr.value.activeProbability, ref dMaxSig1Prob, ref dMinSig1Prob,                  //CurGptr->dSig1Probability
                ref dSumSig1Prob, Convert.ToDouble(CurGptr.cell.cellLeakagePower), ref dMaxPower, ref dMinPower, ref dSumPower);//CurGptr->dPower

                VerilogGate NextTempGptr;  //cGate* NextTempGptr;
                if (Wptr.ToGate.Count == 1)   //Wptr->vCnnctGate_list.size() == 1 這邊有點奇怪，交大寫成只有一個output pin 才要檢查有沒有loop
                {
                    // break recursion
                    NextTempGptr = Wptr.ToGate[0];        //NextTempGptr = Wptr->vCnnctGate_list[0];
                    /*
                    if (NextTempGptr->sFootprint != "SDFF" &&
                        NextTempGptr->sFootprint != "DFF" &&
                        NextTempGptr->sFootprint != "DFFN" &&
                        NextTempGptr->sFootprint != "DFFAR" &&
                        NextTempGptr->sFootprint != "SDFFAR")
                    */
                    if (NextTempGptr.classification != VerilogGateClass.Dff)
                    {
                        // handling cycle, if the wire only connects to 1 gate
                        // and the gate exists in PathStack, its a cycle

                        int check = 0;
                        foreach (var g in vPathStack)//for (int i = 0; i < vPathStack.Count; i++)
                        {
                            if (check > 1)
                                Console.WriteLine("GG了Stack這邊有出錯(PathFeature)");

                            if (NextTempGptr.name == g.name)
                            {
                                check++;    //我自己加的
                                // gate repeats on the path
                                vPathStack.Push(Wptr.ToGate[0]);

                                addPath(vPathStack, iPathGateCount,
                                        dMaxArea, dMinArea, dSumArea, iMaxFin,
                                        iMinFin, iSumFin, iMaxFout, iMinFout,
                                        iSumFout, dMaxSig0Prob, dMinSig0Prob,
                                        dSumSig0Prob, dMaxSig1Prob, dMinSig1Prob,
                                        dSumSig1Prob, dMaxPower, dMinPower,
                                        dSumPower);

                                vPathStack.Pop();    // pop重複的gate
                                vPathStack.Pop();    // pop最後一個gate
                                return;
                            }
                        }
                    }
                }

            }
            if (Wptr.classification == VerilogNetClass.Output && Wptr.ToGate.Count == 0)
            {  // wire is PO, 沒有接到其他的gate
                VerilogGate tempNode = new VerilogGate();//cGate* tempNode = new cGate;                //為了後面的寫入path檔案好處理,把wire型態轉成gate型態
                tempNode.name = Wptr.name;//Primary output          //tempNode->sName = Wptr->sName;
                tempNode.TrojanInputGate = false;                            //tempNode->sFootprint = "PO";
                vPathStack.Push(tempNode);                                  //vPathStack.push_back(tempNode);  // push wire(PO)進去

                addPath(vPathStack, iPathGateCount, dMaxArea,
                        dMinArea, dSumArea, iMaxFin, iMinFin, iSumFin, iMaxFout,
                        iMinFout, iSumFout, dMaxSig0Prob, dMinSig0Prob,
                        dSumSig0Prob, dMaxSig1Prob, dMinSig1Prob, dSumSig1Prob,
                        dMaxPower, dMinPower, dSumPower);

                vPathStack.Pop();  // pop這個wire(PO)
                vPathStack.Pop();  // pop最後一個gate
                return;
            }
            else
            {
                if (Wptr.classification == VerilogNetClass.Output)        // wire is PO, 但還有接到其他的gate
                {
                    VerilogGate tempNode = new VerilogGate();//cGate* tempNode = new cGate;                //為了後面的寫入path檔案好處理,把wire型態轉成gate型態
                    tempNode.name = Wptr.name;   //Primary Output       //tempNode->sName = Wptr->sName;
                    tempNode.TrojanInputGate = false;                                                             //tempNode->sFootprint = "PO";
                    vPathStack.Push(tempNode);                                  //vPathStack.push_back(tempNode);  // push wire(PO)進去

                    addPath(vPathStack, iPathGateCount, dMaxArea,
                            dMinArea, dSumArea, iMaxFin, iMinFin, iSumFin, iMaxFout,
                            iMinFout, iSumFout, dMaxSig0Prob, dMinSig0Prob,
                            dSumSig0Prob, dMaxSig1Prob, dMinSig1Prob, dSumSig1Prob,
                            dMaxPower, dMinPower, dSumPower);       //先處理接到PO的部份

                    vPathStack.Pop();  //  pop wire(PO)
                }
                /*
                for (int i = 0; i < Wptr->vCnnctGate_list.size(); i++)
                 //再處理接到其他的gate
                    cGate* NextGptr = Wptr->vCnnctGate_list[i];  //所有wire可接到的gate, 下一個gate
                */
                foreach (var NextGptr in Wptr.ToGate)
                {  //再處理接到其他的gate
                    if (NextGptr.classification == VerilogGateClass.Dff)
                    {  // PI/FF to FF
                        vPathStack.Push(NextGptr);  //如果下一個gate是FF, 也是一條PATH, terminate

                        addPath(vPathStack, iPathGateCount, dMaxArea,
                                dMinArea, dSumArea, iMaxFin, iMinFin, iSumFin,
                                iMaxFout, iMinFout, iSumFout, dMaxSig0Prob,
                                dMinSig0Prob, dSumSig0Prob, dMaxSig1Prob,
                                dMinSig1Prob, dSumSig1Prob, dMaxPower, dMinPower,
                                dSumPower);  // feature 不包含FF

                        vPathStack.Pop();    // pop FF
                    }
                    else //這邊交大分成一個output跟兩個output(HADD) 我把它整合成一個
                    {
                        foreach (var outputPinOfNextGptr in NextGptr.pinList)
                        {
                            if (outputPinOfNextGptr.classification == VerilogPinClass.Output)
                                RecurrGetPath(vPathStack, NextGptr,
                                        outputPinOfNextGptr.connectNet, iPathGateCount, dMaxArea,
                                        dMinArea, dSumArea, iMaxFin, iMinFin, iSumFin, iMaxFout,
                                        iMinFout, iSumFout, dMaxSig0Prob, dMinSig0Prob,
                                        dSumSig0Prob, dMaxSig1Prob, dMinSig1Prob, dSumSig1Prob,
                                        dMaxPower, dMinPower, dSumPower);
                        }
                    }
                }
                vPathStack.Pop();
                return;

            }
        }


        public static void findGateLevel(VerilogGate g, int level)
        {
            if (g.classification == VerilogGateClass.Dff)
                return;

            //g->maxLevelToOutput = max(level, g->maxLevelToOutput);
            g.minLevelToOutput = Math.Min(level, g.minLevelToOutput);

            /*這邊交大應該只是要把gate的input wire連到的gate傳給findGateLevel
            int outputNum = 1;
            if (g->sFootprint.find("HADD") != string::npos
                    || g->sFootprint.find("DFF") != string::npos)
                outputNum = 2;
            */
            foreach (var p in g.pinList)
            {
                if (p.classification == VerilogPinClass.Input)
                {
                    foreach (var fromGate in p.connectNet.FromGate)
                        findGateLevel(fromGate, level + 1);
                }
            }
        }
        public static void setMinLevelToOutput()
        {
            foreach (var w in Tool.VerilogFile.Output)
            {
                Console.WriteLine(w.name);
                foreach (var g in w.FromGate)
                    findGateLevel(g, 1);
                if (w.FromWire.Count != 0)
                {
                    foreach (var wFrom in w.FromWire)
                        foreach (var g in wFrom.FromGate)
                            findGateLevel(g, 1);
                }
            }
            foreach (var g in Tool.VerilogFile.Gate)
            {
                if (g.classification == VerilogGateClass.Dff)
                {
                    g.minLevelToOutput = 0;//這行我自己加的
                    foreach (var p in g.pinList)
                    {
                        if (p.classification == VerilogPinClass.Input)
                        {
                            //for (auto proceeding_gate: g->vWire_list[i]->vOutputNetGate_list)
                            foreach (var fromGate in p.connectNet.FromGate)
                                findGateLevel(fromGate, 1);
                        }
                    }
                }
            }


        }

        public static void setDiversitySimilarity()
        {
            int maxMinLevelToOutput = -1;
            foreach (var g in Tool.VerilogFile.Gate)
            {
                if (g.minLevelToOutput == int.MaxValue)
                {
                    Console.WriteLine("Waring:  " + g.name + "   not reaching output(這邊不一定是有錯)");
                    continue;
                }
                maxMinLevelToOutput = Math.Max(maxMinLevelToOutput, g.minLevelToOutput);
            }
            similarity = Convert.ToInt32(maxMinLevelToOutput * 0.8);
            diversity = Convert.ToInt32(maxMinLevelToOutput * 0.4);
            Console.WriteLine("maxMinLevelToOutput: " + maxMinLevelToOutput.ToString());
            Console.WriteLine("similarity: " + similarity.ToString());
            Console.WriteLine("diversity: " + diversity.ToString());

        }
        public static void FindPath(string OutFileName)
        {
            /*
            1. from PI to FF
            2. from PI to PO
            3. from FF to FF
            4. from FF to PO
            */
            int iPathGateCount = 0;
            double dMaxArea = 0, dMinArea = 100, dSumArea = 0;
            int iMaxFin = 0, iMinFin = 100, iSumFin = 0;
            int iMaxFout = 0, iMinFout = 100, iSumFout = 0;
            double dMaxSig0Prob = 0, dMinSig0Prob = 1, dSumSig0Prob = 0;
            double dMaxSig1Prob = 0, dMinSig1Prob = 1, dSumSig1Prob = 0;
            double dMaxPower = 0, dMinPower = 1000000, dSumPower = 0;



            // PI to FF, PI to PO
            foreach (var w in Tool.VerilogFile.All)
                if (w.FromWire.Count != 0)
                    Console.WriteLine("yes");
            foreach (var w in Tool.VerilogFile.Input)
            {

                if (w.name == Tool.SettingFile.Clk || w.name == "1'b1" || w.name == "1'b0" /*|| vModule_list[i].vWire_list[j].sName == "test_se"*/)
                    continue;  // Pass net: CK or test_se

                else
                {
                    iPathGateCount = 0;
                    dMaxArea = 0; dMinArea = 100; dSumArea = 0;
                    iMaxFin = 0; iMinFin = 100; iSumFin = 0;
                    iMaxFout = 0; iMinFout = 100; iSumFout = 0;
                    dMaxSig0Prob = 0; dMinSig0Prob = 1; dSumSig0Prob = 0;
                    dMaxSig1Prob = 0; dMinSig1Prob = 1; dSumSig1Prob = 0;
                    dMaxPower = 0; dMinPower = 1000000; dSumPower = 0;
                    Stack<VerilogGate> vPathStack = new Stack<VerilogGate>(); //vector<cGate*> vPathStack;
                    vPathStack.Clear();

                    VerilogGate GNode = new VerilogGate(); //cGate* GNode = new cGate;
                    GNode.name = w.name; //+ "(Primary input)";//GNode->sName = vModule_list[i]->vWire_list[j]->sName;
                    //GNode->sFootprint = "PI";
                    GNode.TrojanInputGate = false;
                    vPathStack.Push(GNode);//vPathStack.push_back(GNode);
                    RecurrGetPath(vPathStack, null,  //NULL,
                            w, iPathGateCount, //vModule_list[i]->vWire_list[j]
                            dMaxArea, dMinArea, dSumArea, iMaxFin, iMinFin,
                            iSumFin, iMaxFout, iMinFout, iSumFout,
                            dMaxSig0Prob, dMinSig0Prob, dSumSig0Prob,
                            dMaxSig1Prob, dMinSig1Prob, dSumSig1Prob,
                            dMaxPower, dMinPower, dSumPower);
                    //vPathStack.Pop(); 這邊應該是交大寫錯了 不用這行 因為這時候stack已經是empty了
                    if (vPathStack.Count != 0)
                        Console.WriteLine("shit");
                }
            }

            // FF to FF, FF to PO
            /*
            cGate* Gptr = vModule_list[i]->Gate_list_head->pNext;
            while (Gptr != NULL)
            {
                if (Gptr->sFootprint == "SDFF" || Gptr->sFootprint == "DFF" ||
                    Gptr->sFootprint == "DFFN" || Gptr->sFootprint == "DFFAR" ||
                    Gptr->sFootprint == "SDFFAR")*/
            foreach (var g in Tool.VerilogFile.Gate)
            {
                if (g.classification == VerilogGateClass.Dff)
                {
                    iPathGateCount = 0;
                    dMaxArea = 0; dMinArea = 100; dSumArea = 0;
                    iMaxFin = 0; iMinFin = 100; iSumFin = 0;
                    iMaxFout = 0; iMinFout = 100; iSumFout = 0;
                    dMaxSig0Prob = 0; dMinSig0Prob = 1; dSumSig0Prob = 0;
                    dMaxSig1Prob = 0; dMinSig1Prob = 1; dSumSig1Prob = 0;
                    dMaxPower = 0; dMinPower = 1000000; dSumPower = 0;
                    Stack<VerilogGate> vPathStack = new Stack<VerilogGate>(); //vector<cGate*> vPathStack;
                    vPathStack.Clear();

                    foreach (var p in g.pinList)
                    {
                        if (p.classification == VerilogPinClass.Output && p.connectNet != null)
                        {
                            vPathStack.Push(g); //vPathStack.push_back(Gptr);
                            RecurrGetPath(vPathStack, null,  //NULL,
                            p.connectNet, iPathGateCount, //Gptr->vWire_list[Gptr->vWire_list.size() - 1]
                            dMaxArea, dMinArea, dSumArea, iMaxFin, iMinFin,
                            iSumFin, iMaxFout, iMinFout, iSumFout,
                            dMaxSig0Prob, dMinSig0Prob, dSumSig0Prob,
                            dMaxSig1Prob, dMinSig1Prob, dSumSig1Prob,
                            dMaxPower, dMinPower, dSumPower);
                            //vPathStack.Pop(); 這邊應該是交大寫錯了 不用這行 因為這時候stack已經是empty了
                            if (vPathStack.Count != 0)
                                Console.WriteLine("shit");
                        }
                    }
                }
                /*
                if (Gptr->vWire_list[Gptr->vWire_list.size() - 1])
                {  // for QN
                    vPathStack.push_back(Gptr);
                    RecurrGetPath(vPathStack, NULL,
                                  Gptr->vWire_list[Gptr->vWire_list.size() - 1],
                                  iPathGateCount, dMaxArea, dMinArea, dSumArea,
                                  iMaxFin, iMinFin, iSumFin, iMaxFout, iMinFout,
                                  iSumFout, dMaxSig0Prob, dMinSig0Prob,
                                  dSumSig0Prob, dMaxSig1Prob, dMinSig1Prob,
                                  dSumSig1Prob, dMaxPower, dMinPower,
                                  dSumPower);
                    vPathStack.pop_back();
                }
                if (Gptr->vWire_list[Gptr->vWire_list.size() - 2])
                {  // for Q
                    vPathStack.push_back(Gptr);
                    RecurrGetPath(vPathStack, NULL,
                                  Gptr->vWire_list[Gptr->vWire_list.size() - 2],
                                  iPathGateCount, dMaxArea, dMinArea, dSumArea,
                                  iMaxFin, iMinFin, iSumFin, iMaxFout, iMinFout,
                                  iSumFout, dMaxSig0Prob, dMinSig0Prob,
                                  dSumSig0Prob, dMaxSig1Prob, dMinSig1Prob,
                                  dSumSig1Prob, dMaxPower, dMinPower,
                                  dSumPower);
                    vPathStack.pop_back();
                }
            }
            Gptr = Gptr->pNext;
            */
            }
            // write recorded path features to files
            //writeFiles();
            writeFiles(ref tailIndexCount);
            pathFile.Close();
            otherPathFile.Close();
            pathTraingFile.Close();
            pathTestFile.Close();
            Console.WriteLine(OutFileName);
        }
    }
}
