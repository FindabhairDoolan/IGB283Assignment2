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

    // Store the previous angle to undo
    private float lastAngle = 0;

    // The corner positions of each limb
    [SerializeField] private Vector3[] limbVertices = new Vector3[4];
    private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private Color colour = Color.white;

    //Nodding parameters (only used by head)
    [SerializeField] private bool nodding = false;
    private float nodAmplitude = 0.26f;//about 20 degrees in radians
    private float nodSpeed = 4f; //how fast QUT Jr nods
    private float nodBaseAngle; //base angle to oscillate around

    //Walking parameters (for base)
    [SerializeField] private bool walking = false;
    private float walkSpeed = 2f;
    private float leftBoundary = -10f;
    private float rightBoundary = 10f;
    private int walkDirection = 1; //1 = right, -1 = left
    private bool flipped = false;//track direction

    //Input keys
    [SerializeField] private InputAction wKey;
    [SerializeField] private InputAction aKey;
    [SerializeField] private InputAction sKey;
    [SerializeField] private InputAction dKey;
    [SerializeField] private InputAction zKey;

    //Movement state of the model
    private enum State { Walking, Jumping, Leaping, Falling, Collapsed, Rising}
    private State currentState = State.Walking; //Initially walking

    //More movement defining variables
    private bool onGround = true;
    private float jumpVelocity = 0f;
    private float gravity = -9.8f;
    private bool jumpHeld = false;

    private float collapseSpeed = 1.5f;

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

        //!!!!!!!!!!!!Alex, these walls is very temporary, just for the purpose of me seeing if it flips at the boundary!!!!!!!!!!!!!!!!
        //!!!!!!!!!!! im not too sure if we need a wall but since ur making the ground I thought it might be easier for you to implement if we do (its a vertical ground pfft idk)!!!!!!!
        //!!!!Also while ur here,!!!!
        //!!It is genuinely rare for it to do but it sometimes does a bad flip and i implemented a failsafe to catch it if it happens (feel free to remove the logwarning for it in the code) so you should never see it walking backwards (let me know if u do)!!!!
        //!!!!! And also you might notice but I just commented out all the z input stuff while im working on it, so feel free to ignore it for now
        CreateWall(-10f); // left
        CreateWall(10f);  // right
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    }

    void CreateWall(float xPos) //Temporary!!!!! Remove before final version!!!!!! It doesnt follow the rule of no transform functions, and it uses vector 3 etc. Its purely for temp visual debugging!!!!
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = xPos < 0 ? "LeftWall" : "RightWall";
        wall.transform.position = new Vector3(xPos, 0, 0);
        wall.transform.localScale = new Vector3(0.2f, 10f, 1f);
        wall.GetComponent<Renderer>().material.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        if (nodding)
        {
            //Oscillate head back and forth
            float oscillation = Mathf.Sin(Time.time * nodSpeed) * nodAmplitude;

            //!!!!Helpful debug for me :)!!!!!!
            //Debug.Log($"[Head] flipped={flipped}  lastAngle={lastAngle:F3}  nodBaseAngle={nodBaseAngle:F3}  jointLocation={jointLocation}");

            //Apply the nodding rotation
            RotateAroundPoint(jointLocation, nodBaseAngle + oscillation);
        }

        if (walking && currentState == State.Walking)
        {
            if ((walkDirection == 1 && flipped) || (walkDirection == -1 && !flipped))
            {
                Debug.LogWarning("Orientation mismatch detected — auto-correcting flip!");
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
            0, 1, 2,  // first triangle
            2, 3, 0   // second triangle
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

        // Check for boundary collision
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

    //Helper for children
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
        if (currentState != State.Walking || flipped) return;
        walkDirection = -1;
        FlipModel(); //flip to face left
    }

    //Move right
    private void FlipRight(InputAction.CallbackContext context)
    {
        if (currentState != State.Walking || !flipped) return;
        walkDirection = 1;
        FlipModel(); //flip to face right
    }

    private void StartJumpHold(InputAction.CallbackContext context)
    {
        jumpHeld = true;
        TryStartJump(); //begin first jump immediately
    }

    private void StopJumpHold(InputAction.CallbackContext context)
    {
        jumpHeld = false; //stop queueing new jumps
    }

    //Tries to jump again when QUT Jr lands on the ground and the w key is held down still
    private void TryStartJump()
    {
        if (!onGround || currentState != State.Walking) return;

        currentState = State.Jumping;
        onGround = false;
        jumpVelocity = 5f;
    }

    //Leap forward
    private void LeapModel(InputAction.CallbackContext context)
    {
        Debug.Log($"[LeapModel] onGround={onGround} state={currentState}");
        if (!onGround || currentState != State.Walking) return;

        currentState = State.Leaping;
        onGround = false;
        jumpVelocity = 5f; //same jump power as jump
    }

    //Collapse and get back up
    private void Fall(InputAction.CallbackContext context)
    {
        StartCoroutine(CollapseModelSimple());
    }


    //Applies rotation to limb to put it in it's default pose
    private void ApplyInitialRotation(){
        RotateAroundPoint(jointLocation, defaultAngle);
    }

    private void OnEnable()
    {
        //Enable inputs
        wKey.Enable();
        aKey.Enable();
        sKey.Enable();
        dKey.Enable();
        zKey.Enable();

        //Trigger events
        wKey.started  += StartJumpHold; //Account for press and hold
        wKey.canceled += StopJumpHold;
        aKey.performed += FlipLeft;
        sKey.performed += LeapModel;
        dKey.performed += FlipRight;
        zKey.performed += Fall;           
    }

    private void OnDisable()
    {
        //Disable inputs
        wKey.Disable();
        aKey.Disable();
        sKey.Disable();
        dKey.Disable();
        zKey.Disable();

        //Stop triggering events
        wKey.started  -= StartJumpHold; //Account for press and hold
        wKey.canceled -= StopJumpHold;
        aKey.performed -= FlipLeft;
        sKey.performed -= LeapModel;
        dKey.performed -= FlipRight;
        zKey.performed -= Fall;
    }
    
    //Disable inputs (wont stop events etc)
    private void DisableInputs()
    {
        wKey.Disable();
        aKey.Disable();
        sKey.Disable();
        dKey.Disable();
        zKey.Disable();
    }

    //Enable inputs
    private void EnableInputs()
    {
        wKey.Enable();
        aKey.Enable();
        sKey.Enable();
        dKey.Enable();
        zKey.Enable();
    }

    // direction helper: backwards rotation
    private float BackwardDirection() => flipped ? -1f : 1f;

    public IEnumerator CollapseModelSimple()
    {
        // --- Step 1: Disable control temporarily ---
        DisableInputs();

        bool wasWalking = walking;
        walking = false;

        // Remember if nodding was active anywhere and disable it globally
        bool wasNodding = IsAnyLimbNodding();
        DisableNoddingForAll();

        // Reset ground contact flags before collapsing
        ResetGroundContact(this);

        // --- Step 2: Sequential collapse (skip base) ---
        Limb current = this.child;
        while (current != null)
        {
            yield return current.RotateUntilGrounded();
            current = current.child;
        }

        yield return new WaitForSeconds(1f);

        // --- Step 3: Restore normal behavior ---
        yield return StartCoroutine(RiseModelSimple());
    }

    public IEnumerator RiseModelSimple()
    {
        // --- Step 1: Disable control during rise ---
        DisableInputs();
        walking = false;
        DisableNoddingForAll();

        // --- Step 2: Collect all limbs in order ---
        Limb[] limbChain = GetAllLimbs();

        // --- Step 3: Rise from last child back to base ---
        for (int i = limbChain.Length - 1; i >= 0; i--)
        {
            yield return limbChain[i].RotateToDefault();
        }

        if (IsBaseLimb())
        {
            onGround = true;
            jumpVelocity = 0f;
        }

        // --- Step 4: Restore normal behavior ---
        currentState = State.Walking;
        walking = true;
        EnableNoddingForAll();
        EnableInputs();
    }

    private Limb[] GetAllLimbs()
    {
        // count chain length first
        int count = 0;
        Limb temp = this;
        while (temp != null)
        {
            count++;
            temp = temp.child;
        }

        // collect limbs
        Limb[] limbs = new Limb[count];
        temp = this;
        for (int i = 0; i < count; i++)
        {
            limbs[i] = temp;
            temp = temp.child;
        }

        return limbs;
    }

    private IEnumerator RotateToDefault()
    {
        float angle = lastAngle;
        float target = flipped ? -defaultAngle : defaultAngle; // account for flipping
        float speed = collapseSpeed;

        while (Mathf.Abs(angle - target) > 0.01f)
        {
            float step = Mathf.Sign(target - angle) * speed * Time.deltaTime;
            angle += step;
            RotateAroundPoint(jointLocation, angle);
            yield return null;
        }

        // snap exactly to the target
        RotateAroundPoint(jointLocation, target);
        lastAngle = target;
    }

    // helper to clear onGround before collapse
    private void ResetGroundContact(Limb limb)
    {
        limb.onGround = false;
        if (limb.child != null)
            ResetGroundContact(limb.child);
    }

    // rotate this single limb backward until its onGround becomes true (collision event)
    private IEnumerator RotateUntilGrounded()
    {
        float angle = lastAngle;

        // simple safety timeout (avoid infinite loop)
        float timeout = 3f;
        float timer = 0f;

        while (!onGround && timer < timeout)
        {
            angle += BackwardDirection() * collapseSpeed * Time.deltaTime;
            RotateAroundPoint(jointLocation, angle);
            timer += Time.deltaTime;
            yield return null;
        }

        // hold final angle
        lastAngle = angle;
    }

    // Check recursively if any limb was nodding before collapse
    private bool IsAnyLimbNodding()
    {
        if (nodding) return true;
        if (child != null) return child.IsAnyLimbNodding();
        return false;
    }

    // Disable nodding for this limb and all children
    private void DisableNoddingForAll()
    {
        nodding = false;
        if (child != null) child.DisableNoddingForAll();
    }

    // Re-enable nodding for this limb and all children that originally nod
    // (in practice, just the head)
    private void EnableNoddingForAll()
    {
        if (child != null) child.EnableNoddingForAll();

        // Simple heuristic: head's GameObject is named "Head" (or similar)
        if (gameObject.name.ToLower().Contains("head"))
        {
            nodding = true;
        }
    }


    private bool IsBaseLimb() => transform.parent == null;

}

