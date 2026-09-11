# Bajar el proyecto y abrirlo

Esto es para arrancar de cero en una máquina nueva. Son cuatro pasos y un rato de espera.

## 0. Antes que nada

El repo es **privado**. Gaspar te tiene que agregar como colaborador y te llega una
invitación por mail: aceptala antes de intentar clonar, o el `git clone` te va a decir
que el repositorio no existe.

Vas a necesitar además una **clave SSH** tuya cargada en tu cuenta de GitHub. Si nunca
armaste una: `ssh-keygen -t ed25519`, y después pegás el contenido de
`~/.ssh/id_ed25519.pub` en <https://github.com/settings/keys>. Para probar que quedó:
`ssh -T git@github.com` tiene que saludarte por tu nombre de usuario.

## 1. Instalar lo necesario

- **Git LFS.** El arte no viaja por Git común. Sin esto te bajás archivos de texto
  de 130 bytes en lugar de los PNG. En Mac: `brew install git-lfs`, o el instalador
  de <https://git-lfs.com>. Después, una sola vez por máquina:

  ```bash
  git lfs install
  ```

- **Unity 6000.5.1f1**, desde Unity Hub. Tiene que ser esa versión: si abrís el
  proyecto con otra, Unity te lo actualiza y eso ensucia el repo para todos.
  En el Hub: pestaña *Installs* → *Install Editor* → *Archive* si no aparece en la lista.

## 2. Clonar

```bash
git clone git@github.com:Gasparjones/Merliot.git
cd Merliot
git lfs pull
```

`git lfs pull` trae el contenido real de los PNG. El `clone` normalmente ya lo hace
solo, pero si abriste el proyecto y las cartas se ven en blanco, correlo a mano:
es siempre eso.

Para saber si quedó bien:

```bash
git lfs ls-files
```

Tiene que listar los PNG. Si la lista sale vacía y en `Assets/Resources/Arte/` hay
archivos, LFS no está andando.

## 3. Abrir desde Unity Hub

*Add* → *Add project from disk* → elegí la carpeta `merliot` (la de arriba de todo,
la que tiene `Assets/` adentro). Después abrilo con **6000.5.1f1**.

**La primera vez tarda: pueden ser diez o quince minutos, y parece colgado.**
Unity está reconstruyendo la carpeta `Library/`, que son los assets importados y
compilados. Esa carpeta no está en el repo a propósito — pesa cientos de megas y se
regenera sola. No canceles, no toques nada, dejalo terminar. Las veces siguientes
abre en segundos.

Si te pide actualizar la versión del proyecto, **decí que no** y verificá que
instalaste la 6000.5.1f1.

## 4. Ver que ande

Abrí `Assets/Scenes/Visor.unity` y dale Play. Deberías ver una carta con sus bandas.
Flechas para recorrer el mazo. Si eso anda, el proyecto está sano.

---

## Cómo trabajamos con el arte

**Los archivos fuente (PSD, PSB, AI, TIFF) no van al repo.** Están ignorados a
propósito: GitHub nos da 1 GB de LFS y 1 GB de tráfico por mes, y un par de PSD en
capas se lo comen en semanas. Los fuentes viven en Drive.

Al repo va **sólo el PNG exportado**, que es lo que lee Unity:

```
Assets/Resources/Arte/Cartas/{id}.png
```

El `{id}` es el de la carta en `data/merliot.json` — `brenna-la-arquera`,
`broten-mayor`. Tiene que coincidir exacto o la carta se muestra sin arte.
**El `id` no se cambia nunca**, ni aunque se renombre la carta: el arte se busca por
ahí. Si falta el archivo, la carta dice "sin arte", que sirve de checklist de lo que
queda por dibujar.

Para saber qué falta dibujar: `python3 tools/editor.py`, y el filtro *sólo sin arte*.

## Dos cosas para no pisarnos

- **Avisá antes de tocar una escena** (`.unity`) o un prefab. Son texto, pero si los
  editamos los dos a la vez el conflicto es muy feo de resolver a mano.
- **Los `.meta` se commitean siempre**, junto con el archivo que acompañan. Si mandás
  un PNG sin su `.meta`, del otro lado Unity le inventa uno nuevo y se rompen las
  referencias.

## Sublime Merge

Anda bien con LFS sin configurar nada. Dos detalles:

- Los PNG los vas a ver en el diff como un puntero de tres líneas (`oid sha256:…`).
  Es lo normal: el archivo real lo maneja LFS aparte.
- La carpeta `Library/` no tiene que aparecer nunca en los cambios. Si aparece,
  algo se rompió en el `.gitignore`; avisá antes de commitear.

## Si algo sale mal

| Qué ves | Qué es |
|---|---|
| Las cartas salen en blanco | Falta `git lfs pull` |
| Unity tarda muchísimo la primera vez | Normal, está armando `Library/` |
| Unity pide actualizar la versión | Instalá 6000.5.1f1 y decí que no |
| Aparecen miles de archivos como cambios | Se abrió con otra versión de Unity |
| Un PNG pesa 130 bytes | Es el puntero de LFS: falta `git lfs install` |

Lo demás está en el `README.md` y, para las reglas del juego, en `docs/reglas.md`.
