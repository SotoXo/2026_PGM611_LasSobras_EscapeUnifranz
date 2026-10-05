using UnityEngine;
using UnityEngine.InputSystem;

// Rigidbody2D (Gravity Scale 0, Freeze Rotation) + Collider2D + tag "Player".
// Animator opcional con parametros: x (float), y (float), caminando (bool).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerTopDown2D : MonoBehaviour
{
    public float velocidad = 3f;
    Rigidbody2D rb; Animator anim; Vector2 mover, ultimo = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.freezeRotation = true;
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        var k = Keyboard.current; if (k == null) return;
        mover = Vector2.zero;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) mover.y += 1;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) mover.y -= 1;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) mover.x -= 1;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) mover.x += 1;
        mover = mover.normalized;
        if (mover != Vector2.zero) ultimo = mover;
        if (anim) { anim.SetFloat("x", ultimo.x); anim.SetFloat("y", ultimo.y); anim.SetBool("caminando", mover != Vector2.zero); }
    }

    void FixedUpdate() { rb.linearVelocity = mover * velocidad; }
}
