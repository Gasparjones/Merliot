using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Merliot.Tests
{
    /// El motor no depende de Unity, así que estos tests no cargan escena.
    /// Lo que se chequea acá es lo que el prototipo web ya hacía bien:
    /// si alguno se cae, el juego dejó de comportarse como la versión que se playtesteó.
    public class PartidaTests
    {
        MerliotData datos;

        [SetUp]
        public void Cargar()
        {
            var ta = Resources.Load<TextAsset>("merliot");
            Assert.IsNotNull(ta, "no está Resources/merliot.json");
            datos = JsonUtility.FromJson<MerliotData>(ta.text);
        }

        Partida Nueva(int semilla = 12345)
        {
            var p = new Partida(datos, semilla);
            p.Iniciar();
            return p;
        }

        static Carta Heroe(string nombre, int vida, string raza = "morza") =>
            new Carta { id = nombre, nombre = nombre, tipo = "perm", vida = vida, raza = raza, copias = 1 };

        [Test]
        public void LaMismaSemillaDaLaMismaPartida()
        {
            var a = Nueva(777);
            var b = Nueva(777);
            CollectionAssert.AreEqual(a.mano.Select(c => c.id).ToList(),
                                      b.mano.Select(c => c.id).ToList());
            CollectionAssert.AreEqual(a.mazo.Select(c => c.id).ToList(),
                                      b.mazo.Select(c => c.id).ToList());
        }

        [Test]
        public void ElMazoTraeTodasLasCopias()
        {
            var p = Nueva();
            int esperadas = datos.cartas.Sum(c => c.copias);
            Assert.AreEqual(esperadas, p.mazo.Count + p.mano.Count);
            Assert.AreEqual(datos.reglas.manoInicial, p.mano.Count);
            Assert.AreEqual(datos.reglas.cristalesIniciales, p.cris);
        }

        [Test]
        public void ElMulliganDeSalidaEsElMismoQueElDelPrototipo()
        {
            // El bucle sólo cambia cartas que NO son perm. Si la mano sale con cinco
            // héroes, no hay nada que devolver al mazo y te quedás con lo que salió.
            // No es un descuido del port: el prototipo hace exactamente esto.
            for (int semilla = 1; semilla <= 40; semilla++)
            {
                var p = Nueva(semilla);
                int baratos = p.mano.Count(c => c.tipo == "perm" && c.costoCristales <= 2);
                if (baratos >= 2) continue;
                Assert.AreEqual(0, p.mano.Count(c => c.tipo != "perm"),
                    $"semilla {semilla}: quedó sin dos héroes baratos teniendo cartas para cambiar");
            }
        }

        [Test]
        public void DosDeCualquieraValenUnoDelQueFalta()
        {
            var p = Nueva();
            p.r.vigor = 4;
            var costo = new Produccion { temple = 2 };
            Assert.IsTrue(p.Puede(costo), "4 de vigor tendrían que pagar 2 de temple a dos por uno");
            Assert.IsFalse(p.Exacto(costo), "no tiene el recurso justo");

            p.r.Vaciar();
            p.r.vigor = 3;
            Assert.IsFalse(p.Puede(costo), "3 no alcanzan para convertir en 2");
        }

        [Test]
        public void ElDañoBajaPorLaFilaDeAdelanteHaciaAtras()
        {
            var p = Nueva();
            p.party.Add(new Heroe { carta = Heroe("Uno", 2), vida = 2 });
            p.party.Add(new Heroe { carta = Heroe("Dos", 5), vida = 5 });

            p.Dañar(4);

            Assert.AreEqual(1, p.party.Count, "el de adelante tendría que haber caído");
            Assert.AreEqual("Dos", p.party[0].Nombre);
            Assert.AreEqual(3, p.party[0].vida, "los 2 que sobraron pasan al siguiente");
            Assert.AreEqual(1, p.cementerio.Count, "el caído va al cementerio, no al descarte");
            Assert.AreEqual(0, p.descarte.Count);
        }

        [Test]
        public void ElEscudoFrenaAntesQueLaVida()
        {
            var p = Nueva();
            p.party.Add(new Heroe { carta = Heroe("Uno", 4), vida = 4, escudo = 2 });

            p.Dañar(3);

            Assert.AreEqual(0, p.party[0].escudo);
            Assert.AreEqual(3, p.party[0].vida, "sólo tendría que haber pasado 1");
        }

        [Test]
        public void AlCaerElEquipoSePierdeYElHeroeVaAlCementerio()
        {
            var p = Nueva();
            var mejora = new Carta { id = "m", nombre = "Mejora", tipo = "mejora" };
            var h = new Heroe { carta = Heroe("Uno", 1), vida = 1 };
            h.mejoras.Add(mejora);
            p.party.Add(h);

            p.Dañar(1);

            Assert.AreEqual(0, p.party.Count);
            Assert.AreEqual(1, p.cementerio.Count);
            CollectionAssert.Contains(p.descarte, mejora, "el equipo se pierde al descarte");
        }

        [Test]
        public void PorCadaRazaMultiplicaLaProduccion()
        {
            var p = Nueva();
            var cabecilla = Heroe("Cabecilla", 3, "aven");
            cabecilla.bandas = new System.Collections.Generic.List<Banda>
            {
                new Banda { desde = 2, hasta = 12, produce = new Produccion { saber = 1 }, porCadaRaza = "aven" }
            };
            p.party.Add(new Heroe { carta = cabecilla, vida = 3 });
            p.party.Add(new Heroe { carta = Heroe("Otro aven", 3, "aven"), vida = 3 });
            p.party.Add(new Heroe { carta = Heroe("Un morza", 3, "morza"), vida = 3 });

            p.Empezar();
            p.TirarFijo(3, 4);

            Assert.AreEqual(2, p.r.saber, "hay dos aven en la fila, tendría que producir el doble");
        }

        [Test]
        public void ElEnemigoSeAnunciaAlTirarYPegaAlTerminar()
        {
            var p = Nueva();
            p.party.Add(new Heroe { carta = Heroe("Aguanta", 9), vida = 9 });
            p.Empezar();
            p.TirarFijo(1, 2);   // 3: el frío del norte pega 2

            Assert.AreEqual(Fase.Gasto, p.fase);
            Assert.AreEqual(1, p.pendientes.Count, "el ambiental tendría que estar anunciado");
            Assert.AreEqual(9, p.party[0].vida, "todavía no pegó");

            p.Terminar();

            Assert.AreEqual(7, p.party[0].vida, "al terminar el turno sí");
            Assert.AreEqual(2, p.turno);
        }

        [Test]
        public void CerrarElTurnoCobraRobaYAbreLoQueToca()
        {
            var p = Nueva();
            p.party.Add(new Heroe { carta = Heroe("Aguanta", 20), vida = 20 });
            p.Empezar();
            int cristales = p.cris, enMano = p.mano.Count;
            p.TirarFijo(6, 6);   // 12: no dispara nada del terreno
            p.Terminar();

            Assert.AreEqual(cristales + datos.reglas.cristalesPorTurno, p.cris);
            Assert.LessOrEqual(p.mano.Count, datos.reglas.limiteMano);
            Assert.IsTrue(p.enemigos.Any(e => e.nombre == "Broten mayor"),
                          "en el turno 2 tendría que aparecer el Broten mayor");
            Assert.AreEqual(1, p.efimeros.Count, "y un efímero por turno");
        }

        [Test]
        public void QuedarseSinNadieTerminaLaPartida()
        {
            var p = Nueva();
            p.party.Add(new Heroe { carta = Heroe("Frágil", 1), vida = 1 });
            p.Empezar();
            p.TirarFijo(1, 2);   // el frío pega 2 al de adelante
            p.Terminar();

            Assert.AreEqual(Fase.Fin, p.fase);
            Assert.IsFalse(p.gano);
        }

        [Test]
        public void LasCartasDeUsoHacenAlgo()
        {
            var p = Nueva();
            var silbato = datos.cartas.First(c => c.efecto != null && c.efecto.robar > 0);
            Assert.IsNotNull(silbato.efecto, "las cartas de uso tienen que traer su efecto del JSON");

            p.mano.Clear();
            p.mano.Add(silbato);
            p.cris = 9;
            int antes = p.mano.Count;

            p.Jugar(0);

            Assert.AreEqual(antes - 1 + silbato.efecto.robar, p.mano.Count,
                            "tendría que haberse ido de la mano y haber robado");
            CollectionAssert.Contains(p.descarte, silbato);
        }
    }
}
