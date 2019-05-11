using BepInEx;
using RoR2;
using RoR2.Stats;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace MultiPing
{
    [BepInDependency("com.bepis.r2api")]

    [BepInPlugin("la.roblab.ror2.MultiPing", "MultiPing", "1.0")]

    public class MultiPing : BaseUnityPlugin
    {
        private Dictionary<GameObject, PingIndicator> pings = new Dictionary<GameObject, PingIndicator>();
        private Dictionary<GameObject, PingIndicator> ownedPings = new Dictionary<GameObject, PingIndicator>();

        void Awake()
        {
            On.RoR2.PingerController.RebuildPing += PingerController_RebuildPing;
        }

        private void PingerController_RebuildPing(On.RoR2.PingerController.orig_RebuildPing orig, PingerController self, PingerController.PingInfo pingInfo)
        {
            Type[] types = { typeof(ShopTerminalBehavior) /* MultiShops */, typeof(ShrineChanceBehavior), typeof(ChestBehavior), typeof(TeleporterInteraction) };
            Type ty = null;
            try
            {
                // TODO: Find a FirstOr method...
                ty = types.First((t) => pingInfo.targetGameObject?.GetComponent(t) != null);
            }
            catch (InvalidOperationException e) { /* Do nothing */ }

            if (ty != null)
                TogglePing(pingInfo.targetGameObject?.GetComponent(ty).gameObject, self, pingInfo);
            else
            {
                // TODO: Broken.
                //orig(self, pingInfo);

                // Since calling orig is broken, let's just copy pasta the game's code, with some small adjustments to take into account the fact that there's only a single behavior
                // (instead of one per player)
                if (!this.ownedPings.ContainsKey(self.gameObject))
                    this.ownedPings[self.gameObject] = null;

                if (!pingInfo.active && this.ownedPings[self.gameObject] != null) {
                    UnityEngine.Object.Destroy((UnityEngine.Object)this.ownedPings[self.gameObject].gameObject);
                    this.ownedPings[self.gameObject] = (PingIndicator)null;
                } else {
                    if (!(bool)((UnityEngine.Object)this.ownedPings[self.gameObject])) {
                        this.ownedPings[self.gameObject] = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Prefabs/PingIndicator"))).GetComponent<PingIndicator>();
                        this.ownedPings[self.gameObject].pingOwner = ((Component)self).gameObject;
                    }
                    this.ownedPings[self.gameObject].pingOrigin = pingInfo.origin;
                    this.ownedPings[self.gameObject].pingNormal = pingInfo.normal;
                    this.ownedPings[self.gameObject].pingTarget = pingInfo.targetGameObject;
                    this.ownedPings[self.gameObject].RebuildPing();
                }
            }
        }

        private void TogglePing(GameObject body, PingerController self, PingerController.PingInfo pingInfo)
        {

            if (pings.ContainsKey(body) && pings[body])
            {
                UnityEngine.Object.Destroy((UnityEngine.Object)pings[body].gameObject);
                pings.Remove(body);
            }
            else if (pings.ContainsKey(body))
            {
                // If the PI is destroyed somewhere else in the game, it suddenly turns into a null reference? WTF?
                // In this case, the PI is not really visible. Let's remove the old (null) one, and create a new one.
                pings.Remove(body);
                pings.Add(body, CreatePingIndicator(self, pingInfo));
            } else
            {
                // Create a new ping indicator.
                pings.Add(body, CreatePingIndicator(self, pingInfo));
            }

        }

        private static PingIndicator CreatePingIndicator(PingerController self, PingerController.PingInfo pingInfo, bool neverExpire = true)
        {
            var pingIndicator = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Prefabs/PingIndicator"))).GetComponent<PingIndicator>();
            pingIndicator.pingOwner = self.gameObject;
            pingIndicator.pingOrigin = pingInfo.origin;
            pingIndicator.pingNormal = pingInfo.normal;
            pingIndicator.pingTarget = pingInfo.targetGameObject;
            pingIndicator.RebuildPing();
            // Never expire plz.
            if (neverExpire)
            {
                var field = typeof(PingIndicator).GetField("fixedTimer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Instance);
                field.SetValue(pingIndicator, float.MaxValue);
            }
            return pingIndicator;
        }
    }
}