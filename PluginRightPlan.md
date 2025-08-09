# PluginRight v1 – Step 1 Plan

## 1. Login / Registration
- Frontend in e.g. Next.js (hosted on Vercel for quick setup).
- Auth via **Google** and **LinkedIn** (Auth0 or Firebase Auth → Postgres/Supabase).
- Simple user profile:
  - ID
  - Name / email
  - Number of available plugin generations (default = 1 free)
  - History (date, input, generated code)

## 2. Free Plugin Generation
- API endpoint `/generate` (Python + OpenAI integration).
- Accepts:
  - Name / namespace
  - Description of logic
  - Step type (Create, Update, Delete)
  - Entity name
- Returns:
  - `.zip` containing:
    - `.csproj` (4.6.2 targeting)
    - Plugin class(es)
    - Minimal README (“how to compile”)
- Backend logs that the free token has been used.

## 3. Payment Solution
- Stripe integration (Checkout/Payments API).
- Packages:
  - **1 generation** – $X
  - **5 generations** – $Y
  - **Unlimited for 1 month** – $Z
- On payment: increase the user's token balance in the database.

## 4. Hosting
- **Frontend:** Vercel/Netlify.
- **Backend:** Railway/Fly.io/Azure Functions.
- **DB:** Supabase (Postgres + Auth sync).
- SSL and `pluginright.com` points to frontend.

---

✅ **Roadmap item:** *“Deliver compiled, signed DLL”* goes into **v2**, when we build the Windows build infrastructure.
