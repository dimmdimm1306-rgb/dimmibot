---
name: Fiber Infrastructure Design System
colors:
  surface: '#f9f9fe'
  surface-dim: '#dad9de'
  surface-bright: '#f9f9fe'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f4f3f8'
  surface-container: '#eeedf2'
  surface-container-high: '#e8e8ed'
  surface-container-highest: '#e2e2e7'
  on-surface: '#1a1c1f'
  on-surface-variant: '#43474f'
  inverse-surface: '#2f3034'
  inverse-on-surface: '#f1f0f5'
  outline: '#737780'
  outline-variant: '#c3c6d1'
  surface-tint: '#3a5f94'
  primary: '#001e40'
  on-primary: '#ffffff'
  primary-container: '#003366'
  on-primary-container: '#799dd6'
  inverse-primary: '#a7c8ff'
  secondary: '#006875'
  on-secondary: '#ffffff'
  secondary-container: '#00e3fd'
  on-secondary-container: '#00616d'
  tertiary: '#381300'
  on-tertiary: '#ffffff'
  tertiary-container: '#592300'
  on-tertiary-container: '#d8885c'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d5e3ff'
  primary-fixed-dim: '#a7c8ff'
  on-primary-fixed: '#001b3c'
  on-primary-fixed-variant: '#1f477b'
  secondary-fixed: '#9cf0ff'
  secondary-fixed-dim: '#00daf3'
  on-secondary-fixed: '#001f24'
  on-secondary-fixed-variant: '#004f58'
  tertiary-fixed: '#ffdbca'
  tertiary-fixed-dim: '#ffb690'
  on-tertiary-fixed: '#341100'
  on-tertiary-fixed-variant: '#723610'
  background: '#f9f9fe'
  on-background: '#1a1c1f'
  surface-variant: '#e2e2e7'
typography:
  h1:
    fontFamily: Space Grotesk
    fontSize: 48px
    fontWeight: '700'
    lineHeight: '1.1'
    letterSpacing: -0.02em
  h2:
    fontFamily: Space Grotesk
    fontSize: 32px
    fontWeight: '600'
    lineHeight: '1.2'
  h3:
    fontFamily: Space Grotesk
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: '1.6'
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.5'
  label-caps:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '700'
    lineHeight: '1'
    letterSpacing: 0.08em
  mono-data:
    fontFamily: Space Grotesk
    fontSize: 14px
    fontWeight: '500'
    lineHeight: '1.4'
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 4px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 48px
  container-max: 1280px
  gutter: 24px
---

## Brand & Style

This design system is engineered for a fiber optic contractor, blending the precision of high-tech data transmission with the grit of physical infrastructure. The brand personality is **authoritative, resilient, and cutting-edge**. It aims to evoke a sense of absolute reliability—the "backbone" of modern communication.

The visual style is **Corporate / Modern** with a **Technical** edge. It utilizes high-contrast accents and structured layouts to reflect engineering blueprints. The aesthetic is clean enough for the boardroom but rugged enough to feel at home on a construction site tablet, emphasizing clarity, safety, and speed.

## Colors

The palette is rooted in the "Deep Tech Blue" of corporate stability, contrasted by "Electric Cyan" to represent the literal transmission of light through fiber. "Safety Orange" is used sparingly but impactfully to denote action, infrastructure, and field-ready visibility.

- **Primary (Deep Tech Blue):** Used for headers, primary buttons, and authoritative branding elements.
- **Secondary (Electric Cyan):** Used for data visualization, progress bars, and high-tech accents that signify "live" data.
- **Accent (Safety Orange):** Reserved for Call-to-Actions (CTAs), safety warnings, and field status indicators.
- **Neutrals:** Dark Graphite is used for primary text; Metallic Silver is used for borders and secondary UI elements; Off-White provides a clean, non-glare canvas for dashboard environments.

## Typography

This design system utilizes a dual-font approach to balance engineering aesthetics with readability. 

- **Headlines:** **Space Grotesk** provides a geometric, technical feel that mirrors precision instrumentation. Its unique glyphs give the brand a modern, futuristic edge.
- **Body & Interface:** **Inter** is the workhorse font, selected for its exceptional legibility on small screens and rugged mobile devices used in the field. 
- **Labels:** Uppercase labels with increased tracking are used for technical specifications and form headers to ensure they are easily scannable under harsh lighting conditions.

## Layout & Spacing

The layout philosophy follows a **Fixed-Fluid Hybrid Grid**. Content is housed in a 12-column grid with a maximum width of 1280px for desktop, while mobile views utilize a single-column layout with generous 24px side margins.

A strict 4px baseline grid ensures vertical rhythm. Spacing between major sections should use the `xl` (48px) unit to maintain the "Corporate" breathability, while data-heavy components (like project lists or fiber maps) should utilize `sm` and `md` units to maximize information density.

## Elevation & Depth

To maintain an industrial feel, the design system avoids heavy, soft shadows. Instead, it uses **Tonal Layering** and **Low-Contrast Outlines**.

1.  **Surfaces:** The primary background is Off-White. Secondary containers (cards) use a White fill with a 1px Metallic Silver (#C0C0C0) border.
2.  **Elevation:** When an element must appear elevated (e.g., a modal or a floating action button), use a sharp, short shadow with a slightly blue tint: `0px 4px 12px rgba(0, 51, 102, 0.15)`.
3.  **Active States:** Interactive elements like pressed buttons or selected cards utilize a slight inset shadow or a 2px Electric Cyan border to indicate focus without breaking the "flat-industrial" aesthetic.

## Shapes

The shape language is **Soft (0.25rem)**. This subtle rounding suggests precision manufacturing. Sharp corners (0px) are used exclusively for decorative line elements or "data-grid" separators to maintain a rugged, technical vibe. 

- **Standard Buttons & Inputs:** 4px (0.25rem) radius.
- **Feature Cards:** 8px (0.5rem) radius to soften larger surfaces.
- **Badges/Status Tags:** Fully pill-shaped (rounded-full) to distinguish them from interactive buttons.

## Components

### Buttons
- **Primary:** Deep Tech Blue background with White text. Bold, sans-serif.
- **Action/Safety:** Safety Orange background with White text. Used for "Start Work," "Emergency," or "Submit Bid."
- **Ghost:** Transparent background with 1px Metallic Silver border and Tech Blue text.

### Inputs & Forms
Inputs use a white background with a 1px #C0C0C0 border. On focus, the border transitions to 2px Electric Cyan. Error states use a 2px Error Red border.

### Cards & Modules
Cards should be white-filled with a subtle 1px border. Use a "Technical Header" style: a 4px left-hand border strip in Electric Cyan or Deep Tech Blue to categorize the card content.

### Status Indicators
Fiber-specific status chips:
- **Active/Lit:** Success Green dot with soft green background.
- **Dark Fiber:** Dark Graphite background with Metallic Silver text.
- **Maintenance:** Warning Yellow background with Black text.

### Additional Components
- **Data Tables:** High-density tables with alternating row tints (Off-White and White) and monospaced "Space Grotesk" digits for coordinates and decibel readings.
- **Infrastructure Map Pins:** Safety Orange pins with high-contrast white icons for visibility over satellite imagery.