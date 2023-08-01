using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using attributer.src;
using HarmonyLib;

namespace attributer.src
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(CollectibleObject))]
    internal class CollectibleObjectPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfo(Vintagestory.API.Common.CollectibleObject __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack.Attributes != null && itemstack.Attributes.HasAttribute("miningspeed"))
            {
                //Thank thy lorde for dsc.Replace()!
                //We shall utilize this to eliminate the false mining speed text, and raise our own!
                //First we reconstruct the original Collectible.cs mining modifier description code.
                int i = 0;
                string replaceThis = "";
                string withThis = "";
                foreach (var val in itemstack.Collectible.MiningSpeed)
                {
                    //For efficacy's sake, we can also build our new string in here as well.
                    if (i > 0) { replaceThis += ", "; withThis += ", "; }
                    if (val.Value > 1) { 
                        replaceThis += Lang.Get(val.Key.ToString()) + " " + val.Value.ToString("#.#") + "x";
                    }
                    if (itemstack.Attributes.GetTreeAttribute("miningspeed").GetFloat(val.Key.ToString()) > 1)
                    {
                        withThis += Lang.Get(val.Key.ToString()) + " " + itemstack.Attributes.GetTreeAttribute("miningspeed").GetFloat(val.Key.ToString()).ToString("#.#") + "x";
                    }
                    i++;
                }
                //Then we replace!
                //Thanks for the report Acouthyt!
                if (replaceThis.Length > 0)
                {
                    dsc.Replace(replaceThis, withThis);
                } else
                {
                    if (!dsc.ToString().Contains(Lang.Get("item-tooltip-miningspeed") + withThis))
                    {
                        dsc.Replace(Lang.Get("item-tooltip-miningspeed"), Lang.Get("item-tooltip-miningspeed") + withThis);
                    }
                }
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetAttackPower"), HarmonyPriority(Priority.Last)]
        public static void GetAttackPower(IItemStack withItemStack, ref float __result)
        {
            if (withItemStack.Attributes.HasAttribute("attackpower"))
            {
                __result = withItemStack.Attributes.GetFloat("attackpower");
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetMaxDurability"), HarmonyPriority(Priority.Last)]
        public static void GetMaxDurability(ItemStack itemstack, ref int __result)
        {
            if (itemstack.Attributes.HasAttribute("maxdurability"))
            {
                __result = itemstack.Attributes.GetInt("maxdurability");
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetMiningSpeed"), HarmonyPriority(Priority.Last)]
        public static void GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer, ref float __result, ref ICoreAPI ___api)
        {
            //This is a check after all other checks, that looks for the miningspeed attribute.
            if (itemstack.Attributes.HasAttribute("miningspeed"))
            {
                //Pretty much have to redo the whole function for this to work.
                //I could do fancy math with the output, but nah.
                float traitRate = 1f;

                var mat = block.GetBlockMaterial(___api.World.BlockAccessor, blockSel.Position);

                if (mat == EnumBlockMaterial.Ore || mat == EnumBlockMaterial.Stone)
                {
                    traitRate = forPlayer.Entity.Stats.GetBlended("miningSpeedMul");
                }
                if (itemstack.Collectible.MiningSpeed == null || !itemstack.Collectible.MiningSpeed.ContainsKey(mat)) __result = traitRate;
                //Additional sanity check seems to fix the "knife only works once bug".
                if (itemstack.Attributes.GetTreeAttribute("miningspeed").HasAttribute(mat.ToString()))
                {
                    float modifiedMiningSpeed = itemstack.Attributes.GetTreeAttribute("miningspeed").GetFloat(mat.ToString());
                    __result = modifiedMiningSpeed * GlobalConstants.ToolMiningSpeedModifier * traitRate;
                }
            }
        }
    }
}
