using System;
using System.Collections.Generic;
using System.Linq;
using ModularPropellers.Propellers;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace ModularPropellers.Motors
{
    public class MotorAssemblyLogic
    {
        public readonly int AssemblyId;
        public List<IMyCubeBlock> Blocks = new List<IMyCubeBlock>();
        public List<RotorLogic> Rotors = new List<RotorLogic>();
        public List<IMyTerminalBlock> Motors = new List<IMyTerminalBlock>();

        internal MyDefinitionId ElectricityId = MyResourceDistributorComponent.ElectricityId;
        public static readonly Dictionary<string, float> MotorOutputs = new Dictionary<string, float>
        {
            ["LG_ModularMotorElectric"] = 150f * 1000000,
            ["SG_ModularMotorElectric"] = 1.5f * 1000000,
        };

        public float AvailablePower { get; private set; } = 0;

        public MotorAssemblyLogic(int assemblyId)
        {
            AssemblyId = assemblyId;
        }

        public void UpdateBeforeSimulation()
        {
            double totalDesiredPower = Rotors.Sum(rotor => rotor.MaxDesiredPower);

            if (totalDesiredPower > 0)
                foreach (var rotor in Rotors)
                    rotor.AvailablePower = AvailablePower * (rotor.MaxDesiredPower / totalDesiredPower);
            else
                foreach (var rotor in Rotors)
                    rotor.AvailablePower = 0;

            float desiredPowerPct = (float) MathHelper.Clamp(totalDesiredPower / Motors.Sum(motor => MotorOutputs[motor.BlockDefinition.SubtypeName]), 0, 1);
            AvailablePower = 0;
            foreach (var motor in Motors)
            {
                if (motor.ResourceSink == null)
                {
                    AvailablePower += motor.IsWorking ? MotorOutputs[motor.BlockDefinition.SubtypeName] : 0;
                    continue;
                }

                // ResourceSink power is in megawatts, this mod uses watts.
                motor.ResourceSink.SetRequiredInputByType(ElectricityId, MotorOutputs[motor.BlockDefinition.SubtypeName] * desiredPowerPct / 1000000);
                AvailablePower += motor.IsWorking ? motor.ResourceSink.CurrentInputByType(ElectricityId) * 1000000 : 0;
            }

            //MyAPIGateway.Utilities.ShowNotification($"{Blocks[0].CubeGrid.ResourceDistributor.MaxAvailableResourceByType(ElectricityId)}", 1000/60);
            //MyAPIGateway.Utilities.ShowNotification($"{AvailablePower / 1000000:N1}/{totalDesiredPower / 1000000:N1} MW ({Blocks.Count} blocks)", 1000/60);
        }

        public void AddBlock(IMyCubeBlock block)
        {
            Blocks.Add(block);

            if (MotorOutputs.ContainsKey(block.BlockDefinition.SubtypeName))
            {
                Motors.Add((IMyTerminalBlock) block);
            }

            var rotorLogic = block.GameLogic.GetAs<RotorLogic>();
            if (rotorLogic != null)
                Rotors.Add(rotorLogic);
        }

        public void RemoveBlock(IMyCubeBlock block)
        {
            if (!Blocks.Remove(block))
                return;

            if (MotorOutputs.ContainsKey(block.BlockDefinition.SubtypeName))
                Motors.Remove((IMyTerminalBlock) block);

            var rotorLogic = block.GameLogic.GetAs<RotorLogic>();
            if (rotorLogic != null)
                Rotors.Remove(rotorLogic);

            if (Blocks.Count == 0)
                MotorManager.Logic.Remove(AssemblyId);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, "LG_ModularMotorElectric", "SG_ModularMotorElectric")]
    internal class ElectricalMotorLogic : MyGameLogicComponent
    {
        private IMyTerminalBlock Block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Block = (IMyTerminalBlock)Entity;
                
            MyResourceSinkComponent sink = new MyResourceSinkComponent();
            sink.Init(MyStringHash.GetOrCompute("Thrust"), new MyResourceSinkInfo
            {
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                RequiredInputFunc = () => 0,
                MaxRequiredInput = MotorAssemblyLogic.MotorOutputs[((IMyTerminalBlock)Entity).BlockDefinition.SubtypeName],
            }, (MyCubeBlock)Block);
            sink.Update();

            Block.ResourceSink = sink;
            
            var Source = Block.CubeGrid.ResourceDistributor as MyResourceDistributorComponent;
            Source?.AddSink(sink);

            Block.AppendingCustomInfo += (tb, sb) =>
            {
                sb.Insert(0, $"{tb.ResourceSink?.CurrentInputByType(MyResourceDistributorComponent.ElectricityId):N1}/{tb.ResourceSink?.RequiredInputByType(MyResourceDistributorComponent.ElectricityId):N1} MW");
            };
        }
    }
}
