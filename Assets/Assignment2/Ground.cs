using UnityEngine;

public class Ground : MonoBehaviour
{

    [SerializeField] private Material material;
    [SerializeField] private Color colour = new Color(0.4f, 0.25f, 0.1f); //brownish
    [SerializeField] private Vector3[] groundVertices = new Vector3[4];
    private Mesh mesh;

    void Awake()
    {
        DrawGround();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void DrawGround()
    {
        mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>().material = material;

        mesh.vertices = groundVertices;

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

}
