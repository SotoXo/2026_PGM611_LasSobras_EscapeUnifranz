using UnityEngine;

public class CamaraSigue2D : MonoBehaviour
{
    public Transform objetivo; public float suavizado = 8f; public Vector2 limiteMin, limiteMax; // = (0,0) y (ancho_u, alto_u) del nivel
    void LateUpdate()
    {
        if (!objetivo) return;
        var cam = GetComponent<Camera>(); float h = cam.orthographicSize, w = h * cam.aspect;
        var p = Vector3.Lerp(transform.position, new Vector3(objetivo.position.x, objetivo.position.y, -10), Time.deltaTime * suavizado);
        if (limiteMax != Vector2.zero) { p.x = Mathf.Clamp(p.x, limiteMin.x + w, Mathf.Max(limiteMin.x + w, limiteMax.x - w)); p.y = Mathf.Clamp(p.y, limiteMin.y + h, Mathf.Max(limiteMin.y + h, limiteMax.y - h)); }
        transform.position = p;
    }
}
