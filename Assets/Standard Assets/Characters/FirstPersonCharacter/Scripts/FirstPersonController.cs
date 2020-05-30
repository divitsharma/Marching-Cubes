using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private bool m_Jumping;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            // This is the nested camera
            m_Camera = Camera.main;
            m_Jumping = false;
			m_MouseLook.Init(transform , m_Camera.transform);
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = Input.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }


        private void FixedUpdate()
        {
            GetInput(out float speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = m_Camera.transform.forward*m_Input.y + m_Camera.transform.right*m_Input.x;

            // Not moving across surface rn
            // get a normal for the surface that is being touched to move along it
            //RaycastHit hitInfo;
            //Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
            //                   m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            //desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            // This refers to the local x and z axes
            m_MoveDir = desiredMove * speed;

            //if (m_CharacterController.isGrounded)
            //{
            //    m_MoveDir.y = -m_StickToGroundForce;

            //    if (m_Jump)
            //    {
            //        m_MoveDir.y = m_JumpSpeed;
            //        m_Jump = false;
            //        m_Jumping = true;
            //    }
            //}
            //else
            //{
            //    m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            //}

            // Not sure what collision flags refers to
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);


            m_MouseLook.UpdateCursorLock();
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
      
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


    }
}
