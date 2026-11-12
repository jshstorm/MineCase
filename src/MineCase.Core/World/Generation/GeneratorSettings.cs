using System;
using MineCase.Block;
using Orleans;

namespace MineCase.World.Generation
{
    [GenerateSerializer]
    public class GeneratorSettings
    {
        [Id(0)]
        public bool GenerateStructure { get; set; } = true;

        [Id(1)]
        public bool UseCaves { get; set; } = true;

        [Id(2)]
        public bool UseRavines { get; set; } = true;

        [Id(3)]
        public bool UseMineShafts { get; set; } = true;

        [Id(4)]
        public bool UseVillages { get; set; } = true;

        [Id(5)]
        public bool UseStrongholds { get; set; } = true;

        [Id(6)]
        public bool UseTemples { get; set; } = true;

        [Id(7)]
        public bool UseMonuments { get; set; } = true;

        [Id(8)]
        public bool UseMansions { get; set; } = true;

        [Id(9)]
        public int SeaLevel { get; set; } = 63;

        [Id(10)]
        public float DepthNoiseScaleX { get; set; } = 200.0F;

        [Id(11)]
        public float DepthNoiseScaleY { get; set; } = 200.0F;

        [Id(12)]
        public float DepthNoiseScaleZ { get; set; } = 200.0F;

        [Id(13)]
        public float CoordinateScale { get; set; } = 0.05F; // mc = 684.412F;0.05

        [Id(14)]
        public float HeightScale { get; set; } = 0.05F; // mc = 684.412F;0.05

        [Id(15)]
        public float MainNoiseScaleX { get; set; } = 80.0F;

        [Id(16)]
        public float MainNoiseScaleY { get; set; } = 160.0F;

        [Id(17)]
        public float MainNoiseScaleZ { get; set; } = 80.0F;

        [Id(18)]
        public float BiomeDepthOffSet { get; set; }

        [Id(19)]
        public float BiomeDepthWeight { get; set; } = 1.0F;

        [Id(20)]
        public float BiomeScaleOffset { get; set; }

        [Id(21)]
        public float BiomeScaleWeight { get; set; } = 1.0F;

        [Id(22)]
        public float BaseSize { get; set; } = 8.5F;

        [Id(23)]
        public float StretchY { get; set; } = 12.0F;

        [Id(24)]
        public float LowerLimitScale { get; set; } = 512.0F;

        [Id(25)]
        public float UpperLimitScale { get; set; } = 512.0F;

        [Id(26)]
        public int BiomeSize { get; set; } = 4;

        [Id(27)]
        public int RiverSize { get; set; } = 4;

        // ores like
        [Id(28)]
        public int DirtSize { get; set; } = 33;

        [Id(29)]
        public int DirtCount { get; set; } = 10;

        [Id(30)]
        public int DirtMinHeight { get; set; } = 0;

        [Id(31)]
        public int DirtMaxHeight { get; set; } = 256;

        [Id(32)]
        public int GravelSize { get; set; } = 33;

        [Id(33)]
        public int GravelCount { get; set; } = 8;

        [Id(34)]
        public int GravelMinHeight { get; set; } = 0;

        [Id(35)]
        public int GravelMaxHeight { get; set; } = 256;

        [Id(36)]
        public int GraniteSize { get; set; } = 33;

        [Id(37)]
        public int GraniteCount { get; set; } = 10;

        [Id(38)]
        public int GraniteMinHeight { get; set; } = 0;

        [Id(39)]
        public int GraniteMaxHeight { get; set; } = 80;

        [Id(40)]
        public int DioriteSize { get; set; } = 33;

        [Id(41)]
        public int DioriteCount { get; set; } = 10;

        [Id(42)]
        public int DioriteMinHeight { get; set; } = 0;

        [Id(43)]
        public int DioriteMaxHeight { get; set; } = 80;

        [Id(44)]
        public int AndesiteSize { get; set; } = 33;

        [Id(45)]
        public int AndesiteCount { get; set; } = 10;

        [Id(46)]
        public int AndesiteMinHeight { get; set; } = 0;

        [Id(47)]
        public int AndesiteMaxHeight { get; set; } = 80;

        // ores
        [Id(48)]
        public int CoalSize { get; set; } = 17;

        [Id(49)]
        public int CoalCount { get; set; } = 20;

        [Id(50)]
        public int CoalMinHeight { get; set; } = 0;

        [Id(51)]
        public int CoalMaxHeight { get; set; } = 128;

        [Id(52)]
        public int IronSize { get; set; } = 9;

        [Id(53)]
        public int IronCount { get; set; } = 20;

        [Id(54)]
        public int IronMinHeight { get; set; } = 0;

        [Id(55)]
        public int IronMaxHeight { get; set; } = 64;

        [Id(56)]
        public int GoldSize { get; set; } = 9;

        [Id(57)]
        public int GoldCount { get; set; } = 2;

        [Id(58)]
        public int GoldMinHeight { get; set; } = 0;

        [Id(59)]
        public int GoldMaxHeight { get; set; } = 32;

        [Id(60)]
        public int RedstoneSize { get; set; } = 8;

        [Id(61)]
        public int RedstoneCount { get; set; } = 8;

        [Id(62)]
        public int RedstoneMinHeight { get; set; } = 0;

        [Id(63)]
        public int RedstoneMaxHeight { get; set; } = 16;

        [Id(64)]
        public int DiamondSize { get; set; } = 8;

        [Id(65)]
        public int DiamondCount { get; set; } = 1;

        [Id(66)]
        public int DiamondMinHeight { get; set; } = 0;

        [Id(67)]
        public int DiamondMaxHeight { get; set; } = 16;

        [Id(68)]
        public int LapisSize { get; set; } = 7;

        [Id(69)]
        public int LapisCount { get; set; } = 1;

        [Id(70)]
        public int LapisCenterHeight { get; set; } = 16;

        [Id(71)]
        public int LapisSpread { get; set; } = 16;

        [Id(72)]
        public BlockState?[] FlatBlockId { get; set; } =
            new BlockState?[] { BlockStates.Bedrock(), BlockStates.Stone(), BlockStates.Dirt(), BlockStates.GrassBlock() };

        // public FlatGeneratorInfo FlatGeneratorInfo { get; set; }

        // public OverworldGeneratorInfo OverworldGeneratorInfo { get; set; }
    }
}
