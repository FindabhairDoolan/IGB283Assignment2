using UnityEngine;

public class Rotate3D : MonoBehaviour
{
    private Mesh mesh;

    // Rotation speed
    [SerializeField] private Vector3 angle = new Vector3(1f, 1f, 1f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = gameObject.GetComponent<MeshFilter>().mesh;

    }

    // Update is called once per frame
    void Update()
    {
        RotateCube();
    }

    // Rotate the cube by angle radians per second
    private void RotateCube()
    {
        // Get the rotation matrix
        Matrix4x4 r = Rotate(angle * Time.deltaTime);
        // Apply the rotation to all vertices
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = r.MultiplyPoint(vertices[i]);
        }
        // Update the mesh
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    //code for rotation matrix
    private Matrix4x4 Rotate(Vector3 angle)
    {
        float cosx = Mathf.Cos(angle.x);
        float sinx = Mathf.Sin(angle.x);
        float cosy = Mathf.Cos(angle.y);
        float siny = Mathf.Sin(angle.y);
        float cosz = Mathf.Cos(angle.z);
        float sinz = Mathf.Sin(angle.z);

        Matrix4x4 rx = new Matrix4x4();
        rx.SetRow(0, new Vector4(1, 0, 0, 0));
        rx.SetRow(1, new Vector4(0, cosx, -sinx, 0));
        rx.SetRow(2, new Vector4(0, sinx, cosx, 0));
        rx.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 ry = new Matrix4x4();
        ry.SetRow(0, new Vector4(cosy, 0, siny, 0));
        ry.SetRow(1, new Vector4(0, 1, 0, 0));
        ry.SetRow(2, new Vector4(-siny, 0, cosy, 0));
        ry.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 rz = new Matrix4x4();
        rz.SetRow(0, new Vector4(cosz, -sinz, 0, 0));
        rz.SetRow(1, new Vector4(sinz, cosz, 0, 0));
        rz.SetRow(2, new Vector4(0, 0, 1, 0));
        rz.SetRow(3, new Vector4(0, 0, 0, 1));

        // Combine the rotations
        return ry * rx * rz;
    }

}
