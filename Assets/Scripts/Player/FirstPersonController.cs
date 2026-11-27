using UnityEngine;

namespace EchoIslands
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public float walkSpeed=4.65f;
        public float jumpHeight=1.08f; // ~8% more travel distance at the same running speed
        public float gravity=-26f;
        public float lookSensitivity=2.2f;
        public Transform cameraPivot;
        CharacterController cc;
        float pitch,verticalSpeed,slipUntil;
        float coyoteTimer;
        float jumpBufferTimer;
        const float CoyoteSeconds = .10f;
        const float JumpBufferSeconds = .12f;
        Vector3 momentum;
        bool locked=true;
        CapsuleCollider wallSensor;
        const float Skin = .025f;

        // A rotated capsule sweep uses the same feet, radius and head on every face.
        // Its trigger companion keeps the player's interactions live off the floor.
        void EnableWallSensor(bool active)
        {
            if (active && wallSensor == null)
            {
                var sensor = new GameObject("Oriented player capsule");
                sensor.transform.SetParent(transform, false);
                wallSensor = sensor.AddComponent<CapsuleCollider>();
                wallSensor.center = cc.center;
                wallSensor.height = cc.height;
                wallSensor.radius = cc.radius;
                wallSensor.isTrigger = true;
                var body = sensor.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
            }
            if (wallSensor != null) wallSensor.gameObject.SetActive(active);
        }

        bool CastBody(Vector3 direction, float length, out RaycastHit hit)
        {
            Vector3 center = transform.TransformPoint(cc.center);
            Vector3 axis = transform.up * (cc.height * .5f - cc.radius);
            return Physics.CapsuleCast(center - axis, center + axis, cc.radius - Skin,
                direction, out hit, length, ~0, QueryTriggerInteraction.Ignore);
        }

        void MoveOnFace(Vector3 motion)
        {
            for (int i = 0; i < 3 && motion.sqrMagnitude > .000001f; i++)
            {
                float length = motion.magnitude;
                Vector3 direction = motion / length;
                if (!CastBody(direction, length + Skin, out var hit))
                {
                    transform.position += motion;
                    break;
                }
                float advance = Mathf.Clamp(hit.distance - Skin, 0f, length);
                transform.position += direction * advance;
                motion = Vector3.ProjectOnPlane(motion - direction * advance, hit.normal);
                if (Vector3.Dot(hit.normal, transform.up) < -.6f && verticalSpeed > 0f)
                    verticalSpeed = 0f;
            }
        }
        public GravitySide GravityOrientation { get; private set; }=GravitySide.Floor;

        void Awake()
        {
            cc=GetComponent<CharacterController>();
            if(cameraPivot==null&&Camera.main!=null)cameraPivot=Camera.main.transform;
        }
        void Start()
        {
            lookSensitivity=PlayerPrefs.GetFloat("echo_sensitivity",lookSensitivity);
            Cursor.lockState=CursorLockMode.Locked;
            Cursor.visible=false;
        }
        void Update()
        {
            if(!locked||cc==null||Time.timeScale<=0f)return;
            bool wall = GravityOrientation != GravitySide.Floor;
            if (!wall && !cc.enabled) return;
            float sens=lookSensitivity;
            transform.Rotate(0f,Input.GetAxis("Mouse X")*sens,0f,Space.Self);
            pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*sens,-85f,85f);
            if(cameraPivot!=null)cameraPivot.localRotation=Quaternion.Euler(pitch,0f,0f);
            Vector3 up=transform.up;
            bool grounded = verticalSpeed <= 0f && ((!wall && cc.isGrounded) ||
                (CastBody(-up, Skin + .09f, out var support) && Vector3.Dot(support.normal, up) > .64f));
            if (grounded) { coyoteTimer = CoyoteSeconds; if (verticalSpeed < 0f) verticalSpeed = -2f; }
            else coyoteTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space)) jumpBufferTimer = JumpBufferSeconds;
            else jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
            if (Input.GetKeyUp(KeyCode.Space) && verticalSpeed > 0f) verticalSpeed *= .58f;
            verticalSpeed += gravity * Time.deltaTime;
            Vector3 wish=transform.right*Input.GetAxisRaw("Horizontal")+
                         transform.forward*Input.GetAxisRaw("Vertical");
            if(wish.sqrMagnitude>1f)wish.Normalize();
            bool slippery=Time.time<slipUntil;
            float acceleration=slippery?1.0f:(grounded?18f:8.5f);
            momentum=Vector3.Lerp(momentum,wish * walkSpeed * (slippery ? .88f : 1f),1f-Mathf.Exp(-acceleration*Time.deltaTime));
            // Preserve precise air steering but avoid acceleration past the course's measured gap size.
            Vector3 displacement = (momentum + up * verticalSpeed) * Time.deltaTime;
            if (wall) MoveOnFace(displacement);
            else cc.Move(displacement);
            if (LevelFlow.Instance != null && LevelFlow.Instance.current == 3)
            {
                var temple = LevelFlow.Instance.levelRoots[2].transform;
                Vector3 local = temple.InverseTransformPoint(transform.position);
                if (Mathf.Abs(local.x) > 8.5f || local.y < -3f || local.y > 11f || local.z < -5f)
                    LevelFlow.Instance.Respawn();
            }
        }
        public void SetSlippery(bool active){slipUntil=active?Time.time+.15f:0f;}
        public void ApplyExternalMotion(Vector3 motion)
        {
            if(enabled&&cc!=null&&cc.enabled)cc.Move(motion);
        }
        public void SetGravitySide(GravitySide side,Vector3 landing)
        {
            if(cc==null)return;
            cc.enabled=false;
            Vector3 up=Vector3.up;
            if(side==GravitySide.LeftWall)up=Vector3.right;
            if(side==GravitySide.RightWall)up=Vector3.left;
            if(side==GravitySide.Ceiling)up=Vector3.down;
            transform.rotation=Quaternion.LookRotation(Vector3.forward,up);
            transform.position=landing;
            pitch=0f;verticalSpeed=-1f;momentum=Vector3.zero;
            if(cameraPivot!=null)cameraPivot.localRotation=Quaternion.identity;
            GravityOrientation=side;
            coyoteTimer = jumpBufferTimer = 0f;
            EnableWallSensor(side != GravitySide.Floor);
            cc.enabled = side == GravitySide.Floor;
        }
        public void ResetGravity()
        {
            EnableWallSensor(false);
            pitch = 0f;
            if(cameraPivot!=null) cameraPivot.localRotation=Quaternion.identity;
            Vector3 forward=Vector3.ProjectOnPlane(transform.forward,Vector3.up);
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            transform.rotation=Quaternion.LookRotation(forward,Vector3.up);
            GravityOrientation=GravitySide.Floor;
            verticalSpeed=0f;
            momentum=Vector3.zero;
            slipUntil=0f;
            coyoteTimer=0f;
            jumpBufferTimer=0f;
        }
        public void SetControl(bool active)
        {
            enabled=active;
            locked=active;
            Cursor.lockState=active?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!active;
        }
    }
}
