using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace attributer.src.patch
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(ItemSlotCharacter))]
    internal class ItemSlotCharacterPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("IsDressType"), HarmonyPriority(Priority.Last)]
        public static bool IsDressTypeItemSlot(IItemStack itemstack, EnumCharacterDressType dressType)
        {
            if (itemstack == null || (itemstack.Collectible.Attributes == null && !itemstack.Attributes.HasAttribute("clothescategory"))) return false;

            string stackDressType = itemstack.Collectible.Attributes["clothescategory"].AsString();
            if (itemstack.Attributes.HasAttribute("clothescategory"))
            {
                stackDressType = itemstack.Attributes.GetString("clothescategory");
            }
            return stackDressType != null && dressType.ToString().Equals(stackDressType, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
