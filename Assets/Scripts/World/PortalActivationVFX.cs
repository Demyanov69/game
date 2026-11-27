using UnityEngine;

namespace EchoIslands
{
    /// <summary>Pulse only the portal membrane/light; never change the asset masonry.</summary>
    public sealed class PortalActivationVFX : MonoBehaviour
    {
        public float activationDuration = 1.30f;
        public float activeLightIntensity = 5.0f;
        public float idlePulseSpeed = 2.2f;
        Renderer energy;
        MaterialPropertyBlock block;
        Light[] lights;
        bool active;
        float energyLevel;
        float time;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            EnsureLights();
        }
        void EnsureLights()
        {
            lights = new Light[3];
            var locations = new[]
            {
                new Vector3(0f,1.72f,-0.12f),
                new Vector3(-0.55f,2.10f,0.12f),
                new Vector3(0.55f,1.35f,0.12f)
            };
            for(int i=0;i<lights.Length;i++)
            {
                string name="AuroraLight_"+i;
                var child=transform.Find(name);
                if(child==null)
                {
                    var obj=new GameObject(name);
                    child=obj.transform;
                    child.SetParent(transform,false);
                }
                child.localPosition=locations[i];
                lights[i]=child.GetComponent<Light>();
                if(lights[i]==null)lights[i]=child.gameObject.AddComponent<Light>();
                lights[i].type=LightType.Point;
                lights[i].range=7.2f;
                lights[i].color=new Color(.26f,1f,.86f);
                lights[i].shadows=LightShadows.None;
            }
        }
        void LateUpdate()
        {
            if (!active && energyLevel <= 0f) return;
            time+=Time.deltaTime;
            float target=active?1f:0f;
            energyLevel=Mathf.MoveTowards(energyLevel,target,Time.deltaTime/Mathf.Max(.1f,activationDuration));
            Render();
        }
        public void SetPortalState(bool newState)
        {
            active=newState;
            energyLevel=newState?1f:0f;
            time=0f;
            Render();
        }
        public void PlayActivation()
        {
            active=true;
            energyLevel=0f;
            time=0f;
            Render();
        }
        void Render()
        {
            if(energy==null)
            {
                var surface=transform.Find("RiftTear");
                if(surface!=null)energy=surface.GetComponent<Renderer>();
            }
            if(energy!=null)
            {
                energy.enabled=active;
                energy.GetPropertyBlock(block);
                block.SetFloat("_Activation",energyLevel);
                energy.SetPropertyBlock(block);
            }
            float intensity=(active?.35f:0f)+energyLevel*activeLightIntensity*
                (1f+Mathf.Sin(time*idlePulseSpeed)*.13f);
            if(lights!=null)
                for(int i=0;i<lights.Length;i++)
                    if(lights[i]!=null)lights[i].intensity=intensity;
        }
    }
}
