using System.Collections.Generic;

namespace ModularPropellers.Propellers
{
    internal static class RotorManager
    {
        public static Dictionary<int, RotorLogic> RotorLogic;


        public static void Init()
        {
            RotorLogic = new Dictionary<int, RotorLogic>();
        }

        public static void Close()
        {
            RotorLogic = null;
        }
    }
}
