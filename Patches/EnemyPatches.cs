using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class LeviathanPatches
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        static void OnStart(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.LeviathanSurfacePatch.Value)
            {
                ScienceBirdTweaks.Logger.LogDebug("Replacing surface tags!");
                string[] newSurfaceTags = ScienceBirdTweaks.LeviathanNaturalSurfaces.Value.Replace(", ", ",").Split(",");
                string[] combinedArray = __instance.naturalSurfaceTags.Concat(newSurfaceTags).ToArray();
                __instance.naturalSurfaceTags = combinedArray;
            }
        }

        [HarmonyPatch(typeof(SandWormAI), nameof(SandWormAI.ShakePlayerCameraInProximity))]
        [HarmonyPrefix]
        static void GroundTransitionPatch(SandWormAI __instance, Vector3 pos)
        {
            if (!ScienceBirdTweaks.LeviathanQuicksand.Value) { return; }

            Vector3 targetPos = __instance.hitGroundParticle.transform.position;
            if (pos == __instance.transform.position)// emerging
            {
                targetPos = __instance.emergeFromGroundParticle2.transform.position;
            }
            GameObject.Instantiate(RoundManager.Instance.quicksandPrefab, targetPos, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
        }
    }

    [HarmonyPatch]
    public class CoilheadElevatorPatch
    {
        private static Matrix4x4 matrix;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void Initialize(PlayerControllerB __instance)// this doesn't really need to be a patch but whatever
        {
            if (ScienceBirdTweaks.CoilheadElevatorFix.Value)
            {
                Vector4 col1 = new Vector4(0f, 0f, 1f, 0f);
                Vector4 col2 = new Vector4(0f, 1f, 0f, 0f);
                Vector4 col3 = new Vector4(-1f, 0f, 0f, 0f);
                Vector4 col4 = Vector4.zero;
                matrix = new Matrix4x4(col1, col2, col3, col4);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.HasLineOfSightToPosition))]
        [HarmonyPostfix]
        static void HasLineOfSightPatch(PlayerControllerB __instance, Vector3 pos, float width, int range, float proximityAwareness, ref bool __result)// re-do line of sight check if non-local player is in elevator, this time applying matrix to forward vector
        {
            if (ScienceBirdTweaks.CoilheadElevatorFix.Value && RoundManager.Instance.currentMineshaftElevator != null && !__instance.IsOwner && __instance.physicsParent != null && __instance.physicsParent.gameObject.name == "AnimContainer")
            {
                Vector3 newForward = matrix * __instance.playerEye.transform.forward;
                RaycastHit hit;
                float num = Vector3.Distance(__instance.transform.position, pos);
                if (num < (float)range && (Vector3.Angle(newForward, pos - __instance.gameplayCamera.transform.position) < width || num < proximityAwareness) && !Physics.Linecast(__instance.playerEye.transform.position, pos, out hit, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    __result = true;
                    return;
                }
                __result = false;
            }
        }


        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LineOfSightToPositionAngle))]
        [HarmonyPostfix]
        static void LineOfSightToPosPatch(PlayerControllerB __instance, Vector3 pos, int range, float proximityAwareness, ref float __result)// same as above
        {
            if (ScienceBirdTweaks.CoilheadElevatorFix.Value && RoundManager.Instance.currentMineshaftElevator != null && !__instance.IsOwner && __instance.physicsParent != null && __instance.physicsParent.gameObject.name == "AnimContainer")
            {
                Vector3 newForward = matrix * __instance.playerEye.transform.forward;
                if (Vector3.Distance(__instance.transform.position, pos) < (float)range && !Physics.Linecast(__instance.playerEye.transform.position, pos, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    __result = Vector3.Angle(newForward, pos - __instance.gameplayCamera.transform.position);
                    return;
                }
                __result = -361f;
            }
        }
    }

    [HarmonyPatch]
    public class CentipedePatch
    {
        public static bool multiplayerSecondChanceGiven = false;

        private static int maxHealth = 100;

        private static int damageAccumulated = 0;

        private static bool subtractInterval = false;

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPostfix]
        static void GetMaxHealth(RoundManager __instance)
        {
            if (ScienceBirdTweaks.CentipedeMode.Value == "Vanilla")
            {
                return;
            }
            damageAccumulated = 0;
            maxHealth = GameNetworkManager.Instance.localPlayerController.health;
            ScienceBirdTweaks.Logger.LogDebug($"Found max health: {maxHealth}");// find max health at level start (in case it isn't 100 for whatever reason)
        }

        [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.Update))]
        [HarmonyPrefix]
        static void CentipedeClearDamage(CentipedeAI __instance)
        {
            if (ScienceBirdTweaks.CentipedeMode.Value == "Vanilla")
            {
                return;
            }
            if (ScienceBirdTweaks.CentipedeMode.Value == "Fixed Damage")
            {
                if (__instance.clingingToPlayer == null)
                {
                    return;
                }
                if (__instance.clingingToPlayer.isPlayerDead)// clear accumulated damage when a player dies
                {
                    subtractInterval = false;
                    damageAccumulated = 0;
                }
            }
        }

        [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.DamagePlayerOnIntervals))]
        [HarmonyPrefix]
        static void CentipedeDamage(CentipedeAI __instance)
        {
            if (ScienceBirdTweaks.CentipedeMode.Value == "Vanilla")
            {
                return;
            }
            if (subtractInterval)// vanilla method for doing damage on a certain interval, this patch essentially replaces vanilla logic
            {
                __instance.damagePlayerInterval -= Time.deltaTime;
            }
            subtractInterval = false;
            if (__instance.damagePlayerInterval <= 0f && !__instance.inDroppingOffPlayerAnim)
            {
                if (__instance.stunNormalizedTimer > 0f || (((ScienceBirdTweaks.CentipedeMode.Value == "Second Chance" && !multiplayerSecondChanceGiven) || (StartOfRound.Instance.connectedPlayersAmount <= 0 && !__instance.singlePlayerSecondChanceGiven && ScienceBirdTweaks.CentipedeMode.Value != "Fixed Damage")) && __instance.clingingToPlayer.health <= ScienceBirdTweaks.CentipedeSecondChanceThreshold.Value))
                {// drop off player, this covers both vanilla second chance behaviour, and the second chance behaviour added by this mod. essentially this runs if: solo (and second chance not given yet), multiplayer with second chance mode (and second chance not given yet), and only after a player is reduced to a certain HP threshold
                    ScienceBirdTweaks.Logger.LogDebug($"Giving second chance!");
                    if (StartOfRound.Instance.connectedPlayersAmount <= 0)
                    {
                        __instance.singlePlayerSecondChanceGiven = true;
                    }
                    else
                    {
                        multiplayerSecondChanceGiven = true;
                    }
                    __instance.inDroppingOffPlayerAnim = true;
                    __instance.StopClingingServerRpc(playerDead: false);
                }
                else if (damageAccumulated < Mathf.RoundToInt(maxHealth * ScienceBirdTweaks.CentipedeFixedDamage.Value) || ScienceBirdTweaks.CentipedeMode.Value != "Fixed Damage" || __instance.clingingToPlayer.criticallyInjured)
                {// main damage loop, runs if in fixed mode and threshold not met yet, if not in fixed mode, or if the player is critically injured
                    if (ScienceBirdTweaks.CentipedeMode.Value == "Fixed Damage")
                    {
                        damageAccumulated += 10;
                        ScienceBirdTweaks.Logger.LogDebug($"Accumulated damage: {damageAccumulated}");
                    }
                    __instance.clingingToPlayer.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Suffocation);
                    __instance.damagePlayerInterval = 2f;
                }
                else// otherwise, drop off and clear accumulated damage
                {
                    ScienceBirdTweaks.Logger.LogDebug("Dropping off player");
                    __instance.inDroppingOffPlayerAnim = true;
                    __instance.StopClingingServerRpc(playerDead: false);
                    damageAccumulated = 0;
                }
            }
            else
            {
                subtractInterval = true;
            }
            if (__instance.damagePlayerInterval <= 0f && !__instance.inDroppingOffPlayerAnim)
            {
                __instance.damagePlayerInterval = 0.01f;// ensure actual game code never runs
            }
        }
    }

    [HarmonyPatch]
    public class TulipSnakePatch
    {
        [HarmonyPatch(typeof(FlowerSnakeEnemy), nameof(FlowerSnakeEnemy.MakeChuckleClientRpc))]
        [HarmonyPrefix]
        static bool ChuckleOverride(FlowerSnakeEnemy __instance)// same method as vanilla with the audible noise calls removed
        {
            if (!ScienceBirdTweaks.TulipSnakeMuteLaugh.Value) { return true; }

            if (__instance.clingingToPlayer != null)
            {
                __instance.isInsidePlayerShip = __instance.clingingToPlayer.isInHangarShipRoom;
            }
            RoundManager.PlayRandomClip(__instance.creatureVoice, __instance.enemyType.audioClips, randomize: true, 1f, 0, 5);
            return false;
        }
    }

    [HarmonyPatch]
    public class ManeaterPatches
    {
        private static Vector3 startPos;
        private static Vector3 storedPos;
        private static bool donePositionFix = true;
        private static bool owner = false;

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.HitEnemy))]
        [HarmonyPrefix]
        static void InterruptAnimation(CaveDwellerAI __instance)
        {
            if (ScienceBirdTweaks.ManeaterTransformInterrupt.Value && __instance.inSpecialAnimation && __instance.growthMeter > 1f)
            {
                donePositionFix = false;
                ScienceBirdTweaks.Logger.LogDebug("Exited early!");
                __instance.inSpecialAnimation = false;
                __instance.agent.enabled = true;
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.StartTransformationAnim))]
        [HarmonyPrefix]
        static void GetStartPos(CaveDwellerAI __instance)
        {
            if (ScienceBirdTweaks.ManeaterTransformInterrupt.Value)
            {
                startPos = __instance.transform.position;// position when transformation starts
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.Update))]
        [HarmonyPrefix]
        static void GetPos(CaveDwellerAI __instance)
        {
            if (ScienceBirdTweaks.ManeaterTransformInterrupt.Value)
            {
                storedPos = __instance.transform.position;// position every frame
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.LateUpdate))]
        [HarmonyPostfix]
        static void FixPos(CaveDwellerAI __instance)// when animation finishes, it tries to set position back to where it was when the transformation started, we detect this change in position and override it
        {
            if (ScienceBirdTweaks.ManeaterTransformInterrupt.Value && !donePositionFix && Vector3.Distance(storedPos, __instance.transform.position) > 1.5f && __instance.transform.position == startPos)
            {
                __instance.agent.speed = 17f;// also give the maneater a speed boost for fun
                __instance.transform.position = storedPos;
                donePositionFix = true;
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.DoNonBabyUpdateLogic))]
        [HarmonyPrefix]
        static void OwnershipCheck(CaveDwellerAI __instance)
        {
            if (!ScienceBirdTweaks.ManeaterAttackFix.Value) { return; }
            if (__instance.IsOwner && !owner)
            {
                __instance.screamTimer = __instance.screamTime;
            }
            owner = __instance.IsOwner;
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.DoNonBabyUpdateLogic))]
        [HarmonyPostfix]
        static void DoorSpeedPatch(CaveDwellerAI __instance)
        {
            if (ScienceBirdTweaks.ManeaterFastDoors.Value)
            {
                switch (__instance.currentBehaviourStateIndex)
                {
                    case 1:
                    case 2:
                        __instance.openDoorSpeedMultiplier = 2f;
                        __instance.openDoorSpeedMultiplier = 2f;
                        break;
                    case 3:
                        __instance.openDoorSpeedMultiplier = 3f;
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.DoBabyAIInterval))]
        [HarmonyPrefix]
        static void BabyAIPrefix(CaveDwellerAI __instance, out GrabbableObject __state)
        {
            __state = null;
            if (ScienceBirdTweaks.KiwiManeaterScream.Value && __instance.observingScrap != null)
            {
                __state = __instance.observingScrap;
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.DoBabyAIInterval))]
        [HarmonyPostfix]
        static void BabyAIPostfix(CaveDwellerAI __instance, GrabbableObject __state)
        {
            if (ScienceBirdTweaks.KiwiManeaterScream.Value)
            {
                if (__instance.observingScrap != null && __instance.eatingScrap && __instance.observingScrap is KiwiBabyItem kiwiEgg && kiwiEgg.currentAnimation != 5)
                {
                    kiwiEgg.currentAnimation = 5;
                    if (!kiwiEgg.eggAudio.isPlaying || kiwiEgg.eggAudio.clip != kiwiEgg.screamAudio)
                    {
                        kiwiEgg.eggAudio.clip = kiwiEgg.screamAudio;
                        kiwiEgg.eggAudio.Play();
                    }
                    kiwiEgg.babyAnimator.SetInteger("babyAnimation", 5);
                }
                if (__instance.observingScrap != __state && __state != null && __state is KiwiBabyItem pastKiwiEgg)
                {
                    ResetKiwi(pastKiwiEgg);
                }
            }
        }

        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.StopObserving))]
        [HarmonyPrefix]
        static void StopObservingCheck(CaveDwellerAI __instance)
        {
            if (ScienceBirdTweaks.KiwiManeaterScream.Value && __instance.observingScrap != null && __instance.observingScrap is KiwiBabyItem kiwiEgg)
            {
                ResetKiwi(kiwiEgg);
            }
        }

        static void ResetKiwi(KiwiBabyItem kiwi)
        {
            if (kiwi.currentAnimation == 5)
            {
                kiwi.currentAnimation = 4;
                kiwi.eggAudio.Stop();
                kiwi.babyAnimator.SetInteger("babyAnimation", 4);
            }
        }
    }

    [HarmonyPatch]
    public class OldBirdPatch
    {
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void OldBirdPrefabPatch(GameNetworkManager __instance)
        {
            if (!ScienceBirdTweaks.SmokeFix.Value) { return; }

            Material smokeParticleMat = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("SmokeParticle");
            RadMechAI[] oldBirds = UnityEngine.Resources.FindObjectsOfTypeAll<RadMechAI>().ToArray();
            foreach (RadMechAI oldBirdEnemy in oldBirds)
            {
                ParticleSystem[] particleSystems = oldBirdEnemy.gameObject.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.material != null && (renderer.material.name == "Default-Particle (Instance)" || renderer.material.name == "Default-ParticleSystem (Instance)"))
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Found old bird target particle on {particleSystem.gameObject.name}!");
                        renderer.material = smokeParticleMat;
                    }
                }
            }

            GameObject[] objectList = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name == "LargeExplosionEffect").ToArray();// this includes old bird missiles
            foreach (GameObject explosionObject in objectList)
            {
                ParticleSystem[] particleSystems = explosionObject.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.material != null && (renderer.material.name == "Default-Particle (Instance)" || renderer.material.name == "Default-ParticleSystem (Instance)"))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Found explosion target particle!");
                        renderer.material = smokeParticleMat;
                    }
                }
            }
        }
    }
}
