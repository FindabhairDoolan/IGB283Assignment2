using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;


public class Limb : MonoBehaviour
{
    // Reference the limb's child
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
    private enum State { Walking, Jumping, Leaping, Falling, Rising}
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private IEnumerator Start()
    {
        initialMove(initialJointLocation);

        yield return null; //Ensure model is initialised properly before applying transformations

        ApplyInitialRotation();

        //Store the starting angle so the nod oscillates around it
        nodBaseAngle = lastAngle;
    }

    // Update is called once per frame
    void Update()
    {
        if (nodding)
        {
            //Oscillate head back and forth
            float oscillation = Mathf.Sin(Time.time * nodSpeed) * nodAmplitude;

            //Apply the nodding rotation
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

        //Handle jumping/leaping
        if (currentState == State.Jumping || currentState == State.Leaping)
        {
            PerformJumpOrLeap();
        }

    }

    //draw limb
    private void DrawLimb()
    {
        mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>().material = material;

        mesh.vertices = limbVertices;

        mesh.colors = new Color[]
        {
            colour,
            colour,
            colour,
            colour
        };

        mesh.triangles = new int[]
        {
            0, 1, 2,  //first triangle
            2, 3, 0   //second triangle
        };
    }

    // Apply any input transformation to the limb
    private void ApplyTransformation(Matrix3x3 transformation)
    {
        // Apply the transformation to each vertex
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = (Vector3)transformation.MultiplyPoint(vertices[i]);
        }

        // Update the mesh
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        //Update the joint location
        jointLocation = (Vector2)transformation.MultiplyPoint(jointLocation);

        // Apply the offset to the child, if not null
        if (child != null)
        {
            child.ApplyTransformation(transformation);
        }

        //Update the box collider to follow the actual movement of the limb
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Bounds bounds = mesh.bounds;
        col.offset = bounds.center;
        col.size = bounds.size;

    }

    //Translate the limb for initial joint location
    private void initialMove(Vector2 offset)
    {
        // Calculate the translation matrix
        Matrix3x3 t = IGB283Transform.Translate(offset);
        ApplyTransformation(t);
    }

    //Translate the limb
    private void Move(IGB283Vector offset){
        Matrix3x3 t = IGB283Transform.Translate(offset);
        ApplyTransformation(t);
    }

    // Rotate the limb around a point
    private void RotateAroundPoint(Vector2 point, float angle)
    {
        // Calculate the transformation matrices
        Matrix3x3 tInv = IGB283Transform.Translate(-point); //Move to origin
        Matrix3x3 rLastAngle = IGB283Transform.Rotate(-lastAngle); //Undo last rotation
        Matrix3x3 rAngle = IGB283Transform.Rotate(angle); //Apply new rotation
        Matrix3x3 t = IGB283Transform.Translate(point); //Move back

        // Apply the rotation around point
        ApplyTransformation(t * rAngle * rLastAngle * tInv);

        // Update the last angle
        lastAngle = angle;
    }

    private void Walk()
    {
        //Compute translation offset based on speed and direction
        IGB283Vector offset = new IGB283Vector(walkSpeed * walkDirection * Time.deltaTime, 0f, 1f);

        //Translate
        Move(offset);

        float currentX = jointLocation.x;

        //Check for boundary collision
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
        // Flip horizontally around the current joint
        Matrix3x3 toOrigin = IGB283Transform.Translate(-jointLocation);
        Matrix3x3 flip = IGB283Transform.FlipX();
        Matrix3x3 back = IGB283Transform.Translate(jointLocation);

        ApplyTransformation(back * flip * toOrigin);

        flipped = !flipped;

        //Apply to the child (so they also know they're flipped)
        if (child != null)
        {
            child.OnParentFlipped(flipped);
        }
        
    }

    //Flip all children
    public void OnParentFlipped(bool isFlipped)
    {
        flipped = isFlipped;
        lastAngle = -lastAngle;
        nodBaseAngle = -nodBaseAngle;

        if (child != null)
        {
            child.OnParentFlipped(isFlipped);
        }
    }

