﻿using GameNetcodeStuff;
using LanternKeeper.Managers;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class Lantern : NetworkBehaviour
{
    public bool isLightOn = false;
    public bool isOutside;
    public int currentColorIndex;
    public LanternKeeperAI lanternKeeper;

    public InteractTrigger interactTrigger;
    public GameObject light1;
    public GameObject light2;

    public Quaternion lightRotation;
    public Vector3 light1Position;
    public Vector3 light2Position;

    [ClientRpc]
    public void InitializeLanternClientRpc(NetworkObjectReference enemyObject, int index, bool isOutside)
    {
        if (enemyObject.TryGet(out NetworkObject networkObject)) lanternKeeper = networkObject.gameObject.GetComponentInChildren<EnemyAI>() as LanternKeeperAI;

        LanternKeeper.spawnedLanterns.Add(this);
        LanternKeeper.currentLanternToLightIndex = 0;

        lightRotation = light1.transform.rotation;
        light1Position = light1.transform.position;
        light2Position = light2.transform.position;
        light1 = null;
        light2 = null;

        currentColorIndex = index;
        this.isOutside = isOutside;

        RefreshHoverTip();
    }

    public void LanternInteraction()
    {
        if (isLightOn)
        {
            if (LanternKeeper.teleportOnCooldown)
            {
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_TELEPORT_COOLDOWN);
                return;
            }

            Lantern[] eligibleLanterns = LanternKeeper.spawnedLanterns.Where(l => l != this).ToArray();
            if (eligibleLanterns.Length > 0)
            {
                Lantern lantern = eligibleLanterns[new System.Random().Next(eligibleLanterns.Length)];
                Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(lantern.transform.position);

                PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
                player.isInsideFactory = !player.isInsideFactory;
                player.averageVelocity = 0f;
                player.velocityLastFrame = Vector3.zero;
                player.TeleportPlayer(position);
                _ = StartCoroutine(TeleportCooldownCoroutine());
                return;
            }
            HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_LANTERN_ALREADY_ON);
            return;
        }

        if (this != LanternKeeper.spawnedLanterns[LanternKeeper.currentLanternToLightIndex])
        {
            TeleportLanternKeeperServerRpc();
            return;
        }

        LanternInteractionServerRpc();
    }

    public IEnumerator TeleportCooldownCoroutine()
    {
        LanternKeeper.teleportOnCooldown = true;
        yield return new WaitForSeconds(ConfigManager.teleportationCooldown.Value);
        LanternKeeper.teleportOnCooldown = false;
    }

    public void SwitchOffLantern()
    {
        isLightOn = false;
        if (light1 != null)
        {
            Destroy(light1.gameObject);
            light1 = null;
        }
        if (light2 != null)
        {
            Destroy(light2.gameObject);
            light2 = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LanternInteractionServerRpc()
        => LanternInteractionClientRpc();

    [ClientRpc]
    public void LanternInteractionClientRpc()
    {
        isLightOn = true;
        InstantiateLight(ref light1, light1Position);
        InstantiateLight(ref light2, light2Position);

        lanternKeeper.lastLanternLit = this;

        if (LanternKeeper.currentLanternToLightIndex == 2)
        {
            HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_ALL_LANTERN_ON);
            SetLanternKeeperVulnerable();
        }
        else
        {
            HUDManager.Instance.DisplayTip(Constants.INFORMATION, LKUtilities.GetLanternColor(currentColorIndex) + Constants.MESSAGE_INFO_LANTERN_ON);
            BoostLanternKeeper();
            LanternKeeper.currentLanternToLightIndex++;
        }
    }

    public void InstantiateLight(ref GameObject lightObject, Vector3 lightPosition)
    {
        GameObject lightColor;
        switch (currentColorIndex)
        {
            case (int)LanternKeeper.ControlTip.RED:
                lightColor = LanternKeeper.redLight;
                break;
            case (int)LanternKeeper.ControlTip.BLUE:
                lightColor = LanternKeeper.blueLight;
                break;
            case (int)LanternKeeper.ControlTip.GREEN:
                lightColor = LanternKeeper.greenLight;
                break;
            default:
                return;
        }

        if (lightObject == null)
        {
            lightObject = Instantiate(lightColor, lightPosition, lightRotation);
            if (isOutside)
            {
                Light light = lightObject.GetComponent<Light>();
                light.range *= 2f;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportLanternKeeperServerRpc()
    {
        BoostLanternKeeperClientRpc();

        Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(transform.position);
        _ = lanternKeeper.StartCoroutine(lanternKeeper.TeleportEnemyCoroutine(position, isOutside));
    }

    [ClientRpc]
    public void BoostLanternKeeperClientRpc()
        => BoostLanternKeeper();

    public void BoostLanternKeeper()
        => lanternKeeper.angerMeter += ConfigManager.angerIncrement.Value;

    public void SetLanternKeeperVulnerable()
    {
        lanternKeeper.enemyType.canDie = true;

        if (!GameNetworkManager.Instance.localPlayerController.IsServer && !GameNetworkManager.Instance.localPlayerController.IsHost) return;
        TeleportLanternKeeperServerRpc();
    }

    public void RefreshHoverTip()
        => interactTrigger.hoverTip = $"Light up : [LMB] - {LKUtilities.GetLanternColor(currentColorIndex)}";

    public override void OnDestroy()
    {
        SwitchOffLantern();
        base.OnDestroy();
    }
}
