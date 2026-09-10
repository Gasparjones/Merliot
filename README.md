# MERLIOT

Juego de aventura por turnos con cartas y dados, ambientado en la región de Merliot.
Diseñado para mesa; el prototipo digital sirve para playtestear y ajustar números.

## Qué hay acá

| Carpeta | Qué contiene |
|---|---|
| `prototipo/` | El prototipo jugable. Abrí `index.html` en cualquier navegador. No necesita servidor. |
| `data/` | Todas las cartas, criaturas, lugares y efímeros en JSON. **Ésta es la fuente de verdad.** |
| `docs/` | Reglas y notas de diseño. |
| `unity/` | Scripts de C# para leer `data/merliot.json` desde Unity. |

## La regla más importante del repo

**Los números viven en `data/merliot.json`, no en el código.**
Si querés cambiar cuánto produce un héroe o cuánto cuesta un lugar, se toca ahí.
El prototipo HTML todavía tiene su copia embebida; cuando se migre a Unity,
el JSON pasa a ser el único lugar.

## Estado

Prototipo funcional y balanceado a mano. Falta: arte, más terrenos,
mapa de exploración, y la campaña de la semilla.

## Validar

```bash
node tools/validar.js
```

Chequea los invariantes y muestra cobertura del dado, curva de costos y reparto por raza.
Sale con código 1 si hay errores, así que sirve en un hook de pre-commit.

---

## El proyecto de Unity

Unity **6000.5.1f1**, template Universal 2D (URP + 2D Renderer), uGUI.

| Dónde | Qué |
|---|---|
| `Assets/Resources/merliot.json` | **el archivo real** de datos |
| `data/merliot.json` | symlink al anterior — `tools/validar.js` y el prototipo siguen andando igual |
| `Assets/Scripts/Merliot/` | `MerliotData`, `MerliotDatabase`, `CartaView`, `VisorDeCartas` |
| `Assets/Editor/Merliot/` | `MerliotValidator`, `CrearEscena` |
| `Assets/Scenes/Visor.unity` | la escena del visor, escena 0 del build |
| `Assets/Resources/Arte/Cartas/` | el arte, un archivo por `id` |

### Por qué el symlink y no una copia

`docs/pasar-a-unity.md` decía copiar el JSON a `Assets/Resources/`. Una copia son dos
archivos que se desincronizan el primer día. Unity necesita el archivo de verdad adentro
de `Assets/`, así que el real vive ahí y `data/merliot.json` lo apunta: **sigue habiendo
una sola fuente de verdad** y la regla de oro del `CLAUDE.md` se mantiene.

### La campaña

`Partida.cs` tiene el viaje entero: las tres etapas, el interludio (descanso, cofre,
dejar una carta atrás, elegir ruta, siguen sólo los primeros cinco), la semilla,
las mutaciones y la rotación del mazo de efímeros. Todo sale de `merliot.json`.

Dos cosas del prototipo web que acá se corrigieron, porque con la campaña se notan:

- El nombre del terreno estaba fijo en el HTML, así que en la etapa 2 seguía diciendo
  "Los Lindes de Urmand". Ahora sale de los datos.
- Los ids de terreno del prototipo (`lindes`) no coinciden con el del JSON
  (`lindes-de-urmand`). Se respetó el del JSON, que ya existía, y los nuevos usan
  los del prototipo.

### El visor

Abrí `Assets/Scenes/Visor.unity` y dale Play. Flechas para recorrer el mazo,
`1`-`5` para filtrar por tipo. Muestra nombre, coste, raza, vida, la tira del 2 al 12
con las bandas encendidas, y lo que produce cada banda. Sin arte todavía: es la lupa
para mirar los datos, no una pantalla de juego.

### Validar y testear

- **Merliot → Validar datos** en el menú del editor: corre los invariantes del `CLAUDE.md`.
- **Window → General → Test Runner → PlayMode**: chequea que el JSON llegue hasta la
  pantalla y que las 51 cartas se dibujen sin romperse.

Desde la terminal, sin abrir el editor:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity

$UNITY -batchmode -quit -nographics -projectPath . \
  -executeMethod Merliot.EditorTools.MerliotValidator.Validar -logFile Logs/validar.log

$UNITY -batchmode -nographics -projectPath . \
  -runTests -testPlatform PlayMode -testResults Logs/tests.xml -logFile Logs/tests.log
```

### El arte

`CartaView` busca `Resources/Arte/Cartas/{id}`. Ojo que `pasar-a-unity.md` dice
`Assets/Arte/Cartas/` — tiene que estar dentro de `Resources/` para que cargue por
nombre en runtime sin Addressables. Si no hay archivo, la carta muestra "sin arte",
que además sirve de checklist visual de lo que falta dibujar.
