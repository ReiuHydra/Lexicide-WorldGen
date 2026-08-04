using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using SubworldLibrary;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace WorldGen
{
    public class TwistedWorldSystem : ModSystem
    {
        internal static bool InTwistedGeneration = false;

        internal static Vector2? PendingEdgeTeleport = null;
        internal static bool PendingTeleportTargetIsSubworld = false;
        internal static Vector2? EdgeTeleportRequest = null;
        internal static bool EdgeTeleportRequestIsSubworld = false;

        private static Hook _genWorldHook;
        private static FieldInfo _passesField;

        private static readonly string[] SkipPassKeywords = {
            "Dungeon", "Jungle Temple", "Temple", "Lihzahrd Altars",
            "Living Trees", "Planting Trees", "Underworld",
            "Settle", "Final Cleanup", "Hellforge", "Smooth"
        };

        public override void Load()
        {
            On_WorldGen.GrowTree += OnGrowTree;
            On_GenPass.Apply += OnPassApply;

            var genType = typeof(WorldGenerator);
            var genMethod = genType.GetMethod("GenerateWorld", BindingFlags.Public | BindingFlags.Instance);
            if (genMethod != null)
            {
                _passesField = genType.GetField("_passes", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_passesField != null)
                    _genWorldHook = new Hook(genMethod, new Action<Action<WorldGenerator, GenerationProgress>, WorldGenerator, GenerationProgress>(GenerateWorldDetour));
            }
        }

        public override void Unload()
        {
            On_WorldGen.GrowTree -= OnGrowTree;
            On_GenPass.Apply -= OnPassApply;
            _genWorldHook?.Dispose();
            _genWorldHook = null;
            InTwistedGeneration = false;
        }

        private static void OnPassApply(On_GenPass.orig_Apply orig, GenPass self, GenerationProgress progress, Terraria.IO.GameConfiguration config)
        {
            orig(self, progress, config);
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            // 仅主世界：追加结构 Pass
            if (!InTwistedGeneration)
            {
                var structPass = new Structures.TwistedStructureGen.MainWorldStructurePass();
                totalWeight += structPass.Weight;
                tasks.Add(structPass);
            }
        }

        private static void GenerateWorldDetour(Action<WorldGenerator, GenerationProgress> orig, WorldGenerator self, GenerationProgress progress)
        {
            if (InTwistedGeneration && _passesField != null)
            {
                var passes = _passesField.GetValue(self) as List<GenPass>;
                if (passes != null)
                    for (int i = passes.Count - 1; i >= 0; i--)
                        if (ContainsAny(passes[i].Name, SkipPassKeywords))
                            passes[i] = new PassLegacy(passes[i].Name, (p, c) => { }, passes[i].Weight);
            }
            orig(self, progress);
        }

        private static int _preGenState = 0; // 0=空闲 1=等待子世界加载 2=等待退回

        public override void PostUpdateWorld()
        {
            // 预生成状态机（首次进入存档时触发，零延迟）
            string sp = Main.WorldPath + System.IO.Path.DirectorySeparatorChar + "Subworlds"
                + System.IO.Path.DirectorySeparatorChar + "WorldGen_TwistedSubworld.twld";

            switch (_preGenState)
            {
                case 0:
                    if (!SubworldSystem.IsActive<TwistedSubworld>() && !System.IO.File.Exists(sp)
                        && Main.netMode != Terraria.ID.NetmodeID.Server)
                    {
                        Main.NewText("[空间扭曲] 正在生成扭曲子世界…", 200, 120, 150);
                        SubworldSystem.Enter<TwistedSubworld>();
                        _preGenState = 1;
                    }
                    if (_preGenState != 0) return;
                    break;

                case 1:
                    if (SubworldSystem.IsActive<TwistedSubworld>())
                    {
                        SubworldSystem.Exit();
                        _preGenState = 2;
                    }
                    return;

                case 2:
                    if (!SubworldSystem.IsActive<TwistedSubworld>())
                        _preGenState = 3;
                    return;
            }

            if (!EdgeTeleportRequest.HasValue) return;
            Vector2 targetPos = EdgeTeleportRequest.Value;
            bool goToSubworld = EdgeTeleportRequestIsSubworld;
            EdgeTeleportRequest = null;
            EdgeTeleportRequestIsSubworld = false;
            PendingEdgeTeleport = targetPos;
            PendingTeleportTargetIsSubworld = goToSubworld;

            if (goToSubworld)
            {
                Main.NewText("[空间扭曲] 你从世界边缘坠入了\"似是而非\"…", 200, 120, 150);
                SubworldSystem.Enter<TwistedSubworld>();
            }
            else
            {
                Main.NewText("[空间扭曲] 你从世界边缘回到了主世界。", 150, 120, 200);
                SubworldSystem.Exit();
            }
        }

        private static bool OnGrowTree(On_WorldGen.orig_GrowTree orig, int i, int j)
        {
            if (InTwistedGeneration) return false;
            return orig(i, j);
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (string kw in keywords)
                if (text.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
