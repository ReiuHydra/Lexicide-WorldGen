using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WorldGen.Biomes
{
    /// <summary>
    /// 扭曲地表群系——子世界地表。由我们的结构物块标记激活。
    /// </summary>
    public class TwistedSurfaceBiome : ModBiome
    {
        private static int _debugCooldown = 0;

        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override int Music => MusicID.Eerie;
        public override string MapBackground => "TwistedSurface";
        public override Color? BackgroundColor => new Color(40, 20, 50);

        public override bool IsBiomeActive(Player player)
        {
            int count = 0;
            int r = 40;
            int cx = (int)(player.Center.X / 16);
            int cy = (int)(player.Center.Y / 16);

            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    if (IsBiomeTile(tile.TileType)) count++;
                }
            }

            bool active = count >= 25;
            if (active && --_debugCooldown <= 0)
            {
                _debugCooldown = 180;
                Main.NewText($"[群系调试] TwistedSurface 激活！周围 {count} 个标记物块", 180, 120, 220);
            }
            if (!active) _debugCooldown = 0;
            return active;
        }

        /// <summary>
        /// 标记为扭曲群系的物块类型。由 StructureTemplates 放置的结构会自动激活群系。
        /// </summary>
        public static bool IsBiomeTile(int type)
        {
            return type == TileID.StoneSlab ||
                   type == TileID.GrayBrick ||
                   type == TileID.RainbowBrick ||
                   type == TileID.AmberStoneBlock ||
                   type == TileID.Marble ||       // 地下柱群
                   type == TileID.Granite;        // 地下柱群
        }
    }

    /// <summary>
    /// 扭曲地下群系——子世界地下。检测地下结构物块。
    /// </summary>
    public class TwistedUndergroundBiome : ModBiome
    {
        private static int _debugCooldown = 0;

        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override int Music => MusicID.Eerie;
        public override string MapBackground => "TwistedUnderground";
        public override Color? BackgroundColor => new Color(15, 10, 30);

        public override bool IsBiomeActive(Player player)
        {
            if (player.Center.Y / 16 < Main.worldSurface) return false;

            int count = 0;
            int r = 50;
            int cx = (int)(player.Center.X / 16);
            int cy = (int)(player.Center.Y / 16);

            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    if (TwistedSurfaceBiome.IsBiomeTile(tile.TileType)) count++;
                }
            }

            bool active = count >= 30;
            if (active && --_debugCooldown <= 0)
            {
                _debugCooldown = 180;
                Main.NewText($"[群系调试] TwistedUnderground 激活！周围 {count} 个标记物块", 180, 120, 220);
            }
            if (!active) _debugCooldown = 0;
            return active;
        }
    }

    /// <summary>
    /// 扭曲主世界群系——主世界中少量分布。
    /// </summary>
    public class TwistedMainWorldBiome : ModBiome
    {
        private static int _debugCooldown = 0;

        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;

        public override int Music => MusicID.Eerie;
        public override string MapBackground => "TwistedMain";
        public override Color? BackgroundColor => new Color(25, 15, 35);

        public override bool IsBiomeActive(Player player)
        {
            int count = 0;
            int r = 30;
            int cx = (int)(player.Center.X / 16);
            int cy = (int)(player.Center.Y / 16);

            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    if (TwistedSurfaceBiome.IsBiomeTile(tile.TileType)) count++;
                }
            }

            bool active = count >= 15;
            if (active && --_debugCooldown <= 0)
            {
                _debugCooldown = 180;
                Main.NewText($"[群系调试] TwistedMainWorld 激活！周围 {count} 个标记物块", 180, 120, 220);
            }
            if (!active) _debugCooldown = 0;
            return active;
        }
    }
}
