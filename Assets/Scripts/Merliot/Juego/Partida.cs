using System;
using System.Collections.Generic;
using System.Linq;

namespace Merliot
{
    public enum Fase { Prep, Tirar, Dobles, Gasto, Interludio, Fin }

    /// Un héroe bajado a la fila. La carta es la definición del JSON y no se toca;
    /// acá vive lo que cambia durante la partida.
    public class Heroe
    {
        public Carta carta;
        public int vida;
        public int escudo;
        public readonly List<Carta> mejoras = new List<Carta>();

        public string Nombre => carta.nombre;
        public string Raza => carta.raza;
        public int VidaBase => carta.vida;
        public int VidaMax => carta.vida + mejoras.Sum(m => m.vida);
        public int SlotsLibres(int max) => max - mejoras.Count;
        public bool Admite(Carta c, int slots) =>
            SlotsLibres(slots) > 0 && (string.IsNullOrEmpty(c.soloRaza) || Raza == c.soloRaza);

        /// Las bandas del héroe más las de lo que lleva encima.
        public IEnumerable<Banda> TodasLasBandas =>
            (carta.bandas ?? new List<Banda>()).Concat(mejoras.SelectMany(m => m.bandas ?? new List<Banda>()));
    }

    /// Criaturas y lugares comparten fila en el terreno pero se resuelven distinto:
    /// a la criatura se le baja la vida con Vigor, el lugar se desarma pagando.
    public class Enemigo
    {
        public string nombre, texto;
        public bool esCriatura;
        public List<BandaEnemiga> bandas;
        public int vida, vidaMax;
        public Produccion costo, pagado;

        public bool Desarmado => Produccion.Todos.All(r => pagado.Get(r) >= costo.Get(r));
    }

    public class Pendiente { public string de; public Efecto ef; public Enemigo quien; }

    /// Lo que pasa entre dos terrenos: descansás, abrís el cofre, podés dejar
    /// una carta atrás y elegís por dónde seguir. No se vuelve.
    public class Interludio
    {
        public List<Carta> cofre = new List<Carta>();
        public bool tomada, sacada;
        public List<Terreno> rutas = new List<Terreno>();
    }

    /// El juego entero, sin una sola referencia a Unity.
    /// Se puede correr en un test, y con la misma semilla da la misma partida.
    public class Partida
    {
        public readonly MerliotData data;
        public Terreno terreno;
        readonly Random rnd;

        public event Action<string> AlLoguear;
        public event Action Cambio;

        public Fase fase = Fase.Prep;
        public int turno, cris, d1, d2, d3, resueltos;
        public int etapa = 1;
        public bool tresdados, yaRetiro, gano, semilla;
        public string motivoFin;
        public readonly List<string> ruta = new List<string>();
        public Interludio interludio;

        public readonly List<Carta> mazo = new List<Carta>();
        public readonly List<Carta> mano = new List<Carta>();
        public readonly List<Carta> descarte = new List<Carta>();
        public readonly List<Carta> cementerio = new List<Carta>();
        public readonly List<Carta> amuletos = new List<Carta>();
        public readonly List<Heroe> party = new List<Heroe>();
        public readonly List<Efimero> efMazo = new List<Efimero>();
        public readonly List<Efimero> efimeros = new List<Efimero>();
        public readonly List<Enemigo> enemigos = new List<Enemigo>();
        public readonly List<Pendiente> pendientes = new List<Pendiente>();
        public readonly Produccion r = new Produccion();

        /// Índice en la mano de la mejora que está esperando dueño, y el héroe elegido.
        public int eligiendo = -1, objetivo = -1;

        public bool Terminada => fase == Fase.Fin;
        public int Slots => data.reglas.slotsPorHeroe;
        public int TiradaActual => fase == Fase.Gasto ? d1 + d2 : -1;
        public int UltimoTurnoDeAparicion => terreno.apariciones.Max(a => a.turno);

        public Partida(MerliotData data, int semillaAzar = 0)
        {
            this.data = data;
            terreno = data.terrenos[0];
            rnd = semillaAzar == 0 ? new Random() : new Random(semillaAzar);
        }

