using System;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace WorldGenVer00.Structures
{
    /// <summary>
    /// 结构生成编排器。负责在子世界和主世界放置自定义结构。
    /// 
    /// 子世界调用：GenerateSubworld()  — 后处理阶段，结构放完后做局部平滑
    /// 主世界调用：作为 GenPass 插入 ModifyWorldGenTasks
    /// </summary>
    public class TwistedStructureGen
    {
        /// <summary>
        /// 子世界结构生成。在后处理阶段调用。
        /// </summary>
        public static void GenerateSubworld()
        {
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;
            int surfEnd = (int)Main.worldSurface;

            // ---- 测试：粘贴 test.shstruct 到多处 ----
            int testCount = width / 120;
            for (int i = 0; i < testCount; i++)
            {
                int tx = WorldGen.genRand.Next(30, width - 30);
                int ty = FindSurfaceY(tx, surfEnd);
                if (ty > 0 && ty < surfEnd)
                    StructureTemplates.PasteStructure("test", tx - 5, ty - 5);
            }

            // ---- 散布群系种子物块，确保 ModBiome 检测覆盖 ----
            PlaceBiomeSeeds(width, height, surfEnd);

            // ---- 群系测试：密集群系标记簇，便于验证 ModBiome 检测 ----
            int biomeTestCount = width / 300;
            for (int i = 0; i < biomeTestCount; i++)
            {
                int bx = WorldGen.genRand.Next(40, width - 40);
                int by = WorldGen.genRand.Next((int)(surfEnd * 0.3f), (int)(height * 0.9f));
                // 8×8 高密度标记：琥珀砖+彩虹砖+石砖混合
                for (int dx = -4; dx <= 4; dx++)
                    for (int dy = -4; dy <= 4; dy++)
                        WorldGen.PlaceTile(bx + dx, by + dy,
                            WorldGen.genRand.Next(3) switch { 0 => TileID.AmberStoneBlock, 1 => TileID.StoneSlab, _ => TileID.RainbowBrick },
                            true, true);
            }

            /*
            // ---- 地表结构 ----
            int hutCount = width / 180;
            for (int i = 0; i < hutCount; i++)
            {
                int hx = WorldGen.genRand.Next(50, width - 50);
                int hy = FindSurfaceY(hx, surfEnd);
                if (hy < surfEnd && hy > 0)
                    StructureTemplates.PlaceRuins(hx - 4, hy, 8, 5);
            }

            int shrineCount = width / 250;
            for (int i = 0; i < shrineCount; i++)
            {
                int sx = WorldGen.genRand.Next(30, width - 30);
                int sy = FindSurfaceY(sx, surfEnd);
                if (sy > 0 && sy < surfEnd)
                {
                    StructureTemplates.PlaceShrine(sx, sy);
                    // 结构附近局部平滑
                    LocalSmooth(sx - 3, sy - 4, 7, 5);
                }
            }

            // ---- 地下结构 ----
            int roomCount = width / 300;
            for (int i = 0; i < roomCount; i++)
            {
                int rx = WorldGen.genRand.Next(30, width - 30);
                int ry = WorldGen.genRand.Next((int)(height * 0.4f), (int)(height * 0.85f));
                StructureTemplates.PlaceUndergroundRoom(rx, ry, 4 + WorldGen.genRand.Next(5));
            }

            // ---- 随机物块柱群（地下） ----
            int pillarCount = width / 200;
            for (int i = 0; i < pillarCount; i++)
            {
                int px = WorldGen.genRand.Next(20, width - 20);
                int py = WorldGen.genRand.Next((int)(height * 0.45f), (int)(height * 0.9f));
                int block = WorldGen.genRand.Next(4) switch
                {
                    0 => TileID.Stone,
                    1 => TileID.Marble,
                    2 => TileID.Granite,
                    _ => TileID.Sandstone
                };
                StructureTemplates.PlacePillarCluster(px, py, 3 + WorldGen.genRand.Next(5), block, 5 + WorldGen.genRand.Next(8));
            }
            */
        }

        /// <summary>
        /// 主世界结构调整用的 GenPass。在 ModifyWorldGenTasks 中插入。
        /// </summary>
        public class MainWorldStructurePass : GenPass
        {
            public MainWorldStructurePass() : base("Twisted: Custom Structures", 1f) { }

            protected override void ApplyPass(GenerationProgress progress, Terraria.IO.GameConfiguration config)
            {
                progress.Message = "扭曲结构：异质点缀…";
                // 主世界结构——与子世界不同，更低调
                int width = Main.maxTilesX;
                int height = Main.maxTilesY;

                /*
                // 散布少量祭坛
                int shrineCount = width / 400;
                for (int i = 0; i < shrineCount; i++)
                {
                    int sx = WorldGen.genRand.Next(30, width - 30);
                    int sy = FindSurfaceY(sx, (int)Main.worldSurface);
                    if (sy > 0 && sy < Main.worldSurface)
                        StructureTemplates.PlaceShrine(sx, sy);
                }

                // 地下零星废墟
                int ruinsCount = width / 350;
                for (int i = 0; i < ruinsCount; i++)
                {
                    int rx = WorldGen.genRand.Next(30, width - 30);
                    int ry = WorldGen.genRand.Next((int)(height * 0.5f), (int)(height * 0.9f));
                    StructureTemplates.PlaceRuins(rx - 4, ry, 8, 5);
                }
                */
            }
        }

        // ---- 工具 ----
        private static void PlaceBiomeSeeds(int width, int height, int surfEnd)
        {
            int seedCount = width / 150;
            for (int i = 0; i < seedCount; i++)
            {
                int sx = WorldGen.genRand.Next(10, width - 10);
                int sy = WorldGen.genRand.Next((int)(surfEnd * 0.6f), (int)(height * 0.9f));
                // 3×3 琥珀/彩虹砖块标记
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        WorldGen.PlaceTile(sx + dx, sy + dy,
                            WorldGen.genRand.NextBool(2) ? TileID.AmberStoneBlock : TileID.RainbowBrick,
                            true, true);
            }
        }

        private static int FindSurfaceY(int x, int maxY)
        {
            for (int y = 1; y < maxY; y++)
                if (Main.tile[x, y].HasTile && !Main.tile[x, y - 1].HasTile)
                    return y;
            return maxY / 2;
        }

        private static void LocalSmooth(int x, int y, int w, int h)
        {
            for (int dx = 0; dx < w; dx++)
            {
                for (int dy = 0; dy < h; dy++)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    if (tx < 1 || tx >= Main.maxTilesX - 1 || ty < 1 || ty >= Main.maxTilesY - 1)
                        continue;

                    Tile tile = Main.tile[tx, ty];
                    if (tile.HasTile && !Main.tile[tx, ty - 1].HasTile && !Main.tile[tx, ty + 1].HasTile)
                        WorldGen.KillTile(tx, ty);
                    if (!tile.HasTile && Main.tile[tx, ty - 1].HasTile && Main.tile[tx + 1, ty].HasTile)
                        WorldGen.PlaceTile(tx, ty, TileID.Dirt, true, true);
                }
            }
        }
    }
}
