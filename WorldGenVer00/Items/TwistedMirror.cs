using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WorldGenVer00.Items
{
    /// <summary>
    /// "扭曲魔镜" —— 在主世界使用进入"似是非是"子世界，
    /// 在子世界使用返回主世界。每次使用都会触发子世界重新生成（SaveSubworld = false）。（此处为旧代码，存疑）
    /// </summary>
    public class TwistedMirror : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Blue;
            Item.maxStack = 1;

            // 不可消耗，可重复使用
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return false;

            // 判断是否已在子世界中
            if (SubworldSystem.IsActive<TwistedSubworld>())
            {
                // 从子世界返回主世界
                SubworldSystem.Exit();
                Main.NewText("[扭曲消退] 你回到了主世界。", 150, 120, 200);
            }
            else
            {
                // 进入子世界
                SubworldSystem.Enter<TwistedSubworld>();
                Main.NewText("[空间扭曲] 你进入了\"似是非是\"…", 200, 120, 150);
            }

            return true;
        }

        public override void AddRecipes()
        {
            // 临时：土块合成，方便测试子世界功能
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
