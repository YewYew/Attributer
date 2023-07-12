using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using attributer.src;
using HarmonyLib;

namespace attributer.src
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemSpear))]
    internal class ItemSpearPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfoSpear(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack.Attributes != null && itemstack.Attributes.HasAttribute("damage"))
            {
                string replaceThis;
                string withThis;
                float damage = 1.5f;

                if (inSlot.Itemstack.Collectible.Attributes != null)
                {
                    damage = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
                }

                replaceThis = (damage + Lang.Get("piercing-damage-thrown"));

                damage = itemstack.Attributes.GetFloat("damage");

                withThis = (damage + Lang.Get("piercing-damage-thrown"));
                //Then we replace!
                dsc.Replace(replaceThis, withThis);
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop"), HarmonyPriority(Priority.Last)]
        public static bool OnHeldInteractStopSpear(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            var spearEntityCode = slot.Itemstack.Collectible.Attributes; //Patchy
            if (byEntity.Attributes.GetInt("aimingCancel") == 1) return false;

            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.StopAnimation("aim");

            if (secondsUsed < 0.35f) return false;

            float damage = 1.5f;

            if (slot.Itemstack.Attributes != null && slot.Itemstack.Attributes.HasAttribute("damage"))
            {
                damage = slot.Itemstack.Attributes.GetFloat("damage");
            }
            else if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage = slot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
            }

            (attributerCoreSystem.sapi as ICoreClientAPI)?.World.AddCameraShake(0.17f);

            ItemStack stack = slot.TakeOut(1);
            slot.MarkDirty();

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, byPlayer, false, 8);

            EntityProperties type = byEntity.World.GetEntityType(new AssetLocation(spearEntityCode["spearEntityCode"].ToString()));//Patchy
            EntityProjectile enpr = byEntity.World.ClassRegistry.CreateEntity(type) as EntityProjectile;
            enpr.FiredBy = byEntity;
            enpr.Damage = damage;
            enpr.ProjectileStack = stack;
            enpr.DropOnImpactChance = 1.1f;
            enpr.DamageStackOnImpact = true;
            enpr.Weight = 0.3f;


            float acc = (1 - byEntity.Attributes.GetFloat("aimingAccuracy", 0));
            double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * acc * 0.75;
            double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * acc * 0.75;

            Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y - 0.2, 0);

            Vec3d aheadPos = pos.AheadCopy(1, byEntity.ServerPos.Pitch + rndpitch, byEntity.ServerPos.Yaw + rndyaw);
            Vec3d velocity = (aheadPos - pos) * 0.65;
            Vec3d spawnPos = byEntity.ServerPos.BehindCopy(0.21).XYZ.Add(byEntity.LocalEyePos.X, byEntity.LocalEyePos.Y - 0.2, byEntity.LocalEyePos.Z);

            enpr.ServerPos.SetPos(spawnPos);
            enpr.ServerPos.Motion.Set(velocity);


            enpr.Pos.SetFrom(enpr.ServerPos);
            enpr.World = byEntity.World;
            enpr.SetRotation();

            byEntity.World.SpawnEntity(enpr);
            byEntity.StartAnimation("throw");

            if (byEntity is EntityPlayer) RefillSlotIfEmpty(slot, byEntity, (itemstack) => itemstack.Collectible is ItemSpear);

            var pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;
            byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/strike"), byPlayer.Entity, byPlayer, pitch * 0.9f + (float)attributerCoreSystem.sapi.World.Rand.NextDouble() * 0.2f, 16, 0.35f);
            return false;
        }
        private static void RefillSlotIfEmpty(ItemSlot slot, EntityAgent byEntity, ActionConsumable<ItemStack> matcher)
        {
            if (!slot.Empty) return;

            byEntity.WalkInventory((invslot) =>
            {
                if (invslot is ItemSlotCreative) return true;

                InventoryBase inv = invslot.Inventory;
                if (!(inv is InventoryBasePlayer) && !inv.HasOpened((byEntity as EntityPlayer).Player)) return true;

                if (invslot.Itemstack != null && matcher(invslot.Itemstack))
                {
                    invslot.TryPutInto(byEntity.World, slot);
                    invslot.Inventory?.PerformNotifySlot(invslot.Inventory.GetSlotId(invslot));
                    slot.Inventory?.PerformNotifySlot(slot.Inventory.GetSlotId(slot));

                    slot.MarkDirty();
                    invslot.MarkDirty();

                    return false;
                }

                return true;
            });
        }
    }
}
