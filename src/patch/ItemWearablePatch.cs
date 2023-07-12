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
    [HarmonyPatch(typeof(ItemWearable))]
    internal class ItemWearablePatch
    {
        //ItemWearable
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo"), HarmonyPriority(Priority.Last)]
        public static void GetHeldItemInfoItemWearable(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo, ref ProtectionModifiers ___ProtectionModifiers, ref StatModifiers ___StatModifers)
        {
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack.Attributes != null && itemstack.Attributes.HasAttribute("protectionModifiers"))
            {
                string replaceThis;
                string withThis;
                //If we need to replace string then replace.
                //This is debug info but whatever.
                if (itemstack.Attributes.HasAttribute("clothescategory"))
                {
                    replaceThis = Lang.Get("Cloth Category: {0}", Lang.Get("clothcategory-" + inSlot.Itemstack.ItemAttributes["clothescategory"].AsString()));
                    withThis = Lang.Get("Cloth Category: {0}", Lang.Get("clothcategory-" + itemstack.Attributes["clothescategory"].ToString()));
                    dsc.Replace(replaceThis, withThis);
                }
                if (itemstack.Attributes.HasAttribute("protectionModifiers") && ___ProtectionModifiers != null)
                {
                    var modModifiers = itemstack.Attributes.GetTreeAttribute("protectionModifiers");
                    if (modModifiers.HasAttribute("protectionTier"))
                    {
                        replaceThis = Lang.Get("Protection tier: {0}", ___ProtectionModifiers.ProtectionTier);
                        withThis = Lang.Get("Protection tier: {0}", modModifiers.GetInt("protectionTier"));
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("flatDamageReduction"))
                    {
                        replaceThis = Lang.Get("Flat damage reduction: {0} hp", ___ProtectionModifiers.FlatDamageReduction);
                        withThis = Lang.Get("Flat damage reduction: {0} hp", modModifiers.GetFloat("flatDamageReduction"));
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("relativeProtection"))
                    {
                        replaceThis = Lang.Get("Percent protection: {0}%", (int)(100 * ___ProtectionModifiers.RelativeProtection));
                        withThis = Lang.Get("Percent protection: {0}%", (int)(100 * modModifiers.GetFloat("relativeProtection")));
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("highDamageTierResistant"))
                    {
                        if (___ProtectionModifiers?.HighDamageTierResistant == true && modModifiers.GetBool("highDamageTierResistant") == false)
                        {
                            //Remove basically.
                            dsc.Replace("<font color=\"#86aad0\">" + Lang.Get("High damage tier resistant") + "</font> " + Lang.Get("When damaged by a higher tier attack, the loss of protection is only half as much."), "");
                        }
                        if (___ProtectionModifiers?.HighDamageTierResistant == false && modModifiers.GetBool("highDamageTierResistant") == true)
                        {
                            //Maybe use dsc.replace to put it above the warmth line, so it matches vanilla's description layout.
                            dsc.Append("<font color=\"#86aad0\">" + Lang.Get("High damage tier resistant") + "</font> " + Lang.Get("When damaged by a higher tier attack, the loss of protection is only half as much."));
                        }
                    }
                    //Megamind.jpg captioned "No arrays?"
                    //if (modModifiers.GetTreeAttribute("perTierFlatDamageReductionLoss") != null) {  }
                    //if (modModifiers.GetTreeAttribute("perTierRelativeProtectionLoss") != null) {  }
                }
                AttributedProtectionModifierInfo(itemstack, dsc, ___ProtectionModifiers);
                if (itemstack.Attributes.HasAttribute("statModifiers") && ___StatModifers != null)
                {
                    var modModifiers = itemstack.Attributes.GetTreeAttribute("statModifiers");
                    if (modModifiers.HasAttribute("healingeffectiveness"))
                    {
                        replaceThis = Lang.Get("Healing effectivness: {0}%", (int)(100 * ___StatModifers.healingeffectivness));
                        withThis = Lang.Get("Healing effectivness: {0}%", (int)(100 * modModifiers.GetFloat("healingeffectiveness")));
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("hungerrate"))
                    {
                        replaceThis = Lang.Get("Hunger rate: {1}{0}%", (int)(100 * ___StatModifers.hungerrate), ___StatModifers.hungerrate > 0 ? "+" : "");
                        withThis = Lang.Get("Hunger rate: {1}{0}%", (int)(100 * modModifiers.GetFloat("hungerrate")), modModifiers.GetFloat("hungerrate") > 0 ? "+" : "");
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("rangedWeaponsAcc"))
                    {
                        replaceThis = Lang.Get("Ranged Weapon Accuracy: {1}{0}%", (int)(100 * ___StatModifers.rangedWeaponsAcc), ___StatModifers.rangedWeaponsAcc > 0 ? "+" : "");
                        withThis = Lang.Get("Ranged Weapon Accuracy: {1}{0}%", (int)(100 * modModifiers.GetFloat("rangedWeaponsAcc")), modModifiers.GetFloat("rangedWeaponsAcc") > 0 ? "+" : "");
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("rangedWeaponsSpeed"))
                    {
                        replaceThis = Lang.Get("Ranged Weapon Charge Time: {1}{0}%", -(int)(100 * ___StatModifers.rangedWeaponsSpeed), -___StatModifers.rangedWeaponsSpeed > 0 ? "+" : "");
                        withThis = Lang.Get("Ranged Weapon Charge Time: {1}{0}%", -(int)(100 * modModifiers.GetFloat("rangedWeaponsSpeed")), -modModifiers.GetFloat("rangedWeaponsSpeed") > 0 ? "+" : "");
                        dsc.Replace(replaceThis, withThis);
                    }
                    if (modModifiers.HasAttribute("walkSpeed"))
                    {
                        replaceThis = Lang.Get("Walk speed: {1}{0}%", (int)(100 * ___StatModifers.walkSpeed), ___StatModifers.walkSpeed > 0 ? "+" : "");
                        withThis = Lang.Get("Walk speed: {1}{0}%", (int)(100 * modModifiers.GetFloat("walkspeed")), modModifiers.GetFloat("walkspeed") > 0 ? "+" : "");
                        dsc.Replace(replaceThis, withThis);
                    }
                }
                AttributedStatModifierInfo(itemstack, dsc, ___StatModifers);
                AttributedProtectionModifierInfoPost(itemstack, dsc, ___StatModifers, ___ProtectionModifiers);
                if (itemstack.Attributes.HasAttribute("warmth")) //GetWarmth exists, but this also uses attributes directly LOL
                {
                    float maxWarmth = inSlot.Itemstack.ItemAttributes?["warmth"].AsFloat(0) ?? 0;
                    replaceThis = Lang.Get("clothing-maxwarmth", maxWarmth);
                    if(dsc.ToString().Contains(replaceThis)) {
                        maxWarmth = inSlot.Itemstack.Attributes.GetFloat("warmth");
                        withThis = Lang.Get("clothing-maxwarmth", maxWarmth);
                        dsc.Replace(replaceThis, withThis);
                    }
                    else
                    {
                        string appendedWarmth = Lang.Get("clothing-maxwarmth", inSlot.Itemstack.Attributes.GetFloat("warmth"));
                        if (!dsc.ToString().Contains(appendedWarmth))
                        {
                            dsc.Append(appendedWarmth);
                        }
                    }
                } 
            }
        }
        //This is to make it so StatModifiers and ProtectionModifiers not only not crash when put on clothes,
        //But also it allows them to appear in the description. Now we can have a +1 Bangle of Protection 'n such.
        //Although, it probably won't be positioned correctly, this is done this way for maximum compatibility.
        private static void AttributedProtectionModifierInfo (ItemStack itemstack, StringBuilder dsc, ProtectionModifiers ___ProtectionModifiers)
        {
            if (itemstack.Attributes.HasAttribute("protectionModifiers") && ___ProtectionModifiers == null)
            {
                //dscString is a hacky fix for repeating appends.
                //Dunno why, but appends would trigger 4 times.
                string dscString = dsc.ToString();
                string modString;
                var modModifiers = itemstack.Attributes.GetTreeAttribute("protectionModifiers");
                if (modModifiers.HasAttribute("flatDamageReduction"))
                {
                    modString = Lang.Get("Flat damage reduction: {0} hp", modModifiers.GetFloat("flatDamageReduction")) + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
                if (modModifiers.HasAttribute("relativeProtection"))
                {
                    modString = Lang.Get("Percent protection: {0}%", (int)(100 * modModifiers.GetFloat("relativeProtection"))) + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
                if (modModifiers.HasAttribute("protectionTier"))
                {
                    modString = Lang.Get("Protection tier: {0}", modModifiers.GetInt("protectionTier")) + "\n";
                    if(!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
            }
        }
        //The game puts the highTierDamageReduction message after stat modifiers.
        private static void AttributedProtectionModifierInfoPost(ItemStack itemStack, StringBuilder dsc, StatModifiers ___StatModifiers, ProtectionModifiers ___ProtectionModifiers)
        {
            if(___ProtectionModifiers == null && ___StatModifiers == null && itemStack.Attributes.HasAttribute("protectionModifiers") && itemStack.Attributes.HasAttribute("statModifiers"))
            {
                var modModifiers = itemStack.Attributes.GetTreeAttribute("protectionModifiers");
                if (modModifiers.HasAttribute("highDamageTierResistant"))
                {
                    if (modModifiers.GetBool("highDamageTierResistant") == true)
                    {
                        string modString;
                        string dscString = dsc.ToString();
                        modString = "<font color=\"#86aad0\">" + Lang.Get("High damage tier resistant") + "</font> " + Lang.Get("When damaged by a higher tier attack, the loss of protection is only half as much.") + "\n";
                        if (!dscString.Contains(modString))
                        {
                            dsc.Append(modString);
                        }
                    }
                }
            }
        }
        private static void AttributedStatModifierInfo(ItemStack itemstack, StringBuilder dsc, StatModifiers ___StatModifiers)
        {
            //dscString is a hacky fix for repeating appends.
            //Dunno why, but appends would trigger 4 times.
            string dscString = dsc.ToString();
            string modString;
            if (itemstack.Attributes.HasAttribute("statModifiers") && ___StatModifiers == null)
            {
                dsc.Append("\n");
                var modModifiers = itemstack.Attributes.GetTreeAttribute("statModifiers");
                if (modModifiers.HasAttribute("healingeffectiveness"))
                {
                    modString = Lang.Get("Healing effectivness: {0}%", (int)(100 * modModifiers.GetFloat("healingeffectiveness")) > 0 ? "+" : "") + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
                if (modModifiers.HasAttribute("hungerrate"))
                {
                    modString = Lang.Get("Hunger rate: {1}{0}%", (int)(100 * modModifiers.GetFloat("hungerrate")), modModifiers.GetFloat("hungerrate") > 0 ? "+" : "") + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
                if (modModifiers.HasAttribute("rangedWeaponsAcc"))
                {
                    modString = Lang.Get("Ranged Weapon Accuracy: {1}{0}%", (int)(100 * modModifiers.GetFloat("rangedWeaponsAcc")), modModifiers.GetFloat("rangedWeaponsAcc") > 0 ? "+" : "") + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
                if (modModifiers.HasAttribute("rangedWeaponsSpeed"))
                {
                    modString = Lang.Get("Ranged Weapon Charge Time: {1}{0}%", -(int)(100 * modModifiers.GetFloat("rangedWeaponsSpeed")), -modModifiers.GetFloat("rangedWeaponsSpeed") > 0 ? "+" : "") + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
                if (modModifiers.HasAttribute("walkSpeed"))
                {
                    modString = Lang.Get("Walk speed: {1}{0}%", (int)(100 * modModifiers.GetFloat("walkspeed")), modModifiers.GetFloat("walkspeed") > 0 ? "+" : "") + "\n";
                    if (!dscString.Contains(modString))
                    {
                        dsc.Append(modString);
                    }
                }
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetWarmth"), HarmonyPriority(Priority.Last)]
        public static void GetWarmthItemWearable(ItemSlot inslot, ref float __result)
        {
            if (inslot.Itemstack.Attributes.HasAttribute("warmth"))
            {
                ensureConditionExists(inslot);
                float warmth = inslot.Itemstack.Attributes.GetFloat("warmth", 0);
                float con = inslot.Itemstack.Attributes.GetFloat("condition", 1);
                __result = Math.Min(warmth, con * 2 * warmth);
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch("ensureConditionExists"), HarmonyPriority(Priority.Last)]
        public static bool ensureConditionExists(ItemSlot slot)
        {
            // Prevent derp in the handbook
            if (slot is DummySlot) return false;

            if (!slot.Itemstack.Attributes.HasAttribute("condition") && attributerCoreSystem.sapi.Side == EnumAppSide.Server)
            {
                if ((slot.Itemstack.ItemAttributes?["warmth"].Exists == true && slot.Itemstack.ItemAttributes?["warmth"].AsFloat() != 0) || (slot.Itemstack.Attributes.HasAttribute("warmth") == true && slot.Itemstack.Attributes.GetFloat("warmth") != 0))
                {
                    if (slot is ItemSlotTrade)
                    {
                        slot.Itemstack.Attributes.SetFloat("condition", (float)attributerCoreSystem.sapi.World.Rand.NextDouble() * 0.25f + 0.75f);
                    }
                    else
                    {
                        slot.Itemstack.Attributes.SetFloat("condition", (float)attributerCoreSystem.sapi.World.Rand.NextDouble() * 0.4f);
                    }

                    slot.MarkDirty();
                }
            }
            return false;
        }
    }
}
