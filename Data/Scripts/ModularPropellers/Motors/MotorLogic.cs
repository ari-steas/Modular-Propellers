using System.Collections.Generic;
using System.Linq;
using ModularPropellers.Propellers;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace ModularPropellers.Motors
{
    public class MotorLogic
    {
        public readonly int AssemblyId;
        public List<IMyCubeBlock> Blocks = new List<IMyCubeBlock>();
        public List<RotorLogic> Rotors = new List<RotorLogic>();

        private static readonly Dictionary<string, double> MotorOutputs = new Dictionary<string, double>
        {
            ["LG_ModularMotorElectric"] = 150f * 1000000,
            ["SG_ModularMotorElectric"] = 1.5f * 1000000,
        };

        public double AvailablePower { get; private set; } = 0;

        public MotorLogic(int assemblyId)
        {
            AssemblyId = assemblyId;
        }

        public void UpdateBeforeSimulation()
        {
            double totalDesiredPower = Rotors.Sum(rotor => rotor.DesiredPower);

            if (totalDesiredPower > 0)
                foreach (var rotor in Rotors)
                    rotor.AvailablePower = AvailablePower * (rotor.DesiredPower / totalDesiredPower);
            else
                foreach (var rotor in Rotors)
                    rotor.AvailablePower = AvailablePower / Rotors.Count;

            MyAPIGateway.Utilities.ShowNotification($"{AvailablePower / 1000000:N1}/{totalDesiredPower / 1000000:N1} MW ({Blocks.Count} blocks)", 1000/60);
        }

        public void AddBlock(IMyCubeBlock block)
        {
            Blocks.Add(block);

            if (MotorOutputs.ContainsKey(block.BlockDefinition.SubtypeName))
                AvailablePower += MotorOutputs[block.BlockDefinition.SubtypeName];

            var rotorLogic = block.GameLogic.GetAs<RotorLogic>();
            if (rotorLogic != null)
                Rotors.Add(rotorLogic);
        }

        public void RemoveBlock(IMyCubeBlock block)
        {
            if (!Blocks.Remove(block))
                return;

            if (MotorOutputs.ContainsKey(block.BlockDefinition.SubtypeName))
                AvailablePower -= MotorOutputs[block.BlockDefinition.SubtypeName];

            var rotorLogic = block.GameLogic.GetAs<RotorLogic>();
            if (rotorLogic != null)
                Rotors.Remove(rotorLogic);

            if (Blocks.Count == 0)
                MotorManager.Logic.Remove(AssemblyId);
        }
    }
}
