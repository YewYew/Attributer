using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemMetalPlate))]
    internal class ItemIronBloomPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CanWork"), HarmonyPriority(Priority.First)]
        public static void CanWorkItemIronBloom(ItemStack stack, ref bool __result)
        {
            float temperature = stack.Collectible.GetTemperature(attributerCoreSystem.sapi.World, stack);
            float meltingpoint = stack.Collectible.GetMeltingPoint(attributerCoreSystem.sapi.World, null, new DummySlot(stack));

            if (stack.Attributes.HasAttribute("workableTemperature") == true)
            {
                __result = stack.Attributes.GetFloat("workableTemperature", meltingpoint / 2) <= temperature;
            }
        }
    }
}
