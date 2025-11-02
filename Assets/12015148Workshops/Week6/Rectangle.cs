using UnityEngine;

//adapted triangles from earlier workshop
public class Rectangle : MonoBehaviour
{
    [SerializeField] private Material material;
    private Mesh mesh;
    [SerializeField] private float angle = 10f;

    // Offset for the rectangle's centre
    private Vector2 offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Add a mesh filter and mesh renderer, getting and setting useful variables
        mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>().material = material;
        // Clear all vertex and index data from the mesh
        mesh.Clear();
        // Create a rectangle between(0, 0, 0), (0, 1, 0), (1, 1, 0), and (1, 0, 0)
        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 0)
        };
        //Set the colours
        Color rectangle_colour = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        mesh.colors = new Color[]
        {
            rectangle_colour,
            rectangle_colour,
            rectangle_colour,
            rectangle_colour
        };
        // Specify which vertices the rectangle uses
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, };

        // Calculate the offset from the mesh size
        offset.x = mesh.bounds.size.x / 2;
        offset.y = mesh.bounds.size.y / 2;
    }

    // Update is called once per frame
    void Update()
    {
        RotateRectangle();
    }

    private Matrix3x3 Rotate(float angle)
    {
        // Calculate sin and cos of the angle once
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);

        // Create a new matrix for rotation
        Matrix3x3 r = new Matrix3x3(
        new Vector3(cos, -sin, 0),
        new Vector3(sin, cos, 0),
        new Vector3(0, 0, 1));
        return r;
    }

    private Matrix3x3 Translate(Vector2 offset)
    {
        // Create a new matrix for rotation
        Matrix3x3 t = new Matrix3x3(
        new Vector3(1, 0, offset.x),
        new Vector3(0, 1, offset.y),
        new Vector3(0, 0, 1));
        return t;
    }

    private void RotateRectangle()
    {
        // Get the current mesh vertices
        Vector3[] vertices = mesh.vertices;
        // Get a rotation matrix for the given angular speed
        Matrix3x3 r = Rotate(angle * Time.deltaTime);
        // Calculate transformation matrix
        Matrix3x3 t = Translate(offset);
        Matrix3x3 tInv = Translate(-offset);
        Matrix3x3 m = t * r * tInv;

        // Rotate every vertex in the mesh
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = m.MultiplyPoint(vertices[i]);
        }

        // Update the mesh vertices
        mesh.vertices = vertices;
        // Recalculate the mesh bounds
        mesh.RecalculateBounds();


    }
}
