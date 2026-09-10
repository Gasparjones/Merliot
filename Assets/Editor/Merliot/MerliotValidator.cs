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
                    if (b.desde < 2 || b.hasta > 12 || b.desde > b.hasta)
                        { Debug.LogError($"{c.nombre}: banda fuera de rango {b.desde}-{b.hasta}"); errores++; }
            }

            foreach (var e in d.efimeros)
            {
                if (e.vias == null || e.vias.Count == 0)
                    { Debug.LogError($"{e.nombre}: efímero sin vía de resolución"); errores++; }
                if (e.nivel < 1 || e.nivel > 3)
                    { Debug.LogError($"{e.nombre}: nivel {e.nivel} fuera de 1-3"); errores++; }
            }

            // toda aparición tiene que nombrar algo que exista
            var enemigos = d.criaturas.Select(c => c.nombre)
                            .Concat(d.lugares.Select(l => l.nombre)).ToHashSet();
            foreach (var t in d.terrenos)
                foreach (var a in t.apariciones ?? new System.Collections.Generic.List<Aparicion>())
                    if (!enemigos.Contains(a.n))
                        { Debug.LogError($"{t.nombre}: aparece '{a.n}', que no existe"); errores++; }

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
                ? $"[Merliot] Datos OK. {d.cartas.Count} cartas revisadas."
                : $"[Merliot] {errores} problemas.");
        }
    }
}
