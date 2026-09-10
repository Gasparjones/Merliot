using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Merliot
{
    /// Carga data/merliot.json y lo deja disponible.
    /// Poné el json en Assets/Resources/merliot.json (o arrastrá el TextAsset).
    public class MerliotDatabase : MonoBehaviour
    {
        public static MerliotDatabase I { get; private set; }

        [Tooltip("Si lo dejás vacío se busca en Resources/merliot")]
        public TextAsset json;

        public MerliotData Data { get; private set; }

        void Awake()
        {
            // Destruyo el componente, no el objeto: en el objeto viaja también
            // la pantalla de la escena que se está cargando, y llevársela puesta
            // deja la escena muda.
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Cargar();
        }

        public void Cargar()
        {
            var ta = json != null ? json : Resources.Load<TextAsset>("merliot");
            if (ta == null) { Debug.LogError("[Merliot] No encuentro merliot.json"); return; }
            Data = JsonUtility.FromJson<MerliotData>(ta.text);
            Debug.Log($"[Merliot] {Data.cartas.Count} cartas, {Data.criaturas.Count} criaturas, " +
                      $"{Data.lugares.Count} lugares, {Data.efimeros.Count} efímeros.");
        }

        public Carta Carta(string id) => Data.cartas.FirstOrDefault(c => c.id == id);
        public IEnumerable<Carta> Heroes => Data.cartas.Where(c => c.tipo == "perm");
        public IEnumerable<Carta> Mejoras => Data.cartas.Where(c => c.tipo == "mejora");
        public IEnumerable<Carta> DeRaza(string raza) => Heroes.Where(c => c.raza == raza);

        /// El mazo de arranque, con las copias que indica cada carta.
        public List<Carta> ArmarMazo()
        {
            var m = new List<Carta>();
            foreach (var c in Data.cartas)
                for (int i = 0; i < c.copias; i++) m.Add(c);
            return m;
        }

        /// Cuánto produce un héroe con una tirada dada, contando sus mejoras
        /// y los multiplicadores por raza.
        public Produccion Producir(Carta heroe, IEnumerable<Carta> mejoras, int tirada,
                                   IReadOnlyList<Carta> filaCompleta)
        {
            var total = new Produccion();
            var todas = new List<Banda>(heroe.bandas ?? new List<Banda>());
            foreach (var m in mejoras) if (m.bandas != null) todas.AddRange(m.bandas);

            foreach (var b in todas)
            {
                if (!b.Cubre(tirada) || b.produce == null) continue;
                int mult = string.IsNullOrEmpty(b.porCadaRaza)
                    ? 1
                    : filaCompleta.Count(h => h.raza == b.porCadaRaza);
                total.vigor    += b.produce.vigor    * mult;
                total.temple   += b.produce.temple   * mult;
                total.destreza += b.produce.destreza * mult;
                total.saber    += b.produce.saber    * mult;
            }
            return total;
        }

        /// Regla de conversión: dos de cualquier otro tipo valen uno del que falte.
        public static bool PuedePagar(Produccion tenes, Produccion costo)
        {
            int falta = 0, sobra = 0;
            foreach (var r in new[] { "vigor", "temple", "destreza", "saber" })
            {
                int n = costo.Get(r), t = tenes.Get(r);
                if (t >= n) sobra += t - n; else falta += n - t;
            }
            return falta * 2 <= sobra;
        }
    }
}
