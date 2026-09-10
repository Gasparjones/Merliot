using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Merliot
{
    /// La misma pantalla que prototipo/index.html, armada con uGUI.
    /// Se repinta entera en cada cambio, igual que el prototipo hace innerHTML:
    /// con una party de cinco y una mano de siete no hace falta nada más fino,
    /// y así no hay estado de UI que se desincronice del estado de juego.
    public class PantallaPartida : MonoBehaviour
    {
        const float AnchoPagina = 1480f, AnchoCol1 = 300f, AnchoCol3 = 320f;

        Partida P;
        readonly List<string> bitacora = new List<string>();

        Text stEnPie, stEtapa, stTurno, stMano, stRes, stEnem;
        Text terrNombre, terrIntro, terrProblema;
        Transform cond, terr, dados, pend, rec, crisbox, acc, party, amuletos, cementerio, eventos, mano, log;
        Text rotFase, dtot, rotRec, npart, namul, ncem, rotEv, pistaFase;
        GameObject velo;
        Transform modal;

        void Start()
        {
            var db = MerliotDatabase.I;
            if (db == null || db.Data == null)
            {
                Debug.LogError("[Merliot] No hay MerliotDatabase en la escena, o el JSON no cargó.");
                enabled = false;
                return;
            }

            ArmarChrome();

            P = new Partida(db.Data);
            P.AlLoguear += Anotar;
            P.Cambio += Pintar;
            P.Iniciar();
        }

        void Anotar(string s)
        {
            bitacora.Insert(0, s);
            while (bitacora.Count > 30) bitacora.RemoveAt(bitacora.Count - 1);
        }

        // ══ estructura fija de la página ══════════════════════════════════════

        void ArmarChrome()
        {
            var canvasGo = U.Nodo("Canvas", transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escala = canvasGo.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var fondo = U.Nodo("fondo", canvasGo.transform);
            U.Estirar(fondo.GetComponent<RectTransform>());
            U.Fondo(fondo, Paleta.Noche);

            // scroll de toda la página, como el scroll del navegador
            var scrollGo = U.Nodo("scroll", canvasGo.transform);
            U.Estirar(scrollGo.GetComponent<RectTransform>());
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 28;
            scrollGo.AddComponent<RectMask2D>();

            var viewport = U.Nodo("viewport", scrollGo.transform);
            U.Estirar(viewport.GetComponent<RectTransform>());
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var contenido = U.Nodo("w", viewport.transform);
            var rt = contenido.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(AnchoPagina, 0);
            scroll.content = rt;
            U.Col(contenido, 13, 11);
            contenido.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Encabezado(contenido.transform);
            Fila1(contenido.transform);
            CajaEventos(contenido.transform);
            CajaMano(contenido.transform);
            CajaLog(contenido.transform);
            Overlay(canvasGo.transform);
        }

        void Encabezado(Transform padre)
        {
            var h = U.Nodo("header", padre);
            var f = U.Fila(h, 0, 15);
            f.childAlignment = TextAnchor.LowerLeft;
            U.Alto(h, 44);

            var titulo = U.Txt(h.transform, "MERLIOT — <i>los lindes de Urmand</i>", 26, Paleta.Tinta);
            U.Elem(titulo.gameObject).flexibleWidth = 1;

            stEnPie = Stat(h.transform, "en pie");
            stEtapa = Stat(h.transform, "etapa / 3");
            stTurno = Stat(h.transform, "turno");
            stMano  = Stat(h.transform, "en mano");
            stRes   = Stat(h.transform, "resueltos");
            stEnem  = Stat(h.transform, "enemigos");
        }

        Text Stat(Transform padre, string caption)
        {
            var go = U.Nodo("s", padre);
            var col = U.Col(go, 0, 0);
            col.childAlignment = TextAnchor.MiddleCenter;
            U.Ancho(go, 58);
            var n = U.Txt(go.transform, "—", 20, Paleta.Tinta, TextAnchor.MiddleCenter);
            U.Micro(go.transform, caption, null, TextAnchor.MiddleCenter);
            return n;
        }

        void Fila1(Transform padre)
        {
            var fila = U.Nodo("fila1", padre);
            var f = U.Fila(fila, 0, 11);
            f.childAlignment = TextAnchor.UpperLeft;
            f.childForceExpandHeight = false;

            // ── columna 1: el terreno
            var c1 = U.Nodo("col1", fila.transform);
            U.Col(c1, 0, 11);
            U.Ancho(c1, AnchoCol1);
            var cajaTerr = U.Caja(c1.transform);
            U.Rot(cajaTerr.transform, "El terreno");
            terrNombre = U.Txt(cajaTerr.transform, "", 15, Paleta.Tinta, TextAnchor.UpperLeft, wrap: true);
            terrIntro = U.Txt(cajaTerr.transform, "", 11, Paleta.Tenue,
                              TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
            terrProblema = U.Txt(cajaTerr.transform, "", 11, Paleta.Rojo,
                                 TextAnchor.UpperLeft, FontStyle.Normal, wrap: true);
            cond = U.Nodo("cond", cajaTerr.transform).transform;
            U.Col(cond.gameObject, 0, 4);
            terr = U.Nodo("terr", c1.transform).transform;
            U.Col(terr.gameObject, 0, 7);

            // ── columna 2: el motor del turno
            var c2 = U.Nodo("col2", fila.transform);
            U.Col(c2, 0, 0);
            U.Elem(c2).flexibleWidth = 1;
            var cajaTurno = U.Caja(c2.transform, 9);
            var rf = U.Rot(cajaTurno.transform, "fase");
            rotFase = rf.transform.GetChild(0).GetComponent<Text>();

            dados = U.Nodo("dados", cajaTurno.transform).transform;
            var fd = U.Fila(dados.gameObject, 0, 9);
            fd.childAlignment = TextAnchor.MiddleCenter;
            fd.childForceExpandWidth = false;
            U.Alto(dados.gameObject, 52);

            dtot = U.Txt(cajaTurno.transform, "", 15, Paleta.Tenue, TextAnchor.MiddleCenter, wrap: true);
            U.Alto(dtot.gameObject, 44);

            pend = U.Nodo("pendientes", cajaTurno.transform).transform;
            U.Col(pend.gameObject, 0, 0);

            var rr = U.Rot(cajaTurno.transform, "recursos");
            rotRec = rr.transform.GetChild(0).GetComponent<Text>();

            rec = U.Nodo("rec", cajaTurno.transform).transform;
            var fr = U.Fila(rec.gameObject, 0, 6);
            fr.childForceExpandWidth = true;
            U.Alto(rec.gameObject, 44);

            crisbox = U.Nodo("crisbox", cajaTurno.transform).transform;
            U.Col(crisbox.gameObject, 0, 0);

            U.Txt(cajaTurno.transform,
                  "Dos de cualquier tipo valen uno del que te falte. Los cristales son lo único que se acumula. " +
                  "El Temple que no gastes frena hasta 2 de mordida.",
                  11, Paleta.Tenue, TextAnchor.UpperCenter, FontStyle.Italic, wrap: true)
             .gameObject.name = "recordatorio";

            pistaFase = U.Txt(cajaTurno.transform, "", 13, Paleta.Tenue,
                              TextAnchor.MiddleCenter, FontStyle.Italic, wrap: true);
            U.Alto(pistaFase.gameObject, 38);

            acc = U.Nodo("acc", cajaTurno.transform).transform;
            var fa = U.Fila(acc.gameObject, 0, 6);
            fa.childForceExpandWidth = true;

            // ── columna 3: tu party
            var c3 = U.Nodo("col3", fila.transform);
            U.Col(c3, 0, 0);
            U.Ancho(c3, AnchoCol3);
            var cajaParty = U.Caja(c3.transform, 8);
            var rp = U.Rot(cajaParty.transform, "Tu party · el 1 recibe el daño", "0");
            npart = rp.transform.GetChild(1).GetComponent<Text>();
            party = U.Nodo("party", cajaParty.transform).transform;
            U.Col(party.gameObject, 0, 7);

            var ra = U.Rot(cajaParty.transform, "Amuletos", "0 de 2");
            namul = ra.transform.GetChild(1).GetComponent<Text>();
            amuletos = U.Nodo("amuletos", cajaParty.transform).transform;
            U.Col(amuletos.gameObject, 0, 6);

            var rc = U.Rot(cajaParty.transform, "Cementerio", "");
            ncem = U.Micro(rc.transform, "", null, TextAnchor.MiddleRight);
            cementerio = U.Nodo("cementerio", cajaParty.transform).transform;
            var fc = U.Fila(cementerio.gameObject, 0, 5);
            fc.childForceExpandWidth = false;
        }

        void CajaEventos(Transform padre)
        {
            var caja = U.Caja(padre, 8);
            var r = U.Rot(caja.transform, "Lo que tenés delante", "");
            rotEv = U.Micro(r.transform, "", null, TextAnchor.MiddleRight);
            eventos = U.Nodo("eventos", caja.transform).transform;
            U.Grilla(eventos.gameObject, new Vector2(232, 148), 8);
            eventos.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void CajaMano(Transform padre)
        {
            mano = U.Nodo("mano", padre).transform;
            U.Grilla(mano.gameObject, new Vector2(206, 172), 9);
            mano.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void CajaLog(Transform padre)
        {
            var caja = U.Nodo("log", padre);
            U.Fondo(caja, Paleta.Caja);
            U.Borde(caja, Paleta.Borde);
            U.Col(caja, 10, 5);
            U.Alto(caja, 132);
            log = caja.transform;
        }

        void Overlay(Transform padre)
        {
            velo = U.Nodo("ov", padre);
            U.Estirar(velo.GetComponent<RectTransform>());
            U.Fondo(velo, Paleta.Velo);
            var col = U.Col(velo, 40, 0);
            col.childAlignment = TextAnchor.MiddleCenter;
            col.childForceExpandWidth = false;

            // el interludio no entra en una pantalla, así que el modal scrollea
            var scrollGo = U.Nodo("scroll", velo.transform);
            U.Ancho(scrollGo, 900);
            U.Elem(scrollGo).flexibleHeight = 1;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 28;
            scrollGo.AddComponent<RectMask2D>();

            var viewport = U.Nodo("viewport", scrollGo.transform);
            U.Estirar(viewport.GetComponent<RectTransform>());
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var m = U.Nodo("mod", viewport.transform);
            U.Fondo(m, Paleta.Caja);
            U.Borde(m, Paleta.Borde);
            U.Col(m, 28, 12);
            var rt = m.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(900, 0);
            m.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rt;

            modal = m.transform;
            velo.SetActive(false);
        }

        // ══ el repintado ══════════════════════════════════════════════════════

        void Pintar()
        {
            if (P.fase == Fase.Interludio) { PintarInterludio(); return; }
            int v = P.TiradaActual;

            PintarStats();
            PintarObjetivo();
            PintarTerreno(v);
            PintarDados();
            PintarPendientes();
            PintarRecursos();
            PintarCristales();
            PintarFase();
            PintarAcciones();
            PintarParty(v);
            PintarAmuletos();
            PintarCementerio();
            PintarEventos();
            PintarMano();
            PintarLog();
            PintarFinal();
        }

        void PintarStats()
        {
            stEnPie.text = P.party.Count.ToString();
            stEnPie.color = P.party.Count <= 1 ? Paleta.Rojo : Paleta.Tinta;
            stEtapa.text = P.etapa.ToString();
            stTurno.text = P.fase == Fase.Prep ? "—" : P.turno.ToString();
            stMano.text = P.mano.Count.ToString();
            stRes.text = P.resueltos.ToString();
            stEnem.text = P.enemigos.Count.ToString();
            stEnem.color = P.enemigos.Count >= 3 ? Paleta.Rojo : Paleta.Tinta;
        }

        void PintarObjetivo()
        {
            // El prototipo dejó el nombre del terreno fijo en el HTML, así que en la
            // etapa 2 sigue diciendo "Los Lindes de Urmand". Acá sale de los datos.
            terrNombre.text = P.terreno.nombre;
            terrIntro.text = P.terreno.intro;
            terrProblema.text = P.terreno.problema;

            U.Limpiar(cond);
            U.Txt(cond, "Para llevarte el cofre tenés que <b>limpiar el linde</b>:", 13, Paleta.Objetivo, wrap: true);
            foreach (var a in P.terreno.apariciones)
            {
                bool vivo = P.enemigos.Any(e => e.nombre == a.n);
                bool falta = P.turno < a.turno;
                if (vivo) U.Txt(cond, $"· {a.n}", 13, Paleta.Objetivo, wrap: true);
                else if (falta) U.Txt(cond, "? <i>algo más, todavía no aparece</i>", 13, Paleta.Tenue, wrap: true);
                else U.Txt(cond, $"✓ {a.n}", 13, Paleta.Destreza, wrap: true);
            }
            U.Txt(cond, "Los efímeros duran un turno: si no los pagás, te caen encima.",
                  11, Paleta.Tenue, TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
        }

        void PintarTerreno(int v)
        {
            U.Limpiar(terr);
            var amb = P.terreno.ambiental;
            bool despierta = amb.bandas.Any(b => v >= b.desde && v <= b.hasta);
            var ficha = Ficha(terr, "amb", amb.nombre, null, amb.texto, despierta);
            foreach (var b in amb.bandas)
                BandaEnemiga(ficha.transform, b, v);

            for (int i = 0; i < P.enemigos.Count; i++) PintarEnemigo(P.enemigos[i], i, v);
        }

        void PintarEnemigo(Enemigo e, int idx, int v)
        {
            bool despierta = e.bandas.Any(b => v >= b.desde && v <= b.hasta);
            var ficha = Ficha(terr, e.esCriatura ? "malo" : "lugar", e.nombre,
                              e.esCriatura ? "criatura" : "lugar", e.texto, despierta);
            foreach (var b in e.bandas) BandaEnemiga(ficha.transform, b, v);

            bool gasto = P.fase == Fase.Gasto && !P.Terminada;

            if (e.esCriatura)
            {
                U.Barra(ficha.transform, e.vidaMax == 0 ? 0 : (float)e.vida / e.vidaMax, Paleta.Vigor, Paleta.PistaVida);
                var pie = U.Nodo("vidatxt", ficha.transform);
                var f = U.Fila(pie, 0, 5);
                f.childForceExpandWidth = false;
                U.Alto(pie, 24);
                U.Pip(pie.transform, Paleta.Vida, 9);
                U.Txt(pie.transform, $"{e.vida} de {e.vidaMax}", 11, Paleta.Tenue);
                U.Empuje(pie.transform);
                if (gasto)
                {
                    int i = idx;
                    var b = U.Btn(pie.transform, "pegarle", () => P.Golpear(i), P.r.vigor > 0,
                                  Paleta.Vigor, Paleta.BordeDe("vigor"), 11, 22);
                    U.Ancho(b.gameObject, 72);
                }
            }
            else
            {
                var eq = U.Nodo("equipo", ficha.transform);
                var f = U.Fila(eq, 0, 8);
                f.childForceExpandWidth = false;
                U.Alto(eq, 24);
                foreach (var rr in Produccion.Todos)
                {
                    if (e.costo.Get(rr) == 0) continue;
                    var grupo = U.Nodo("obj", eq.transform);
                    var fg = U.Fila(grupo, 0, 3);
                    fg.childForceExpandWidth = false;
                    for (int k = 0; k < e.pagado.Get(rr); k++) U.Pip(grupo.transform, Paleta.De(rr), 10);
                    for (int k = 0; k < e.costo.Get(rr) - e.pagado.Get(rr); k++)
                        U.Pip(grupo.transform, Paleta.De(rr), 10, 0.25f);
                }
                if (gasto)
                {
                    var botones = U.Nodo("pagar", ficha.transform);
                    var fb = U.Fila(botones, 0, 5);
                    fb.childForceExpandWidth = false;
                    U.Alto(botones, 24);
                    foreach (var rr in Produccion.Todos)
                    {
                        if (e.costo.Get(rr) - e.pagado.Get(rr) <= 0 || P.r.Get(rr) < 1) continue;
                        int i = idx; string res = rr;
                        var b = U.Btn(botones.transform, "pagar", () => P.PagarLugar(i, res), true,
                                      Paleta.De(rr), Paleta.BordeDe(rr), 10, 22);
                        U.Ancho(b.gameObject, 58);
                    }
                }
            }
        }

        void PintarDados()
        {
            U.Limpiar(dados);
            bool muestra = P.fase == Fase.Gasto || P.fase == Fase.Dobles;
            Dado(muestra ? P.d1.ToString() : "?", 1f);
            Dado(muestra ? P.d2.ToString() : "?", 1f);
            if (P.d3 > 0 && muestra) Dado(P.d3.ToString(), 0.35f);

            dtot.text = P.fase switch
            {
                Fase.Dobles => $"<b>{P.d1 + P.d2}</b>  dobles · ¿los tomás o tirás de nuevo?",
                Fase.Gasto  => $"<b>{P.d1 + P.d2}</b>  producción de este turno",
                Fase.Prep   => "<b>—</b>  todavía no arrancó la aventura",
                _           => "<b>—</b>  tirá para que tu party produzca",
            };
        }

        void Dado(string cara, float alfa)
        {
            var go = U.Nodo("dd", dados);
            U.Fondo(go, new Color(Paleta.Caja2.r, Paleta.Caja2.g, Paleta.Caja2.b, alfa));
            U.Borde(go, Paleta.Borde2);
            U.Ancho(go, 50);      // Ancho() fija flexibleWidth en 0: sin esto el dado se estira
            U.Alto(go, 50);
            var col = U.Col(go, 0, 0);
            col.childAlignment = TextAnchor.MiddleCenter;
            var t = U.Txt(go.transform, cara, 26, Paleta.Tinta, TextAnchor.MiddleCenter);
            t.color = new Color(t.color.r, t.color.g, t.color.b, alfa);
        }

        void PintarPendientes()
        {
            U.Limpiar(pend);
            if (P.fase != Fase.Gasto || P.pendientes.Count == 0) return;
            var caja = U.Nodo("pend", pend);
            U.Fondo(caja, Paleta.PendFondo);
            U.Borde(caja, Paleta.PendBorde);
            U.Col(caja, 8, 4);
            U.Micro(caja.transform, "Al terminar el turno", Paleta.PendNombre);
            foreach (var p in P.pendientes)
            {
                var fila = U.Nodo("pl", caja.transform);
                var f = U.Fila(fila, 0, 6);
                f.childForceExpandWidth = false;
                U.Alto(fila, 16);
                U.Txt(fila.transform, p.de, 11, Paleta.PendNombre);
                U.Empuje(fila.transform);
                U.Txt(fila.transform, TxtEfecto(p.ef), 11, Paleta.PendEfecto, TextAnchor.MiddleRight);
            }
        }

        void PintarRecursos()
        {
            U.Limpiar(rec);
            rotRec.text = (P.fase == Fase.Gasto ? "Lo que produjo tu party" : "Todavía no produjiste nada").ToUpperInvariant();
            foreach (var rr in Produccion.Todos)
            {
                int n = P.r.Get(rr);
                var caja = U.Nodo("rp", rec);
                U.Fondo(caja, Paleta.Caja2);
                U.Borde(caja, Paleta.BordeDe(rr));
                U.Elem(caja).flexibleWidth = 1;
                U.Alto(caja, 42);
                var col = U.Col(caja, 5, 2);
                col.childAlignment = TextAnchor.MiddleCenter;
                var fila = U.Nodo("pips", caja.transform);
                var f = U.Fila(fila, 0, 3);
                f.childAlignment = TextAnchor.MiddleCenter;
                f.childForceExpandWidth = false;
                if (n == 0) U.Pip(fila.transform, Paleta.De(rr), 16, 0.22f);
                else for (int i = 0; i < Mathf.Min(n, 8); i++) U.Pip(fila.transform, Paleta.De(rr), 16);
                U.Micro(caja.transform, rr, null, TextAnchor.MiddleCenter);
            }
        }

        void PintarCristales()
        {
            U.Limpiar(crisbox);
            var caja = U.Nodo("cris", crisbox);
            U.Fondo(caja, Paleta.Caja2);
            U.Borde(caja, Paleta.RazaBorde);
            var f = U.Fila(caja, 7, 4);
            f.childForceExpandWidth = false;
            U.Alto(caja, 28);
            U.Micro(caja.transform, "cristales · +2 por turno · se acumulan", Paleta.Cristal);
            U.Empuje(caja.transform);
            if (P.cris == 0) U.Pip(caja.transform, Paleta.Cristal, 11, 0.28f);
            else for (int i = 0; i < Mathf.Min(P.cris, 12); i++) U.Pip(caja.transform, Paleta.Cristal, 11);
        }

        void PintarFase()
        {
            rotFase.text = (P.fase switch
            {
                Fase.Prep   => "Preparación",
                Fase.Dobles => "Dobles",
                Fase.Tirar  => "Tirada de producción",
                Fase.Gasto  => "Gastar lo que salió",
                Fase.Interludio => "Al otro lado",
                _           => "Se terminó",
            }).ToUpperInvariant();

            if (P.Terminada) pistaFase.text = "";
            else if (P.eligiendo >= 0)
                pistaFase.text = $"Elegí a quién le ponés {P.mano[P.eligiendo].nombre}. " +
                                 "Dos por héroe, y si cae se pierde con él.";
            else pistaFase.text = P.fase switch
            {
                Fase.Prep => "Traés tres cristales y cinco cartas. Invocá a quien te sirva contra lo que ves. " +
                             "Todavía no se roba ni se tira.",
                Fase.Tirar => "Tirá los dados: cada carta de la mesa produce según dónde caiga el número.",
                _ => "Escudá a quien va a recibir el golpe: cada escudo frena 1. " +
                     "Cambiar la fila de orden cuesta 1 de Destreza por lugar.",
            };
        }

        void PintarAcciones()
        {
            U.Limpiar(acc);
            if (P.Terminada) return;
            switch (P.fase)
            {
                case Fase.Prep:
                    Principal("Empezar la aventura", () => P.Empezar());
                    break;
                case Fase.Tirar:
                    Principal("Tirar los dados", () => P.Tirar());
                    break;
                case Fase.Dobles:
                    Principal("Me quedo con esto", () => P.AceptarDobles());
                    Principal("Volver a tirar", () => P.Retirar());
                    break;
                case Fase.Gasto:
                    Principal("Terminar el turno", () => P.Terminar());
                    break;
            }
        }

        void Principal(string etiqueta, System.Action alTocar)
        {
            var b = U.Btn(acc, etiqueta, alTocar, true, Paleta.Oro, Paleta.Oro, 13, 38);
            U.Elem(b.gameObject).flexibleWidth = 1;
        }

        void PintarParty(int v)
        {
            U.Limpiar(party);
            npart.text = P.party.Count.ToString();
            if (P.party.Count == 0)
            {
                U.Txt(party, "Nadie en pie. Si terminás el turno así, se acabó.",
                      11, Paleta.Tenue, TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
                return;
            }

            bool eligiendo = P.eligiendo >= 0;
            Carta cartaElegida = eligiendo ? P.mano[P.eligiendo] : null;

            for (int i = 0; i < P.party.Count; i++)
            {
                var h = P.party[i];
                int idx = i;
                bool admite = eligiendo && h.Admite(cartaElegida, P.Slots);

                var fila = U.Nodo("heroe", party);
                var ff = U.Fila(fila, 0, 7);
                ff.childForceExpandHeight = false;
                ff.childAlignment = TextAnchor.UpperLeft;

                // rail izquierdo: el número de posición
                var pos = U.Nodo("pos", fila.transform);
                U.Fondo(pos, i == 0 ? Paleta.PendFondo : Paleta.Caja2);
                U.Borde(pos, i == 0 ? Paleta.FrenteBorde : Paleta.Borde);
                U.Ancho(pos, 22);
                U.Alto(pos, 30);
                var cp = U.Col(pos, 0, 0);
                cp.childAlignment = TextAnchor.MiddleCenter;
                U.Txt(pos.transform, (i + 1).ToString(), 16,
                      i == 0 ? Paleta.Vigor : Paleta.Tinta, TextAnchor.MiddleCenter);

                // cuerpo
                var cuerpo = U.Nodo("hcuerpo", fila.transform);
                U.Col(cuerpo, 0, 0);
                U.Elem(cuerpo).flexibleWidth = 1;

                var ficha = Ficha(cuerpo.transform, "mio", h.Nombre,
                                  P.NombreDeRaza(h.Raza), h.carta.texto, Despierta(h, v));
                foreach (var b in h.carta.bandas ?? new List<Banda>()) BandaPropia(ficha.transform, b, v);

                if (eligiendo)
                {
                    if (admite)
                    {
                        U.Borde(ficha, Paleta.Oro);
                        var btn = ficha.AddComponent<Button>();
                        btn.targetGraphic = ficha.GetComponent<Image>();
                        btn.onClick.AddListener(() => P.AplicarA(idx));
                    }
                    else
                    {
                        var g = U.Comp<CanvasGroup>(ficha);
                        g.alpha = 0.4f;
                    }
                }

                for (int s = 0; s < h.SlotsLibres(P.Slots); s++)
                {
                    var slot = U.Nodo("slotvacio", cuerpo.transform);
                    U.Borde(slot, Paleta.Borde);
                    U.Alto(slot, 18);
                    var cs = U.Col(slot, 3, 0);
                    cs.childAlignment = TextAnchor.MiddleCenter;
                    U.Txt(slot.transform, "slot libre", 10, Paleta.SlotVacio, TextAnchor.MiddleCenter);
                }

                foreach (var m in h.mejoras)
                {
                    var mej = U.Nodo("mej", cuerpo.transform);
                    U.Fondo(mej, Paleta.MejFondo);
                    U.Borde(mej, Paleta.MejBorde);
                    U.Col(mej, 5, 2);
                    U.Txt(mej.transform, m.nombre, 11, Paleta.MejNombre);
                    foreach (var b in m.bandas ?? new List<Banda>()) BandaPropia(mej.transform, b, v, chico: true);
                }

                U.Barra(cuerpo.transform, h.VidaBase == 0 ? 0 : (float)h.vida / h.VidaBase,
                        Paleta.Vigor, Paleta.PistaVida);

                var pie = U.Nodo("vidatxt", cuerpo.transform);
                U.Fondo(pie, Paleta.Caja);
                U.Borde(pie, Paleta.Borde);
                var fp = U.Fila(pie, 5, 4);
                fp.childForceExpandWidth = false;
                U.Alto(pie, 22);
                if (h.escudo > 0)
                    for (int k = 0; k < h.escudo; k++) U.Pip(pie.transform, Paleta.Temple, 9);
                else if (i == 0)
                    U.Txt(pie.transform, "recibe el daño", 10, Paleta.Vigor);
                U.Empuje(pie.transform);
                U.Pip(pie.transform, Paleta.Vida, 9);
                U.Txt(pie.transform, $"{h.vida} de {h.VidaMax}", 10, Paleta.Tenue);
                if (P.fase == Fase.Gasto && !P.Terminada)
                {
                    var b = U.Btn(pie.transform, "+", () => P.Escudar(idx), P.r.temple > 0,
                                  Paleta.Temple, Paleta.BordeDe("temple"), 10, 16);
                    U.Ancho(b.gameObject, 22);
                }

                // rail derecho: reordenar
                var mueve = U.Nodo("mueve", fila.transform);
                U.Col(mueve, 0, 2);
                U.Ancho(mueve, 26);
                U.Btn(mueve.transform, "↑", () => P.Mover(idx, -1), P.PuedeMover && i > 0, null, null, 11, 18);
                if (P.fase == Fase.Gasto) U.Pip(mueve.transform, Paleta.Destreza, 8);
                U.Btn(mueve.transform, "↓", () => P.Mover(idx, +1),
                      P.PuedeMover && i < P.party.Count - 1, null, null, 11, 18);
            }
        }

        bool Despierta(Heroe h, int v) => h.TodasLasBandas.Any(b => b.Cubre(v));

        void PintarAmuletos()
        {
            U.Limpiar(amuletos);
            namul.text = $"{P.amuletos.Count} de {P.data.reglas.amuletosMax}";
            if (P.amuletos.Count == 0)
            {
                U.Txt(amuletos, "Ninguno. No producen nada, pero cambian las reglas.",
                      11, Paleta.Tenue, TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
                return;
            }
            foreach (var a in P.amuletos)
                Ficha(amuletos, "amuleto", a.nombre, null, a.efectoTexto, false);
        }

        void PintarCementerio()
        {
            U.Limpiar(cementerio);
            ncem.text = P.cementerio.Count > 0 ? $"{P.cementerio.Count} CAÍDOS" : "";
            if (P.cementerio.Count == 0)
            {
                U.Txt(cementerio, "Nadie todavía.", 11, Paleta.Tenue, TextAnchor.MiddleLeft, FontStyle.Italic);
                return;
            }
            foreach (var c in P.cementerio)
            {
                var chip = U.Nodo("lapida", cementerio);
                U.Fondo(chip, Paleta.LapidaFondo);
                U.Borde(chip, Paleta.LapidaBorde);
                var f = U.Col(chip, 4, 0);
                f.childAlignment = TextAnchor.MiddleCenter;
                U.Txt(chip.transform, c.nombre, 10, Paleta.Lapida, TextAnchor.MiddleCenter);
            }
        }

        void PintarEventos()
        {
            U.Limpiar(eventos);
            rotEv.text = P.efimeros.Count > 0 ? "SE RESUELVEN ESTE TURNO" : "";
            if (P.efimeros.Count == 0)
            {
                U.Txt(eventos, "Nada inmediato delante tuyo.", 11, Paleta.Tenue,
                      TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
                return;
            }

            for (int i = 0; i < P.efimeros.Count; i++)
            {
                var e = P.efimeros[i];
                int idx = i;
                var ev = U.Nodo("ev", eventos);
                U.Fondo(ev, Paleta.Caja);
                U.Borde(ev, e.bueno ? Paleta.Oro : Paleta.Rojo);
                U.Col(ev, 10, 5);

                U.Txt(ev.transform, e.nombre, 14, Paleta.Tinta, TextAnchor.UpperLeft, wrap: true);
                var fs = U.Txt(ev.transform, e.texto, 11, Paleta.Tenue,
                               TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
                U.Elem(fs.gameObject).flexibleHeight = 1;

                U.Txt(ev.transform,
                      e.bueno ? "si lo dejás pasar, no perdés nada" : TxtEfecto(e.efecto),
                      10, e.bueno ? Paleta.Tenue : Paleta.Rojo, TextAnchor.UpperLeft, wrap: true);

                for (int j = 0; j < e.vias.Count; j++)
                {
                    var via = e.vias[j];
                    int jv = j;
                    bool puede = P.fase == Fase.Gasto && !P.Terminada && P.Puede(via.costo);
                    bool cruzado = puede && !P.Exacto(via.costo);

                    // texto a la izquierda y costo a la derecha, como el .via del CSS
                    var b = U.BtnCrudo(ev.transform, () => P.Resolver(idx, jv), puede);
                    U.Alto(b, 24);
                    var f = U.Fila(b, 6, 5);
                    f.childForceExpandWidth = false;
                    f.childAlignment = TextAnchor.MiddleLeft;
                    var vt = U.Txt(b.transform, via.texto + (cruzado ? "  ×2" : ""), 11,
                                   puede ? Paleta.Tinta : new Color(1, 1, 1, 0.32f));
                    U.Elem(vt.gameObject).flexibleWidth = 1;
                    var costo = U.Nodo("cst", b.transform);
                    var fcst = U.Fila(costo, 0, 2);
                    fcst.childForceExpandWidth = false;
                    foreach (var rr in Produccion.Todos)
                        for (int k = 0; k < via.costo.Get(rr); k++)
                            U.Pip(costo.transform, Paleta.De(rr), 9, cruzado ? 0.45f : 1f);
                }
            }
        }

        void PintarMano()
        {
            U.Limpiar(mano);
            for (int i = 0; i < P.mano.Count; i++)
            {
                var c = P.mano[i];
                int idx = i;
                var cd = U.Nodo("cd", mano);
                U.Fondo(cd, Paleta.Caja);
                U.Borde(cd, P.eligiendo == i ? Paleta.Oro : Paleta.Borde);
                U.Col(cd, 10, 5);

                Franja(cd.transform, Paleta.Franja(c.tipo));

                U.Micro(cd.transform, EtiquetaDeTipo(c));
                U.Txt(cd.transform, c.nombre, 14, Paleta.Tinta, TextAnchor.UpperLeft, wrap: true);

                var cuerpo = U.Nodo("cx", cd.transform);
                U.Col(cuerpo, 0, 3);
                U.Elem(cuerpo).flexibleHeight = 1;
                if (c.tipo == "perm" || c.tipo == "mejora")
                    foreach (var b in c.bandas ?? new List<Banda>()) BandaPropia(cuerpo.transform, b, -1);
                else
                    U.Txt(cuerpo.transform, c.efectoTexto ?? c.texto, 11, Paleta.TextoCuerpo,
                          TextAnchor.UpperLeft, wrap: true);

                string etiqueta = c.tipo switch
                {
                    "amuleto" => "Colgar",
                    "mejora"  => P.eligiendo == i ? "Elegí un héroe…" : "Equipar",
                    _         => "Invocar",
                };
                var pie = U.Nodo("pie", cd.transform);
                var fp = U.Fila(pie, 0, 4);
                fp.childForceExpandWidth = false;
                U.Alto(pie, 26);
                var b2 = U.Btn(pie.transform, etiqueta, () => P.Jugar(idx), P.SePuedeJugar(c), null, null, 12, 26);
                U.Elem(b2.gameObject).flexibleWidth = 1;
                for (int k = 0; k < P.CostoEf(c); k++) U.Pip(pie.transform, Paleta.Cristal, 10);
            }
        }

        void PintarLog()
        {
            U.Limpiar(log);
            foreach (var linea in bitacora.Take(12))
                U.Txt(log, linea, 12, Paleta.TextoLog, TextAnchor.UpperLeft, wrap: true);
        }

        void PintarFinal()
        {
            velo.SetActive(P.Terminada);
            if (!P.Terminada) return;
            U.Limpiar(modal);
            U.Txt(modal, P.gano ? "Plantaste la semilla" : "Ahí terminó el viaje",
                  26, Paleta.Tinta, TextAnchor.MiddleCenter, wrap: true);
            U.Txt(modal,
                  P.gano
                    ? $"Cruzaste {string.Join(", ", P.ruta)} y llegaste con {P.party.Count} en pie."
                    : (P.motivoFin ?? "No quedó nadie en pie.") + $" Turno {P.turno}.",
                  15, Paleta.TextoModal, TextAnchor.MiddleCenter, FontStyle.Italic, wrap: true);
            U.Btn(modal, "Otra vez", Reiniciar, true, Paleta.Noche, Paleta.Oro, 13, 38, solido: true);
        }

        // ══ el interludio ═════════════════════════════════════════════════════

        void PintarInterludio()
        {
            var I = P.interludio;
            velo.SetActive(true);
            U.Limpiar(modal);

            U.Txt(modal, "Al otro lado", 26, Paleta.Tinta, TextAnchor.MiddleCenter, wrap: true);
            U.Txt(modal, $"Cruzaste <b>{P.terreno.nombre}</b>. Descansan, se curan, " +
                         "y los caídos vuelven al mazo.",
                  14, Paleta.TextoModal, TextAnchor.MiddleCenter, FontStyle.Italic, wrap: true);

            Seccion("El cofre · llevate una");
            var cofre = U.Nodo("cofre", modal);
            U.Grilla(cofre, new Vector2(268, 150), 8);
            U.Alto(cofre, 150);
            for (int i = 0; i < I.cofre.Count; i++)
            {
                int idx = i;
                TarjetaDeCofre(cofre.transform, I.cofre[i], !I.tomada, () => P.TomarDelCofre(idx));
            }

            Seccion(I.sacada ? "Ya dejaste una atrás" : "Dejá una carta atrás · opcional");
            var lista = U.Nodo("lista", modal);
            U.Grilla(lista, new Vector2(196, 22), 4);
            var porNombre = P.mazo.GroupBy(c => c.nombre).OrderBy(g => g.Key).ToList();
            U.Alto(lista, Mathf.Ceil(porNombre.Count / 4f) * 26);
            foreach (var g in porNombre)
            {
                string nombre = g.Key;
                var b = U.Btn(lista.transform, $"{nombre}  ×{g.Count()}",
                              () => P.SacarDelMazo(nombre), !I.sacada, null, null, 10, 22);
            }

            if (P.party.Count > 5)
            {
                var aviso = U.Nodo("aviso5", modal);
                U.Fondo(aviso, Paleta.PendFondo);
                U.Borde(aviso, Paleta.PendBorde);
                U.Col(aviso, 8, 2);
                U.Txt(aviso.transform,
                      $"Siguen viaje sólo los <b>primeros cinco</b> de la fila. " +
                      $"Van: {string.Join(", ", P.party.Take(5).Select(h => h.Nombre))}. " +
                      $"Se quedan: {string.Join(", ", P.party.Skip(5).Select(h => h.Nombre))}.",
                      12, Paleta.PendEfecto, TextAnchor.UpperLeft, wrap: true);
            }

            Seccion("¿Hacia dónde? · no se vuelve");
            var rutas = U.Nodo("rutas", modal);
            U.Grilla(rutas, new Vector2(410, 170), 8);
            U.Alto(rutas, 170);
            foreach (var t in I.rutas) TarjetaDeRuta(rutas.transform, t);
        }

        void Seccion(string titulo)
        {
            var go = U.Nodo("rot", modal);
            var col = U.Col(go, 0, 0);
            col.childAlignment = TextAnchor.MiddleCenter;
            U.Alto(go, 20);
            U.Micro(go.transform, titulo, null, TextAnchor.MiddleCenter);
        }

        void TarjetaDeCofre(Transform padre, Carta c, bool clickeable, System.Action alTocar)
        {
            var go = U.Nodo("opt", padre);
            U.Fondo(go, Paleta.Caja);
            U.Borde(go, clickeable ? Paleta.Borde2 : Paleta.Borde);
            U.Col(go, 10, 4);
            Franja(go.transform, Paleta.Franja(c.tipo));
            if (clickeable)
            {
                var b = go.AddComponent<Button>();
                b.targetGraphic = go.GetComponent<Image>();
                b.onClick.AddListener(() => alTocar());
            }
            else U.Comp<CanvasGroup>(go).alpha = 0.35f;

            U.Micro(go.transform, EtiquetaDeTipo(c));
            U.Txt(go.transform, c.nombre, 14, Paleta.Tinta, TextAnchor.UpperLeft, wrap: true);

            var cuerpo = U.Nodo("cx", go.transform);
            U.Col(cuerpo, 0, 3);
            U.Elem(cuerpo).flexibleHeight = 1;
            if (c.tipo == "perm" || c.tipo == "mejora")
                foreach (var b in c.bandas ?? new List<Banda>()) BandaPropia(cuerpo.transform, b, -1);
            else
                U.Txt(cuerpo.transform, c.efectoTexto ?? c.texto, 11, Paleta.TextoCuerpo,
                      TextAnchor.UpperLeft, wrap: true);

            var pie = U.Nodo("cq", go.transform);
            var fp = U.Fila(pie, 0, 3);
            fp.childForceExpandWidth = false;
            U.Alto(pie, 14);
            for (int k = 0; k < c.costoCristales; k++) U.Pip(pie.transform, Paleta.Cristal, 10);
        }

        void TarjetaDeRuta(Transform padre, Terreno t)
        {
            var go = U.Nodo("ruta", padre);
            U.Fondo(go, Paleta.Caja);
            U.Borde(go, Paleta.Oro);
            U.Col(go, 12, 5);
            var b = go.AddComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();
            b.onClick.AddListener(() => P.PartirHacia(t.id));

            U.Txt(go.transform, t.nombre, 16, Paleta.Tinta, TextAnchor.UpperLeft, wrap: true);
            var intro = U.Txt(go.transform, t.intro, 11, Paleta.Tenue,
                              TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
            U.Elem(intro.gameObject).flexibleHeight = 1;
            U.Txt(go.transform, t.problema, 11, Paleta.Rojo, TextAnchor.UpperLeft, wrap: true);
            U.Txt(go.transform, string.Join(" · ", t.apariciones.Select(a => a.n)),
                  10, Paleta.SlotVacio, TextAnchor.UpperLeft, wrap: true);
        }

        string EtiquetaDeTipo(Carta c) => c.tipo switch
        {
            "perm"    => (string.IsNullOrEmpty(c.raza) ? "" : P.NombreDeRaza(c.raza) + " · ") + $"{c.vida} de vida",
            "amuleto" => "amuleto · pasiva",
            "mejora"  => string.IsNullOrEmpty(c.soloRaza)
                         ? "mejora · va sobre un héroe"
                         : $"mejora · sólo {P.NombreDeRaza(c.soloRaza)}",
            _         => "un solo uso",
        };

        void Reiniciar()
        {
            bitacora.Clear();
            P.AlLoguear -= Anotar;
            P.Cambio -= Pintar;
            P = new Partida(MerliotDatabase.I.Data);
            P.AlLoguear += Anotar;
            P.Cambio += Pintar;
            P.Iniciar();
        }

        // ══ piezas compartidas ════════════════════════════════════════════════

        /// La .ficha del CSS: franja de color a la izquierda, nombre, chip de raza y flavor.
        GameObject Ficha(Transform padre, string clase, string nombre, string raza, string flavor, bool despierta)
        {
            var go = U.Nodo("ficha", padre);
            U.Fondo(go, despierta ? Paleta.Despierta : Paleta.Caja);
            U.Borde(go, despierta ? Paleta.Borde2 : Paleta.Borde);
            U.Col(go, 9, 4);
            Franja(go.transform, Paleta.Franja(clase));

            var enc = U.Nodo("enc", go.transform);
            var f = U.Fila(enc, 0, 6);
            f.childForceExpandWidth = false;
            U.Alto(enc, 20);
            U.Txt(enc.transform, nombre, 14, Paleta.Tinta);
            if (!string.IsNullOrEmpty(raza))
            {
                U.Empuje(enc.transform);
                var chip = U.Nodo("raza", enc.transform);
                U.Borde(chip, Paleta.RazaBorde);
                var cc = U.Col(chip, 3, 0);
                cc.childAlignment = TextAnchor.MiddleCenter;
                U.Micro(chip.transform, raza, Paleta.Cristal, TextAnchor.MiddleCenter);
            }
            if (!string.IsNullOrEmpty(flavor))
                U.Txt(go.transform, flavor, 11, Paleta.Tenue, TextAnchor.UpperLeft, FontStyle.Italic, wrap: true);
            return go;
        }

        /// La franja de 2px del borde izquierdo que dice de qué es la carta.
        void Franja(Transform padre, Color c)
        {
            var go = U.Nodo("franja", padre);
            U.Fondo(go, c);
            U.Alto(go, 2);
        }

        /// Una banda del jugador: chip con el rango + los pips de lo que produce.
        void BandaPropia(Transform padre, Banda b, int v, bool chico = false)
        {
            bool on = v >= 2 && b.Cubre(v);
            var fila = ChipBanda(padre, Rango(b.desde, b.hasta), on, mala: false, chico);
            if (b.produce != null && b.produce.Total > 0)
            {
                var pips = U.Pips(fila.transform, b.produce, chico ? 9 : 11);
                if (!string.IsNullOrEmpty(b.porCadaRaza))
                    U.Txt(fila.transform, $"por cada {P.NombreDeRaza(b.porCadaRaza)}", 10, Paleta.Tenue);
            }
            else if (b.cristales > 0)
            {
                for (int i = 0; i < b.cristales; i++) U.Pip(fila.transform, Paleta.Cristal, chico ? 9 : 11);
            }
            else if (b.cura > 0)
            {
                for (int i = 0; i < b.cura; i++) U.Pip(fila.transform, Paleta.Vida, chico ? 9 : 11);
            }
        }

        void BandaEnemiga(Transform padre, BandaEnemiga b, int v)
        {
            bool on = v >= 2 && v >= b.desde && v <= b.hasta;
            var fila = ChipBanda(padre, Rango(b.desde, b.hasta), on, mala: true, chico: false);
            U.Txt(fila.transform, TxtEfecto(b.efecto), 11, Paleta.TextoBanda, TextAnchor.MiddleLeft, wrap: true);
        }

        GameObject ChipBanda(Transform padre, string rango, bool on, bool mala, bool chico)
        {
            var fila = U.Nodo("banda", padre);
            U.Fondo(fila, on ? (mala ? Paleta.BandaMala : Paleta.BandaOn) : new Color(0, 0, 0, 0));
            if (on) U.Borde(fila, mala ? Paleta.BandaMalaBor : Paleta.BandaOnBorde);
            var f = U.Fila(fila, 3, 6);
            f.childForceExpandWidth = false;
            U.Alto(fila, chico ? 16 : 20);

            var chip = U.Nodo("bnum", fila.transform);
            U.Fondo(chip, Paleta.Caja2);
            U.Borde(chip, Paleta.Borde);
            U.Ancho(chip, chico ? 30 : 36);
            var cc = U.Col(chip, 2, 0);
            cc.childAlignment = TextAnchor.MiddleCenter;
            U.Txt(chip.transform, rango, chico ? 11 : 12,
                  on ? Paleta.Oro : Paleta.Tinta, TextAnchor.MiddleCenter);
            return fila;
        }

        static string Rango(int desde, int hasta) => desde == hasta ? desde.ToString() : $"{desde}–{hasta}";

        /// El mismo texto que txtEfecto() del prototipo, armado desde los datos.
        public static string TxtEfecto(Efecto ef)
        {
            if (ef == null) return "";
            var d = new List<string>();
            if (ef.dmg > 0)       d.Add($"{ef.dmg} al de adelante");
            if (ef.dmgTodos > 0)  d.Add($"{ef.dmgTodos} a cada uno");
            if (ef.dmgFondo > 0)  d.Add($"{ef.dmgFondo} al último");
            if (ef.dmgDado > 0)   d.Add("igual al dado menor");
            if (ef.rotar > 0)     d.Add("el de adelante pasa al fondo");
            if (ef.descartar > 0) d.Add("perdés una carta");
            if (ef.brota > 0)     d.Add("larga un efímero");
            if (ef.cura > 0)      d.Add($"se recompone {ef.cura}");
            return string.Join(" · ", d);
        }
    }
}
