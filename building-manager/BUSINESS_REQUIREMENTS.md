# Building Manager Application - Business Requirements & Use Cases

## Executive Summary

### Product Vision
A mobile-first digital platform that enables building managers (upravnici) to efficiently manage residential communities, automate administrative tasks, and improve communication with residents—all while ensuring compliance with Serbian housing law.

### Problem Statement

**Current Pain Points:**
- Paper-based records are lost, damaged, or outdated
- No centralized system for tenant/owner information
- Key management is chaotic with no audit trail
- Maintenance requests get lost or forgotten
- Fee collection is manual and error-prone
- Assembly meetings are poorly documented
- Legal deadlines are missed
- Emergency response is slow
- No visibility into building financial health
- Communication relies on physical bulletin boards

### Target Market

| Segment | Description | Primary Pain Point |
|---------|-------------|-------------------|
| Regular Managers (Upravnici) | Owner-elected volunteers | Lack of tools & organization |
| Professional Managers | Licensed professionals managing multiple buildings | Need 24/7 availability, compliance tracking |
| Residential Communities | Stambene zajednice | Legal compliance, deadline tracking |
| Property Management Companies | Companies managing multiple buildings | Scaling operations efficiently |

---

## User Personas

### Persona 1: Milan - Regular Building Manager

**Demographics:**
- Age: 55 years old
- Occupation: Retired engineer
- Location: Belgrade, Serbia
- Tech Comfort: Moderate (uses smartphone, WhatsApp, online banking)
- Time Available: 2-3 hours per week for building duties

**Professional Context:**
- Owner of apartment in 24-unit building (built 1985)
- Building has: 24 apartments, 12 garages, 8 storage units
- Elected as manager by assembly (3-year term)
- Receives small monthly compensation (5,000 RSD ≈ 45 EUR)
- Not licensed, not required to be (building under 25 units)

**Goals:**
- Keep building records organized and accessible
- Respond quickly to resident requests
- Track who has which keys (especially entrance keys)
- Collect maintenance fees on time (currently 80% collection rate)
- Organize annual assembly meeting
- Handle emergencies (leaks, power outages, elevator issues)
- Leave good legacy for next manager

**Frustrations:**
- Papers get lost or damaged
- Can't remember who reported what problem and when
- Never sure if all fees are paid without checking bank statements
- Difficult to reach all 24 owners (some live abroad, some are tenants)
- Meeting attendance is poor (last year only 8 of 24 attended)
- No backup when he's away or sick
- Owners complain about lack of transparency

**Daily Tasks:**
- Check for new maintenance requests
- Follow up on outstanding work orders
- Answer resident calls/messages
- Record key handouts/returns
- Update payment records (manually from bank)

**Key Use Cases:**
1. Look up resident phone number quickly (emergency)
2. Record who took which key (with photo of ID)
3. Log a maintenance request from resident call
4. Send announcement about water shutoff
5. Check who hasn't paid fees this month
6. Schedule and document annual assembly meeting

