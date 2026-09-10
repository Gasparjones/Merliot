using System;
using UnityEngine;
using UnityEngine.UI;

namespace Merliot
{
    /// Constructores de uGUI a mano. El prototipo arma HTML con strings;
    /// acá se arma la misma jerarquía con objetos. Nada de esto es de juego:
    /// es la traducción de las clases del CSS a componentes.
    public static class U
    {
        static Font _fuente;
        public static Font Fuente => _fuente ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static T Comp<T>(GameObject go) where T : Component =>
            go.GetComponent<T>() ?? go.AddComponent<T>();

        public static LayoutElement Elem(GameObject go) => Comp<LayoutElement>(go);

        public static GameObject Nodo(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go;
        }

        /// Destroy es diferido: si sólo destruyo, el layout sigue contando los hijos
        /// viejos hasta el fin del frame y todo salta de lugar.
        public static void Limpiar(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var h = t.GetChild(i);
                h.SetParent(null, false);
                UnityEngine.Object.Destroy(h.gameObject);
            }
        }

        public static Image Fondo(GameObject go, Color c)
        {
            var img = Comp<Image>(go);
            img.color = c;
            return img;
        }

        public static Outline Borde(GameObject go, Color c, float grosor = 1f)
        {
            var o = Comp<Outline>(go);
            o.effectColor = c;
            o.effectDistance = new Vector2(grosor, grosor);
            return o;
        }

        public static VerticalLayoutGroup Col(GameObject go, int pad = 0, int gap = 0)
        {
            var l = Comp<VerticalLayoutGroup>(go);
            l.padding = new RectOffset(pad, pad, pad, pad);
            l.spacing = gap;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandWidth = true;
            l.childForceExpandHeight = false;
            return l;
        }

        public static HorizontalLayoutGroup Fila(GameObject go, int pad = 0, int gap = 0)
        {
            var l = Comp<HorizontalLayoutGroup>(go);
            l.padding = new RectOffset(pad, pad, pad, pad);
            l.spacing = gap;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandWidth = false;
            l.childForceExpandHeight = false;
            l.childAlignment = TextAnchor.MiddleLeft;
            return l;
        }

        public static GridLayoutGroup Grilla(GameObject go, Vector2 celda, int gap)
        {
            var l = Comp<GridLayoutGroup>(go);
            l.cellSize = celda;
            l.spacing = new Vector2(gap, gap);
            l.childAlignment = TextAnchor.UpperLeft;
            return l;
        }

