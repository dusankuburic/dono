# Building Manager App - UI Wireframes

**Version:** 1.0  
**Platform:** Mobile (React Native)

---

## Screen Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        SCREEN HIERARCHY                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Auth Stack                                                      │
│  ├── Login                                                       │
│  ├── Register                                                    │
│  ├── Forgot Password                                             │
│  └── Reset Password                                              │
│                                                                   │
│  Main App (Tab Navigator)                                        │
│  ├── Dashboard (Home)                                            │
│  ├── Residents                                                   │
│  ├── Keys                                                        │
│  ├── Work Orders                                                 │
│  └── More (Drawer/Stack)                                         │
│      ├── Announcements                                           │
│      ├── Assemblies                                              │
│      ├── Finance                                                 │
│      ├── Documents                                               │
│      ├── Service Providers                                       │
│      ├── Deadlines                                               │
│      ├── Settings                                                │
│      └── Profile                                                 │
│                                                                   │
│  Modals                                                          │
│  ├── Add Resident                                                │
│  ├── Key Checkout                                                │
│  ├── Key Checkin                                                 │
│  ├── Create Work Order                                           │
│  ├── Create Announcement                                         │
│  ├── Create Assembly                                             │
│  ├── Record Payment                                              │
│  └── Record Expense                                              │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Authentication Screens

### 1.1 Login Screen

```
┌────────────────────────────────────┐
│                                    │
│            📱 LOGO                 │
│         Upravnik Zgrade            │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 📧 Email                     │  │
│  └──────────────────────────────┘  │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 🔒 Password                  │  │
│  └──────────────────────────────┘  │
│                                    │
│  [         LOG IN         ]        │
│                                    │
│      Forgot password?              │
│                                    │
│  ─────────── or ───────────        │
│                                    │
│  Don't have an account?  Register  │
│                                    │
└────────────────────────────────────┘
```

**Behavior:**
- Email validation on blur
- Show/hide password toggle
- "Forgot password?" links to reset flow
- Error messages below fields
- Loading state on button

### 1.2 Register Screen

```
┌────────────────────────────────────┐
│  ← Back                            │
│                                    │
│         Create Account             │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 👤 First Name                │  │
│  └──────────────────────────────┘  │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 👤 Last Name                 │  │
│  └──────────────────────────────┘  │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 📧 Email                     │  │
│  └──────────────────────────────┘  │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 📱 Phone (optional)          │  │
│  └──────────────────────────────┘  │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ 🔒 Password                  │  │
│  └──────────────────────────────┘  │
│  ┌──────────────────────────────┐  │
│  │ 🔒 Confirm Password          │  │
│  └──────────────────────────────┘  │
│                                    │
│  [      CREATE ACCOUNT     ]       │
│                                    │
│  Already have an account? Login    │
│                                    │
└────────────────────────────────────┘
```

---

## 2. Main App - Tab Screens

### 2.1 Dashboard (Home)

```
┌────────────────────────────────────┐
│  ≡  Dashboard           👤 Milan   │
├────────────────────────────────────┤
│                                    │
│  Building: Bulevar Oslobođenja 12  │
│  ▼                                 │
│                                    │
│  ┌────────────────────────────────┐│
│  │ ⚠️  ACTION NEEDED              ││
│  │                                ││
│  │ • 3 overdue keys               ││
│  │ • 2 new work orders            ││
│  │ • Assembly in 5 days           ││
│  └────────────────────────────────┘│
│                                    │
│  ┌──────────┐ ┌──────────┐         │
│  │ 24       │ │ 3        │         │
│  │ Units    │ │ Keys Out │         │
│  └──────────┘ └──────────┘         │
│                                    │
│  ┌──────────┐ ┌──────────┐         │
│  │ 5        │ │ 85%      │         │
│  │ Open WOs │ │ Collected│         │
│  └──────────┘ └──────────┘         │
│                                    │
│  ─── Recent Work Orders ───        │
│                                    │
│  🔴 WO-2024-0123                   │
│  Leaking pipe in unit 4B           │
│  High priority • 2h ago            │
│                                    │
│  🟡 WO-2024-0122                   │
│  Intercom not working              │
│  Normal • Yesterday                │
│                                    │
│  🟢 WO-2024-0121                   │
│  Light bulb replacement            │
│  Low • 2 days ago                  │
│                                    │
│  [View All Work Orders →]          │
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
│ Home Res. Keys W.O.  More          │
└────────────────────────────────────┘
```