**Success Metrics (Milan's View):**
- Residents can reach him easily
- Problems get fixed within a week
- 90% fee collection rate
- No lost keys
- Smooth assembly meetings

---

### Persona 2: Dragan - Professional Manager

**Demographics:**
- Age: 42 years old
- Occupation: Licensed professional building manager
- Location: Novi Sad, Serbia
- Tech Comfort: High (uses multiple apps, comfortable with technology)
- Time Available: Full-time job (manages buildings professionally)

**Professional Context:**
- Licensed by Chamber of Engineers (license valid 10 years)
- Works for "Stambeno Upravljanje Plus" management company
- Manages 8 buildings (203 units total)
- Required to be available 24/7 for all buildings
- Must have liability insurance (10,000 EUR minimum)
- Required to submit reports twice yearly per building
- Subject to forced administration assignments

**Buildings Managed:**
| Building | Units | Type | Notes |
|----------|-------|------|-------|
| Nikola Tesla 12 | 48 | Large residential | Has elevator, complex |
| Cara Dušana 5 | 24 | Medium residential | Built 1970, frequent repairs |
| Đure Jakšića 8 | 36 | Medium residential | New construction, 2018 |
| Laze Kostića 15 | 18 | Small residential | Easy to manage |
| Braće Radić 3 | 32 | Mixed use | Commercial + residential |
| Novosadska 25 | 15 | Small residential | Forced administration |
| Temerinska 10 | 12 | Small residential | Many elderly residents |
| Futoška 7 | 18 | Medium residential | Recently renovated |

**Goals:**
- Efficiently manage all 8 buildings from phone
- Receive and respond to reports at any hour
- Track all legal deadlines across buildings
- Maintain insurance compliance
- Generate required semi-annual reports
- Propose fees with proper documentation (3 bids required)
- Coordinate with local government when needed
- Handle forced administration buildings correctly

**Frustrations:**
- Can't be in 8 places at once
- Phone rings all night with complaints
- Keeping track of 8 different registration deadlines
- Insurance paperwork and renewal
- Producing 16 reports per year (2 per building)
- Dealing with forced administration bureaucracy
- Owners who don't understand his legal obligations
- Substitute manager arrangements when traveling

**Daily Tasks:**
- Review overnight messages/calls
- Check deadline dashboard across all buildings
- Log 24/7 reports as they come in
- Follow up on emergency interventions
- Coordinate with service providers
- Update work orders
- Prepare for upcoming assemblies
- Document everything for liability protection

**Key Use Cases:**
1. Receive problem report at 2 AM (pipe burst)
2. See all deadlines across 8 buildings on one screen
3. Track insurance expiration (alert at 90 days)
4. Generate semi-annual report for building
5. Record 3 bids for fee proposal (with attachments)
6. Hand over to substitute manager (power of attorney)
7. Handle forced administration case (Novosadska 25)
8. Mark emergency intervention complete within 48 hours

**Success Metrics (Dragan's View):**
- All deadlines met
- No insurance lapses
- 48-hour emergency response
- Reports submitted on time
- No complaints to management company
- Forced administration handovers smooth

---

### Persona 3: Ana - Unit Owner

**Demographics:**
- Age: 35 years old
- Occupation: Marketing manager at tech company
- Location: Belgrade, Serbia (lives in managed building)
- Tech Comfort: High (daily app user, mobile-first)
- Time Available: Limited (busy professional, family)

**Property Context:**
- Owns one apartment (85 m², 4th floor)
- Lives there with husband and two children
- Rents out parking space to neighbor
- Not involved in building management
- Wants building well-maintained but doesn't want to manage
- Attends assembly meetings when she can

**Goals:**
- Pay maintenance fees easily (from phone)
- Report maintenance issues quickly
- Know what's happening in building
- Participate in important decisions (vote)
- Access financial information (where does money go?)
- Update contact information when needed
- Download official documents when required

**Frustrations:**
- Never knows when assembly meetings are scheduled
- Fee payment requires bank visit or manual entry
- Can't see how money is spent
- Announcements posted on bulletin board she never checks
- Slow response to maintenance requests
- No transparency into building finances
- Feels disconnected from building community

**Monthly Tasks:**
- Pay maintenance fee
- Check for announcements
- Report any issues
- Check if any upcoming meetings

**Occasional Tasks:**
- Attend assembly meeting
- Vote on decisions
- Update contact info
- Download financial statement

**Key Use Cases:**
1. Pay monthly fee via mobile app (QR or card)
2. Submit maintenance request with photo
3. View upcoming assembly meeting details
4. Vote on decisions electronically
5. See financial statement (income/expenses)
6. Update phone number after changing
7. Download official documents (decision, minutes)
8. See who else lives in building

**Success Metrics (Ana's View):**
- Fee payment takes 30 seconds
- Maintenance requests acknowledged within hours
- Knows about meetings in advance
- Can vote without attending in person
- Understands where fees go

---

### Persona 4: Petar - Tenant

**Demographics:**
- Age: 28 years old
- Occupation: Software developer at startup
- Location: Belgrade, Serbia (rents in managed building)
- Tech Comfort: Very high (software professional)
- Time Available: Flexible (works remotely)

**Housing Context:**
- Rents apartment (55 m², 2nd floor)
- Has lived there 2 years
- Landlady lives abroad (Serbian diaspora)
- Pays rent to landlady, not building fees directly
- Not owner, limited access to owner-only information
- May move in 1-2 years (typical renter pattern)

**Goals:**
- Report issues in apartment and common areas
- Know building rules (quiet hours, garbage, parking)
- Find emergency contacts quickly
- Communicate with manager easily
- Reserve common areas for events
- Stay informed about building issues

**Frustrations:**
- Doesn't know who to call for problems
- Never informed about water/electricity shutoffs
- Can't access building documents (owner only)
- Parking rules unclear
- Manager hard to reach
- No way to reserve laundry room

**As-Needed Tasks:**
- Report maintenance issue
- Check for announcements
- Find emergency contact
- Reserve common room
- Contact manager

**Key Use Cases:**
1. Report broken intercom (with photo)
2. View emergency contacts (plumber, electrician, manager)
3. See upcoming maintenance schedule (water shutoff)
4. Reserve common room for birthday party
5. Read building rules (quiet hours, garbage)
6. Contact manager via in-app chat

**Success Metrics (Petar's View):**
- Knows who to call in emergency
- Issues acknowledged quickly
- Informed about disruptions
- Can reach manager easily

---

### Persona 5: Jelena - Property Company Admin

**Demographics:**
- Age: 38 years old
- Occupation: Operations Manager at "Stambeno Upravljanje Plus"
- Location: Novi Sad, Serbia
- Tech Comfort: High (business apps, spreadsheets)
- Time Available: Full-time job

**Professional Context:**
- Works at professional management company
- Oversees 15 professional managers (including Dragan)
- Company manages 120 buildings total
- Responsible for compliance, licensing, and operations
- Reports to company director
- Interfaces with local government

**Goals:**
- Ensure all managers maintain valid licenses
- Track insurance across all managers
- Monitor deadline compliance across portfolio
- Assign forced administration cases
- Generate company-wide reports
- Handle customer complaints about managers
- Coordinate with local government

**Frustrations:**
- Manual tracking of 15 licenses
- Insurance renewals slip through cracks
- No visibility into manager activities
- Complaints from building owners
- Forced administration paperwork
- Difficulty scaling operations

**Daily Tasks:**
- Check manager compliance dashboard
- Assign new forced administration cases
- Review manager reports
- Handle escalated complaints
- Coordinate with local government
- Track company-wide deadlines

**Key Use Cases:**
1. View all manager licenses and expirations
2. Assign forced administration to specific manager
3. View portfolio-wide deadline status
4. Generate company compliance report
5. Review and approve substitute manager arrangements
6. Handle owner complaint about manager

**Success Metrics (Jelena's View):**
- All licenses valid
- All insurance current
- No compliance violations
- Forced administrations handled correctly
- Satisfied building owners

---

### Persona 6: Slobodan - Substitute Manager

**Demographics:**
- Age: 45 years old
- Occupation: Professional manager (freelance)
- Location: Belgrade, Serbia
- Tech Comfort: High
- Time Available: Variable (covers for other managers)

**Professional Context:**
- Licensed professional manager
- Works as substitute for other professional managers
- Takes over when primary manager is ill, traveling, or unavailable
- Must have power of attorney documented
- Limited authority (emergency decisions only)
- Covers 2-3 buildings at a time, temporarily

**Goals:**
- Quickly understand building status when taking over
- Handle emergencies during coverage period
- Document all actions for primary manager
- Return building in good order
- Maintain liability coverage

**Frustrations:**
- No context when taking over building
- Don't know ongoing issues
- Hard to make decisions without history
- Unclear what authority exists
- Awkward communication with residents

**Key Use Cases:**
1. Receive handover summary for building
2. View ongoing work orders
3. See upcoming deadlines
4. Log emergency action taken
5. Return building to primary manager
6. Document power of attorney

---

### Persona 7: Goran - Local Government Official

**Demographics:**
- Age: 50 years old
- Occupation: Clerk at City Administration (Mesna kancelarija)
- Location: Belgrade, Serbia
- Tech Comfort: Moderate
- Time Available: Full-time job

**Professional Context:**
- Works at local government office
- Handles residential community registrations
- Receives reports from professional managers
- Initiates forced administration proceedings
- Reviews appeals and complaints

**Goals:**
- Process community registrations efficiently
- Monitor forced administration buildings
- Receive required reports from managers
- Track building compliance
- Handle appeals correctly

**Key Use Cases:**
1. View registered communities
2. Receive registration applications
3. Assign forced administration
4. Review semi-annual reports
5. Process appeals

**Note:** This persona represents external stakeholder. The system may need features for reporting TO government, not necessarily FOR government users.

---

## Core Use Cases by Priority

### P0 - Critical (MVP - Must Have)

#### UC-001: View Resident Directory
**Actor:** Building Manager
**Goal:** Quickly find contact information for any resident
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Building has at least one resident registered
- Manager has permission to view resident data

**Main Success Scenario:**
1. Manager opens app
2. Navigates to "Residents" section
3. Searches by name, unit number, or phone
4. Views resident details including:
   - Full name
   - Unit number
   - Phone number(s)
   - Email
   - Emergency contact
   - Relationship (owner/tenant/family)
5. Can call or message directly from app

**Alternative Flows:**
- 3a. No results found: Display "No residents match your search" with option to add new
- 5a. Multiple phone numbers: Show primary first, tap to see others

**Postconditions:**
- Manager has found needed contact information
- No data modifications made

**Business Value:** 
- Saves 4-5 minutes per lookup (vs. paper records)
- Always have current information
- Available offline for emergencies
- Reduces "can't reach anyone" situations

**Acceptance Criteria:**
- [ ] Search returns results in under 1 second
- [ ] Works offline with last synced data (cached)
- [ ] Can initiate call/message directly from result (one tap)
- [ ] Shows relationship (owner/tenant/family member)
- [ ] Sortable by name, unit number, last contact date
- [ ] Filterable by type (owners only, tenants only)
- [ ] Emergency contacts clearly highlighted

**Error Handling:**
- Offline mode: Show banner "Offline - showing cached data from [date]"
- No results: Clear message with add new option
- Permission denied: "You don't have permission to view residents"

---

#### UC-002: Log Key Handout
**Actor:** Building Manager
**Goal:** Record when a key is given to someone
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Building has keys registered in system
- Manager has permission to manage keys

**Main Success Scenario:**
1. Manager opens app
2. Selects "Keys" section
3. Selects key type (unit key, entrance key, storage key, etc.)
4. Selects or enters recipient information:
   - Name (can select from residents OR enter manually)
   - Phone number
   - ID number (optional)
   - Purpose (repair, viewing, new resident, etc.)
   - Expected return date
5. Takes photo of person's ID (optional but recommended)
6. Confirms handout
7. System generates receipt (PDF/SMS)
8. Key marked as "checked out" in inventory

**Alternative Flows:**
- 4a. Recipient is registered resident: Auto-fill name, phone, unit
- 7a. Print receipt: Generate printable PDF for signature
- 7b. SMS receipt: Send confirmation to recipient phone

**Postconditions:**
- Key status changed to "checked out"
- Checkout record created with timestamp
- Recipient information recorded
- Return deadline set

**Business Value:**
- Complete audit trail for all keys
- Know who has access at all times
- Prevents "who has the key?" disputes
- Legal protection for manager

**Acceptance Criteria:**
- [ ] Can complete entire flow in under 30 seconds
- [ ] Works offline, syncs when connected
- [ ] Can generate printable or SMS receipt
- [ ] Links to resident record if exists
- [ ] Photo of ID stored securely
- [ ] Overdue return triggers alert
- [ ] Key history viewable (all checkouts/returns)

**Error Handling:**
- Key already checked out: Show who has it and when due
- Offline mode: Queue for sync, show pending indicator
- Camera unavailable: Allow skip with warning

---

#### UC-003: Create Maintenance Request
**Actor:** Resident (Owner or Tenant)
**Goal:** Report a problem that needs fixing
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Resident has logged into the app
- Resident is associated with at least one unit
- App has camera access (optional, for photos)

**Main Success Scenario:**
1. Resident opens app
2. Taps "New Request" button (prominent on home screen)
3. Selects problem category:
   - Plumbing
   - Electrical
   - Elevator
   - Common areas
   - Facade/roof
   - Entrance/intercom
   - Heating
   - Other
4. Enters problem description (text, min 10 characters)
5. Takes photo of problem (optional, up to 3 photos)
6. Indicates urgency:
   - Low (can wait weeks)
   - Normal (within a week)
   - High (within days)
   - Emergency (immediate danger)
7. Confirms location (auto-filled from resident's unit)
8. Submits request
9. Receives confirmation with reference number
10. Manager receives notification

**Alternative Flows:**
- 3a. Previous requests exist: Show similar past requests for reference
- 6a. Emergency selected: Show warning to also call manager directly
- 8a. Different location: Resident can select different unit or common area

**Postconditions:**
- Work order created with "New" status
- Reference number generated
- Manager notified (push notification)
- Resident can track status

**Business Value:**
- No more lost verbal requests
- Documentation with photos
- Priority visibility for manager
- History of all issues per unit

**Acceptance Criteria:**
- [ ] Can submit in under 1 minute
- [ ] Photo upload works reliably (compression, retry on failure)
- [ ] Confirmation received within 5 seconds
- [ ] Reference number unique and readable (e.g., WO-2024-0123)
- [ ] Can track status of request in real-time
- [ ] Push notification sent to manager immediately
- [ ] Offline mode: Queue request, send when connected

**Error Handling:**
- Photo too large: Compress automatically
- No internet: Queue locally, show "pending" indicator
- Description too short: Show character counter

---

#### UC-004: View and Update Work Orders
**Actor:** Building Manager
**Goal:** See all maintenance requests and manage them
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- At least one work order exists
- Manager has permission to manage work orders

**Main Success Scenario:**
1. Manager opens app
2. Sees dashboard with work order count by status
3. Taps to see list of work orders sorted by:
   - Priority (Emergency first)
   - Date created (newest first)
   - Status (New → In Progress → Completed)
4. Filters by status (tabs: All, New, In Progress, Completed)
5. Taps work order to see details:
   - Reporter information
   - Problem description and photos
   - Location
   - Priority
   - Created date
   - Status history
6. Updates status:
   - Acknowledged (I've seen it)
   - In Progress (working on it)
   - Waiting for Parts
   - Waiting for Decision
   - Completed
7. Adds notes visible to resident
8. Assigns to service provider (optional)
9. Adds completion photos (optional)
10. Resident receives notification of update

**Alternative Flows:**
- 6a. Mark as Emergency: Trigger 48-hour countdown
- 8a. Service provider not in system: Quick-add new provider
- 10a. Resident has notifications off: Note in work order that notification skipped

**Postconditions:**
- Work order status updated
- Timestamp recorded
- Resident notified (if possible)
- History preserved

**Business Value:**
- Never lose track of requests
- Residents see progress
- Complete history for each unit
- Accountability documentation

**Acceptance Criteria:**
- [ ] List loads in under 2 seconds
- [ ] Can filter by status, priority, date range
- [ ] Can sort by multiple criteria
- [ ] Push notification sent to resident on status change
- [ ] All history preserved (audit trail)
- [ ] Can export to PDF for documentation
- [ ] Emergency work orders clearly highlighted

**Error Handling:**
- Many work orders: Pagination, search
- Resident unreachable: Note in work order

---

#### UC-005: Send Announcement
**Actor:** Building Manager
**Goal:** Quickly notify all residents of important information
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Manager has permission to send announcements
- At least one resident has contact information

**Main Success Scenario:**
1. Manager opens app
2. Selects "Announcements" section
3. Taps "New Announcement"
4. Enters title (required, max 100 chars)
5. Enters message (required, max 1000 chars)
6. Selects type:
   - General Information
   - Emergency Alert
   - Maintenance Notice
   - Assembly Meeting
   - Payment Reminder
7. Chooses recipients:
   - All owners
   - All tenants
   - All residents (owners + tenants)
   - Specific units (multi-select)
8. Selects delivery method:
   - In-app notification (free)
   - SMS (costs apply)
   - Email
   - All of the above
9. Schedules delivery:
   - Send now
   - Schedule for specific date/time
10. Reviews preview
11. Confirms and sends
12. Sees delivery statistics

**Alternative Flows:**
- 6a. Emergency Alert: Bypass quiet hours, require confirmation
- 8a. SMS cost warning: Show estimated cost before sending
- 9a. Schedule in past: Error, must be future

**Postconditions:**
- Announcement created in system
- Sent via selected channels
- Delivery status tracked
- Residents can view in app

**Business Value:**
- Reach everyone in under 1 minute
- Emergency alerts for safety
- Documented communication (legal proof)
- No more bulletin board only

**Acceptance Criteria:**
- [ ] Can send to all residents in under 1 minute
- [ ] Delivery confirmation within 5 minutes
- [ ] Can schedule for future date/time
- [ ] Residents see announcement in app feed
- [ ] Emergency announcements override quiet hours
- [ ] Delivery statistics (sent, delivered, read)
- [ ] Can attach images (for meeting notices)
- [ ] SMS cost estimate before sending

**Error Handling:**
- No recipients: Clear message explaining why
- SMS balance low: Warning before send
- Schedule conflict: Allow override

---

#### UC-006: Record Assembly Meeting
**Actor:** Building Manager
**Goal:** Document assembly meeting and decisions properly
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Meeting was announced (UC-007 completed)
- Manager has permission to manage assemblies

**Main Success Scenario:**
1. Manager opens existing meeting (created via UC-007)
2. At meeting time, records attendance:
   - Who attended in person (check in by unit)
   - Who sent proxy (with proxy holder info)
   - Who submitted written vote in advance
   - Who is voting electronically
3. System calculates:
   - Total eligible votes (sum of all unit shares)
   - Present votes (in person + proxy + written + electronic)
   - Quorum status (51% first session, 1/3 repeated)
4. Manager records each agenda item
5. For each item requiring vote:
   - Records voting type required (unanimous/2/3/simple)
   - Records votes (for/against/abstain)
   - System validates if threshold met
   - Records result (passed/failed)
6. Manager adds notes/comments
7. Manager finalizes minutes
8. System generates official minutes document
9. Minutes distributed to all owners automatically

**Alternative Flows:**
- 3a. Quorum not met (first session): Schedule repeated session, 3b
- 3b. Quorum met (repeated session): Continue with 1/3 threshold
- 5a. Wrong voting type: System warns if type doesn't match decision
- 5b. Threshold not met: Decision fails, record anyway

**Postconditions:**
- Meeting minutes created
- All decisions recorded with proper validation
- Attendance recorded
- Minutes distributed to owners
- Quorum calculation documented

**Business Value:**
- Legal compliance with Serbian law
- Transparent decisions
- Correct voting procedures enforced
- Documented proof of valid assembly

**Acceptance Criteria:**
- [ ] Automatic quorum calculation based on unit shares
- [ ] Validation of voting thresholds (unanimous/2/3/simple)
- [ ] Minutes generated automatically in proper format
- [ ] Can add electronic votes after in-person meeting
- [ ] Proxy tracking with proper documentation
- [ ] Unavailable owner tracking (3 missed sessions)
- [ ] PDF minutes for download/print

**Error Handling:**
- Quorum not met: Clear guidance on repeated session
- Invalid vote combination: Prevent and explain

---

#### UC-007: Notify About Assembly Meeting
**Actor:** Building Manager
**Goal:** Notify owners of upcoming assembly meeting legally
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Manager has permission to manage assemblies
- All owners have at least one contact method

**Main Success Scenario:**
1. Manager navigates to "Assemblies" section
2. Taps "New Assembly"
3. Enters meeting details:
   - Date and time
   - Location (building address, common room, etc.)
   - Agenda items (at least one required)
   - Agenda attachments (optional)
4. System calculates:
   - Notice deadline (minimum 3 days before)
   - Voting requirements for each agenda item
5. Manager reviews and confirms
6. System sends notice via:
   - In-app notification to all owners
   - Email (if available)
   - SMS (optional, costs apply)
   - Generates printable notice for physical posting
7. Manager confirms physical posting (checkbox)
8. System sends reminders:
   - 2 days before
   - 1 day before
   - Morning of meeting

**Alternative Flows:**
- 4a. Less than 3 days notice: Warning shown, manager can override
- 6a. Some owners have no contact: Show list of unreachable owners
- 8a. Reminder disabled: Manager can disable individual reminders

**Postconditions:**
- Assembly meeting created
- All owners notified
- Notice period respected
- Physical posting documented

**Business Value:**
- Legal compliance (3-day notice minimum)
- Better attendance through reminders
- Proof of notice delivery
- Professional meeting management

**Acceptance Criteria:**
- [ ] Warning if less than 3 days notice
- [ ] Delivery tracking per owner per channel
- [ ] Automatic reminders sent
- [ ] Can attach agenda and proxy form (PDF)
- [ ] Physical posting checkbox for legal proof
- [ ] Can generate printable notice
- [ ] List of owners who received/viewed notice

**Error Handling:**
- Date in past: Prevent creation
- No contact for owner: Highlight in dashboard

---

#### UC-008: Track Fee Payments
**Actor:** Building Manager, Unit Owner
**Goal:** Know who has paid maintenance fees
**Priority:** P0 - Critical (MVP)

**Preconditions (Manager):**
- Manager has logged into the app
- Fee structure is defined (amount per m² or fixed)
- Manager has permission to view finances

**Preconditions (Owner):**
- Owner has logged into the app
- Owner is associated with at least one unit

**Main Success Scenario (Manager):**
1. Manager sees dashboard with payment status overview:
   - Total collected this month
   - Total outstanding
   - Units paid/unpaid
2. Sees visual indicator per unit:
   - Green: Paid current month
   - Yellow: Paid last month, not current
   - Red: 2+ months unpaid
3. Taps unit to see payment history
4. Can record manual payment:
   - Date received
   - Amount
   - Payment method (cash, bank transfer, etc.)
   - Reference number
5. Can send payment reminder to specific units
6. Can export unpaid units list (PDF/Excel)

**Main Success Scenario (Owner):**
1. Owner sees dashboard with own payment status
2. Views current amount due
3. Views payment history (last 12 months)
4. Can download payment confirmation for any past payment
5. Sees upcoming payment due date

**Alternative Flows:**
- 4a. Payment amount differs: Manager can record partial or overpayment
- 5a. Owner has multiple units: See all units' status

**Postconditions:**
- Payment recorded in system
- Balance updated
- History preserved

**Business Value:**
- No surprise unpaid fees
- Easy to follow up with non-payers
- Owner self-service reduces manager queries
- Financial transparency

**Acceptance Criteria:**
- [ ] Real-time payment status dashboard
- [ ] Export to Excel/PDF for unpaid list
- [ ] Payment reminder automation (can send to multiple)
- [ ] History preserved indefinitely
- [ ] Can record partial payments
- [ ] Can add notes to payments
- [ ] Owner sees only own units

**Error Handling:**
- Negative balance: Clear indication and reason
- Duplicate payment entry: Warning before saving

---

#### UC-009: Register New Resident
**Actor:** Building Manager
**Goal:** Add a new resident to the building directory
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Manager has permission to manage residents
- Unit exists in system

**Main Success Scenario:**
1. Manager taps "Add Resident"
2. Selects or creates unit:
   - Existing unit (select from list)
   - New unit (enter number, floor, area m²)
3. Enters resident information:
   - First name, last name
   - Phone number (primary)
   - Phone number (secondary, optional)
   - Email (optional)
   - Relationship (owner/tenant/family)
   - Move-in date
   - Emergency contact (name, phone)
4. For owners: Enter ownership share (%)
5. For tenants: Enter owner reference
6. Uploads ID document (optional)
7. Saves resident
8. Resident receives invitation to download app (if email/phone provided)

**Alternative Flows:**
- 3a. Existing resident: Edit instead of create
- 8a. No contact info: Skip invitation, manual only

**Postconditions:**
- Resident added to directory
- Unit association created
- Invitation sent (if possible)

**Business Value:**
- Keep directory current
- Know who lives where
- Legal compliance (tenant registration within 30 days)

**Acceptance Criteria:**
- [ ] Can add resident in under 2 minutes
- [ ] Phone/email validation
- [ ] Owner share validation (total = 100%)
- [ ] Can mark resident as inactive (moved out)
- [ ] History of residents per unit preserved

---

#### UC-010: Return Key (Check-in)
**Actor:** Building Manager
**Goal:** Record that a key has been returned
**Priority:** P0 - Critical (MVP)

**Preconditions:**
- Manager has logged into the app
- Key was previously checked out (UC-002)
- Manager has permission to manage keys

**Main Success Scenario:**
1. Manager sees "Keys Due" list on dashboard
2. Taps overdue or due key
3. Selects "Return Key"
4. Confirms recipient identity
5. Inspects key condition
6. Records:
   - Return date (auto-filled)
   - Key condition (good/damaged/lost)
   - Notes (optional)
7. Confirms return
8. Key status changed to "available"
9. Checkout record marked complete

**Alternative Flows:**
- 6a. Key damaged: Record damage, may require replacement
- 6b. Key lost: Mark as lost, create replacement task

**Postconditions:**
- Key available for checkout
- Complete checkout/return history
- Overdue alerts cleared

**Business Value:**
- Know keys are back
- Track key condition
- Complete audit trail

**Acceptance Criteria:**
- [ ] Overdue keys prominently displayed
- [ ] One-tap return for simple cases
- [ ] Condition tracking
- [ ] Lost key workflow

---

### P1 - High Priority (Phase 2)

#### UC-011: Register Residential Community
**Actor:** Building Manager
**Goal:** Register community with local government as legal entity
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Manager has logged into the app
- Building is not yet registered
- First assembly meeting has occurred
- Manager has assembly decision document

**Main Success Scenario:**
1. Manager navigates to "Community" section
2. Selects "Register Community"
3. System shows auto-filled information:
   - Business name (generated from address: "SZ [Street] [Number]")
   - Building address
   - Total area (m²)
   - Number of units
4. Manager enters:
   - Manager name and ID
   - First assembly date
   - Assembly decision number
5. Uploads required documents:
   - Assembly decision (mandatory)
   - Building permit or cadastre extract (optional)
6. System calculates registration deadline (15 days from assembly)
7. Manager confirms application
8. System generates registration application (PDF)
9. Manager downloads and submits to local government
10. Manager records:
    - Submission date
    - MB number (when received)
    - PIB (when obtained)
    - Bank account (when opened)
11. System tracks all milestones

**Alternative Flows:**
- 3a. Address incomplete: Prompt to complete building setup
- 10a. Registration rejected: Record reason, generate appeal

**Postconditions:**
- Community registration workflow started
- Deadline tracked (15 days)
- Documents stored
- MB/PIB recorded when received

**Business Value:**
- Legal compliance (legal entity status)
- Deadline tracking prevents non-compliance
- Document storage
- One-stop registration tracking

**Acceptance Criteria:**
- [ ] Auto-generated business name in correct format
- [ ] 15-day deadline countdown from assembly date
- [ ] PDF application form generated
- [ ] Checklist of required documents
- [ ] Milestone tracking (MB, PIB, bank account)
- [ ] Warning if deadline approaching

---

#### UC-012: Track Deadlines
**Actor:** Building Manager, Professional Manager
**Goal:** Never miss a legal deadline
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Manager has logged into the app
- Building is registered in system
- Manager has permission to view compliance

**Main Success Scenario:**
1. Manager sees "Deadlines" widget on dashboard
2. Widget shows:
   - Number of upcoming deadlines (next 30 days)
   - Number of overdue items (if any)
3. Taps to see full deadline list sorted by:
   - Due date (soonest first)
   - Priority (critical/high/normal)
4. Each deadline shows:
   - Description
   - Due date
   - Days remaining (countdown)
   - Status (pending, in progress, completed, overdue)
   - Related building (for multi-building managers)
5. Taps deadline to see details:
   - Full description
   - Legal basis
   - Required actions
   - Related documents
   - History of this deadline type
6. Manager marks deadline as:
   - In Progress (working on it)
   - Completed (with completion date and notes)
7. System sends automatic reminders:
   - 7 days before
   - 3 days before
   - 1 day before
   - Day of deadline
   - Overdue (escalated)

**Key Deadlines Tracked:**
| Deadline | Period | Trigger | Consequence |
|----------|--------|---------|-------------|
| First assembly | 60 days | Building becomes legal entity | Possible forced administration |
| Community registration | 15 days | First assembly held | Non-compliance |
| Update registration | 15 days | Manager/decision change | Non-compliance |
| Tenant data submission | 30 days | New lease signed | Non-compliance |
| Emergency intervention | 48 hours | Emergency reported | Liability |
| Maintenance Program | End of Feb | Annually | Non-compliance |
| Appeal compulsory mgmt | 8 days | Decision received | Lost appeal rights |
| Semi-annual report | Twice/year | Fixed dates | Non-compliance |
| Insurance renewal | Annual | Policy expiration | License at risk |

**Alternative Flows:**
- 4a. No deadlines: Show "All caught up!" message
- 6a. Deadline cannot be completed: Mark as blocked with reason

**Postconditions:**
- Deadline status updated
- Reminders sent appropriately
- Completion documented

**Business Value:**
- Avoid legal penalties
- Prevent forced administration
- Peace of mind
- Professional compliance tracking

**Acceptance Criteria:**
- [ ] Dashboard widget always visible
- [ ] Automatic reminders at configurable intervals
- [ ] Color coding (green/yellow/red by urgency)
- [ ] Can create custom deadlines
- [ ] Deadline history preserved
- [ ] Multi-building view for professionals

---

#### UC-013: Receive 24/7 Problem Reports (Professional Manager)
**Actor:** Professional Manager
**Goal:** Log and respond to reports received at any hour
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Professional manager has logged into the app
- Manager is assigned to at least one building
- Manager has professional manager status in system

**Main Success Scenario:**
1. Report comes in (call, message, in-app) at any hour
2. Manager opens "24/7 Reports" section
3. Taps "New Report"
4. Logs report with:
   - Date/time (auto-filled, editable)
   - Building (select from assigned)
   - Reporter name (if given, or "anonymous")
   - Reporter contact (if provided)
   - Problem description
   - Location (unit, common area, etc.)
   - Category (plumbing, electrical, etc.)
   - Urgency assessment
5. Manager indicates immediate action taken:
   - None required
   - Advised reporter
   - Dispatched service provider
   - Notified competent authority
   - Other (with description)
6. Manager saves report
7. Report appears in:
   - Building's work orders (if action needed)
   - 24/7 report log
   - Semi-annual report data
8. System tracks response time

**Alternative Flows:**
- 4a. Reporter is registered resident: Auto-fill from directory
- 5a. Emergency: Mark as emergency, trigger 48-hour workflow
- 7a. No action needed: Report logged for documentation only

**Postconditions:**
- Report logged with timestamp
- Action recorded
- Data available for semi-annual report
- Response time tracked

**Business Value:**
- Legal compliance (24/7 availability requirement)
- Documentation for liability protection
- Building issue tracking
- Report data for semi-annual submissions

**Acceptance Criteria:**
- [ ] Can log report in under 60 seconds
- [ ] Works offline, syncs when connected
- [ ] Timestamp accurate to minute
- [ ] Can attach photos
- [ ] Links to work order if action needed
- [ ] Report history searchable
- [ ] Export for semi-annual report

---

#### UC-014: Manage Professional Manager License
**Actor:** Professional Manager, Company Admin
**Goal:** Keep license and insurance valid
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Professional manager has logged into the app
- Manager has professional status in system
- Company admin (if applicable) has access

**Main Success Scenario:**
1. Manager navigates to "Professional Status" section
2. Views license information:
   - License number
   - Issuing authority
   - Issue date
   - Expiration date (10 years from issue)
   - License document (uploaded PDF)
3. Views insurance information:
   - Policy number
   - Insurance provider
   - Coverage amount (minimum 10,000 EUR)
   - Valid from/to (minimum 3 years duration)
   - Annual premium due date
   - Insurance document (uploaded PDF)
4. System validates:
   - Coverage ≥ 10,000 EUR (warning if less)
   - Duration ≥ 3 years (warning if less)
   - Annual proof submission status
5. System shows alerts:
   - License expiring (90/60/30 days)
   - Insurance expiring (90/60/30 days)
   - Annual proof not submitted
6. Manager can update:
   - Upload new license document
   - Upload new insurance document
   - Record annual proof submission
7. Company admin (Jelena) can view all managers' status

**Alternative Flows:**
- 4a. Coverage below minimum: Prominent warning
- 4b. Duration below minimum: Prominent warning
- 5a. Already expired: Critical alert, mark non-compliant

**Postconditions:**
- License/insurance information current
- Expiration tracked
- Compliance status visible

**Business Value:**
- Avoid license revocation
- Maintain professional status
- Compliance tracking
- Company oversight (for management companies)

**Acceptance Criteria:**
- [ ] Expiration countdown visible
- [ ] Alerts at 90/60/30 days
- [ ] Warning for non-compliant amounts/durations
- [ ] Document upload and storage
- [ ] Company admin dashboard for all managers
- [ ] Compliance status indicator (green/yellow/red)
- [ ] Historical record of documents

---

#### UC-015: Vote with Correct Majority
**Actor:** Building Manager, Assembly
**Goal:** Ensure decisions use correct voting threshold
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Assembly meeting in progress (UC-006)
- Agenda item requires vote
- Manager has permission to manage assemblies

**Main Success Scenario:**
1. Manager selects agenda item to vote on
2. System prompts for decision type:
   - Adopt Owners' Rules (Pravilnik)
   - Hire/fire professional manager
   - Take loan
   - Dispose of common parts
   - Establish cost criteria for professional manager
   - Regular decision (default)
3. System shows required majority:
   - UNANIMOUS (100% of present votes) - for Owners' Rules
   - 2/3 MAJORITY (66.67%+) - for professional manager, loans, disposal, cost criteria
   - SIMPLE MAJORITY (50%+1) - for regular decisions
4. Manager records votes:
   - Votes for
   - Votes against
   - Abstentions
   - (Can enter per unit or total counts)
5. System calculates:
   - Total eligible votes (sum of shares)
   - Total present votes
   - Percentage for/against/abstain
   - Whether threshold is met
6. System validates result:
   - PASSED (green) or FAILED (red)
   - If failed: Shows how many more votes needed
7. Manager confirms result
8. System records decision with proper validation

**Alternative Flows:**
- 2a. Wrong type selected: System warns if doesn't match agenda item
- 4a. Invalid vote count (exceeds total): Error and correction prompt
- 6a. Threshold not met: Decision can still be recorded as failed

**Postconditions:**
- Decision recorded with validation
- Correct threshold applied
- Result clearly documented

**Business Value:**
- Legal compliance (correct voting procedures)
- Prevent challengeable decisions
- Automatic calculation reduces errors
- Clear documentation

**Acceptance Criteria:**
- [ ] Voting type selection with descriptions
- [ ] Automatic percentage calculation
- [ ] Threshold validation (pass/fail)
- [ ] Clear indication of how many votes needed
- [ ] Vote recording per unit or total
- [ ] Prevent invalid vote combinations
- [ ] Audit trail of all votes

---

#### UC-016: Track Unavailable Owners
**Actor:** Building Manager
**Goal:** Identify and track owners who don't participate in assemblies
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Manager has logged into the app
- Multiple assemblies have been held
- Manager has permission to manage assemblies

**Main Success Scenario:**
1. System automatically tracks assembly attendance per owner
2. After each assembly, system checks:
   - Was owner present? (in person, proxy, written, electronic)
   - Was notice received?
   - Was absence explained?
3. After 3 consecutive assemblies without participation:
   - Owner flagged as "potentially unavailable"
   - Manager sees notification
4. Manager reviews case:
   - Can confirm as unavailable
   - Can mark as "attempted contact, no response"
   - Can reset if owner responds
5. If confirmed unavailable:
   - Owner's share excluded from quorum calculation
   - Owner marked as "unavailable" in directory
   - Automatic restoration when participates
6. Manager can view all unavailable owners
7. System tracks unavailable owner history

**Alternative Flows:**
- 3a. Owner has valid excuse: Manager can reset counter
- 5a. Owner participates: Automatic restoration, counter reset

**Postconditions:**
- Unavailable status tracked
- Quorum calculations adjusted
- History preserved

**Business Value:**
- Easier to achieve quorum
- Legal compliance
- Fair participation tracking
- Encourages owner engagement

**Acceptance Criteria:**
- [ ] Automatic attendance tracking
- [ ] Notification when approaching unavailable status
- [ ] Manual confirmation of unavailable status
- [ ] Automatic restoration on participation
- [ ] Quorum calculation automatically excludes unavailable
- [ ] History of availability status
- [ ] Report of all unavailable owners

---

#### UC-017: Handle Forced Administration
**Actor:** Professional Manager, Company Admin
**Goal:** Manage buildings under forced administration correctly
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Professional manager is assigned to handle forced administration
- Building is in (or about to enter) forced administration
- Manager has professional manager status

**Main Success Scenario:**
1. Company admin assigns forced administration case:
   - Building address
   - Reason for forced administration (no manager, not registered, etc.)
   - Local government reference
   - Assigned manager
   - Fee amount (set by local government)
2. Manager accepts assignment
3. Manager views building summary:
   - Known units and owners
   - Outstanding issues
   - Fee status
   - Required actions
4. Manager takes over building in system:
   - Becomes manager of record
   - Access granted to all building data
5. Manager performs required duties:
   - Logs all 24/7 reports
   - Manages work orders
   - Tracks fee collection
   - Generates reports for local government
6. System tracks forced administration:
   - Start date
   - Required actions
   - Reports due
   - Termination conditions
7. When community elects manager:
   - Manager initiates handover
   - Records new manager information
   - Documents handover
   - Archives forced administration record

**Alternative Flows:**
- 1a. Building not in system: Create building from scratch
- 7a. Appeal filed: Track appeal status

**Postconditions:**
- Forced administration tracked
- All actions documented
- Reports generated for government
- Handover documented

**Business Value:**
- Clear process for difficult situations
- Documentation for all parties
- Compliance with law
- Proper handover when resolved

**Acceptance Criteria:**
- [ ] Assignment workflow for company admin
- [ ] Forced administration dashboard
- [ ] Local government fee tracking
- [ ] Report generation for government
- [ ] Handover workflow
- [ ] Archive of forced administration history
- [ ] Clear status indicator

---

#### UC-018: Record Fee Structure
**Actor:** Building Manager
**Goal:** Define how maintenance fees are calculated
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- Manager has logged into the app
- Building exists in system
- Units have area (m²) recorded
- Manager has permission to manage finances

**Main Success Scenario:**
1. Manager navigates to "Fees" section
2. Selects "Fee Structure"
3. Defines fee calculation:
   - Per m² (amount × unit area)
   - Fixed amount (same for all units)
   - Mixed (base + per m²)
4. Enters fee amount(s)
5. Defines fee frequency:
   - Monthly
   - Quarterly
   - Annually
6. Defines fee categories:
   - Regular maintenance
   - Reserve fund
   - Special assessment
7. Sets fee start date
8. System calculates fees for all units
9. Manager reviews and confirms
10. System shows projected monthly income

**Alternative Flows:**
- 3a. Complex calculation: Custom formula (advanced)
- 9a. Unit missing area: Warning, prompt to add

**Postconditions:**
- Fee structure defined
- Fees calculated per unit
- Collection tracking enabled

**Business Value:**
- Clear fee structure
- Automatic calculation
- Transparent basis for fees
- Accurate financial projections

**Acceptance Criteria:**
- [ ] Multiple calculation methods supported
- [ ] Preview before confirmation
- [ ] Historical fee structures preserved
- [ ] Can update fees with effective date
- [ ] Shows projected income
- [ ] Unit-by-unit fee breakdown

---

#### UC-019: View Building Financial Summary
**Actor:** Building Manager, Unit Owner
**Goal:** See building's financial health at a glance
**Priority:** P1 - High (Phase 2)

**Preconditions:**
- User has logged into the app
- User has permission to view finances
- Financial data exists (fees, expenses)

**Main Success Scenario (Manager):**
1. Manager navigates to "Finances" section
2. Sees dashboard with:
   - Total income (period selectable)
   - Total expenses
   - Balance
   - Collection rate (%)
   - Outstanding receivables
3. Income breakdown:
   - By fee category
   - By unit
   - By month
4. Expense breakdown:
   - By category (utilities, repairs, services)
   - By service provider
   - By month
5. Can drill into any category for details
6. Can export financial report (PDF/Excel)

**Main Success Scenario (Owner):**
1. Owner navigates to "Finances" section
2. Sees summary for building:
   - Total collected
   - Total spent
   - Current balance
3. Sees own contribution vs. total
4. Can view expense categories
5. Can download annual statement

**Alternative Flows:**
- 2a. No data yet: Show empty state with guidance
- 6a. Export: Generate formatted PDF report

**Postconditions:**
- Financial information viewed
- No modifications made

**Business Value:**
- Financial transparency
- Easy reporting
- Owner trust through visibility
- Manager oversight

**Acceptance Criteria:**
- [ ] Dashboard loads in under 2 seconds
- [ ] Period selection (month, quarter, year, custom)
- [ ] Visual charts (income vs. expenses)
- [ ] Export to PDF/Excel
- [ ] Owner sees limited view (no individual unit data)
- [ ] Manager sees full breakdown

---

### P2 - Medium Priority (Phase 3)

#### UC-020: Propose Fees with 3 Bids
**Actor:** Professional Manager
**Goal:** Document fee proposal with required 3 bids
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- Professional manager has logged into the app
- Manager is assigned to building
- Manager has collected bids from service providers

**Main Success Scenario:**
1. Manager navigates to "Fee Proposals" section
2. Taps "New Proposal"
3. Enters proposal details:
   - Proposal name
   - Service type (cleaning, maintenance, security, etc.)
   - Proposed fee amount
   - Valid from/to
   - Reason for proposal
4. Adds bids (minimum 3 required by law):
   - For each bid:
     - Service provider name
     - Contact information
     - Scope of services (detailed)
     - Amount offered
     - Valid until date
     - Supporting document (PDF)
5. System validates: At least 3 bids entered
6. System shows side-by-side comparison:
   - Provider names
   - Amounts
   - Key service differences
7. Manager records recommendation:
   - Which bid recommended
   - Reasoning
8. Manager attaches proposal to assembly agenda
9. Assembly votes on proposal (via UC-006)
10. If approved, fee structure updated

**Alternative Flows:**
- 4a. Less than 3 bids: Warning, cannot proceed without override
- 5a. Override for fewer bids: Document reason (may be non-compliant)

**Postconditions:**
- Proposal documented with all bids
- Ready for assembly presentation
- If approved, fee structure updated

**Business Value:**
- Legal compliance (3 bids required)
- Transparency for owners
- Best value selection
- Documentation for audits

**Acceptance Criteria:**
- [ ] Minimum 3 bids required (with override option)
- [ ] Side-by-side bid comparison
- [ ] Document upload for each bid
- [ ] Recommendation recording
- [ ] Link to assembly agenda item
- [ ] Approval workflow

---

#### UC-021: Generate Semi-Annual Report
**Actor:** Professional Manager
**Goal:** Create required semi-annual report efficiently
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- Professional manager has logged into the app
- Manager is assigned to building
- Manager has been managing for at least 6 months
- Data exists in system (reports, work orders, finances)

**Main Success Scenario:**
1. Manager navigates to "Reports" section
2. Selects "Semi-Annual Report"
3. Selects reporting period:
   - First half (Jan-Jun)
   - Second half (Jul-Dec)
   - Custom date range
4. System pre-populates report with:
   - All 24/7 problem reports received (with timestamps)
   - All maintenance work performed
   - Financial summary (income/expenses)
   - Open issues and status
   - Deadlines tracked
   - Assemblies held
5. Manager reviews each section:
   - Can edit/add commentary
   - Can add recommendations
   - Can highlight issues
6. Manager adds summary and conclusions
7. System generates formatted PDF report:
   - Professional layout
   - Building header
   - All required sections
   - Manager signature
8. Manager downloads PDF
9. Report stored in documents section
10. Can be shared with assembly or local government

**Alternative Flows:**
- 4a. Missing data: Highlight gaps, allow manual entry
- 5a. Add section: Manager can add custom sections

**Postconditions:**
- Report generated and stored
- Data preserved for future reports
- Can be downloaded/shared

**Business Value:**
- Save significant time (hours vs days)
- Nothing forgotten
- Professional appearance
- Compliance documentation

**Acceptance Criteria:**
- [ ] Pre-populated from system data
- [ ] All required sections included
- [ ] Professional PDF layout
- [ ] Can add commentary to sections
- [ ] Stored in document archive
- [ ] Can regenerate with updated data
- [ ] Serbian language formatting

---

#### UC-022: Adopt Owners' Rules (Pravilnik)
**Actor:** Assembly, Building Manager
**Goal:** Create and register custom building rules
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- Manager has logged into the app
- Building is registered as legal entity
- Manager has permission to manage assemblies

**Main Success Scenario:**
1. Manager navigates to "Owners' Rules" section
2. Sees current status (none exist / exists / registered)
3. Taps "Create New Rules" or "Amend Rules"
4. Uploads draft rules document (PDF)
5. System creates special voting item:
   - Type: Unanimous (100% required)
   - All owners must approve
6. Manager initiates voting process:
   - Can be done at assembly
   - Can be done electronically
   - Can combine both
7. System tracks:
   - Who has voted
   - Who has approved
   - Who has not yet voted
8. Manager follows up with non-voters
9. When 100% approval achieved:
   - System confirms unanimous approval
   - Manager collects all owner signatures (physical or digital)
10. Manager records registration:
    - Submission date to local government
    - Registration number
11. Rules distributed to all owners
12. Rules become effective

**Alternative Flows:**
- 5a. Any rejection: Rules fail, must revise and restart
- 7a. Owner unreachable: Document attempts, may need extended timeline
- 9a. Never achieves 100%: Rules cannot be adopted

**Postconditions:**
- Rules documented
- Unanimous approval verified
- Registration tracked
- Rules distributed

**Business Value:**
- Legal compliance
- Customization for building needs
- Proper documentation
- Transparent process

**Acceptance Criteria:**
- [ ] Voting type enforced as unanimous
- [ ] Track all owner approvals
- [ ] Digital signature support
- [ ] Registration workflow
- [ ] Distribution to all owners
- [ ] Document storage

---

#### UC-023: 48-Hour Emergency Intervention
**Actor:** Building Manager
**Goal:** Complete emergency repairs within legal deadline
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- Manager has logged into the app
- Emergency has been reported (UC-003 or UC-013)
- Emergency marked as such

**Main Success Scenario:**
1. Work order marked as "Emergency" (from UC-003 or manually)
2. 48-hour countdown automatically starts
3. Manager sees emergency on dashboard with countdown timer
4. System sends alerts:
   - 12 hours: First reminder (email/push)
   - 24 hours: Escalation reminder (SMS)
   - 40 hours: Final warning (SMS + email)
   - 48 hours: Failure alert (escalation to all contacts)
5. Manager takes action:
   - Dispatches service provider
   - Coordinates with residents
   - Documents work being done
6. Manager documents intervention:
   - Photos of issue
   - Work performed
   - Service provider used
   - Cost (if known)
   - Resolution
7. Manager marks as "Resolved"
8. System records:
   - Time to resolution
   - All documentation
   - Compliance status (within 48h / exceeded)

**Alternative Flows:**
- 4a. Already resolved: Stop alerts
- 6a. Partial resolution: Document what was done, create follow-up
- 8a. Exceeded 48 hours: Record reason, still document

**Postconditions:**
- Emergency documented
- Timeline recorded
- Compliance status known

**Business Value:**
- Avoid liability
- Document compliance
- Accountability
- Evidence for disputes

**Acceptance Criteria:**
- [ ] Automatic countdown timer
- [ ] Escalating alerts (12h, 24h, 40h, 48h)
- [ ] Clear compliance status (green/red)
- [ ] Complete documentation
- [ ] Can record reason for delay
- [ ] Report of all emergencies and compliance rate

---

#### UC-024: Manage Service Providers
**Actor:** Building Manager
**Goal:** Maintain directory of contractors and service providers
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- Manager has logged into the app
- Manager has permission to manage service providers

**Main Success Scenario:**
1. Manager navigates to "Service Providers" section
2. Sees list of existing providers
3. Can add new provider:
   - Company name
   - Contact person
   - Services offered (multi-select: plumbing, electrical, cleaning, etc.)
   - Phone number(s)
   - Email
   - Address
   - Insurance on file (yes/no, expiry date)
   - Notes
4. Can search providers by:
   - Name
   - Service type
   - Rating
5. Can view provider history:
   - All work orders assigned to this provider
   - Total amount paid
   - Average rating
   - Notes from past jobs
6. Can rate provider after job:
   - 1-5 stars
   - Comments
   - Would use again (yes/no)
7. Can mark provider as:
   - Preferred
   - Avoid
   - Inactive

**Alternative Flows:**
- 3a. Import from another building: For multi-building managers
- 6a. No completed jobs: No rating available

**Postconditions:**
- Provider added/updated
- History preserved
- Ratings recorded

**Business Value:**
- Know who to call quickly
- Quality tracking over time
- Price comparison data
- Avoid bad providers

**Acceptance Criteria:**
- [ ] Quick add in under 1 minute
- [ ] Search by service type
- [ ] Rating system (1-5 stars)
- [ ] Work history per provider
- [ ] Preferred/avoid flags
- [ ] Insurance tracking
- [ ] Can share across buildings (for professionals)

---

#### UC-025: Record Expense
**Actor:** Building Manager
**Goal:** Log a building expense for financial tracking
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- Manager has logged into the app
- Manager has permission to manage finances
- Expense has occurred

**Main Success Scenario:**
1. Manager navigates to "Finances" section
2. Taps "Record Expense"
3. Enters expense details:
   - Date (default today, editable)
   - Amount
   - Category (utilities, repairs, cleaning, services, supplies, etc.)
   - Description
   - Service provider (optional, link to UC-024)
   - Payment method (cash, bank transfer, etc.)
4. Uploads receipt/invoice (photo or PDF)
5. Links to work order (if applicable)
6. Saves expense
7. Expense appears in financial reports
8. Balance updated

**Alternative Flows:**
- 3a. New category: Quick-add new category
- 4a. No receipt: Can save without, flag for later

**Postconditions:**
- Expense recorded
- Financial balance updated
- Receipt stored

**Business Value:**
- Accurate financial tracking
- Receipt documentation
- Expense categorization
- Transparency for owners

**Acceptance Criteria:**
- [ ] Quick entry in under 1 minute
- [ ] Photo receipt capture
- [ ] Category management
- [ ] Link to work orders
- [ ] Link to service providers
- [ ] Export for accounting

---

#### UC-026: Manage Building Documents
**Actor:** Building Manager, Unit Owner
**Goal:** Store and access important building documents
**Priority:** P2 - Medium (Phase 3)

**Preconditions:**
- User has logged into the app
- User has permission to view documents

**Main Success Scenario (Manager):**
1. Manager navigates to "Documents" section
2. Sees folder structure:
   - Registration documents
   - Assembly minutes
   - Financial reports
   - Contracts
   - Permits
   - Insurance
   - Other
3. Can upload document:
   - Select file (PDF, image)
   - Select category/folder
   - Add title and description
   - Set access level (manager only / all owners / public)
4. Can search documents
5. Can share document link

**Main Success Scenario (Owner):**
1. Owner navigates to "Documents" section
2. Sees documents shared with owners:
   - Assembly minutes
   - Financial reports
   - Building rules
   - Public notices
3. Can download documents
4. Cannot see manager-only documents

**Alternative Flows:**
- 3a. Large file: Compress or reject with size limit warning

**Postconditions:**
- Document stored
- Accessible per permissions
- Searchable

**Business Value:**
- Central document storage
- Easy access for all
- No more lost papers
- Owner transparency

**Acceptance Criteria:**
- [ ] Folder organization
- [ ] Upload from camera or files
- [ ] Access control (manager/owner)
- [ ] Search by title
- [ ] Download functionality
- [ ] Storage quota management

---

### P3 - Nice to Have (Phase 4+)

#### UC-027: Reserve Common Areas
**Actor:** Resident
**Goal:** Book shared facilities (common room, laundry, etc.)
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- Resident has logged into the app
- Building has bookable common areas
- Resident has permission to book (may require approval)

**Main Success Scenario:**
1. Resident navigates to "Common Areas" section
2. Sees list of bookable facilities:
   - Common room
   - Laundry room
   - Terrace
   - BBQ area
3. Taps facility to see calendar
4. Selects date and time slot
5. Enters booking details:
   - Purpose
   - Expected number of people
   - Contact phone
   - Special requests
6. Submits booking request
7. Manager receives notification
8. Manager approves or declines:
   - If approved: Calendar updated, resident notified
   - If declined: Reason recorded, resident notified
9. Reminders sent before booking

**Alternative Flows:**
- 5a. Slot already booked: Show unavailable
- 8a. Auto-approve: Skip manager approval for certain facilities

**Postconditions:**
- Booking recorded
- Calendar updated
- Notifications sent

**Business Value:**
- Fair access to shared resources
- No double-bookings
- Clear schedule visibility

---

#### UC-028: Online Payment
**Actor:** Unit Owner
**Goal:** Pay maintenance fee digitally through the app
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- Owner has logged into the app
- Owner has amount due
- Payment integration is configured

**Main Success Scenario:**
1. Owner navigates to "Payments" section
2. Sees current amount due
3. Taps "Pay Now"
4. Selects payment method:
   - Credit/debit card
   - Bank transfer (QR code)
   - Digital wallet
5. Confirms payment amount
6. Completes payment (redirect to payment provider)
7. Payment confirmed
8. System records payment automatically
9. Receipt generated
10. Manager sees payment in real-time

**Alternative Flows:**
- 4a. Saved card: One-tap payment
- 6a. Payment failed: Error message, retry option
- 8a. Multiple units: Pay all at once

**Postconditions:**
- Payment recorded
- Balance updated
- Receipt available

**Business Value:**
- Convenience for owners
- Faster payment collection
- Automatic recording
- Reduced manual work

**Acceptance Criteria:**
- [ ] Multiple payment methods
- [ ] Real-time confirmation
- [ ] Automatic recording
- [ ] Receipt generation
- [ ] Payment history
- [ ] Serbian payment providers (QR code)

---

#### UC-029: Digital Voting
**Actor:** Unit Owner
**Goal:** Vote on assembly items remotely and securely
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- Owner has logged into the app
- Owner is verified (identity confirmed)
- Active assembly with open voting items
- Voting period is open

**Main Success Scenario:**
1. Owner receives notification: "New vote available"
2. Opens "Assembly" section
3. Sees active voting items with:
   - Item description
   - Voting deadline
   - Required majority
   - Supporting documents
4. Taps item to vote
5. Reviews full details and documents
6. Casts vote:
   - For
   - Against
   - Abstain
7. Confirms vote (may require re-authentication)
8. Vote recorded securely
9. Owner sees confirmation
10. Can change vote until deadline
11. Vote counted in assembly results

**Alternative Flows:**
- 7a. Vote already cast: Show current vote, option to change
- 10a. Deadline passed: Show results only

**Postconditions:**
- Vote recorded
- Can be changed until deadline
- Counted in results

**Business Value:**
- Increase participation
- Convenience for busy owners
- Accommodate remote owners
- Secure and verifiable

**Acceptance Criteria:**
- [ ] Secure vote recording
- [ ] Vote change until deadline
- [ ] Identity verification
- [ ] Real-time count update (for manager)
- [ ] Audit trail
- [ ] Anonymous results display

---

#### UC-030: Multi-Building Dashboard
**Actor:** Professional Manager, Property Company
**Goal:** Overview of all managed buildings at once
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- User has logged into the app
- User manages multiple buildings
- User has permission for portfolio view

**Main Success Scenario:**
1. User sees portfolio dashboard with:
   - All buildings listed
   - Key metrics per building (units, residents, collection rate)
   - Alerts across portfolio
   - Consolidated financial summary
2. Can filter/sort buildings by:
   - Name
   - Status (compliant, at risk)
   - Collection rate
   - Open work orders
3. Taps building to drill into details
4. Sees cross-portfolio alerts:
   - Upcoming deadlines
   - License/insurance expirations
   - Emergency work orders
   - Overdue payments
5. Can perform bulk actions:
   - Send announcement to all buildings
   - Export portfolio report
   - Filter by deadline type

**Alternative Flows:**
- 3a. Single building: Show building dashboard instead

**Postconditions:**
- Portfolio overview viewed
- Can drill into any building

**Business Value:**
- Scale operations efficiently
- Never miss cross-building issues
- Portfolio-level reporting
- Efficient management

**Acceptance Criteria:**
- [ ] All buildings visible at a glance
- [ ] Key metrics per building
- [ ] Cross-portfolio alerts
- [ ] Consolidated financial view
- [ ] Bulk operations
- [ ] Export portfolio report

---

#### UC-031: Delegate to Substitute Manager
**Actor:** Professional Manager
**Goal:** Temporarily delegate building management
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- Professional manager has logged into the app
- Substitute manager exists in system
- Power of attorney document prepared

**Main Success Scenario:**
1. Manager navigates to "Delegation" section
2. Selects "New Delegation"
3. Selects buildings to delegate (one or more)
4. Selects substitute manager from list
5. Defines delegation period:
   - Start date
   - End date
   - Or indefinite (until revoked)
6. Defines scope of authority:
   - Emergency decisions only
   - All decisions
   - Custom (specify)
7. Uploads power of attorney document
8. Submits delegation
9. Substitute manager receives notification and access
10. During delegation:
    - Substitute sees assigned buildings
    - Actions logged under substitute's name
    - Original manager has view access
11. Delegation ends (date or revocation)
12. Access revoked, handover documented

**Alternative Flows:**
- 9a. Substitute declines: Delegation cancelled
- 11a. Early revocation: Document reason

**Postconditions:**
- Delegation active
- Substitute has appropriate access
- All actions logged

**Business Value:**
- Coverage when unavailable
- Clear authority boundaries
- Audit trail
- Legal compliance

**Acceptance Criteria:**
- [ ] Date-based delegation
- [ ] Scope of authority options
- [ ] Document upload (power of attorney)
- [ ] Access management
- [ ] Action logging
- [ ] Revocation workflow
- [ ] Handover documentation

---

#### UC-032: Generate Financial Report
**Actor:** Building Manager
**Goal:** Create formatted financial report for owners or audit
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- Manager has logged into the app
- Financial data exists (income and expenses)
- Manager has permission to manage finances

**Main Success Scenario:**
1. Manager navigates to "Reports" section
2. Selects "Financial Report"
3. Selects report parameters:
   - Period (month, quarter, year, custom)
   - Include categories (all or selected)
   - Grouping (by month, by category)
   - Format (summary or detailed)
4. System generates report preview
5. Manager reviews and adjusts if needed
6. Generates final PDF:
   - Building header
   - Period summary
   - Income breakdown
   - Expense breakdown
   - Balance
   - Charts/graphs
7. Downloads or shares report
8. Report stored in documents

**Alternative Flows:**
- 3a. Export to Excel: Generate spreadsheet instead

**Postconditions:**
- Report generated
- Available for distribution
- Stored for records

**Business Value:**
- Professional reporting
- Transparency for owners
- Audit documentation
- Easy period comparison

**Acceptance Criteria:**
- [ ] Multiple period options
- [ ] Professional PDF layout
- [ ] Visual charts
- [ ] Excel export option
- [ ] Stored in document archive
- [ ] Can regenerate

---

#### UC-033: Manage Notification Preferences
**Actor:** All Users
**Goal:** Control which notifications are received and how
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- User has logged into the app
- User has notification settings

**Main Success Scenario:**
1. User navigates to "Settings" > "Notifications"
2. Sees notification categories:
   - Work order updates
   - Announcements
   - Assembly meetings
   - Payment reminders
   - Emergency alerts
3. For each category, selects delivery method:
   - Push notification (on/off)
   - Email (on/off)
   - SMS (on/off, shows cost if applicable)
4. Sets quiet hours:
   - Start time
   - End time
   - Exceptions (emergencies always override)
5. Saves preferences
6. System applies preferences to future notifications

**Alternative Flows:**
- 3a. SMS not available: Gray out option

**Postconditions:**
- Preferences saved
- Future notifications respect settings

**Business Value:**
- User control over notifications
- Reduce notification fatigue
- Respect user preferences
- Cost management (SMS)

---

#### UC-034: Offline Data Sync
**Actor:** All Users
**Goal:** Work offline and sync when connected
**Priority:** P3 - Nice to Have (Phase 4+)

**Preconditions:**
- User has logged into the app at least once
- User is about to go offline or is offline

**Main Success Scenario:**
1. User works in app normally
2. When offline:
   - Banner shows "Offline mode"
   - Cached data available (residents, keys, work orders)
   - Can create new items locally
3. User creates/modifies data while offline
4. Items marked as "pending sync"
5. When connection restored:
   - Automatic sync initiated
   - Pending items uploaded
   - Conflicts resolved (server wins or user chooses)
6. Sync complete notification
7. All data current

**Alternative Flows:**
- 5a. Conflict: Notify user, offer resolution options

**Postconditions:**
- All offline changes synced
- Data current

**Business Value:**
- Work without internet
- Never lose data
- Seamless experience
- Reliable for field work

**Acceptance Criteria:**
- [ ] Clear offline indicator
- [ ] Core features work offline
- [ ] Automatic sync on reconnect
- [ ] Conflict resolution
- [ ] Sync status visible

---

## Feature Summary by Role

### Regular Building Manager (Upravnik)

| Feature | Description | Priority |
|---------|-------------|----------|
| Resident Directory | Search and view all residents | P0 |
| Key Management | Track who has which keys | P0 |
| Work Orders | Create, assign, track maintenance | P0 |
| Announcements | Send notifications to residents | P0 |
| Assembly Management | Schedule, document meetings | P0 |
| Fee Tracking | View payment status | P0 |
| Community Registration | Register with local government | P1 |
| Deadline Tracking | Never miss legal deadlines | P1 |
| Emergency Interventions | 48-hour deadline tracking | P1 |
| Service Providers | Contractor directory | P1 |

### Professional Manager (Profesionalni Upravnik)

All Regular Manager features, PLUS:

| Feature | Description | Priority |
|---------|-------------|----------|
| 24/7 Problem Reports | Log all reports received | P1 |
| License Management | Track license & insurance | P1 |
| 3-Bid Fee Proposals | Document fee basis | P2 |
| Semi-Annual Reports | Generate required reports | P2 |
| Forced Administration | Handle assigned buildings | P1 |
| Multi-Building View | Manage multiple buildings | P3 |
| Substitute Manager | Power of attorney | P2 |

### Unit Owner (Vlasnik)

| Feature | Description | Priority |
|---------|-------------|----------|
| Submit Request | Report maintenance issues | P0 |
| View Announcements | See building news | P0 |
| View Meetings | See assembly info | P0 |
| Vote | Participate in decisions | P1 |
| Fee Status | View own payments | P0 |
| Documents | Access building documents | P1 |
| Profile | Update own information | P0 |

### Tenant (Stanar)

| Feature | Description | Priority |
|---------|-------------|----------|
| Submit Request | Report issues in unit | P0 |
| View Announcements | See building news | P0 |
| Emergency Contacts | Find who to call | P0 |
| Building Rules | View house rules | P1 |
| Common Area Booking | Reserve facilities | P3 |

---

## Critical Legal Requirements (Serbian Law)

### Registration & Deadlines

| Requirement | Deadline | Consequence | System Feature |
|-------------|----------|-------------|----------------|
| First assembly | 60 days from legal entity status | Possible forced admin | Deadline tracking |
| Register community | 15 days from first assembly | Non-compliance | Registration workflow |
| Update registration | 15 days from change | Non-compliance | Change notification |
| Tenant data | 30 days from lease | Non-compliance | Tenant registration |

### Assembly & Voting

| Requirement | Rule | System Feature |
|-------------|------|----------------|
| Notice period | Minimum 3 days | Notice deadline |
| Quorum (first) | 51% of votes | Auto calculation |
| Quorum (repeated) | 1/3 of votes | Auto calculation |
| Unavailable owner | 3 missed sessions | Tracking & flagging |
| Unanimous vote | All must agree | Validation |
| 2/3 majority | Specific decisions | Validation |
| Simple majority | Most decisions | Validation |

### Professional Manager

| Requirement | Rule | System Feature |
|-------------|------|----------------|
| License | Valid from Chamber | Expiration tracking |
| Insurance | 10,000 EUR minimum | Amount validation |
| Insurance validity | 3 years minimum | Duration validation |
| Annual proof | Submit yearly | Reminder |
| 24/7 reports | All day, every day | Report logging |
| Semi-annual report | Twice per year | Report generation |
| Fee basis | 3 bids required | Bid documentation |

### Maintenance

| Requirement | Rule | System Feature |
|-------------|------|----------------|
| Emergency intervention | 48 hours | Countdown timer |
| Maintenance program | By end of February | Deadline tracking |
| Priority maintenance | Specific items | Priority list |
| Report problems | Manager must receive | 24/7 logging |

---

## Implementation Roadmap

### Phase 1: MVP - Core Management (Months 1-3)
**Goal:** Essential functionality for single building management

**Target User:** Regular Building Manager (Milan)

**Sprint 1 (Weeks 1-4): Foundation**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Project setup | Tech stack, CI/CD, development environment | - |
| Authentication | Login, registration, password reset | All |
| Building setup | Create building, add units, define structure | UC-009 |
| Basic UI shell | Navigation, screens, responsive layout | All |
| User roles | Manager, owner, tenant roles and permissions | All |

**Sprint 2 (Weeks 5-8): Resident & Key Management**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Resident directory | Add, view, search, edit residents | UC-001, UC-009 |
| Owner management | Track ownership shares, owner info | UC-009 |
| Tenant management | Track tenants, link to owners | UC-009 |
| Key inventory | List all keys by type and unit | UC-002, UC-010 |
| Key checkout | Record key handout with recipient info | UC-002 |
| Key checkin | Record key return, track overdue | UC-010 |
| Offline mode | Cache residents and keys for offline access | UC-001, UC-002 |

**Sprint 3 (Weeks 9-12): Maintenance & Communication**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Work order creation | Resident submits request with photo | UC-003 |
| Work order list | Manager views and filters work orders | UC-004 |
| Work order updates | Status changes, notes, assignments | UC-004 |
| Work order history | Per-unit and per-building history | UC-004 |
| Announcements | Create and send announcements | UC-005 |
| Notification delivery | Push notifications to residents | UC-005 |
| Announcement history | View past announcements | UC-005 |

**Sprint 4 (Weeks 13-16): Assembly & Finance Basics**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Assembly scheduling | Create assembly with date, time, location | UC-007 |
| Assembly notice | Send notice to owners, track delivery | UC-007 |
| Attendance tracking | Record who attended (in person, proxy) | UC-006 |
| Basic voting | Record votes, show results | UC-006 |
| Meeting minutes | Generate minutes document | UC-006 |
| Fee structure | Define monthly fees per unit | UC-008 |
| Payment tracking | Record payments, show status | UC-008 |
| Payment reminders | Send reminder to unpaid units | UC-008 |

**Phase 1 Deliverables:**
- Mobile app for managers (iOS/Android)
- Mobile app for residents (view-only for owners/tenants)
- Web admin panel (basic)
- 10 use cases fully implemented
- Offline capability for core features
- Serbian language interface

**Phase 1 Success Criteria:**
| Metric | Target |
|--------|--------|
| Find any resident | < 5 seconds |
| Log key handout | < 30 seconds |
| Create work order | < 1 minute |
| Send announcement | < 1 minute |
| Record assembly | < 30 minutes |
| Track payments | Real-time status |

---

### Phase 2: Legal Compliance (Months 4-6)
**Goal:** Full Serbian law compliance for professional managers

**Target User:** Professional Manager (Dragan), Company Admin (Jelena)

**Sprint 5 (Weeks 17-20): Community & Deadlines**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Community registration | Register as legal entity workflow | UC-011 |
| MB/PIB tracking | Record registration numbers | UC-011 |
| Deadline system | Track all legal deadlines | UC-012 |
| Deadline alerts | Reminders at 7/3/1 days | UC-012 |
| Deadline dashboard | Visual countdown for all deadlines | UC-012 |
| Document storage | Upload and organize building documents | UC-026 |

**Sprint 6 (Weeks 21-24): Professional Manager Features**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Professional status | Track license and insurance | UC-014 |
| License expiration | Alerts at 90/60/30 days | UC-014 |
| Insurance validation | Check 10k EUR minimum, 3-year duration | UC-014 |
| 24/7 reports | Log problem reports received any time | UC-013 |
| Report history | View all 24/7 reports | UC-013 |
| Multi-building support | Assign manager to multiple buildings | UC-012 |

**Sprint 7 (Weeks 25-28): Enhanced Assembly & Voting**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Quorum calculation | Auto-calculate based on shares | UC-015 |
| Unanimous voting | Enforce 100% requirement | UC-015 |
| 2/3 majority | Enforce two-thirds requirement | UC-015 |
| Simple majority | Enforce standard majority | UC-015 |
| Unavailable owners | Track and exclude from quorum | UC-016 |
| Repeated session | Lower quorum (1/3) for second session | UC-006 |

**Sprint 8 (Weeks 29-32): Emergency & Finance**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Emergency flag | Mark work orders as emergency | UC-023 |
| 48-hour countdown | Timer for emergency resolution | UC-023 |
| Emergency alerts | Escalating reminders (12/24/40/48h) | UC-023 |
| Expense tracking | Record building expenses | UC-025 |
| Financial summary | Dashboard of income/expenses | UC-019 |
| Fee structure options | Per m², fixed, or mixed | UC-018 |

**Phase 2 Deliverables:**
- Legal deadline tracking system
- Professional manager compliance dashboard
- Enhanced voting with validation
- Emergency intervention tracking
- Financial management basics
- Company admin dashboard

**Phase 2 Success Criteria:**
| Metric | Target |
|--------|--------|
| Deadlines tracked | 100% of legal deadlines |
| License/insurance alerts | 90/60/30 day warnings |
| Voting validation | All thresholds enforced |
| Emergency compliance | 95% within 48 hours |
| Financial visibility | Real-time balance |

---

### Phase 3: Enhanced Features (Months 7-9)
**Goal:** Improve user experience and add value-added features

**Target User:** All users

**Sprint 9 (Weeks 33-36): Professional Reporting**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Fee proposals | Create proposal with 3 bids | UC-020 |
| Bid comparison | Side-by-side view of bids | UC-020 |
| Semi-annual report | Generate required report | UC-021 |
| Report templates | Pre-formatted professional layouts | UC-021 |
| Financial reports | Detailed income/expense reports | UC-032 |

**Sprint 10 (Weeks 37-40): Service Providers & Rules**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Provider directory | Add and manage service providers | UC-024 |
| Provider rating | Rate providers after work | UC-024 |
| Provider history | View work done by provider | UC-024 |
| Owners' rules | Manage Pravilnik adoption | UC-022 |
| Unanimous voting UI | Special flow for rules approval | UC-022 |

**Sprint 11 (Weeks 41-44): Communication & Engagement**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Notification preferences | User control over notifications | UC-033 |
| Quiet hours | Respect user quiet times | UC-033 |
| Emergency override | Always alert for emergencies | UC-033 |
| In-app messaging | Direct message to manager | (New) |
| Message history | View past conversations | (New) |

**Sprint 12 (Weeks 45-48): User Experience Polish**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Dashboard improvements | Role-specific dashboards | All |
| Performance optimization | Faster load times | All |
| Offline improvements | More features work offline | UC-034 |
| Sync conflict resolution | Handle offline conflicts | UC-034 |
| UI/UX refinements | Based on user feedback | All |
| English language | Full English translation | All |

**Phase 3 Deliverables:**
- 3-bid fee proposal system
- Semi-annual report generation
- Service provider management
- Owners' rules workflow
- Enhanced notifications
- Offline improvements
- Multi-language support

**Phase 3 Success Criteria:**
| Metric | Target |
|--------|--------|
| Report generation | < 10 minutes (vs. hours manually) |
| Provider lookup | < 10 seconds |
| User satisfaction | 4.0+ app store rating |
| Feature adoption | 70% of users using 3+ features |

---

### Phase 4: Scale & Platform (Months 10-12)
**Goal:** Support professional managers and property companies at scale

**Target User:** Professional Manager (Dragan), Company Admin (Jelena)

**Sprint 13 (Weeks 49-52): Multi-Building Management**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Portfolio dashboard | All buildings at a glance | UC-030 |
| Cross-building alerts | Deadlines across portfolio | UC-030 |
| Bulk operations | Actions across multiple buildings | UC-030 |
| Portfolio reports | Consolidated reporting | UC-030 |
| Building comparison | Side-by-side metrics | UC-030 |

**Sprint 14 (Weeks 53-56): Forced Administration & Delegation**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Forced admin workflow | Assign and manage cases | UC-017 |
| Government reporting | Reports for local government | UC-017 |
| Substitute delegation | Temporary handover | UC-031 |
| Power of attorney | Document delegation | UC-031 |
| Delegation tracking | Monitor substitute actions | UC-031 |

**Sprint 15 (Weeks 57-60): Digital Services**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Online payment | Card/bank payment integration | UC-028 |
| QR code payment | Serbian bank QR codes | UC-028 |
| Auto payment recording | Payments logged automatically | UC-028 |
| Digital voting | Remote assembly voting | UC-029 |
| Vote verification | Secure vote recording | UC-029 |

**Sprint 16 (Weeks 61-64): Platform & Integration**
| Deliverable | Description | Use Cases |
|-------------|-------------|-----------|
| Common area booking | Reserve shared facilities | UC-027 |
| Booking calendar | Visual availability | UC-027 |
| API documentation | Public API for integrations | (New) |
| Webhooks | Event notifications for integrations | (New) |
| Bulk import | Migrate from other systems | (New) |
| Advanced analytics | Usage and performance metrics | (New) |

**Phase 4 Deliverables:**
- Multi-building portfolio management
- Forced administration handling
- Substitute manager delegation
- Online payment integration
- Digital voting system
- Common area booking
- API platform
- Bulk data import

**Phase 4 Success Criteria:**
| Metric | Target |
|--------|--------|
| Buildings per manager | 10+ buildings manageable |
| Payment digital rate | 50% of payments online |
| Voting participation | 60% digital voting |
| Platform adoption | 5+ external integrations |

---

### Post-Launch Roadmap (Year 2+)

**Potential Future Features:**
- Energy consumption tracking
- IoT device integration (smart meters)
- Predictive maintenance (ML-based)
- Owner/resident marketplace
- Building comparison benchmarks
- Advanced financial analytics
- Public website generation
- Multi-country support

---

## Success Metrics

### Efficiency Metrics
| Metric | Baseline | Target |
|--------|----------|--------|
| Time to find resident | 5 minutes (paper) | 5 seconds |
| Time to log key handout | 5 minutes (paper) | 30 seconds |
| Time to create work order | 10 minutes | 1 minute |
| Time to send announcement | 1 hour | 1 minute |
| Time to document assembly | 2 hours | 30 minutes |

### Compliance Metrics
| Metric | Target |
|--------|--------|
| Deadlines missed | 0 |
| Incorrect voting | 0 |
| Emergency interventions overdue | 0 |
| Insurance lapses | 0 |

### Engagement Metrics
| Metric | Target |
|--------|--------|
| Residents using app | 70% |
| Assembly attendance | 60% |
| Digital fee payment | 50% |
| Announcement read rate | 80% |

---

## Technical Constraints

### Must Support
- Mobile-first design (iOS and Android)
- Offline functionality for key features
- Low bandwidth operation
- Simple, intuitive interface
- Serbian and English languages
- RSD currency formatting
- Serbian date/time formats

### Must NOT Require
- Constant internet connection
- Technical expertise
- Desktop computer
- Paper backups (system is the record)

### Security Requirements
- Secure authentication
- Role-based access
- Audit logging
- Data encryption
- Privacy compliance

---

## Out of Scope (Future Phases)

- IoT/smart building integration
- Energy monitoring
- Advanced financial reporting
- Public website
- Owner marketplace
- Social features
- Multi-country support
- Desktop application

---

## Questions for Stakeholders

1. **Payment Integration:** Should we integrate with specific Serbian banks/payment providers?

2. **Local Government Integration:** Should we attempt to integrate with local government registers?

3. **Offline Priority:** Which features absolutely must work offline?

4. **Notification Preferences:** SMS is expensive - is email/app notification sufficient?

5. **Professional Manager Focus:** Should MVP focus on regular managers or include professional manager features?

6. **Forced Administration:** Is this a common enough scenario to include in MVP?

7. **Document Storage:** What volume of documents should we plan for?

8. **Historical Data:** Should we support importing data from existing systems?

---

## Appendix A: User Stories by Epic

### Epic: Building Setup & Management
- As a manager, I want to create a building with units so I can start managing it
- As a manager, I want to edit unit information so records are accurate
- As a manager, I want to see building overview with key metrics so I know the status at a glance
- As a manager, I want to upload building photos so I have visual documentation
- As a manager, I want to record building details (year built, area, floors) so I have complete information

### Epic: Resident Management
- As a manager, I want to see all residents in one place so I can quickly find contact information
- As a manager, I want to add new residents so the directory stays current
- As a manager, I want to edit resident information so records are accurate
- As a manager, I want to see who is an owner vs tenant so I know who can vote
- As a manager, I want to mark residents as inactive when they move out so history is preserved
- As a resident, I want to update my own information so records stay accurate without manager involvement
- As a manager, I want to see who has emergency contacts so I can reach family in crisis
- As a manager, I want to see ownership shares so I can calculate voting weights
- As a manager, I want to record tenant information within 30 days of lease so I comply with the law

### Epic: Key Management
- As a manager, I want to see all keys so I know what exists in the building
- As a manager, I want to see which keys are currently checked out so I know who has access
- As a manager, I want to record who takes a key so I can get it back
- As a manager, I want to see overdue keys so I can follow up
- As a manager, I want to record key returns so I know keys are back in the building
- As a manager, I want to see key history so I have a complete audit trail
- As a manager, I want to generate a key receipt so I can give proof to the recipient
- As a manager, I want to mark a key as lost so I can track replacement needs

### Epic: Maintenance & Work Orders
- As a resident, I want to report a problem so it gets fixed
- As a resident, I want to attach a photo so the manager understands the issue
- As a resident, I want to see the status of my request so I know progress
- As a resident, I want to receive a confirmation number so I can reference my request
- As a manager, I want to see all requests so nothing is forgotten
- As a manager, I want to sort requests by priority so emergencies get attention first
- As a manager, I want to update request status so residents know progress
- As a manager, I want to assign requests to service providers so work gets done
- As a manager, I want to see request history so I can spot patterns
- As a manager, I want to mark emergencies so they get priority and 48-hour tracking
- As a manager, I want to close a request with documentation so I have proof of completion
- As a professional manager, I want to log 24/7 reports so I meet my legal obligation

### Epic: Communication
- As a manager, I want to send announcements so everyone is informed
- As a manager, I want to schedule announcements so they go at the right time
- As a manager, I want to target specific groups so only relevant people receive messages
- As a manager, I want to see who read announcements so I know who to follow up with
- As a resident, I want to receive notifications so I don't miss anything important
- As a resident, I want to control which notifications I receive so I'm not overwhelmed
- As a resident, I want to message the manager so I can ask questions privately
- As a manager, I want to send emergency alerts that override quiet hours so everyone is safe

### Epic: Assembly & Voting
- As a manager, I want to schedule meetings so owners can plan to attend
- As a manager, I want to send meeting notices so I meet the 3-day legal requirement
- As a manager, I want to attach the agenda so owners know what will be discussed
- As a manager, I want to record attendance so I have proof of quorum
- As a manager, I want to see automatic quorum calculation so I know if the meeting is valid
- As a manager, I want to record votes so decisions are documented
- As a manager, I want the system to validate voting thresholds so decisions are legally correct
- As a manager, I want to generate minutes so I have official documentation
- As an owner, I want to see meeting details so I can participate
- As an owner, I want to vote electronically so I can participate remotely
- As an owner, I want to change my vote before the deadline so I can reconsider
- As a manager, I want to track unavailable owners so quorum calculations are correct
- As a manager, I want to schedule a repeated session when quorum isn't met so I can still hold the meeting

### Epic: Financial Management
- As a manager, I want to define fee structure so amounts are calculated automatically
- As a manager, I want to see who has paid so I can follow up with those who haven't
- As a manager, I want to record payments so accounts are current
- As a manager, I want to record expenses so I can track where money goes
- As a manager, I want to see a financial summary so I understand the building's financial health
- As a manager, I want to send payment reminders so collection improves
- As a manager, I want to generate statements so owners have records
- As an owner, I want to see my balance so I know what I owe
- As an owner, I want to see payment history so I have a record
- As an owner, I want to see the building's financial summary so I understand where fees go
- As a manager, I want to track special assessments so one-time costs are recovered

### Epic: Legal Compliance
- As a manager, I want to see upcoming deadlines so I don't miss any legal requirements
- As a manager, I want to receive deadline reminders so I have time to act
- As a manager, I want to mark deadlines complete so I have proof of compliance
- As a manager, I want to register the community so we are a legal entity
- As a manager, I want to track the 15-day registration deadline so I don't miss it
- As a professional manager, I want to track my license so it doesn't expire
- As a professional manager, I want to track my insurance so I stay compliant with the 10,000 EUR requirement
- As a professional manager, I want to generate semi-annual reports so I meet my reporting obligation
- As a manager, I want to track emergency interventions within 48 hours so I avoid liability
- As a professional manager, I want to document 3 bids for fee proposals so I meet legal requirements
- As a company admin, I want to see all managers' compliance status so I can manage risk

### Epic: Service Provider Management
- As a manager, I want to add service providers so I know who to call
- As a manager, I want to search providers by service type so I find the right person quickly
- As a manager, I want to see provider history so I know their track record
- As a manager, I want to rate providers after jobs so I can track quality
- As a manager, I want to mark providers as preferred or avoid so I remember for next time
- As a professional manager, I want to share providers across buildings so I don't re-enter data

### Epic: Document Management
- As a manager, I want to upload and organize documents so they don't get lost
- As a manager, I want to share documents with owners so they have access
- As an owner, I want to download building documents so I have my own copies
- As a manager, I want to find documents by search so I don't have to browse folders
- As a manager, I want to control who can see each document so sensitive info is protected

### Epic: Professional Manager Operations
- As a professional manager, I want to see all my buildings on one dashboard so I can manage them efficiently
- As a professional manager, I want to see cross-building alerts so nothing falls through the cracks
- As a professional manager, I want to delegate to a substitute manager so I have coverage when away
- As a professional manager, I want to document power of attorney so delegation is legal
- As a company admin, I want to assign forced administration cases so they are handled correctly
- As a professional manager, I want to generate government reports so I meet reporting requirements

### Epic: Owner/Resident Self-Service
- As an owner, I want to pay fees online so I don't have to visit the bank
- As an owner, I want to see my payment history so I have records for tax purposes
- As a resident, I want to reserve common areas so I can use building facilities
- As a resident, I want to see building rules so I know what's allowed
- As a resident, I want to find emergency contacts so I know who to call

---

## Appendix B: Data Entities Summary

### Core Entities

**Building**
- Address, units count, total area, year built
- Registration status (MB, PIB, bank account)
- Fee structure

**Unit**
- Unit number, floor, area (m²)
- Owner(s) with shares
- Current residents

**Person**
- Name, phone, email, emergency contact
- Role (owner, tenant, family member)
- Unit association
- User account (optional)

**Key**
- Key type (unit, entrance, storage)
- Unit association
- Current status (available, checked out)
- Checkout history

**Work Order**
- Problem description, photos
- Priority, status, category
- Reporter, assigned provider
- Timestamps, resolution

**Assembly**
- Date, time, location
- Agenda items
- Attendance (in person, proxy, electronic)
- Decisions with vote counts

**Financial Transaction**
- Type (income/expense)
- Amount, date, category
- Related unit or provider
- Document reference

### Compliance Entities

**Deadline**
- Type, due date, status
- Related building
- Reminder schedule

**Professional Manager**
- License number, expiration
- Insurance details
- Assigned buildings

---

## Appendix C: Question Log

### Resolved Questions
1. ✅ **Payment Integration:** Will integrate with Serbian payment providers (QR codes for bank transfer, card payments)
2. ✅ **Offline Priority:** Core features must work offline (residents, keys, work order creation)
3. ✅ **Professional Manager Focus:** MVP focuses on regular managers, Phase 2 adds professional features
4. ✅ **Notification Preferences:** SMS is optional and costs apply; in-app and email are free

### Open Questions
1. **Local Government Integration:** Should we attempt to integrate with local government registers? (Low priority, may not be feasible)
2. **Forced Administration in MVP:** Is this common enough to include in MVP? (Currently Phase 4)
3. **Document Storage Volume:** What volume of documents should we plan for? (Need quota management)
4. **Historical Data Import:** Should we support importing data from existing systems? (Phase 4, API-based)
5. **Multi-country Support:** Is this needed or is Serbia-only sufficient? (Currently out of scope)

---

## Appendix D: Non-Functional Requirements

### Performance
| Requirement | Target |
|-------------|--------|
| App launch time | < 3 seconds |
| Screen load time | < 2 seconds |
| Search response | < 1 second |
| Photo upload | < 10 seconds |
| Offline data load | < 1 second |

### Availability
| Requirement | Target |
|-------------|--------|
| Uptime | 99.5% |
| Maintenance window | Sundays 2-4 AM |
| Graceful degradation | Core features work when backend down |

### Security
| Requirement | Description |
|-------------|-------------|
| Authentication | JWT tokens, secure storage |
| Authorization | Role-based access control |
| Data encryption | TLS in transit, encrypted at rest |
| Audit logging | All sensitive actions logged |
| Privacy | GDPR-compliant data handling |

### Scalability
| Requirement | Target |
|-------------|--------|
| Concurrent users | 10,000+ |
| Buildings | 10,000+ |
| Requests per second | 1,000+ |
| Storage per building | 1 GB |

### Mobile Requirements
| Requirement | Description |
|-------------|-------------|
| Platforms | iOS 14+, Android 10+ |
| Offline storage | 100 MB cached data |
| Network | Works on 3G, optimized for 4G |
| Battery | Minimal background drain |

---

*Document Version: 2.0*
*Last Updated: 2024*
*Author: Project Team*
