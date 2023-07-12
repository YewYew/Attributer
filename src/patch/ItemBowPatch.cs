using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using HarmonyLib;
using Vintagestory.API.Config;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemBow))]
    internal class ItemBowPatch
    {
        private static ItemSlot GetNextArrow(EntityAgent byEntity)
        {
            ItemSlot slot = null;
            byEntity.WalkInventory((invslot) =>
            {
                if (invslot is ItemSlotCreative) return true;

                if (invslot.Itemstack != null && invslot.Itemstack.Collectible.Code.Path.StartsWith("arrow-"))
                {
                    slot = invslot;
                    return false;
                }

                return true;
            });

            return slot;
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfoBow(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack.Attributes != null && itemstack.Attributes.HasAttribute("damage"))
            {
                string replaceThis;
                string withThis;
                float dmg = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
                replaceThis = (dmg + Lang.Get("piercing-damage"));

                dmg = itemstack.Attributes.GetFloat("damage");
                withThis = (dmg + Lang.Get("piercing-damage"));

                //Then we replace!
                dsc.Replace(replaceThis, withThis);
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop"), HarmonyPriority(Priority.Last)]
        public static bool OnHeldInteractStopBow(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Attributes.GetInt("aimingCancel") == 1) return false;
            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.AnimManager.StopAnimation("bowaim");

            if (byEntity.World is IClientWorldAccessor)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
            }

            slot.Itemstack.Attributes.SetInt("renderVariant", 0);
            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();

            if (secondsUsed < 0.35f) return false;

            ItemSlot arrowSlot = GetNextArrow(byEntity);
            if (arrowSlot == null) return false;

            string arrowMaterial = arrowSlot.Itemstack.Collectible.FirstCodePart(1);
            float damage = 0;

            // Bow damage
            if (slot.Itemstack.Attributes != null && slot.Itemstack.Attributes.HasAttribute("damage"))
            {
                damage += slot.Itemstack.Attributes.GetFloat("damage");
            }
            else if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage += slot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
            }

            // Arrow damage
            if (arrowSlot.Itemstack.Attributes != null && arrowSlot.Itemstack.Attributes.HasAttribute("damage"))
            {
                damage += arrowSlot.Itemstack.Attributes.GetFloat("damage");
            }
            else if (arrowSlot.Itemstack.Collectible.Attributes != null)
            {
                damage += arrowSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
            }

            ItemStack stack = arrowSlot.TakeOut(1);
            arrowSlot.MarkDirty();

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            byEntity.World.PlaySoundAt(new AssetLocation("sounds/bow-release"), byEntity, byPlayer, false, 8);

            float breakChance = 0.5f;
            if (stack.ItemAttributes != null) breakChance = stack.ItemAttributes["breakChanceOnImpact"].AsFloat(0.5f);
            if (stack.Attributes.HasAttribute("breakChanceOnImpact")) breakChance = stack.Attributes.GetFloat("breakChanceOnImpact");

            EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("arrow-" + stack.Collectible.Variant["material"]));
            Entity entity = byEntity.World.ClassRegistry.CreateEntity(type);
            ((EntityProjectile)entity).FiredBy = byEntity;
            ((EntityProjectile)entity).Damage = damage;
            ((EntityProjectile)entity).ProjectileStack = stack;
            ((EntityProjectile)entity).DropOnImpactChance = 1 - breakChance;

            float acc = Math.Max(0.001f, (1 - byEntity.Attributes.GetFloat("aimingAccuracy", 0)));
            double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * acc * 0.75;
            double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * acc * 0.75;

            Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, byEntity.SidedPos.Pitch + rndpitch, byEntity.SidedPos.Yaw + rndyaw);
            Vec3d velocity = (aheadPos - pos) * byEntity.Stats.GetBlended("bowDrawingStrength");


            entity.ServerPos.SetPos(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0, byEntity.LocalEyePos.Y, 0));
            entity.ServerPos.Motion.Set(velocity);



            entity.Pos.SetFrom(entity.ServerPos);
            entity.World = byEntity.World;
            ((EntityProjectile)entity).SetRotation();

            byEntity.World.SpawnEntity(entity);

            slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);

            byEntity.AnimManager.StartAnimation("bowhit");
            return false;
        }

    }
}
