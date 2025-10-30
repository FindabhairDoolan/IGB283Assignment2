using UnityEngine;

public class Planet : MonoBehaviour 
{

    [SerializeField] private float speed = 1.0f;
    [SerializeField] private float orbitSpeed = 1.0f;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private Vector3 rotationOrigin;
    [SerializeField] private Vector3 initialPosition;
    [SerializeField] private GameObject rotationOriginObject;

    private Mesh mesh;

    void Start () {
        mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        Matrix3x3 t = Translate(initialPosition);

        //apply transformation to mesh
        for(int i = 0; i < vertices.Length; i++) {
            Vector3 v = t.MultiplyPoint(vertices[i]);

            vertices[i].x = v.x;
            vertices[i].y = v.y;
        }

        mesh.vertices = vertices;
        // Recalculate the bounds of the mesh
        mesh.RecalculateBounds();
    }
    
    // Update is called once per frame
    void Update () {
        Vector3[] vertices = mesh.vertices;

        Matrix3x3 rOrigin = Rotate(0);
        //If rotate around earth
        if(rotationOriginObject != null) {
            Planet originObject = rotationOriginObject.GetComponent<Planet>();
            rotationOrigin = rotationOriginObject.transform.GetComponent<Renderer>().bounds.center;
            rOrigin = Rotate(originObject.speed * Time.deltaTime * originObject.orbitSpeed);
        }

        //Orbit
        Matrix3x3 tOrigin = Translate(-rotationOrigin);
        Matrix3x3 tOriginal = Translate(rotationOrigin);
        Matrix3x3 r = Rotate(speed * Time.deltaTime * orbitSpeed);
        Matrix3x3 M = rOrigin * tOriginal * r * tOrigin;

        //Self rotation
        Matrix3x3 tOrigin2 = Translate(-transform.GetComponent<Renderer>().bounds.center);
        Matrix3x3 tOriginal2 = Translate(transform.GetComponent<Renderer>().bounds.center);
        Matrix3x3 r2 = Rotate(speed * Time.deltaTime * rotationSpeed);
        M = M * tOriginal2 * r2 * tOrigin2;

        for(int i = 0; i < vertices.Length; i++) {
            vertices[i] = M.MultiplyPoint(vertices[i]);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
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

    private Matrix3x3 Translate(Vector3 offset)
    {
        // Create a new matrix for rotation
        Matrix3x3 t = new Matrix3x3(
        new Vector3(1, 0, offset.x),
        new Vector3(0, 1, offset.y),
        new Vector3(0, 0, 1));
        return t;
    }   
}