    //Detect when limb touches the ground
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
                TryStartJump();// immediately jump again if the key is still held
            }

        }
    }

    //Detect when limb leaves the ground
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            onGround = false;
        }
    }

    //Same function for both since leap is just jumping but with x axis movement
    private void PerformJumpOrLeap()
    {
        // Apply vertical velocity
        float deltaY = jumpVelocity * Time.deltaTime;
        jumpVelocity += gravity * Time.deltaTime; //simulate gravity because i can

        //Create translation vector
        IGB283Vector moveVec = new IGB283Vector(0, deltaY, 1);

        //Add horizontal motion for leaping
        if (currentState == State.Leaping)
        {
            float horizontalMove = walkSpeed * walkDirection * Time.deltaTime;
            moveVec.x = horizontalMove;

            //Boundary check
            float nextX = jointLocation.x + moveVec.x;

            if (nextX >= rightBoundary && walkDirection == 1)
            {
                walkDirection = -1;
                FlipModel();
                moveVec.x = -Mathf.Abs(horizontalMove); //flip horizontal motion mid-air
            }
            else if (nextX <= leftBoundary && walkDirection == -1)
            {
                walkDirection = 1;
                FlipModel();
                moveVec.x = Mathf.Abs(horizontalMove);
            }

        }

        //Apply translation
        ApplyTransformation(IGB283Transform.Translate(moveVec));
    }

    //Move left
    private void FlipLeft(InputAction.CallbackContext context)
    {
        if (currentState != State.Walking || flipped)
        {
            return;
        }

        walkDirection = -1;
        FlipModel(); //flip to face left
    }

    //Move right
    private void FlipRight(InputAction.CallbackContext context)
    {
        if (currentState != State.Walking || !flipped)
        {
            return;
        }

        walkDirection = 1;
        FlipModel(); //flip to face right
    }

    //Ask politely to start jumping
    private void StartJumpHold(InputAction.CallbackContext context)
    {
        jumpHeld = true;
        TryStartJump();
    }

    //Ask politely to stop jumping
    private void StopJumpHold(InputAction.CallbackContext context)
    {
        jumpHeld = false; //stop queueing new jumps
    }

    //Tries to jump again when QUT Jr lands on the ground and the w key is held down still
    private void TryStartJump()
    {
        //Should be on the ground and walking to jump
        if (!onGround || currentState != State.Walking)
        {
            return;
        }

        currentState = State.Jumping;
        onGround = false;
        jumpVelocity = 5f;
    }

    //Leap forward
    private void LeapModel(InputAction.CallbackContext context)
    {
        //Should be on the ground and walking to leap
        if (!onGround || currentState != State.Walking)
        {
            return;
        } 

        currentState = State.Leaping;
        onGround = false;
        jumpVelocity = 5f; //same jump power as jump
    }

    //Collapse and get back up
    private void Fall(InputAction.CallbackContext context)
    {
        //Only can start falling if on ground
        if(onGround && currentState == State.Walking)
        {
            StartCoroutine(CollapseModel());
        }
    }

    //Applies rotation to limb to put it in it's default pose
    private void ApplyInitialRotation(){
        RotateAroundPoint(jointLocation, defaultAngle);
    }

    //Collapse the model
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
        else {
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
        else {
            nodding = true;
        }
    }

    private void OnEnable()
    {
        EnableInputs();

        if (useArrowInputs)
        {
            //Arrow scheme
            upArrow.started += StartJumpHold;
            upArrow.canceled += StopJumpHold;
            leftArrow.performed += FlipLeft;
            downArrow.performed += LeapModel;
            rightArrow.performed += FlipRight;
            rightCtrl.performed += Fall;
        }
        else
        {
            //WASD scheme
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
            //Disable inputs
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

            //Arrows (fix duplicates)
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
                //Only arrows ON
                upArrow.Enable();
                leftArrow.Enable();
                downArrow.Enable();
                rightArrow.Enable();   
                rightCtrl.Enable();

                //WASD OFF
                wKey.Disable();
                aKey.Disable();
                sKey.Disable();
                dKey.Disable();
                zKey.Disable();
            }
            else
            {
                //Only WASD ON
                wKey.Enable();
                aKey.Enable();
                sKey.Enable();
                dKey.Enable();
                zKey.Enable();

                //Arrows OFF
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