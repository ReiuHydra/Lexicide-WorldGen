using System;
using Microsoft.Xna.Framework;
using StructureHelper.API;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace WorldGenVer00.Structures
{
    /// <summary>
    /// 可复用的结构模板方法。
    /// 每个方法接收坐标和参数，返回实际放置的宽/高。
    /// 新增结构类型只需在此文件中加一个 PlaceXxx() 方法即可。
    /// </summary>
    public static class StructureTemplates
    {
        // ---- 小型地表建筑 ----
        public static void PlaceHut(int x, int y, int width, int height, int wallType, int tileType)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    // 外壳
                    if (dx == 0 || dx == width - 1 || dy == height - 1)
                    {
                        WorldGen.PlaceTile(tx, ty, tileType, true, true);
                        Main.tile[tx, ty].WallType = (ushort)wallType;
                    }
                    else if (dy == 0) // 地板
                    {
                        WorldGen.PlaceTile(tx, ty, tileType, true, true);
                    }
                    else // 内部墙壁
                    {
                        Main.tile[tx, ty].WallType = (ushort)wallType;
                    }
                }
            }
        }

        // ---- 地下房间 ----
        public static void PlaceUndergroundRoom(int x, int y, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy * 0.5f);

                    // 椭圆房间：内部掏空，边界砌墙
                    if (dist < radius * 0.85f)
                    {
                        WorldGen.KillTile(tx, ty);
                        Main.tile[tx, ty].WallType = WallID.Stone;
                    }
                    else if (dist < radius)
                    {
                        if (Main.tile[tx, ty].HasTile)
                            Main.tile[tx, ty].TileType = TileID.Stone;
                        else
                            WorldGen.PlaceTile(tx, ty, TileID.Stone, true, true);
                        Main.tile[tx, ty].WallType = WallID.Stone;
                    }
                }
            }
        }

        // ---- 扭曲石碑/祭坛 ----
        public static void PlaceShrine(int x, int y)
        {
            // 底座
            for (int dx = -2; dx <= 2; dx++)
                WorldGen.PlaceTile(x + dx, y, TileID.StoneSlab, true, true);

            for (int dx = -1; dx <= 1; dx++)
                WorldGen.PlaceTile(x + dx, y - 1, TileID.StoneSlab, true, true);

            // 核心
            WorldGen.PlaceTile(x, y - 2, TileID.DemonAltar, true, true);

            // 顶部光照
            WorldGen.PlaceTile(x, y - 3, TileID.Torches, true, true);
        }

        // ---- 物块柱群 ----
        public static void PlacePillarCluster(int x, int y, int count, int blockType, int maxHeight)
        {
            for (int i = 0; i < count; i++)
            {
                int px = x + WorldGen.genRand.Next(-8, 8);
                int h = 3 + WorldGen.genRand.Next(maxHeight - 2);
                for (int dy = 0; dy < h; dy++)
                {
                    if (WorldGen.genRand.NextBool(4)) continue; // 随机缺口
                    WorldGen.PlaceTile(px, y - dy, blockType, true, true);
                }
            }
        }

        // ---- 扭曲地宫：彩虹砖椭圆 + 岩浆半椭圆核心 ----
        public static void PlaceTwistedCavern(int centerX, int centerY)
        {
            // 外椭圆 100×50，彩虹砖外壳
            int aOuter = 50, bOuter = 25;
            double aOuterSq = aOuter * aOuter, bOuterSq = bOuter * bOuter;

            // 内半椭圆 50×25，只在下半部分（dy >= 0）填岩浆
            int aInner = 25, bInner = 12;
            double aInnerSq = aInner * aInner, bInnerSq = bInner * bInner;

            for (int dx = -aOuter; dx <= aOuter; dx++)
            {
                for (int dy = -bOuter; dy <= bOuter; dy++)
                {
                    double outerDist = (dx * dx) / aOuterSq + (dy * dy) / bOuterSq;
                    if (outerDist > 1.0) continue; // 在椭圆外

                    int tx = centerX + dx;
                    int ty = centerY + dy;
                    if (tx < 0 || tx >= Main.maxTilesX || ty < 0 || ty >= Main.maxTilesY) continue;

                    // 检查是否在内半椭圆（仅下半）→ 填岩浆
                    double innerDist = (dx * dx) / aInnerSq + (dy * dy) / bInnerSq;
                    if (innerDist <= 1.0 && dy >= 0)
                    {
                        WorldGen.KillTile(tx, ty);
                        // 用 Liquid 类写入岩浆
                        WorldGen.PlaceLiquid(tx, ty, 1, 255); // 1=lava
                    }
                    // 在椭圆边缘 → 彩虹砖外壳
                    else if (outerDist > 0.75 || WorldGen.genRand.NextBool(8))
                    {
                        WorldGen.PlaceTile(tx, ty, TileID.RainbowBrick, true, true);
                    }
                    // 内部中空
                    else
                    {
                        WorldGen.KillTile(tx, ty);
                    }
                }
            }
        }

        // ---- 废弃小屋残骸（部分坍毁效果） ----
        public static void PlaceRuins(int x, int y, int width, int height)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    // 随机空缺制造残骸感
                    if (WorldGen.genRand.NextBool(5)) continue;

                    if (dx == 0 || dx == width - 1 || dy == height - 1)
                    {
                        WorldGen.PlaceTile(tx, ty, TileID.GrayBrick, true, true);
                        if (!WorldGen.genRand.NextBool(3))
                            Main.tile[tx, ty].WallType = WallID.Stone;
                    }
                }
            }
        }
        // ---- StructureHelper 粘贴（3.0 API） ----
        /// <summary>
        /// 从 mod 的 Structures 文件夹粘贴 .shstruct 结构文件。
        /// 用法：PasteStructure("test", x, y) → 加载 Structures/test.shstruct
        /// </summary>
        public static bool PasteStructure(string name, int x, int y)
        {
            var mod = ModContent.GetInstance<TwistedWorldSystem>().Mod;
            // path 相对于 mod 根目录，不含扩展名
            StructureHelper.API.Generator.GenerateStructure(
                "Structures/" + name, new Point16(x, y), mod);
            return true;
        }
    }
}