**Components:**
- Building selector (dropdown if managing multiple)
- Alert card for urgent items
- Quick stats grid (4 cards)
- Recent work orders list
- Tab bar navigation

### 2.2 Residents List

```
┌────────────────────────────────────┐
│  Residents           [+ Add]       │
├────────────────────────────────────┤
│  🔍 Search by name, unit, phone    │
│                                    │
│  Filter: [All ▼] [All Types ▼]     │
├────────────────────────────────────┤
│                                    │
│  ┌────────────────────────────────┐│
│  │ 👤 Marković Jovan              ││
│  │ Apt 12 • Owner                 ││
│  │ 📱 +381 63 123 4567            ││
│  └────────────────────────────────┐│
│  │ 👤 Petrović Ana                ││
│  │ Apt 4B • Tenant                ││
│  │ 📱 +381 62 987 6543            ││
│  └────────────────────────────────┐│
│  │ 👤 Nikolić Dragan              ││
│  │ Apt 7 • Owner                  ││
│  │ 📱 +381 64 555 1234            ││
│  │ ⚠️ Emergency: +381 63...       ││
│  └────────────────────────────────┐│
│  │ 👤 Ivić Marija                 ││
│  │ Apt 2A • Family                ││
│  │ 📱 +381 65 333 4444            ││
│  └────────────────────────────────┘│
│                                    │
│         Showing 24 of 48           │
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
│ Home Res. Keys W.O.  More          │
└────────────────────────────────────┘
```

**Features:**
- Search with debounce (300ms)
- Filter chips (owner/tenant/family)
- Tap to view details
- Swipe actions: Call, Message
- Offline indicator if cached

### 2.3 Resident Detail

```
┌────────────────────────────────────┐
│  ← Back        Resident            │
├────────────────────────────────────┤
│                                    │
│         ┌─────────────┐            │
│         │   👤        │            │
│         │  (avatar)   │            │
│         └─────────────┘            │
│                                    │
│        Marković Jovan              │
│        Unit 12 • Owner             │
│                                    │
├────────────────────────────────────┤
│  CONTACT                           │
│                                    │
│  📱 +381 63 123 4567     [Call]    │
│  📱 +381 11 555 1234     [Call]    │
│  📧 jovan@email.com      [Email]   │
│                                    │
├────────────────────────────────────┤
│  UNIT INFORMATION                  │
│                                    │
│  Unit:            12               │
│  Floor:           3                │
│  Area:            75 m²            │
│  Ownership Share: 3.2%             │
│                                    │
├────────────────────────────────────┤
│  EMERGENCY CONTACT                 │
│                                    │
│  Name:      Marković Mila          │
│  Relation:  Spouse                 │
│  Phone:     +381 63 123 4568       │
│                                    │
├────────────────────────────────────┤
│  HISTORY                           │
│                                    │
│  • Moved in: Jan 2015              │
│  • 2 work orders reported          │
│  • 0 keys currently out            │
│                                    │
├────────────────────────────────────┤
│  [Edit Resident]                   │
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 2.4 Keys List

```
┌────────────────────────────────────┐
│  Keys                  [+ Add]     │
├────────────────────────────────────┤
│  Filter: [All ▼] [Status ▼]        │
├────────────────────────────────────┤
│                                    │
│  ⚠️ OVERDUE (3)                    │
│  ┌────────────────────────────────┐│
│  │ 🔑 Entrance Main #1            ││
│  │ ⚠️ 3 days overdue              ││
│  │ Ivan Petrović • +381 62...     ││
│  │ Due: Jan 15, 2024              ││
│  │ [Check In]  [Call]             ││
│  └────────────────────────────────┘│
│                                    │
│  CHECKED OUT (2)                   │
│  ┌────────────────────────────────┐│
│  │ 🔑 Apt 12 #1                   ││
│  │ Due: Jan 20, 2024              ││
│  │ Plumber • +381 63...           ││
│  │ [Check In]  [Call]             ││
│  └────────────────────────────────┘│
│                                    │
│  AVAILABLE                         │
│  ┌────────────────────────────────┐│
│  │ 🔑 Entrance Main #2            ││
│  │ ✓ Available                    ││
│  │ [Check Out]                    ││
│  └────────────────────────────────┐│
│  │ 🔑 Apt 12 #2                   ││
│  │ ✓ Available                    ││
│  │ [Check Out]                    ││
│  └────────────────────────────────┘│
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 2.5 Key Checkout Modal

