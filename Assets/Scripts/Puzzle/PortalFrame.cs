using UnityEngine;

namespace EchoIslands
{
    public class PortalFrame : MonoBehaviour
    {
        public int requiredOrbs = 1;
        public int nextLevel = 2;
        public Transform[] sockets;
        public Transform playerSpawnOnNext;
        public Renderer[] activeVisuals;
        public GameObject portalArt;
        public PortalActivationVFX activationVFX;
        public bool completeOnEnter = true;

        int filled;
        bool activated;
        public bool IsActivated => activated;
        BoxCollider enterTrigger;
        Transform centralAbsorptionPoint;

        /// <summary>
        /// The socket can be approached from the SIDE, but the orb must always
        /// dissolve in the geometric centre of the portal membrane.
        /// Local coordinates work for rotated portals as well as dungeon gates.
        /// </summary>
        public Transform GetAbsorptionPoint()
        {
            if (centralAbsorptionPoint == null)
            {
                var existing = transform.Find("PortalAbsorptionCenter");
                if (existing == null)
                {
                    var go = new GameObject("PortalAbsorptionCenter");
                    existing = go.transform;
                    existing.SetParent(transform, false);
                }
                centralAbsorptionPoint = existing;
            }
            centralAbsorptionPoint.localPosition = new Vector3(0f, 1.72f, -0.045f);
            centralAbsorptionPoint.localRotation = Quaternion.identity;
            centralAbsorptionPoint.localScale = Vector3.one;
            return centralAbsorptionPoint;
        }

        void Awake()
        {
            if (activationVFX == null)
                activationVFX = GetComponentInChildren<PortalActivationVFX>(true);
            if (activationVFX == null)
                activationVFX = gameObject.AddComponent<PortalActivationVFX>();
            GetAbsorptionPoint();
            CreateEnterTrigger();
            EchoVisualOverhaul.ReskinPortal(this);
            if (activationVFX != null) activationVFX.SetPortalState(false);
            SetVisuals(false);
        }

        void CreateEnterTrigger()
        {
            var existing = transform.Find("PortalEnterTrigger");
            if (existing != null)
            {
                enterTrigger = existing.GetComponent<BoxCollider>();
            }
            else
            {
                var go = new GameObject("PortalEnterTrigger");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                enterTrigger = go.AddComponent<BoxCollider>();
            }
            enterTrigger.isTrigger = true;
            enterTrigger.size = new Vector3(2.6f, 3.4f, 1.25f);
            enterTrigger.enabled = false;
            if (enterTrigger.GetComponent<PortalEntrance>() == null)
                enterTrigger.gameObject.AddComponent<PortalEntrance>();
        }

        public void Activate()
        {
            if (activated) return;
            activated = true;
            EchoVisualOverhaul.ReskinPortal(this);
            SetVisuals(true);
            if (enterTrigger != null) enterTrigger.enabled = completeOnEnter;
            if (activationVFX != null) activationVFX.PlayActivation();
        }

        public void TryInsertHeld()
        {
            if (activated)
            {
                CompleteNextLevel();
                return;
            }

            Orb carriedOrb = null;
            var all = FindObjectsByType<Orb>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].carried)
                {
                    carriedOrb = all[i];
                    break;
                }
            }

            if (carriedOrb == null || filled >= requiredOrbs) return;

            Transform socket = GetAbsorptionPoint();
            carriedOrb.Place(socket, () =>
            {
                filled++;
                if (filled >= requiredOrbs) Activate();
            });
        }

        void OnTriggerEnter(Collider other)
        {
            if (!activated || !completeOnEnter) return;
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            CompleteNextLevel();
        }

        public void TryEnter()
        {
            if (activated && completeOnEnter) CompleteNextLevel();
        }

        void CompleteNextLevel()
        {
            if (!activated) return;
            activated = false;
            if (enterTrigger != null) enterTrigger.enabled = false;
            if (LevelFlow.Instance != null)
                LevelFlow.Instance.CompleteLevel(nextLevel, playerSpawnOnNext);
        }

        public void BindPortalArt(GameObject art)
        {
            portalArt = art;
            if (portalArt != null)
            {
                portalArt.transform.SetParent(transform, false);
                portalArt.SetActive(activated);
                var colliders = portalArt.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null) colliders[i].enabled = false;
            }
        }

        void SetVisuals(bool on)
        {
            if (activeVisuals != null)
            {
                for (int i = 0; i < activeVisuals.Length; i++)
                    if (activeVisuals[i] != null) activeVisuals[i].enabled = on;
            }

            if (portalArt != null)
                portalArt.SetActive(on);
        }
    }
}
