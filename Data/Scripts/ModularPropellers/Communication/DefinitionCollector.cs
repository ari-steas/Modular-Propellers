﻿using ModularPropellers.Communication;
using static ModularPropellers.Communication.DefinitionDefs;

// ReSharper disable once CheckNamespace
namespace ModularPropellers
{
    internal partial class ModularDefinition
    {
        internal static ModularDefinitionApi ModularApi = new ModularDefinitionApi();
        internal ModularDefinitionContainer Container = new ModularDefinitionContainer();

        internal void LoadDefinitions(params ModularPhysicalDefinition[] defs)
        {
            Container.PhysicalDefs = defs;
        }

        /// <summary>
        ///     Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static ModularDefinitionContainer GetBaseDefinitions()
        {
            return new ModularDefinition().Container;
        }
    }
}