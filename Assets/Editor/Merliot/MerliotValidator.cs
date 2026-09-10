using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Merliot.EditorTools
{
    /// Chequea que el JSON no tenga incoherencias antes de que las descubras jugando.
    public static class MerliotValidator
    {
        [MenuItem("Merliot/Validar datos")]
        public static void Validar()
        {
            var ta = Resources.Load<TextAsset>("merliot");
            if (ta == null) { Debug.LogError("No encuentro Resources/merliot.json"); return; }
            var d = JsonUtility.FromJson<MerliotData>(ta.text);
            int errores = 0;

            foreach (var c in d.cartas)
            {
                if (c.tipo == "perm" && c.vida <= 0)
                    { Debug.LogError($"{c.nombre}: héroe sin vida"); errores++; }
                if (c.tipo == "perm" && (c.bandas == null || c.bandas.Count == 0))
                    { Debug.LogError($"{c.nombre}: héroe sin bandas de producción"); errores++; }
                if (c.tipo == "mejora" && !string.IsNullOrEmpty(c.soloRaza)
                    && !d.razas.Any(r => r.id == c.soloRaza))
                    { Debug.LogError($"{c.nombre}: raza desconocida '{c.soloRaza}'"); errores++; }
                foreach (var b in c.bandas ?? new System.Collections.Generic.List<Banda>())
                {
                    if (b.desde < 2 || b.hasta > 12 || b.desde > b.hasta)
                        { Debug.LogError($"{c.nombre}: banda fuera de rango {b.desde}-{b.hasta}"); errores++; }
                    if ((b.produce == null || b.produce.Total == 0) && b.cristales == 0 && b.cura == 0 && b.roba == 0)
                        { Debug.LogError($"{c.nombre}: banda {b.desde}-{b.hasta} no da nada"); errores++; }
                }
            }

            foreach (var e in d.efimeros)
            {
                if (e.vias == null || e.vias.Count == 0)
                    { Debug.LogError($"{e.nombre}: efímero sin vía de resolución"); errores++; }
                if (e.nivel < 1 || e.nivel > 3)
                    { Debug.LogError($"{e.nombre}: nivel {e.nivel} fuera de 1-3"); errores++; }
            }

            // toda aparición tiene que nombrar algo que exista, del tipo que dice su 'q'
            var criaturas = d.criaturas.Select(c => c.nombre).ToHashSet();
            var lugares   = d.lugares.Select(l => l.nombre).ToHashSet();
            var sitios    = (d.sitios ?? new System.Collections.Generic.List<Sitio>())
                            .Select(s => s.nombre).ToHashSet();
            foreach (var t in d.terrenos)
                foreach (var a in t.apariciones ?? new System.Collections.Generic.List<Aparicion>())
                {
                    var donde = a.q == "criatura" ? criaturas : a.q == "sitio" ? sitios : lugares;
                    if (!donde.Contains(a.n))
                        { Debug.LogError($"{t.nombre}: aparece '{a.n}' como {a.q}, y no existe"); errores++; }
                }

            // las marcas: todo lo que las nombra tiene que nombrar una que exista
            var marcas = (d.marcas ?? new System.Collections.Generic.List<Marca>())
                         .Select(m => m.id).ToHashSet();
            void ChequearMarca(string id, string quien)
            {
                if (!string.IsNullOrEmpty(id) && !marcas.Contains(id))
                    { Debug.LogError($"{quien}: marca desconocida '{id}'"); errores++; }
            }
            foreach (var e in d.efimeros)
            {
                ChequearMarca(e.requiere, e.nombre);
                ChequearMarca(e.marcaSiFalla, e.nombre);
                foreach (var v in e.vias ?? new System.Collections.Generic.List<Via>())
                    ChequearMarca(v.marca, e.nombre);
            }
            foreach (var l in d.lugares)
                if (l.descuento != null && l.descuento.Hay)
                {
                    ChequearMarca(l.descuento.marca, l.nombre);
                    if (!Produccion.Todos.Contains(l.descuento.r))
                        { Debug.LogError($"{l.nombre}: descuento sobre '{l.descuento.r}', que no es un recurso"); errores++; }
                }

            // el mazo de salida tiene que existir de verdad
            var porNombre = d.cartas.Select(c => c.nombre).ToHashSet();
            foreach (var e in d.mazoInicial ?? new System.Collections.Generic.List<EntradaMazo>())
            {
                if (!porNombre.Contains(e.carta))
                    { Debug.LogError($"Mazo inicial: '{e.carta}' no está en cartas"); errores++; }
                if (e.copias < 1)
                    { Debug.LogError($"Mazo inicial: '{e.carta}' con {e.copias} copias"); errores++; }
            }

            // la campaña son tres etapas, y en la 2 y la 3 hay que poder elegir
            for (int etapa = 1; etapa <= 3; etapa++)
            {
                int n = d.terrenos.Count(t => t.etapa == etapa);
                int minimo = etapa == 1 ? 1 : 2;
                if (n < minimo)
                    { Debug.LogError($"Etapa {etapa}: {n} terrenos, hacen falta {minimo}"); errores++; }
            }

            // cobertura del dado: ¿hay algo que produzca en cada resultado?
            for (int v = 2; v <= 12; v++)
            {
                int n = d.cartas.Count(c => c.bandas != null &&
                        c.bandas.Any(b => b.Cubre(v) && b.produce != null && b.produce.Total > 0));
                if (n == 0) { Debug.LogWarning($"Ninguna carta produce con {v}"); }
            }

            Debug.Log(errores == 0
                ? $"[Merliot] Datos OK. {d.cartas.Count} cartas, {d.sitios?.Count ?? 0} sitios, {d.marcas?.Count ?? 0} marcas."
                : $"[Merliot] {errores} problemas.");
        }
    }
}
