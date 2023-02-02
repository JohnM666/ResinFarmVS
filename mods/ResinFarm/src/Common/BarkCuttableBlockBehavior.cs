using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace ResinFarm.src.Common
{
    public class BarkCuttableBlockBehavior : BlockBehavior
    {
        public string[] ToolNames { get; private set; }

        public ItemStack[] Tools { get; private set; }

        public float Duration { get; private set; }

        public float Chance { get; private set; }

        public class ModConfig
        {
            public float duration { get; private set; }
            public float chance { get; private set; }
            public string[] tools { get; private set; }

            public ModConfig(float duration, float chance, string[] tools)
            {
                this.duration = duration;
                this.chance = chance;
                this.tools = tools;
            }
        }

        public BarkCuttableBlockBehavior(Block block) :
            base(block)
        {

        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            var configName = block.Code.Path.Substring(0, block.Code.Path.LastIndexOf("-")) + "-config.json";
            var config = ResinFarmMod.CoreAPI.LoadModConfig<ModConfig>(configName);

            if (config == null)
            {
                ToolNames = properties["tools"].AsArray<string>();
                Duration = properties["duration"].AsFloat();
                Chance = properties["chance"].AsFloat();

                ResinFarmMod.CoreAPI.StoreModConfig(new ModConfig(Duration, Chance, ToolNames), configName);
            }
            else
            {
                ToolNames = config.tools;
                Duration = config.duration;
                Chance = config.chance;
            }

            UpdateTools(ResinFarmMod.CoreAPI.World);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer player, ref EnumHandling handled)
        {
            return new WorldInteraction[]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = ResinFarmMod.ModId + ":blockhelp-cut",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = false,
                        Itemstacks = Tools
                    }
                };
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            var currentItem = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (currentItem == null)
                return false;

            if (!Tools.Select((ItemStack stack) => stack.Item).Contains(currentItem.Itemstack?.Item))
                return false;

            handling = EnumHandling.PreventDefault;
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();
                tf.Translation.Set(0f, 0f, 0f - Math.Min(0.6f, secondsUsed * 2f));
                tf.Rotation.Y = Math.Min(20f, secondsUsed * 90f * 2f);

                if (secondsUsed > 0.4f)
                {
                    tf.Translation.X += (float)Math.Cos(secondsUsed * 15f) / 10f;
                    tf.Translation.Z += (float)Math.Sin(secondsUsed * 5f) / 30f;
                }

                byPlayer.Entity.Controls.UsingHeldItemTransformBefore = tf;
            }

            handling = EnumHandling.Handled;
            return secondsUsed <= Duration;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (secondsUsed >= Duration)
            {
                var shouldGenerateResin = ShouldGenerateResin(world.Seed, blockSel.Position);

                if (world.Side == EnumAppSide.Server)
                {
                    if (shouldGenerateResin)
                    {
                        var currentBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                        string rotation = "-ud";

                        if (blockSel.Face == BlockFacing.SOUTH)
                            rotation = "-south";
                        else if (blockSel.Face == BlockFacing.NORTH)
                            rotation = "-north";
                        else if (blockSel.Face == BlockFacing.WEST)
                            rotation = "-west";
                        else if (blockSel.Face == BlockFacing.EAST)
                            rotation = "-east";

                        var newBlockId = "game:log-resin-" + currentBlock.Variant["wood"] + rotation;
                        world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation(newBlockId)).Id, blockSel.Position);
                    }

                    byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Item?.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
                }
                else
                {
                    if (!shouldGenerateResin)
                        ResinFarmMod.ClientAPI.TriggerIngameError(this, "resinfarm:invalid-log-block", Lang.Get("resinfarm:invalid-log-block"));
                }
            }

            handling = EnumHandling.Handled;
        }

        private void UpdateTools(IWorldAccessor world)
        {
            if (Tools == null)
            {
                var tools = new List<ItemStack>();

                foreach (string toolName in ToolNames)
                {
                    Item[] items = world.SearchItems(new AssetLocation(toolName));

                    foreach (Item item in items)
                    {
                        tools.Add(new ItemStack(item));
                    }
                }

                Tools = tools.ToArray();
            }
        }

        private bool ShouldGenerateResin(int worldSeed, BlockPos position)
        {
            return GameMath.MurmurHash3Mod(position.X, position.Y, position.Z, 1000) < Chance * 1000.0f;
        }
    }
}
