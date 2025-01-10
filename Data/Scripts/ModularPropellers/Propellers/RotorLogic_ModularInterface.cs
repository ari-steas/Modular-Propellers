using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace ModularPropellers.Propellers
{
    internal partial class RotorLogic
    {
        public void AddBlade(IMyCubeBlock blade)
        {
            Matrix discard;
            blade.Visible = false;
            var animation = new AnimationBlade(blade.CalculateCurrentModel(out discard), Matrix.Identity, (MyEntity) _block);
            _bladeParts.Add(blade, animation);

            // Really inefficient way to get prop blades
            _bladeSets.Clear();
            foreach (var basePart in ModularDefinition.ModularApi.GetConnectedBlocks(_block, "PropellerDefinition",
                         false))
            {
                HashSet<IMyCubeBlock> set = new HashSet<IMyCubeBlock>
                {
                    basePart
                };
                RecursiveGetBlades(basePart, ref set);
                _bladeSets.Add(set);
            }

            InitialRotateBlades();
        }

        private void RecursiveGetBlades(IMyCubeBlock block, ref HashSet<IMyCubeBlock> set)
        {
            foreach (var part in ModularDefinition.ModularApi.GetConnectedBlocks(block, "PropellerDefinition",
                         false))
                if (part != _block && set.Add(part))
                    RecursiveGetBlades(part, ref set);
        }


        public void RemoveBlade(IMyCubeBlock blade)
        {
            HashSet<IMyCubeBlock> setToRemove = null;
            foreach (var set in _bladeSets)
            {
                if (!set.Remove(blade))
                    continue;
                if (set.Count == 0)
                    setToRemove = set;
                break;
            }

            if (_bladeSets.Remove(setToRemove))
                InitialRotateBlades();

            _bladeParts[blade].Close();
            _bladeParts.Remove(blade);
        }

        public void InitialCheck(int assemblyId)
        {
            foreach (var part in ModularDefinition.ModularApi.GetMemberParts(assemblyId).Where(part => !(part is IMyThrust)))
                AddBlade(part);
        }

        public void ClearParts()
        {
            _bladeParts.Clear();
        }
    }
}
