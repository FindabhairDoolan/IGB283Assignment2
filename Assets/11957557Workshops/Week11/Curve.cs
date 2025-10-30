using UnityEngine;

public class Curve : MonoBehaviour
{

    [Header("Curve")]
    [SerializeField] private int resolution = 31;
    [SerializeField] private float domainMin = -7f;
    [SerializeField] private float domainMax = 7f;

    [Space]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = Color.white;
    private LineRenderer lineRenderer;

    [Header("Fill Domain")]
    [SerializeField] private int rectangleCount = 5;
    [SerializeField] private int fillResolution = 7;
    [SerializeField] private float minXBound = -2f;
    [SerializeField] private float maxXBound = 3f;

    [Header("Fill Appearance")]
    [SerializeField] private Material fillMaterial;
    [SerializeField] private Color fillColour = new Color(0.8f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color rectangleColour = new Color(0.2f, 0.3f, 0.6f, 0.5f);

    [Header("Object References")]
    [SerializeField] private GameObject areaFill;
    [SerializeField] private GameObject rectangles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DrawCurve();
        FillArea();

        // Calculate and output the area under the curve
        float areaUnderCurve = DrawRectangles();
        Debug.Log(string.Format("Area under curve = {0} square units", areaUnderCurve));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Define y as a function of x
    private float FindY1(float x)
    {
        return -Mathf.Pow(x / 3f, 2f) + 4f;
    }

    // Draw the curve in the world
    private void DrawCurve(){
        // Set up the line renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = resolution;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // Determine the spacing between each point on the curve
        float increment = Mathf.Abs(domainMax - domainMin) / (resolution - 1);
        Vector3 position = Vector3.zero;

        // Set the line renderer's positions
        for (int i = 0; i < resolution; i++)
        {
            position.x = domainMin + i * increment;
            position.y = FindY1(position.x);
            lineRenderer.SetPosition(i, position);
        }

    }

    // Fill the area under the curve between the bounds
    private void FillArea(){
        // Add a mesh filter and mesh renderer to the area fill object
        Mesh mesh = areaFill.AddComponent<MeshFilter>().mesh;
        areaFill.AddComponent<MeshRenderer>().material = fillMaterial;
        // Clear all vertex and index data from the mesh
        mesh.Clear();

        // Find the lower y value out of the two boundary positions
        float y1 = FindY1(minXBound);
        float y2 = FindY1(maxXBound);
        // Use the ternary conditional operator to set the lower y
        float lowerY = y1 < y2 ? y1 : y2;

        // Log an error if the lower value is negative
        if (lowerY < 0)
        {
            Debug.LogError("This implementation does not support curves below 0.");
            return;
        }

        // Find the distance between each fill rectangle
        float increment = Mathf.Abs(maxXBound - minXBound) / (fillResolution - 1);

        // Initialise the vertices array
        int vertexCount = fillResolution * 2 + 6;
        Vector3[] vertices = new Vector3[vertexCount];
        // Set the filling rectangle between 0 and lowerY, and minXBound and maxXBound
        vertices[0] = new Vector3(minXBound, 0f, 0f);
        vertices[1] = new Vector3(minXBound, lowerY, 0f);
        vertices[2] = new Vector3(maxXBound, 0f, 0f);
        vertices[3] = new Vector3(maxXBound, lowerY, 0f);
        // Initialise the triangles array
        int[] triangles = new int[fillResolution * 6 + 12];
        // Set the filling rectangle vertices
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 2;
        triangles[4] = 1;
        triangles[5] = 3;
        // Set the first and last vertices above the filling rectangle
        vertices[4] = new Vector3(minXBound, FindY1(minXBound), 0f);
        float penultimateX = maxXBound - increment;
        vertices[^3] = new Vector3(penultimateX, lowerY, 0f);
        vertices[^2] = new Vector3(penultimateX, FindY1(penultimateX), 0f);
        vertices[^1] = new Vector3(maxXBound, FindY1(maxXBound), 0f);
        // Set the first and last triangles above the filling rectangle; one will be degenerate
        triangles[6] = 1;
        triangles[7] = 4;
        triangles[8] = 6;
        triangles[9] = 1;
        triangles[10] = 6;
        triangles[11] = 5;
        triangles[^6] = vertexCount - 3;
        triangles[^5] = vertexCount - 2;
        triangles[^4] = 3;
        triangles[^3] = 3;
        triangles[^2] = vertexCount - 2;
        triangles[^1] = vertexCount - 1;
        // Initialise the colours array
        Color[] colours = new Color[vertexCount]; vertices[0] = new
        Vector3(minXBound, 0f, 0f);
        colours[0] = fillColour;
        colours[1] = fillColour;
        colours[2] = fillColour;
        colours[3] = fillColour;
        colours[4] = fillColour;
        colours[^3] = fillColour;
        colours[^2] = fillColour;
        colours[^1] = fillColour;
        Vector3 v1 = Vector3.zero;
        Vector3 v2 = Vector3.zero;

        // Loop over each remaining pair of vertices
        for (int i = 0; i < fillResolution - 1; i++)
        {
            // Find the next vertices of the rectangle
            v1.x = minXBound + i * increment;
            v1.y = lowerY;
            v2.x = v1.x;
            v2.y = FindY1(v2.x);
            // Set the vertices
            int vertIndex = i * 2 + 5;
            vertices[vertIndex + 0] = v1;
            vertices[vertIndex + 1] = v2;
            // Set the rectangle's triangles
            int triIndex = i * 6 + 12;
            triangles[triIndex + 0] = vertIndex;
            triangles[triIndex + 1] = vertIndex + 1;
            triangles[triIndex + 2] = vertIndex + 2;
            triangles[triIndex + 3] = vertIndex + 2;
            triangles[triIndex + 4] = vertIndex + 1;
            triangles[triIndex + 5] = vertIndex + 3;
            // Set the colours
            colours[vertIndex + 0] = fillColour;
            colours[vertIndex + 1] = fillColour;
        }

        // Set the vertices, triangles, and colours
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colours;
        mesh.RecalculateBounds();

    }

    // Draw the rectangles and approximate the area
    private float DrawRectangles(){
        // Add a mesh filter and mesh renderer to the area fill object
        Mesh mesh = rectangles.AddComponent<MeshFilter>().mesh;
        rectangles.AddComponent<MeshRenderer>().material = fillMaterial;
        // Clear all vertex and index data from the mesh
        mesh.Clear();

        // Initialise the area
        float areaUnderCurve = 0f;
        // Find the width of each rectangle
        float increment = Mathf.Abs(maxXBound - minXBound) / rectangleCount;

        // Initialise rectangle vertices
        Vector3[] rectVerts = new Vector3[4];

        // Initialise the mesh arrays
        Vector3[] vertices = new Vector3[rectangleCount * 4];
        int[] triangles = new int[rectangleCount * 6];
        Color[] colours = new Color[vertices.Length];

        // Iterate over every rectangle, setting its vertices, triangles, and colours
        for (int i = 0; i < rectangleCount; i++)
        {
            // Find the x bounds of the rectangle
            float lowerX = minXBound + i * increment;
            float upperX = lowerX + increment;
            // Calculate the lower y value for the top of the rectangle
            float y1 = FindY1(lowerX);
            float y2 = FindY1(upperX);
            float lowerY = y1 < y2 ? y1 : y2;
            // Calculate the rectangle's vertices
            rectVerts[0] = new Vector3(lowerX, 0f, -1f);
            rectVerts[1] = new Vector3(lowerX, lowerY, -1f);
            rectVerts[2] = new Vector3(upperX, 0f, -1f);
            rectVerts[3] = new Vector3(upperX, lowerY, -1f);

            // Set the vertices and colours
            for (int j = 0; j < 4; j++)
            {
                vertices[i * 4 + j] = rectVerts[j];
                colours[i * 4 + j] = rectangleColour;
            }

            // Set the triangles
            triangles[i * 6 + 0] = i * 4 + 0;
            triangles[i * 6 + 1] = i * 4 + 1;
            triangles[i * 6 + 2] = i * 4 + 2;
            triangles[i * 6 + 3] = i * 4 + 2;
            triangles[i * 6 + 4] = i * 4 + 1;
            triangles[i * 6 + 5] = i * 4 + 3;

            // Add the area of the rectangle to the total area (using length * width)
            areaUnderCurve += increment * lowerY;

            // Set the vertices, triangles, and colours
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colours;
            mesh.RecalculateBounds();

        }

        return areaUnderCurve;

    }

}
