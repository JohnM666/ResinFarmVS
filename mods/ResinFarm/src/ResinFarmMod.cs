using ResinFarm.src.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ResinFarm.src
{
    public class ResinFarmMod : ModSystem
    {
        public static string ModId { get; } = "resinfarm";

        public static ICoreAPI CoreAPI { get; private set; }

        public static ICoreClientAPI ClientAPI { get; private set; }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockBehaviorClass("BarkCuttable", typeof(BarkCuttableBlockBehavior));

            CoreAPI = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            ClientAPI = api;
        }
    }
}
