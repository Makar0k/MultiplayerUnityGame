using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PhysPuppet
{
    [SerializeField]
    protected Vector3 currentCamPosition;
    [SerializeField]
    protected Vector3 currentCamRotation;
    [SerializeField]
    protected float baseMovementSpeed = 0;
    [SerializeField]
    private Transform usingCamera;
    private int cameraLayerMask;
    private float verticalInput;
    private Quaternion latestRot;
    private float horizontalInput;
    public GameObject bullet_test;
    private Vector3 movementForce;
    [SerializeField]
    public Transform cursor;
    [SerializeField]
    private Transform locomotionFace;
    [SerializeField]
    private bool isUsingLocomotionFace = false;
    [SerializeField]
    private float locomotionFaceMin = -140f;
    [SerializeField]
    private float locomotionFaceMax = 140f;
    private double rotateTime = 0;
    [HideInInspector]
    public bool isHost = false;
    [HideInInspector]
    public bool isClient = false;
    [SerializeField]
    private MPClient client;
    [SerializeField]
    private MPServer server;
    private float bulletCooldown = 0.3f;
    private float bulletTimer = 0f;
    [SerializeField]
    private float bulletHeight = 1f;
    private float shotAngling = 0;
    [SerializeField]
    private Transform shotFacingObject;
    [SerializeField]
    private float additionalShotRot;
    new void Start()
    {
        base.Start();
        cameraLayerMask = ~(LayerMask.GetMask("Player") | LayerMask.GetMask("IgnoreCursor"));
    }

    new void Update()
    {
        base.Update();
        // Selected Camera Update
        if(bulletTimer > 0)
        {
            bulletTimer -= Time.deltaTime;
        }
        if(Input.GetMouseButton(0) && bulletTimer <= 0)
        {
            if(isHost)
            {
                //var bullet = Instantiate(bullet_test, transform.position + new Vector3(0, bulletHeight, 0) + shotFacingObject.forward, shotFacingObject.rotation);
                server.RequestBullet(transform.position + new Vector3(0, bulletHeight, 0) + shotFacingObject.forward, shotFacingObject.rotation, shotFacingObject.forward * 20f, -1);
            }
            if(isClient)
            {
                client.RequestBullet(transform.position + new Vector3(0, bulletHeight, 0) + shotFacingObject.forward, shotFacingObject.forward * 20f, shotFacingObject.rotation.eulerAngles);
            }
            bulletTimer = bulletCooldown;
        }
    }
    new void FixedUpdate()
    {
        if(isDead)
        return;
        //Debug.DrawRay(transform.position + new Vector3(0, bulletHeight, 0), new Vector3(cursor.position.x, transform.position.y + bulletHeight, cursor.position.z) - new Vector3(transform.position.x, transform.position.y, transform.position.z), Color.green);
        base.FixedUpdate();
        // If camera selected, set position close to player
        if(usingCamera != null)
        {
            usingCamera.position = transform.position + currentCamPosition;
            usingCamera.rotation = Quaternion.Euler(currentCamRotation);
        }
        // Movement
        verticalInput = Input.GetAxis("Horizontal");
        horizontalInput = Input.GetAxis("Vertical");
        movementForce = new Vector3(verticalInput * baseMovementSpeed, 0, horizontalInput * baseMovementSpeed);
        movementForce = Vector3.ClampMagnitude(movementForce, 5);
        var velocityMagn = (new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude);
        if(!inBattle)
        {
            if(movementForce != Vector3.zero) 
            {
                latestRot = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(rb.velocity.x, 0, rb.velocity.z), Vector3.up), Time.fixedDeltaTime * 10f); 
            }
        }
        else 
        {
            var angle = Mathf.Abs(Vector3.SignedAngle(rb.velocity, cursor.position - transform.position, Vector3.up));
            if(movementForce == Vector3.zero) 
            {
                angle = Mathf.Abs(Vector3.SignedAngle(transform.forward, cursor.position - transform.position, Vector3.up));
            }
                if(angle > 110 || angle < 70)
                {
                    latestRot = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(cursor.position.x, 0, cursor.position.z) - new Vector3(transform.position.x, 0, transform.position.z), Vector3.up), Time.fixedDeltaTime * 5f); 
                }
                else
                {
                   latestRot = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(rb.velocity.x, 0, rb.velocity.z), Vector3.up), Time.fixedDeltaTime * 10f); 
                }
                if(velocityMagn <= 0.1f)
                {
                   latestRot = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(cursor.position.x, 0, cursor.position.z) - new Vector3(transform.position.x, 0, transform.position.z), Vector3.up), Time.fixedDeltaTime * 5f); 
                }
                SetAnimatorFloat("LookAngle", Mathf.Abs(angle));


        }
        // Locomotion edits using Animation Rigging components. Selected part of skeleton will be facing to
        // player cursor place. Only by Y coord. There are some math, to make transitions smooth.
        if(usingCamera != null && locomotionFace != null && cursor != null)
        {
            RaycastHit hit;
            Ray ray = usingCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 15f, cameraLayerMask)) 
            {
                cursor.position = Vector3.Lerp(cursor.position, hit.point, Time.fixedDeltaTime * 20f);
                if(isUsingLocomotionFace) {
                    var angle = Vector3.SignedAngle(transform.forward, hit.point - transform.position, Vector3.up);
                    if(angle > locomotionFaceMax || angle < locomotionFaceMin) { angle = 0; }
                    var rot_z = Mathf.Sin((transform.eulerAngles.y + 90 + angle) * Mathf.PI/180);
                    var rot_x = -Mathf.Cos((transform.eulerAngles.y + 90 + angle) * Mathf.PI/180);
                    locomotionFace.localPosition = Vector3.Lerp(locomotionFace.localPosition, new Vector3(transform.localPosition.x+(rot_x * 3), transform.localPosition.y, transform.localPosition.z+(rot_z * 3)), Time.fixedDeltaTime * 15f);
                } else locomotionFace.localPosition =  transform.position + transform.forward;
            }
            else 
            {
                cursor.position = Vector3.Lerp(cursor.position, ray.GetPoint(15f), Time.fixedDeltaTime * 10f);
                if(isUsingLocomotionFace) {
                    var angle = Vector3.SignedAngle(transform.forward, ray.GetPoint(15f) - transform.position, Vector3.up);
                    if(angle > locomotionFaceMax || angle < locomotionFaceMin) { angle = 0; }
                    var rot_z = Mathf.Sin((transform.eulerAngles.y + 90 + angle) * Mathf.PI/180);
                    var rot_x = -Mathf.Cos((transform.eulerAngles.y + 90 +angle) * Mathf.PI/180);
                    locomotionFace.localPosition = Vector3.Lerp(locomotionFace.localPosition, new Vector3(transform.localPosition.x+(rot_x * 3), transform.localPosition.y, transform.localPosition.z+(rot_z * 3)), Time.fixedDeltaTime * 15f);
                } else locomotionFace.localPosition =  transform.position + transform.forward;
            }
        }
        else
        {
            locomotionFace.localPosition =  transform.position + transform.forward;
        }
        transform.rotation = latestRot;
        rb.rotation = latestRot;
        rb.AddForce(movementForce, ForceMode.VelocityChange);
    }
    public override void Kill()
    {
        TurnRagdoll(true);
    }
}

