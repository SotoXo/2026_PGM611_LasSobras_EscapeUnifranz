using UnityEngine;
using UnityEngine.SceneManagement;

// Rigidbody2D (Gravity 0) + Collider2D. Persigue al Player si esta cerca; si lo toca, reinicia el nivel.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemigoPersigue2D : MonoBehaviour
{
    public float velocidad = 1.6f, rango = 5f;
    Rigidbody2D rb; Transform jugador;

    void Awake() { rb = GetComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.freezeRotation = true; }
    void Start() { var p = GameObject.FindWithTag("Player"); if (p) jugador = p.transform; }

    void FixedUpdate()
    {
        if (!jugador) return;
        Vector2 d = jugador.position - transform.position;
        rb.linearVelocity = d.magnitude < rango ? d.normalized * velocidad : Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.CompareTag("Player")) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
