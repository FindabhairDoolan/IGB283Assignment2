using UnityEngine;
using UnityEngine.UI;


public class Limb : MonoBehaviour
{
    // Reference the limb’s child and controller
    [SerializeField] private Limb child;
    [SerializeField] private Slider control;

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

    private void Awake()
    {
        DrawLimb();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Move(initialJointLocation);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void DrawLimb()
    {
        mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>().material = material;

        mesh.vertices = limbVertices;

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

        // Update the joint location
        jointLocation = (Vector2)transformation.MultiplyPoint(jointLocation);

        // Apply the offset to the child, if not null
        if (child != null)
        {
            child.ApplyTransformation(transformation);
        }

    }

    // Translate the limb
    public void Move(Vector2 offset)
    {
        // Calculate the translation matrix
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

    private void OnControlChanged(float value)
    {
        RotateAroundPoint(jointLocation, value);
    }

    private void OnEnable()
    {
        if (control != null)
        {
            control.onValueChanged.AddListener(OnControlChanged);
        }
    }
    private void OnDisable()
    {
        if (control != null)
        {
            control.onValueChanged.RemoveListener(OnControlChanged);
        }
    }


}
