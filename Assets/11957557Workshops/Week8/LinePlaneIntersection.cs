using UnityEngine;

public class LinePlaneIntersection : MonoBehaviour
{

    [Header("Plane")]
    [SerializeField] private Vector3[] planeCorners;
    [SerializeField] private Material planeMaterial;
    [SerializeField]
    private Color planeColour = new Color(0.8f, 0.8f, 0.8f, 1);
    private Mesh planeMesh;

    [Header("Line")]
    [SerializeField] private Vector3[] lineEnds;
    [SerializeField] private float lineWidth;
    [SerializeField] private Material lineMaterial;
    private LineRenderer line;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Setup the line and plane
        SetupPlane();
        SetupLine();
        // Find the line-plane intersection
        FindIntersection();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Create the plane
    private void SetupPlane()
    {
        // Create a new empty GameObject for the plane
        GameObject plane = new GameObject("Plane");

        // Add a Mesh Filter and Mesh Renderer to the plane
        planeMesh = plane.AddComponent<MeshFilter>().mesh;
        plane.AddComponent<MeshRenderer>().material = planeMaterial;
        planeMesh.Clear();

        // Set the vertices and colours as the Inspector inputs
        planeMesh.vertices = planeCorners;
        planeMesh.colors = new Color[] { planeColour, planeColour, planeColour, planeColour };
        planeMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        // Recalculate the normals for correct lighting
        planeMesh.RecalculateNormals();
    }

    // Create the Line
    private void SetupLine()
    {
        // Create a new empty GameObject for the plane
        GameObject lineObj = new GameObject("Line");
        line = lineObj.AddComponent<LineRenderer>();

        // Set the line variables
        line.widthMultiplier = lineWidth;
        line.positionCount = 2;
        line.SetPositions(lineEnds);
        line.material = lineMaterial;
    }

    // Find the line-plane intersection
    private void FindIntersection()
    {
        // Get the plane's Mesh data
        Vector3[] vertices = planeMesh.vertices;
        int[] triangles = planeMesh.triangles;

        // Get a triangle and the line ends
        Vector3 p0 = vertices[triangles[1]];
        Vector3 p1 = vertices[triangles[0]];
        Vector3 p2 = vertices[triangles[2]];
        Vector3 la = lineEnds[0];
        Vector3 lb = lineEnds[1];

        // Calculate the equation vectors
        Vector3 lab = lb - la;
        Vector3 p01 = p1 - p0;
        Vector3 p02 = p2 - p0;

        // Find the inverse coefficient matrix
        Matrix3x3 m = new Matrix3x3();
        m.SetColumn(0, -lab);
        m.SetColumn(1, p01);
        m.SetColumn(2, p02);
        m = m.Inverse;

        // Apply the matrix to solve the system
        Vector3 constantVec = la - p0;
        Vector3 tuv = m.MultiplyVector(constantVec);
        // Separate t, u, and v
        float t = tuv.x;
        float u = tuv.y;
        float v = tuv.z;

        // Determine if the point is on the line and plane
        bool onLine = t <= 1 && t >= 0;
        bool onPlane = (u >= 0 && u <= 1) && (v >= 0 && v <= 1);
        bool inTriangle = (u + v) <= 1;

        // Set the location if on the line and plane, or log a message otherwise
        if (onLine && onPlane)
        {
            // Create a new sphere to show the intersection
            GameObject intersection = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            intersection.name = "Intersection Point";
            intersection.transform.localScale = new Vector3(0.5f, 0.5f,
            0.5f);

            // Move the sphere to the intersection point
            intersection.transform.position = la + lab * t;

            // Output whether or not the intersection occured in the selected triangle
            Debug.Log(string.Format("Inside triangle {0}, {1}, {2}: {3}", p0, p1, p2, inTriangle));
        }
        else
        {
            Debug.Log("The line and plane do not intersect.");
        }

    }
}