        /// Las cartas que se reparten. La semilla y las mutaciones están en el
        /// catálogo con copias 0: entran por la campaña, no por el mazo de salida.
        public IEnumerable<Carta> Repartibles => data.cartas.Where(c => c.copias > 0);
        public Carta LaSemilla => data.cartas.FirstOrDefault(c => c.id == "la-semilla");
        public Carta LaMutacion => data.cartas.FirstOrDefault(c => c.id == "mutaci-n");

        int R(int n) => rnd.Next(n);
        void Log(string s) => AlLoguear?.Invoke(s);
        void Pintar() => Cambio?.Invoke();

        void Barajar<T>(List<T> a)
        {
            for (int i = a.Count - 1; i > 0; i--) { int j = R(i + 1); (a[i], a[j]) = (a[j], a[i]); }
        }

        // ── arranque ──────────────────────────────────────────────────────────

        public void Iniciar()
        {
            foreach (var c in data.cartas)
                for (int i = 0; i < c.copias; i++) mazo.Add(c);
            Barajar(mazo);

            efMazo.AddRange(MazoEfimeros(1));

            cris = data.reglas.cristalesIniciales;
            Robar(data.reglas.manoInicial);

            // La mano de salida siempre tiene que traer con quién armar una fila:
            // si no, el primer turno es mirar cartas que no podés bajar.
            int intentos = 0;
            while (mano.Count(c => c.tipo == "perm" && c.costoCristales <= 2) < 2 && intentos++ < 14)
            {
                int j = mazo.FindIndex(c => c.tipo == "perm" && c.costoCristales <= 2);
                int k = mano.FindIndex(c => c.tipo != "perm");
                if (j < 0 || k < 0) break;
                mazo.Add(mano[k]); mano.RemoveAt(k);
                int jj = mazo.FindIndex(c => c.tipo == "perm" && c.costoCristales <= 2);
                mano.Add(mazo[jj]); mazo.RemoveAt(jj);
            }
            Barajar(mazo);

            Log($"<b>{terreno.nombre}.</b> {terreno.intro}");
            Log("Revelás el terreno. Por ahora está quieto. <b>Salís solo: elegí con quién enfrentarlo.</b>");
            Log("<b>Antes de empezar, acomodá tu party con lo que traés de casa.</b> Todavía no se roba ni se tira.");
            Pintar();
        }

        public void Robar(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (mazo.Count == 0)
                {
                    if (descarte.Count == 0) return;
                    mazo.AddRange(descarte); descarte.Clear(); Barajar(mazo);
                }
                mano.Add(mazo[mazo.Count - 1]);
                mazo.RemoveAt(mazo.Count - 1);
            }
        }

        /// El mazo de efímeros no crece: rota. Entran los del nivel de la etapa
        /// y los del anterior, y salen los viejos.
        public List<Efimero> MazoEfimeros(int nivel)
        {
            var m = new List<Efimero>();
            foreach (var e in data.efimeros.Where(e => e.nivel <= nivel && e.nivel >= nivel - 1))
            { m.Add(e); m.Add(e); }
            Barajar(m);
            return m;
        }

        Efimero AbrirEfimero()
        {
            if (efMazo.Count == 0) efMazo.AddRange(MazoEfimeros(etapa));
            var e = efMazo[efMazo.Count - 1];
            efMazo.RemoveAt(efMazo.Count - 1);
            efimeros.Add(e);
            Log($"Se presenta <b>{e.nombre}</b>. Tenés este turno para resolverlo.");
            return e;
        }

        void AbrirEnemigo(string q, string nombre)
        {
            if (enemigos.Any(x => x.nombre == nombre)) return;
            Enemigo e;
            if (q == "criatura")
            {
                var b = data.criaturas.FirstOrDefault(x => x.nombre == nombre);
                if (b == null) return;
                e = new Enemigo { nombre = b.nombre, texto = b.texto, esCriatura = true,
                                  bandas = b.bandas, vida = b.vida, vidaMax = b.vida };
                Log($"Aparece <b>{e.nombre}</b>.");
            }
            else
            {
                var b = data.lugares.FirstOrDefault(x => x.nombre == nombre);
                if (b == null) return;
                e = new Enemigo { nombre = b.nombre, texto = b.texto, esCriatura = false,
                                  bandas = b.bandas, costo = b.costo, pagado = new Produccion() };
                Log($"Encontrás <b>{e.nombre}</b>. No se mata: se desarma.");
            }
            enemigos.Add(e);
        }

