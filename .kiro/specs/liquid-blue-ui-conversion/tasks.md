# Implementation Plan: Liquid Blue UI Conversion

## Overview

This plan converts the SyncChain Desktop application from its current UI styling to the "Lumière Liquid Blue" glassmorphism design system. The approach is bottom-up: first establish design tokens and resource dictionaries, then restyle the Shell navigation, and finally convert each page XAML to use the new styles. All text is hardcoded in Vietnamese. The existing navigation structure, data models, and code-behind logic remain intact.

## Tasks

- [x] 1. Set up foundation resources and fonts
  - [x] 1.1 Add font assets and register in MauiProgram.cs
    - Add `SourceSerif4-Medium.ttf`, `SourceSerif4-SemiBold.ttf`, `Inter-Regular.ttf`, `Inter-SemiBold.ttf`, `Inter-Bold.ttf`, and `MaterialSymbolsOutlined.ttf` to `Resources/Fonts/`
    - Register all fonts in `MauiProgram.cs` using `.ConfigureFonts()` with aliases: `SourceSerif4`, `Inter`, `MaterialSymbols`
    - _Requirements: 1.2, 13.1_

  - [x] 1.2 Create Liquid Blue color tokens in Colors.xaml
    - Replace or overwrite `Resources/Styles/Colors.xaml` with all Liquid Blue palette colors
    - Define color keys: Primary (#50616b), PrimaryContainer (#e0f2fe), Surface (#f8f9ff), OnSurface (#0b1c30), OnSurfaceVariant (#43474b), Outline (#73787b), OutlineVariant (#c3c7cb), Error (#ba1a1a), Secondary (#565e74), SurfaceContainerLowest (#ffffff), SurfaceContainerLow (#eff4ff), SurfaceContainer (#e5eeff), SurfaceContainerHigh (#dce9ff), SurfaceContainerHighest (#d3e4fe), GlassWhite60 (#99FFFFFF), GlassBorder30 (#4DFFFFFF), StatusEmerald (#dcfce7), StatusAmber (#fef3c7), StatusBlue (#dbeafe), StatusSlate (#f1f5f9)
    - Define spacing constants: GutterSpacing (24), MarginDesktop (64), MarginMobile (16)
    - Define corner radius values: CornerRadiusSm (4), CornerRadiusDefault (8), CornerRadiusMd (12), CornerRadiusLg (16), CornerRadiusXl (24), CornerRadiusFull (9999)
    - _Requirements: 1.1, 1.3, 1.4_

  - [x] 1.3 Create Liquid Blue styles in Styles.xaml
    - Replace or overwrite `Resources/Styles/Styles.xaml` with all Liquid Blue style definitions
    - Define typography styles: DisplayXlLabel (SourceSerif4, 72px, 600), HeadlineLgLabel (SourceSerif4, 48px, 500), HeadlineLgMobileLabel (SourceSerif4, 32px, 500), BodyMdLabel (Inter, 16px, 400), DataTabularLabel (Inter, 14px, 600), LabelCapsLabel (Inter, 12px, 700, TextTransform=Uppercase)
    - Define GlassCardBorder style: BackgroundColor GlassWhite60, Stroke GlassBorder30, StrokeShape RoundRectangle 24, Shadow with offset and opacity
    - Define PillPrimaryButton: CornerRadius 9999, BackgroundColor Primary, TextColor White, FontFamily Inter, FontSize 12, TextTransform Uppercase
    - Define PillSecondaryButton: CornerRadius 9999, BackgroundColor GlassWhite60, BorderColor Primary
    - Define MinimalistEntry: BackgroundColor Transparent, bottom border in OutlineVariant
    - Define StatusBadgePill and CategoryBadgePill styles
    - Define LiquidGradientBrush (LinearGradientBrush 135°: #f8f9ff → #e0f2fe → #eff4ff)
    - Define ContentPageStyle applying LiquidGradientBrush as page background
    - _Requirements: 1.2, 1.5, 13.1, 13.2, 13.3, 13.4, 13.5_

  - [x] 1.4 Update App.xaml to reference new resource dictionaries
    - Ensure `App.xaml` merges the updated `Colors.xaml` and `Styles.xaml` resource dictionaries
    - Remove any references to old style files if they were renamed
    - _Requirements: 1.1, 1.5_

- [x] 2. Checkpoint - Verify foundation builds
  - Ensure all resource dictionaries compile without errors, fonts are registered, and the app launches with the gradient background visible. Ask the user if questions arise.

- [x] 3. Restyle AppShell navigation
  - [x] 3.1 Convert AppShell.xaml to Liquid Blue glassmorphism flyout
    - Set flyout background to PrimaryContainer at 40% opacity
    - Create FlyoutHeader with SyncChain logo (primary-colored rounded-xl icon container, white icon) and brand name in SourceSerif4
    - Create FlyoutContent with navigation items using MaterialSymbols icons and LabelCapsLabel Vietnamese text: "Bảng điều khiển", "Sản phẩm", "Đơn hàng", "Tạo đơn hàng", "Nhập hàng", "Nhật ký", "Tin nhắn", "Người dùng & phân quyền"
    - Implement active item highlighting: PrimaryContainer background, Primary text color, bold weight, subtle shadow
    - Create FlyoutFooter with "Hỗ trợ" and "Đăng xuất" links separated by a top border at 20% white opacity
    - Preserve all existing ShellContent routes and navigation structure
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 12.1_

  - [ ]* 3.2 Write unit tests for AppShell navigation structure
    - Verify all navigation routes are preserved after restyling
    - Verify flyout items count and labels match specification
    - _Requirements: 2.3, 12.1_

- [ ] 4. Convert primary content pages
  - [x] 4.1 Convert DashboardPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Render page header with "Bảng điều khiển" in HeadlineLgLabel and welcome message
    - Create Bento_Grid statistics section with GlassCardBorder containers showing key metrics (total products, active orders, pending imports, messages) with icons and large numeric values
    - Create recent activity section in GlassCardBorder with event list, timestamps, and status indicators
    - Create quick-action cards grid for navigation shortcuts
    - All text in Vietnamese
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

  - [x] 4.2 Convert ProductsPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create hero section: full-width rounded container (24px radius), overlay gradient, featured product badge, DisplayXlLabel headline, BodyMdLabel description, PillPrimaryButton and PillSecondaryButton action buttons
    - Create Bento_Grid section (2:1 column ratio) with GlassCardBorder containers, SourceSerif4 headlines, descriptive text
    - Create inventory data table in GlassCardBorder: LabelCapsLabel headers ("Sản phẩm", "Mã lô", "Danh mục", "Tồn kho", "Trạng thái", "Sức khỏe")
    - Create product rows with thumbnail, name, batch ID (DataTabularLabel), CategoryBadgePill, stock count, status dot + label, health progress bar
    - Add pagination with "Trước"/"Tiếp" pill buttons and Vietnamese record count label
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 12.3_

  - [x] 4.3 Convert ProductDetailPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create hero image section with product photo, overlay gradient, DisplayXlLabel product title
    - Create specifications GlassCardBorder with key-value pairs in DataTabularLabel
    - Create regional distribution section in GlassCardBorder grid
    - Create stock trends section with visual representation
    - All labels and headings in Vietnamese
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 4.4 Convert OrdersPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create page header: "Quản lý Đơn hàng" in DisplayXlLabel, "Xuất báo cáo" PillPrimaryButton
    - Create operational velocity GlassCardBorder: large metric value, percentage change, throughput/latency sub-metrics, "Thời gian thực" badge
    - Create summary statistics GlassCardBorder: "Đơn đang vận chuyển", "Chờ duyệt", "Thời gian giao trung bình" with DataTabularLabel values
    - Create order table in GlassCardBorder: columns "Mã đơn", "Điểm đến", "Sản phẩm", "Trạng thái", "Giá trị (VNĐ)" with StatusBadgePill
    - Create contextual insight cards in 2-column grid with icon, title, description
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 12.2, 12.4_

  - [x] 4.5 Convert OrderDetailPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create shipping timeline with step indicators (completed/in-progress/pending), location labels, timestamps
    - Create itemized product list in GlassCardBorder: thumbnail, name, quantity, unit price
    - Create financial summary section: subtotal, shipping, tax, total in DataTabularLabel
    - All labels, statuses, headings in Vietnamese
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 12.2_

- [x] 5. Checkpoint - Verify primary pages
  - Ensure all primary content pages compile, render with correct gradient background and glass cards, and navigation between pages works. Ask the user if questions arise.

- [ ] 6. Convert operational pages
  - [x] 6.1 Convert ImportsPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create page header: "Quản lý Đơn nhập hàng" title, "Tạo Đơn Nhập Mới" PillPrimaryButton
    - Create 4-column statistics grid with GlassCardBorder: "Tổng kiện hàng", "Đang trên biển", "Đợi thông quan", "Hoàn tất nhập kho" with DisplayXlLabel numeric values
    - Create live tracking timeline in GlassCardBorder: completed/in-transit/pending waypoints with vertical line connector
    - Create map/visual section as large rounded container with overlay GlassCardBorder showing vessel info
    - Create shipment table: "Mã vận đơn", "Nhà cung cấp", "Hàng hóa", "Ngày đến (dự kiến)", "Trạng thái", "Quản lý" with Vietnamese StatusBadgePill and pagination
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 12.4, 12.5_

  - [x] 6.2 Convert CreateOrderPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create form layout: logistics overview section, item manifest section, summary sidebar
    - Style all inputs with MinimalistEntry: no background, bottom border in light blue, thickens on focus
    - Use GlassCardBorder for each form section with appropriate spacing (GutterSpacing) and CornerRadiusXl
    - Add PillPrimaryButton action buttons with LabelCapsLabel text
    - All form labels, placeholders, button text in Vietnamese
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [x] 6.3 Convert ChatPage.xaml to Liquid Blue design
    - Apply ContentPageStyle with LiquidGradientBrush background
    - Create two-panel layout: fixed-width chat list panel + flexible conversation panel
    - Chat list panel: "Hộp thư" in HeadlineLgMobileLabel, search input, conversation cards (avatar, name, last message, timestamp)
    - Active conversation highlighting: GlassCardBorder, primary border tint, elevated shadow
    - Conversation panel header: contact name in DisplayXlLabel, role description, circular GlassCardBorder action buttons (call, video, more)
    - Message bubbles: incoming (white bg, 2px top-left / 18px others), outgoing (PrimaryContainer bg, 18px top / 2px bottom-right)
    - Message input: pill-shaped GlassCardBorder, attachment button, emoji button, "GỬI" PillPrimaryButton
    - Date dividers: LabelCapsLabel on semi-transparent pill background
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 12.3_

- [ ] 7. Convert supporting pages
  - [x] 7.1 Convert LoginPage.xaml and RegisterPage.xaml to Liquid Blue design
    - Apply LiquidGradientBrush background to both pages
    - Create centered GlassCardBorder form container
    - Style inputs with MinimalistEntry
    - Add PillPrimaryButton for submit actions
    - All text, labels, placeholders in Vietnamese
    - _Requirements: 11.1, 11.2, 12.3_

  - [x] 7.2 Convert CustomerHomePage.xaml to Liquid Blue design
    - Apply LiquidGradientBrush background
    - Use GlassCardBorder containers for content sections
    - Apply Typography_Hierarchy (SourceSerif4 headlines, Inter body/data)
    - All text in Vietnamese
    - _Requirements: 11.3, 12.2_

  - [x] 7.3 Convert UserAccessPage.xaml to Liquid Blue design
    - Apply LiquidGradientBrush background
    - Render user management content in GlassCardBorder containers
    - Use DataTabularLabel for table data
    - Use LabelCapsLabel for column headers
    - Vietnamese labels throughout
    - _Requirements: 11.4, 12.4_

  - [x] 7.4 Convert LogsPage.xaml to Liquid Blue design
    - Apply LiquidGradientBrush background
    - Render log entries in GlassCardBorder table
    - Use DataTabularLabel for log data
    - Use StatusBadgePill for log status indicators
    - Vietnamese column headers and labels
    - _Requirements: 11.5, 12.4, 12.5_

- [ ] 8. Add model extensions and converters
  - [x] 8.1 Add view-model properties for Liquid Blue status mapping
    - Add `StatusBadgeBackground` computed property to order/import models mapping Vietnamese status strings to Liquid Blue palette colors (emerald for "Đã giao", blue for "Đang vận chuyển", amber for "Đang xử lý", slate for "Chờ duyệt")
    - Add `IsDateDivider` property to ChatMessage model for date separator rendering
    - Update any existing converters in `Converters/` folder to use new Liquid Blue color values
    - _Requirements: 5.4, 7.5, 9.7, 13.5_

- [x] 9. Checkpoint - Verify all pages and integration
  - Ensure all pages compile without XAML errors, the app launches successfully, all navigation routes work, data binding populates pages correctly, and the visual appearance matches the Liquid Blue specification. Ask the user if questions arise.

- [ ] 10. Final validation and cleanup
  - [x] 10.1 Verify cross-page visual consistency
    - Confirm all pages use LiquidGradientBrush background
    - Confirm all cards use GlassCardBorder style consistently
    - Confirm typography hierarchy is correct (SourceSerif4 for headlines, Inter for data/body)
    - Confirm no green/teal/yellow tones appear in the palette
    - Confirm all buttons use pill-shaped styling
    - Confirm all status badges use correct contextual colors
    - Remove any leftover old style references
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

  - [ ]* 10.2 Write unit tests for resource dictionary values
    - Verify all color token keys exist with correct hex values
    - Verify typography styles have correct font family, size, and weight
    - Verify spacing and radius constants match specification
    - Verify GlassCardBorder style properties
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [ ]* 10.3 Write integration tests for page structure
    - Verify each page applies ContentPageStyle
    - Verify GlassCardBorder containers are present on each page
    - Verify Vietnamese text labels match specification
    - Verify navigation routes are preserved and functional
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 13.2_

- [~] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each major phase
- No property-based tests are included because this is a UI rendering/styling conversion where PBT is not applicable (as noted in the design document)
- The existing navigation structure, data models, and code-behind logic are preserved — only XAML markup and resource dictionaries change
- Font files must be obtained separately and placed in `Resources/Fonts/` before task 1.1 can be completed
- The glassmorphism effect uses semi-transparent backgrounds as a universal baseline; platform-specific blur enhancements are optional future work

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3"] },
    { "id": 2, "tasks": ["1.4"] },
    { "id": 3, "tasks": ["3.1"] },
    { "id": 4, "tasks": ["3.2", "4.1", "4.2", "4.3"] },
    { "id": 5, "tasks": ["4.4", "4.5", "8.1"] },
    { "id": 6, "tasks": ["6.1", "6.2", "6.3"] },
    { "id": 7, "tasks": ["7.1", "7.2", "7.3", "7.4"] },
    { "id": 8, "tasks": ["10.1"] },
    { "id": 9, "tasks": ["10.2", "10.3"] }
  ]
}
```
