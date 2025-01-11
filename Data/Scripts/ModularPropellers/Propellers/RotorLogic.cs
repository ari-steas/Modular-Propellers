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
        /// <summary>
        /// Lift and drag are multiplied by this number.
        /// </summary>
        public const float LiftModifier = 3f;
        /// <summary>
        /// Torque requirement is multiplied by this number.
        /// </summary>
        public const float TorqueModifier = 1 / 3f;


        public static readonly Dictionary<string, RotorInfo> RotorInfos = new Dictionary<string, RotorInfo>
        {
            ["ModularPropellerRotorLarge"] = new RotorInfo
            {
                MaxRpm = 500,
                MaxAngle = (float) MathHelper.ToRadians(15.5),
                MaxTorque = float.MaxValue
            },
            ["ModularPropellerRotorSmall"] = new RotorInfo
            {
                MaxRpm = 500,
                MaxAngle = (float) MathHelper.ToRadians(15.5),
                MaxTorque = float.MaxValue
            },
        };

        private IMyThrust _block;
        private IMyCubeGrid _grid => _block.CubeGrid;
        public RotorInfo Info;
        private List<HashSet<IMyCubeBlock>> _bladeSets = new List<HashSet<IMyCubeBlock>>();

        public bool IsValid = true;

        internal MySync<float, SyncDirection.BothWays> BladeAngle, MaxRpm;
        internal MySync<float, SyncDirection.FromServer> RPM;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            _block = (IMyThrust) Entity;

            Info = RotorInfos[_block.BlockDefinition.SubtypeName];
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if(_block?.CubeGrid?.Physics == null)
                return;

            BladeAngle.Value = Info.MaxAngle;
            RPM.Value = 0f;
            MaxRpm.Value = Info.MaxRpm;

            RotorControls.DoOnce();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (!IsValid)
                return;
            double availablePower = 2.5 * 1000000 * _block.CurrentThrustPercentage;

            //MyAPIGateway.Utilities.ShowNotification("Parts: " + _bladeParts.Count + " Sets: " + _bladeSets.Count, 1000/60);

            //if (MyAPIGateway.Session.IsServer)
            //    RPM.Value = MaxRpm * MathHelper.Lerp(RPM.Value/MaxRpm.Value, _block.CurrentThrustPercentage/100f, 0.02f);
            //if (RPM.Value == 0)
            //    return;

            if (float.IsNaN(RPM.Value) || float.IsInfinity(RPM.Value))
                RPM.Value = 0;

            foreach (var bladePair in _bladeParts)
                bladePair.Value.Update((float) (RPM * Math.PI / 1800), BladeAngle);

            double torqueNeeded; // Newton-Meters
            // If the thrust multiplier hits zero, it sometimes breaks.
            _block.ThrustMultiplier = MathHelper.Clamp((float) CalculateThrust(RPM, MasterSession.I.GetAtmosphereDensity(_grid), out torqueNeeded) / 100f, 1, float.MaxValue);
            double powerNeeded = torqueNeeded * 2 * Math.PI * RPM / 60; // Watts
            double netPower = availablePower - powerNeeded;

            if (MyAPIGateway.Session.IsServer)
            {
                var newRpm = RPM.Value + (float) ((60 * netPower) / (torqueNeeded * 2 * Math.PI)) / 60f;
                if (newRpm < 0 || float.IsNaN(newRpm))
                    newRpm = 0;
                if (newRpm > MaxRpm.Value)
                    newRpm = MaxRpm.Value;
                RPM.Value = newRpm;
            }

            if (!_block.IsWorking)
                return;

            MyAPIGateway.Utilities.ShowNotification($"RPM: {RPM.Value:N0}", 1000/60);
            MyAPIGateway.Utilities.ShowNotification($"Power: {powerNeeded/1000000:F1}MW ({100*powerNeeded/availablePower:N0}%)", 1000/60);
            //DebugDraw.I.DrawLine0(_block.PositionComp.GetPosition(), _block.PositionComp.GetPosition() + _block.WorldMatrix.Backward * _block.MaxEffectiveThrust / 10000, Color.Blue);
        }

        internal double CalculateThrust(double rpm, double airDensity, out double torqueNeeded)
        {
            Vector3D totalForce = Vector3D.Zero;
            torqueNeeded = 0;
            float propArea = _block.CubeGrid.GridSize * _block.CubeGrid.GridSize;
            foreach (var part in _bladeParts.Values)
            {
                double propDistanceFromCenter =
                    Vector3D.Distance(part.PositionComp.GetPosition(), _block.GetPosition());
                double propSpeedLocal = (rpm * Math.PI / 30) * propDistanceFromCenter;
                Vector3D propSpeedDirection = Vector3D.Rotate(Vector3D.Left, MatrixD.CreateFromAxisAngle(Vector3D.Forward, -BladeAngle));
                var globalVelocity = _grid.LinearVelocity + LocalToWorldRotation(propSpeedDirection * propSpeedLocal, part.PositionComp.WorldMatrixRef);
                double speedSq = globalVelocity.LengthSquared();
                if (speedSq <= 1)
                    continue;

                Vector3D dragNormal = -Vector3D.Normalize(globalVelocity);
                Vector3D liftNormal = LocalToWorldRotation(Vector3D.Up, part.PositionComp.WorldMatrixRef);

                // angle between chord line and airflow
                double angleOfAttack = Math.Asin(Vector3D.Dot(dragNormal, liftNormal));

                // Approximation of NACA 0012 airfoil. Maximum lift is achieved around 15.65 degrees.
                double liftCoefficient = 1.34951 * Math.Sin(5.75 * angleOfAttack);
                double dragCoefficient = 1.77372503 * angleOfAttack;
                dragCoefficient = dragCoefficient * dragCoefficient * dragCoefficient * dragCoefficient + 0.00608945;

                // induced drag, increases with lift
                double inducedDragCoefficient = liftCoefficient * liftCoefficient / Math.PI;
                dragCoefficient += inducedDragCoefficient;

                double dynamicPressure = 0.5 * speedSq * airDensity * propArea * LiftModifier;

                totalForce += (liftNormal * liftCoefficient + dragNormal * dragCoefficient) * dynamicPressure;
                torqueNeeded += dragCoefficient * dynamicPressure * propDistanceFromCenter * TorqueModifier; // Newtons * Meters

                //DebugDraw.I.DrawLine0(part.PositionComp.GetPosition(), part.PositionComp.GetPosition() + totalForce / 10000, Color.Green);
                //DebugDraw.I.DrawLine0(part.PositionComp.GetPosition(), part.PositionComp.GetPosition() + dragNormal, Color.Red);
            }

            // We only care about the force that's aligned to the thruster direction.
            totalForce = WorldToLocalRotation(totalForce, _block.WorldMatrix);
            return totalForce.Z < 0 ? 0 : totalForce.Z;
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
            /// <summary>
            /// Highest RPM this rotor can achieve.
            /// </summary>
            public float MaxRpm;
            /// <summary>
            /// Highest propeller angle for this rotor.
            /// </summary>
            public float MaxAngle;
            /// <summary>
            /// Highest safe torque for this rotor.
            /// </summary>
            public float MaxTorque;
        }
    }
}