```
┌────────────────────────────────────┐
│  Check Out Key            [✕]     │
├────────────────────────────────────┤
│                                    │
│  🔑 Entrance Main #2               │
│                                    │
│  RECIPIENT                         │
│  ┌────────────────────────────────┐│
│  │ 🔍 Search residents...         ││
│  └────────────────────────────────┘│
│  ─── or enter manually ───         │
│  ┌────────────────────────────────┐│
│  │ Name *                         ││
│  └────────────────────────────────┘│
│  ┌────────────────────────────────┐│
│  │ Phone *                        ││
│  └────────────────────────────────┘│
│  ┌────────────────────────────────┐│
│  │ ID Number (optional)           ││
│  └────────────────────────────────┘│
│                                    │
│  DETAILS                           │
│  ┌────────────────────────────────┐│
│  │ Purpose *                      ││
│  └────────────────────────────────┘│
│  ┌────────────────────────────────┐│
│  │ Expected Return *    📅        ││
│  │ Jan 20, 2024                   ││
│  └────────────────────────────────┘│
│                                    │
│  DOCUMENTATION                     │
│  [📷 Take Photo of ID]             │
│                                    │
│  [      CHECK OUT KEY      ]       │
│                                    │
└────────────────────────────────────┘
```

### 2.6 Work Orders List

```
┌────────────────────────────────────┐
│  Work Orders           [+ New]     │
├────────────────────────────────────┤
│  🔍 Search...                      │
│                                    │
│  [All] [New] [In Progress] [Done]  │
├────────────────────────────────────┤
│                                    │
│  🔴 EMERGENCY                      │
│  ┌────────────────────────────────┐│
│  │ WO-2024-0123                   ││
│  │ Water pipe burst in Apt 4B     ││
│  │ ⏱️ 23h remaining                ││
│  │ 📍 Unit 4B • Plumbing          ││
│  │ 📅 Today, 08:30                ││
│  └────────────────────────────────┘│
│                                    │
│  🟡 HIGH PRIORITY                  │
│  ┌────────────────────────────────┐│
│  │ WO-2024-0122                   ││
│  │ Intercom not working           ││
│  │ 📍 Common Area • Electrical    ││
│  │ 📅 Yesterday • Acknowledged    ││
│  └────────────────────────────────┘│
│                                    │
│  🟢 NORMAL                         │
│  ┌────────────────────────────────┐│
│  │ WO-2024-0121                   ││
│  │ Light bulb in hallway          ││
│  │ 📍 Floor 3 • Electrical        ││
│  │ 📅 2 days ago • In Progress    ││
│  └────────────────────────────────┘│
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 2.7 Work Order Detail

```
┌────────────────────────────────────┐
│  ← Back      Work Order            │
├────────────────────────────────────┤
│                                    │
│  WO-2024-0123                      │
│  Water pipe burst in Apt 4B        │
│                                    │
│  ┌────────────────────────────────┐│
│  │ 🔴 EMERGENCY                   ││
│  │ Status: In Progress            ││
│  │ ⏱️ 23h 15m remaining            ││
│  └────────────────────────────────┘│
│                                    │
├────────────────────────────────────┤
│  DETAILS                           │
│                                    │
│  Category:    Plumbing             │
│  Location:    Unit 4B              │
│  Reported by: Ana Petrović         │
│  Created:     Today, 08:30         │
│                                    │
├────────────────────────────────────┤
│  DESCRIPTION                       │
│                                    │
│  Water pipe burst under kitchen    │
│  sink. Water leaking into hallway. │
│  Building water shut off needed.   │
│                                    │
├────────────────────────────────────┤
│  PHOTOS                            │
│                                    │
│  ┌─────┐ ┌─────┐ ┌─────┐           │
│  │ 📷  │ │ 📷  │ │ 📷  │           │
│  └─────┘ └─────┘ └─────┘           │
│                                    │
├────────────────────────────────────┤
│  STATUS HISTORY                    │
│                                    │
│  ✓ In Progress    Today, 09:00     │
│  ✓ Acknowledged   Today, 08:45     │
│  ✓ New            Today, 08:30     │
│                                    │
├────────────────────────────────────┤
│  NOTES                             │
│                                    │
│  [Add Note]                        │
│                                    │
├────────────────────────────────────┤
│  [Update Status ▼]                 │
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 2.8 Create Work Order (Resident View)

