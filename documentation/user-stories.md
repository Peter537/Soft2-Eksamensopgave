# User Stories

Dette dokument indeholder user stories for MToGo platformen, opdelt efter service. Hver user story (US) inkluderer størrelse, prioritet, kort beskrivelse og acceptance criterias (AC).

## Service: Customer Service

**Size:** M  
**Priority:** High  
**US-1:** Customer Registration  
**As a** new customer,  
**I want** to register an account with details,  
**So that** I can place orders and track deliveries.  
**AC-1:** Given valid email, password, name, address, and phone, When submitting registration, Then a new account is created.  
**AC-2:** Given duplicate email, When registering, Then 400 error returned.  
**AC-3:** Given successful registration, When logging in, Then profile access granted.  
**AC-4:** Given registration succeeds, When checking database, Then password hashed with BCrypt.

---

**Size:** XS  
**Priority:** High  
**US-2:** Customer Login  
**As a** registered customer,  
**I want** to log in with credentials,  
**So that** I can access my account securely.  
**AC-1:** Given valid credentials, When logging in, Then JWT token issued.  
**AC-2:** Given invalid credentials, When logging in, Then 401 Unauthorized returned.

---

**Size:** S  
**Priority:** Low  
**US-3:** Update Customer Profile  
**As a** customer,  
**I want** to update delivery address and preferences,  
**So that** orders use accurate information.  
**AC-1:** Given authenticated, When updating address or notification method, Then changes saved and 200 OK returned.  
**AC-2:** Given invalid data, When updating, Then 400 error with details.  
**AC-3:** Given update succeeds, When placing new order, Then updated details applied.

---

**Size:** XS  
**Priority:** Low  
**US-4:** Delete Customer Account  
**As a** customer,  
**I want** to delete my account,  
**So that** personal data is removed.  
**AC-1:** Given authenticated, When confirming deletion, Then account marked deleted and logged out.  
**AC-2:** Given account deleted, When attempting login, Then "Account not found" error.

---

## Service: Order Service

**Size:** L  
**Priority:** High  
**US-5:** Place Food Order  
**As a** customer,  
**I want** to place order with items and payment,  
**So that** food is delivered.  
**AC-1:** Given items in cart and valid payment, When submitting, Then order created in Placed status with ID.  
**AC-2:** Given order value, When creating, Then service fee calculated (6% <101 DKK, sliding to 3% >1000 DKK).  
**AC-3:** Given success, When created, Then OrderCreated kafka event published.  
**AC-4:** Given payment fails, When submitting, Then order not created and error notified.  
**AC-5:** Given delivery fee, When created, Then 15% to bonus pool.

---

**Size:** M  
**Priority:** Low  
**US-6:** View Order History  
**As a** customer,  
**I want** to view past orders,  
**So that** I can track spending.  
**AC-1:** Given logged in, When navigating history, Then list with dates, partners, items, totals.  
**AC-2:** Given date range, When filtering, Then only matching orders shown.

---

**Size:** M  
**Priority:** High  
**US-7:** Accept Order  
**As a** partner,  
**I want** to accept placed order,  
**So that** preparation starts.  
**AC-1:** Given Placed status, When accepting, Then status Accepted and OrderAccepted kafka event published.  
**AC-2:** Given not Placed, When accepting, Then 400 error.

---

**Size:** M  
**Priority:** High  
**US-8:** Reject Order  
**As a** partner,  
**I want** to reject placed order,  
**So that** customer is notified of order rejection.  
**AC-1:** Given Placed, When rejecting with or without reason, Then status Rejected and kafka event published.  
**AC-2:** Given not Placed, When rejecting, Then 400 error.  
**AC-3:** Given rejected, When processed, Then refund initiated and customer notified.

---

**Size:** M  
**Priority:** High  
**US-9:** Mark Order Ready  
**As a** partner,  
**I want** to mark order prepared,  
**So that** agent can pick up.  
**AC-1:** Given Accepted, When marking ready, Then status Prepared and OrderReady kafka event published.  
**AC-2:** Given no agent assigned, When marking, Then still Prepared.

