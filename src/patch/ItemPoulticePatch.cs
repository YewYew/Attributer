using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemPoultice))]
    internal class ItemPoulticePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop"), HarmonyPriority(Priority.Last)]
        public static bool OnHeldInteractStopItemPoultice(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed > 0.7f && byEntity.World.Side == EnumAppSide.Server)
            {
                JsonObject attr = slot.Itemstack.Collectible.Attributes;
                float health = attr["health"].AsFloat();
                if (slot.Itemstack.Attributes.HasAttribute("health"))
                {
                    health = slot.Itemstack.Attributes.GetFloat("health", health);
                }
                byEntity.ReceiveDamage(new DamageSource()
                {
                    Source = EnumDamageSource.Internal,
                    Type = health > 0 ? EnumDamageType.Heal : EnumDamageType.Poison
                }, Math.Abs(health));

                slot.TakeOut(1);
                slot.MarkDirty();
            }
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfoItemPoultice(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            string replaceThis;
            string withThis;
            var modattr = inSlot.Itemstack.Attributes;
            if (modattr != null && modattr.HasAttribute("health"))
            {
                JsonObject attr = inSlot.Itemstack.Collectible.Attributes;
                if (attr != null && attr["health"].Exists)
                {
                    float health = attr["health"].AsFloat();
                    replaceThis = Lang.Get("When used: +{0} hp", health);
                    health = modattr.GetFloat("health");
                    withThis = Lang.Get("When used: +{0} hp", health);
                    dsc.Replace(replaceThis, withThis);
                } else
                {
                    dsc.Append(Lang.Get("When used: +{0} hp", modattr.GetFloat("health")));
                }
            }
        }
    }
}
