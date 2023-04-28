using KokoLib;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GarnsMod.Content.Players
{
    // This class deals with holding / syncing all the RPG stats stored on the player instance
    public class GarnsFishingRPGPlayer : ModPlayer
    {
        public int totalFishCaught;
        public int totalCratesCaught;
        public bool usedFishingPermUpgrade1;
        public bool usedFishingPermUpgrade2;

        // Only called on the client that caught the fish, so totalCratesCaught and totalFishCaught will get desynced
        // Thankfully we have clientClone and SendClientChanges to the rescue
        public override void ModifyCaughtFish(Item item)
        {
            if (ItemID.Sets.IsFishingCrate[item.type])
            {
                totalCratesCaught++;
            }
            else
            {
                totalFishCaught++;
            }
        }

        public override void clientClone(ModPlayer clientClone)
        {
            if (clientClone is not GarnsFishingRPGPlayer player)
            {
                return;
            }
            player.totalFishCaught = totalFishCaught;
            player.totalCratesCaught = totalCratesCaught;
        }

        // Called when players join. When someone joins, the server syncs the new player => server, then the server
        // syncs the new player => other players, then the server syncs the other players => new player
        // since usedFishingPermUpgrade1 and 2 are calculated deterministicly (all clients see it happen), no further syncing is
        // needed after SyncPlayer
        // however, for totalFishCaught and totalCratesCaught, those change on one client, but the other clients dont see it
        // thats why we use clientClone, sendClientChanges, and, for this case in particular (those 2 fields) we use SendCatchStatsSyncPacket
        // See GarnsMod.Networking.cs
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            Net<IRPGPlayerNetHandler>.Proxy.SyncPlayer(Player, totalFishCaught, totalCratesCaught, usedFishingPermUpgrade1, usedFishingPermUpgrade2);
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            // If either totalFishCaught or totalCratesCaught are desynced, they will both be re-synced
            if (clientPlayer is not GarnsFishingRPGPlayer clone)
            {
                return;
            }
            if (clone.totalFishCaught != totalFishCaught)
            {
                Net<IRPGPlayerNetHandler>.Proxy.SyncTotalFishCaught(Player, totalFishCaught);
            }
            if (clone.totalCratesCaught != totalCratesCaught)
            {
                Net<IRPGPlayerNetHandler>.Proxy.SyncTotalCratesCaught(Player, totalCratesCaught);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["totalFishCaught"] = totalFishCaught;
            tag["totalCratesCaught"] = totalCratesCaught;
            tag["usedFishingPermUpgrade1"] = usedFishingPermUpgrade1;
            tag["usedFishingPermUpgrade2"] = usedFishingPermUpgrade2;
        }

        public override void LoadData(TagCompound tag)
        {
            totalFishCaught = (int)tag["totalFishCaught"];
            totalCratesCaught = (int)tag["totalCratesCaught"];
            usedFishingPermUpgrade1 = tag.Get<bool>("usedFishingPermUpgrade1");
            usedFishingPermUpgrade2 = tag.Get<bool>("usedFishingPermUpgrade2");
        }
    }

    public interface IRPGPlayerNetHandler
    {
        void SyncPlayer(Player player, int totalFishCaught, int totalCratesCaught, bool usedFishingPermUpgrade1, bool usedFishingPermUpgrade2);
        void SyncTotalFishCaught(Player player, int totalFishCaught);
        void SyncTotalCratesCaught(Player player, int totalCratesCaught);

        private class GarnsFishingRPGPlayerHandler : ModHandler<IRPGPlayerNetHandler>, IRPGPlayerNetHandler
        {
            public override IRPGPlayerNetHandler Handler => this;

            public void SyncPlayer(Player player, int totalFishCaught, int totalCratesCaught, bool usedFishingPermUpgrade1, bool usedFishingPermUpgrade2)
            {
                GarnsFishingRPGPlayer p = player.GetModPlayer<GarnsFishingRPGPlayer>();
                p.totalFishCaught = totalFishCaught;
                p.totalCratesCaught = totalCratesCaught;
                p.usedFishingPermUpgrade1 = usedFishingPermUpgrade1;
                p.usedFishingPermUpgrade2 = usedFishingPermUpgrade2;
            }

            public static void ServerRelay(Action action)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    action();
                }
            }

            public void SyncTotalFishCaught(Player player, int totalFishCaught)
            {
                GarnsFishingRPGPlayer p = player.GetModPlayer<GarnsFishingRPGPlayer>();
                p.totalFishCaught = totalFishCaught;
                ServerRelay(() => Net<IRPGPlayerNetHandler>.Proxy.SyncTotalFishCaught(player, totalFishCaught));
            }

            public void SyncTotalCratesCaught(Player player, int totalCratesCaught)
            {
                GarnsFishingRPGPlayer p = player.GetModPlayer<GarnsFishingRPGPlayer>();
                p.totalCratesCaught = totalCratesCaught;
                ServerRelay(() => Net<IRPGPlayerNetHandler>.Proxy.SyncTotalFishCaught(player, totalCratesCaught));
            }
        }
    }
}
