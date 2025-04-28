using HarmonyLib;
using LanternKeeper.Behaviours;
using LanternKeeper.Managers;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Patches;

internal class RoundManagerPatch
{
    public static bool isLanternKeeperSpawned = false; // Valorisé à vrai seulement chez le host

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
    [HarmonyPostfix]
    private static void SpawnOutsideHazards(ref RoundManager __instance)
    {
        LanternKeeper.spawnedLanterns.Clear();

        if (!__instance.IsHost) return;

        if (new System.Random().Next(1, 100) <= ConfigManager.rarity.Value)
        {
            isLanternKeeperSpawned = true;

            NetworkObject enemyObject = SpawnLanternKeeper(__instance);
            LKUtilities.Shuffle(LanternKeeper.shuffledLights);
            SpawnLantern(__instance, enemyObject);
        }
    }

    public static NetworkObject SpawnLanternKeeper(RoundManager roundManager)
    {
        Vector3 spawnPosition = roundManager.outsideAINodes[Random.Range(0, roundManager.outsideAINodes.Length)].transform.position;
        spawnPosition = roundManager.GetRandomNavMeshPositionInRadiusSpherical(spawnPosition);

        GameObject gameObject = Object.Instantiate(LanternKeeper.lanternKeeperEnemy.enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = gameObject.GetComponentInChildren<NetworkObject>();
        networkObject.Spawn(true);
        return networkObject;
    }

    public static void SpawnLantern(RoundManager roundManager, NetworkObject enemyObject)
    {
        System.Random random = new System.Random();
        const float minDistance = 100f;
        List<Vector3> selectedPositions =
        [
            StartOfRound.Instance.shipLandingPosition.position,
            RoundManager.FindMainEntrancePosition()
        ];

        LKUtilities.Shuffle(roundManager.outsideAINodes);
        LKUtilities.Shuffle(roundManager.insideAINodes);

        // Créer un ordre aléatoire pour les lanternes (2 extérieures, 2 intérieures)
        List<bool> spawnOrder = [true, true, false];
        LKUtilities.Shuffle(spawnOrder);

        for (int i = 0; i < spawnOrder.Count; i++)
        {
            Vector3 bestPosition = Vector3.zero;
            float maxDistance = float.MinValue;

            // Déterminer si cette lanterne est à l'extérieur ou à l'intérieur
            bool isOutside = spawnOrder[i];
            GameObject[] nodes = isOutside ? roundManager.outsideAINodes : roundManager.insideAINodes;
            float radius = isOutside ? 10f : 2f;

            foreach (GameObject node in nodes)
            {
                Vector3 candidatePosition = roundManager.GetRandomNavMeshPositionInBoxPredictable(node.transform.position, radius, default, random) + Vector3.up;
                if (!Physics.Raycast(candidatePosition, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) continue;

                Vector3 validPosition = hit.point;

                // Calculer la distance minimale avec les positions sélectionnées
                float minDistanceToSelected = selectedPositions.Count > 0
                    ? selectedPositions.Min(p => Vector3.Distance(p, validPosition))
                    : float.MaxValue;

                // Garder la position la plus éloignée des autres sélectionnées
                if (minDistanceToSelected > minDistance || minDistanceToSelected > maxDistance)
                {
                    maxDistance = minDistanceToSelected;
                    bestPosition = validPosition;

                    if (minDistanceToSelected > minDistance) break;
                }
            }

            if (bestPosition != Vector3.zero)
            {
                selectedPositions.Add(bestPosition);

                GameObject gameObject = Object.Instantiate(LanternKeeper.lanternObj, bestPosition + (Vector3.down * 0.5f), Quaternion.identity, roundManager.mapPropsContainer.transform);
                Lantern lantern = gameObject.GetComponent<Lantern>();

                if (isOutside) lantern.transform.localScale *= 2f;

                gameObject.GetComponent<NetworkObject>().Spawn(true);
                lantern.InitializeLanternClientRpc(enemyObject, LanternKeeper.shuffledLights[i], isOutside);
            }
        }
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DetectElevatorIsRunning))]
    [HarmonyPostfix]
    private static void EndGame()
    {
        foreach (Lantern lantern in LanternKeeper.spawnedLanterns)
        {
            if (lantern == null || !lantern.IsSpawned) continue;
            lantern.GetComponent<NetworkObject>().Despawn(destroy: true);
        }
        LanternKeeper.spawnedLanterns.Clear();

        foreach (FortuneCookie fortuneCookie in Object.FindObjectsOfType<FortuneCookie>()) fortuneCookie.DestroyObjectServerRpc();
    }
}