        // ── economía: la regla del dos por uno ────────────────────────────────

        public bool Puede(Produccion costo)
        {
            int falta = 0, sobra = 0;
            foreach (var x in Produccion.Todos)
            {
                int n = costo?.Get(x) ?? 0;
                if (r.Get(x) >= n) sobra += r.Get(x) - n; else falta += n - r.Get(x);
            }
            return falta * 2 <= sobra;
        }

        /// Si podés pagarlo con el recurso justo o si tenés que convertir.
        public bool Exacto(Produccion costo) =>
            Produccion.Todos.All(x => (costo?.Get(x) ?? 0) == 0 || r.Get(x) >= costo.Get(x));

        void PagarCosto(Produccion costo)
        {
            int falta = 0;
            foreach (var x in Produccion.Todos)
            {
                int n = costo?.Get(x) ?? 0;
                int u = Math.Min(r.Get(x), n);
                r.Sumar(x, -u);
                falta += n - u;
            }
            for (int i = 0; i < falta; i++)
            {
                int q = 2;
                foreach (var x in Produccion.Todos)
                    while (q > 0 && r.Get(x) > 0) { r.Sumar(x, -1); q--; }
            }
        }

        public bool Tiene(string pasiva) => amuletos.Any(a => a.pasiva == pasiva);

        public int CostoEf(Carta c) =>
            Math.Max(1, c.costoCristales - (c.tipo == "uso" && Tiene("barato") ? 1 : 0));

        // ── daño ──────────────────────────────────────────────────────────────

        public int Dañar(int n)
        {
            var caidos = new List<string>();
            while (n > 0 && party.Count > 0)
            {
                var h = party[0];
                if (h.escudo > 0)
                {
                    int a = Math.Min(h.escudo, n);
                    h.escudo -= a; n -= a;
                    Log($"El escudo de <b>{h.Nombre}</b> para {a}.");
                    if (n <= 0) break;
                }
                int q = Math.Min(h.vida, n);
                h.vida -= q; n -= q;
                if (h.vida <= 0) { caidos.Add(h.Nombre); party.RemoveAt(0); Caer(h); }
            }
            if (caidos.Count > 0)
                Log($"<b>{string.Join(" y ", caidos)}</b> {(caidos.Count > 1 ? "caen" : "cae")}. " +
                    $"{(caidos.Count > 1 ? "Van" : "Va")} al cementerio.");
            return caidos.Count;
        }

        public void DañarTodos(int n)
        {
            var caidos = new List<string>();
            for (int i = party.Count - 1; i >= 0; i--)
            {
                var h = party[i];
                int q = n;
                if (h.escudo > 0) { int a = Math.Min(h.escudo, q); h.escudo -= a; q -= a; }
                h.vida -= q;
                if (h.vida <= 0) { caidos.Add(h.Nombre); party.RemoveAt(i); Caer(h, callado: true); }
            }
            if (caidos.Count > 0) Log($"Caen <b>{string.Join(", ", caidos)}</b>. Van al cementerio.");
        }

        public void DañarFondo(int n)
        {
            if (party.Count == 0) return;
            var h = party[party.Count - 1];
            int q = n;
            if (h.escudo > 0)
            {
                int a = Math.Min(h.escudo, q);
                h.escudo -= a; q -= a;
                Log($"El escudo de <b>{h.Nombre}</b> para {a}.");
            }
            h.vida -= q;
            if (h.vida <= 0)
            {
                party.RemoveAt(party.Count - 1);
                Log($"Por atrás cae <b>{h.Nombre}</b>. Va al cementerio.");
                Caer(h, callado: true);
            }
        }