```
┌────────────────────────────────────┐
│  New Request              [✕]     │
├────────────────────────────────────┤
│                                    │
│  What's the problem?               │
│                                    │
│  ┌────────────────────────────────┐│
│  │ Select category                ││
│  │ 🔽                             ││
│  │ ┌────────────────────────────┐ ││
│  │ │ 🔧 Plumbing                │ ││
│  │ │ ⚡ Electrical               │ ││
│  │ │ 🛗 Elevator                 │ ││
│  │ │ 🏢 Common Areas             │ ││
│  │ │ 🏠 Facade/Roof              │ ││
│  │ │ 📞 Entrance/Intercom        │ ││
│  │ │ 🌡️ Heating                  │ ││
│  │ │ 📦 Other                    │ ││
│  │ └────────────────────────────┘ ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ Describe the problem...        ││
│  │ (minimum 10 characters)        ││
│  │                                ││
│  └────────────────────────────────┘│
│                                    │
│  Urgency                           │
│  ○ Low    ● Normal    ○ High      │
│                                    │
│  Add photos (optional)             │
│  ┌─────┐ ┌─────┐ ┌─────┐           │
│  │  +  │ │     │ │     │           │
│  └─────┘ └─────┘ └─────┘           │
│                                    │
│  Location                          │
│  ● My Unit (Apt 12)               │
│  ○ Other location                 │
│                                    │
│  [      SUBMIT REQUEST     ]       │
│                                    │
└────────────────────────────────────┘
```

---

## 3. More Section Screens

### 3.1 Announcements List

