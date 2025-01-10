using System;
using System.Collections.Generic;
using System.Linq;
using ModularPropellers.Propellers;
using Sandbox.ModAPI;
using VRageMath;
using static ModularPropellers.Communication.DefinitionDefs;

namespace ModularPropellers
{
    /* Hey there modders!
     *
     * This file is a *template*. Make sure to keep up-to-date with the latest version, which can be found at https://github.com/StarCoreSE/Modular-Assemblies-Client-Mod-Template.
     *
     * If you're just here for the API, head on over to https://github.com/StarCoreSE/Modular-Assemblies/wiki/The-Modular-API for a (semi) comprehensive guide.
     *
     */
    internal partial class ModularDefinition
    {
        // You can declare functions in here, and they are shared between all other ModularDefinition files.
        // However, for all but the simplest of assemblies it would be wise to have a separate utilities class.

        // This is the important bit.
        internal ModularPhysicalDefinition PropellerDefinition => new ModularPhysicalDefinition
        {
            // Unique name of the definition.
            Name = "PropellerDefinition",

            OnInit = () =>
            {
                
            },

            // Triggers whenever a new part is added to an assembly.
            OnPartAdd = (assemblyId, block, isBasePart) =>
            {
                // Handling for duplicate rotors
                if (block is IMyThrust)
                {
                    var logic = block.GameLogic.GetAs<RotorLogic>();
                    if (RotorManager.RotorLogic.ContainsKey(assemblyId))
                    {
                        RotorManager.RotorLogic[assemblyId].IsValid = false;
                        logic.IsValid = false;
                    }
                    else
                    {
                        RotorManager.RotorLogic[assemblyId] = logic;
                        logic.InitialCheck(assemblyId);
                    }
                }
                else
                {
                    RotorManager.RotorLogic.GetValueOrDefault(assemblyId, null)?.AddBlade(block);
                }
            },

            // Triggers whenever a part is removed from an assembly.
            OnPartRemove = (assemblyId, block, isBasePart) =>
            {
                // Handling for duplicate rotors
                if (block is IMyThrust)
                {
                    var remainingLogics = ModularApi.GetMemberParts(assemblyId).Where(part => part is IMyThrust).ToArray();
                    if (remainingLogics.Length == 1)
                    {
                        var logic = remainingLogics[0].GameLogic.GetAs<RotorLogic>();
                        logic.IsValid = true;
                        logic.InitialCheck(assemblyId);
                    }

                    block.GameLogic.GetAs<RotorLogic>().ClearParts();
                }
                else
                {
                    block.Visible = true;
                    RotorManager.RotorLogic.GetValueOrDefault(assemblyId, null)?.RemoveBlade(block);
                }
            },

            // Triggers whenever a part is destroyed, just after OnPartRemove.
            OnPartDestroy = (assemblyId, block, isBasePart) =>
            {
                
            },

            // Optional - if this is set, an assembly will not be created until a baseblock exists.
            // 
            BaseBlockSubtype = null,

            // All SubtypeIds that can be part of this assembly.
            AllowedBlockSubtypes = new[]
            {
                "ModularPropellerRotorLarge",
                "ModularPropellerRotorSmall",
                "ModularPropellerBladeLarge",
                "ModularPropellerBladeSmall",
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                ["ModularPropellerRotorLarge"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Up] = Array.Empty<string>(),
                    [Vector3I.Down] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                    [Vector3I.Left] = Array.Empty<string>(),
                },
                ["ModularPropellerRotorSmall"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Up] = Array.Empty<string>(),
                    [Vector3I.Down] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                    [Vector3I.Left] = Array.Empty<string>(),
                },

                ["ModularPropellerBladeLarge"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
                ["ModularPropellerBladeSmall"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
            },
        };
    }
}
