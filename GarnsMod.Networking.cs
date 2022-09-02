using System.IO;

namespace GarnsMod
{
	// This is a partial class, meaning some of its parts were split into other files. See ExampleMod.*.cs for other portions.
	partial class GarnsMod
	{
		/*	internal abstract class PacketHandler
			{
				internal byte HandlerType { get; set; }
				internal Mod ModInstance { get; set; }

				internal static Dictionary<byte, PacketHandler> PacketHandlers = new();
				internal static byte CurrHandlerType = 0;

				public abstract void HandlePacket(BinaryReader reader, int fromWho);

				protected PacketHandler(Mod modInstance) {
					ModInstance = modInstance;
					if (CurrHandlerType == byte.MaxValue)
						throw new Exception("You have way too many packet handlers");
					PacketHandlers[CurrHandlerType++] = this;
				}

				protected ModPacket GetPacket(byte packetType, int? extraData) {
					var p = ModInstance.GetPacket();
					p.Write(HandlerType);
					p.Write(packetType);
					if (extraData is not null)
						p.Write((byte)extraData);
					return p;
				}
			}

			Example of server -> all clients one way workflow
			internal class MyBossNPCPacketHandler : PacketHandler
			{
				public const byte SyncTarget = 1;

				public MyBossNPCPacketHandler(Mod modInstance) : base(modInstance) { }


				// Would only ever be called on the client
				public override void HandlePacket(BinaryReader reader, int fromWho) {
					int packetType = reader.ReadByte();
					int _ = reader.ReadByte(); // No need for the extraData because this is a Server -> Client one way handler
					switch (packetType) {
						case (SyncTarget):
							ReceiveTarget(reader);
							break;
					}
				}

				// Would only ever be called on the server
				public void SendTarget(int npc, int target) {
					ModPacket packet = GetPacket(SyncTarget, null);
					packet.Write(npc);
					packet.Write(target);
					packet.Send(-1, -1);
				}

				// Would only ever be called on the client
				public static void ReceiveTarget(BinaryReader reader) {
					int npc = reader.ReadInt32();
					int target = reader.ReadInt32();
					NPC theNpc = Main.npc[npc];
					theNpc.oldTarget = theNpc.target;
					theNpc.target = target;
				}
			}


			internal MyBossNPCPacketHandler bossNpcPacketHandler = new(ModLoader.GetMod("ExampleMod"));


			public override void HandlePacket(BinaryReader r, int fromWho) {
				PacketHandler handler = PacketHandler.PacketHandlers[r.ReadByte()];
				handler.HandlePacket(r, fromWho);
			}*/


		internal enum MessageType : byte
		{
			FishingRPGPlayer_SyncPlayer,
			FishingRPGPlayer_CatchStatsChanged
		}

		// Override this method to handle network packets sent for this mod.
		//TODO: Introduce OOP packets into tML, to avoid this god-class level hardcode.
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			MessageType msgType = (MessageType)reader.ReadByte();

			switch (msgType)
			{
				case MessageType.FishingRPGPlayer_SyncPlayer:
					// Fuck this non koko lib bullshit
					break;
				case MessageType.FishingRPGPlayer_CatchStatsChanged:
					// Fuck this non koko lib bullshit
					break;
				default:
					Logger.WarnFormat("ExampleMod: Unknown Message type: {0}", msgType);
					break;
			}
		}
	}
}