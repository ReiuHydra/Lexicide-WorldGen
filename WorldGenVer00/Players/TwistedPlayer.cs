using System;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WorldGenVer00.Players
{
    /// <summary>
    /// 提供"似是非是"子世界内的玩家效果，包括重力微调、
    /// 视野变化、世界边缘传送等，增强"扭曲世界"的沉浸感。
    /// </summary>
    public class TwistedPlayer : ModPlayer
    {
        private const float GravityMultiplier = 0.6f;

        /// <summary>边缘传送触发距离（格）</summary>
        private const int EdgeTeleportDistance = 60;

        /// <summary>边缘传送冷却（ticks，60 ticks = 1 秒）</summary>
        private const int EdgeTeleportCooldownTicks = 300;

        /// <summary>冷却计时器</summary>
        private int _edgeTeleportCooldown = 0;

        public override void PostUpdate()
        {
            // 边缘传送检测（仅在本地玩家执行）
            if (Main.myPlayer == Player.whoAmI)
            {
                CheckEdgeTeleport();

                // 处理挂起的边缘传送（跨世界到达后应用）
                if (TwistedWorldSystem.PendingEdgeTeleport.HasValue)
                {
                    // 进入子世界时：InTwistedSubworld 应为 true
                    // 退出子世界时：InTwistedSubworld 应为 false
                    bool shouldTeleportHere =
                        (TwistedWorldSystem.PendingTeleportTargetIsSubworld && SubworldSystem.IsActive<TwistedSubworld>()) ||
                        (!TwistedWorldSystem.PendingTeleportTargetIsSubworld && !SubworldSystem.IsActive<TwistedSubworld>());

                    if (shouldTeleportHere)
                    {
                        Player.Teleport(TwistedWorldSystem.PendingEdgeTeleport.Value, TeleportationStyleID.RodOfDiscord);
                        TwistedWorldSystem.PendingEdgeTeleport = null;
                        TwistedWorldSystem.PendingTeleportTargetIsSubworld = false;
                        // 应用挂起传送后也设冷却，防止立即反向触发
                        _edgeTeleportCooldown = EdgeTeleportCooldownTicks;
                    }
                }
            }

            if (!SubworldSystem.IsActive<TwistedSubworld>())
                return;

            // 微重力效果（重力略低，跳跃略高）
            Player.gravDir = 1f;
            Player.gravity = Player.gravity * GravityMultiplier;
            Player.jumpSpeedBoost += 0.5f;
            Player.moveSpeed += 0.15f;

            // 添加微弱光效（扭曲世界的光晕效果）
            Lighting.AddLight(Player.Center, 0.1f, 0.05f, 0.15f);
        }

        /// <summary>
        /// 冷却递减，确保每 tick 都
        /// </summary>
        public override void PreUpdate()
        {
            if (_edgeTeleportCooldown > 0)
                _edgeTeleportCooldown--;
        }

        /// <summary>
        /// 检测玩家是否靠近世界边缘，触发跨世界传送。
        /// 主世界边缘 → 进入子世界；子世界边缘 → 返回主世界。
        /// </summary>
        private void CheckEdgeTeleport()
        {
            //Main.NewText("[调试] CheckEdgeTeleport, 冷却=" + _edgeTeleportCooldown + ", IsActive=" + SubworldSystem.IsActive<TwistedSubworld>() + ", maxTilesX=" + Main.maxTilesX, 255, 255, 0);

            if (_edgeTeleportCooldown > 0)
            {
                // 🔥 明确显示因冷却跳过
                // Main.NewText("[调试] 冷却中，跳过边缘检测, 剩余=" + _edgeTeleportCooldown, 200, 150, 0);
                return;
            }

            int tileX = (int)(Player.Center.X / 16);
            int tileY = (int)(Player.Center.Y / 16);

            // 检查是否靠近水平边缘
            bool nearLeftEdge = tileX <= EdgeTeleportDistance;
            bool nearRightEdge = tileX >= Main.maxTilesX - 1 - EdgeTeleportDistance;
            bool nearEdge = nearLeftEdge || nearRightEdge;

            // 🔥 打印玩家当前位置和边缘阈值
            // Main.NewText("[调试] 玩家 tileX=" + tileX + ", tileY=" + tileY + ", nearLeft=" + nearLeftEdge + ", nearRight=" + nearRightEdge + ", 左阈值=" + EdgeTeleportDistance + ", 右阈值=" + (Main.maxTilesX - 1 - EdgeTeleportDistance), 150, 200, 150);

            if (!nearEdge)
            {
                // 🔥 明确显示未触发的原因
                //Main.NewText("[调试] 未靠近边缘, tileX=" + tileX + " 不在边缘范围", 200, 100, 100);
                return;
            }

            // 计算目标世界坐标
            bool currentlyInSubworld = SubworldSystem.IsActive<TwistedSubworld>();

            // Main.NewText("[调试] 边缘检测触发！tileX=" + tileX + ", max=" + Main.maxTilesX + ", 目标世界=" + (currentlyInSubworld ? "主世界" : "子世界"), 100, 200, 100);

            // 目标 X：左边缘 → 对方右边缘，右边缘 → 对方左边缘
            int targetX;
            if (nearLeftEdge)
                targetX = Main.maxTilesX - 2 - EdgeTeleportDistance;
            else
                targetX = EdgeTeleportDistance + 1;

            // 目标 Y：直接用玩家当前 Y，不在源世界 tile 中搜索目标世界的位置
            //（跨世界时双方 tile 数据不互通，FindSafeY 会读到错误的 tile）
            int targetY = tileY;

            // 如果玩家在地狱附近，落到地表附近更合理
            if (targetY > Main.worldSurface * 2)
                targetY = (int)Main.worldSurface;

            // 执行传送
            Vector2 targetPos = new Vector2(targetX * 16 + 8, targetY * 16 + 8);

            _edgeTeleportCooldown = EdgeTeleportCooldownTicks;

            // 向 TwistedWorldSystem 提交传送请求（由 PostUpdateWorld 处理世界转换）
            TwistedWorldSystem.EdgeTeleportRequest = targetPos;
            TwistedWorldSystem.EdgeTeleportRequestIsSubworld = !currentlyInSubworld;
        }
    }
}
