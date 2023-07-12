using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ModSystemWearableStats))]
    internal class WearableStatsPatch
    {//ItemWearable and Shield.
        [HarmonyPrefix]
        [HarmonyPatch("handleDamaged"), HarmonyPriority(Priority.Last)]
        public static bool handleDamagedWearableStats(IPlayer player, float damage, DamageSource dmgSource, ref ICoreAPI ___api, ref float __result)
        {
            EnumDamageType type = dmgSource.Type;
            double angleProtectionRange = 120 / 2 * GameMath.DEG2RAD;

            // Reduce damage if player holds a shield
            ItemSlot[] shieldSlots = new ItemSlot[] { player.Entity.LeftHandItemSlot, player.Entity.RightHandItemSlot };
            foreach (var shieldSlot in shieldSlots)
            {
                float dmgabsorb;
                float chance;

                string usetype = player.Entity.Controls.Sneak ? "active" : "passive";

                var attr = shieldSlot.Itemstack?.ItemAttributes?["shield"];
                var ratt = shieldSlot.Itemstack?.Attributes?["shield"];
                if ((attr == null || !attr.Exists) && (ratt == null)) continue;

                var itemstack = shieldSlot.Itemstack;
                bool dmgabsorbMod = false;
                bool chanceMod = false;
                if (itemstack.Attributes.HasAttribute("shield"))
                {
                    dmgabsorbMod = itemstack.Attributes.GetTreeAttribute("shield").HasAttribute("damageAbsorption");
                    chanceMod = itemstack.Attributes.GetTreeAttribute("shield").HasAttribute("protectionChance");
                }
                if (dmgabsorbMod)
                {
                    dmgabsorb = itemstack.Attributes.GetTreeAttribute("shield").GetTreeAttribute("damageAbsorption").GetFloat(usetype);
                }
                else
                {
                    dmgabsorb = attr["damageAbsorption"][usetype].AsFloat(0);
                }
                if (chanceMod)
                {
                    chance = itemstack.Attributes.GetTreeAttribute("shield").GetTreeAttribute("protectionChance").GetFloat(usetype);
                }
                else
                {
                    chance = attr["protectionChance"][usetype].AsFloat(0);
                }

                (player as IServerPlayer)?.SendMessage(GlobalConstants.DamageLogChatGroup, Lang.Get("{0:0.#} of {1:0.#} damage blocked by shield", Math.Min(dmgabsorb, damage), damage), EnumChatType.Notification);

                double dx;
                double dz;
                if (dmgSource.HitPosition != null)
                {
                    dx = dmgSource.HitPosition.X;
                    dz = dmgSource.HitPosition.Z;
                }
                else if (dmgSource.SourceEntity != null)
                {
                    dx = dmgSource.SourceEntity.Pos.X - player.Entity.Pos.X;
                    dz = dmgSource.SourceEntity.Pos.Z - player.Entity.Pos.Z;
                }
                else if (dmgSource.SourcePos != null)
                {
                    dx = dmgSource.SourcePos.X - player.Entity.Pos.X;
                    dz = dmgSource.SourcePos.Z - player.Entity.Pos.Z;
                }
                else
                {
                    break;
                }

                double attackYaw = Math.Atan2((double)dx, (double)dz);
                double playerYaw = player.Entity.Pos.Yaw + GameMath.PIHALF;

                bool inProtectionRange = Math.Abs(GameMath.AngleRadDistance((float)playerYaw, (float)attackYaw)) < angleProtectionRange;

                if (inProtectionRange && ___api.World.Rand.NextDouble() < chance)
                {
                    damage = Math.Max(0, damage - dmgabsorb);

                    var loc = shieldSlot.Itemstack.ItemAttributes["blockSound"].AsString("held/shieldblock");
                    ___api.World.PlaySoundAt(AssetLocation.Create(loc, shieldSlot.Itemstack.Collectible.Code.Domain).WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg"), player, null);

                    if (___api.Side == EnumAppSide.Server)
                    {
                        shieldSlot.Itemstack.Collectible.DamageItem(___api.World, dmgSource.SourceEntity, shieldSlot, 1);
                        shieldSlot.MarkDirty();
                    }
                }
            }

            if (damage <= 0) { __result = 0; return false; };
            // The code below only the server needs to execute
            if (___api.Side == EnumAppSide.Client) { __result = damage; return false; };

            // Does not protect against non-attack damages

            if (type != EnumDamageType.BluntAttack && type != EnumDamageType.PiercingAttack && type != EnumDamageType.SlashingAttack) { __result = damage; return false; };
            if (dmgSource.Source == EnumDamageSource.Internal || dmgSource.Source == EnumDamageSource.Suicide) { __result = damage; return false; };

            ItemSlot armorSlot;
            IInventory inv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            double rnd = ___api.World.Rand.NextDouble();


            int attackTarget;

            if ((rnd -= 0.2) < 0)
            {
                // Head
                armorSlot = inv[12];
                attackTarget = 0;
            }
            else if ((rnd -= 0.5) < 0)
            {
                // Body
                armorSlot = inv[13];
                attackTarget = 1;
            }
            else
            {
                // Legs
                armorSlot = inv[14];
                attackTarget = 2;
            }

            // Apply full damage if no armor is in this slot
            if (armorSlot.Empty || !(armorSlot.Itemstack.Item is ItemWearable) || armorSlot.Itemstack.Collectible.GetRemainingDurability(armorSlot.Itemstack) <= 0)
            {
                EnumCharacterDressType[] dressTargets = clothingDamageTargetsByAttackTacket[attackTarget];
                EnumCharacterDressType target = dressTargets[___api.World.Rand.Next(dressTargets.Length)];

                ItemSlot targetslot = player.Entity.GearInventory[(int)target];
                if (!targetslot.Empty)
                {
                    // Wolf: 10 hp damage = 10% condition loss
                    // Ram: 10 hp damage = 2.5% condition loss
                    // Bronze locust: 10 hp damage = 5% condition loss
                    float mul = 0.25f;
                    if (type == EnumDamageType.SlashingAttack) mul = 1f;
                    if (type == EnumDamageType.PiercingAttack) mul = 0.5f;

                    float diff = -damage / 100 * mul;

                    if (Math.Abs(diff) > 0.05)
                    {
                        AssetLocation ripSound = new AssetLocation("sounds/effect/clothrip");
                        ___api.World.PlaySoundAt(ripSound, player.Entity);
                    }

                    (targetslot.Itemstack.Collectible as ItemWearable)?.ChangeCondition(targetslot, diff);
                }

                __result = damage;
                return false;
            }

            //We do NOT want to edit item protmods directly, as it is applied globally.
            ProtectionModifiers preProtMods = (armorSlot.Itemstack.Item as ItemWearable).ProtectionModifiers;
            ProtectionModifiers protMods = new ProtectionModifiers();
            if (armorSlot.Itemstack.Attributes.HasAttribute("protectionModifiers"))
            {
                var modModifiers = armorSlot.Itemstack.Attributes.GetTreeAttribute("protectionModifiers");
                if (modModifiers.HasAttribute("protectionTier")) { protMods.ProtectionTier = modModifiers.GetInt("protectionTier"); }
                else { protMods.ProtectionTier = preProtMods.ProtectionTier; }
                if (modModifiers.HasAttribute("highDamageTierResistant")) { protMods.HighDamageTierResistant = modModifiers.GetBool("highDamageTierResistant"); }
                else { protMods.HighDamageTierResistant = preProtMods.HighDamageTierResistant; }
                if (modModifiers.HasAttribute("flatDamageReduction")) { protMods.FlatDamageReduction = modModifiers.GetFloat("flatDamageReduction"); }
                else { protMods.FlatDamageReduction = preProtMods.FlatDamageReduction; }
                if (modModifiers.HasAttribute("relativeProtection")) { protMods.RelativeProtection = modModifiers.GetFloat("relativeProtection"); }
                else { protMods.RelativeProtection = preProtMods.RelativeProtection; }
                //This is temp until arrays r added to api.
                if (modModifiers.GetTreeAttribute("perTierFlatDamageReductionLoss") != null)
                {
                    var modModModifiers = modModifiers.GetTreeAttribute("perTierFlatDamageReductionLoss");
                    if (modModModifiers.HasAttribute("0")) { protMods.PerTierFlatDamageReductionLoss[0] = modModModifiers.GetFloat("0"); }
                    else { protMods.PerTierFlatDamageReductionLoss[0] = preProtMods.PerTierFlatDamageReductionLoss[0]; }
                    if (modModModifiers.HasAttribute("1")) { protMods.PerTierFlatDamageReductionLoss[1] = modModModifiers.GetFloat("1"); }
                    else { protMods.PerTierFlatDamageReductionLoss[1] = preProtMods.PerTierFlatDamageReductionLoss[1]; }
                }
                else { protMods.PerTierFlatDamageReductionLoss = preProtMods.PerTierFlatDamageReductionLoss; }
                if (modModifiers.GetTreeAttribute("perTierRelativeProtectionLoss") != null)
                {
                    var modModModifiers = modModifiers.GetTreeAttribute("perTierRelativeProtectionLoss");
                    if (modModModifiers.HasAttribute("0")) { protMods.PerTierRelativeProtectionLoss[0] = modModModifiers.GetFloat("0"); }
                    else { protMods.PerTierRelativeProtectionLoss[0] = preProtMods.PerTierRelativeProtectionLoss[0]; }
                    if (modModModifiers.HasAttribute("1")) { protMods.PerTierRelativeProtectionLoss[0] = modModModifiers.GetFloat("1"); }
                    else { protMods.PerTierRelativeProtectionLoss[1] = preProtMods.PerTierRelativeProtectionLoss[1]; }
                }
                else { protMods.PerTierRelativeProtectionLoss = preProtMods.PerTierRelativeProtectionLoss; }
            }

            int weaponTier = dmgSource.DamageTier;
            float flatDmgProt = protMods.FlatDamageReduction;
            float percentProt = protMods.RelativeProtection;

            for (int tier = 1; tier <= weaponTier; tier++)
            {
                bool aboveTier = tier > protMods.ProtectionTier;

                float flatLoss = aboveTier ? protMods.PerTierFlatDamageReductionLoss[1] : protMods.PerTierFlatDamageReductionLoss[0];
                float percLoss = aboveTier ? protMods.PerTierRelativeProtectionLoss[1] : protMods.PerTierRelativeProtectionLoss[0];

                if (aboveTier && protMods.HighDamageTierResistant)
                {
                    flatLoss /= 2;
                    percLoss /= 2;
                }

                flatDmgProt -= flatLoss;
                percentProt *= 1 - percLoss;
            }

            // Durability loss is the one before the damage reductions
            float durabilityLoss = 0.5f + damage * Math.Max(0.5f, (weaponTier - protMods.ProtectionTier) * 3);
            int durabilityLossInt = GameMath.RoundRandom(___api.World.Rand, durabilityLoss);

            // Now reduce the damage
            damage = Math.Max(0, damage - flatDmgProt);
            damage *= 1 - Math.Max(0, percentProt);

            armorSlot.Itemstack.Collectible.DamageItem(___api.World, player.Entity, armorSlot, durabilityLossInt);

            if (armorSlot.Empty)
            {
                ___api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
            }
            __result = damage;
            return false;
        }
        private static Dictionary<int, EnumCharacterDressType[]> clothingDamageTargetsByAttackTacket = new Dictionary<int, EnumCharacterDressType[]>()
        {
            { 0, new EnumCharacterDressType[] { EnumCharacterDressType.Head, EnumCharacterDressType.Face, EnumCharacterDressType.Neck } },
            { 1, new EnumCharacterDressType[] { EnumCharacterDressType.UpperBody, EnumCharacterDressType.UpperBodyOver, EnumCharacterDressType.Shoulder, EnumCharacterDressType.Arm, EnumCharacterDressType.Hand } },
            { 2, new EnumCharacterDressType[] { EnumCharacterDressType.LowerBody, EnumCharacterDressType.Foot } }
        };
        [HarmonyPrefix]
        [HarmonyPatch("updateWearableStats"), HarmonyPriority(Priority.Last)]
        public static bool updateWearableStats(IInventory inv, IServerPlayer player)
        {
            StatModifiers allmod = new StatModifiers();

            float walkSpeedmul = player.Entity.Stats.GetBlended("armorWalkSpeedAffectedness");

            foreach (var slot in inv)
            {
                if (slot.Empty || !(slot.Itemstack.Item is ItemWearable)) continue;
                StatModifiers statmod = (slot.Itemstack.Item as ItemWearable).StatModifers;
                if(slot.Itemstack.Attributes.HasAttribute("statModifiers") && statmod == null)
                {
                    StatModifiers modstat = new StatModifiers();
                    modstat.canEat = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetBool("canEat", false);
                    modstat.healingeffectivness = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("healingeffectiveness", 0f);
                    modstat.hungerrate = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("hungerrate", 0f);
                    modstat.rangedWeaponsAcc = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("rangedWeaponsAcc", 0f);
                    modstat.rangedWeaponsSpeed = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("rangedWeaponsSpeed", 0f);
                    modstat.walkSpeed = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("walkspeed", 0f);
                    statmod = modstat;
                } else if (slot.Itemstack.Attributes.HasAttribute("statModifiers") && statmod != null)
                {
                    StatModifiers modstat = new StatModifiers();
                    modstat.canEat = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetBool("canEat", statmod.canEat);
                    modstat.healingeffectivness = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("healingeffectiveness", statmod.healingeffectivness);
                    modstat.hungerrate = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("hungerrate", statmod.hungerrate);
                    modstat.rangedWeaponsAcc = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("rangedWeaponsAcc", statmod.rangedWeaponsAcc);
                    modstat.rangedWeaponsSpeed = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("rangedWeaponsSpeed", statmod.rangedWeaponsSpeed);
                    modstat.walkSpeed = slot.Itemstack.Attributes.GetTreeAttribute("statModifiers").GetFloat("walkspeed", statmod.walkSpeed);
                    statmod = modstat;
                }
                if (statmod == null) continue;

                // No positive effects when broken
                bool broken = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) == 0;

                allmod.canEat &= statmod.canEat;
                allmod.healingeffectivness += broken ? Math.Min(0, statmod.healingeffectivness) : statmod.healingeffectivness;
                allmod.hungerrate += broken ? Math.Max(0, statmod.hungerrate) : statmod.hungerrate;

                if (statmod.walkSpeed < 0)
                {
                    allmod.walkSpeed += statmod.walkSpeed * walkSpeedmul;
                }
                else
                {
                    allmod.walkSpeed += broken ? 0 : statmod.walkSpeed;
                }

                allmod.rangedWeaponsAcc += broken ? Math.Min(0, statmod.rangedWeaponsAcc) : statmod.rangedWeaponsAcc;
                allmod.rangedWeaponsSpeed += broken ? Math.Min(0, statmod.rangedWeaponsSpeed) : statmod.rangedWeaponsSpeed;
            }

            EntityPlayer entity = player.Entity;
            entity.Stats
                .Set("walkspeed", "wearablemod", allmod.walkSpeed, true)
                .Set("healingeffectivness", "wearablemod", allmod.healingeffectivness, true)
                .Set("hungerrate", "wearablemod", allmod.hungerrate, true)
                .Set("rangedWeaponsAcc", "wearablemod", allmod.rangedWeaponsAcc, true)
                .Set("rangedWeaponsSpeed", "wearablemod", allmod.rangedWeaponsSpeed, true)
            ;

            entity.WatchedAttributes.SetBool("canEat", allmod.canEat);
            return false;
        }
    }
}
