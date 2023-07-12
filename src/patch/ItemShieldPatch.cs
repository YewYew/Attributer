using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using attributer.src;
using HarmonyLib;
using Vintagestory.GameContent;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemShield))]
    internal class ItemShieldPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetMaxDurability"), HarmonyPriority(Priority.Last)]
        public static void GetMaxDurabilityShield(ItemStack itemstack, ref int __result)
        {
            if (itemstack.Attributes.HasAttribute("maxdurability"))
            {
                __result = itemstack.Attributes.GetInt("maxdurability");
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfoShield(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack.Attributes != null && itemstack.Attributes.HasAttribute("shield"))
            {
                //Thank thy lorde for dsc.Replace()! Again!
                //Reconstruct the Shield Text.
                var attr = inSlot.Itemstack?.ItemAttributes?["shield"];

                float acdmgabsorb = attr["damageAbsorption"]["active"].AsFloat(0);
                float acchance = attr["protectionChance"]["active"].AsFloat(0);

                float padmgabsorb = attr["damageAbsorption"]["passive"].AsFloat(0);
                float pachance = attr["protectionChance"]["passive"].AsFloat(0);

                string replaceThis = Lang.Get("shield-stats", (int)(100 * acchance), (int)(100 * pachance), acdmgabsorb, padmgabsorb);
                //New Text.
                acdmgabsorb = itemstack.Attributes.GetTreeAttribute("shield").GetTreeAttribute("damageAbsorption").GetFloat("active");
                acchance = itemstack.Attributes.GetTreeAttribute("shield").GetTreeAttribute("protectionChance").GetFloat("active");

                padmgabsorb = itemstack.Attributes.GetTreeAttribute("shield").GetTreeAttribute("damageAbsorption").GetFloat("passive");
                pachance = itemstack.Attributes.GetTreeAttribute("shield").GetTreeAttribute("protectionChance").GetFloat("passive");

                string withThis = Lang.Get("shield-stats", (int)(100 * acchance), (int)(100 * pachance), acdmgabsorb, padmgabsorb);
                //Then we replace!
                dsc.Replace(replaceThis, withThis);
            }
        }

    }
}
