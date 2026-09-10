using System.Collections.Generic;
using UnityEngine;

namespace Merliot
{
    /// Los colores salen tal cual del :root del prototipo. Si cambia el CSS,
    /// cambia acá y en ningún otro lado.
    public static class Paleta
    {
        public static Color H(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        public static readonly Color Noche     = H("#0f1319");  // --n  fondo
        public static readonly Color Caja      = H("#181d26");  // --c  panel
        public static readonly Color Caja2     = H("#1f252f");  // --c2 hundido
        public static readonly Color Borde     = H("#2a313d");  // --b
        public static readonly Color Borde2    = H("#3b4453");  // --b2
        public static readonly Color Tinta     = H("#e6ddc7");  // --p  texto
        public static readonly Color Tenue     = H("#87857a");  // --t  microtexto

        public static readonly Color Vigor     = H("#c9563f");
        public static readonly Color Temple    = H("#5f8fa8");
        public static readonly Color Destreza  = H("#8a9c55");
        public static readonly Color Saber     = H("#9a86c4");
        public static readonly Color Oro       = H("#e8a33d");
        public static readonly Color Rojo      = H("#b04a35");
        public static readonly Color Cristal   = H("#8fd0d8");
        public static readonly Color Vida      = H("#c96a55");

        // secundarios que el CSS usa sueltos
        public static readonly Color PistaVida    = H("#301f1c");
        public static readonly Color PistaBarra   = H("#22272f");
        public static readonly Color PendFondo    = H("#241a18");
        public static readonly Color PendBorde    = H("#4a2f28");
        public static readonly Color PendNombre   = H("#d4907c");
        public static readonly Color PendEfecto   = H("#b09a91");
        public static readonly Color FrenteBorde  = H("#5a2f24");
        public static readonly Color RazaBorde    = H("#2a4a4e");
        public static readonly Color MejFondo     = H("#1b1a26");
        public static readonly Color MejBorde     = H("#332d45");
        public static readonly Color MejNombre    = H("#b3a9cc");
        public static readonly Color Lapida       = H("#8a8477");
        public static readonly Color LapidaFondo  = H("#171b22");
        public static readonly Color LapidaBorde  = H("#262b33");
        public static readonly Color SlotVacio    = H("#4d5147");
        public static readonly Color BandaOn      = new Color(0.910f, 0.639f, 0.239f, 0.12f);
        public static readonly Color BandaOnBorde = H("#5a4526");
        public static readonly Color BandaMala    = new Color(0.690f, 0.290f, 0.208f, 0.14f);
        public static readonly Color BandaMalaBor = H("#5e3128");
        public static readonly Color TextoBanda   = H("#aca696");
        public static readonly Color TextoCuerpo  = H("#aca696");
        public static readonly Color Objetivo     = H("#c3bda9");
        public static readonly Color TextoLog     = H("#a8a394");
        public static readonly Color Despierta    = H("#242b36");
        public static readonly Color HoverHeroe   = H("#2a3140");
        public static readonly Color Velo         = new Color(8/255f, 10/255f, 14/255f, 0.94f);
        public static readonly Color TextoModal   = H("#bdb7a5");

        static readonly Dictionary<string, Color> PorRecurso = new Dictionary<string, Color>
        {
            { "vigor", Vigor }, { "temple", Temple }, { "destreza", Destreza },
            { "saber", Saber }, { "cristal", Cristal }, { "vida", Vida },
        };

        static readonly Dictionary<string, Color> BordePorRecurso = new Dictionary<string, Color>
        {
            { "vigor", H("#4d2b23") }, { "temple", H("#25404d") },
            { "destreza", H("#3a4426") }, { "saber", H("#3d3453") },
        };

        public static Color De(string recurso) => PorRecurso.TryGetValue(recurso, out var c) ? c : Tenue;
        public static Color BordeDe(string recurso) => BordePorRecurso.TryGetValue(recurso, out var c) ? c : Borde;

        /// El color de la franja izquierda de una ficha, según qué es.
        public static Color Franja(string clase) => clase switch
        {
            "mio" or "perm" => Destreza,
            "malo"          => Rojo,
            "lugar"         => Saber,
            "mejora"        => Saber,
            "amuleto"       => Cristal,
            "amb"           => Temple,
            "uso"           => Oro,
            _               => Borde,
        };
    }
}