        /// Al caer: el equipo se pierde al descarte, el héroe va al cementerio
        /// (que no es lo mismo que el descarte: de ahí sólo se vuelve con cartas puntuales).
        void Caer(Heroe h, bool callado = false)
        {
            if (h.mejoras.Count > 0)
            {
                descarte.AddRange(h.mejoras);
                if (!callado) Log($"Se pierden en el camino: {string.Join(", ", h.mejoras.Select(m => m.nombre))}.");
                h.mejoras.Clear();
            }
            cementerio.Add(h.carta);
            var al = h.carta.alCaer;
            if (al == null) return;
            if (al.cris > 0)
            {
                cris += al.cris;
                if (!callado) Log($"Al caer, <b>{h.Nombre}</b> deja {al.cris} de cristal.");
            }
            if (al.curaTodos > 0)
            {
                foreach (var x in party) x.vida = Math.Min(x.VidaMax, x.vida + al.curaTodos);
                if (!callado) Log($"Al apagarse, <b>{h.Nombre}</b> recompone a los que quedan.");
            }
        }

        void AplicarEfecto(Efecto ef, string de, Enemigo quien)
        {
            if (ef == null) return;
            var d = new List<string>();
            if (ef.dmg > 0) { Dañar(ef.dmg); d.Add($"{ef.dmg} al de adelante"); }
            if (ef.dmgTodos > 0) { DañarTodos(ef.dmgTodos); d.Add($"{ef.dmgTodos} a cada uno"); }
            if (ef.dmgFondo > 0) { DañarFondo(ef.dmgFondo); d.Add($"{ef.dmgFondo} al último"); }
            if (ef.dmgDado > 0)
            {
                int n = Math.Min(d1, d2) * ef.dmgDado;
                Dañar(n);
                d.Add($"{n} al de adelante, por el dado menor");
            }
            if (ef.rotar > 0 && party.Count > 1)
            {
                var p = party[0]; party.RemoveAt(0); party.Add(p);
                d.Add("el de adelante pasa al fondo");
            }
            if (ef.descartar > 0 && mano.Count > 0)
            {
                int i = R(mano.Count);
                var c = mano[i]; mano.RemoveAt(i); descarte.Add(c);
                d.Add($"perdés {c.nombre}");
            }
            if (ef.brota > 0) { AbrirEfimero(); d.Add("larga algo más"); }
            // Ojo: la curación del enemigo no tiene techo, igual que en el prototipo web.
            if (ef.cura > 0 && quien != null) { quien.vida += ef.cura; d.Add($"se recompone {ef.cura}"); }
            if (d.Count > 0) Log($"<b>{de}</b>: {string.Join(", ", d)}.");
        }

        /// Cura al que está peor, no al primero de la fila.
        public bool Curar(int n)
        {
            Heroe peor = null;
            foreach (var h in party)
                if (h.vida < h.VidaMax && (peor == null || h.vida - h.VidaMax < peor.vida - peor.VidaMax))
                    peor = h;
            if (peor == null) return false;
            int q = Math.Min(n, peor.VidaMax - peor.vida);
            peor.vida += q;
            Log($"<b>{peor.Nombre}</b> se recompone: +{q} de vida.");
            return true;
        }

        // ── acciones del jugador ──────────────────────────────────────────────

        public void Golpear(int idx)
        {
            if (fase != Fase.Gasto || Terminada) return;
            if (idx < 0 || idx >= enemigos.Count) return;
            var e = enemigos[idx];
            if (!e.esCriatura || r.vigor < 1) return;
            r.vigor--; e.vida--;
            if (e.vida <= 0) { enemigos.RemoveAt(idx); Log($"<b>{e.nombre}</b> cae."); RevisarVictoria(); }
            Pintar();
        }

        public void PagarLugar(int idx, string rec)
        {
            if (fase != Fase.Gasto || Terminada) return;
            if (idx < 0 || idx >= enemigos.Count) return;
            var e = enemigos[idx];
            if (e.esCriatura || r.Get(rec) < 1) return;
            if (e.costo.Get(rec) - e.pagado.Get(rec) <= 0) return;
            r.Sumar(rec, -1);
            e.pagado.Sumar(rec, 1);
            if (e.Desarmado) { enemigos.RemoveAt(idx); Log($"<b>{e.nombre}</b> queda desarmado."); RevisarVictoria(); }
            Pintar();
        }

        public void Escudar(int i)
        {
            if (fase != Fase.Gasto || Terminada) return;
            if (i < 0 || i >= party.Count || r.temple < 1) return;
            r.temple--; party[i].escudo++;
            Pintar();
        }

        public bool PuedeMover =>
            !Terminada && party.Count > 1 && (fase == Fase.Prep || (fase == Fase.Gasto && r.destreza > 0));

