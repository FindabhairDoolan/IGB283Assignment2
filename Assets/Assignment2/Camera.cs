using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoZoomFromCollidersIGB : MonoBehaviour
{
    [Header("Targets (2D)")]
    [SerializeField] private BoxCollider2D box1;  //collider on Base
    [SerializeField] private BoxCollider2D box2;  //collider on Base2

    [Header("Framing / Padding")]
    [SerializeField] private float worldPadding = 1.0f; //world-units padding around both boxes
    [SerializeField] private float minOrthoSize = 2.0f; //floor for orthographic size
    [SerializeField] private float minDistance = 2.0f;  //floor for perspective distance

    [Header("Smoothing")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float moveSmooth = 10f;    //higher = faster
    [SerializeField] private float zoomSmooth = 10f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (box1 == null || box2 == null) return;

        Rect r1 = WorldRectFromBox(box1);
        Rect r2 = WorldRectFromBox(box2);

        //Union AABB
        float minX = Mathf.Min(r1.xMin, r2.xMin);
        float maxX = Mathf.Max(r1.xMax, r2.xMax);
        float minY = Mathf.Min(r1.yMin, r2.yMin);
        float maxY = Mathf.Max(r1.yMax, r2.yMax);

        float width = maxX - minX;
        float height = maxY - minY;

        //Midpoint of union
        IGB283Vector mid = new IGB283Vector(minX + width * 0.5f, minY + height * 0.5f, 0f);

        //Current camera pos
        IGB283Vector cur = transform.position;

        if (cam.orthographic)
        {
            //Required half-extents with padding
            float halfW = 0.5f * width + worldPadding;
            float halfH = 0.5f * height + worldPadding;

            float sizeByH = halfH;
            float sizeByW = halfW / Mathf.Max(cam.aspect, 1e-4f);
            float targetSize = Mathf.Max(sizeByH, sizeByW, minOrthoSize);

            //Target pos centered on midpoint (preserve Z)
            IGB283Vector targetPos = new IGB283Vector(mid.x, mid.y, cur.z);

            //Apply movement
            transform.position = (useSmoothing
                ? Lerp(cur, targetPos, 1f - Mathf.Exp(-moveSmooth * Time.deltaTime))
                : targetPos).ToUnityVector();

            //Apply zoom
            cam.orthographicSize = useSmoothing
                ? Mathf.Lerp(cam.orthographicSize, targetSize, 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime))
                : targetSize;
        }
        else
        {
            //Perspective fit
            float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float tanV = Mathf.Tan(vFovRad * 0.5f);
            float tanH = tanV * cam.aspect;

            float halfW = 0.5f * width + worldPadding;
            float halfH = 0.5f * height + worldPadding;

            float distByH = halfH / Mathf.Max(tanV, 1e-4f);
            float distByW = halfW / Mathf.Max(tanH, 1e-4f);
            float requiredDistance = Mathf.Max(distByH, distByW, minDistance);

            //stay facing boxes
            IGB283Vector fwd = cam.transform.forward; //implicit IGB Vector
            fwd = IGB283Vector.Normalize(fwd);

            IGB283Vector desiredPos = new IGB283Vector(mid.x, mid.y, 0f) - (fwd * requiredDistance);
            //Keep Z only from desiredPos
            IGB283Vector targetPos = new IGB283Vector(desiredPos.x, desiredPos.y, desiredPos.z);

            transform.position = (useSmoothing
                ? Lerp(cur, targetPos, 1f - Mathf.Exp(-moveSmooth * Time.deltaTime))
                : targetPos).ToUnityVector();
        }
    }

    //Helpers

    //Build a world-space Rect to handle collider tracking
    private Rect WorldRectFromBox(BoxCollider2D box)
    {
        Transform t = box.transform;

        //center of collider
        IGB283Vector localOffset = (IGB283Vector)box.offset;

        //Include scale in the offset
        Vector3 lossy = t.lossyScale;
        localOffset = new IGB283Vector(localOffset.x * lossy.x, localOffset.y * lossy.y, 1f);

        //Rotation about Z
        float theta = t.eulerAngles.z * Mathf.Deg2Rad;

        //World center
        Matrix3x3 M = IGB283Transform.Translate((IGB283Vector)t.position) * IGB283Transform.Rotate(theta);
        IGB283Vector worldCenter = M.MultiplyPoint(localOffset);

        //Half-extents in local (scaled)
        float hx = Mathf.Abs(box.size.x * lossy.x) * 0.5f;
        float hy = Mathf.Abs(box.size.y * lossy.y) * 0.5f;

        //AABB half extents
        float c = Mathf.Cos(theta);
        float s = Mathf.Sin(theta);
        float aabbHx = Mathf.Abs(c) * hx + Mathf.Abs(s) * hy;
        float aabbHy = Mathf.Abs(s) * hx + Mathf.Abs(c) * hy;

        float xMin = worldCenter.x - aabbHx;
        float xMax = worldCenter.x + aabbHx;
        float yMin = worldCenter.y - aabbHy;
        float yMax = worldCenter.y + aabbHy;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static IGB283Vector Lerp(IGB283Vector a, IGB283Vector b, float t)
    {
        t = Mathf.Clamp01(t);
        return new IGB283Vector(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }
}