        public static Text Txt(Transform padre, string s, int tam, Color c,
                               TextAnchor anclaje = TextAnchor.MiddleLeft,
                               FontStyle estilo = FontStyle.Normal, bool wrap = false)
        {
            var go = Nodo("txt", padre);
            var t = go.AddComponent<Text>();
            t.font = Fuente;
            t.text = s;
            t.fontSize = tam;
            t.color = c;
            t.alignment = anclaje;
            t.fontStyle = estilo;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// El microtexto en versalitas que el CSS repite en .rot, .tipo, .raza, .crlab.
        public static Text Micro(Transform padre, string s, Color? c = null,
                                 TextAnchor anclaje = TextAnchor.MiddleLeft)
        {
            var t = Txt(padre, s.ToUpperInvariant(), 9, c ?? Paleta.Tenue, anclaje);
            t.lineSpacing = 1f;
            return t;
        }

        /// El botón fantasma del prototipo: transparente, borde fino, texto claro.
        public static Button Btn(Transform padre, string etiqueta, Action alTocar, bool habilitado,
                                 Color? colorTexto = null, Color? colorBorde = null,
                                 int tam = 12, int altoMin = 26, bool solido = false)
        {
            var go = Nodo("btn", padre);
            var img = Fondo(go, solido ? (colorBorde ?? Paleta.Oro) : new Color(0, 0, 0, 0));
            img.raycastTarget = true;
            Borde(go, colorBorde ?? Paleta.Borde);

            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            b.interactable = habilitado;
            if (habilitado && alTocar != null) b.onClick.AddListener(() => alTocar());

            var col = Col(go, 4, 0);
            col.childAlignment = TextAnchor.MiddleCenter;
            Elem(go).minHeight = altoMin;

            var t = Txt(go.transform, etiqueta, tam,
                        solido ? Paleta.Noche : (colorTexto ?? Paleta.Tinta), TextAnchor.MiddleCenter);
            if (!habilitado) t.color = new Color(t.color.r, t.color.g, t.color.b, 0.26f);
            return b;
        }

        /// Botón sin layout propio: el que llama le arma el contenido.
        /// U.Btn ya le mete un VerticalLayoutGroup y los LayoutGroup son
        /// DisallowMultipleComponent, así que no se le puede poner otro encima.
        public static GameObject BtnCrudo(Transform padre, Action alTocar, bool habilitado, Color? borde = null)
        {
            var go = Nodo("btn", padre);
            var img = Fondo(go, new Color(0, 0, 0, 0));
            img.raycastTarget = true;
            Borde(go, borde ?? Paleta.Borde);
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            b.interactable = habilitado;
            if (habilitado && alTocar != null) b.onClick.AddListener(() => alTocar());
            return go;
        }

        /// Un pip: el cuadradito de color que reemplaza al ícono SVG del prototipo.
        public static GameObject Pip(Transform padre, Color c, int tam = 11, float alfa = 1f)
        {
            var go = Nodo("pip", padre);
            var img = Fondo(go, new Color(c.r, c.g, c.b, alfa));
            img.raycastTarget = false;
            var le = Elem(go);
            le.preferredWidth = le.minWidth = tam;
            le.preferredHeight = le.minHeight = tam;
            return go;
        }

        /// Una hilera de pips de un recurso, uno por punto.
        public static GameObject Pips(Transform padre, Produccion p, int tam = 11)
        {
            var go = Nodo("pips", padre);
            var f = Fila(go, 0, 3);
            f.childForceExpandWidth = false;
            foreach (var rec in Produccion.Todos)
                for (int i = 0; i < p.Get(rec); i++) Pip(go.transform, Paleta.De(rec), tam);
            return go;
        }

        /// El panel .caja: fondo, borde y padding 12.
        public static GameObject Caja(Transform padre, int gap = 6)
        {
            var go = Nodo("caja", padre);
            Fondo(go, Paleta.Caja);
            Borde(go, Paleta.Borde);
            Col(go, 12, gap);
            return go;
        }

        /// El encabezado de panel: título a la izquierda, contador a la derecha.
        public static GameObject Rot(Transform padre, string izq, string der = "")
        {
            var go = Nodo("rot", padre);
            var f = Fila(go, 0, 6);
            f.childForceExpandWidth = false;
            Elem(go).minHeight = 14;
            var a = Micro(go.transform, izq);
            Elem(a.gameObject).flexibleWidth = 1;
            if (!string.IsNullOrEmpty(der)) Micro(go.transform, der, null, TextAnchor.MiddleRight);
            return go;
        }

        /// Barra de progreso de 5px (vida de las criaturas, avance sobre un lugar).
        public static void Barra(Transform padre, float fraccion, Color relleno, Color pista)
        {
            var go = Nodo("barra", padre);
            Fondo(go, pista);
            Elem(go).minHeight = 5;
            var f = Fila(go, 0, 0);
            f.childForceExpandHeight = true;
            var lleno = Nodo("lleno", go.transform);
            Fondo(lleno, relleno);
            var le = Elem(lleno);
            le.flexibleWidth = Mathf.Clamp01(fraccion);
            le.minHeight = 5;
            var resto = Nodo("resto", go.transform);
            Elem(resto).flexibleWidth = 1f - Mathf.Clamp01(fraccion);
        }

        /// Espaciador flexible para empujar lo que sigue al otro extremo.
        public static void Empuje(Transform padre)
        {
            var go = Nodo("empuje", padre);
            Elem(go).flexibleWidth = 1;
        }

        public static void Alto(GameObject go, float alto)
        {
            var le = Elem(go);
            le.minHeight = le.preferredHeight = alto;
        }

        public static void Ancho(GameObject go, float ancho)
        {
            var le = Elem(go);
            le.minWidth = le.preferredWidth = ancho;
            le.flexibleWidth = 0;
        }

        public static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