        public void Mover(int i, int dir)
        {
            if (fase != Fase.Gasto && fase != Fase.Prep) return;
            int j = i + dir;
            if (j < 0 || j >= party.Count) return;
            if (fase == Fase.Gasto)
            {
                if (r.destreza < 1) return;
                r.destreza--;
                Log($"<b>{party[i].Nombre}</b> cambia de lugar con <b>{party[j].Nombre}</b>.");
            }
            (party[i], party[j]) = (party[j], party[i]);
            Pintar();
        }

        public void ElegirDestino(int i)
        {
            if (Terminada || party.Count == 0) return;
            var c = mano[i];
            if (!party.Any(h => h.Admite(c, Slots)))
            {
                Log(!string.IsNullOrEmpty(c.soloRaza)
                    ? $"Ningún {NombreDeRaza(c.soloRaza)} tuyo puede llevarla."
                    : "Nadie tiene un slot libre. Son dos por héroe.");
                return;
            }
            eligiendo = eligiendo == i ? -1 : i;
            Pintar();
        }

        public void AplicarA(int h)
        {
            if (eligiendo < 0) return;
            var cm = mano[eligiendo];
            var hh = party[h];
            if (hh.SlotsLibres(Slots) <= 0) { Log($"<b>{hh.Nombre}</b> ya lleva dos cosas encima."); return; }
            if (!string.IsNullOrEmpty(cm.soloRaza) && hh.Raza != cm.soloRaza)
            {
                Log($"<b>{cm.nombre}</b> es sólo para los {NombreDeRaza(cm.soloRaza)}.");
                return;
            }
            objetivo = h;
            int i = eligiendo;
            eligiendo = -1;
            Jugar(i);
        }

        public string NombreDeRaza(string id) =>
            data.razas.FirstOrDefault(x => x.id == id)?.nombre ?? id;

        public bool SePuedeJugar(Carta c) =>
            (fase == Fase.Prep || fase == Fase.Gasto) && !Terminada && cris >= CostoEf(c)
            && !(c.tipo == "amuleto" && amuletos.Count >= data.reglas.amuletosMax)
            && !(c.tipo == "mejora" && !party.Any(h => h.SlotsLibres(Slots) > 0));

        public void Jugar(int i)
        {
            if (Terminada) return;
            var c = mano[i];
            if (c.tipo == "mejora" && objetivo < 0) { ElegirDestino(i); return; }
            if (fase != Fase.Prep && fase != Fase.Gasto) return;

            int cf = CostoEf(c);
            if (cris < cf) return;
            if (c.tipo == "amuleto" && amuletos.Count >= data.reglas.amuletosMax)
            {
                Log("Ya llevás dos amuletos encima.");
                return;
            }

            cris -= cf;
            mano.RemoveAt(i);

            if (c.tipo == "mejora")
            {
                party[objetivo].mejoras.Add(c);
                Log($"<b>{c.nombre}</b> queda en manos de <b>{party[objetivo].Nombre}</b>.");
                objetivo = -1;
            }
            else if (c.tipo == "perm")
            {
                party.Add(new Heroe { carta = c, vida = c.vida });
                Log($"Bajás a <b>{c.nombre}</b>.");
            }
            else if (c.tipo == "amuleto")
            {
                amuletos.Add(c);
                Log($"Te colgás <b>{c.nombre}</b>.");
            }
            else
            {
                descarte.Add(c);
                UsarCarta(c);
            }
            Pintar();
        }

