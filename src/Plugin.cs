using BepInEx;
using BoplFixedMath;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MoreThanOneSpike
{
    [BepInPlugin("com.ra4ina.MoreThanOneSpike", "MoreThanOneSpike", "1.0.0")]//A unique name could be anything, The name of your plugin, The version of your plugin
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin MoreThanOneSpike is loaded!");//feel free to remove this
            Harmony harmony = new Harmony("com.ra4ina.MoreThanOneSpike");


            MethodInfo original = AccessTools.Method(typeof(Spike), "CastSpike");
            MethodInfo patch = AccessTools.Method(typeof(MyPatch), "CastSpike_Spike_modif");
            harmony.Patch(original, new HarmonyMethod(patch));
        }

    }
    public class MyPatch
    {
        public static bool CastSpike_Spike_modif(Spike __instance, ref PlayerPhysics ___playerPhysics,
            ref DPhysicsRoundedRect ___groundRect, ref PlayerBody ___body, ref ParticleSystem ___dustParticle,
            ref ParticleSystem ___dustParticleFar, ref ShakableCamera ___shakeCam,
             ref SpikeAttack ___currentSpike, ref SpikeAttack ___spikePrefab, ref Fix ___surfaceOffset,
             ref Fix ___castForce,
             ref AnimationData ___animData, ref SpriteAnimator ___animator)
        {

            StickyRoundedRectangle attachedGround = ___playerPhysics.getAttachedGround();
            if (attachedGround == null || attachedGround.IsDestroyed || !attachedGround.ThisGameObject().activeInHierarchy)
            {
                __instance.ExitAbility(default(AbilityExitInfo));
                return false;
            }
            ___groundRect = attachedGround.GetComponent<DPhysicsRoundedRect>();
            Vec2 vec = attachedGround.currentNormal(___body.position);
            Fix zero = Fix.Zero;
            Fix zero2 = Fix.Zero;
            if (!Raycast.RayCastRoundedRect(___body.position, -vec, ___groundRect.GetRoundedRect(), ref zero, ref zero2))
            {
                MonoBehaviour.print("Spike raycast inexplicibly missed");
                __instance.ExitAbility(default(AbilityExitInfo));
                return false;
            }
            Vec2 vec2 = ___body.position - vec * zero2;
            AudioManager.Get().Play("spikeRock");

            ___dustParticle.transform.parent.position = __instance.transform.position;
            ___dustParticle.transform.parent.up = __instance.transform.up;
            ___dustParticle.Play();
            ___dustParticleFar.transform.parent.position = (Vector3)vec2;
            ___dustParticleFar.transform.parent.up = __instance.transform.up;
            ___dustParticleFar.Play();
            ___shakeCam.AddTrauma(0.3f);
            Vec2 pos = vec2;
            ___currentSpike = FixTransform.InstantiateFixed<SpikeAttack>(___spikePrefab, pos);
            FixTransform component = ___currentSpike.GetComponent<FixTransform>();
            component.up = vec;
            component.transform.SetParent(___groundRect.transform);
            ___currentSpike.transform.localScale = new Vector3(1f / attachedGround.transform.localScale.x, 1f / attachedGround.transform.localScale.x, 1f);
            ___currentSpike.Initialize(vec2, ___surfaceOffset, attachedGround);
            DetPhysics.Get().AddBoxToRR(___currentSpike.gameObject.GetInstanceID(), attachedGround.gameObject.GetInstanceID());
            ___currentSpike.UpdateRelativeOrientation();
            ___currentSpike.UpdatePosition();
            ___currentSpike.GetHitbox().Scale = ___body.fixtrans.Scale;
            attachedGround.GetGroundBody().AddForceAtPosition(vec * ___castForce, ___body.position, ForceMode2D.Force);
            ___animator.beginAnimThenDoAction(___animData.GetAnimation("recover"), delegate
            {
                __instance.ExitAbility(default(AbilityExitInfo));
            });
            return false;
        }
    }
}