```
┌────────────────────────────────────┐
│  ← Back     Announcements          │
│                         [+ New]    │
├────────────────────────────────────┤
│                                    │
│  ┌────────────────────────────────┐│
│  │ 🔴 EMERGENCY                   ││
│  │ Water Shutoff Tomorrow         ││
│  │ Water will be shut off from    ││
│  │ 9 AM to 2 PM for repairs...    ││
│  │ 📅 Today • 📨 Delivered to 48  ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ 📢 GENERAL                     ││
│  │ Assembly Meeting Notice        ││
│  │ Annual assembly meeting on     ││
│  │ February 15th at 7 PM...       ││
│  │ 📅 Jan 20 • 📨 Delivered to 46 ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ 🔧 MAINTENANCE                 ││
│  │ Elevator Inspection            ││
│  │ Annual elevator inspection     ││
│  │ scheduled for next week...     ││
│  │ 📅 Jan 18 • 📨 Delivered to 48 ││
│  └────────────────────────────────┘│
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 3.2 Create Announcement

```
┌────────────────────────────────────┐
│  New Announcement         [✕]     │
├────────────────────────────────────┤
│                                    │
│  Type                              │
│  ○ General  ○ Emergency  ○ Maint. │
│  ○ Meeting  ○ Payment              │
│                                    │
│  ┌────────────────────────────────┐│
│  │ Title *                        ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ Message *                      ││
│  │                                ││
│  │                                ││
│  │                                ││
│  │ 0/1000                         ││
│  └────────────────────────────────┘│
│                                    │
│  Send to                           │
│  ● All residents                  │
│  ○ Owners only                    │
│  ○ Tenants only                   │
│  ○ Specific units                 │
│                                    │
│  Delivery method                   │
│  ☑️ App notification              │
│  ☐ SMS (costs apply)              │
│  ☐ Email                          │
│                                    │
│  Schedule                          │
│  ● Send now                       │
│  ○ Schedule for later             │
│                                    │
│  [         SEND          ]         │
│                                    │
└────────────────────────────────────┘
```

### 3.3 Assemblies List

```
┌────────────────────────────────────┐
│  ← Back       Assemblies           │
│                          [+ New]   │
├────────────────────────────────────┤
│                                    │
│  UPCOMING                          │
│  ┌────────────────────────────────┐│
│  │ 📅 Feb 15, 2024 at 19:00       ││
│  │ Annual Assembly                ││
│  │ 📍 Common Room                 ││
│  │ Status: Notice sent            ││
│  │ 👥 12/48 confirmed             ││
│  │ [View Details]                 ││
│  └────────────────────────────────┘│
│                                    │
│  PAST                              │
│  ┌────────────────────────────────┐│
│  │ 📅 Jan 10, 2024                ││
│  │ Emergency Assembly             ││
│  │ ✓ Completed                    ││
│  │ 3 decisions passed             ││
│  │ [View Minutes]                 ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ 📅 Nov 20, 2023                ││
│  │ Regular Assembly               ││
│  │ ✓ Completed                    ││
│  │ 5 decisions passed             ││
│  │ [View Minutes]                 ││
│  └────────────────────────────────┘│
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 3.4 Assembly Detail (During Meeting)

```
┌────────────────────────────────────┐
│  ← Back       Assembly             │
├────────────────────────────────────┤
│                                    │
│  Annual Assembly                   │
│  Feb 15, 2024 at 19:00             │
│  Common Room                       │
│                                    │
│  ┌────────────────────────────────┐│
│  │ QUORUM                         ││
│  │ ✓ Achieved (62%)               ││
│  │ 28/48 owners present           ││
│  │ Required: 51%                  ││
│  └────────────────────────────────┘│
│                                    │
├────────────────────────────────────┤
│  AGENDA                            │
│                                    │
│  1. ✓ Opening (Completed)          │
│                                    │
│  2. 🔄 Financial Report            │
│     Simple majority                │
│     [Record Vote]                  │
│                                    │
│  3. ⏳ Fee Increase                │
│     2/3 majority required          │
│                                    │
│  4. ⏳ Elevator Repair             │
│     Simple majority                │
│                                    │
│  5. ⏳ Closing                      │
│                                    │
├────────────────────────────────────┤
│  [Manage Attendance]               │
│  [Finalize Meeting]                │
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

### 3.5 Finance Dashboard

```
┌────────────────────────────────────┐
│  ← Back        Finance             │
├────────────────────────────────────┤
│                                    │
│  Period: [Jan 2024 ▼]              │
│                                    │
│  ┌────────────────────────────────┐│
│  │      BALANCE                   ││
│  │      125,400 RSD               ││
│  │                                ││
│  │  Income    Expenses            ││
│  │  180,000   54,600              ││
│  └────────────────────────────────┘│
│                                    │
│  COLLECTION RATE                   │
│  ████████████░░░░ 85%              │
│  41 of 48 units paid               │
│                                    │
├────────────────────────────────────┤
│  EXPENSES BY CATEGORY              │
│                                    │
│  Utilities     25,000 █████████    │
│  Repairs       15,000 █████        │
│  Cleaning      8,000  ███          │
│  Other         6,600  ██           │
│                                    │
├────────────────────────────────────┤
│  QUICK ACTIONS                     │
│                                    │
│  [Record Payment]                  │
│  [Record Expense]                  │
│  [View Arrears]                    │
│  [Export Report]                   │
│                                    │
├────────────────────────────────────┤
│  🏠    👥    🔑    📋    ☰        │
└────────────────────────────────────┘
```

---

## 4. Component Library

### 4.1 Buttons

```
Primary Button (Filled)
┌──────────────────────┐
│    Submit Request    │  <- Blue background, white text
└──────────────────────┘

