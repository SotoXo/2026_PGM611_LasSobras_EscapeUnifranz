using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class FinalAtmosphereSetup
    {
        private const string Art = "Assets/_Project/Art/Environment/Abandono/";
        private enum Prop { Grieta, Muro, Rocas, Papeles, Equipo, Cables, Suciedad, Mueble, Aviso, Cristal, Robot }
        private static readonly Color32 Shadow = new Color32(26, 25, 31, 255);
        private static readonly Color32 Slate = new Color32(76, 74, 81, 255);
        private static readonly Color32 Stone = new Color32(118, 110, 111, 255);
        private static readonly Color32 Dust = new Color32(158, 143, 128, 255);
        private static readonly Color32 Rust = new Color32(106, 48, 38, 255);
        private static readonly Color32 Amber = new Color32(210, 141, 59, 255);
        private static readonly Color32 Glass = new Color32(135, 164, 163, 215);

        public static void Install()
        {
            if (!AssetDatabase.IsValidFolder(Art.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Environment", "Abandono");
            foreach (Prop prop in Enum.GetValues(typeof(Prop)))
                FinalUiAssets.WriteSprite(PathOf(prop), 24, 24, 32, Vector4.zero, (x, y) => Pixel(prop, x, y));

            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guarda las escenas abiertas.");
            try
            {
                foreach (string name in new[] { "Z00_Entrada", "Z01_Hall", "Z02_ARCA", "Z03_Piso2", "Z04_Piso3" })
                    CampaignReleaseSetup.Edit("Assets/_Project/Scenes/Zones/" + name + ".unity", scene => Dress(scene, name));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static string PathOf(Prop prop) => Art + prop + ".png";

        private static void Dress(Scene scene, string zone)
        {
            if (scene.GetRootGameObjects().Any(o => o.name == "Ambientación · Abandono")) return;
            var root = new GameObject("Ambientación · Abandono").transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            void Put(Prop prop, float x, float y, float scale = 1f, bool solid = false)
            {
                var obj = new GameObject(prop.ToString());
                obj.transform.SetParent(root, false);
                obj.transform.position = new Vector3(x, y, 0);
                obj.transform.localScale = Vector3.one * scale;
                var sprite = obj.AddComponent<SpriteRenderer>();
                sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(prop));
                sprite.sharedMaterial = material;
                sprite.sortingLayerName = solid || prop == Prop.Aviso || prop == Prop.Grieta ? "World" : "Decals";
                sprite.sortingOrder = sprite.sortingLayerName == "World" ? World.WorldYSort2D.Order(y) : 4;
                if (!solid) return;
                var body = obj.AddComponent<BoxCollider2D>();
                body.size = new Vector2(.58f, .31f);
                body.offset = new Vector2(0, -.05f);
            }

            if (zone == "Z03_Piso2")
            {
                Put(Prop.Grieta, 5.35f, 6.08f, .9f);
                Put(Prop.Muro, 18.3f, 5.55f, 1.25f);
                Put(Prop.Papeles, 12.35f, 5.05f, .75f);
                Put(Prop.Cables, 15.05f, 6.75f, 1.05f);
                Put(Prop.Equipo, 16.4f, 8.35f, .9f);
                Put(Prop.Aviso, 19.25f, 5.1f, .8f);
                Put(Prop.Robot, 21.05f, 4.85f, 1.3f);
                Put(Prop.Suciedad, 9.8f, 4.1f, .85f);
                return;
            }
            if (zone == "Z04_Piso3")
            {
                Put(Prop.Grieta, 6.4f, 4.95f, 1.05f);
                Put(Prop.Papeles, 5.45f, 4.15f, .8f);
                Put(Prop.Cables, 11.1f, 4.05f, .9f);
                Put(Prop.Cristal, 19.3f, 4.95f, 1.2f);
                Put(Prop.Mueble, 20.7f, 5.4f, 1f);
                Put(Prop.Robot, 23.7f, 4.9f, 1.05f);
                Put(Prop.Aviso, 24.8f, 5.35f, .82f);
                Put(Prop.Suciedad, 14.1f, 3.45f, .9f);
                return;
            }

            Put(Prop.Rocas, 21.93f, 2.62f, 1.55f, true);
            Put(Prop.Rocas, 12.98f, 4.64f, 1.28f, true);
            // Keep the small stair landing beside the elevator clear.
            Put(Prop.Aviso, 22.66f, 2.66f, .75f);
            Put(Prop.Papeles, 4.95f, 7.18f, .75f);
            Put(Prop.Grieta, 5.4f, 8.55f, .95f);
            Put(Prop.Muro, 9.88f, 7.12f, .92f);
            Put(Prop.Equipo, 6.80f, 6.47f, .72f);
            Put(Prop.Suciedad, 8.18f, 6.13f, 1.25f);
            Put(Prop.Cables, 11.85f, 6.44f, .85f);
            Put(Prop.Cristal, 13.5f, 5.82f, .8f);
        }

        private static Color Pixel(Prop prop, int x, int y)
        {
            int grain = (x * 29 + y * 41 + x * y * 7) % 17;
            switch (prop)
            {
                case Prop.Grieta:
                    int line = 6 + (23 - y) / 2 + (y / 3) % 2;
                    if ((y > 3 && y < 22 && Mathf.Abs(x - line) < 2) ||
                        (y > 7 && y < 14 && x == 6 + y)) return Shadow;
                    if (y > 6 && y < 21 && x == line - 2) return new Color32(97, 89, 83, 210);
                    break;
                case Prop.Muro:
                    if (x < 2 || x > 20 || y < 3 || y > 19 - Mathf.Abs(x - 11) / 2) break;
                    if (y % 5 == 0 || (x + 4 * (y / 5)) % 9 == 0) return Shadow;
                    return grain < 4 ? Dust : grain < 8 ? Slate : Stone;
                case Prop.Rocas:
                    for (int cluster = 0; cluster < 3; cluster++)
                    {
                        int dx = x - (5 + cluster * 7), dy = y - (6 + (cluster % 2) * 4);
                        int distance = dx * dx * 2 + dy * dy;
                        if (distance > 50 + cluster * 2) continue;
                        if (distance > 30 + cluster * 2 || dy < -2) return Shadow;
                        return dy > 1 && grain < 10 ? Dust : grain < 3 ? Slate : Stone;
                    }
                    break;
                case Prop.Papeles:
                    if ((x > 2 && x < 12 && y > 6 && y < 16) ||
                        (x > 11 && x < 20 && y > 3 && y < 11 && x + y < 28))
                        return y % 4 == 0 && x % 9 > 3 ? Slate : new Color32(181, 168, 141, 255);
                    break;
                case Prop.Equipo:
                case Prop.Robot:
                    if (x < 2 || x > 21 || y < 3 || y > 17 || x + y > 37) break;
                    if (y == 3 || x <= 3 || x >= 20 || y == 17) return Shadow;
                    if (prop == Prop.Equipo && x > 5 && x < 16 && y > 8)
                        return y == x - 1 ? Glass : Shadow;
                    if (prop == Prop.Robot && x > 10 && x < 16 && y == 12) return Rust;
                    return grain < 5 ? Rust : grain < 11 ? Slate : Stone;
                case Prop.Cables:
                    if (x > 1 && x < 23 && (y == 12 + Mathf.RoundToInt(4 * Mathf.Sin(x * .52f)) ||
                        y == 8 + Mathf.RoundToInt(3 * Mathf.Cos(x * .41f))))
                        return x > 16 ? Amber : Shadow;
                    if (x > 5 && x < 21 && y == x / 3 + 3) return Rust;
                    break;
                case Prop.Suciedad:
                    float ellipse = (x-11)*(x-11)*.014f + (y-11)*(y-11)*.033f;
                    if (ellipse < .95f && grain < 16 - ellipse * 9)
                        return new Color32(61, 46, 41, (byte)(120 - ellipse * 52));
                    break;
                case Prop.Mueble:
                    if ((x >= 2 && x <= 17 && y >= 10 && y <= 14) ||
                        (x >= 4 && x <= 6 && y >= 3 && y <= 12) ||
                        (x >= 16 && x <= 20 && y >= 4 && y <= 6))
                        return grain < 4 ? Shadow : grain < 8 ? Amber : Rust;
                    break;
                case Prop.Aviso:
                    if (y < 4 || y > 20 || Mathf.Abs(x - 11) > (22 - y) / 2 + 2) break;
                    if ((x >= 10 && x <= 12 && y > 9 && y < 17) || (x >= 10 && x <= 12 && y == 7))
                        return Shadow;
                    return y == 4 || Mathf.Abs(x - 11) == (22 - y) / 2 + 2 ? Shadow : Amber;
                case Prop.Cristal:
                    if (((x > 3 && x < 9 && y > 3 && y < 13 && x + y > 10) ||
                         (x > 11 && x < 20 && y > 6 && y < 18 && x - y > 1))) return grain < 9 ? Glass : Slate;
                    if (grain == 1 && x > 3 && x < 22 && y > 2 && y < 18) return Glass;
                    break;
            }
            return Color.clear;
        }
    }
}
