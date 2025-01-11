using System;
using System.Collections.Generic;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;

namespace ModularPropellers.Propellers
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "ModularPropellerRotorLarge", "ModularPropellerRotorSmall")]
    internal partial class RotorLogic : MyGameLogicComponent, IMyEventProxy
    {
        public const float EfficiencyModifier = 5;
        public static readonly Dictionary<string, RotorInfo> RotorInfos = new Dictionary<string, RotorInfo>
        {
            ["ModularPropellerRotorLarge"] = new RotorInfo
            {
                MaxRpm = 500,
                MaxTorque = float.MaxValue
            },
            ["ModularPropellerRotorSmall"] = new RotorInfo
            {
                MaxRpm = 500,
                MaxTorque = float.MaxValue
            },
        };

        private IMyThrust _block;
        private IMyCubeGrid _grid => _block.CubeGrid;
        private RotorInfo _info;
        private List<HashSet<IMyCubeBlock>> _bladeSets = new List<HashSet<IMyCubeBlock>>();

        public bool IsValid = true;

        internal MySync<float, SyncDirection.BothWays> BladeAngle, MaxRpm;
        internal MySync<float, SyncDirection.FromServer> RPM;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            _block = (IMyThrust) Entity;

            _info = RotorInfos[_block.BlockDefinition.SubtypeName];
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if(_block?.CubeGrid?.Physics == null)
                return;

            BladeAngle.Value = (float) Math.PI/4;
            RPM.Value = 0f;
            MaxRpm.Value = _info.MaxRpm;

            RotorControls.DoOnce();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (!IsValid)
                return;
            MyAPIGateway.Utilities.ShowNotification("Parts: " + _bladeParts.Count + " Sets: " + _bladeSets.Count, 1000/60);

            RPM.Value = MaxRpm * MathHelper.Lerp(RPM.Value/MaxRpm.Value, _block.CurrentThrustPercentage/100f, 0.02f);

            if (float.IsNaN(RPM.Value) || float.IsInfinity(RPM.Value))
                RPM.Value = 0;

            MyAPIGateway.Utilities.ShowNotification($"RPM: {RPM.Value:N0}", 1000/60);
            foreach (var bladePair in _bladeParts)
                bladePair.Value.Update((float) (RPM * Math.PI / 1800), BladeAngle);

            _block.ThrustMultiplier = (float) CalculateThrust(RPM, MasterSession.I.GetAtmosphereDensity(_grid)) / 100f;
            DebugDraw.I.DrawLine0(_block.PositionComp.GetPosition(), _block.PositionComp.GetPosition() + _block.WorldMatrix.Backward * _block.MaxEffectiveThrust / 10000, Color.Blue);
        }

        internal double CalculateThrust(double rpm, double airDensity)
        {
            Vector3D totalLift = Vector3D.Zero;
            float propArea = _block.CubeGrid.GridSize * _block.CubeGrid.GridSize;
            foreach (var part in _bladeParts.Values)
            {
                double propSpeedLocal = (rpm * Math.PI / 30) * Vector3D.Distance(part.PositionComp.GetPosition(), _block.GetPosition());
                Vector3D propSpeedDirection = Vector3D.Rotate(Vector3D.Left, MatrixD.CreateFromAxisAngle(Vector3D.Forward, -BladeAngle));
                var globalVelocity = _grid.LinearVelocity + LocalToWorldRotation(propSpeedDirection * propSpeedLocal, part.PositionComp.WorldMatrixRef);
                double speedSq = globalVelocity.LengthSquared();
                if (speedSq <= 1)
                    continue;

                Vector3D dragNormal = -Vector3D.Normalize(globalVelocity);
                Vector3D liftNormal = LocalToWorldRotation(Vector3D.Up, part.PositionComp.WorldMatrixRef);

                // angle between chord line and airflow
                double angleOfAttack = Math.Asin(Vector3D.Dot(dragNormal, liftNormal));

                // Approximation of NACA 0012 airfoil, adjusted so that maximum lift is given at a 45-degree angle of attack.
                double liftCoefficient = 1.34951 * Math.Sin(2 * angleOfAttack);
                double dragCoefficient = 0.616947838 * angleOfAttack;
                dragCoefficient = dragCoefficient * dragCoefficient * dragCoefficient * dragCoefficient + 0.00608945;

                // induced drag, increases with lift
                double inducedDragCoefficient = liftCoefficient * liftCoefficient / Math.PI;
                dragCoefficient += inducedDragCoefficient;

                double dynamicPressure = 0.5 * speedSq * airDensity * propArea * EfficiencyModifier;

                Vector3D totalForce = (liftNormal * liftCoefficient + dragNormal * dragCoefficient) * dynamicPressure;
                totalLift += totalForce;

                //DebugDraw.I.DrawLine0(part.PositionComp.GetPosition(), part.PositionComp.GetPosition() + totalForce / 10000, Color.Green);
                //DebugDraw.I.DrawLine0(part.PositionComp.GetPosition(), part.PositionComp.GetPosition() + dragNormal, Color.Red);
            }

            totalLift = WorldToLocalRotation(totalLift, _block.WorldMatrix);

            return totalLift.Z < 0 ? 0 : totalLift.Z;
        }

        Vector3D WorldToLocalRotation(Vector3D pos, MatrixD parentMatrix)
        {
            parentMatrix = MatrixD.Invert(parentMatrix);
            return Vector3D.Rotate(pos, parentMatrix);
        }

        Vector3D LocalToWorldRotation(Vector3D pos, MatrixD parentMatrix)
        {
            return Vector3D.Rotate(pos, parentMatrix);
        }

        public struct RotorInfo
        {
            public float MaxRpm;
            public float MaxTorque;
        }
    }
}
