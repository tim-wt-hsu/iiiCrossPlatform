using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iiiCrossPlatform
{
    public static class ShowMessage
    {
		public static void showLicenseMessage()
		{
			Console.Write(" /");
			for (int i = 0; i < 83; i++)
			{
				Console.Write("-");
			}
			Console.Write("\\\n");
			Console.WriteLine(" |                                                                                   |");
			Console.WriteLine(" | Hardware Trojan Benchmarks Path Feature Selector                                  |");
			Console.WriteLine(" |                                                                                   |");
			Console.WriteLine(" | Developer : 2021 Chi-Wei Chen  <r08921a28@ntu.edu.tw>                             |");
			Console.WriteLine(" |             2021 Wei-Ting Hsu  <r09921a30@ntu.edu.tw>                             |");
			Console.WriteLine(" |             2021 Yu-Ming Chou  <r10921a20@ntu.edu.tw>                             |");
			Console.WriteLine(" |             2021 Yen-Peng Liao <r10921a10@ntu.edu.tw>                             |");
			Console.Write(" \\");
			for (int i = 0; i < 83; i++)
			{
				Console.Write("-");
			}
			Console.Write("/");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(" HtBG 1.0");
		}
	}
}
