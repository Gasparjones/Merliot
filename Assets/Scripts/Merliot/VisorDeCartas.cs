using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Merliot
{
    /// Paso 1 de docs/pasar-a-unity.md: mostrar una carta con sus bandas leídas del JSON.
    /// Flechas para recorrer el mazo, 1-5 para filtrar por tipo.
    /// No es pantalla de juego: es la lupa para mirar los datos.
    public class VisorDeCartas : MonoBehaviour
    {
        static readonly (string tipo, string nombre)[] Filtros =
        {
            (null, "todas"), ("perm", "héroes"), ("mejora", "mejoras"),
            ("amuleto", "amuletos"), ("uso", "de uso"),
        };

        [SerializeField] int indice;
        [SerializeField] int filtro;

        MerliotData _data;
        List<Carta> _visibles = new();
        CartaView _carta;
        Text _encabezado, _ayuda;

        void Start()
        {
            var db = MerliotDatabase.I;
            if (db == null || db.Data == null)
            {
                Debug.LogError("[Merliot] No hay MerliotDatabase en la escena, o el JSON no cargó.");
                enabled = false;
                return;
            }

            _data = db.Data;
            ArmarPantalla();
            AplicarFiltro();
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;

            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Mover(+1);
            if (k.leftArrowKey.wasPressedThisFrame  || k.aKey.wasPressedThisFrame) Mover(-1);

            for (int i = 0; i < Filtros.Length; i++)
                if (k[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { filtro = i; AplicarFiltro(); }
        }

        void Mover(int paso)
        {
            if (_visibles.Count == 0) return;
            indice = (indice + paso + _visibles.Count) % _visibles.Count;
            Refrescar();
        }

        void AplicarFiltro()
        {
            var tipo = Filtros[filtro].tipo;
            _visibles = tipo == null
                ? _data.cartas
                : _data.cartas.Where(c => c.tipo == tipo).ToList();
            indice = 0;
            Refrescar();
        }

        void Refrescar()
        {
            if (_visibles.Count == 0) return;
            var c = _visibles[indice];
            _carta.Mostrar(c, _data);
            _encabezado.text = $"{indice + 1}/{_visibles.Count}   ·   {Filtros[filtro].nombre}";
        }

        // ── armado de la pantalla ─────────────────────────────────────────────

        void ArmarPantalla()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escala = canvasGo.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.matchWidthOrHeight = 0.5f;

            var fondo = new GameObject("fondo", typeof(RectTransform), typeof(Image));
            fondo.transform.SetParent(canvasGo.transform, false);
            fondo.GetComponent<RectTransform>().Estirar();
            fondo.GetComponent<Image>().color = new Color32(0x11, 0x10, 0x16, 0xff);

            _encabezado = Texto(canvasGo, "encabezado", new Vector2(0, 300), 20, new Color32(0xe8, 0xe3, 0xd8, 0xff));
            _ayuda = Texto(canvasGo, "ayuda", new Vector2(0, -300), 15, new Color32(0x6b, 0x65, 0x5c, 0xff));
            _ayuda.text = "← →  recorrer     1 todas · 2 héroes · 3 mejoras · 4 amuletos · 5 de uso";

            var cartaGo = new GameObject("carta", typeof(RectTransform));
            cartaGo.transform.SetParent(canvasGo.transform, false);
            var rt = cartaGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            _carta = cartaGo.AddComponent<CartaView>();
        }

        static Text Texto(GameObject padre, string nombre, Vector2 pos, int tam, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1200, 40);

            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = tam;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }
    }
}
