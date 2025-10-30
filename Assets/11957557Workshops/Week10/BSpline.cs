using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class BSpline : MonoBehaviour
{

    // Spline variables
    [Header("Spline Settings")]
    [SerializeField] private int numControlPoints = 9;
    [SerializeField] private int numLinePositionsMultiplier = 10;
    [SerializeField] private int k = 4;
    [SerializeField] private ControlPoint controlPointPrefab;

    private int numLinePositions;
    private int m;
    private int n;
    private List<float> knots = new List<float>();
    private float cameraDistance;
    private bool drawCurve = false;
    private List<ControlPoint> controlPoints = new
    List<ControlPoint>();

    // Line Renderer variables
    [Header("Line Renderer Variables")]
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Material lineMaterial;
    private LineRenderer curve;

    // Input variables
    [Header("Input")]
    [SerializeField] private InputAction moveControlPoint = new
    InputAction();
    [SerializeField] private InputAction newControlPoint = new
    InputAction();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up the B-Spline variables
        SetupBSpline();

        // Create or reset the control points
        if (controlPoints == null)
        {
            controlPoints = new List<ControlPoint>();
        }
        else
        {
            controlPoints.Clear();
        }

        // Calculate the control point offsets
        float intervalSize = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0f, cameraDistance)).x / numControlPoints;
        float startPosition = (Camera.main.ScreenToWorldPoint(new Vector3(0f, 0f, cameraDistance)).x + intervalSize) / 2;

        Vector3 pointPosition = new Vector3(0f, 0f, 1f);
        for (int i = 0; i < numControlPoints; i++)
        {
            // Calculate the x and y of the new point
            pointPosition.x = startPosition + intervalSize * i;
            pointPosition.y = Mathf.Sin(pointPosition.x);

            ControlPoint controlPoint = Instantiate(controlPointPrefab, pointPosition, Quaternion.identity, transform);
            controlPoints.Add(controlPoint);

        }

        // Set up the line renderer
        curve = gameObject.AddComponent<LineRenderer>();
        curve.widthMultiplier = lineWidth;
        curve.material = lineMaterial;

        DrawCurve();

    }

    // Update is called once per frame
    void Update()
    {
        // Visualise a linear path between control points
        DrawDebugLines();

        // Only draw the curve when necessary
        if (drawCurve)
            DrawCurve();
    }


    // Set up the B-Spline variables
    private void SetupBSpline(){
        m = numControlPoints - 1;
        n = m + k;
        numLinePositions = numControlPoints * numLinePositionsMultiplier;
        cameraDistance = 1 - Camera.main.transform.position.z; //Control point at z=1

        // Create or reset the knots list
        if (knots == null)
        {
            knots = new List<float>();
        }
        else
        {
            knots.Clear();
        }
        // Populate the knots list
        for (int i = 0; i < n + 1; i++)
        {
            knots.Add(i);
        }

    }

    private Vector3 GetMousePosition()
    {
        // Find the mouse position in world space
        Vector3 mouseInput = Mouse.current.position.ReadValue();
        mouseInput.z = cameraDistance;
        return Camera.main.ScreenToWorldPoint(mouseInput);
    }

    // Start moving a control point
    private void MoveControlPointStart(InputAction.CallbackContext context){
        // Find the mouse position in world space
        Vector3 mouseInWorld = GetMousePosition();

        // Detect any colliders at the mouse position
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseInWorld);
        if (hitCollider != null && hitCollider.TryGetComponent(out ControlPoint controlPoint))
        {
            controlPoint.IsMoving = true;
            drawCurve = true;
        }
    }

    // Stop moving all control points
    private void MoveControlPointStop(InputAction.CallbackContext context)
    {
        foreach (ControlPoint controlPoint in controlPoints)
        {
            controlPoint.IsMoving = false;
        }
        drawCurve = false;
    }

    // Remove a control point if hovering over one; otherwise, create a new one
    private void AddOrRemoveControlPoint(InputAction.CallbackContext context)
    {
        // Look for a collider under the mouse
        Vector3 mouseInWorld = GetMousePosition();
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseInWorld);

        // Check if a collider was found, and then if it's a control point
        if (hitCollider != null && hitCollider.TryGetComponent(out ControlPoint controlPoint))
        {
            // If it was a control point, remove it
            numControlPoints -= 1;
            controlPoints.Remove(controlPoint);
            Destroy(controlPoint.gameObject);
        }
        else
        {
            // If there was no control point, create one
            ControlPoint newControlPoint = Instantiate(controlPointPrefab, mouseInWorld, Quaternion.identity, transform);
            numControlPoints += 1;
            controlPoints.Add(newControlPoint);
        }

        SetupBSpline();

        DrawCurve();

    }

    private void OnEnable()
    {
        // Enable the input actions
        moveControlPoint.Enable();
        newControlPoint.Enable();

        // Subscribe to the required methods when the inputs are enacted
        moveControlPoint.performed += MoveControlPointStart;
        moveControlPoint.canceled += MoveControlPointStop;
        newControlPoint.performed += AddOrRemoveControlPoint;
    }

    private void OnDisable()
    {
        // Disable the input actions
        moveControlPoint.Disable();
        newControlPoint.Disable();

        // Unsubscribe from the required methods when the inputs are enacted
        moveControlPoint.performed -= MoveControlPointStart;
        moveControlPoint.canceled -= MoveControlPointStop;
        newControlPoint.performed -= AddOrRemoveControlPoint;
    }

    // Draw debug lines between all control points
    private void DrawDebugLines()
    {
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Debug.DrawLine(controlPoints[i].transform.position, controlPoints[i + 1].transform.position, Color.green);
        }
    }

    private float BSplineBasis(int i, int k, float t)
    {
        if (k == 1)
        {
            if (knots[i] <= t && knots[i + 1] > t)
                return 1;
            else
                return 0;
        }
        else
        {
            return BSplineBasis(i, k - 1, t) * (t - knots[i]) / (knots[i + k - 1] - knots[i]) + BSplineBasis(i + 1, k - 1, t) * (knots[i + k] - t) / (knots[i + k] - knots[i + 1]);
        }
    }

    private Vector3 FindBSpline(float t)
    {
    // Find the first knot larger than or equal to t
    int i = k - 1;
    while (knots[i + 1] < t)
    {
        i++;
    }

    if (i > m)
    {
        i = m;
    }

    float x = BSplineBasis(i - 3, k, t) * controlPoints[i - 3].transform.position.x
    + BSplineBasis(i - 2, k, t) * controlPoints[i - 2].transform.position.x
    + BSplineBasis(i - 1, k, t) * controlPoints[i - 1].transform.position.x
    + BSplineBasis(i, k, t) * controlPoints[i].transform.position.x;

    float y = BSplineBasis(i - 3, k, t) * controlPoints[i - 3].transform.position.y
    + BSplineBasis(i - 2, k, t) * controlPoints[i - 2].transform.position.y
    + BSplineBasis(i - 1, k, t) * controlPoints[i - 1].transform.position.y
    + BSplineBasis(i, k, t) * controlPoints[i].transform.position.y;

    return new Vector3(x, y, 1f);
    }

    private void DrawCurve()
    {
        // Prevent curve updates when there are too few control points
        if (numControlPoints < k)
            return;

        curve.positionCount = numLinePositions;
        
        for (int i = 0; i < numLinePositions; i++)
        {
            curve.SetPosition(i, FindBSpline(knots[k - 1] + (knots[m + 1] - knots[k - 1]) * i / (numLinePositions - 1)));
        }
    }

}
