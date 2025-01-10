using System;
using System.Collections.Generic;
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
        private IMyThrust _block;
        private IMyCubeGrid _grid => _block.CubeGrid;
        private List<HashSet<IMyCubeBlock>> _bladeSets = new List<HashSet<IMyCubeBlock>>();

        public bool IsValid = true;

        internal MySync<float, SyncDirection.BothWays> BladeAngle;
        internal MySync<float, SyncDirection.FromServer> RPM;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            _block = (IMyThrust) Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if(_block?.CubeGrid?.Physics == null)
                return;

            BladeAngle.Value = (float) Math.PI/4;
            RPM.Value = 0f;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (!IsValid)
                return;
            MyAPIGateway.Utilities.ShowNotification("Parts: " + _bladeParts.Count + " Sets: " + _bladeSets.Count, 1000/60);

            float maxRpm = 500;
            _block.ThrustMultiplier = (float) CalculateThrust(maxRpm, MasterSession.I.GetAtmosphereDensity(_grid)) / 100f;
            RPM.Value = maxRpm * MathHelper.Lerp(RPM.Value/maxRpm, _block.CurrentThrustPercentage, 0.02f);

            if (float.IsNaN(RPM.Value) || float.IsInfinity(RPM.Value))
                RPM.Value = 0;

            foreach (var bladePair in _bladeParts)
                bladePair.Value.Update(RPM/360, BladeAngle);

            //BladeAngle.Value += _amt;
            //if (BladeAngle > Math.PI / 4 || BladeAngle < 0)
            //    _amt *= -1;
        }

        internal double CalculateThrust(double rpm, double airDensity)
        {
            Vector3D totalLift = Vector3D.Zero;
            foreach (var part in _bladeParts.Values)
            {
                double propSpeedLocal = (rpm * Math.PI / 30) * Vector3D.Distance(part.PositionComp.GetPosition(), _block.GetPosition());
                Vector3D propSpeedDirection = Vector3D.Rotate(Vector3D.Left, MatrixD.CreateFromAxisAngle(Vector3D.Forward, -BladeAngle));
                var globalVelocity = _grid.LinearVelocity + LocalToWorldRotation(propSpeedDirection * propSpeedLocal, part.PositionComp.WorldMatrixRef);
                double speedSq = globalVelocity.LengthSquared();
                if (speedSq == 0)
                    continue;

                Vector3D dragNormal = -Vector3D.Normalize(globalVelocity);

                Vector3D liftNormal = LocalToWorldRotation(Vector3D.Up, part.PositionComp.WorldMatrixRef);

                // angle between chord line and airflow
                double angleOfAttack = -Math.Asin(Vector3D.Dot(dragNormal, liftNormal));
                double liftCoefficient =
                    1.35 * Math.Sin(5.75 * angleOfAttack); // Approximation of NACA 0012 airfoil

                double dynamicUnitPressure = 0.5 * speedSq * airDensity;

                double liftForce = liftCoefficient * dynamicUnitPressure;
                totalLift += liftNormal * liftForce;
            }

            //DebugDraw.I.DrawLine0(_block.PositionComp.GetPosition(), _block.PositionComp.GetPosition() + totalLift/1000, Color.Green);

            return totalLift.Length();
        }

        Vector3D WorldToLocal(Vector3D pos, MatrixD parentMatrix)
        {
            MatrixD inv = MatrixD.Invert(parentMatrix);
            return Vector3D.Rotate(pos - parentMatrix.Translation, inv);
        }
        Vector3D WorldToLocalRotation(Vector3D pos, MatrixD parentMatrix)
        {
            MatrixD inv = MatrixD.Invert(parentMatrix);
            return Vector3D.Rotate(pos, inv);
        }
        Vector3D LocalToWorld(Vector3D pos, MatrixD parentMatrix)
        {
            return Vector3D.Rotate(pos, parentMatrix) + parentMatrix.Translation;
        }
        Vector3D LocalToWorldRotation(Vector3D pos, MatrixD parentMatrix)
        {
            return Vector3D.Rotate(pos, parentMatrix);
        }
    }
}
