# Evidence for fork PRs #7, #8 and #9

This branch only holds screenshots, test records and the throwaway harnesses that produced
them. It is not meant to be merged. "Before" always means upstream `dev` at
`91bd68483ed89a783cb18c7b7900eaa5834e1175`.

| Folder | PR | Commit shown |
|---|---|---|
| `grub/` | #7 | `9e33ebd` (#39 fix) and `42d6dd8` (current head: fix, MingCute proposal, declaration fix) |
| `allocation/` | #8 | `workB-*`: `7578ea2` (current head) |
| `distribution/` | #9 | `workC-*`: `9a3aca1` (current head) |

## GRUB menu (`grub/`): real GRUB in a disposable VM

`tools/grub-vm.sh` builds a GRUB rescue ISO in WSL Ubuntu 24.04 with `grub-mkrescue` from the
noble GRUB 2.12-1ubuntu7.3 packages, the theme produced by the commit's own
`assets/grub-theme/generate-theme.sh 1024x768`, and a menu produced by the commit's own
`grub/render-libertix-menu.py` from input shaped like the noble `10_linux`, `20_memtest86+`
(memtest86+ 7.00) and `30_uefi-firmware` output, plus one entry the renderer cannot classify.
QEMU boots it with no disks attached under SeaBIOS and OVMF 2024.02, and `tools/drive.py` captures
the root menu, Advanced options and the nested submenu through the QEMU monitor.

- `grub-uefi-comparison.png`, `grub-bios-comparison.png`: rows are `91bd684`, `9e33ebd`, `42d6dd8`.
- `grub-script-check.txt`: `tools/script-check.sh` renders a complete menu that includes a
  one-line entry body, a comment after the opening brace, and titles with escaped quotes and
  braces, then runs `grub-script-check`. The previous head `3ceb8bf` is rejected; `42d6dd8` is
  accepted. Every input snippet is accepted on its own.

This is not an installed Mint or Zorin system: no kernel update, `update-grub` or real firmware.

## Allocation screen (`allocation/`)

Offscreen renders: `tools/RenderPage.cs` is compiled into a copy of each build's
`bin\Release`, builds the page from fixed test data, lays it out at the window's client size
(1054x591 DIP, or 1054x559 below the development-mode banner) and saves it with
`RenderTargetBitmap` at 96 DPI. These are real WPF renders of the page, not captures of the
running application. Test data: `C:` 476 GiB with 168 GiB free and 120 GiB shrinkable, `D:` 931 GiB
with 610 GiB free on a second disk, Linux Mint 22.3 selected.

- `workB-resize-drag-en.txt`: `DragStarted`/`DragDelta(-200 px)`/`DragCompleted` raised on the
  slider's own `Thumb` offscreen (WPF's drag handling, no pointer).
- `real-input-record.txt`: OS-injected mouse and keyboard input in a real window at the host's
  125% scaling (120 DPI), for base and #8, with the measured offset between the rendered Thumb
  centre and the rendered boundary after every step. Read its caveat: those runs started the
  window with `Application.Run`, which also ran Libertix's `App.OnStartup` in the harness and
  showed its permission prompt (left unanswered; nothing was written). One later run, after that
  was removed, sent its input while its window was not in front, so the input went to another
  window; nothing from it is used here, and `RealInput.cs` now refuses to send input unless its
  own window is in the foreground.

Not covered: other DPI settings (the host's scaling was not changed), touch, Narrator, the
running elevated application.

## Distribution cards (`distribution/`)

Same offscreen method. The two screenshots in the cards are local copies of the catalogue
`imageUrl` files (`screens\mint.jpg`, `screens\zorin.png`, not included), so no page code that
would run in the live application (catalogue loading, logging) is reached.

`AutomationProperties.Name` is checked in a C# test; that establishes the accessible name the
item exposes. It does not show what a particular screen reader announces, which was not tested.
