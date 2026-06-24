# Design Tokens

## Colors

### Brand

```yaml
primary: "#7C3AED"
primary-hover: "#8B5CF6"
primary-focus: "#6D28D9"
primary-soft: "rgba(124, 58, 237, 0.16)"

secondary: "#2563EB"
secondary-hover: "#3B82F6"
secondary-soft: "rgba(37, 99, 235, 0.16)"
```

### Background

```yaml
canvas: "#09090B"
canvas-raised: "#0D0D12"
surface-1: "#111113"
surface-2: "#18181B"
surface-3: "#27272A"
surface-4: "#323238"
sidebar: "#07070A"
```

### Borders

```yaml
hairline: "#2A2A30"
hairline-strong: "#3A3A42"
hairline-soft: "rgba(58, 58, 66, 0.42)"
```

### Text

```yaml
ink: "#FAFAFA"
ink-muted: "#C9CED6"
ink-subtle: "#8B949E"
ink-disabled: "#5B616A"
```

### Semantic

```yaml
success: "#22C55E"
warning: "#F59E0B"
danger: "#EF4444"
info: "#3B82F6"
```

### Effects

```yaml
focus-ring: "rgba(139, 92, 246, 0.42)"
glow-primary: "rgba(124, 58, 237, 0.28)"
glow-secondary: "rgba(37, 99, 235, 0.22)"
overlay: "rgba(5, 5, 8, 0.72)"
```

---

# Typography

## Font Family

```yaml
display:
  Inter

body:
  Inter

mono:
  JetBrains Mono
```

---

## Display

```yaml
display-xl:
  size: 64px
  weight: 700

display-lg:
  size: 48px
  weight: 700

display-md:
  size: 36px
  weight: 600
```

Display deve ser usado com moderação em páginas de entrada, headers e momentos de destaque. Telas operacionais devem priorizar legibilidade.

---

## Headings

```yaml
h1: 32px
h2: 28px
h3: 24px
h4: 20px
```

---

## Body

```yaml
body-lg: 18px
body: 16px
body-sm: 14px
caption: 12px
```

Fontes mono devem ser usadas apenas para dados curtos: status, rotas, tags, contadores, ids visuais e navegação compacta.

---

# Radius

```yaml
xs: 4px
sm: 6px
md: 8px
lg: 12px
xl: 16px
xxl: 24px
pill: 9999px
```

---

# Spacing

```yaml
xxs: 4px
xs: 8px
sm: 12px
md: 16px
lg: 24px
xl: 32px
xxl: 48px
section: 96px
```

Escala operacional recomendada:

```yaml
control-height-sm: 36px
control-height-md: 42px
control-height-lg: 48px
container-padding-mobile: 16px
container-padding-desktop: 32px
```

---

# Elevation

## Level 1

```yaml
background: surface-1
border: hairline
```

## Level 2

```yaml
background: surface-2
border: hairline-strong
```

## Level 3

```yaml
background: surface-3
border: hairline-strong
```

Sombras devem ser sutis e raras. Preferir camadas, bordas e contraste de superfície.

```yaml
shadow-sm: "0 8px 24px rgba(0, 0, 0, 0.22)"
shadow-md: "0 18px 48px rgba(0, 0, 0, 0.32)"
shadow-lg: "0 28px 90px rgba(0, 0, 0, 0.48)"
```

---

# Motion

```yaml
duration-fast: 140ms
duration-base: 180ms
duration-slow: 240ms
ease-standard: "cubic-bezier(0.2, 0, 0, 1)"
```

Microinterações devem comunicar estado e affordance. Evitar animações decorativas repetitivas.

---

# Focus

Todo elemento interativo deve ter foco visível.

```yaml
focus-outline: "2px solid focus-ring"
focus-offset: 2px
```

---

# League Roles

## Top

```yaml
color: "#F97316"
```

## Jungle

```yaml
color: "#22C55E"
```

## Mid

```yaml
color: "#8B5CF6"
```

## ADC

```yaml
color: "#3B82F6"
```

## Support

```yaml
color: "#EC4899"
```

---

# Elo Colors

## Ferro

```yaml
#6B7280
```

## Bronze

```yaml
#B45309
```

## Prata

```yaml
#CBD5E1
```

## Ouro

```yaml
#FACC15
```

## Platina

```yaml
#14B8A6
```

## Esmeralda

```yaml
#10B981
```

## Diamante

```yaml
#60A5FA
```

## Mestre+

```yaml
#A855F7
```
