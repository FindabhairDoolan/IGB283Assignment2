using UnityEngine;

public class LineCubeIntersection : MonoBehaviour
{

    [Header("Cube")]
    [SerializeField] private MeshFilter cube;
    private Mesh cubeMesh;

    [Header("Line")]
    [SerializeField] private Vector3[] lineEnds;
    [SerializeField] private float lineWidth;
    [SerializeField] private Material lineMaterial;
    private LineRenderer line;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Setup the line and plane
        SetupCube();
        SetupLine();
        // Find the line-plane intersection
        FindIntersectionFaces();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Create the cube
    private void SetupCube()
    {
        cubeMesh = cube.mesh;

        cubeMesh.RecalculateBounds();
        cubeMesh.RecalculateNormals();
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
    private bool FindIntersection(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 la, Vector3 lb)
    {
        // Get the plane's Mesh data
        Vector3[] vertices = cubeMesh.vertices;
        int[] triangles = cubeMesh.triangles;

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
        if (onLine && onPlane && inTriangle)
        {
            // Create a new sphere to show the intersection
            GameObject intersection = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            intersection.name = "Intersection Point";
            intersection.transform.localScale = new Vector3(0.5f, 0.5f,
            0.5f);

            // Move the sphere to the intersection point
            intersection.transform.position = la + lab * t;

            return true;
        }
        else
        {
            return false;
        }

    }

    // Find the line-cube intersections
    private int FindIntersectionFaces()
    {
        // Line ends
        Vector3 la = lineEnds[0];
        Vector3 lb = lineEnds[1];
        // Get the cube's position and scale
        float scale = cube.transform.lossyScale.x;
        Vector3 offset = cube.transform.position;

        // Test the intersection with every triangle in the mesh
        int intersectionCount = 0;
        for (int i = 0; i < cubeMesh.triangles.Length; i += 3)
        {
            // Find the transformed cube vertices
            Vector3 p0 = cubeMesh.vertices[cubeMesh.triangles[i + 1]] * scale + offset;
            Vector3 p1 = cubeMesh.vertices[cubeMesh.triangles[i + 0]] * scale + offset;
            Vector3 p2 = cubeMesh.vertices[cubeMesh.triangles[i + 2]] * scale + offset;

            // Check if the line and current face intersect
            if (FindIntersection(p0, p1, p2, la, lb))
            {
                intersectionCount++;
            }
        }

        string pluralIsAre = intersectionCount == 1 ? "is" : "are";
        string pluralS = intersectionCount == 1 ? string.Empty : "s";
        Debug.Log(string.Format("There {0} {1} intersection{2}.", pluralIsAre, intersectionCount.ToString(), pluralS));

        return intersectionCount;

    }
}
