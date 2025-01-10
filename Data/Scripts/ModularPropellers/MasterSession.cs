using ModularPropellers.Propellers;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace ModularPropellers
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class MasterSession : MySessionComponentBase
    {
        public static MasterSession I;

        private readonly List<MyPlanet> _planets = new List<MyPlanet>();

        public override void LoadData()
        {
            I = this;
            RotorManager.Init();
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            RotorManager.Close();
            I = null;
        }

        public float GetAtmosphereDensity(IMyCubeGrid grid)
        {
            Vector3D gridPos = grid.PositionComp.GetPosition();
            foreach (var planet in _planets.ToArray())
            {
                if (planet.Closed || planet.MarkedForClose)
                {
                    _planets.Remove(planet);
                    continue;
                }

                if (Vector3D.DistanceSquared(gridPos, planet.PositionComp.GetPosition()) >
                    planet.AtmosphereRadius * planet.AtmosphereRadius)
                    continue;
                return planet.GetAirDensity(gridPos);
            }

            return 0;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
                _planets.Add(planet);
        }
    }
}
