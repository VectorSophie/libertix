#!/bin/bash
# Render complete menus with a given render-libertix-menu.py and run grub-script-check on them.
# Usage: script-check.sh <label> <renderer.py> <translations.json>
set -uo pipefail
label=$1; renderer=$2; translations=$3
prefix=/lbx-grub-1/root
export PATH=$prefix/usr/bin:$PATH LD_LIBRARY_PATH=$prefix/usr/lib/x86_64-linux-gnu
work=/lbx-work/script-check/$label
mkdir -p "$work"
cp "$renderer" "$work/render-libertix-menu.py"
cp "$translations" "$work/Libertix.Translations.json"
cd "$work"

classes="--class linux --class gnu-linux --class gnu --class os"
cat > linux.cfg <<EOF
menuentry 'Linux Mint 22.3 Cinnamon' $classes \$menuentry_id_option 'gnulinux-simple-u1' {
	recordfail
	linux /vmlinuz root=UUID=u1 ro quiet splash
}
submenu 'Options avanc'\''ees pour Linux Mint' \$menuentry_id_option 'gnulinux-advanced-u1' {
	menuentry 'Linux Mint, avec Linux 6.8.0-90-generic' $classes \$menuentry_id_option 'gnulinux-6.8.0-90-generic-advanced-u1' {
		linux /vmlinuz-6.8.0-90-generic root=UUID=u1 ro
	}
	menuentry 'Linux Mint, avec Linux 6.8.0-90-generic (mode de d'\''epannage)' $classes \$menuentry_id_option 'gnulinux-6.8.0-90-generic-recovery-u1' {
		linux /vmlinuz-6.8.0-90-generic root=UUID=u1 ro recovery nomodeset
	}
}
EOF
cat > windows.cfg <<'EOF'
menuentry "Windows Boot Manager" --class windows --class os --id libertix-windows {
	chainloader /EFI/Microsoft/Boot/bootmgfw.efi
}
EOF
cat > firmware.cfg <<'EOF'
if [ "$grub_platform" = "efi" ]; then
	fwsetup --is-supported
	if [ "$?" = 0 ]; then
		menuentry 'UEFI Firmware Settings' $menuentry_id_option 'uefi-firmware' {
			fwsetup
		}
	fi
fi
EOF
cat > memtest.cfg <<'EOF'
menuentry "Memory test (memtest86+x64.efi)" --class memtest $menuentry_id_option 'memtest86+' { # memtest86+ 7.00
	linux /boot/memtest86+x64.efi
}
EOF
cat > custom.cfg <<'EOF'
menuentry 'Custom tool' --id 'custom' { echo hello; }
menuentry "Say \"{hi}\" now" --id "custom-quoted" {
	echo hi
}
menuentry 'Tool {beta}' --id 'custom-brace' {
	echo beta
}
EOF
cat > plan.json <<'EOF'
{"distribution": {"grubDisplayName": "Linux Mint 22.3 Cinnamon", "grubIcon": "linuxmint"},
 "locale": {"languageCode": "fr"}}
EOF
for input in linux.cfg windows.cfg firmware.cfg memtest.cfg custom.cfg; do
  grub-script-check "$input" || echo "INPUT $input rejected"
done
python3 render-libertix-menu.py --linux linux.cfg --windows windows.cfg --firmware firmware.cfg \
  --extra memtest.cfg --extra custom.cfg --plan plan.json > grub.cfg
echo "== $label: grub-script-check on the rendered menu"
grub-script-check grub.cfg && echo "accepted" || echo "REJECTED (exit $?)"
grep -nE 'menuentry|submenu' grub.cfg | grep -E 'custom|memtest|avanc' | sed 's/^/  /'
