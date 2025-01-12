using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace ModularPropellers.Propellers
{
    public partial class RotorLogic
    {
        private Dictionary<IMyCubeBlock, AnimationBlade> _bladeParts = new Dictionary<IMyCubeBlock, AnimationBlade>();

        private void InitialRotateBlades()
        {
            try
            {
                var spacing = Math.PI * 2 / _bladeSets.Count;
                float currentRotation = (float)spacing;

                foreach (var set in _bladeSets)
                {
                    Matrix rotationMatrix = Matrix.CreateFromAxisAngle(Vector3.Forward, currentRotation);
                    Matrix m;
                    foreach (var blade in set)
                    {
                        m = Matrix.CreateWorld(
                                Vector3.Up * (((blade.Position - _block.Position) * _block.CubeGrid.GridSize).Length() -
                                              _block.CubeGrid.GridSize / 4), Vector3.Down, Vector3.Backward) *
                            rotationMatrix;
                        var part = _bladeParts[blade];
                        part.PositionComp.SetLocalMatrix(ref m);
                        part.Update(0, 0);
                    }

                    currentRotation += (float)spacing;
                }
            }
            catch (Exception ex)
            {
                ModularDefinition.ModularApi.Log("[Handled]" + ex);
            }
        }

        private sealed class AnimationBlade : MyEntity
        {
            private MyEntity _parent;
            private float _bladeAngle = 0;

            public AnimationBlade(string model, Matrix localMatrix, MyEntity parent)
            {
                _parent = parent;
                Init(null, model, parent, 1);
                if (string.IsNullOrEmpty(model))
                    Flags &= ~EntityFlags.Visible;
                Save = false;
                NeedsWorldMatrix = true;

                PositionComp.SetLocalMatrix(ref localMatrix);
                MyEntities.Add(this);
            }

            public void Update(float rotationSpeed, float bladeAngle)
            {
                var refMatrix = _parent.PositionComp.WorldMatrixRef;
                PositionComp.UpdateWorldMatrix(ref refMatrix);
                if (rotationSpeed > 0)
                    RotateAroundParentAxis(Vector3.Forward, rotationSpeed);
                if (bladeAngle != _bladeAngle)
                    RotateAroundLocalAxis(Vector3.Forward, bladeAngle - _bladeAngle);
                _bladeAngle = bladeAngle;
            }

            public void RotateAroundParentAxis(Vector3 axis, double amount)
            {
                Matrix newMatrix = PositionComp.LocalMatrixRef * Matrix.CreateFromAxisAngle(axis, (float) amount);
                PositionComp.SetLocalMatrix(ref newMatrix);
            }

            public void RotateAroundLocalAxis(Vector3 axis, double amount)
            {
                Matrix newMatrix = RotateMatrixAroundPoint(PositionComp.LocalMatrixRef, PositionComp.LocalMatrixRef.Translation, Vector3.RotateAndScale(axis, PositionComp.LocalMatrixRef), amount);
                PositionComp.SetLocalMatrix(ref newMatrix);
            }
        }

        public static Matrix RotateMatrixAroundPoint(Matrix matrix, Vector3D point, Vector3D axis, double angleRadians)
        {
            matrix.Translation -= point;
            Matrix transformedMatrix = matrix * MatrixD.CreateFromAxisAngle(axis, angleRadians);
            transformedMatrix.Translation += point;

            return transformedMatrix;
        }
    }
}
