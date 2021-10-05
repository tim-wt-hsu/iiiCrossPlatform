using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iiiCrossPlatform
{
    public static class Constants
    {
        public static int Infinity = 10000;
    }

    public enum IO
    {
        Input,
        Output
    }

    public enum Clock
    {
        LowToHigh,
        LowToLow,
        HighToHigh,
        HighToLow
    }

    public enum Status
    {
        Module,
        Input,
        Output,
        Wire,
        Assign,
        Gate,
        EndModule
    }

    public enum PinClass
    {
        Input,
        Output,
        Internal
    }

    public enum NetClass
    {
        Input,
        Internal,
        Output,
    }

    public enum TrojanClass
    {
        Trojan,
        Normal,
        TrojanInput,
        TrojanOutput,
        TrojanInputElse,
        TrojanOutputElse,
        TrojanPath,
        Else
    }
}
