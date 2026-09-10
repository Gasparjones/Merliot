using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Merliot.EditorTools
{
    /// Arma la escena del visor desde cero. Está en un script y no a mano
    /// para que se pueda volver a generar sin depender de un .unity que alguien tocó.
    public static class CrearEscena
    {
        const string Ruta = "Assets/Scenes/Visor.unity";
        const string RutaPartida = "Assets/Scenes/Partida.unity";

        [MenuItem("Merliot/Crear escena del visor")]
        public static void Crear()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Camara", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x11, 0x10, 0x16, 0xff);
            camGo.tag = "MainCamera";

            var eventos = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var merliot = new GameObject("Merliot");
            merliot.AddComponent<MerliotDatabase>();
            merliot.AddComponent<VisorDeCartas>();

            Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
            EditorSceneManager.SaveScene(escena, Ruta);

            // que sea la escena 0 del build, así "Play" y el build muestran lo mismo
            var lista = EditorBuildSettings.scenes.Where(s => s.path != Ruta).ToList();
            lista.Insert(0, new EditorBuildSettingsScene(Ruta, true));
            EditorBuildSettings.scenes = lista.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Merliot] Escena creada en {Ruta} y puesta como escena 0 del build.");
        }

        [MenuItem("Merliot/Crear escena de la partida")]
        public static void CrearPartida()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Camara", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Paleta.Noche;
            camGo.tag = "MainCamera";

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var merliot = new GameObject("Merliot");
            merliot.AddComponent<MerliotDatabase>();
            merliot.AddComponent<PantallaPartida>();

            Directory.CreateDirectory(Path.GetDirectoryName(RutaPartida));
            EditorSceneManager.SaveScene(escena, RutaPartida);

            // la partida es la escena 0; el visor queda de segunda
            var lista = EditorBuildSettings.scenes.Where(s => s.path != RutaPartida).ToList();
            lista.Insert(0, new EditorBuildSettingsScene(RutaPartida, true));
            EditorBuildSettings.scenes = lista.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Merliot] Escena creada en {RutaPartida} y puesta como escena 0 del build.");
        }
    }
}
