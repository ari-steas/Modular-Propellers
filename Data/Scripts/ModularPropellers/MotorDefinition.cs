using System;
using System.Collections.Generic;
using ModularPropellers.Motors;
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
        internal ModularPhysicalDefinition MotorDefinition => new ModularPhysicalDefinition
        {
            // Unique name of the definition.
            Name = "MotorDefinition",

            OnInit = () =>
            {
                
            },

            // Triggers whenever a new part is added to an assembly.
            OnPartAdd = (assemblyId, block, isBasePart) =>
            {
                if (!MotorManager.Logic.ContainsKey(assemblyId))
                    MotorManager.Logic[assemblyId] = new MotorAssemblyLogic(assemblyId);
                MotorManager.Logic[assemblyId].AddBlock(block);
            },

            // Triggers whenever a part is removed from an assembly.
            OnPartRemove = (assemblyId, block, isBasePart) =>
            {
                MotorManager.Logic[assemblyId].RemoveBlock(block);
            },

            // Triggers whenever a part is destroyed, just after OnPartRemove.
            OnPartDestroy = (assemblyId, block, isBasePart) =>
            {
                
            },

            OnAssemblyClose = (assemblyId) =>
            {
                MotorManager.Logic.Remove(assemblyId);
            },

            // Optional - if this is set, an assembly will not be created until a baseblock exists.
            // 
            BaseBlockSubtype = null,

            // All SubtypeIds that can be part of this assembly.
            AllowedBlockSubtypes = new[]
            {
                "ModularPropellerRotorLarge",
                "ModularPropellerRotorSmall",

                "LG_ModularMotorElectric",
                "SG_ModularMotorElectric",

                "LG_ModularMotorShaft",
                "LG_ModularMotorShaftCorner",
                "LG_ModularMotorShaftCross",
                "LG_ModularMotorShaftT",
                "SG_ModularMotorShaft",
                "SG_ModularMotorShaftCorner",
                "SG_ModularMotorShaftCross",
                "SG_ModularMotorShaftT",
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                ["ModularPropellerRotorLarge"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
                ["ModularPropellerRotorSmall"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },

                ["LG_ModularMotorElectric"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                },
                ["SG_ModularMotorElectric"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                },

                ["LG_ModularMotorShaft"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
                ["LG_ModularMotorShaftCorner"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Backward] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                },
                ["SG_ModularMotorShaftCross"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                    [Vector3I.Left] = Array.Empty<string>(),
                },
                ["SG_ModularMotorShaft"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
                ["SG_ModularMotorShaftCorner"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Backward] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                },
                ["SG_ModularMotorShaftCross"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                    [Vector3I.Left] = Array.Empty<string>(),
                },
            },
        };
    }
}
