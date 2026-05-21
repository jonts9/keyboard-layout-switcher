# KeyboardLayoutSwitcher

Troca automaticamente o layout do teclado quando o hub USB-C é conectado ou desconectado.

- **Hub conectado** → receptor Bolt Logitech ativo → layout **POR-INTL** (teclado mecânico americano)
- **Hub desconectado** → layout **POR-PTB2 / ABNT2** (teclado built-in do notebook)

---

## Requisitos

- Windows 10 ou 11 (64-bit)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) para compilar

Se não tiver o .NET SDK, instale pelo winget:
```
winget install Microsoft.DotNet.SDK.8
```

---

## Como instalar

1. Baixe/copie esta pasta para o seu PC
2. Execute `build_e_instalar.bat` como administrador (ou usuário normal)
3. O executável será compilado e instalado em `%LOCALAPPDATA%\KeyboardLayoutSwitcher\`
4. Para iniciar com o Windows: clique com botão direito no ícone da bandeja → "Iniciar com o Windows"

---

## Personalização (Program.cs)

Se precisar ajustar, edite as constantes no topo do `Program.cs`:

```csharp
const string DEVICE_VID_PID   = "VID_046D&PID_C548";  // ID do receptor Bolt
const string LAYOUT_EXTERNAL  = "00000416";            // POR-INTL
const string LAYOUT_BUILTIN   = "00010416";            // POR-PTB2 / ABNT2
const int    POLL_INTERVAL_MS = 3000;                  // intervalo de verificação (ms)
```

---

## Como funciona

- A cada 3 segundos, consulta o WMI para verificar se algum dispositivo `VID_046D&PID_C548` tem `Status: OK`
- Se sim → ativa POR-INTL
- Se não → ativa POR-PTB2
- Roda minimizado na bandeja do sistema (system tray)
- Usa mutex para evitar múltiplas instâncias
- Não precisa de instalador — é um único `.exe` standalone

---

## Desinstalar

1. Clique com botão direito no ícone da bandeja → desmarque "Iniciar com o Windows"
2. Feche o programa
3. Delete a pasta `%LOCALAPPDATA%\KeyboardLayoutSwitcher\`
