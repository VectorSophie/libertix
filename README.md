# Evidence for the GRUB icon, allocation screen and distribution card PRs

This branch only holds screenshots and the throwaway harnesses that produced them. It is not meant
to be merged. Every image was made from upstream `dev` at
`91bd68483ed89a783cb18c7b7900eaa5834e1175` ("before") or from the PR branch named in the file.

## GRUB menu (`grub/`)

Real GRUB, not a mockup. `tools/grub-vm.sh` builds a GRUB rescue ISO in WSL Ubuntu 24.04 with
`grub-mkrescue` from the noble `grub-common` / `grub-pc-bin` / `grub-efi-amd64-bin`
2.12-1ubuntu7.3 packages, the theme produced by the branch's own
`assets/grub-theme/generate-theme.sh 1024x768`, and a menu produced by the branch's own
`grub/render-libertix-menu.py`. The renderer input is shaped like the noble `10_linux`,
`20_memtest86+` (memtest86+ 7.00) and `30_uefi-firmware` output for Linux Mint 22.3, plus one
entry the renderer cannot classify. QEMU boots it with no disks attached, under SeaBIOS and under
OVMF 2024.02, and `tools/drive.py` captures the root menu, Advanced options and the distribution's
nested submenu through the QEMU monitor.

- `grub-uefi-comparison.png`, `grub-bios-comparison.png`: rows are upstream `dev`, the #39 fix
  (`9e33ebd`) and the MingCute proposal (`3ceb8bf`).

This is not an installed Linux Mint or Zorin system, so it does not cover kernel updates,
`update-grub` or real firmware.

## Allocation screen (`allocation/`) and distribution cards (`distribution/`)

Real WPF rendering of the pages, but offscreen. `tools/RenderPage.cs` is compiled into a copy of
each build's `bin\Release` (`tools/build-renderer.ps1`), creates `Libertix.App` resources without
running `OnStartup`, builds the page with fixed test data, lays it out at the window's client size
(1054x591 DIP at 96 DPI, or 1054x559 below the development-mode banner) and saves it with
`RenderTargetBitmap`. The banner is composited with the same colours as `MainWindow.xaml`.

- Test data: `C:` 476 GiB with 168 GiB free, 120 GiB shrinkable; `D:` 931 GiB with 610 GiB free on
  a second disk; Linux Mint 22.3 selected. File names give the scenario and language.
- `workB-resize-drag-en.txt` records a drag: `DragStarted`, `DragDelta(-200 px)` and
  `DragCompleted` raised on the slider's own `Thumb`, which goes through WPF's
  `Slider.OnThumbDragDelta`. Mouse capture and real pointer input were not exercised.
- The distribution screenshots are local copies of the two catalogue `imageUrl` files
  (`screens\mint.jpg`, `screens\zorin.png`, not included here), so the harness does not let page
  code run as the live application would.

These are not screenshots of the running elevated application, and they were rendered at 96 DPI
only.
