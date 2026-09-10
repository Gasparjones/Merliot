using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Merliot
{
    /// Dibuja una carta leída del JSON. Es andamio: no tiene arte final ni layout definitivo,
    /// sirve para ver que los datos llegan enteros y que las bandas dan lo que uno cree.
    /// El arte, cuando exista, se busca por id en Resources/Arte/Cartas/{id}.
    public class CartaView : MonoBehaviour
    {
        public const float Ancho = 320f, Alto = 470f;

        static readonly Color Fondo   = new Color32(0x1b, 0x19, 0x22, 0xff);
        static readonly Color Panel   = new Color32(0x26, 0x23, 0x30, 0xff);
        static readonly Color Apagado = new Color32(0x3a, 0x36, 0x46, 0xff);
        static readonly Color Tinta   = new Color32(0xe8, 0xe3, 0xd8, 0xff);
        static readonly Color Tenue   = new Color32(0x9a, 0x92, 0x85, 0xff);

        /// Un color por recurso. Si mañana cambian, cambian acá y en ningún otro lado.
        static readonly Dictionary<string, Color> ColorRecurso = new()
        {
            { "vigor",    new Color32(0xc4, 0x4a, 0x3f, 0xff) },
            { "temple",   new Color32(0x3f, 0x7d, 0xc4, 0xff) },
            { "destreza", new Color32(0x4f, 0xa8, 0x5c, 0xff) },
            { "saber",    new Color32(0x8b, 0x5c, 0xc4, 0xff) },
            { "cristal",  new Color32(0x4a, 0xb8, 0xc4, 0xff) },
            { "vida",     new Color32(0xc4, 0x8b, 0x3f, 0xff) },
        };

        static Font _fuente;
        static Font Fuente => _fuente ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform _raiz;

        public void Mostrar(Carta c, MerliotData data)
        {
            Limpiar();

            _raiz = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            _raiz.sizeDelta = new Vector2(Ancho, Alto);
            Pintar(gameObject, Fondo);
            var borde = Comp<Outline>(gameObject);
            borde.effectColor = ColorDeTipo(c.tipo);
            borde.effectDistance = new Vector2(3, 3);

            var col = Vertical(gameObject, padding: 12, spacing: 6);
            col.childForceExpandHeight = false;

            Encabezado(col.gameObject, c);
            Subtitulo(col.gameObject, c, data);
            Arte(col.gameObject, c);
            TiraDelDado(col.gameObject, c);
            Bandas(col.gameObject, c);
            Cuerpo(col.gameObject, c);
            Pie(col.gameObject, c);
        }

        // ── piezas ────────────────────────────────────────────────────────────

        void Encabezado(GameObject padre, Carta c)
        {
            var fila = Horizontal(Hijo(padre, "encabezado", alto: 30), padding: 0, spacing: 4).gameObject;
            var nombre = Etiqueta(fila, c.nombre, 17, Tinta, TextAnchor.MiddleLeft, bold: true);
            Elem(nombre.gameObject).flexibleWidth = 1;

            if (c.costoCristales > 0)
            {
                var badge = Hijo(fila, "costo", alto: 30);
                Elem(badge).preferredWidth = 34;
                Pintar(badge, ColorRecurso["cristal"]);
                Etiqueta(badge, c.costoCristales.ToString(), 16, Color.white, TextAnchor.MiddleCenter, bold: true)
                    .rectTransform.Estirar();
            }
        }

        void Subtitulo(GameObject padre, Carta c, MerliotData data)
        {
            var partes = new List<string> { NombreDeTipo(c.tipo) };
            if (!string.IsNullOrEmpty(c.raza))     partes.Add(NombreDeRaza(c.raza, data));
            if (c.vida > 0)                        partes.Add($"vida {c.vida}");
            if (!string.IsNullOrEmpty(c.soloRaza)) partes.Add($"sólo {NombreDeRaza(c.soloRaza, data)}");
            if (!string.IsNullOrEmpty(c.pasiva))   partes.Add($"pasiva: {c.pasiva}");

            Etiqueta(Hijo(padre, "subtitulo", alto: 18), string.Join(" · ", partes),
                     12, Tenue, TextAnchor.MiddleLeft);
        }

        void Arte(GameObject padre, Carta c)
        {
            var caja = Hijo(padre, "arte", alto: 130);
            var sprite = Resources.Load<Sprite>($"Arte/Cartas/{c.id}");
            if (sprite != null)
            {
                var img = caja.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            else
            {
                Pintar(caja, Panel);
                Etiqueta(caja, "sin arte", 11, Apagado, TextAnchor.MiddleCenter).rectTransform.Estirar();
            }
        }

        /// La tira del 2 al 12: se enciende el número que alguna banda cubre.
        /// Es la lectura que más importa mientras se balancea.
        void TiraDelDado(GameObject padre, Carta c)
        {
            var fila = Horizontal(Hijo(padre, "dado", alto: 24), padding: 0, spacing: 2).gameObject;
            for (int v = 2; v <= 12; v++)
            {
                var banda = (c.bandas ?? new List<Banda>()).FirstOrDefault(b => b.Cubre(v));
                var celda = Hijo(fila, $"d{v}", alto: 24);
                Elem(celda).flexibleWidth = 1;
                Pintar(celda, banda != null ? ColorDeBanda(banda) : Apagado);
                Etiqueta(celda, v.ToString(), 11, banda != null ? Color.white : Tenue,
                         TextAnchor.MiddleCenter).rectTransform.Estirar();
            }
        }

        void Bandas(GameObject padre, Carta c)
        {
            if (c.bandas == null || c.bandas.Count == 0) return;
            var col = Vertical(Hijo(padre, "bandas", alto: 0), padding: 0, spacing: 2).gameObject;
            Elem(col).preferredHeight = c.bandas.Count * 20;

            foreach (var b in c.bandas)
            {
                var fila = Horizontal(Hijo(col, "banda", alto: 18), padding: 0, spacing: 6).gameObject;
                var rango = Etiqueta(fila, b.desde == b.hasta ? $"{b.desde}" : $"{b.desde}–{b.hasta}",
                                     13, ColorDeBanda(b), TextAnchor.MiddleLeft, bold: true);
                Elem(rango.gameObject).preferredWidth = 42;

                var texto = Etiqueta(fila, DescribirBanda(b), 13, Tinta, TextAnchor.MiddleLeft);
                Elem(texto.gameObject).flexibleWidth = 1;
            }
        }

        void Cuerpo(GameObject padre, Carta c)
        {
            var texto = !string.IsNullOrEmpty(c.efectoTexto) ? c.efectoTexto : c.texto;
            if (string.IsNullOrEmpty(texto)) return;
            var et = Etiqueta(Hijo(padre, "texto", alto: 44), texto, 12,
                              string.IsNullOrEmpty(c.efectoTexto) ? Tenue : Tinta, TextAnchor.UpperLeft);
            et.horizontalOverflow = HorizontalWrapMode.Wrap;
            et.verticalOverflow = VerticalWrapMode.Truncate;
        }

        void Pie(GameObject padre, Carta c)
        {
            var partes = new List<string>();
            if (c.copias > 0) partes.Add($"×{c.copias}");
            if (c.alCaer != null && (c.alCaer.cris > 0 || c.alCaer.curaTodos > 0))
                partes.Add($"al caer: {(c.alCaer.cris > 0 ? $"{c.alCaer.cris} cristal " : "")}" +
                           $"{(c.alCaer.curaTodos > 0 ? $"cura {c.alCaer.curaTodos} a todos" : "")}".Trim());
            partes.Add(c.id);
            Etiqueta(Hijo(padre, "pie", alto: 14), string.Join(" · ", partes), 10, Apagado, TextAnchor.MiddleLeft);
        }

        // ── lectura de datos ──────────────────────────────────────────────────

        public static string DescribirBanda(Banda b)
        {
            var sb = new StringBuilder();
            if (b.produce != null)
                foreach (var r in new[] { "vigor", "temple", "destreza", "saber" })
                    if (b.produce.Get(r) > 0) sb.Append($"+{b.produce.Get(r)} {r}  ");
            if (b.cristales > 0) sb.Append($"+{b.cristales} cristal  ");
            if (b.cura > 0)      sb.Append($"cura {b.cura}  ");
            if (!string.IsNullOrEmpty(b.porCadaRaza)) sb.Append($"por cada {b.porCadaRaza}");
            return sb.ToString().Trim();
        }

        static Color ColorDeBanda(Banda b)
        {
            if (b.produce != null)
                foreach (var r in new[] { "vigor", "temple", "destreza", "saber" })
                    if (b.produce.Get(r) > 0) return ColorRecurso[r];
            if (b.cristales > 0) return ColorRecurso["cristal"];
            if (b.cura > 0)      return ColorRecurso["vida"];
            return Apagado;
        }

        static Color ColorDeTipo(string tipo) => tipo switch
        {
            "perm"    => new Color32(0x8a, 0x7a, 0x4f, 0xff),
            "mejora"  => new Color32(0x4f, 0x7a, 0x8a, 0xff),
            "amuleto" => new Color32(0x7a, 0x4f, 0x8a, 0xff),
            _         => new Color32(0x5a, 0x5a, 0x5a, 0xff),
        };

        static string NombreDeTipo(string tipo) => tipo switch
        {
            "perm" => "Héroe", "mejora" => "Mejora", "amuleto" => "Amuleto", "uso" => "De uso", _ => tipo
        };

        static string NombreDeRaza(string id, MerliotData data) =>
            data?.razas?.FirstOrDefault(r => r.id == id)?.nombre ?? id;

        // ── helpers de uGUI ───────────────────────────────────────────────────

        /// Get-or-add. Importa más de lo que parece: los LayoutGroup son
        /// DisallowMultipleComponent, así que un segundo AddComponent devuelve null.
        static T Comp<T>(GameObject go) where T : Component =>
            go.GetComponent<T>() ?? go.AddComponent<T>();

        static LayoutElement Elem(GameObject go) => Comp<LayoutElement>(go);

        /// Destroy es diferido hasta el fin del frame: si sólo destruyo, al redibujar
        /// en el mismo frame el layout todavía cuenta los hijos viejos. Por eso los despego.
        void Limpiar()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var h = transform.GetChild(i);
                h.SetParent(null, false);
                Destroy(h.gameObject);
            }
        }

        static GameObject Hijo(GameObject padre, string nombre, float alto)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre.transform, false);
            var le = Elem(go);
            if (alto > 0) le.preferredHeight = alto;
            return go;
        }

        static VerticalLayoutGroup Vertical(GameObject go, int padding, int spacing)
        {
            var l = Comp<VerticalLayoutGroup>(go);
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.spacing = spacing;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandWidth = true;
            l.childForceExpandHeight = false;
            return l;
        }

        static HorizontalLayoutGroup Horizontal(GameObject go, int padding, int spacing)
        {
            var l = Comp<HorizontalLayoutGroup>(go);
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.spacing = spacing;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandWidth = false;
            l.childForceExpandHeight = true;
            return l;
        }

        static Text Etiqueta(GameObject padre, string texto, int tam, Color color,
                             TextAnchor anclaje, bool bold = false)
        {
            var go = new GameObject("txt", typeof(RectTransform));
            go.transform.SetParent(padre.transform, false);
            var t = go.AddComponent<Text>();
            t.font = Fuente;
            t.text = texto;
            t.fontSize = tam;
            t.color = color;
            t.alignment = anclaje;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void Pintar(GameObject go, Color c)
        {
            var img = Comp<Image>(go);
            img.color = c;
            img.raycastTarget = false;
        }
    }

    static class RectTransformExt
    {
        public static void Estirar(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
