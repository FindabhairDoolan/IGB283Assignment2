using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class Limb : MonoBehaviour
{
    // Up to three child limbs (order matters for DFS ops)
    [SerializeField] private Limb[] children = new Limb[3];

    //for fall over function
    [SerializeField] private Limb child;

    //Default angle of each limb
    [SerializeField] private float defaultAngle = 0f;

    // Keep the joint location from being altered after Start
    [SerializeField] private Vector2 initialJointLocation;
    private Vector2 jointLocation;

    //Store the previous angle to undo
    private float lastAngle = 0;

    //The corner positions of each limb
    [SerializeField] private Vector3[] limbVertices = new Vector3[4];
    private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private Color colour = Color.white;

    //Nodding parameters (only used by head)
    [SerializeField] private bool nodding = false;
    private float nodAmplitude = 0.26f;//about 15 degrees in radians
    private float nodSpeed = 4f; //how fast QUT Jr nods
    private float nodBaseAngle; //base angle to oscillate around

    //Walking parameters (for base)
    [SerializeField] private bool walking = false;
    private float walkSpeed = 2f;
    private float leftBoundary = -10f;
    private float rightBoundary = 10f;
    private int walkDirection = 1; //1 = right, -1 = left
    private bool flipped = false;//track direction

    //Input keys for original
    [SerializeField] public InputAction wKey;
    [SerializeField] private InputAction aKey;
    [SerializeField] private InputAction sKey;
    [SerializeField] private InputAction dKey;
    [SerializeField] private InputAction zKey;

    //Input keys for clone
    [SerializeField] private InputAction upArrow;
    [SerializeField] private InputAction leftArrow;
    [SerializeField] private InputAction downArrow;
    [SerializeField] private InputAction rightArrow;
    [SerializeField] private InputAction rightCtrl;

    [SerializeField] public bool useArrowInputs = false;

    //Movement state of the model
    private enum State { Walking, Jumping, Leaping, Falling, Rising }
    private State currentState = State.Walking; //Initially walking

    //More movement defining variables
    private bool onGround = true;
    private float jumpVelocity = 0f;
    private float gravity = -9.8f;
    private bool jumpHeld = false;
    private float collapseSpeed = 3f;

    private void Awake()
    {
        DrawLimb();
    }

    private IEnumerator Start()
    {
        initialMove(initialJointLocation);
        yield return null; //Ensure model is initialised properly before applying transformations
        ApplyInitialRotation();

        //Store the starting angle so the nod oscillates around it
        nodBaseAngle = lastAngle;
    }

    void Update()
    {
        if (nodding)
        {
            float oscillation = Mathf.Sin(Time.time * nodSpeed) * nodAmplitude;
            RotateAroundPoint(jointLocation, nodBaseAngle + oscillation);
        }

        if (walking && currentState == State.Walking)
        {
            if ((walkDirection == 1 && flipped) || (walkDirection == -1 && !flipped))
            {
                FlipModel();
            }
            Walk();
        }

        if (currentState == State.Jumping || currentState == State.Leaping)
        {
            PerformJumpOrLeap();
        }
    }

    private void DrawLimb()
    {
        mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>().material = material;

        mesh.vertices = limbVertices;
        mesh.colors = new Color[] { colour, colour, colour, colour };
        mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
    }

    private void ApplyTransformation(Matrix3x3 transformation)
    {
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = (Vector3)transformation.MultiplyPoint(vertices[i]);
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        jointLocation = (Vector2)transformation.MultiplyPoint(jointLocation);

        // Propagate to children
        foreach (var c in children)
        {
            if (c != null) c.ApplyTransformation(transformation);
        }

        // Update the collider to follow
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Bounds bounds = mesh.bounds;
        col.offset = bounds.center;
        col.size = bounds.size;
    }

    private void initialMove(Vector2 offset)
    {
        Matrix3x3 t = IGB283Transform.Translate(offset);
        ApplyTransformation(t);
    }

    private void Move(IGB283Vector offset)
    {
        Matrix3x3 t = IGB283Transform.Translate(offset);
        ApplyTransformation(t);
    }

    private void RotateAroundPoint(Vector2 point, float angle)
    {
        Matrix3x3 tInv = IGB283Transform.Translate(-point);
        Matrix3x3 rLastAngle = IGB283Transform.Rotate(-lastAngle);
        Matrix3x3 rAngle = IGB283Transform.Rotate(angle);
        Matrix3x3 t = IGB283Transform.Translate(point);

        ApplyTransformation(t * rAngle * rLastAngle * tInv);
        lastAngle = angle;
    }

    private void Walk()
    {
        IGB283Vector offset = new IGB283Vector(walkSpeed * walkDirection * Time.deltaTime, 0f, 1f);
        Move(offset);

        float currentX = jointLocation.x;
        if (currentX >= rightBoundary)
        {
            walkDirection = -1;
            FlipModel();
        }
        else if (currentX <= leftBoundary)
        {
            walkDirection = 1;
            FlipModel();
        }
    }

    private void FlipModel()
    {
        Matrix3x3 toOrigin = IGB283Transform.Translate(-jointLocation);
        Matrix3x3 flip = IGB283Transform.FlipX();
        Matrix3x3 back = IGB283Transform.Translate(jointLocation);

        ApplyTransformation(back * flip * toOrigin);

        flipped = !flipped;

        // Notify all children
        foreach (var c in children)
        {
            if (c != null) c.OnParentFlipped(flipped);
        }
    }

    //Flip all descendants
    public void OnParentFlipped(bool isFlipped)
    {
        flipped = isFlipped;
        lastAngle = -lastAngle;
        nodBaseAngle = -nodBaseAngle;

        foreach (var c in children)
        {
            if (c != null) c.OnParentFlipped(isFlipped);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            onGround = true;
            jumpVelocity = 0f;

            if (currentState == State.Jumping || currentState == State.Leaping)
            {
                currentState = State.Walking;
            }

            if (jumpHeld && currentState == State.Walking)
            {
                TryStartJump();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            onGround = false;
        }
    }

    private void PerformJumpOrLeap()
    {
        float deltaY = jumpVelocity * Time.deltaTime;
        jumpVelocity += gravity * Time.deltaTime;

        IGB283Vector moveVec = new IGB283Vector(0, deltaY, 1);

        if (currentState == State.Leaping)
        {
            float horizontalMove = walkSpeed * walkDirection * Time.deltaTime;
            moveVec.x = horizontalMove;

            float nextX = jointLocation.x + moveVec.x;

            if (nextX >= rightBoundary && walkDirection == 1)
            {
                walkDirection = -1;
                FlipModel();
                moveVec.x = -Mathf.Abs(horizontalMove);
            }
            else if (nextX <= leftBoundary && walkDirection == -1)
            {
                walkDirection = 1;
                FlipModel();
                moveVec.x = Mathf.Abs(horizontalMove);
            }
        }

        ApplyTransformation(IGB283Transform.Translate(moveVec));
    }

    private void FlipLeft(InputAction.CallbackContext context)
    {
        if (currentState != State.Walking || flipped) return;
        walkDirection = -1;
        FlipModel();
    }

    private void FlipRight(InputAction.CallbackContext context)
    {
        if (currentState != State.Walking || !flipped) return;
        walkDirection = 1;
        FlipModel();
    }

    private void StartJumpHold(InputAction.CallbackContext context)
    {
        jumpHeld = true;
        TryStartJump();
    }

    private void StopJumpHold(InputAction.CallbackContext context)
    {
        jumpHeld = false;
    }

    private void TryStartJump()
    {
        if (!onGround || currentState != State.Walking) return;
        currentState = State.Jumping;
        onGround = false;
        jumpVelocity = 5f;
    }

    private void LeapModel(InputAction.CallbackContext context)
    {
        if (!onGround || currentState != State.Walking) return;
        currentState = State.Leaping;
        onGround = false;
        jumpVelocity = 5f;
    }

    private void Fall(InputAction.CallbackContext context)
    {
        if (onGround && currentState == State.Walking)
        {
            StartCoroutine(CollapseModel());
        }
    }

    private void ApplyInitialRotation()
    {
        RotateAroundPoint(jointLocation, defaultAngle);
    }

    public IEnumerator CollapseModel()
    {
        currentState = State.Falling;
        DisableInputs();
        walking = false;
        DisableNodding();
        ResetGroundContact(this);

        Limb current = child;
        while (current != null)
        {
            yield return current.RotateUntilGrounded();
            current = current.child;
        }

        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(RiseModel());
    }

    public IEnumerator RiseModel()
    {
        currentState = State.Rising;
        DisableInputs();
        walking = false;
        DisableNodding();

        //Start recursive rise limb rise
        yield return StartCoroutine(RiseRecursive());

        //If is the base limb restore variables
        if (transform.parent == null)
        {
            onGround = true;
            jumpVelocity = 0f;
            walking = true;
            EnableInputs();
            EnableNodding();
        }

        currentState = State.Walking;
    }

    //Rise last child first
    private IEnumerator RiseRecursive()
    {
        //Go down limb hierarchy
        if (child != null)
            yield return StartCoroutine(child.RiseRecursive());

        //Rotate last child first
        yield return StartCoroutine(RotateToDefault());
    }

    //Rotate this limb back to default pose
    private IEnumerator RotateToDefault()
    {
        float angle = lastAngle;
        float target = flipped ? -defaultAngle : defaultAngle; //account for flipping
        float speed = collapseSpeed;

        //Keep rotating upwards until pretty much back in default pose
        while (Mathf.Abs(angle - target) > 0.01f)
        {
            float step = Mathf.Sign(target - angle) * speed * Time.deltaTime;
            angle += step;
            RotateAroundPoint(jointLocation, angle);
            yield return null; //Wait a frame for gradual rising
        }

        //Ensure at default pose by snapping the rest of the way (should be close enough by now to be smooth)
        RotateAroundPoint(jointLocation, target);
        lastAngle = target;
    }

    //Rotate this limb backward until it touches ground
    private IEnumerator RotateUntilGrounded()
    {
        float angle = lastAngle;

        //timeout just in case infinite loop
        float timeout = 3f;
        float timer = 0f;
        float backwardDirection = flipped ? -1f : 1f; //What direction is backwards

        //Keep going backward until on the ground
        while (!onGround && timer < timeout)
        {
            angle += backwardDirection * collapseSpeed * Time.deltaTime;
            RotateAroundPoint(jointLocation, angle);
            timer += Time.deltaTime;
            yield return null; //Wait a frame for gradual lowering
        }

        //hold final angle
        lastAngle = angle;
    }

    //Disable nodding for head limb
    private void DisableNodding()
    {
        if (child != null)
        {
            child.DisableNodding();
        }
        else
        {
            nodding = false;
        }
    }

    //Enable nodding for head limb
    private void EnableNodding()
    {
        if (child != null)
        {
            child.EnableNodding();
        }
        else
        {
            nodding = true;
        }
    }


    private void OnEnable()
    {
        EnableInputs();

        if (useArrowInputs)
        {
            upArrow.started += StartJumpHold;
            upArrow.canceled += StopJumpHold;
            leftArrow.performed += FlipLeft;
            downArrow.performed += LeapModel;
            rightArrow.performed += FlipRight;
            rightCtrl.performed += Fall;
        }
        else
        {
            wKey.started += StartJumpHold;
            wKey.canceled += StopJumpHold;
            aKey.performed += FlipLeft;
            sKey.performed += LeapModel;
            dKey.performed += FlipRight;
            zKey.performed += Fall;
        }
    }

    private void OnDisable()
    {
        DisableInputs();

        //WASD
        wKey.started -= StartJumpHold;
        wKey.canceled -= StopJumpHold;
        aKey.performed -= FlipLeft;
        sKey.performed -= LeapModel;
        dKey.performed -= FlipRight;
        zKey.performed -= Fall;

        //Arrows
        upArrow.started -= StartJumpHold;
        upArrow.canceled -= StopJumpHold;
        leftArrow.performed -= FlipLeft;
        downArrow.performed -= LeapModel;
        rightArrow.performed -= FlipRight;
        rightCtrl.performed -= Fall;
    }

    public void DisableInputs()
    {
        //WASD
        wKey.Disable();
        aKey.Disable();
        sKey.Disable();
        dKey.Disable();
        zKey.Disable();

        //Arrows
        upArrow.Disable();
        leftArrow.Disable();
        downArrow.Disable();
        rightArrow.Disable();
        rightCtrl.Disable();
    }

    public void EnableInputs()
    {
        if (useArrowInputs)
        {
            upArrow.Enable();
            leftArrow.Enable();
            downArrow.Enable();
            rightArrow.Enable();
            rightCtrl.Enable();

            wKey.Disable();
            aKey.Disable();
            sKey.Disable();
            dKey.Disable();
            zKey.Disable();
        }
        else
        {
            wKey.Enable();
            aKey.Enable();
            sKey.Enable();
            dKey.Enable();
            zKey.Enable();

            upArrow.Disable();
            leftArrow.Disable();
            downArrow.Disable();
            rightArrow.Disable();
            rightCtrl.Disable();
        }
    }

    //Clear onGround before collapse
    private void ResetGroundContact(Limb limb)
    {
        limb.onGround = false;
        if (limb.child != null)
        {
            ResetGroundContact(limb.child);
        }
    }
}