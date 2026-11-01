"""Drive the QEMU monitor: capture the root menu, open Advanced options, then the nested submenu."""

import socket
import sys
import time

sock_path, prefix = sys.argv[1], sys.argv[2]


def command(text: str, pause: float = 0.4) -> None:
    # One connection per command; QEMU's server chardev accepts the next client.
    for _ in range(300):
        try:
            with socket.socket(socket.AF_UNIX) as monitor:
                monitor.connect(sock_path)
                monitor.settimeout(2)
                monitor.recv(4096)  # banner
                monitor.sendall(text.encode() + b"\n")
                time.sleep(0.2)
                break
        except OSError:
            time.sleep(0.1)
    else:
        raise SystemExit(f"monitor unavailable for: {text}")
    time.sleep(pause)


time.sleep(25)  # firmware plus GRUB theme load
command(f"screendump {prefix}-1-root.ppm", 1)
for _ in range(3):  # Linux, Windows, Shutdown, Advanced options
    command("sendkey down")
command("sendkey ret", 3)
command(f"screendump {prefix}-2-advanced.ppm", 1)
command("sendkey ret", 3)  # first advanced entry is the distribution's own submenu
command(f"screendump {prefix}-3-nested.ppm", 1)
command("quit")
