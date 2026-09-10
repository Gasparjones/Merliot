using System;
using System.Collections.Generic;

namespace Merliot
{
    // Espejo 1 a 1 de data/merliot.json.
    // Si cambiás el JSON, cambiá esto. Si cambiás esto, cambiá el JSON.
    // No dupliques valores en ningún otro lado.

    [Serializable] public class Banda
    {
        public int desde;
        public int hasta;
        public Produccion produce;   // puede venir null
        public int cristales;
        public int cura;
        public string porCadaRaza;   // multiplica la produccion por cuantos de esa raza tengas
        public bool Cubre(int tirada) => tirada >= desde && tirada <= hasta;
    }

    [Serializable] public class Produccion
    {
        public int vigor, temple, destreza, saber;
        public int Get(string r) => r switch {
            "vigor" => vigor, "temple" => temple,
            "destreza" => destreza, "saber" => saber, _ => 0 };
        public int Total => vigor + temple + destreza + saber;
    }

    [Serializable] public class Efecto
    {
        public int dmg;         // al de adelante
        public int dmgTodos;    // a cada uno
        public int dmgFondo;    // al ultimo de la fila
        public int dmgDado;     // multiplica el dado menor
        public int rotar;       // el de adelante pasa al fondo
        public int descartar;
        public int brota;       // larga un efimero
        public int cura;
    }

    [Serializable] public class AlCaer { public int cris; public int curaTodos; }

    [Serializable] public class Carta
    {
        public string id, nombre, tipo;   // perm | mejora | amuleto | uso
        public int copias;
        public int costoCristales;
        public string texto;
        public string raza;               // solo tipo perm
        public int vida;                  // solo tipo perm
        public string soloRaza;           // solo tipo mejora
        public AlCaer alCaer;
        public string pasiva;             // solo tipo amuleto: dobles | barato | escudo
        public string efectoTexto;
        public List<Banda> bandas;
    }

    [Serializable] public class BandaEnemiga { public int desde, hasta; public Efecto efecto; }

    [Serializable] public class Criatura
    {
        public string id, nombre, texto;
        public int vida;                  // se baja gastando vigor, 1 a 1
        public List<BandaEnemiga> bandas;
    }

    [Serializable] public class Lugar
    {
        public string id, nombre, texto;
        public Produccion costo;          // pago acumulable entre turnos
        public List<BandaEnemiga> bandas;
    }

    [Serializable] public class Via { public string texto; public Produccion costo; }

    [Serializable] public class Recompensa { public int cura, carta, cris; }

    [Serializable] public class Efimero
    {
        public string id, nombre, texto;
        public bool bueno;                // si no lo pagas, simplemente se va
        public List<Via> vias;
        public Efecto efecto;
        public Recompensa recompensa;
    }

    [Serializable] public class Aparicion { public string q, n; public int turno; }

    [Serializable] public class Ambiental
    {
        public string nombre, texto;
        public List<BandaEnemiga> bandas;
    }

    [Serializable] public class Terreno
    {
        public string id, nombre;
        public Ambiental ambiental;
        public List<Aparicion> apariciones;
        public int efimerosPorTurno;
    }

    [Serializable] public class Reglas
    {
        public int cristalesIniciales, cristalesPorTurno, manoInicial;
        public int robaPorTurno, limiteMano, slotsPorHeroe, amuletosMax;
        public string conversionRecursos, escudo, reordenar;
    }

    [Serializable] public class IdNombre { public string id, nombre; }

    [Serializable] public class MerliotData
    {
        public string version;
        public List<IdNombre> recursos;
        public List<IdNombre> razas;
        public List<Carta> cartas;
        public List<Criatura> criaturas;
        public List<Lugar> lugares;
        public List<Efimero> efimeros;
        public List<Terreno> terrenos;
        public Reglas reglas;
    }
}
