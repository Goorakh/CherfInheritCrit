using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RoR2;
using RoR2.Orbs;
using System.Diagnostics;

namespace CherfInheritCrit
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "CherfInheritCrit";
        public const string PluginVersion = "1.0.0";

        internal static Main Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            IL.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        void OnDestroy()
        {
            IL.RoR2.GlobalEventManager.OnHitEnemy -= GlobalEventManager_OnHitEnemy;

            Instance = SingletonHelper.Unassign(Instance, this);
        }

        static void GlobalEventManager_OnHitEnemy(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int damageInfoParameterIndex = -1;
            for (int i = 0; i < il.Method.Parameters.Count; i++)
            {
                if (il.Method.Parameters[i].ParameterType.Is(typeof(DamageInfo)))
                {
                    damageInfoParameterIndex = i;
                    break;
                }
            }

            if (damageInfoParameterIndex == -1)
            {
                Log.Error("Failed to find DamageInfo parameter");
                return;
            }

            if (c.TryGotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Items), nameof(RoR2Content.Items.LightningStrikeOnHit))))
            {
                if (c.TryGotoNext(MoveType.Before, x => x.MatchStfld<GenericDamageOrb>(nameof(GenericDamageOrb.isCrit))))
                {
                    c.Emit(OpCodes.Ldarg, damageInfoParameterIndex);
                    c.EmitDelegate((bool isCrit, DamageInfo damageInfo) =>
                    {
                        return damageInfo.crit;
                    });
                }
                else
                {
                    Log.Error("Failed to find patch location");
                }
            }
            else
            {
                Log.Error("Failed to find LightningStrikeOnHit item check");
            }
        }
    }
}
