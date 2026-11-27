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

        public bool requiresStaff;
        public bool HasOrb => filled >= requiredOrbs;
        public bool HasStaff { get; private set; }
        public bool IsAbsorbing { get; private set; }
        int filled;
        bool activated;
        float enterAfter;
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
            if (GetComponent<Collider>() == null)
            {
                var interaction = gameObject.AddComponent<BoxCollider>();
                interaction.isTrigger = true;
                interaction.center = new Vector3(0f,1.62f,-.06f);
                interaction.size = new Vector3(2.75f,3.18f,.50f);
            }
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
            if (enterTrigger == null) enterTrigger = existing.gameObject.AddComponent<BoxCollider>();
            enterTrigger.isTrigger = true;
            enterTrigger.gameObject.layer = 2;
            if (enterTrigger.attachedRigidbody == null)
            {
                var body = enterTrigger.gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
            }
            enterTrigger.size = new Vector3(2.6f, 3.4f, 1.25f);
            enterTrigger.enabled = false;
            if (enterTrigger.GetComponent<PortalEntrance>() == null)
                enterTrigger.gameObject.AddComponent<PortalEntrance>();
        }

        public void Activate()
        {
            if (activated) return;
            activated = true;
            enterAfter = Time.time + .65f;
            EchoVisualOverhaul.ReskinPortal(this);
            SetVisuals(true);
            if (enterTrigger != null) enterTrigger.enabled = completeOnEnter;
            if (activationVFX != null) activationVFX.PlayActivation();
        }

        void TryOpen()
        {
            if (HasOrb && (!requiresStaff || HasStaff)) Activate();
        }

        public void InsertStaff()
        {
            HasStaff = true;
            TryOpen();
            if (LevelFlow.Instance != null) LevelFlow.Instance.SaveCurrent();
        }

        public void RestoreRequirements(bool orb, bool staff)
        {
            filled = orb ? requiredOrbs : 0;
            HasStaff = staff && requiresStaff && orb;
            if (HasStaff)
            {
                var pedestal = GetComponentInChildren<StaffPedestal>();
                if (pedestal != null) pedestal.ShowPlacedStaff();
            }
            TryOpen();
        }

        public void TryInsertHeld()
        {
            if (activated || IsAbsorbing || HasOrb) return;
            Orb carried = null;
            foreach (var orb in FindObjectsByType<Orb>(FindObjectsSortMode.None))
                if (orb.carried) { carried = orb; break; }

            if (carried == null)
            {
                var hud = FindAnyObjectByType<GameHUD>();
                if (hud != null) hud.Notify("Для портала нужна сфера из тёмного мира.");
                return;
            }

            IsAbsorbing = true;
            carried.Place(GetAbsorptionPoint(), () =>
            {
                IsAbsorbing = false;
                filled++;
                TryOpen();
                if (HasOrb && requiresStaff && !HasStaff)
                {
                    var hud = FindAnyObjectByType<GameHUD>();
                    if (hud != null) hud.Notify("Сфера принята. Оставьте посох на пьедестале справа.",4.2f);
                }
                if (LevelFlow.Instance != null) LevelFlow.Instance.SaveCurrent();
            });
        }

        void OnTriggerEnter(Collider other)
        {
            if (!activated || !completeOnEnter) return;
            var flow = LevelFlow.Instance;
            if (flow == null || other.GetComponentInParent<FirstPersonController>() != flow.Controller) return;
            TryEnter();
        }

        public void TryEnter()
        {
            if (activated && completeOnEnter && Time.time >= enterAfter) CompleteNextLevel();
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
