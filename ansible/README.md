# Fleet provisioning

Placeholder. Real playbooks land with:

- **`site.yml`** and **`roles/kiosk/`** v0 in Phase 1 (IMPLEMENTATION_PLAN.md milestone 19) —
  Chromium + openbox + autologin, pointed at `/play`.
- Productionized in Phase 2 (milestone 29): USB SSD partition/format automation, `signage-agent`
  binary deploy, both systemd units (`../device-agent/systemd/`), an OS-update playbook.

SSH-based, no agent needed for imaging itself — see design doc §7.2 for why Ansible over a
self-hosted alternative like balenaCloud at this fleet size.
