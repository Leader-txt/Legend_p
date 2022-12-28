using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace test
{
    [ApiVersion(2, 1)]
    public class Class1 : TerrariaPlugin
    {
        public Class1(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            //GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
            Commands.ChatCommands.Add(new Command("", test, "test"));
        }

        private void test(CommandArgs args)
        {
            args.Player.SendData(PacketTypes.CreateCombatTextExtended, "Exp:7298 好", (int)Color.Yellow.PackedValue,args.Player.X,args.Player.Y);
            /*var data = new MemoryStream();
            using (var wr = new BinaryWriter(data))
            {
                wr.Write((short)0);
                wr.Write((byte)82);
                wr.Write((short)1);
                wr.Write((byte)args.Player.Index);
                wr.Write((byte)0);
                wr.Write("aaa");
                wr.Write((byte)255);
                wr.Write((byte)255);
                wr.Write((byte)255);
                var length = wr.BaseStream.Length;
                wr.BaseStream.Position = 0;
                wr.Write((short)length);
            }
            args.Player.SendRawData(data.ToArray());*/
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
        }
    }
}
