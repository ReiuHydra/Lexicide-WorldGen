using System;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace WorldGen
{
    public class TwistedWorldGenPass
    {
        public static void Run()
        {
            TwistedWorldSystem.InTwistedGeneration = true;

            try
            {
                if (Main.ActiveWorldFileData != null && !Main.ActiveWorldFileData.HasCrimson && !Main.ActiveWorldFileData.HasCorruption)
                {
                    WorldGen.crimson = Main.rand.NextBool();
                }

                var progress = new GenerationProgress();
                WorldGen.GenerateWorld((int)(Main.ActiveWorldFileData.Seed ^ 0xDEADBEEF), progress);

                // 后处理
                progress.Message = "似是而非 — 扭曲地形…";
                ClearTrees();
                ReplaceOreWithRandomClusters();
                AddRandomClusters();
                // SurfaceSmooth();
                Structures.TwistedStructureGen.GenerateSubworld();
            }
            finally
            {
                TwistedWorldSystem.InTwistedGeneration = false;
            }
        }

        private static void ClearTrees()
        {
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile)
                    {
                        if (tile.TileType == TileID.Trees ||
                            tile.TileType == TileID.PalmTree ||
                            tile.TileType == TileID.Bamboo)
                        {
                            WorldGen.KillTile(x, y);
                        }
                        else if (tile.TileType == TileID.LivingWood ||
                                 tile.TileType == TileID.LeafBlock)
                        {
                            WorldGen.KillTile(x, y);
                        }
                    }

                    ushort wall = tile.WallType;
                    if (wall == WallID.LivingWood || wall == WallID.LivingWoodUnsafe)
                    {
                        WorldGen.KillWall(x, y);
                    }
                }
            }
        }

        private static void ReplaceOreWithRandomClusters()
        {
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;

            int[] oreTypes = {
                TileID.Copper, TileID.Tin,
                TileID.Iron, TileID.Lead,
                TileID.Silver, TileID.Tungsten,
                TileID.Gold, TileID.Platinum,
                TileID.Demonite, TileID.Crimtane,
                TileID.Meteorite,
                TileID.Hellstone,
                TileID.Chlorophyte,
                TileID.Cobalt, TileID.Palladium,
                TileID.Mythril, TileID.Orichalcum,
                TileID.Adamantite, TileID.Titanium
            };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    foreach (int ore in oreTypes)
                    {
                        if (tile.TileType == ore)
                        {
                            int clusterBlock = PickRandomClusterBlock();
                            PlaceCluster(x, y, clusterBlock, 2 + WorldGen.genRand.Next(5));
                            break;
                        }
                    }
                }
            }
        }

        private static void AddRandomClusters()
        {
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;
            int clusterCount = (width * height) / 8000;

            for (int i = 0; i < clusterCount; i++)
            {
                int cx = WorldGen.genRand.Next(10, width - 10);
                int cy = WorldGen.genRand.Next((int)(height * 0.25f), height - 10);
                int blockType = PickRandomClusterBlock();
                int radius = 2 + WorldGen.genRand.Next(4);
                PlaceCluster(cx, cy, blockType, radius);
            }
        }

        private static void PlaceCluster(int centerX, int centerY, int blockType, int radius)
        {
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x < 0 || x >= width || y < 0 || y >= height)
                        continue;

                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist > radius) continue;
                    if (dist > radius * 0.6f && WorldGen.genRand.NextBool(3)) continue;

                    WorldGen.PlaceTile(x, y, blockType, true, true);
                }
            }
        }

        // ---- 地表局部平滑：将地表物块转为斜坡形态 ----
        private static void SurfaceSmooth()
        {
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;
            int surfEnd = (int)Main.worldSurface; // 游戏定义的地表线

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < surfEnd; y++)
                {
                    if (!Main.tile[x, y].HasTile) continue;
                    if (Main.tile[x, y].Slope != 0) continue;

                    bool airAbove = !Main.tile[x, y - 1].HasTile;
                    if (!airAbove) continue;

                    bool tileLeft = Main.tile[x - 1, y].HasTile;
                    bool tileRight = Main.tile[x + 1, y].HasTile;
                    bool tileBelow = Main.tile[x, y + 1].HasTile;
                    bool airAboveLeft = !Main.tile[x - 1, y - 1].HasTile;
                    bool airAboveRight = !Main.tile[x + 1, y - 1].HasTile;

                    if (tileRight && !tileLeft && airAboveRight)
                        WorldGen.SlopeTile(x, y, 2); // 右上到左下
                    else if (tileLeft && !tileRight && airAboveLeft)
                        WorldGen.SlopeTile(x, y, 1); // 左上到右下
                    else if (tileBelow && !tileLeft && !tileRight)
                        WorldGen.PoundTile(x, y);     // 半砖
                }
            }
        }

        private static int PickRandomClusterBlock()
        {
            int roll = WorldGen.genRand.Next(40);
            return roll switch
            {
                < 5  => TileID.Stone,
                < 8  => TileID.Mud,
                < 11 => TileID.ClayBlock,
                < 14 => TileID.Silt,
                < 17 => TileID.Slush,
                < 20 => TileID.Sand,
                < 22 => TileID.HardenedSand,
                < 24 => TileID.Sandstone,
                < 26 => TileID.SnowBlock,
                < 28 => TileID.IceBlock,
                < 30 => TileID.CrimsonGrass,
                < 32 => TileID.CorruptGrass,
                < 34 => TileID.HallowedGrass,
                < 35 => TileID.Granite,
                < 36 => TileID.Marble,
                < 37 => TileID.AmberStoneBlock,
                < 38 => TileID.RainbowBrick,
                _    => TileID.Stone
            };
        }
    }
}
