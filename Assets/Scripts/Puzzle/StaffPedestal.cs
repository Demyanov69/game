using UnityEngine;

namespace EchoIslands
{
    public sealed class StaffPedestal : MonoBehaviour
    {
        public PortalFrame portal;
        GameObject placedStaff;

        public string Hint
        {
            get
            {
                if (portal.HasStaff) return "Посох передан хранителям";
                return portal.HasOrb ? "E — положить посох на пьедестал" : "Сначала отдайте сферу порталу";
            }
        }

        public void TryPlace(ToolController tools)
        {
            if (portal == null || tools == null || portal.HasStaff) return;
            if (!portal.HasOrb)
            {
                var hud = FindAnyObjectByType<GameHUD>();
                if (hud != null) hud.Notify("Сначала поднесите сферу к порталу.");
                return;
            }
            if (!tools.RemoveStaff()) return;
            ShowPlacedStaff();
            portal.InsertStaff();
        }

        public void ShowPlacedStaff()
        {
            if (placedStaff != null) return;
            var asset = Resources.Load<GameObject>("EchoL1/AirStaff");
            if (asset == null) return;
            placedStaff = Instantiate(asset,transform,false);
            placedStaff.transform.localPosition = new Vector3(0f,.46f,0f);
            placedStaff.transform.localRotation = Quaternion.Euler(4f,20f,-13f);
            placedStaff.transform.localScale = Vector3.one * .72f;
            foreach (var collider in placedStaff.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            StaffSurface.Apply(placedStaff);
        }
    }
}