---

**Size:** M  
**Priority:** High  
**US-10:** Accept Delivery Offer  
**As an** agent,  
**I want** to accept order assignment,  
**So that** I secure work.  
**AC-1:** Given notification, When accepting, Then order locked and agent is Assigned.  
**AC-2:** Given race condition, When accepting, Then "Delivery unavailable" if taken.  
**AC-3:** Given high concurrency, When handling, Then flow manages without errors.

---

**Size:** M  
**Priority:** High  
**US-11:** Pick Up Order  
**As an** agent,  
**I want** to confirm pickup,  
**So that** customer knows en route.  
**AC-1:** Given Prepared and agent assigned, When confirming, Then status PickedUp.  
**AC-2:** Given not assigned, When confirming, Then 403 Forbidden.  
**AC-3:** Given confirmed, When status changes, Then OrderPickedUp kafka event published.

---

**Size:** M  
**Priority:** High  
**US-12:** Complete Delivery  
**As an** agent,  
**I want** to mark delivered,  
**So that** workflow completes.  
**AC-1:** Given PickedUp and agent assigned, When marking, Then status Delivered.  
**AC-2:** Given not assigned, When marking, Then 403 Forbidden.  
**AC-3:** Given completed, When changed, Then OrderDelivered kafka event published.

---

## Service: Partner Service

**Size:** M  
**Priority:** High  
**US-13:** Partner Registration  
**As a** new partner,  
**I want** to register with menu,  
**So that** customers can order.  
**AC-1:** Given details and menu items, When submitting, Then account created.  
**AC-2:** Given empty menu, When registering, Then 400 error.  
**AC-3:** Given success, When checking, Then password hashed.

---

**Size:** M  
**Priority:** Medium  
**US-14:** Manage Menu Items  
**As a** partner,  
**I want** to add/update/delete menu items,  
**So that** offerings are current.  
**AC-1:** Given authenticated, When adding item with name/price, Then item added with ID.  
**AC-2:** Given existing item, When updating, Then changes saved.  
**AC-3:** Given item exists, When deleting, Then removed.  
**AC-4:** Given invalid price <=0 or name, When saving, Then 400 error.

---

**Size:** S  
**Priority:** Medium  
**US-15:** Toggle Availability  
**As a** partner,  
**I want** to set active/inactive,  
**So that** no orders when unavailable.  
**AC-1:** Given authenticated, When toggling, Then status updated and persisted.

---

**Size:** M  
**Priority:** High  
**US-16:** Browse Partner Menus  
**As a** customer,  
**I want** to view active partner menus,  
**So that** I can select items to order.  
**AC-1:** Given on partner list, When loading, Then partners with names and locations displayed.  
**AC-2:** Given partner selected, When viewing menu, Then items with names and prices shown.  
**AC-3:** Given inactive partner selected, When viewing menu, Then customer cannot order.

---

## Service: Agent Service

**Size:** M  
**Priority:** High  
**US-17:** Agent Registration  
**As an** new agent,  
**I want** to register account,  
**So that** I can pick deliveries.  
**AC-1:** Given valid details, When submitting, Then account created.

---

**Size:** M  
**Priority:** Medium  
**US-18:** Agent Availability Toggle  
**As an** agent,  
**I want** to set available/unavailable,  
**So that** I receive offers when ready.  
**AC-1:** Given on dashboard, When toggling, Then status updated immediately.  
**AC-2:** Given available, When changed, Then added to delivery pool.  
**AC-3:** Given change, When persisted, Then reflected across sessions.

---

**Size:** M  
**Priority:** Low  
**US-19:** View Performance Stats  
**As an** agent,  
**I want** to view stats,  
**So that** I improve bonus eligibility.  
**AC-1:** Given logged in, When requesting, Then KPIs like completed orders shown.

