using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using attributer.src.patch;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System;

namespace attributer.src
{
    [HarmonyPatch]
    class attributerCoreSystem : ModSystem
    {
        public static ICoreAPI sapi;
        public static Harmony harmonyInstance;

        public const string harmonyID = "attributer.Patches";
        public attributerCoreSystem()
        {
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.PatchAll();
        }
        public override void Start(ICoreAPI api)
        {
            //This is necessary for api patch stuphfs.
            sapi = api;
            harmonyInstance.PatchAll();
            //api.Event.OnEntitySpawn += OnEntitySpawn;
            base.Start(api);
        }
        /*private void OnEntitySpawn(Entity spawnedEntity)
        {
            if(spawnedEntity is EntityItem)
            {
                EntityItem item = (EntityItem)spawnedEntity;
                //For testing.
            }
        }*/
    }
}
