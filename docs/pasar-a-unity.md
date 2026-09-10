# Pasar esto a Unity

## Antes de abrir Unity

Pensalo dos veces. Este es un juego de cartas por turnos: no tiene física,
ni animación pesada, ni 3D. Unity te da build a Steam y a mobile, y un pipeline
de assets cómodo para dos ilustradores. Si no vas a necesitar eso pronto,
el prototipo HTML ya playtestea igual y no te cuesta meses.

Lo que sí conviene hacer siempre, uses lo que uses: **mantener los datos afuera del código**.
Eso ya está hecho en `data/merliot.json`.

## Si vas con Unity

1. Proyecto nuevo, 2D, Unity 2022 LTS o superior.
2. Copiá `unity/Runtime/*.cs` a `Assets/Scripts/`.
3. Copiá `unity/Editor/*.cs` a `Assets/Editor/`.
4. Copiá `data/merliot.json` a `Assets/Resources/merliot.json`.
5. GameObject vacío en la escena con el componente `MerliotDatabase`.
6. Menú **Merliot → Validar datos** para chequear que el JSON esté sano.

`MerliotDatabase` ya te resuelve tres cosas que son fáciles de arruinar:
armar el mazo con las copias correctas, calcular la producción de un héroe
contando mejoras y multiplicadores de raza, y la regla de conversión de dos por uno.

## El orden en que yo lo haría

1. Mostrar una carta con sus bandas leídas del JSON. Nada más.
2. La fila de héroes con posición y vida.
3. Tirar 2d6 y que se enciendan las bandas correctas.
4. Gastar recursos.
5. El lado enemigo.

Si te trabás en el paso 1, el problema es de setup y no de diseño.

## Sobre el arte

Cada carta ya tiene un `id` estable (`brenna-la-arquera`, `broten-mayor`).
Nombrá los archivos con ese id y la carga es automática:
`Assets/Arte/Cartas/brenna-la-arquera.png`.

No cambies los `id` cuando renombres una carta para castellano o estilo —
cambiá el `nombre`, dejá el `id` quieto.