---

**Size:** M  
**Priority:** Low  
**US-20:** View Agent Bonus History  
**As an** agent,  
**I want** to view bonus history,  
**So that** I see extra earnings.  
**AC-1:** Given authenticated, When viewing, Then bonus history displayed.

---

## Service: Feedback Hub Service

**Size:** M  
**Priority:** High  
**US-21:** Submit Service Rating  
**As a** customer,  
**I want** to review the order,  
**So that** quality is maintained.  
**AC-1:** Given Delivered, When submitting 1-5 stars and comment on food, agent and order, Then review created.  
**AC-2:** Given input, When validating, Then 1-5 integer, comment <500 chars, prevent injections.

---

**Size:** M  
**Priority:** Medium  
**US-22:** View Reviews  
**As a** management user,  
**I want** to view filtered reviews,  
**So that** I analyze feedback.  
**AC-1:** Given authenticated, When requesting with date range, Then list returned.

---

## Service: Agent Bonus Service

---

**Size:** L  
**Priority:** Medium  
**US-23:** Calculate Monthly Bonus  
**As a** system,  
**I want** to calculate agent bonuses,  
**So that** performance is incentivized.  
**AC-1:** Given end of cycle, When batch runs, Then aggregates ratings from previous month.  
**AC-2:** Given >=20 deliveries, Then bonus computed per formula.

---

## Service: WebSocket Services

**Size:** L  
**Priority:** Medium  
**US-24:** Real-Time Order Updates  
**As a** customer,  
**I want** instant status updates,  
**So that** I track without refresh.  
**AC-1:** Given on tracking page, When status changes, Then UI updates via WebSocket.  
**AC-2:** Given connection drops, When occurring, Then reconnect with backoff.

---

**Size:** M  
**Priority:** High  
**US-25:** Real-Time Notifications for Partner  
**As a** partner,  
**I want** notifications for new orders,  
**So that** I respond quickly.  
**AC-1:** Given connected to WebSocket, When OrderCreated or AgentAssigned, Then partner is notified.

---

**Size:** M  
**Priority:** High  
**US-26:** Notify Agents When Orders Are Accepted  
**As an** agent,  
**I want** notifications for new orders,  
**So that** I know about new delivery opportunities.  
**AC-1:** Given connected, When OrderAccepted, Then agent is notified.

---

**Size:** M  
**Priority:** High  
**US-27:** Real-Time Notifications for Agent  
**As an** agent,  
**I want** notifications for agent assigned orders,  
**So that** I pick up deliveries when they are ready.  
**AC-1:** Given connected, When OrderReady, Then agent is notified.

---

## Service: Notification Service

**Size:** S  
**Priority:** Medium  
**US-28:** Send Status Notifications  
**As a** system,  
**I want** to send updates,  
**So that** users are informed.  
**AC-1:** Given status change, When triggered, Then sms/push sent via Legacy Notification Service.

---

## External Service: Analytics Service

**Size**: L  
**Priority**: Medium  
**US-29:** View Business Metrics  
**As a** management user,  
**I want** to view key business metrics,  
**So that** I can make informed decisions.  
**AC-1:** Given authenticated, When requesting dashboard, Then metrics like total orders, active partners displayed.

---

**Size:** M  
**Priority:** Low  
**US-30:** Monitor System Health Metrics  
**As a** DevOps engineer,  
**I want** to monitor system health metrics,  
**So that** I can ensure platform reliability.  
**AC-1:** Given access to monitoring tool, When viewing metrics, Then CPU usage, memory consumption, request latency displayed.

---

**Size:** S  
**Priority:** Medium  
**US-31:** Partner Notified When Order Picked Up  
**As a** partner,  
**I want** to be notified when an agent picks up an order,  
**So that** the order is removed from my active orders view.  
**AC-1:** Given connected to WebSocket, When OrderPickedUp event occurs, Then order is removed from accepted orders list.

---
