# Requirements Document

## Introduction

This document defines the requirements for converting the "Lumière Liquid Blue" design system from HTML stitch reference files into .NET MAUI XAML, replacing the current UI pages in the SyncChain Desktop application. The conversion encompasses 8 primary pages, the Shell navigation, supporting pages, and full Vietnamese localization of all user-facing text.

## Glossary

- **Design_System**: The "Lumière Liquid Blue" visual language defined in DESIGN.md, characterized by glassmorphism, cool blue-tinted color palette, Source Serif 4 / Inter typography pairing, and rounded card-based layouts.
- **Glass_Card**: A UI container component featuring semi-transparent white background (60% opacity), 24px backdrop blur, 1px white border at 30% opacity, and rounded corners (24px).
- **Stitch_File**: An HTML reference file in the sampleUI folder that serves as the visual specification for a given page layout.
- **XAML_Page**: A .NET MAUI ContentPage defined in XAML markup within the Views/Pages directory.
- **AppShell**: The .NET MAUI Shell component that provides the application's navigation structure including the sidebar flyout.
- **Resource_Dictionary**: A XAML file (Colors.xaml, Styles.xaml) that defines reusable color, style, and template resources for the application.
- **Liquid_Gradient**: The background gradient defined as a 135-degree linear gradient from #f8f9ff through #e0f2fe to #eff4ff.
- **Typography_Hierarchy**: The dual-font system using Source Serif 4 for editorial headlines and Inter for functional/data text.
- **Vietnamese_Localization**: The process of translating all user-facing text strings to Vietnamese language.
- **Bento_Grid**: A multi-column asymmetric grid layout used for dashboard-style content presentation.

## Requirements

### Requirement 1: Design Token Resource Dictionary

**User Story:** As a developer, I want a centralized resource dictionary with all Liquid Blue design tokens, so that all pages share consistent colors, typography, and spacing values.

#### Acceptance Criteria