        void UsarCarta(Carta c)
        {
            var e = c.efecto;
            if (e == null) return;

            if (e.cura > 0) Curar(e.cura);

            if (e.robar > 0) { Robar(e.robar); Log($"<b>{c.nombre}</b>: robás {e.robar} cartas."); }

            if (e.dar != null && e.dar.Total > 0)
            {
                foreach (var x in Produccion.Todos) r.Sumar(x, e.dar.Get(x));
                Log($"<b>{c.nombre}</b>: te suma recursos.");
            }

            if (e.nido > 0)
            {
                var lug = enemigos.FirstOrDefault(x => !x.esCriatura);
                if (lug == null) Log($"<b>{c.nombre}</b>: no hay nada sobre lo que empujar.");
                else
                {
                    var rec = Produccion.Todos.FirstOrDefault(x => lug.costo.Get(x) > lug.pagado.Get(x));
                    if (rec != null) lug.pagado.Sumar(rec, e.nido);
                    Log($"<b>{c.nombre}</b>: avanzás sobre <b>{lug.nombre}</b>.");
                    if (lug.Desarmado)
                    {
                        enemigos.Remove(lug);
                        Log($"<b>{lug.nombre}</b> queda desarmado.");
                        RevisarVictoria();
                    }
                }
            }

            if (e.resolver > 0)
            {
                if (efimeros.Count == 0) Log($"<b>{c.nombre}</b>: no hay nada que resolver.");
                else
                {
                    var q = efimeros[0]; efimeros.RemoveAt(0); resueltos++;
                    Log($"<b>{c.nombre}</b>: se ocupan de <b>{q.nombre}</b> por vos.");
                }
            }

            if (e.revivir > 0)
            {
                if (cementerio.Count == 0) Log($"<b>{c.nombre}</b>: no hay a quién llamar todavía.");
                else
                {
                    var m = cementerio[cementerio.Count - 1];
                    cementerio.RemoveAt(cementerio.Count - 1);
                    mano.Add(m);
                    Log($"<b>{c.nombre}</b>: <b>{m.nombre}</b> responde y vuelve a tu mano.");
                }
            }

            if (e.tresdados > 0)
            {
                tresdados = true;
                Log($"<b>{c.nombre}</b>: el próximo turno tirás tres dados.");
            }

            if (e.huir > 0)
            {
                if (efimeros.Count == 0) Log("No hay de qué huir.");
                else
                {
                    var q = efimeros[efimeros.Count - 1];
                    efimeros.RemoveAt(efimeros.Count - 1);
                    Log($"<b>{c.nombre}</b>: te vas de <b>{q.nombre}</b> sin resolverlo.");
                }
            }
        }

        public void Resolver(int idx, int via)
        {
            if (fase != Fase.Gasto || Terminada) return;
            var e = efimeros[idx];
            var costo = e.vias[via].costo;
            if (!Puede(costo)) return;
            PagarCosto(costo);
            efimeros.RemoveAt(idx);
            resueltos++;
            string extra = "";
            if (e.recompensa != null)
            {
                if (e.recompensa.cura > 0) { Curar(e.recompensa.cura); extra += " Alguien se recompone."; }
                if (e.recompensa.carta > 0) { Robar(e.recompensa.carta); extra += $" Robás {e.recompensa.carta}."; }
                if (e.recompensa.cris > 0) { cris += e.recompensa.cris; extra += $" Te deja {e.recompensa.cris} de cristal."; }
            }
            Log($"<b>{e.nombre}</b>: {e.vias[via].texto.ToLower()}.{extra}");
            Pintar();
        }

        // ── el turno ──────────────────────────────────────────────────────────

        public void Empezar()
        {
            if (fase != Fase.Prep) return;
            if (party.Count == 0) { Log("No podés salir sin nadie. Invocá al menos a uno."); return; }
            fase = Fase.Tirar;
            turno = 1;
            r.Vaciar();
            cris += data.reglas.cristalesPorTurno;
            Log("<b>Turno 1.</b> Un cristal más, robás y tirás.");
            Robar(data.reglas.robaPorTurno);
            Pintar();
        }

        public void Tirar()
        {
            if (fase != Fase.Tirar || Terminada) return;
            int a = 1 + R(6), b = 1 + R(6);
            if (a == b && Tiene("dobles") && !yaRetiro)
            {
                d1 = a; d2 = b; fase = Fase.Dobles;
                Log($"Sacás dobles de <b>{a}</b>. El amuleto te deja volver a tirar si querés.");
                Pintar();
                return;
            }
            d1 = a; d2 = b;
            Producir();
        }

        public void Retirar()
        {
            if (fase != Fase.Dobles) return;
            yaRetiro = true;
            d1 = 1 + R(6); d2 = 1 + R(6);
            Log($"Volvés a tirar: <b>{d1}</b> y <b>{d2}</b>.");
            Producir();
        }

        public void AceptarDobles() { if (fase == Fase.Dobles) Producir(); }

