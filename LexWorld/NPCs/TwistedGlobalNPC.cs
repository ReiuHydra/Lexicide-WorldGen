using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using SubworldLibrary;

namespace LexWorld.NPCs
{
    /// <summary>
    /// 调整"似是非是"子世界内的 NPC 生成速率和强度。
    ///
    /// 生成管线：
    /// 1. EditSpawnRate —— 将生成速率降低至 40%（更少怪物）
    /// 2. EditSpawnRange —— 缩小生成范围（怪物从更近处出现，增加压迫感）
    /// 3. OnSpawn / PostAI —— 替换/增强已生成的怪物（与 TwistedWorldSystem 的 On_ 钩子互补）
    /// </summary>
    public class TwistedGlobalNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (!SubworldSystem.IsActive<TwistedSubworld>())
                return;

            // 降低生成速率（数值越大，生成越慢）
            spawnRate = (int)(spawnRate * 2.5f);    // 2.5x 生成间隔

            // 减少最大同时存在 NPC 数量
            maxSpawns = (int)(maxSpawns * 0.4f);    // 最多 40% 的 NPC
        }

        public override void EditSpawnRange(Player player, ref int spawnRangeX, ref int spawnRangeY, ref int safeRangeX, ref int safeRangeY)
        {
            if (!SubworldSystem.IsActive<TwistedSubworld>())
                return;

            // 缩小安全范围（怪物可以从更近处生成）
            safeRangeX = (int)(safeRangeX * 0.6f);
            safeRangeY = (int)(safeRangeY * 0.6f);

            // 略微扩大生成范围（怪物从稍远处出现）
            spawnRangeX = (int)(spawnRangeX * 1.2f);
            spawnRangeY = (int)(spawnRangeY * 1.2f);
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (!SubworldSystem.IsActive<TwistedSubworld>())
                return;

            // 不替换城镇 NPC、Boss 等特殊 NPC
            if (npc.townNPC || npc.boss || npc.type == Terraria.ID.NPCID.None)
                return;

            // 尝试替换为更强变种
            int newType = GetUpgradedNPCType(npc.type);
            if (newType != npc.type)
            {
                float oldX = npc.position.X;
                float oldY = npc.position.Y;

                npc.SetDefaults(newType);
                npc.position.X = oldX;
                npc.position.Y = oldY;

                // 增强属性：2x 生命、1.5x 伤害、1.2x 防御
                npc.lifeMax = (int)(npc.lifeMax * 2.0f);
                npc.life = npc.lifeMax;
                npc.damage = (int)(npc.damage * 1.5f);
                npc.defense = (int)(npc.defense * 1.2f);

                // 增大尺寸（视觉上更具压迫感）
                npc.scale *= 1.2f;
            }
        }

        /// <summary>
        /// 获取更强怪物变种的映射表。
        /// 将前期怪物替换为中期怪物，中期替换为后期。
        /// </summary>
        private static int GetUpgradedNPCType(int originalType)
        {
            switch (originalType)
            {
                // 史莱姆升级链
                case Terraria.ID.NPCID.GreenSlime:
                    return Terraria.ID.NPCID.JungleSlime;
                case Terraria.ID.NPCID.BlueSlime:
                    return Terraria.ID.NPCID.IceSlime;
                case Terraria.ID.NPCID.RedSlime:
                    return Terraria.ID.NPCID.ToxicSludge;
                case Terraria.ID.NPCID.Pinky:
                    return Terraria.ID.NPCID.DungeonSlime;
                // 僵尸升级链
                case Terraria.ID.NPCID.Zombie:
                    return Terraria.ID.NPCID.BaldZombie;
                case Terraria.ID.NPCID.BaldZombie:
                    return Terraria.ID.NPCID.BloodZombie;
                // 眼球升级链
                case Terraria.ID.NPCID.DemonEye:
                    return Terraria.ID.NPCID.CataractEye;
                case Terraria.ID.NPCID.CataractEye:
                    return Terraria.ID.NPCID.DemonEye2;
                // 邪恶阵营升级
                case Terraria.ID.NPCID.EaterofSouls:
                    return Terraria.ID.NPCID.Crimera;
                case Terraria.ID.NPCID.Crimera:
                    return Terraria.ID.NPCID.FaceMonster;
                // 骷髅升级
                case Terraria.ID.NPCID.Skeleton:
                    return Terraria.ID.NPCID.ArmoredSkeleton;
                // 蝙蝠升级链
                case Terraria.ID.NPCID.CaveBat:
                    return Terraria.ID.NPCID.JungleBat;
                case Terraria.ID.NPCID.JungleBat:
                    return Terraria.ID.NPCID.GiantBat;
                // 其他升级
                case Terraria.ID.NPCID.BloodZombie:
                    return Terraria.ID.NPCID.Drippler;
                case Terraria.ID.NPCID.Harpy:
                    return Terraria.ID.NPCID.WyvernHead;
                default:
                    return originalType;
            }
        }
    }
}
