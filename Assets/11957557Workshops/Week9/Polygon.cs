using UnityEngine;

public class Polygon : MonoBehaviour
{
    [Header("Polygon")]
    [SerializeField] private Transform vertexPrefab;
    [Min(0)]
    [SerializeField] private int numVertices = 10;
    public int NumVertices { get { return numVertices; } }
    private Transform[] vertices;

    [SerializeField] private float radius = 2f;

    [Header("Line Renderer")]
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Gradient lineColour = new Gradient();
    private LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Determine the angle between each vertex
        float angle = 2f * Mathf.PI / numVertices;

        // Spawn the vertices around the circumference of a circle
        vertices = new Transform[numVertices];
        for (int i = 0; i < numVertices; i++)
        {
            Vector3 vertexPosition = new Vector3(
            radius * Mathf.Cos(angle * i),
            radius * Mathf.Sin(angle * i),
            0);

            // Create a new vertex at the calculated position
            Transform vertexInstance = Instantiate(vertexPrefab,
            vertexPosition, Quaternion.identity, this.transform);
            vertices[i] = vertexInstance;
        }

        // Set up the line renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.material = lineMaterial;
        lineRenderer.colorGradient = lineColour;
        lineRenderer.positionCount = numVertices + 1;
    }

    // Update is called once per frame
    void Update()
    {
        DrawEdges();
    }

    // Calculate the left-most vertex
    public float MinX
    {
        get
        {
            float minX = float.MaxValue;
            foreach (Transform vertex in vertices)
            {
                if (vertex.position.x < minX)
                    minX = vertex.position.x;
            }
            return minX;
        }
    }

    // Calculate the right-most vertex
    public float MaxX
    {
        get
        {
            float maxX = float.MinValue;
            foreach (Transform vertex in vertices)
            {
                if (vertex.position.x > maxX)
                    maxX = vertex.position.x;
            }
            return maxX;
        }
    }

    // Draw the polygon edges
    private void DrawEdges()
    {
        for (int i = 0; i <= numVertices; i++)
        {
            // Join the last line position to the first vertex
            int j = i % numVertices;
            lineRenderer.SetPosition(i, vertices[j].position);
        }
    }

    // Create an array of Edges from the vertex definitions
    public Edge[] EdgesFromVertices()
    {
        Edge[] edges = new Edge[vertices.Length];
        for (int i = 0; i < numVertices; i++)
        {
            // Loop back to the first vertex at the end
            int j = (i + 1) % numVertices;
            Edge edge = new Edge(vertices[i].position,
            vertices[j].position);
            edges[i] = edge;
        }
        return edges;
    }


}
