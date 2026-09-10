using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Merliot.Tests
{
    /// Que la escena de la partida abra y dibuje. Sin esto, un error de layout
    /// o un null en el repintado se descubre recién apretando Play.
    public class PantallaTests
    {
        [UnityTest]
        public IEnumerator LaEscenaDeLaPartidaAbreYDibuja()
        {
            SceneManager.LoadScene("Partida", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var pantalla = Object.FindFirstObjectByType<PantallaPartida>();
            Assert.IsNotNull(pantalla, "la escena no tiene PantallaPartida");
            Assert.IsNotNull(MerliotDatabase.I?.Data, "el JSON no cargó");

            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas, "no se armó el Canvas");

            var textos = canvas.GetComponentsInChildren<Text>().Select(t => t.text).ToList();
            Assert.Greater(textos.Count, 30, "la pantalla quedó casi vacía");

            // las piezas que tienen que estar sí o sí en preparación
            Assert.IsTrue(textos.Any(t => t.Contains("MERLIOT")), "falta el título");
            Assert.IsTrue(textos.Any(t => t.Contains("Empezar la aventura")), "falta el botón de la fase");
            Assert.IsTrue(textos.Any(t => t.Contains("PREPARACIÓN")), "falta el nombre de la fase");
            Assert.IsTrue(textos.Any(t => t.Contains("limpiar el linde")), "falta el objetivo");
            Assert.IsTrue(textos.Any(t => t.Contains("Los Lindes de Urmand")), "falta el terreno");

            // y las cinco cartas de la mano, cada una con su botón
            var db = MerliotDatabase.I.Data;
            var mano = GameObject.Find("mano");
            Assert.IsNotNull(mano, "no está la mano");
            Assert.AreEqual(db.reglas.manoInicial, mano.transform.childCount, "la mano no tiene cinco cartas");
        }

        [UnityTest]
        public IEnumerator SePuedeJugarUnTurnoEnteroDesdeLaPantalla()
        {
            SceneManager.LoadScene("Partida", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var canvas = Object.FindFirstObjectByType<Canvas>();

            // bajar el primer héroe que se pueda pagar, y arrancar
            var botones = canvas.GetComponentsInChildren<Button>()
                                .Where(b => b.interactable).ToList();
            Assert.Greater(botones.Count, 0, "no hay nada apretable en preparación");
            foreach (var b in botones)
            {
                var t = b.GetComponentInChildren<Text>();
                if (t != null && t.text == "Invocar") { b.onClick.Invoke(); break; }
            }
            yield return null;

            var party = GameObject.Find("party");
            Assert.Greater(party.transform.childCount, 0, "no bajó ningún héroe a la fila");
        }
    }
}
