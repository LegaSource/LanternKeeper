using LanternKeeper.Managers;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours
{
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
            if (enemyObject.TryGet(out NetworkObject networkObject))
                lanternKeeper = networkObject.gameObject.GetComponentInChildren<EnemyAI>() as LanternKeeperAI;
            
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
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_LANTERN_ALREADY_ON);
                return;
            }

            if (this != LanternKeeper.spawnedLanterns[LanternKeeper.currentLanternToLightIndex])
            {
                SwitchOffAllLanternsServerRpc();
                return;
            }

            LanternInteractionServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SwitchOffAllLanternsServerRpc()
        {
            LanternKeeper.currentLanternToLightIndex = 0;
            SwitchOffAllLanternsClientRpc();
        }

        [ClientRpc]
        public void SwitchOffAllLanternsClientRpc()
        {
            foreach (Lantern lantern in LanternKeeper.spawnedLanterns.Where(l => l.isLightOn))
                lantern.SwitchOffLantern();
            lanternKeeper.angerMeter = 1f;
            HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_ALL_LANTERN_OFF);
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

            if (LanternKeeper.currentLanternToLightIndex == 3)
            {
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_ALL_LANTERN_ON);
                SetLanternKeeperVulnerable();
            }
            else
            {
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, LKUtilities.GetLanternColor(currentColorIndex) + Constants.MESSAGE_INFO_LANTERN_ON);
                lanternKeeper.angerMeter += ConfigManager.angerIncrement.Value;
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
                case (int)LanternKeeper.ControlTip.WHITE:
                    lightColor = LanternKeeper.whiteLight;
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

        public void SetLanternKeeperVulnerable()
        {
            lanternKeeper.enemyType.canDie = true;
            lanternKeeper.angerMeter += ConfigManager.angerIncrementLast.Value;
        }

        public void RefreshHoverTip()
            => interactTrigger.hoverTip = $"Light up : [LMB] - {LKUtilities.GetLanternColor(currentColorIndex)}";

        public override void OnDestroy()
        {
            SwitchOffLantern();
            base.OnDestroy();
        }
    }
}
