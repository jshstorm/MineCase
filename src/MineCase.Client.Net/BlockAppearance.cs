using System;
using System.Collections.Generic;
using System.Numerics;
using MineCase.Block;

namespace MineCase.Client.Net
{
    public enum BlockShape
    {
        None,
        Cube,
        Cross,
    }

    /// <summary>
    /// How a block is drawn: flat colours per block type instead of textures.
    /// </summary>
    public readonly struct BlockAppearance
    {
        private static readonly Vector4 StoneColor = Rgb(0.49f, 0.49f, 0.49f);
        private static readonly Vector4 DirtColor = Rgb(0.53f, 0.38f, 0.26f);
        private static readonly Vector4 GrassColor = Rgb(0.37f, 0.62f, 0.25f);
        private static readonly Vector4 LeavesColor = Rgb(0.22f, 0.47f, 0.16f);
        private static readonly Vector4 LogSideColor = Rgb(0.40f, 0.31f, 0.19f);
        private static readonly Vector4 LogTopColor = Rgb(0.62f, 0.50f, 0.31f);
        private static readonly Vector4 PlanksColor = Rgb(0.67f, 0.54f, 0.33f);
        private static readonly Vector4 PlantColor = Rgb(0.33f, 0.58f, 0.22f);

        // Longer names first so "LightBlue" is not read as "Blue".
        private static readonly (string Name, Vector4 Color)[] DyeColors =
        {
            ("LightBlue", Rgb(0.43f, 0.65f, 0.85f)),
            ("LightGray", Rgb(0.62f, 0.62f, 0.60f)),
            ("White", Rgb(0.92f, 0.92f, 0.92f)),
            ("Orange", Rgb(0.92f, 0.52f, 0.17f)),
            ("Magenta", Rgb(0.75f, 0.30f, 0.75f)),
            ("Yellow", Rgb(0.93f, 0.82f, 0.20f)),
            ("Lime", Rgb(0.45f, 0.75f, 0.18f)),
            ("Pink", Rgb(0.93f, 0.55f, 0.67f)),
            ("Gray", Rgb(0.30f, 0.30f, 0.32f)),
            ("Cyan", Rgb(0.10f, 0.55f, 0.58f)),
            ("Purple", Rgb(0.45f, 0.20f, 0.65f)),
            ("Blue", Rgb(0.20f, 0.25f, 0.65f)),
            ("Brown", Rgb(0.45f, 0.30f, 0.18f)),
            ("Green", Rgb(0.33f, 0.43f, 0.18f)),
            ("Red", Rgb(0.65f, 0.18f, 0.16f)),
            ("Black", Rgb(0.10f, 0.10f, 0.12f)),
        };

        private static readonly Dictionary<BlockId, BlockAppearance> Table = BuildTable();

        public BlockAppearance(BlockShape shape, Vector4 topColor, Vector4 sideColor, bool solid, bool occludes)
        {
            Shape = shape;
            TopColor = topColor;
            SideColor = sideColor;
            Solid = solid;
            Occludes = occludes;
        }

        public BlockShape Shape { get; }

        public Vector4 TopColor { get; }

        public Vector4 SideColor { get; }

        /// <summary>
        /// Gets a value indicating whether the player collides with the block.
        /// </summary>
        public bool Solid { get; }

        /// <summary>
        /// Gets a value indicating whether the block hides the faces of the blocks next to it.
        /// </summary>
        public bool Occludes { get; }

        public bool IsTranslucent => TopColor.W < 1;

        public static BlockAppearance Get(BlockId id) => Table.TryGetValue(id, out var appearance) ? appearance : Cube(HashColor(id));

        private static Dictionary<BlockId, BlockAppearance> BuildTable()
        {
            var table = new Dictionary<BlockId, BlockAppearance>();
            foreach (BlockId id in Enum.GetValues(typeof(BlockId)))
                table[id] = FromName(id, Enum.GetName(typeof(BlockId), id));

            table[BlockId.Air] = default;
            table[BlockId.GrassBlock] = new BlockAppearance(BlockShape.Cube, GrassColor, DirtColor, true, true);
            table[BlockId.Podzol] = new BlockAppearance(BlockShape.Cube, Rgb(0.42f, 0.30f, 0.15f), DirtColor, true, true);
            table[BlockId.Mycelium] = new BlockAppearance(BlockShape.Cube, Rgb(0.45f, 0.40f, 0.45f), DirtColor, true, true);
            table[BlockId.Dirt] = Cube(DirtColor);
            table[BlockId.CoarseDirt] = Cube(Rgb(0.47f, 0.34f, 0.24f));
            table[BlockId.Farmland] = Cube(Rgb(0.40f, 0.26f, 0.15f));
            table[BlockId.Stone] = Cube(StoneColor);
            table[BlockId.Cobblestone] = Cube(Rgb(0.45f, 0.45f, 0.45f));
            table[BlockId.MossyCobblestone] = Cube(Rgb(0.40f, 0.48f, 0.38f));
            table[BlockId.Bedrock] = Cube(Rgb(0.20f, 0.20f, 0.20f));
            table[BlockId.Sand] = Cube(Rgb(0.86f, 0.81f, 0.60f));
            table[BlockId.Sandstone] = Cube(Rgb(0.84f, 0.79f, 0.58f));
            table[BlockId.RedSand] = Cube(Rgb(0.74f, 0.40f, 0.13f));
            table[BlockId.Gravel] = Cube(Rgb(0.53f, 0.50f, 0.49f));
            table[BlockId.Clay] = Cube(Rgb(0.62f, 0.65f, 0.71f));
            table[BlockId.Granite] = Cube(Rgb(0.60f, 0.42f, 0.35f));
            table[BlockId.Diorite] = Cube(Rgb(0.74f, 0.74f, 0.74f));
            table[BlockId.Andesite] = Cube(Rgb(0.53f, 0.53f, 0.53f));
            table[BlockId.Obsidian] = Cube(Rgb(0.08f, 0.06f, 0.13f));
            table[BlockId.Snow] = Cube(Rgb(0.96f, 0.98f, 0.98f));
            table[BlockId.SnowBlock] = Cube(Rgb(0.96f, 0.98f, 0.98f));
            table[BlockId.Ice] = new BlockAppearance(BlockShape.Cube, Rgba(0.57f, 0.72f, 0.96f, 0.75f), Rgba(0.57f, 0.72f, 0.96f, 0.75f), true, false);
            table[BlockId.Glass] = new BlockAppearance(BlockShape.Cube, Rgba(0.80f, 0.90f, 0.95f, 0.35f), Rgba(0.80f, 0.90f, 0.95f, 0.35f), true, false);
            table[BlockId.Water] = new BlockAppearance(BlockShape.Cube, Rgba(0.20f, 0.36f, 0.86f, 0.65f), Rgba(0.20f, 0.36f, 0.86f, 0.65f), false, false);
            table[BlockId.Lava] = new BlockAppearance(BlockShape.Cube, Rgb(0.92f, 0.40f, 0.08f), Rgb(0.92f, 0.40f, 0.08f), false, true);
            table[BlockId.Cactus] = new BlockAppearance(BlockShape.Cube, Rgb(0.30f, 0.55f, 0.20f), Rgb(0.24f, 0.47f, 0.16f), true, true);
            table[BlockId.Pumpkin] = Cube(Rgb(0.85f, 0.50f, 0.10f));
            table[BlockId.Melon] = Cube(Rgb(0.45f, 0.65f, 0.15f));
            table[BlockId.CoalOre] = Cube(Rgb(0.33f, 0.33f, 0.33f));
            table[BlockId.IronOre] = Cube(Rgb(0.60f, 0.53f, 0.47f));
            table[BlockId.GoldOre] = Cube(Rgb(0.65f, 0.60f, 0.35f));
            table[BlockId.DiamondOre] = Cube(Rgb(0.42f, 0.66f, 0.66f));
            table[BlockId.RedstoneOre] = Cube(Rgb(0.60f, 0.35f, 0.35f));
            table[BlockId.LapisOre] = Cube(Rgb(0.35f, 0.42f, 0.62f));
            table[BlockId.EmeraldOre] = Cube(Rgb(0.38f, 0.60f, 0.42f));
            table[BlockId.SugarCane] = Cross(Rgb(0.55f, 0.75f, 0.35f));
            table[BlockId.DeadBush] = Cross(Rgb(0.55f, 0.40f, 0.20f));
            table[BlockId.Torch] = Cross(Rgb(0.95f, 0.80f, 0.30f));
            table[BlockId.Cobweb] = Cross(Rgba(0.90f, 0.90f, 0.90f, 0.6f));
            table[BlockId.Dandelion] = Cross(Rgb(0.95f, 0.85f, 0.15f));
            table[BlockId.Poppy] = Cross(Rgb(0.80f, 0.12f, 0.10f));
            table[BlockId.BlueOrchid] = Cross(Rgb(0.20f, 0.60f, 0.90f));
            table[BlockId.Allium] = Cross(Rgb(0.70f, 0.45f, 0.85f));
            table[BlockId.AzureBluet] = Cross(Rgb(0.85f, 0.88f, 0.90f));
            table[BlockId.OxeyeDaisy] = Cross(Rgb(0.90f, 0.90f, 0.80f));
            table[BlockId.Cornflower] = Cross(Rgb(0.30f, 0.40f, 0.85f));
            table[BlockId.BrownMushroom] = Cross(Rgb(0.60f, 0.45f, 0.33f));
            table[BlockId.RedMushroom] = Cross(Rgb(0.80f, 0.15f, 0.12f));
            return table;
        }

        private static BlockAppearance FromName(BlockId id, string name)
        {
            if (name.EndsWith("Leaves", StringComparison.Ordinal))
                return Cube(LeavesColor);
            if (name.EndsWith("Log", StringComparison.Ordinal) || name.EndsWith("Wood", StringComparison.Ordinal) || name == "MushroomStem")
                return new BlockAppearance(BlockShape.Cube, LogTopColor, LogSideColor, true, true);
            if (name.EndsWith("Planks", StringComparison.Ordinal))
                return Cube(PlanksColor);
            if (name.EndsWith("Ore", StringComparison.Ordinal))
                return Cube(StoneColor);
            if (name.EndsWith("Sapling", StringComparison.Ordinal) || name.EndsWith("Tulip", StringComparison.Ordinal) || name is "Grass" or "Fern" or "Seagrass" or "TallSeagrass" or "LilyOfTheValley" or "WitherRose" or "Wheat" or "Carrots" or "Potatoes" or "Beetroots")
                return Cross(PlantColor);
            if (name.EndsWith("StainedGlass", StringComparison.Ordinal) && TryDyeColor(name, out var glass))
                return new BlockAppearance(BlockShape.Cube, glass with { W = 0.5f }, glass with { W = 0.5f }, true, false);
            if ((name.EndsWith("Wool", StringComparison.Ordinal) || name.EndsWith("Terracotta", StringComparison.Ordinal) || name.Contains("Concrete", StringComparison.Ordinal)) && TryDyeColor(name, out var dyed))
                return Cube(dyed);

            // Small or flat blocks (rails, signs, buttons, torches on walls, ...) are not drawn.
            if (name.Contains("Torch", StringComparison.Ordinal) || name.Contains("Rail", StringComparison.Ordinal) || name.Contains("Sign", StringComparison.Ordinal)
                || name.Contains("Button", StringComparison.Ordinal) || name.Contains("PressurePlate", StringComparison.Ordinal) || name.Contains("Wire", StringComparison.Ordinal)
                || name.Contains("Lever", StringComparison.Ordinal) || name.Contains("Banner", StringComparison.Ordinal) || name.Contains("Head", StringComparison.Ordinal)
                || name.Contains("Skull", StringComparison.Ordinal) || name.StartsWith("Potted", StringComparison.Ordinal) || name.Contains("Carpet", StringComparison.Ordinal)
                || name is "Fire" or "NetherPortal" or "EndPortal" or "Vine" or "LilyPad" or "Ladder" or "Tripwire" or "Air" or "CaveAir" or "VoidAir" or "Barrier" or "StructureVoid")
                return default;

            return Cube(HashColor(id));
        }

        private static bool TryDyeColor(string name, out Vector4 color)
        {
            foreach (var dye in DyeColors)
            {
                if (name.StartsWith(dye.Name, StringComparison.Ordinal))
                {
                    color = dye.Color;
                    return true;
                }
            }

            color = default;
            return false;
        }

        private static BlockAppearance Cube(Vector4 color) => new BlockAppearance(BlockShape.Cube, color, color, true, color.W >= 1);

        private static BlockAppearance Cross(Vector4 color) => new BlockAppearance(BlockShape.Cross, color, color, false, false);

        private static Vector4 HashColor(BlockId id)
        {
            var hash = (uint)id * 2654435761u;
            var shade = 0.45f + ((hash & 0xFF) / 255f * 0.3f);
            return Rgb(shade, shade * (0.9f + (((hash >> 8) & 0xFF) / 255f * 0.2f)), shade * (0.9f + (((hash >> 16) & 0xFF) / 255f * 0.2f)));
        }

        private static Vector4 Rgb(float r, float g, float b) => new Vector4(r, g, b, 1);

        private static Vector4 Rgba(float r, float g, float b, float a) => new Vector4(r, g, b, a);
    }
}
