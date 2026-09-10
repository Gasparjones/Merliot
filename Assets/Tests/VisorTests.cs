using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Merliot.Tests
{
    /// Lo que verifican estos tests no es "que compile": es que el JSON llegue
    /// hasta la pantalla. Si algo se rompe entre el archivo y la carta, salta acá.
    public class VisorTests
    {
        [UnityTest]
        public IEnumerator LaEscenaMuestraUnaCartaConSusBandas()
        {
            SceneManager.LoadScene("Visor", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var db = MerliotDatabase.I;
            Assert.IsNotNull(db, "no hay MerliotDatabase en la escena");
            Assert.IsNotNull(db.Data, "el JSON no cargó");
            Assert.AreEqual(51, db.Data.cartas.Count, "cambió la cantidad de cartas del mazo");

            var carta = GameObject.Find("carta");
            Assert.IsNotNull(carta, "el visor no instanció la carta");

            var textos = carta.GetComponentsInChildren<Text>().Select(t => t.text).ToList();
            var primera = db.Data.cartas[0];
            CollectionAssert.Contains(textos, primera.nombre, "no se ve el nombre de la carta");
            Assert.IsTrue(textos.Any(x => x.Contains(primera.id)), "no se ve el id en el pie");

            // la tira del dado: once celdas, del 2 al 12
            for (int v = 2; v <= 12; v++)
                CollectionAssert.Contains(textos, v.ToString(), $"falta la celda {v} de la tira del dado");

            // y las bandas de esa carta, con su rango y su descripción
            foreach (var b in primera.bandas)
            {
                var rango = b.desde == b.hasta ? $"{b.desde}" : $"{b.desde}–{b.hasta}";
                CollectionAssert.Contains(textos, rango, $"falta la banda {rango}");
                CollectionAssert.Contains(textos, CartaView.DescribirBanda(b), "falta lo que produce la banda");
            }
        }

        [UnityTest]
        public IEnumerator TodasLasCartasSeDibujanSinRomperse()
        {
            SceneManager.LoadScene("Visor", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var data = MerliotDatabase.I.Data;
            var vista = GameObject.Find("carta").GetComponent<CartaView>();

            foreach (var c in data.cartas)
            {
                vista.Mostrar(c, data);
                yield return null;
                var textos = vista.GetComponentsInChildren<Text>().Select(t => t.text).ToList();
                CollectionAssert.Contains(textos, c.nombre, $"'{c.nombre}' no se dibujó");
            }
        }
    }
}
