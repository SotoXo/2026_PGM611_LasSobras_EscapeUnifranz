using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class LvCol { public float cx, cy, w, h; }
[System.Serializable] public class LvTrig
{
    public string tipo, nombre, requiere, da, destino, mensaje, orient, enemigo, sprite;
    public float cx, cy, w, h;
}
[System.Serializable] public class LvData { public string nivel; public float ancho_u, alto_u; public LvCol[] colisiones; public LvTrig[] triggers; }

// Pon este script en un GameObject vacio en posicion (0,0,0).
// Las capas PNG del nivel se importan con Pivot = Bottom Left y se colocan en (0,0,0).
public class LevelBuilder : MonoBehaviour
{
    public TextAsset json;                  // nivelX.json
    public GameObject jugador;              // objeto Player de la escena (se mueve al spawn)
    public GameObject sombraPrefab, zombiPrefab;
    public Sprite puertaVertical, puertaHorizontal;      // puerta_v.png / door_wood.png
    public List<Sprite> spritesItems;       // llave, tarjeta, linterna, bateria, nota
    public int ordenItems = 50;

    void Start()
    {
        var d = JsonUtility.FromJson<LvData>(json.text);
        foreach (var c in d.colisiones)
        {
            var g = Nuevo("Col", c.cx, c.cy);
            g.AddComponent<BoxCollider2D>().size = new Vector2(c.w, c.h);
        }
        foreach (var t in d.triggers)
        {
            switch (t.tipo)
            {
                case "spawn_jugador":
                    if (jugador) jugador.transform.position = new Vector3(t.cx, t.cy, 0); break;
                case "spawn_enemigo":
                    var pf = t.enemigo == "sombra" ? sombraPrefab : zombiPrefab;
                    if (pf) Instantiate(pf, new Vector3(t.cx, t.cy, 0), Quaternion.identity, transform); break;
                default:
                    var g = Nuevo(t.nombre, t.cx, t.cy);
                    var bc = g.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(t.w, t.h);
                    var it = g.AddComponent<Interactable2D>();
                    it.requiere = t.requiere; it.da = t.da; it.mensaje = t.mensaje;
                    it.destino = t.destino == "FIN" ? "Fin" : t.destino;
                    if (t.tipo == "item") PonerSprite(g, t.sprite);
                    if (t.tipo == "puerta_bloqueada")
                    {
                        var b = Nuevo("Bloqueo_" + t.nombre, t.cx, t.cy);
                        b.AddComponent<BoxCollider2D>().size = new Vector2(1, 1);
                        var sr = b.AddComponent<SpriteRenderer>();
                        sr.sprite = t.orient == "v" ? puertaVertical : puertaHorizontal; sr.sortingOrder = 40;
                        it.bloqueo = b;
                    }
                    break;
            }
        }
    }

    GameObject Nuevo(string nombre, float x, float y)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(x, y, 0);
        return g;
    }

    void PonerSprite(GameObject g, string nombre)
    {
        var s = spritesItems.Find(x => x != null && x.name == nombre);
        if (!s) return;
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = ordenItems;
    }
}
