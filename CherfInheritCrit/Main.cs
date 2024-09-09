using BepInEx;
using Mono.Cecil;
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
        public const string PluginVersion = "1.0.1";

        internal static Main Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_ProcessHitEnemy;

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        void OnDestroy()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy -= GlobalEventManager_ProcessHitEnemy;

            Instance = SingletonHelper.Unassign(Instance, this);
        }

        static void GlobalEventManager_ProcessHitEnemy(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ParameterDefinition damageInfoParameter = null;
            for (int i = 0; i < il.Method.Parameters.Count; i++)
            {
                ParameterDefinition parameterDefinition = il.Method.Parameters[i];
                if (parameterDefinition.ParameterType.Is(typeof(DamageInfo)))
                {
                    damageInfoParameter = parameterDefinition;
                    break;
                }
            }

            if (damageInfoParameter == null)
            {
                Log.Error("Failed to find DamageInfo parameter");
                return;
            }

            if (c.TryGotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Items), nameof(RoR2Content.Items.LightningStrikeOnHit))))
            {
                if (c.TryGotoNext(MoveType.Before, x => x.MatchStfld<GenericDamageOrb>(nameof(GenericDamageOrb.isCrit))))
                {
                    c.Emit(OpCodes.Ldarg, damageInfoParameter);
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
