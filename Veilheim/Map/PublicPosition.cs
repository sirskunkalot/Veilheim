// Veilheim
// a Valheim mod
// 
// File:    PublicPosition.cs
// Project: Veilheim

using Jotunn.Utils;
using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Map
{
    public class PublicPostion_Patches : IPatchEventConsumer
    {

        [PatchInit(0)]
        public static void InitializePatches()
        {
            On.ZNet.SetPublicReferencePosition += PreventDisablePublicPosition;
            On.ZNet.Awake += EnablePublicPosition;
        }

        private static void EnablePublicPosition(On.ZNet.orig_Awake orig, ZNet self)
        {
            orig(self);

            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.playerPositionPublicOnJoin)
            {
                // Set player position visibility to public by default on server join
                self.m_publicReferencePosition = true;
            }
        }

        private static void PreventDisablePublicPosition(On.ZNet.orig_SetPublicReferencePosition orig, ZNet self, bool pub)
        {
            orig(self, pub);

            //isn't there a limit to identifiers in c#?
            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.preventPlayerFromTurningOffPublicPosition) 
            {
                self.m_publicReferencePosition = true;
            }
        }
    }
}