Secondary Button (Outlined)
┌──────────────────────┐
│    Cancel            │  <- White background, blue border/text
└──────────────────────┘

Destructive Button
┌──────────────────────┐
│    Delete            │  <- Red background, white text
└──────────────────────┘

Disabled Button
┌──────────────────────┐
│    Submit Request    │  <- Gray background, gray text
└──────────────────────┘

Loading Button
┌──────────────────────┐
│    ○ Loading...      │  <- Spinner + text
└──────────────────────┘
```

### 4.2 Status Badges

```
Priority:
  🔴 Emergency  (red background)
  🟠 High       (orange background)
  🟡 Normal     (yellow background)
  🟢 Low        (green background)

Work Order Status:
  New           (blue)
  Acknowledged  (purple)
  In Progress   (orange)
  Completed     (green)
  Cancelled     (gray)

Key Status:
  Available     (green)
  Checked Out   (yellow)
  Overdue       (red)
  Lost          (gray)
```

### 4.3 Cards

```
Standard Card
┌────────────────────────────────────┐
│ Title                              │
│ Description text goes here and     │
│ may wrap to multiple lines.        │
│                                    │
│ Footer with actions                │
└────────────────────────────────────┘

Alert Card (Emergency/Urgent)
┌────────────────────────────────────┐
│ ⚠️  ALERT TITLE                    │
│                                    │
│ Alert description text.            │
│                                    │
│ [Action Button]                    │
└────────────────────────────────────┘

Stats Card
┌──────────────┐
│     24       │
│    Units     │
└──────────────┘
```

### 4.4 Form Inputs

```
Text Input
┌────────────────────────────────────┐
│ Label                              │
│ ┌────────────────────────────────┐ │
│ │ Placeholder text               │ │
│ └────────────────────────────────┘ │
│ Helper text (optional)             │
└────────────────────────────────────┘

Text Input with Error
┌────────────────────────────────────┐
│ Label                              │
│ ┌────────────────────────────────┐ │
│ │ Invalid input                  │ │ <- Red border
│ └────────────────────────────────┘ │
│ ⚠️ This field is required          │ <- Red text
└────────────────────────────────────┘

Select/Dropdown
┌────────────────────────────────────┐
│ Label                              │
│ ┌────────────────────────────────┐ │
│ │ Selected Option          🔽    │ │
│ └────────────────────────────────┘ │
└────────────────────────────────────┘

Date Picker
┌────────────────────────────────────┐
│ Label                              │
│ ┌────────────────────────────────┐ │
│ │ Jan 20, 2024              📅   │ │
│ └────────────────────────────────┘ │
└────────────────────────────────────┘
```

### 4.5 Navigation

```
Tab Bar (Bottom)
┌────────────────────────────────────┐
│  🏠    👥    🔑    📋    ☰        │
│ Home Res. Keys W.O.  More          │
└────────────────────────────────────┘
  (active tab highlighted in blue)

Header
┌────────────────────────────────────┐
│  ← Back        Page Title          │
├────────────────────────────────────┤

Header with Action
┌────────────────────────────────────┐
│  ← Back        Page Title    [+ ]  │
├────────────────────────────────────┤
```

### 4.6 List Items

```
Simple List Item
┌────────────────────────────────────┐
│ 👤 Marković Jovan              →   │
└────────────────────────────────────┘

List Item with Subtitle
┌────────────────────────────────────┐
│ 👤 Marković Jovan                  │
│ Apt 12 • Owner                →   │
└────────────────────────────────────┘

