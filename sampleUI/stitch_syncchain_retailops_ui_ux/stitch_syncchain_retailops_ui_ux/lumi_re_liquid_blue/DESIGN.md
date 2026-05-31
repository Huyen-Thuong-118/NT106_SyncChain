---
name: Lumière Liquid Blue
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#43474b'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#73787b'
  outline-variant: '#c3c7cb'
  surface-tint: '#50616b'
  primary: '#50616b'
  on-primary: '#ffffff'
  primary-container: '#e0f2fe'
  on-primary-container: '#5e6f79'
  inverse-primary: '#b7c9d5'
  secondary: '#565e74'
  on-secondary: '#ffffff'
  secondary-container: '#dae2fd'
  on-secondary-container: '#5c647a'
  tertiary: '#5c5f61'
  on-tertiary: '#ffffff'
  tertiary-container: '#eef0f2'
  on-tertiary-container: '#6a6d6f'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d3e5f1'
  primary-fixed-dim: '#b7c9d5'
  on-primary-fixed: '#0c1e26'
  on-primary-fixed-variant: '#384953'
  secondary-fixed: '#dae2fd'
  secondary-fixed-dim: '#bec6e0'
  on-secondary-fixed: '#131b2e'
  on-secondary-fixed-variant: '#3f465c'
  tertiary-fixed: '#e0e3e5'
  tertiary-fixed-dim: '#c4c7c9'
  on-tertiary-fixed: '#191c1e'
  on-tertiary-fixed-variant: '#444749'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
typography:
  display-xl:
    fontFamily: Source Serif 4
    fontSize: 72px
    fontWeight: '600'
    lineHeight: 80px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Source Serif 4
    fontSize: 48px
    fontWeight: '500'
    lineHeight: 56px
  headline-lg-mobile:
    fontFamily: Source Serif 4
    fontSize: 32px
    fontWeight: '500'
    lineHeight: 40px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 28px
  data-tabular:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.05em
  label-caps:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.1em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  fluid-gap: clamp(2rem, 5vw, 6rem)
  container-max: 1440px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 64px
---

## Brand & Style

The design system embodies "Digital Luxury"—a serene, high-end atmosphere that feels as fluid as water and as light as air. It is tailored for an elite audience that values editorial sophistication and technological precision. 

The visual style is a hybrid of **Minimalism** and **Glassmorphism**, characterized by:
- **Atmospheric Depth:** Using "Liquid" glass cards that float over high-fidelity product photography.
- **Luminous Surfaces:** A focus on light-refracting elements and soft, porcelain-like textures.
- **Editorial Presence:** Large-scale, high-contrast serif typography paired with clinical, data-driven sans-serif accents.
- **Sophisticated Calm:** A strictly cool-toned palette that avoids any vibrancy outside of the sapphire and light-blue spectrum, ensuring a sense of purity and premium quality.

## Colors

The palette is strictly monochromatic in hue but varied in value to create a sense of depth and luxury.

- **Primary (Luminous Blue):** `#E0F2FE` is used for large background washes, highlight states, and soft containers. It should feel like a tinted glow.
- **Secondary (Deep Sapphire):** `#0F172A` provides the "anchor." Used for high-impact buttons, primary navigation text, and deep footers to ground the airy layout.
- **Tertiary (Porcelain):** `#F8FAFC` acts as the base canvas, providing a slightly warmer-than-white clinical cleanliness.
- **Accent (Glass Stroke):** Use white at 40% opacity for borders on glass elements to simulate light catching an edge.

**Note:** Absolutely no green, teal, or yellow tones are permitted. All grays must be blue-tinted (slate/cool grays).

## Typography

The typography strategy relies on the tension between the classical **Source Serif 4** and the utilitarian **Inter**.

- **Editorial Headers:** Use Source Serif 4 for all marketing copy, headlines, and pull quotes. Keep tracking tight on large sizes.
- **Functional Data:** Use Inter for product specifications, pricing, navigation, and UI controls. 
- **The "Clinical" Look:** Use `label-caps` for section overlines (e.g., / INGREDIENTS) to provide a structured, scientific counter-balance to the flowing layout.

## Layout & Spacing

This design system uses a **Fluid Grid** model with "Liquid Flow" transitions.

- **Organic Asymmetry:** Break the 12-column grid frequently. Use wide-screen imagery that bleeds off the edge of the page, contrasted against tight, centered data columns.
- **Whitespace as Luxury:** Negative space is a functional element. Use the `fluid-gap` variable for vertical section spacing to ensure the UI feels "airy."
- **Data Grids:** While marketing sections are fluid, product and checkout pages should snap to a rigid 12-column grid for clarity and trust.
- **Mobile Reflow:** On mobile, organic overlaps should be simplified into a single vertical stack, but maintaining the high-contrast typography hierarchy.

## Elevation & Depth

Hierarchy is defined through **Glassmorphism** and transparency rather than heavy shadows.

- **Floating Glass:** Use backdrop-blur (minimum 20px) on surfaces with a 60% white fill. Surfaces should have a 1px solid white border at 30% opacity to define the edge.
- **Layering:** Level 1 is the porcelain background; Level 2 is the product photography; Level 3 is the floating glass UI card.
- **Shadows:** Use only one "Ambient" shadow style—extremely diffused (40px-60px blur), low opacity (5-8%), tinted with the Sapphire primary color to avoid "dirty" gray shadows.

## Shapes

The shape language is "Soft-Organic." 

- **Primary Containers:** Use `rounded-lg` (1rem) for most glass cards and input fields.
- **Buttons:** Use pill-shapes (rounded-full) for primary actions to contrast against the sharp editorial typography.
- **Image Treatment:** Product photography should either be full-bleed or use the "Liquid" mask—asymmetric, wavy container edges that mimic fluid movement (as seen in the reference images).

## Components

- **Buttons:** Primary buttons are Deep Sapphire with white Inter text. Secondary buttons are glass-filled with a Sapphire border.
- **Glass Cards:** The signature component. Always features a `backdrop-filter: blur(24px)` and a thin light-blue inner stroke.
- **Input Fields:** Minimalist. No background fill—only a bottom border (1px) in light blue, which thickens and darkens to Sapphire on focus.
- **Chips/Badges:** Small, pill-shaped elements with `#E0F2FE` backgrounds and Sapphire text in `label-caps` style.
- **Lists:** Product lists should use high-quality cut-out photography (PNGs) that slightly overlap the text containers to create depth.
- **Interactive Transitions:** Hover states on cards should involve a subtle scale-up (1.02x) and an increase in backdrop-blur intensity.