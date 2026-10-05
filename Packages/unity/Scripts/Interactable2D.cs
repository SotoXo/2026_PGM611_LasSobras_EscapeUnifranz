using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Inventario minimo (ids de texto: "llave_lab", "tarjeta_acceso", ...)
public static class Inventario
{
    static readonly HashSet<string> items = new HashSet<string>();
    public static bool Tiene(string id) => string.IsNullOrEmpty(id) || items.Contains(id);
    public static void Dar(string id) { items.Add(id); }
    public static void Limpiar() { items.Clear(); }
}

// Pon esto en un objeto con BoxCollider2D (Is Trigger). El jugador necesita tag "Player".
// Tecla E: pide item, entrega item, abre puerta (destruye el bloqueo) o cambia de escena.
public class Interactable2D : MonoBehaviour
{
    public static System.Action<string> Mensaje = m => Debug.Log(m); // conecta aqui tu UI de texto
    public string requiere, da, destino, mensaje;
    public GameObject bloqueo;
    bool dentro;

    void OnTriggerEnter2D(Collider2D c) { if (c.CompareTag("Player")) dentro = true; }
    void OnTriggerExit2D(Collider2D c) { if (c.CompareTag("Player")) dentro = false; }

    void Update()
    {
        var k = Keyboard.current;
        if (dentro && k != null && k.eKey.wasPressedThisFrame) Usar();
    }

    void Usar()
    {
        if (!Inventario.Tiene(requiere))
        {
            Mensaje(string.IsNullOrEmpty(mensaje) ? "Te falta: " + requiere : mensaje);
            return;
        }
        if (!string.IsNullOrEmpty(da)) { Inventario.Dar(da); Mensaje("Recogiste: " + da); }
        if (bloqueo != null) { Destroy(bloqueo); Mensaje("Abierto."); }
        if (!string.IsNullOrEmpty(destino)) { SceneManager.LoadScene(destino); return; }
        if (!string.IsNullOrEmpty(da) || bloqueo != null) Destroy(gameObject);
        else if (!string.IsNullOrEmpty(mensaje)) Mensaje(mensaje);
    }
}
