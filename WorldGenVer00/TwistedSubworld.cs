using System.Collections.Generic;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace WorldGenVer00
{
    public class TwistedSubworld : Subworld
    {
        public override int Width => 4200;
        public override int Height => 1200;

        public override bool ShouldSave => true;

        public override List<GenPass> Tasks
        {
            get
            {
                return new List<GenPass>();
            }
        }

        public override void OnEnter()
        {
        }

        public override void OnExit()
        {
        }

        public override void OnLoad()
        {
            // 仅在首次生成时运行（空世界无物块）；加载存档时跳过
            if (!Main.tile[Main.maxTilesX / 2, Main.maxTilesY / 2].HasTile)
                TwistedWorldGenPass.Run();
        }
    }
}
