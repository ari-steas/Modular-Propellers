using System.Collections.Generic;
using Sandbox.ModAPI;

namespace ModularPropellers.Motors
{
    public static class MotorManager
    {
        public static Dictionary<int, MotorLogic> Logic;

        public static void Init()
        {
            Logic = new Dictionary<int, MotorLogic>();
        }

        public static void UpdateBeforeSimulation()
        {
            foreach (var logic in Logic)
                logic.Value.UpdateBeforeSimulation();
        }

        public static void Close()
        {
            Logic = null;
        }
    }
}
