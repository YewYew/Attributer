using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemArrow))]
    internal class ItemArrowPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfoItemArrow(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            string replaceThis;
            string withThis;
            string modDamage = "";
            if (inSlot.Itemstack.Attributes.HasAttribute("damage"))
            {
                float dmg = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
                if (dmg != 0)
                {
                    replaceThis = ((dmg > 0 ? "+" : "") + dmg + Lang.Get("piercing-damage"));
                    dmg = inSlot.Itemstack.Attributes.GetFloat("damage", 0f);
                    if (dmg != 0) {
                        withThis = ((dmg > 0 ? "+" : "") + dmg + Lang.Get("piercing-damage"));
                        dsc.Replace(replaceThis, withThis);
                    };
                }
                else
                {
                    //If arrow damage is zero, but ours isn't, we need to append our own value over it.
                    dmg = inSlot.Itemstack.Attributes.GetFloat("damage", 0f);
                    if(dmg != 0)
                    {
                        withThis = ((dmg > 0 ? "+" : "") + dmg + Lang.Get("piercing-damage"));
                        modDamage = withThis + "\n";
                    }
                }
            }
            if (inSlot.Itemstack.Attributes.HasAttribute("breakChanceOnImpact"))
            {
                float breakChanceOnImpact = inSlot.Itemstack.Collectible.Attributes["breakChanceOnImpact"].AsFloat(0.5f);
                replaceThis = (Lang.Get("breakchanceonimpact", (int)(breakChanceOnImpact * 100)));
                breakChanceOnImpact = inSlot.Itemstack.Attributes.GetFloat("breakChanceOnImpact");
                withThis = (Lang.Get("breakchanceonimpact", (int)(breakChanceOnImpact * 100)));
                dsc.Replace(replaceThis, modDamage + withThis);
            }

        }
    }
}
