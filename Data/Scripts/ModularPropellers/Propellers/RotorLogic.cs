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
    public partial class RotorLogic : MyGameLogicComponent, IMyEventProxy
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
                MaxRpm = 5000,
                MaxAngle = (float) MathHelper.ToRadians(15.5),
            },
            ["ModularPropellerRotorSmall"] = new RotorInfo
            {
                MaxRpm = 5000,
                MaxAngle = (float) MathHelper.ToRadians(15.5),
            },
        };

        public int AssemblyId = -1;
        private IMyThrust _block;
        private IMyCubeGrid _grid => _block.CubeGrid;
        public RotorInfo Info;
        private List<HashSet<IMyCubeBlock>> _bladeSets = new List<HashSet<IMyCubeBlock>>();

        private double _desiredPower = 0;
        public double MaxDesiredPower { get; private set; } = 0;

        public double AvailablePower = 0;

        public float MaxRpm
        {
            get
            {
                return AbsMaxRpm * MaxRpmPercent;
            }
            set
            {
                MaxRpmPercent.Value = MathHelper.Clamp(value / AbsMaxRpm, 0, 1);
            }
        }

        public float AbsMaxRpm = 0;
        internal MySync<float, SyncDirection.BothWays> BladeAngle, MaxRpmPercent;
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
            MaxRpmPercent.Value = 1;

            RotorControls.DoOnce();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            MaxDesiredPower = _block.IsWorking ? CalculateMaxPower() : 0;
            AbsMaxRpm = RotorInfos[_block.BlockDefinition.SubtypeName].MaxRpm / _bladeParts.Count * 2.875f;

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
            ApplyThrust(RPM, MasterSession.I.GetAtmosphereDensity(_grid), true, out torqueNeeded);
            //_block.ThrustMultiplier = MathHelper.Clamp((float) CalculateThrust(RPM, MasterSession.I.GetAtmosphereDensity(_grid), out torqueNeeded) / 100f, 1, float.MaxValue);
            _desiredPower = torqueNeeded * 2 * Math.PI * RPM / 60; // Watts
            double netPower = AvailablePower - _desiredPower;

            if (MyAPIGateway.Session.IsServer)
            {
                var newRpm = RPM.Value + (float) ((60 * netPower) / (torqueNeeded * 2 * Math.PI)) / 60f;
                if (newRpm < 0 || float.IsNaN(newRpm))
                    newRpm = 0;
                if (newRpm > MaxRpm)
                    newRpm = MaxRpm;
                RPM.Value = newRpm;
            }

            if (!_block.IsWorking)
                return;

            MyAPIGateway.Utilities.ShowNotification($"RPM: {RPM.Value:N0}", 1000/60);
            MyAPIGateway.Utilities.ShowNotification($"Rotor Power: {_desiredPower/1000000:F1}MW ({100*_desiredPower/AvailablePower:N0}%)", 1000/60);
            //DebugDraw.I.DrawLine0(_block.PositionComp.GetPosition(), _block.PositionComp.GetPosition() + _block.WorldMatrix.Backward * _block.MaxEffectiveThrust / 10000, Color.Blue);
        }

        private double CalculateMaxPower()
        {
            double torqueNeeded;
            ApplyThrust(MaxRpm, MasterSession.I.GetAtmosphereDensity(_grid), false, out torqueNeeded);
            return torqueNeeded * 2 * Math.PI * MaxRpm / 60;
        }

        private void ApplyThrust(double rpm, double airDensity, bool applyImpulse, out double torqueNeeded)
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

                var bladeForce = (liftNormal * liftCoefficient + dragNormal * dragCoefficient) * dynamicPressure;
                totalForce += bladeForce;
                torqueNeeded += dragCoefficient * dynamicPressure * propDistanceFromCenter * TorqueModifier; // Newtons * Meters, represents drag force

                if (applyImpulse && bladeForce.LengthSquared() > 1)
                    _block.CubeGrid.Physics.ApplyImpulse(bladeForce / 60, part.PositionComp.GetPosition());

                //DebugDraw.I.DrawLine0(part.PositionComp.GetPosition(), part.PositionComp.GetPosition() + force / 10000, Color.Green);
                //DebugDraw.I.DrawLine0(part.PositionComp.GetPosition(), part.PositionComp.GetPosition() + dragNormal, Color.Red);
            }

            if (!applyImpulse)
                return;

            if (ModularDefinition.ModularApi.IsDebug())
            {
                // We only care about the force that's aligned to the thruster direction for debug output.
                //totalForce = WorldToLocalRotation(totalForce, _block.WorldMatrix);
                //totalForce.X = 0; // Ignoring horizontal components to make it easier for players to use
                //totalForce.Y = 0;
                //totalForce = LocalToWorldRotation(totalForce, _block.WorldMatrix);
                DebugDraw.I.DrawLine0(_block.PositionComp.GetPosition(), _block.PositionComp.GetPosition() + totalForce / 10000, Color.Green);
            }
            

            //
            //if (totalForce.LengthSquared() > 1)
            //    _block.CubeGrid.Physics.ApplyImpulse(totalForce / 60, _block.PositionComp.GetPosition());
            //MyAPIGateway.Utilities.ShowNotification(totalForce.Length()/1000 + " kN", 1000/60);
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
        }
    }
}
