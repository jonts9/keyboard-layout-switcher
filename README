# KeyboardLayoutSwitcher

Utilitário para Windows que troca automaticamente o layout do teclado com base na presença de um dispositivo USB específico.

Desenvolvido para um fluxo de trabalho com notebook + teclado mecânico externo: quando o hub USB-C é conectado (e com ele o receptor do teclado externo), o layout muda automaticamente para o teclado externo. Quando o hub é desconectado, volta para o layout do teclado built-in.

---

## Como funciona

A cada 3 segundos, o programa consulta o Windows para verificar se o receptor Bolt da Logitech (`VID_046D&PID_C548`) está presente e ativo. Com base nisso:

- **Receptor detectado** → ativa **POR-INTL** (teclado mecânico americano)
- **Receptor ausente** → ativa **POR-PTB2 / ABNT2** (teclado built-in do notebook)

Se o layout for trocado acidentalmente (ex: `Win+Space` sem querer), o programa corrige automaticamente no próximo ciclo.

O programa roda minimizado na bandeja do sistema (system tray) com um ícone colorido:
- 🔵 **Azul** — ativo, teclado externo conectado (INTL)
- ⚫ **Cinza** — ativo, teclado externo desconectado (ABNT2)
- 🟠 **Laranja** — pausado (troca automática desativada)

---

## Limitações

> ⚠️ **Este utilitário funciona apenas com exatamente dois layouts configurados no Windows.**

A troca de layout é feita simulando o atalho `Win+Space`, que alterna circularmente entre os layouts disponíveis. Com dois layouts, isso funciona de forma determinística — sempre vai para o único outro. Com três ou mais layouts, o programa não consegue garantir que vai parar no layout correto, e o comportamento seria imprevisível.

Para funcionar com mais de dois layouts seria necessário refatorar o algoritmo para usar `LoadKeyboardLayout` e `PostMessage` diretamente para cada janela aberta — o que aumentaria consideravelmente a complexidade.

**Configuração necessária no Windows:**
Certifique-se de que apenas dois layouts estão configurados em:
`Configurações → Hora e idioma → Digitação → Idiomas preferenciais → Português (Brasil) → Opções → Teclados`

---

## Requisitos

- Windows 10 ou 11 (64-bit)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) para compilar

Instalação do SDK via winget:
```
winget install Microsoft.DotNet.SDK.9
```

---

## Instalação

1. Clone o repositório:
```bash
git clone https://github.com/SEU_USUARIO/keyboard-layout-switcher.git
cd keyboard-layout-switcher
```

2. Execute o script de build:
```
build_e_instalar.bat
```

O executável será compilado e instalado em `%LOCALAPPDATA%\KeyboardLayoutSwitcher\`.

3. Para iniciar com o Windows: clique com botão direito no ícone da bandeja → **"Iniciar com o Windows"**.

---

## Personalização

Se você usa um dispositivo USB diferente ou outros layouts, edite as constantes no topo do `Program.cs`:

```csharp
const string DEVICE_VID_PID   = "VID_046D&PID_C548"; // ID do seu dispositivo USB
const int    POLL_INTERVAL_MS = 3000;                 // intervalo de verificação (ms)

const uint KLID_INTL  = 0xF0010416; // layout quando dispositivo conectado
const uint KLID_ABNT2 = 0xF0100416; // layout quando dispositivo desconectado
```

### Como descobrir o VID/PID do seu dispositivo

Com o dispositivo **conectado**, rode no PowerShell:
```powershell
Get-PnpDevice | Where-Object { $_.Status -eq "OK" -and $_.Class -eq "USB" } |
  Select-Object FriendlyName, InstanceId | Format-List
```
Procure o dispositivo desejado e copie a parte `VID_XXXX&PID_XXXX` do `InstanceId`.

### Como descobrir os KLIDs dos seus layouts

Com cada layout **ativo**, rode no PowerShell:
```powershell
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class KBD {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint t);
}
"@
$hwnd = [KBD]::GetForegroundWindow()
$tid  = [KBD]::GetWindowThreadProcessId($hwnd, [IntPtr]::Zero)
$hkl  = [KBD]::GetKeyboardLayout($tid)
"KLID: 0x{0:X8}" -f ([uint32][int32]$hkl.ToInt64())
```
Anote o valor para cada layout e use nas constantes `KLID_INTL` e `KLID_ABNT2`.

---

## Desinstalar

1. Clique com botão direito no ícone da bandeja → desmarque **"Iniciar com o Windows"**
2. Clique em **"Fechar"**
3. Delete a pasta `%LOCALAPPDATA%\KeyboardLayoutSwitcher\`