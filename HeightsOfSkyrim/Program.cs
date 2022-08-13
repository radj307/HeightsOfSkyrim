using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HeightsOfSkyrim
{
    public static class Program
    {
        private const double _heightThreshold = 0.00001;
        private static readonly ModKey HeightsOfSkyrimModKey = ModKey.FromNameAndExtension("Heights_of_Skyrim.esp");

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "HeightOfSkyrimPatch.esp")
                .Run(args);
        }
        private static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LoadOrder.TryGetIfEnabledAndExists(HeightsOfSkyrimModKey, out var heightsMod) || heightsMod is null)
            {
                throw new FileNotFoundException($"'{HeightsOfSkyrimModKey.FileName}' wasn't found in your load order; ensure that it is enabled & that it loads before '{state.PatchMod.ModKey.FileName}'!", HeightsOfSkyrimModKey.FileName);
            }

            foreach (INpcGetter npc in heightsMod.Npcs)
            {
                try
                {
                    INpcGetter winningOverride = npc.ToLink().Resolve(state.LinkCache);

                    if (Math.Abs(winningOverride.Height - npc.Height) < _heightThreshold || winningOverride.Height.Equals(npc.Height))
                        continue;

                    INpc patchedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(winningOverride);
                    patchedNpc.Height = npc.Height;

                    Console.WriteLine($"Corrected height for NPC record '{npc.EditorID ?? npc.FormKey.IDString()}'");
                }
                catch (Exception ex)
                {
                    throw RecordException.Enrich(ex, npc);
                }
            }
        }
    }
}