1. THE Resource_Dictionary SHALL define all color tokens from the Design_System palette including primary (#50616b), primary-container (#e0f2fe), surface (#f8f9ff), on-surface (#0b1c30), on-surface-variant (#43474b), outline (#73787b), outline-variant (#c3c7cb), error (#ba1a1a), secondary (#565e74), and all surface-container variants.
2. THE Resource_Dictionary SHALL define typography styles for display-xl (Source Serif 4, 72px, weight 600), headline-lg (Source Serif 4, 48px, weight 500), headline-lg-mobile (Source Serif 4, 32px, weight 500), body-md (Inter, 16px, weight 400), data-tabular (Inter, 14px, weight 600), and label-caps (Inter, 12px, weight 700, uppercase).
3. THE Resource_Dictionary SHALL define spacing constants for gutter (24px), margin-desktop (64px), and margin-mobile (16px).
4. THE Resource_Dictionary SHALL define corner radius values: sm (4px), default (8px), md (12px), lg (16px), xl (24px), and full (9999px for pill shapes).
5. THE Resource_Dictionary SHALL define Glass_Card styles with semi-transparent white background, border styling, and shadow properties consistent with the Design_System.

### Requirement 2: AppShell Glassmorphism Navigation

**User Story:** As a user, I want a visually refined sidebar navigation that matches the Liquid Blue glassmorphism aesthetic, so that the navigation feels premium and consistent with the overall design.

#### Acceptance Criteria

1. THE AppShell SHALL render a flyout sidebar with a semi-transparent background using the primary-container color at 40% opacity.
2. THE AppShell SHALL display the SyncChain logo with a primary-colored icon container (rounded-xl, white icon) and the brand name "SyncChain" in Source Serif 4 font.
3. THE AppShell SHALL display navigation items with Material Symbols icons and label-caps styled text in Vietnamese: "Bảng điều khiển" (Dashboard), "Sản phẩm" (Products), "Đơn hàng" (Orders), "Tạo đơn hàng" (Create Order), "Nhập hàng" (Imports), "Nhật ký" (Logs), "Tin nhắn" (Chat), "Người dùng & phân quyền" (User Access).
4. WHEN a navigation item is selected, THE AppShell SHALL highlight the active item with a primary-container background, primary text color, bold font weight, and a subtle shadow.
5. THE AppShell SHALL include a footer section with "Hỗ trợ" (Support) and "Đăng xuất" (Sign Out) links separated by a top border at 20% white opacity.

### Requirement 3: Products Page (ProductsPage.xaml)

**User Story:** As a warehouse manager, I want to view product inventory in an editorial layout with hero imagery and a data table, so that I can quickly assess stock status.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a hero section with a full-width rounded container (border-radius 24px), overlay gradient (from background/90 via background/40 to transparent), featured product badge, headline text in display-xl typography, description in body-md, and two action buttons (pill-shaped primary and glass secondary).
2. THE XAML_Page SHALL render a Bento_Grid section below the hero with a 2:1 column ratio containing Glass_Card containers with editorial headlines in Source Serif 4 and descriptive text.
3. THE XAML_Page SHALL render an inventory data table inside a Glass_Card with column headers in label-caps style: "Sản phẩm", "Mã lô", "Danh mục", "Tồn kho", "Trạng thái", "Sức khỏe".
4. THE XAML_Page SHALL display product rows with thumbnail image, product name, batch ID in data-tabular style, category badge (pill-shaped, primary-container background), stock count, status indicator (colored dot with label), and a health progress bar.
5. THE XAML_Page SHALL include pagination controls with "Trước" and "Tiếp" buttons and a record count label in Vietnamese.
6. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 4: Product Detail Page (ProductDetailPage.xaml)

**User Story:** As a warehouse manager, I want to view detailed product information including specifications and distribution data, so that I can make informed inventory decisions.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a hero image section with the product photograph, overlay gradient, and product title in display-xl typography.
2. THE XAML_Page SHALL render a specifications section inside a Glass_Card with key-value pairs displayed in data-tabular style.
3. THE XAML_Page SHALL render a regional distribution section showing distribution data across regions in a grid or chart format within Glass_Card containers.
4. THE XAML_Page SHALL render a stock trends section with a visual representation of historical stock levels.
5. THE XAML_Page SHALL display all labels and headings in Vietnamese.
6. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 5: Orders Page (OrdersPage.xaml)

**User Story:** As an operations manager, I want to view and manage orders with velocity metrics and a detailed table, so that I can monitor fulfillment performance.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a page header with the title "Quản lý Đơn hàng" in display-xl typography and an "Xuất báo cáo" (Export Report) action button.
2. THE XAML_Page SHALL render an operational velocity section in a Glass_Card showing a large metric value, percentage change indicator, throughput and latency sub-metrics, and a "Thời gian thực" (Real-time) badge.
3. THE XAML_Page SHALL render a summary statistics card alongside the velocity section displaying "Đơn đang vận chuyển" (Active Shipments), "Chờ duyệt" (Pending Review), and "Thời gian giao trung bình" (Avg Delivery Time) with data-tabular values.
4. THE XAML_Page SHALL render an order table inside a Glass_Card with columns: "Mã đơn", "Điểm đến", "Sản phẩm", "Trạng thái", "Giá trị (VNĐ)" with status badges using colored pill shapes.
5. THE XAML_Page SHALL render contextual insight cards at the bottom in a 2-column grid with icon, title, and description text.
6. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 6: Order Detail Page (OrderDetailPage.xaml)

**User Story:** As an operations manager, I want to view complete order details including shipping timeline and financial summary, so that I can track individual order progress.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a shipping timeline with step indicators (completed, in-progress, pending states), location labels, and timestamps.
2. THE XAML_Page SHALL render an itemized product list showing each order item with thumbnail, name, quantity, and unit price in a Glass_Card container.
3. THE XAML_Page SHALL render a financial summary section showing subtotal, shipping cost, tax, and total amount in data-tabular typography.
4. THE XAML_Page SHALL display all labels, statuses, and headings in Vietnamese.
5. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 7: Import Management Page (ImportsPage.xaml)

**User Story:** As a logistics coordinator, I want to track inbound shipments with real-time status and a shipment table, so that I can manage cross-border logistics efficiently.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a page header with the title "Quản lý Đơn nhập hàng" and a "Tạo Đơn Nhập Mới" action button.
2. THE XAML_Page SHALL render a 4-column statistics grid with Glass_Card containers showing: "Tổng kiện hàng", "Đang trên biển", "Đợi thông quan", "Hoàn tất nhập kho" with large numeric values in display-xl style.
3. THE XAML_Page SHALL render a live tracking timeline in a Glass_Card showing shipment progress with completed, in-transit, and pending waypoints connected by a vertical line indicator.
4. THE XAML_Page SHALL render a map/visual section as a large rounded container with an overlay Glass_Card showing vessel information.
5. THE XAML_Page SHALL render a shipment table with columns: "Mã vận đơn", "Nhà cung cấp", "Hàng hóa", "Ngày đến (dự kiến)", "Trạng thái", "Quản lý" with Vietnamese status badges and pagination.
6. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 8: Create Import Page (CreateOrderPage.xaml)

**User Story:** As a logistics coordinator, I want to create new import orders through a structured form, so that I can initiate inbound shipments with all required details.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a form layout with a logistics overview section, item manifest section, and a summary sidebar.
2. THE XAML_Page SHALL style all form inputs with minimalist design: no background fill, bottom border in light blue that thickens on focus, consistent with the Design_System input field specification.
3. THE XAML_Page SHALL display all form labels, placeholders, and button text in Vietnamese.
4. THE XAML_Page SHALL use Glass_Card containers for each form section with appropriate spacing and rounded corners.
5. THE XAML_Page SHALL include primary action buttons styled as pill-shaped with primary background color and label-caps text.
6. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 9: Chat Page (ChatPage.xaml)

**User Story:** As a team member, I want to communicate with colleagues through an internal messaging interface, so that I can collaborate on logistics operations.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a two-panel layout: a chat list panel (fixed width) and an active conversation panel (flexible width).
2. THE XAML_Page SHALL render the chat list panel with a "Hộp thư" (Inboxes) header in headline-lg-mobile typography, a search input, and conversation cards showing avatar, name, last message preview, and timestamp.
3. WHEN a conversation is selected, THE XAML_Page SHALL highlight the active conversation card with a Glass_Card style, primary border tint, and elevated shadow.
4. THE XAML_Page SHALL render the active conversation panel with a header showing contact name in display-xl style, role description, and action buttons (call, video, more) as circular Glass_Card buttons.
5. THE XAML_Page SHALL render incoming messages with white background bubbles (rounded: 2px top-left, 18px others) and outgoing messages with primary-container tinted bubbles (rounded: 18px top, 2px bottom-right).
6. THE XAML_Page SHALL render a message input area with a pill-shaped Glass_Card container, attachment button, emoji button, and a "GỬI" (Send) primary button.
7. THE XAML_Page SHALL display date dividers between message groups with label-caps styling on a semi-transparent pill background.
8. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 10: Dashboard Page (DashboardPage.xaml)

**User Story:** As a manager, I want an overview dashboard showing key business metrics, so that I can quickly assess operational health.

#### Acceptance Criteria

1. THE XAML_Page SHALL render a page header with "Bảng điều khiển" title in headline-lg typography and a welcome message.
2. THE XAML_Page SHALL render a statistics overview section using a Bento_Grid layout with Glass_Card containers showing key metrics (total products, active orders, pending imports, messages) with icons and large numeric values.
3. THE XAML_Page SHALL render a recent activity section with a Glass_Card containing a list of recent events with timestamps and status indicators.
4. THE XAML_Page SHALL render quick-action cards in a grid layout providing navigation shortcuts to primary functions.
5. THE XAML_Page SHALL display all text content in Vietnamese.
6. THE XAML_Page SHALL apply the Liquid_Gradient as the page background.

### Requirement 11: Supporting Pages Styling (LoginPage, RegisterPage, CustomerHomePage, UserAccessPage, LogsPage)

**User Story:** As a user, I want all application pages to share the same Liquid Blue visual language, so that the experience feels cohesive throughout the application.

#### Acceptance Criteria

1. THE LoginPage SHALL apply the Liquid_Gradient background, Glass_Card form container, minimalist input fields, and pill-shaped primary action button with all text in Vietnamese.
2. THE RegisterPage SHALL apply the same styling as LoginPage with appropriate form fields and Vietnamese labels.
3. THE CustomerHomePage SHALL apply the Liquid_Gradient background, Glass_Card containers, and Typography_Hierarchy with Vietnamese text.
4. THE UserAccessPage SHALL render user management content within Glass_Card containers using the data-tabular typography for table data and Vietnamese labels.
5. THE LogsPage SHALL render log entries within a Glass_Card table using data-tabular typography, status badges with colored pills, and Vietnamese column headers.

### Requirement 12: Vietnamese Localization

**User Story:** As a Vietnamese-speaking user, I want all interface text displayed in Vietnamese, so that I can use the application in my native language.

#### Acceptance Criteria

1. THE Design_System SHALL display all navigation labels in Vietnamese: "Bảng điều khiển", "Sản phẩm", "Đơn hàng", "Tạo đơn hàng", "Nhập hàng", "Nhật ký", "Tin nhắn", "Người dùng & phân quyền".
2. THE Design_System SHALL display all page titles in Vietnamese as specified in each page requirement.
3. THE Design_System SHALL display all button labels in Vietnamese: "Xuất báo cáo" (Export), "Lọc dữ liệu" (Filter), "Tìm kiếm" (Search), "Trước" (Previous), "Tiếp" (Next), "Gửi" (Send), "Tạo mới" (Create New), "Chi tiết" (Details), "Đăng xuất" (Sign Out), "Hỗ trợ" (Support).
4. THE Design_System SHALL display all table column headers in Vietnamese as specified in each page requirement.
5. THE Design_System SHALL display all status labels in Vietnamese: "Tối ưu" (Optimal), "Tồn kho thấp" (Low Stock), "Đang vận chuyển" (In Transit), "Đang xử lý" (Processing), "Đã giao" (Delivered), "Đã thông quan" (Cleared), "Trên biển" (At Sea), "Đang xếp dỡ" (Loading/Unloading).

### Requirement 13: Glassmorphism Visual Consistency

**User Story:** As a user, I want the application to maintain a consistent glassmorphism aesthetic across all pages, so that the interface feels unified and premium.

#### Acceptance Criteria

1. THE Design_System SHALL apply Glass_Card styling consistently: semi-transparent white background (rgba 255,255,255,0.6 equivalent), 24px backdrop blur where platform-supported, 1px border with white at 30% opacity, and rounded corners of 24px.
2. THE Design_System SHALL apply the Liquid_Gradient (135deg, #f8f9ff 0%, #e0f2fe 50%, #eff4ff 100%) as the fixed background on all content pages.
3. THE Design_System SHALL use only blue-tinted cool grays for all neutral colors with no green, teal, or yellow tones in the palette.
4. THE Design_System SHALL apply pill-shaped (border-radius full) styling to all primary action buttons with primary background color and white label-caps text.
5. THE Design_System SHALL apply status badges as pill-shaped elements with contextual background colors (emerald for success, amber for warning, blue for in-progress, slate for pending) and bold uppercase text at 10px size.