        /// Tirada con los dados puestos a mano. La usan los tests para no pelear
        /// contra el azar, y sirve para reproducir una situación puntual.
        public void TirarFijo(int a, int b)
        {
            if (fase != Fase.Tirar || Terminada) return;
            d1 = a; d2 = b;
            Producir();
        }

        void Producir()
        {
            var ds = new List<int> { d1, d2 };
            if (tresdados)
            {
                ds.Add(1 + R(6));
                ds.Sort((x, y) => y.CompareTo(x));   // el menor de los tres se descarta
                d3 = ds[2];
                tresdados = false;
            }
            else d3 = 0;
            d1 = ds[0]; d2 = ds[1];
            int v = d1 + d2;

            r.Vaciar();
            var producen = new List<string>();
            foreach (var h in party)
                foreach (var b in h.TodasLasBandas)
                {
                    if (!b.Cubre(v)) continue;
                    int mult = string.IsNullOrEmpty(b.porCadaRaza) ? 1 : party.Count(x => x.Raza == b.porCadaRaza);
                    if (b.produce != null)
                        foreach (var x in Produccion.Todos) r.Sumar(x, b.produce.Get(x) * mult);
                    if (b.cristales > 0) cris += b.cristales;
                    if (b.cura > 0) Curar(b.cura);
                    producen.Add(h.Nombre);
                }

            Log($"Sale <b>{v}</b>{(d3 > 0 ? $" (descartás un {d3})" : "")}. " +
                (producen.Count > 0 ? "Producen: " + string.Join(", ", producen) + "." : "No produce nadie."));

            // Lo del enemigo se anuncia ahora y se aplica al terminar, para que puedas responder.
            pendientes.Clear();
            foreach (var b in terreno.ambiental.bandas)
                if (v >= b.desde && v <= b.hasta)
                    pendientes.Add(new Pendiente { de = terreno.ambiental.nombre, ef = b.efecto });
            foreach (var e in enemigos)
                foreach (var b in e.bandas)
                    if (v >= b.desde && v <= b.hasta)
                        pendientes.Add(new Pendiente { de = e.nombre, ef = b.efecto, quien = e });

            foreach (var h in party) h.escudo = 0;
            if (Tiene("escudo") && party.Count > 0) { party[0].escudo = 1; Log("El farol cubre al de adelante."); }

            if (pendientes.Count > 0)
                Log($"Se viene: {string.Join(", ", pendientes.Select(p => p.de))}.");

            fase = Fase.Gasto;
            if (party.Count == 0) { Fin(false, "No te queda nadie en pie."); return; }
            Pintar();
        }

        public void Terminar()
        {
            if (fase != Fase.Gasto || Terminada) return;

            foreach (var p in pendientes.ToList())
                if (!Terminada) AplicarEfecto(p.ef, p.quien?.nombre ?? p.de, p.quien);
            pendientes.Clear();
            if (party.Count == 0) { Fin(false, "No te queda nadie en pie."); return; }

            foreach (var e in efimeros.ToList())
            {
                if (Terminada) break;
                if (e.bueno) { Log($"Dejás pasar <b>{e.nombre}</b>."); continue; }
                Log($"No llegaste a <b>{e.nombre}</b>.");
                AplicarEfecto(e.efecto, e.nombre, null);
            }
            efimeros.Clear();
            if (party.Count == 0) { Fin(false, "No te queda nadie en pie."); return; }

            while (mano.Count > data.reglas.limiteMano)
            {
                var c = mano[mano.Count - 1];
                mano.RemoveAt(mano.Count - 1);
                descarte.Add(c);
                Log($"No te entra todo en la mochila: se te cae <b>{c.nombre}</b>.");
            }

            turno++;
            fase = Fase.Tirar;
            yaRetiro = false;
            r.Vaciar();
            cris += data.reglas.cristalesPorTurno;
            Robar(data.reglas.robaPorTurno);

            foreach (var a in terreno.apariciones.Where(a => a.turno == turno))
                AbrirEnemigo(a.q, a.n);
            if (turno >= 2)
                for (int i = 0; i < terreno.efimerosPorTurno; i++) AbrirEfimero();

            Log($"<b>Turno {turno}.</b>");
            Pintar();
        }

