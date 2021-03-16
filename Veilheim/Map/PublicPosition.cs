// Veilheim
// a Valheim mod
// 
// File:    PublicPosition.cs
// Project: Veilheim

using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Map
{
    public class PublicPostion_Patches : Payload
    {
        [PatchEvent(typeof(ZNet), nameof(ZNet.Awake), PatchEventType.Postfix, 600)]
        public static void EnablePublicPosition(ZNet instance)
        {
            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.playerPositionPublicOnJoin)
            {
                // Set player position visibility to public by default on server join
                instance.m_publicReferencePosition = true;
            }
        }

        [PatchEvent(typeof(ZNet), nameof(ZNet.SetPublicReferencePosition), PatchEventType.Postfix)]
        public static void PreventDisablePublicPosition(ZNet instance)
        {
            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.preventPlayerFromTurningOffPublicPosition
            ) //isn't there a limit to identifiers in c#?
            {
                instance.m_publicReferencePosition = true;
            }
        }
    }
}