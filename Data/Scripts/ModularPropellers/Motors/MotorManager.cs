using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace ModularPropellers.Motors
{
    public static class MotorManager
    {
        public static Dictionary<int, MotorAssemblyLogic> Logic;

        public static void Init()
        {
            Logic = new Dictionary<int, MotorAssemblyLogic>();
            MyAPIGateway.TerminalControls.CustomControlGetter += AssignDetailedInfoGetter;
        }

        public static void UpdateBeforeSimulation()
        {
            foreach (var logic in Logic)
                logic.Value.UpdateBeforeSimulation();
        }

        public static void Close()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter -= AssignDetailedInfoGetter;
            Logic = null;
        }

        private static void AssignDetailedInfoGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!MotorAssemblyLogic.MotorOutputs.ContainsKey(block.BlockDefinition.SubtypeName))
                return;
            block.RefreshCustomInfo();
            block.SetDetailedInfoDirty();
        }
    }
}