List Item with Actions
┌────────────────────────────────────┐
│ 👤 Marković Jovan                  │
│ Apt 12 • Owner        [Call][→]   │
└────────────────────────────────────┘
```

### 4.7 Empty States

```
┌────────────────────────────────────┐
│                                    │
│              📋                    │
│                                    │
│       No work orders yet           │
│                                    │
│   Tap the + button to create       │
│       your first work order        │
│                                    │
│      [ Create Work Order ]         │
│                                    │
└────────────────────────────────────┘
```

### 4.8 Loading States

```
Skeleton Loading
┌────────────────────────────────────┐
│ ░░░░░░░░░░░░░░░░░░░░              │
│ ░░░░░░░░░░                        │
│ ░░░░░░░░░░░░░░░░                  │
└────────────────────────────────────┘

Spinner
        ○ (rotating)

Pull to Refresh
┌────────────────────────────────────┐
│          ↻ Pull to refresh         │
├────────────────────────────────────┤
```

---

## 5. Offline States

### 5.1 Offline Banner

```
┌────────────────────────────────────┐
│ ⚠️ Offline - Showing cached data   │
│    Last synced: 5 min ago          │
├────────────────────────────────────┤
```

### 5.2 Pending Changes Indicator

```
┌────────────────────────────────────┐
│ 🔄 3 changes pending sync          │
└────────────────────────────────────┘
```

### 5.3 Sync Status in Settings

```
Sync Status
┌────────────────────────────────────┐
│ Status:        ● Online            │
│ Last synced:   2 minutes ago       │
│ Pending:       0 changes           │
│                                    │
│ [Sync Now]                         │
└────────────────────────────────────┘
```

---

## 6. Owner/Tenant Views (Simplified)

### 6.1 Owner Dashboard

```
┌────────────────────────────────────┐
│  ≡  Home                  👤 Ana   │
├────────────────────────────────────┤
│                                    │
│  My Unit: Apt 12                   │
│  Building: Bulevar Oslobođenja 12  │
│                                    │
│  ┌────────────────────────────────┐│
│  │ FEE STATUS                     ││
│  │                                ││
│  │ January:  ✓ Paid               ││
│  │ Amount:    3,500 RSD            ││
│  │ Due:       Feb 10               ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ 📢 LATEST ANNOUNCEMENT         ││
│  │ Water Shutoff Tomorrow         ││
│  │ Water will be shut off...      ││
│  └────────────────────────────────┘│
│                                    │
│  ┌────────────────────────────────┐│
│  │ 📅 UPCOMING ASSEMBLY           ││
│  │ Feb 15 at 19:00                ││
│  │ Annual Assembly                ││
│  │ [View Details] [RSVP]          ││
│  └────────────────────────────────┘│
│                                    │
│  [Report Issue]                    │
│                                    │
├────────────────────────────────────┤
│  🏠    📢    📋    💰    👤       │
│ Home Ann.  W.O. Finance Profile    │
└────────────────────────────────────┘
```

---

## 7. Design Tokens

### Colors
```
Primary:        #1976D2 (Blue)
Primary Dark:   #1565C0
Primary Light:  #42A5F5

Success:        #4CAF50 (Green)
Warning:        #FF9800 (Orange)
Error:          #F44336 (Red)
Info:           #2196F3 (Blue)

Text Primary:   #212121
Text Secondary: #757575
Text Disabled:  #BDBDBD

Background:     #FAFAFA
Surface:        #FFFFFF
Divider:        #E0E0E0
```

### Typography
```
Font Family:    System default (San Francisco on iOS, Roboto on Android)

H1:             32px, Bold
H2:             24px, Bold
H3:             20px, SemiBold
H4:             18px, SemiBold
Body:           16px, Regular
Body Small:     14px, Regular
Caption:        12px, Regular
Button:         14px, SemiBold (uppercase)
```

### Spacing
```
xs:     4px
sm:     8px
md:     16px
lg:     24px
xl:     32px
xxl:    48px
```

### Border Radius
```
sm:     4px
md:     8px
lg:     16px
full:   9999px (pill)
```

---

*Document Version: 1.0*
*Created: 2024*