        void RevisarVictoria()
        {
            if (enemigos.Count > 0) return;
            if (turno < UltimoTurnoDeAparicion) return;
            ruta.Add(terreno.nombre);
            if (etapa >= 3) { Fin(true, null); return; }
            AbrirInterludio();
        }

        // ── la campaña ────────────────────────────────────────────────────────

        void AbrirInterludio()
        {
            fase = Fase.Interludio;

            var pozo = Repartibles
                .Where(c => c.tipo != "amuleto" || !amuletos.Any(a => a.nombre == c.nombre))
                .ToList();
            Barajar(pozo);

            interludio = new Interludio
            {
                cofre = pozo.Take(3).ToList(),
                rutas = data.terrenos.Where(t => t.etapa == etapa + 1).ToList(),
            };

            if (cementerio.Count > 0)
            {
                Log($"Los caídos vuelven al mazo: {string.Join(", ", cementerio.Select(c => c.nombre))}.");
                mazo.AddRange(cementerio);
                cementerio.Clear();
            }

            foreach (var h in party) h.vida = h.VidaMax;

            mazo.AddRange(descarte); descarte.Clear();
            mazo.AddRange(mano); mano.Clear();
            Barajar(mazo);

            Log("<b>Cruzaste el terreno.</b> Descansan, se curan y abren el cofre.");
            Pintar();
        }

        public void TomarDelCofre(int i)
        {
            if (fase != Fase.Interludio || interludio.tomada) return;
            if (i < 0 || i >= interludio.cofre.Count) return;
            var c = interludio.cofre[i];
            mazo.Add(c);
            interludio.tomada = true;
            Log($"Del cofre sacás <b>{c.nombre}</b>.");
            Pintar();
        }

        /// Adelgazar el mazo es tan valioso como engordarlo, así que se puede
        /// dejar una carta atrás. La semilla no.
        public void SacarDelMazo(string nombre)
        {
            if (fase != Fase.Interludio || interludio.sacada) return;
            int i = mazo.FindIndex(c => c.nombre == nombre);
            if (i < 0) return;
            if (mazo[i] == LaSemilla) { Log("La semilla no se puede dejar atrás."); return; }
            mazo.RemoveAt(i);
            interludio.sacada = true;
            Log($"Dejás atrás <b>{nombre}</b>. No lo vas a volver a ver.");
            Pintar();
        }

        public void PartirHacia(string id)
        {
            if (fase != Fase.Interludio) return;
            var t = data.terrenos.FirstOrDefault(x => x.id == id);
            if (t == null) return;

            // Siguen viaje sólo los primeros cinco: el orden que armaste para
            // pelear también decide quién te acompaña.
            if (party.Count > 5)
            {
                var quedan = party.Skip(5).ToList();
                party.RemoveRange(5, party.Count - 5);
                foreach (var h in quedan)
                {
                    if (h.mejoras.Count > 0) { mazo.AddRange(h.mejoras); h.mejoras.Clear(); }
                    mazo.Add(h.carta);
                }
                Log($"Se quedan atrás: <b>{string.Join(", ", quedan.Select(h => h.Nombre))}</b>. Vuelven al mazo.");
            }

            etapa++;
            terreno = t;
            enemigos.Clear();
            efimeros.Clear();
            pendientes.Clear();
            efMazo.Clear();
            efMazo.AddRange(MazoEfimeros(etapa));

            if (!semilla)
            {
                semilla = true;
                if (LaSemilla != null) mazo.Add(LaSemilla);
                Log("<b>Encontraste la semilla</b> entre las raíces. Va con vos y no se puede dejar.");
            }
            else
            {
                if (LaMutacion != null) mazo.Add(LaMutacion);
                Log("<b>La semilla se mueve sola.</b> Entra una mutación a tu mazo.");
            }

            Barajar(mazo);
            turno = 0;
            interludio = null;
            Log($"<b>{t.nombre}.</b> {t.intro}");

            // Terminar() hace el papeleo de arranque de turno: cobra, roba y abre.
            fase = Fase.Gasto;
            Terminar();
        }

        void Fin(bool ganaste, string motivo)
        {
            fase = Fase.Fin;
            gano = ganaste;
            motivoFin = motivo;
            Pintar();
        }
    }
}
